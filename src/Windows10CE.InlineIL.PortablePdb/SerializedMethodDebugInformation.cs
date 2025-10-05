using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace Windows10CE.InlineIL.PortablePdb;

public class SerializedMethodDebugInformation : MethodDebugInformation
{
    private readonly ModuleReaderContext _context;
    private readonly MethodDebugInformationRow _row;
    
    public SerializedMethodDebugInformation(ModuleReaderContext context, MetadataToken token, in MethodDebugInformationRow row) : base(token)
    {
        _context = context;
        _row = row;
    }

    protected override Document? GetDocument() => _context.ParentModule.LookupDocument(new MetadataToken(TableIndex.Document, _row.Document));

    protected override SequencePointCollection GetSequencePoints()
    {
        if (!_context.PdbBlobStream!.TryGetBlobReaderByIndex(_row.SequencePoints, out var reader))
        {
            return new SequencePointCollection(this);
        }
        return SequencePointCollection.FromReader(_context, this, ref reader);
    }
}
