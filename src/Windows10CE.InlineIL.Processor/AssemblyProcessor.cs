using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.IO;
using AsmResolver.PE;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Echo;
using Echo.Ast.Construction;
using Echo.Platforms.AsmResolver;
using Windows10CE.InlineIL.PortablePdb;

namespace Windows10CE.InlineIL.Processor;

public static class AssemblyProcessor
{
    public static void Process(string inputPath, IEnumerable<string> allReferences, string outputPath, string targetFramework, string debugType, string? pdbPath)
    {
        var resolver = new PathAssemblyResolver([inputPath, ..allReferences], targetFramework);

        var asmImage = PEImage.FromFile(inputPath);

        pdbPath ??= Path.ChangeExtension(inputPath, ".pdb");

        var pdbMetadata = debugType switch
        {
            "portable" => MetadataDirectory.FromFile(pdbPath),
            "embedded" => asmImage.DotNetDirectory?.Metadata,
            "full" or "pdbonly" => null, // TODO: emit warning
            _ => null,
        };

        var module = new SerializedModuleDefinition(asmImage, resolver.ReaderParameters);
        module.ReaderContext.PdbDirectory = pdbMetadata;

        var asm = module.Assembly!;
        resolver.AddToCache(asm, asm);

        Process(asm.ManifestModule!);
        asm.Write(outputPath);
        if (debugType == "portable")
        {
            using var pdbFile = File.Open(pdbPath, FileMode.Create, FileAccess.Write);
            pdbMetadata!.Write(new BinaryStreamWriter(pdbFile));
        }
    }

    public static void Process(ModuleDefinition module)
    {
        var purityClassifier = new CilPurityClassifier
        {
            DefaultMethodAccessPurity = true,
            DefaultMethodCallPurity = Trilean.Unknown,
            DefaultTypeAccessPurity = true,
        };
        
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

            var instructions = body.Instructions;

            instructions.ExpandMacros();

            var compilation = body.ConstructStaticFlowGraph().Lift(purityClassifier).ToCompilationUnit();

            var methodState = new MethodState
            {
                Compilation = compilation,
                Method = method,
            };

            compilation.Accept(InlineILAstVisitor.Instance, methodState);

            for (int i = instructions.Count - 1; i >= 0; i--)
            {
                var instruction = instructions[i];
                if (methodState.LabelFixups.TryGetValue(instruction, out var label))
                {
                    if (i == instructions.Count - 1)
                    {
                        label.CilLabel = instructions.EndLabel;
                    }
                    else
                    {
                        label.CilLabel = new CilInstructionLabel(instructions[i + 1]);
                    }
                }
                if (methodState.ReplacementMap.TryGetValue(instruction, out var replacement))
                {
                    instructions.RemoveAt(i);
                    if (replacement is not null)
                    {
                        instructions.Insert(i, replacement);
                    }
                }
            }

            for (int i = body.LocalVariables.Count - 1; i >= 0; i--)
            {
                var local = body.LocalVariables[i];
                if (local.VariableType.Scope?.Name == "Windows10CE.InlineIL")
                {
                    body.LocalVariables.RemoveAt(i);
                }
            }

            instructions.OptimizeMacros();
        }
    }
    
    internal static bool IsInlineILCommand(CilInstruction instruction)
    {
        var scope = instruction.Operand switch
        {
            ITypeDescriptor td => td.Scope,
            IFieldDescriptor fd => fd.DeclaringType?.Scope,
            IMethodDescriptor md => md.DeclaringType?.Scope,
            _ => null,
        };

        return scope?.Name == "Windows10CE.InlineIL";
    }
}
