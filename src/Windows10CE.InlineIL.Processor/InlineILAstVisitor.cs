using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;

namespace Windows10CE.InlineIL.Processor;

public class InlineILAstVisitor : IAstNodeVisitor<CilInstruction, MethodState>
{
    public void Visit(CompilationUnit<CilInstruction> unit, MethodState state) => unit.Root.Accept(this, state);

    public void Visit(AssignmentStatement<CilInstruction> statement, MethodState state)
    {
        
    }

    public void Visit(ExpressionStatement<CilInstruction> statement, MethodState state)
    {
        throw new NotImplementedException();
    }

    public void Visit(PhiStatement<CilInstruction> statement, MethodState state)
    {
        throw new NotImplementedException();
    }

    public void Visit(BlockStatement<CilInstruction> statement, MethodState state)
    {
        throw new NotImplementedException();
    }

    public void Visit(ExceptionHandlerStatement<CilInstruction> statement, MethodState state)
    {
        throw new NotImplementedException();
    }

    public void Visit(HandlerClause<CilInstruction> clause, MethodState state)
    {
        throw new NotImplementedException();
    }

    public void Visit(InstructionExpression<CilInstruction> expression, MethodState state)
    {
        throw new NotImplementedException();
    }

    public void Visit(VariableExpression<CilInstruction> expression, MethodState state)
    {
        throw new NotImplementedException();
    }
}