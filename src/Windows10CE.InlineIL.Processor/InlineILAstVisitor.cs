using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
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

    private TUserData GetUserData<TUserData>(AstNode<CilInstruction> node, MethodState state) where TUserData : UserData
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
                            operand = GetUserData<StringData>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.InlineI:
                            operand = GetUserData<Int32Data>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.InlineI8:
                            operand = GetUserData<Int64Data>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.ShortInlineR:
                            operand = GetUserData<SingleData>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.InlineR:
                            operand = GetUserData<DoubleData>(expression.Arguments[0], state).Value;
                            break;
                        case CilOperandType.InlineVar:
                            operand = GetUserData<LocalReferenceData>(expression.Arguments[0], state).Variable;
                            break;
                        case CilOperandType.InlineArgument:
                            operand = GetUserData<ParameterReferenceData>(expression.Arguments[0], state).Parameter;
                            break;
                        case CilOperandType.InlineType:
                            operand = GetUserData<ConstructedTypeData>(expression.Arguments[0], state).Signature.ToTypeDefOrRef();
                            break;
                        case CilOperandType.InlineTok:
                            IMetadataMember member = GetUserData<UserData>(expression.Arguments[0], state) switch
                            {
                                // not sure how this happens but sure
                                MetadataMemberData mm => mm.Member,
                                ConstructedTypeData ct => ct.Signature.ToTypeDefOrRef(),
                                ConstructedMethodData cm => cm.ToMethodDescriptor(),
                                _ => throw new InvalidOperationException(),
                            };
                            operand = member;
                            break;
                        case CilOperandType.InlineMethod:
                            operand = GetUserData<ConstructedMethodData>(expression.Arguments[0], state).ToMethodDescriptor();
                            break;
                        case CilOperandType.InlineField:
                            operand = GetUserData<ConstructedFieldData>(expression.Arguments[0], state).ToFieldDescriptor();
                            break;
                        case CilOperandType.InlineSig:
                            operand = GetUserData<ConstructedMethodSignatureData>(expression.Arguments[0], state).ToSignature();
                            break;
                        case CilOperandType.InlineBrTarget:
                            operand = GetUserData<UserData>(expression.Arguments[0], state) switch
                            {
                                LocalReferenceData lr => GetLabelForLocal(lr.Variable, state),
                                LabelData label => label,
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
                    var local = GetUserData<LocalReferenceData>(expression.Arguments[0], state).Variable;
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
                        expression.UserData = new ConstructedMethodData(
                            GetUserData<ConstructedTypeData>(expression.Arguments[0], state).Signature.ToTypeDefOrRef(),
                            GetUserData<StringData>(expression.Arguments[1], state).Value,
                            GetUserData<ConstructedTypeData>(expression.Arguments[2], state).Signature,
                            (CallingConventionAttributes)GetUserData<Int32Data>(expression.Arguments[3], state).Value,
                            ImmutableList<TypeSignature>.Empty,
                            ImmutableList<TypeSignature>.Empty
                        );
                        break;
                    }
                    case "Create" when method.DeclaringType!.Name == "MethodSig":
                    {
                        expression.UserData = new ConstructedMethodSignatureData(
                            GetUserData<ConstructedTypeData>(expression.Arguments[0], state).Signature,
                            (CallingConventionAttributes)GetUserData<Int32Data>(expression.Arguments[1], state).Value,
                            ImmutableList<TypeSignature>.Empty
                        );
                        break;
                    }
                    case "Create" when method.DeclaringType!.Name == "FieldRef":
                    {
                        expression.UserData = new ConstructedFieldData(
                            GetUserData<ConstructedTypeData>(expression.Arguments[0], state).Signature.ToTypeDefOrRef(),
                            GetUserData<StringData>(expression.Arguments[1], state).Value,
                            GetUserData<ConstructedTypeData>(expression.Arguments[2], state).Signature,
                            (CallingConventionAttributes)GetUserData<Int32Data>(expression.Arguments[3], state).Value
                        );
                        break;
                    }
                    case "Create" when method.DeclaringType!.Name == "AssemblyRef":
                    {
                        var mod = state.Method.DeclaringModule!;
                        var desc = new ReflectionAssemblyDescriptor(mod, new AssemblyName(GetUserData<StringData>(expression.Arguments[0], state).Value));
                        expression.UserData = new ConstructedAssemblyData(desc.ImportWith(mod.DefaultImporter));
                        break;
                    }
                    case "GenericTypeParam":
                    {
                        var index = GetUserData<Int32Data>(expression.Arguments[0], state).Value;
                        expression.UserData = new ConstructedTypeData(new GenericParameterSignature(state.Method.DeclaringModule, GenericParameterType.Type, index));
                        break;
                    }
                    case "GenericMethodParam":
                    {
                        var index = GetUserData<Int32Data>(expression.Arguments[0], state).Value;
                        expression.UserData = new ConstructedTypeData(new GenericParameterSignature(state.Method.DeclaringModule, GenericParameterType.Method, index));
                        break;
                    }
                    case "WithParameter":
                    {
                        var originalMethod = GetUserData<UserData>(expression.Arguments[0], state);
                        var newParam = GetUserData<ConstructedTypeData>(expression.Arguments[1], state).Signature;
                        expression.UserData = originalMethod switch
                        {
                            ConstructedMethodData cm => cm with { ParameterTypes = cm.ParameterTypes.Add(newParam) },
                            ConstructedMethodSignatureData cms => cms with { ParameterTypes = cms.ParameterTypes.Add(newParam) },
                            _ => throw new NotSupportedException(),
                        };
                        break;
                    }
                    case "WithGenericArg" when method.DeclaringType!.Name == "MethodRef":
                    {
                        var originalMethod = GetUserData<ConstructedMethodData>(expression.Arguments[0], state);
                        var newArg = GetUserData<ConstructedTypeData>(expression.Arguments[1], state).Signature;
                        expression.UserData = originalMethod with { GenericArguments = originalMethod.GenericArguments.Add(newArg) };
                        break;
                    }
                    case "WithGenericArg" when method.DeclaringType!.Name == "TypeRef":
                    {
                        var originalType = GetUserData<ConstructedTypeData>(expression.Arguments[0], state).Signature;
                        var newArg = GetUserData<ConstructedTypeData>(expression.Arguments[1], state).Signature;
                        var args = originalType is GenericInstanceTypeSignature gits ? gits.TypeArguments : [];
                        expression.UserData = new ConstructedTypeData(originalType.GetUnderlyingTypeDefOrRef()!.MakeGenericInstanceType(state.RuntimeContext, [..args, newArg]));
                        break;
                    }
                    case "MakePointerType":
                    {
                        var type = GetUserData<ConstructedTypeData>(expression.Arguments[0], state).Signature;
                        expression.UserData = new ConstructedTypeData(type.MakePointerType());
                        break;
                    }
                    case "MakeByRefType":
                    {
                        var type = GetUserData<ConstructedTypeData>(expression.Arguments[0], state).Signature;
                        expression.UserData = new ConstructedTypeData(type.MakeByReferenceType());
                        break;
                    }
                    case "op_Implicit":
                    {
                        expression.UserData = GetUserData<UserData>(expression.Arguments[0], state);
                        break;
                    }
                    case "GetTypeFromHandle":
                    {
                        var type = (ITypeDescriptor)GetUserData<MetadataMemberData>(expression.Arguments[0], state).Member;
                        expression.UserData = new ConstructedTypeData(type.ToTypeSignature(state.RuntimeContext));
                        break;
                    }
                    case "CreateTypeRef":
                    {
                        var assembly = GetUserData<ConstructedAssemblyData>(expression.Arguments[0], state).AssemblyDescriptor;
                        var @namespace = GetUserData<StringData>(expression.Arguments[1], state).Value;
                        var name = GetUserData<StringData>(expression.Arguments[2], state).Value;
                        var isValueType = GetUserData<Int32Data>(expression.Arguments[3], state).Value != 0;
                        expression.UserData = new ConstructedTypeData(new TypeReference(state.Method.DeclaringModule!, assembly, @namespace, name).ToTypeSignature(isValueType));
                        break;
                    }
                    case "Use":
                    {
                        var local = GetUserData<LocalReferenceData>(expression.Arguments[0], state).Variable;
                        expression.UserData = GetLabelForLocal(local, state);
                        break;
                    }
                }
                break;
            case CilCode.Ldstr:
                expression.UserData = new StringData((string)expression.Instruction.Operand!);
                break;
            case CilCode.Ldtoken:
                expression.UserData = new MetadataMemberData((IMetadataMember)expression.Instruction.Operand!);
                break;
            case CilCode.Ldc_I4:
                expression.UserData = new Int32Data((int)expression.Instruction.Operand!);
                break;
            case CilCode.Ldc_I8:
                expression.UserData = new Int64Data((long)expression.Instruction.Operand!);
                break;
            case CilCode.Ldc_R4:
                expression.UserData = new SingleData((float)expression.Instruction.Operand!);
                break;
            case CilCode.Ldc_R8:
                expression.UserData = new DoubleData((double)expression.Instruction.Operand!);
                break;
            case CilCode.Ldloc:
            case CilCode.Ldloca:
                expression.UserData = new LocalReferenceData((CilLocalVariable)expression.Instruction.Operand!);
                break;
            case CilCode.Ldarg:
            case CilCode.Ldarga:
                expression.UserData = new ParameterReferenceData((Parameter)expression.Instruction.Operand!);
                break;
        }
    }

    private LabelData GetLabelForLocal(CilLocalVariable local, MethodState state)
    {
        if (!state.Labels.TryGetValue(local, out var label))
        {
            state.Labels[local] = label = new LabelData(local);
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