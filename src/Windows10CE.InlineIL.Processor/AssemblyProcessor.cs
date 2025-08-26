using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Echo.Ast.Construction;
using Echo.Platforms.AsmResolver;

namespace Windows10CE.InlineIL.Processor;

public static class AssemblyProcessor
{
    public static void Process(string inputPath, IEnumerable<string> allReferences, string outputPath)
    {
        var asm = AssemblyDefinition.FromFile(inputPath);
        var module = asm.ManifestModule ?? throw new NotSupportedException("how did you even do this");

        var purityClassifier = new CilPurityClassifier
        {
            DefaultMethodAccessPurity = true,
            DefaultMethodCallPurity = true,
            DefaultTypeAccessPurity = true,
        };

        var file = File.Open(outputPath, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(file);
        
        foreach (var method in module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            if (method.CilMethodBody is null)
            {
                continue;
            }

            var body = method.CilMethodBody;

            if (!body.Instructions.Any(IsInlineILCommand))
            {
                continue;
            }

            var flowGraph = body.ConstructStaticFlowGraph().Lift(purityClassifier);
            
            flowGraph.ToDotGraph(writer);
        }
    }
    
    private static bool IsInlineILCommand(CilInstruction instruction)
    {
        var scope = instruction.Operand switch
        {
            TypeSpecification spec => spec.Scope,
            ITypeDescriptor td => td.Scope,
            IFieldDescriptor fd => fd.DeclaringType?.Scope,
            IMethodDescriptor md => md.DeclaringType?.Scope,
            _ => null
        };

        return scope?.Name == "InlineIL";
    }
}
