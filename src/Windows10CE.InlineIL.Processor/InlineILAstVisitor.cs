using System.Collections.Immutable;
using System.Diagnostics;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;

namespace Windows10CE.InlineIL.Processor;

internal class InlineILAstVisitor : IAstNodeVisitor<CilInstruction, MethodState>
{
    public static InlineILAstVisitor Instance { get; } = new();
    
    public void Visit(CompilationUnit<CilInstruction> unit, MethodState state) => unit.Root.Accept(this, state);

    public void Visit(AssignmentStatement<CilInstruction> statement, MethodState state)
    {
        if (statement.UserData is not null) return;
        statement.UserData = GetUserData<UserData>(statement.Expression, state);
    }

    public void Visit(ExpressionStatement<CilInstruction> statement, MethodState state)
    {
        if (statement.UserData is not null) return;
        statement.Expression.Accept(this, state);
        statement.UserData = statement.Expression.UserData;
    }

    public void Visit(PhiStatement<CilInstruction> statement, MethodState state)
    {
        if (state.IsInILExpression)
        {
            throw new NotSupportedException();
        }
    }

    public void Visit(BlockStatement<CilInstruction> statement, MethodState state)
    {
        foreach (var inner in statement.Statements)
        {
            inner.Accept(this, state);
        }
    }

    public void Visit(ExceptionHandlerStatement<CilInstruction> statement, MethodState state)
    {
        statement.ProtectedBlock.Accept(this, state);
        foreach (var handler in statement.Handlers)
        {
            handler.Accept(this, state);
        }
    }

    public void Visit(HandlerClause<CilInstruction> clause, MethodState state)
    {
        clause.Prologue?.Accept(this, state);
        clause.Contents.Accept(this, state);
        clause.Epilogue?.Accept(this, state);
    }

    private TUserData GetUserData<TUserData>(AstNode<CilInstruction> node, MethodState state)
    {
        if (node.UserData is not UserData)
        {
            node.Accept(this, state);
        }
        return (TUserData)node.UserData!;
    }

    public void Visit(InstructionExpression<CilInstruction> expression, MethodState state)
    {
        if (expression.UserData is not null) return;

        if (state.IsInILExpression)
        {
            HandleInlineExpression(expression, state);
            return;
        }

        if (!state.IsInILExpression && !AssemblyProcessor.IsInlineILCommand(expression.Instruction))
        {
            foreach (var inner in expression.Arguments)
            {
                inner.Accept(this, state);
            }
            return;
        }

        var oldFlag = state.IsInILExpression;
        state.IsInILExpression = true;
        switch (expression.Instruction.OpCode.Code)
        {
            case CilCode.Call:
            case CilCode.Callvirt:
                var method = (IMethodDescriptor)expression.Instruction.Operand!;
                if (Enum.TryParse<CilCode>(method.Name, out var code))
                {
                    var opcode = code.ToOpCode();
                    object? operand = null;
                    switch (opcode.OperandType)
                    {
                        case CilOperandType.InlineNone:
                            break;
                        case CilOperandType.InlineString:
                            operand = GetUserData<UserData.String>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.InlineI:
                            operand = GetUserData<UserData.Int32>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.InlineI8:
                            operand = GetUserData<UserData.Int64>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.ShortInlineR:
                            operand = GetUserData<UserData.Single>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.InlineR:
                            operand = GetUserData<UserData.Double>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.InlineVar:
                            operand = GetUserData<UserData.LocalReference>(expression.Arguments[0], state).Variable;
                            break;
                        case CilOperandType.InlineArgument:
                            operand = GetUserData<UserData.ParameterReference>(expression.Arguments[0], state).Parameter;
                            break;
                        case CilOperandType.InlineType:
                            operand = GetUserData<UserData.ConstructedType>(expression.Arguments[0], state).Signature.ToTypeDefOrRef();
                            break;
                        case CilOperandType.InlineTok:
                            IMetadataMember member = GetUserData<UserData>(expression.Arguments[0], state) switch
                            {
                                // not sure how this happens but sure
                                UserData.MetadataMember mm => mm.Member,
                                UserData.ConstructedType ct => ct.Signature.ToTypeDefOrRef(),
                                UserData.ConstructedMethod cm => cm.ToMethodDescriptor(),
                                _ => throw new InvalidOperationException(),
                            };
                            operand = member;
                            break;
                        case CilOperandType.InlineMethod:
                            operand = GetUserData<UserData.ConstructedMethod>(expression.Arguments[0], state).ToMethodDescriptor();
                            break;
                        case CilOperandType.InlineField:
                            operand = GetUserData<UserData.ConstructedField>(expression.Arguments[0], state).ToFieldDescriptor();
                            break;
                        case CilOperandType.InlineSig:
                            operand = GetUserData<UserData.ConstructedMethodSignature>(expression.Arguments[0], state).ToSignature();
                            break;
                        case CilOperandType.InlineBrTarget:
                            operand = GetUserData<UserData>(expression.Arguments[0], state) switch
                            {
                                UserData.LocalReference lr => GetLabelForLocal(lr.Variable, state),
                                UserData.Label label => label,
                                _ => throw new InvalidOperationException(),
                            };
                            break;
                    }
                    state.ReplacementMap[expression.Instruction] = new CilInstruction(opcode, operand);
                }
                else if (method.Name!.Value is "Push" or "Return" or "ReturnRef" or "ReturnPointer")
                {
                    state.ReplacementMap[expression.Instruction] = null;
                }
                else if (method.Name!.Value is "Mark" or "DefineAndMark")
                {
                    var local = GetUserData<UserData.LocalReference>(expression.Arguments[0], state).Variable;
                    var label = GetLabelForLocal(local, state);
                    state.ReplacementMap[expression.Instruction] = null;
                    state.LabelFixups[expression.Instruction] = label;
                }
                break;
        }
        state.IsInILExpression = oldFlag;
    }

