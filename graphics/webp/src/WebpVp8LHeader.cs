namespace webp;

internal class WebpVp8LHeader
{
    internal byte Signature;
    internal int Width;
    internal int Height;
    internal bool HasAlpha;
    internal int Version;

    internal WebpVp8LHeader()
    {
        Signature = 0;
        Width = 0;
        Height = 0;
        HasAlpha = false;
        Version = 0;
    }
}
