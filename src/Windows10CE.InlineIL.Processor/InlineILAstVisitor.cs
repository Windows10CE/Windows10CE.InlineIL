using System.Diagnostics;
using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;

namespace Windows10CE.InlineIL.Processor;

public class InlineILAstVisitor : IAstNodeVisitor<CilInstruction, MethodState>
{
    private static readonly 
    
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
        switch (expression.Instruction.OpCode.Code)
        {
            case CilCode.Call:
            case CilCode.Callvirt:
                
                break;
        }
    }

    public void Visit(VariableExpression<CilInstruction> expression, MethodState state)
    {
        var writes = expression.Variable.GetIsWrittenBy(state.Compilation);
        Debug.Assert(writes.Count == 1);
        writes[0].Accept(this, state);
        expression.UserData = writes[0].UserData;
    }
}