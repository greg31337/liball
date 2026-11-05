namespace image;

internal class PsdSection
{
    internal string Group;
    internal string Name;
    internal long Offset;
    internal int LengthFieldSize;
    internal long DataOffset;
    internal ulong DeclaredLength;
    internal long AvailableLength;
    internal bool Truncated;
    internal bool Limited;
    internal int Compression;
    internal bool CompressionValid;
    internal MetadataDirectory Directory;

    internal PsdSection()
    {
        Group = "";
        Name = "";
        Offset = -1;
        LengthFieldSize = 0;
        DataOffset = -1;
        DeclaredLength = 0;
        AvailableLength = 0;
        Truncated = false;
        Limited = false;
        Compression = -1;
        CompressionValid = false;
        Directory = new MetadataDirectory();
    }
}
