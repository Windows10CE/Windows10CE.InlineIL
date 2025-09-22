using System.Diagnostics;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;

namespace Windows10CE.InlineIL.Processor;

public class InlineILAstVisitor : IAstNodeVisitor<CilInstruction, MethodState>
{
    public static InlineILAstVisitor Instance { get; } = new();
    
    public void Visit(CompilationUnit<CilInstruction> unit, MethodState state) => unit.Root.Accept(this, state);

    public void Visit(AssignmentStatement<CilInstruction> statement, MethodState state)
    {
        if (statement.UserData is not null) return;
        statement.Expression.Accept(this, state);
        statement.UserData = statement.Expression.UserData;
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

    public void Visit(InstructionExpression<CilInstruction> expression, MethodState state)
    {
        if (expression.UserData is not null) return;
        if (!state.IsInILExpression && !AssemblyProcessor.IsInlineILCommand(expression.Instruction))
        {
            return;
        }

        var oldFlag = state.IsInILExpression;
        state.IsInILExpression = true;
        switch (expression.Instruction.OpCode.Code)
        {
            case CilCode.Call:
            case CilCode.Callvirt:
                var method = (IMethodDefOrRef)expression.Instruction.Operand!;
                if (method.Name == "Throw")
                {
                    state.ReplacementMap[expression.Instruction] = new CilInstruction(CilOpCodes.Throw);
                }
                else if (method.Name == "Ldarg")
                {
                    expression.Arguments[0].Accept(this, state);
                    var pname = (UserData.String)expression.Arguments[0].UserData!;
                    state.ReplacementMap[expression.Instruction] = new CilInstruction(CilOpCodes.Ldarg,
                        state.Method.Parameters.Single(p => p.Name == pname.Value));
                }
                break;

            case CilCode.Ldstr:
                expression.UserData = new UserData.String((string)expression.Instruction.Operand!);
                state.ReplacementMap[expression.Instruction] = null;
                break;
        }
        state.IsInILExpression = oldFlag;
    }

    public void Visit(VariableExpression<CilInstruction> expression, MethodState state)
    {
        var writes = expression.Variable.GetIsWrittenBy(state.Compilation);
        Debug.Assert(writes.Count == 1);
        writes[0].Accept(this, state);
        expression.UserData = writes[0].UserData;
    }
}