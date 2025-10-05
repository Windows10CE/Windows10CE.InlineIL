using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace Windows10CE.InlineIL.PortablePdb;

public static class ModuleDefinitionExtensions
{
    private static readonly ConditionalWeakTable<ModuleDefinition, ModuleDefinitionExt> _state = new();

    private sealed class ModuleDefinitionExt
    {
        public readonly LazyRidListRelation2<MethodDefinitionRow>? _localScopeLists;
        public LocalScope?[]? _localScopes;
        public MethodDebugInformation?[]? _methodDebugInformations;
        public Document?[]? _documents;
        
        public ModuleDefinitionExt(ModuleDefinition module)
        {
            if (module.ReaderContext is not { PdbDirectory: not null } context)
            {
                return;
            }

            _localScopeLists = new LazyRidListRelation2<MethodDefinitionRow>(context.PdbDirectory!,
                context.Metadata,
                TableIndex.LocalScope,
                TableIndex.Method,
                (rid, _) => rid,
                rid =>
                {
                    var table = context.PdbTablesStream!.GetTable<LocalScopeRow>();
                    if (TryGetRidEdgeByKey(table, 0, rid, out var left, leftmost: true) && TryGetRidEdgeByKey(table, 0, rid, out var right, leftmost: false))
                    {
                        return new MetadataRange(TableIndex.LocalScope, left, right);
                    }

                    return MetadataRange.Empty;
                });
        }
    }
    
    public static bool TryGetRidEdgeByKey<TRow>(MetadataTable<TRow> table, int keyColumnIndex, uint key, out uint rid, bool leftmost)
        where TRow : struct, IMetadataRow
    {
        rid = 0;
        if (table.Count == 0)
            return false;

        int left = 0;
        int right = table.Count - 1;

        while (left <= right)
        {
            int m = (left + right) / 2;
            var currentRow = table.GetByRid((uint)m + 1);
            uint currentKey = currentRow[keyColumnIndex];

            if (currentKey > key)
            {
                right = m - 1;
            }
            else if (currentKey < key)
            {
                left = m + 1;
            }
            else if (leftmost)
            {
                if (m == 0 || table.GetByRid((uint)m)[keyColumnIndex] != key)
                {
                    rid = (uint) (m + 1);
                    return true;
                }

                right = m - 1;
            }
            else
            {
                if (m == table.Count - 1 || table.GetByRid((uint)m + 2)[keyColumnIndex] != key)
                {
                    rid = (uint) (m + 1);
                    return true;
                }

                left = m + 1;
            }
        }

        return false;
    }

    extension(ModuleDefinition module)
    {
        public ModuleReaderContext? ReaderContext => (module as SerializedModuleDefinition)?.ReaderContext;

        private ModuleDefinitionExt Ext => _state.GetValue(module, module => new ModuleDefinitionExt(module));
    }
    
    private static TMember? LookupOrCreateMember<TMember, TRow>(ModuleReaderContext context, TablesStream tables, ref TMember?[]? cache, MetadataToken token,
        Func<ModuleReaderContext, MetadataToken, TRow, TMember?> createMember)
        where TRow : struct, IMetadataRow
        where TMember : class, IMetadataMember
    {
        // Obtain table.
        var table = (MetadataTable<TRow>) tables.GetTable(token.Table);

        // Check if within bounds.
        if (token.Rid == 0 || token.Rid > table.Count)
            return null;

        // Allocate cache if necessary.
        if (cache is null)
            Interlocked.CompareExchange(ref cache, new TMember[table.Count], null);

        // Get or create cached member.
        int index = (int) token.Rid - 1;
        var member = cache[index];
        if (member is null)
        {
            member = createMember(context, token, table[index]);
            member = Interlocked.CompareExchange(ref cache[index], member, null)
                ?? member;
        }

        return member;
    }

    extension(SerializedModuleDefinition module)
    {
        public MetadataRange GetLocalScopeRange(uint methodRid) => module.Ext._localScopeLists!.GetMemberRange(methodRid);

        public LocalScope? LookupLocalScope(MetadataToken token)
        {
            var ext = module.Ext;

            if (module.ReaderContext.PdbTablesStream is null)
                return null;

            return LookupOrCreateMember<LocalScope, LocalScopeRow>(module.ReaderContext, module.ReaderContext.PdbTablesStream!, ref ext._localScopes, token, (context, token, row) => new SerializedLocalScope(context, token, row));
        }
        
        public MethodDebugInformation? LookupMethodDebugInformation(MetadataToken token)
        {
            var ext = module.Ext;

            if (module.ReaderContext.PdbTablesStream is null)
                return null;

            return LookupOrCreateMember<MethodDebugInformation, MethodDebugInformationRow>(module.ReaderContext, module.ReaderContext.PdbTablesStream!, ref ext._methodDebugInformations, token, (context, token, row) => new SerializedMethodDebugInformation(context, token, row));
        }
        
        public Document? LookupDocument(MetadataToken token)
        {
            var ext = module.Ext;

            if (module.ReaderContext.PdbTablesStream is null)
                return null;

            return LookupOrCreateMember<Document, DocumentRow>(module.ReaderContext, module.ReaderContext.PdbTablesStream!, ref ext._documents, token, (context, token, row) => new SerializedDocument(context, token, row));
        }
    }
}
