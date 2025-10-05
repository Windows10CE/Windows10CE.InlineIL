using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace Windows10CE.InlineIL.PortablePdb;

public class Document : IMetadataMember
{
    private readonly LazyVariable<Document, Utf8String?> _name;
    private readonly LazyVariable<Document, Guid> _hashAlgorithm;
    private readonly LazyVariable<Document, byte[]?> _hash;
    private readonly LazyVariable<Document, Guid> _language;
    
    public Document(MetadataToken token)
    {
        MetadataToken = token;

        _name = new LazyVariable<Document, Utf8String?>(doc => doc.GetName());
        _hashAlgorithm = new LazyVariable<Document, Guid>(doc => doc.GetHashAlgorithm());
        _hash = new LazyVariable<Document, byte[]?>(doc => doc.GetHash());
        _language = new LazyVariable<Document, Guid>(doc => doc.GetLanguage());
    }

    public MetadataToken MetadataToken { get; }

    public Utf8String? Name
    {
        get => _name.GetValue(this);
        set => _name.SetValue(value);
    }

    public Guid HashAlgorithm
    {
        get => _hashAlgorithm.GetValue(this);
        set => _hashAlgorithm.SetValue(value);
    }

    public byte[]? Hash
    {
        get => _hash.GetValue(this);
        set => _hash.SetValue(value);
    }

    public Guid Language
    {
        get => _language.GetValue(this);
        set => _language.SetValue(value);
    }

    protected virtual Utf8String? GetName() => null;
    
    protected virtual Guid GetHashAlgorithm() => Guid.Empty;

    protected virtual byte[]? GetHash() => null;

    protected virtual Guid GetLanguage() => Guid.Empty;
}
