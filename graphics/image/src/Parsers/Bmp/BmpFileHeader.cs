namespace image;

internal class BmpFileHeader
{
    internal string Signature;
    internal uint FileSize;
    internal ushort Reserved1;
    internal ushort Reserved2;
    internal uint PixelDataOffset;
    internal byte[] Data;

    internal BmpFileHeader()
    {
        Signature = "";
        FileSize = 0;
        Reserved1 = 0;
        Reserved2 = 0;
        PixelDataOffset = 0;
        Data = new byte[0];
    }
}
