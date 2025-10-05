using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using Echo.Ast;

namespace Windows10CE.InlineIL.Processor;

internal sealed class MethodState
{
    public required CompilationUnit<CilInstruction> Compilation { get; init; }
    public Dictionary<CilInstruction, CilInstruction?> ReplacementMap { get; } = new();
    public Dictionary<CilLocalVariable, UserData.Label> Labels { get; } = new();
    public Dictionary<CilInstruction, UserData.Label> LabelFixups { get; } = new();
    public required MethodDefinition Method { get; init; }
    public bool IsInILExpression { get; set; }
}