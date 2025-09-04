using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;
using Echo.DataFlow;

namespace Windows10CE.InlineIL.Processor;

public sealed class MethodState
{
    public required CompilationUnit<CilInstruction> Compilation { get; init; }
    public required DataFlowGraph<CilInstruction> DataFlowGraph { get; init; }
    public required Dictionary<long, DataFlowNode<CilInstruction>> OffsetMap { get; init; }
    public bool IsInILExpression { get; set; }
}