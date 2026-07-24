using System.Collections.Concurrent;
using System.Collections.Immutable;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;

namespace Windows10CE.InlineIL.Processor;

/// <summary>
/// A custom <see cref="IAssemblyResolver"/> from a specific set of reference paths.
/// </summary>
internal sealed class PathAssemblyResolver(ImmutableArray<string> referencePaths) : IAssemblyResolver
{
    private readonly ImmutableArray<string> _referencePaths = referencePaths;
    
    /// <inheritdoc/>
    public ResolutionStatus Resolve(AssemblyDescriptor assembly, ModuleDefinition? originModule, out AssemblyDefinition? result)
    {
        result = null;
        
        // We can't load an assembly without a name
        if (assembly.Name is null)
        {
            return ResolutionStatus.InvalidReference;
        }

        // Find the first match in our list of reference paths, and load that assembly
        foreach (string path in _referencePaths)
        {
            if (Path.GetFileNameWithoutExtension(path).Equals(assembly.Name))
            {
                result = AssemblyDefinition.FromFile(path, originModule?.RuntimeContext?.DefaultReaderParameters, createRuntimeContext: false);
                return ResolutionStatus.Success;
            }
        }

        return ResolutionStatus.AssemblyNotFound;
    }
}