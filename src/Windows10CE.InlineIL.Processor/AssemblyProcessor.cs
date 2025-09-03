using System.Collections.Immutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Echo.Ast.Construction;
using Echo.Platforms.AsmResolver;

namespace Windows10CE.InlineIL.Processor;

public static class AssemblyProcessor
{
    private abstract record UserData
    {
        public bool ShouldRemove { get; init; }

        public sealed record String(string Value) : UserData;

        public sealed record Int32(int Value) : UserData;
        
        public sealed record ConstructedType(TypeSignature Signature) : UserData;

        public sealed record ConstructedMethod(
            TypeReference OwningType,
            string Name,
            TypeSignature ReturnType,
            ImmutableList<TypeSignature> ParameterTypes
        ) : UserData;
    }
    
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

            var flowGraph = body.ConstructSymbolicFlowGraph(out var dataGraph).Lift(purityClassifier);
            var offsetMap = dataGraph.Nodes.CreateOffsetMap();

            foreach (var statement in flowGraph.Nodes.SelectMany(node => node.Contents.Instructions))
            {
                
            }
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

        return scope?.Name == "Windows10CE.InlineIL";
    }
}
