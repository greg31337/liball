namespace webp;

internal class WebpAlphaHeader
{
    internal byte RawHeader;
    internal byte CompressionMethod;
    internal byte FilterMethod;
    internal byte Preprocessing;
    internal byte ReservedBits;
    internal bool HasReservedBits;

    internal WebpAlphaHeader()
    {
        RawHeader = 0;
        CompressionMethod = 0;
        FilterMethod = 0;
        Preprocessing = 0;
        ReservedBits = 0;
        HasReservedBits = false;
    }
}
