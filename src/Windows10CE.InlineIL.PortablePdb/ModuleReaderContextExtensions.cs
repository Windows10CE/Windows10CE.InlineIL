using System.Runtime.CompilerServices;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Metadata;

namespace Windows10CE.InlineIL.PortablePdb;

public static class ModuleReaderContextExtensions
{
    private static readonly ConditionalWeakTable<ModuleReaderContext, StrongBox<MetadataDirectory?>> _table = new();

    extension(ModuleReaderContext context)
    {
        public MetadataDirectory? PdbDirectory
        {
            get => _table.GetOrCreateValue(context).Value;
            set => _table.GetOrCreateValue(context).Value = value;
        }

        public TablesStream? PdbTablesStream => context.PdbDirectory?.GetStream<TablesStream>();

        public BlobStream? PdbBlobStream => context.PdbDirectory?.GetStream<BlobStream>();
    }
}
