using System;
using System.Collections.Generic;

namespace avif;

internal class AvifBoxReaderFrame
{
    internal List<AvifBox>? Boxes;
    internal int Index;

    internal AvifBoxReaderFrame(List<AvifBox>? boxes, int index)
    {
        Boxes = boxes;
        Index = index;
    }
}
