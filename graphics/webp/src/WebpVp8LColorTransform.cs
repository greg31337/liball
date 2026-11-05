using System;

namespace webp;

internal class WebpVp8LColorTransform
{
    internal int SizeBits;
    internal int Width;
    internal int Height;
    internal WebpVp8LColorTransformElement[] Elements;

    internal WebpVp8LColorTransform()
    {
        SizeBits = 0;
        Width = 0;
        Height = 0;
        Elements = Array.Empty<WebpVp8LColorTransformElement>();
    }
}
