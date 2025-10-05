using System.Runtime.CompilerServices;
using AsmResolver;
using AsmResolver.Collections;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace Windows10CE.InlineIL.PortablePdb;

public static class MethodDefinitionExtensions
{
    private static readonly ConditionalWeakTable<MethodDefinition, MethodDefinitionExt> _state = new();
    
    private sealed class MethodDefinitionExt
    {
        public readonly LazyVariable<MethodDefinition, MethodDebugInformation?> _debugInfo  = new(method => method.GetMethodDebugInformation());
        
        public IList<LocalScope>? _localScopes;
    }
    
    extension(MethodDefinition method)
    {
        private MethodDefinitionExt Ext => _state.GetOrCreateValue(method);
        
        private MethodDebugInformation? GetMethodDebugInformation()
        {
            if (method.Module is SerializedModuleDefinition module)
            {
                return module.LookupMethodDebugInformation(new MetadataToken(TableIndex.MethodDebugInformation, method.MetadataToken.Rid));
            }

            return null;
        }

        public MethodDebugInformation? MethodDebugInformation
        {
            get => method.Ext._debugInfo.GetValue(method);
            set => method.Ext._debugInfo.SetValue(value);
        }

        private IList<LocalScope> GetLocalScopes()
        {
            if (method.Module is SerializedModuleDefinition module)
            {
                var range = module.GetLocalScopeRange(method.MetadataToken.Rid);
                var scopes = new OwnedCollection<MethodDefinition, LocalScope>(method, range.Count);
                foreach (var token in range)
                {
                    scopes.Add(module.LookupLocalScope(token)!);
                }
                return scopes;
            }

            return [];
        }

        public IList<LocalScope> LocalScopes
        {
            get
            {
                var ext = method.Ext;
                if (ext._localScopes is null)
                {
                    Interlocked.CompareExchange(ref ext._localScopes, method.GetLocalScopes(), null);
                }

                return ext._localScopes;
            }
        }
    }
}
