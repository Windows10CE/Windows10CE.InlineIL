using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace Windows10CE.InlineIL.PortablePdb;

public class SerializedLocalScope : LocalScope
{
    public SerializedLocalScope(ModuleReaderContext context, MetadataToken token, in LocalScopeRow row) : base(token)
    {
        
    }
}
