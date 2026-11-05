using System;

namespace webp;

internal class WebpVp8LEntropyCode
{
    internal int AlphabetSize;
    internal int[] CodeLengths;
    internal WebpVp8LPrefixCodeTable Table;

    internal WebpVp8LEntropyCode()
    {
        AlphabetSize = 0;
        CodeLengths = Array.Empty<int>();
        Table = new WebpVp8LPrefixCodeTable();
    }
}
