using System;

namespace webp;

internal class WebpVp8LEntropyImage
{
    internal int PrefixBits;
    internal int Width;
    internal int Height;
    internal int[] MetaPrefixCodes;
    internal int NumPrefixGroups;

    internal WebpVp8LEntropyImage()
    {
        PrefixBits = 0;
        Width = 0;
        Height = 0;
        MetaPrefixCodes = Array.Empty<int>();
        NumPrefixGroups = 1;
    }
}
