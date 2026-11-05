using System;

namespace webp;

internal class WebpVp8LColorIndexingEncodePlan
{
    internal bool UseColorIndexing;
    internal int ColorTableSize;
    internal int WidthBits;
    internal int PackedWidth;
    internal int Height;
    internal WebpVp8LColor[] ColorTable;
    internal WebpVp8LColor[] PackedPixels;

    internal WebpVp8LColorIndexingEncodePlan()
    {
        UseColorIndexing = false;
        ColorTableSize = 0;
        WidthBits = 0;
        PackedWidth = 0;
        Height = 0;
        ColorTable = Array.Empty<WebpVp8LColor>();
        PackedPixels = Array.Empty<WebpVp8LColor>();
    }
}
