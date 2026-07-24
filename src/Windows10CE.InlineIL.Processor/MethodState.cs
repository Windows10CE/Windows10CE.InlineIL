using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;

namespace Windows10CE.InlineIL.Processor;

internal sealed class MethodState
{
    public required RuntimeContext RuntimeContext { get; init; }
    public required CompilationUnit<CilInstruction> Compilation { get; init; }
    public Dictionary<CilInstruction, CilInstruction?> ReplacementMap { get; } = new();
    public Dictionary<CilLocalVariable, LabelData> Labels { get; } = new();
    public Dictionary<CilInstruction, LabelData> LabelFixups { get; } = new();
    public required MethodDefinition Method { get; init; }
    public bool IsInILExpression { get; set; }
}
