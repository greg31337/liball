namespace image;

internal class TiffIfdEntry
{
    internal int TagId;
    internal int TypeId;
    internal ulong Count;
    internal ulong ValueOrOffset;
    internal byte[] ValueField;
    internal long EntryOffset;
    internal long ValueFieldOffset;

    internal TiffIfdEntry()
    {
        TagId = 0;
        TypeId = 0;
        Count = 0;
        ValueOrOffset = 0;
        ValueField = new byte[0];
        EntryOffset = 0;
        ValueFieldOffset = 0;
    }
}
