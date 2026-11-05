namespace image;

internal class BmpDibHeader
{
    internal uint HeaderSize;
    internal string HeaderType;
    internal int Width;
    internal int Height;
    internal ushort Planes;
    internal ushort BitCount;
    internal uint Compression;
    internal uint ImageSize;
    internal int XPelsPerMeter;
    internal int YPelsPerMeter;
    internal uint ColorsUsed;
    internal uint ColorsImportant;
    internal uint RedMask;
    internal uint GreenMask;
    internal uint BlueMask;
    internal uint AlphaMask;
    internal uint ColorSpaceType;
    internal byte[] ColorSpaceEndpoints;
    internal uint GammaRed;
    internal uint GammaGreen;
    internal uint GammaBlue;
    internal uint Intent;
    internal uint ProfileDataOffset;
    internal uint ProfileSize;
    internal uint Reserved;
    internal ushort Units;
    internal ushort Recording;
    internal ushort Rendering;
    internal uint Size1;
    internal uint Size2;
    internal uint ColorEncoding;
    internal uint Identifier;
    internal byte[] Data;

    internal BmpDibHeader()
    {
        HeaderSize = 0;
        HeaderType = "";
        Width = 0;
        Height = 0;
        Planes = 0;
        BitCount = 0;
        Compression = 0;
        ImageSize = 0;
        XPelsPerMeter = 0;
        YPelsPerMeter = 0;
        ColorsUsed = 0;
        ColorsImportant = 0;
        RedMask = 0;
        GreenMask = 0;
        BlueMask = 0;
        AlphaMask = 0;
        ColorSpaceType = 0;
        ColorSpaceEndpoints = new byte[0];
        GammaRed = 0;
        GammaGreen = 0;
        GammaBlue = 0;
        Intent = 0;
        ProfileDataOffset = 0;
        ProfileSize = 0;
        Reserved = 0;
        Units = 0;
        Recording = 0;
        Rendering = 0;
        Size1 = 0;
        Size2 = 0;
        ColorEncoding = 0;
        Identifier = 0;
        Data = new byte[0];
    }
}
