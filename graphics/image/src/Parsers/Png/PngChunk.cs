namespace image;

internal class PngChunk
{
    internal int Index;
    internal string Type;
    internal long Offset;
    internal long Length;
    internal long DataOffset;
    internal long CrcOffset;
    internal uint ExpectedCrc;
    internal uint ActualCrc;
    internal bool CrcValid;
    internal bool TypeValid;
    internal bool Critical;
    internal bool Public;
    internal bool ReservedBitValid;
    internal bool SafeToCopy;
    internal bool DataLimited;
    internal byte[] Data;

    internal PngChunk()
    {
        Index = 0;
        Type = "";
        Offset = -1;
        Length = 0;
        DataOffset = -1;
        CrcOffset = -1;
        ExpectedCrc = 0;
        ActualCrc = 0;
        CrcValid = false;
        TypeValid = false;
        Critical = false;
        Public = false;
        ReservedBitValid = false;
        SafeToCopy = false;
        DataLimited = false;
        Data = new byte[0];
    }
}
