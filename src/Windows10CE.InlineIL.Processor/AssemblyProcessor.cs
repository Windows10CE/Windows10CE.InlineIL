using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Echo;
using Echo.Ast.Construction;
using Echo.Platforms.AsmResolver;

namespace Windows10CE.InlineIL.Processor;

public static class AssemblyProcessor
{
    public static void Process(string inputPath, IEnumerable<string> allReferences, string outputPath, string targetFramework)
    {
        var resolver = new PathAssemblyResolver([inputPath, ..allReferences], targetFramework);

        var asm = AssemblyDefinition.FromFile(inputPath, resolver.ReaderParameters);
        resolver.AddToCache(asm, asm);

        var module = asm.ManifestModule!;

        var purityClassifier = new CilPurityClassifier
        {
            DefaultMethodAccessPurity = true,
            DefaultMethodCallPurity = Trilean.Unknown,
            DefaultTypeAccessPurity = true,
        };

        using var file = File.Open(outputPath, FileMode.Create, FileAccess.Write);
        
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

            body.Instructions.ExpandMacros();

            var compilation = body.ConstructSymbolicFlowGraph(out var dataGraph).Lift(purityClassifier).ToCompilationUnit();
            var offsetMap = dataGraph.Nodes.CreateOffsetMap();

            var methodState = new MethodState
            {
                Compilation = compilation,
                DataFlowGraph = dataGraph,
                Method = method,
                OffsetMap = offsetMap,
            };

            compilation.Accept(InlineILAstVisitor.Instance, methodState);

            var instructions = body.Instructions;

            for (int i = instructions.Count - 1; i >= 0; i--)
            {
                var instruction = instructions[i];
                if (methodState.ReplacementMap.TryGetValue(instruction, out var replacement))
                {
                    instructions.RemoveAt(i);
                    if (replacement is not null)
                    {
                        instructions.Insert(i, replacement);
                    }
                }
            }

            instructions.OptimizeMacros();

            for (int i = body.LocalVariables.Count - 1; i >= 0; i--)
            {
                var local = body.LocalVariables[i];
                if (local.VariableType.Scope?.Name == "Windows10CE.InlineIL")
                {
                    body.LocalVariables.RemoveAt(i);
                }
            }
        }
        module.Write(file);
    }
    
    internal static bool IsInlineILCommand(CilInstruction instruction)
    {
        var scope = instruction.Operand switch
        {
            ITypeDescriptor td => td.Scope,
            IFieldDescriptor fd => fd.DeclaringType?.Scope,
            IMethodDescriptor md => md.DeclaringType?.Scope,
            _ => null
        };

        return scope?.Name == "Windows10CE.InlineIL";
    }
}
