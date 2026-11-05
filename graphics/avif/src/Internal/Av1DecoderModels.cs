using System;
using System.Collections.Generic;

namespace avif;

internal class Av1DecoderFrame
{
    internal int Offset;
    internal int Size;

    internal Av1DecoderFrame(int offset, int size)
    {
        Offset = offset;
        Size = size;
    }
}
