namespace image;

internal class PsdHeader
{
    internal string Signature;
    internal int Version;
    internal bool ReservedValid;
    internal int ChannelCount;
    internal uint Height;
    internal uint Width;
    internal int BitDepth;
    internal int ColorMode;
    internal bool SignatureValid;
    internal bool VersionValid;
    internal bool ChannelCountValid;
    internal bool DimensionsValid;
    internal bool BitDepthValid;
    internal bool ColorModeValid;

    internal PsdHeader()
    {
        Signature = "";
        Version = 0;
        ReservedValid = false;
        ChannelCount = 0;
        Height = 0;
        Width = 0;
        BitDepth = 0;
        ColorMode = 0;
        SignatureValid = false;
        VersionValid = false;
        ChannelCountValid = false;
        DimensionsValid = false;
        BitDepthValid = false;
        ColorModeValid = false;
    }
}
