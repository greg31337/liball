namespace image;

internal class JpegXlExtraChannel
{
    internal uint Type;
    internal uint BitsPerSample;
    internal uint ExponentBitsPerSample;
    internal uint DimensionShift;
    internal string Name;
    internal bool AlphaPremultiplied;

    internal JpegXlExtraChannel()
    {
        Type = 0;
        BitsPerSample = 8;
        ExponentBitsPerSample = 0;
        DimensionShift = 0;
        Name = "";
        AlphaPremultiplied = false;
    }
}
