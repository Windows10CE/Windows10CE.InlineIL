using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace Windows10CE.InlineIL.PortablePdb;

internal static class TablesStreamExtensions
{
    extension(TablesStream tables)
    {
        public MetadataRange GetPdbMemberRange<TOwnerRow>(
            TablesStream pdbTables,
            TableIndex ownerTableIndex,
            uint ownerRid,
            int ownerColumnIndex,
            TableIndex memberTableIndex)
            where TOwnerRow : struct, IMetadataRow
        {
            int index = (int) (ownerRid - 1);

            // Check if valid owner RID.
            var ownerTable = tables.GetTable<TOwnerRow>(ownerTableIndex);
            if (index < 0 || index >= ownerTable.Count)
                return MetadataRange.Empty;

            // Obtain boundaries.
            uint startRid = ownerTable[index][ownerColumnIndex];
            uint endRid = index < ownerTable.Count - 1
                ? ownerTable[index + 1][ownerColumnIndex]
                : (uint) pdbTables.GetTable(memberTableIndex).Count + 1;

            // If not, its a simple range.
            return new MetadataRange(memberTableIndex, startRid, endRid);
        }
    }
}
