namespace image;

internal class RiffChunk
{
    internal int Index;
    internal string FourCc;
    internal long Offset;
    internal long Length;
    internal long DataOffset;
    internal long PaddingOffset;
    internal bool HasPadding;
    internal bool PaddingValid;
    internal bool TypeValid;
    internal bool DataLimited;
    internal byte[] Data;

    internal RiffChunk()
    {
        Index = 0;
        FourCc = "";
        Offset = -1;
        Length = 0;
        DataOffset = -1;
        PaddingOffset = -1;
        HasPadding = false;
        PaddingValid = true;
        TypeValid = false;
        DataLimited = false;
        Data = new byte[0];
    }
}
