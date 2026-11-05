using System;

namespace webp;

internal class WebpVp8LColorIndexingTransform
{
    internal int ColorTableSize;
    internal int WidthBits;
    internal int PackedWidth;
    internal int Height;
    internal WebpVp8LColor[] ColorTable;

    internal WebpVp8LColorIndexingTransform()
    {
        ColorTableSize = 0;
        WidthBits = 0;
        PackedWidth = 0;
        Height = 0;
        ColorTable = Array.Empty<WebpVp8LColor>();
    }
}
