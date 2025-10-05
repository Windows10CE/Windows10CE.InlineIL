using AsmResolver.Collections;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace Windows10CE.InlineIL.PortablePdb;

public class LocalScope : IMetadataMember, IOwnedCollectionElement<MethodDefinition>
{
    public LocalScope() { }

    public LocalScope(MetadataToken token)
    {
        MetadataToken = token;
    }
    
    public MetadataToken MetadataToken { get;  }
    
    public virtual MethodDefinition? Owner { get; set; }
}
