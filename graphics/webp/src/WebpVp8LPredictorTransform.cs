using System;

namespace webp;

internal class WebpVp8LPredictorTransform
{
    internal int SizeBits;
    internal int Width;
    internal int Height;
    internal byte[] Modes;

    internal WebpVp8LPredictorTransform()
    {
        SizeBits = 0;
        Width = 0;
        Height = 0;
        Modes = Array.Empty<byte>();
    }
}
