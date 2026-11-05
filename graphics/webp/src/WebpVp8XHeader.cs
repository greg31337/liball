namespace webp;

internal class WebpVp8XHeader
{
    internal byte FeatureFlags;
    internal byte ReservedFeatureBits;
    internal bool HasIccProfile;
    internal bool HasAlpha;
    internal bool HasExif;
    internal bool HasXmp;
    internal bool HasAnimation;
    internal bool HasReservedFeatureBits;
    internal bool HasReservedBytes;
    internal int CanvasWidth;
    internal int CanvasHeight;

    internal WebpVp8XHeader()
    {
        FeatureFlags = 0;
        ReservedFeatureBits = 0;
        HasIccProfile = false;
        HasAlpha = false;
        HasExif = false;
        HasXmp = false;
        HasAnimation = false;
        HasReservedFeatureBits = false;
        HasReservedBytes = false;
        CanvasWidth = 0;
        CanvasHeight = 0;
    }
}