    private void HandleInlineExpression(InstructionExpression<CilInstruction> expression, MethodState state)
    {
        state.ReplacementMap[expression.Instruction] = null;
        switch (expression.Instruction.OpCode.Code)
        {
            case CilCode.Call:
            case CilCode.Callvirt:
                var method = (IMethodDefOrRef)expression.Instruction.Operand!;
                switch (method.Name)
                {
                    case "Create" when method.DeclaringType!.Name == "MethodRef":
                    {
                        expression.UserData = new UserData.ConstructedMethod(
                            GetUserData<UserData.ConstructedType>(expression.Arguments[0], state).Signature.ToTypeDefOrRef(),
                            GetUserData<UserData.String>(expression.Arguments[1], state).Value,
                            GetUserData<UserData.ConstructedType>(expression.Arguments[2], state).Signature,
                            (CallingConventionAttributes)GetUserData<UserData.Int32>(expression.Arguments[3], state).Value,
                            ImmutableList<TypeSignature>.Empty,
                            ImmutableList<TypeSignature>.Empty
                        );
                        break;
                    }
                    case "Create" when method.DeclaringType!.Name == "MethodSig":
                    {
                        expression.UserData = new UserData.ConstructedMethodSignature(
                            GetUserData<UserData.ConstructedType>(expression.Arguments[0], state).Signature,
                            (CallingConventionAttributes)GetUserData<UserData.Int32>(expression.Arguments[1], state).Value,
                            ImmutableList<TypeSignature>.Empty
                        );
                        break;
                    }
                    case "Create" when method.DeclaringType!.Name == "FieldRef":
                    {
                        expression.UserData = new UserData.ConstructedField(
                            GetUserData<UserData.ConstructedType>(expression.Arguments[0], state).Signature.ToTypeDefOrRef(),
                            GetUserData<UserData.String>(expression.Arguments[1], state).Value,
                            GetUserData<UserData.ConstructedType>(expression.Arguments[2], state).Signature,
                            (CallingConventionAttributes)GetUserData<UserData.Int32>(expression.Arguments[3], state).Value
                        );
                        break;
                    }
                    case "GenericTypeParam":
                    {
                        var index = GetUserData<UserData.Int32>(expression.Arguments[0], state).Value;
                        expression.UserData = new UserData.ConstructedType(new GenericParameterSignature(state.Method.Module, GenericParameterType.Type, index));
                        break;
                    }
                    case "GenericMethodParam":
                    {
                        var index = GetUserData<UserData.Int32>(expression.Arguments[0], state).Value;
                        expression.UserData = new UserData.ConstructedType(new GenericParameterSignature(state.Method.Module, GenericParameterType.Method, index));
                        break;
                    }
                    case "WithParameter":
                    {
                        var originalMethod = GetUserData<UserData>(expression.Arguments[0], state);
                        var newParam = GetUserData<UserData.ConstructedType>(expression.Arguments[1], state).Signature;
                        expression.UserData = originalMethod switch
                        {
                            UserData.ConstructedMethod cm => cm with { ParameterTypes = cm.ParameterTypes.Add(newParam) },
                            UserData.ConstructedMethodSignature cms => cms with { ParameterTypes = cms.ParameterTypes.Add(newParam) },
                            _ => throw new NotSupportedException(),
                        };
                        break;
                    }
                    case "WithGenericArg":
                    {
                        var originalMethod = GetUserData<UserData.ConstructedMethod>(expression.Arguments[0], state);
                        var newArg = GetUserData<UserData.ConstructedType>(expression.Arguments[1], state).Signature;
                        expression.UserData = originalMethod with { GenericArguments = originalMethod.GenericArguments.Add(newArg) };
                        break;
                    }
                    case "MakePointerType":
                    {
                        var type = GetUserData<UserData.ConstructedType>(expression.Arguments[0], state).Signature;
                        expression.UserData = new UserData.ConstructedType(type.MakePointerType());
                        break;
                    }
                    case "MakeByRefType":
                    {
                        var type = GetUserData<UserData.ConstructedType>(expression.Arguments[0], state).Signature;
                        expression.UserData = new UserData.ConstructedType(type.MakeByReferenceType());
                        break;
                    }
                    case "op_Implicit":
                    {
                        expression.UserData = GetUserData<UserData>(expression.Arguments[0], state);
                        break;
                    }
                    case "GetTypeFromHandle":
                    {
                        var type = (ITypeDescriptor)GetUserData<UserData.MetadataMember>(expression.Arguments[0], state).Member;
                        expression.UserData = new UserData.ConstructedType(type.ToTypeSignature());
                        break;
                    }
                    case "Use":
                    {
                        var local = GetUserData<UserData.LocalReference>(expression.Arguments[0], state).Variable;
                        expression.UserData = GetLabelForLocal(local, state);
                        break;
                    }
                }
                break;
            case CilCode.Ldstr:
                expression.UserData = new UserData.String((string)expression.Instruction.Operand!);
                break;
            case CilCode.Ldtoken:
                expression.UserData = new UserData.MetadataMember((IMetadataMember)expression.Instruction.Operand!);
                break;
            case CilCode.Ldc_I4:
                expression.UserData = new UserData.Int32((int)expression.Instruction.Operand!);
                break;
            case CilCode.Ldc_I8:
                expression.UserData = new UserData.Int64((long)expression.Instruction.Operand!);
                break;
            case CilCode.Ldc_R4:
                expression.UserData = new UserData.Single((float)expression.Instruction.Operand!);
                break;
            case CilCode.Ldc_R8:
                expression.UserData = new UserData.Double((double)expression.Instruction.Operand!);
                break;
            case CilCode.Ldloc:
                expression.UserData = new UserData.LocalReference((CilLocalVariable)expression.Instruction.Operand!);
                break;
            case CilCode.Ldloca:
                expression.UserData = new UserData.LocalReference((CilLocalVariable)expression.Instruction.Operand!);
                break;
            case CilCode.Ldarga:
                expression.UserData = new UserData.ParameterReference((Parameter)expression.Instruction.Operand!);
                break;
        }
    }

    private UserData.Label GetLabelForLocal(CilLocalVariable local, MethodState state)
    {
        if (!state.Labels.TryGetValue(local, out var label))
        {
            state.Labels[local] = label = new UserData.Label(local);
        }

        return label;
    }

    public void Visit(VariableExpression<CilInstruction> expression, MethodState state)
    {
        var writes = expression.Variable.GetIsWrittenBy(state.Compilation);
        Debug.Assert(writes.Count == 1);
        expression.UserData = GetUserData<UserData>(writes[0], state);
    }
}