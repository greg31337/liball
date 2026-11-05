using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace avif;

internal class AvifWriterImageInfo
{
    internal uint ItemId;
    internal int Width;
    internal int Height;
    internal int BitDepth;

    internal AvifWriterImageInfo(
        uint itemId,
        int width,
        int height,
        int bitDepth)
    {
        ItemId = itemId;
        Width = width;
        Height = height;
        BitDepth = bitDepth;
    }
}

internal class AvifWriterLayeredPayload
{
    internal AvifResultCode Code;
    internal byte[] Data;
    internal uint[] LayerSizes;

    internal AvifWriterLayeredPayload(AvifResultCode code)
    {
        Code = code;
        Data = System.Array.Empty<byte>();
        LayerSizes = System.Array.Empty<uint>();
    }
}
