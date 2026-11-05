using System;

namespace avif;

internal class AvifPackedPixelFormatInfo
{
    internal AvifPixelFormat Format;
    internal int BitDepth;
    internal bool FloatingPoint;
    internal int ChannelCount;
    internal int BytesPerComponent;
    internal int BytesPerPixel;
    internal bool HasAlpha;
}

internal class AvifPackedPixelStorage
{
    internal byte[] Data;
    internal PooledByteBuffer? Pooled;
    internal bool Closed;

    internal static AvifPackedPixelStorage Allocate(int length, long maxBytes)
    {
        PooledByteBuffer pooled = new PooledByteBuffer(length, maxBytes, true);
        AvifPackedPixelStorage result =
            new AvifPackedPixelStorage(pooled.Data);
        result.Pooled = pooled;
        result.Closed = pooled.Code != AvifResultCode.Success;
        return result;
    }

    internal AvifPackedPixelStorage(byte[] data)
    {
        Data = data;
        Pooled = null;
        Closed = false;
    }

    internal AvifResultCode GetCode()
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }
        return Pooled == null ? AvifResultCode.Success : Pooled.Code;
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }
        Closed = true;
        if (Pooled != null)
        {
            Pooled.Close();
        }
        Data = System.Array.Empty<byte>();
        Pooled = null;
        return AvifResultCode.Success;
    }
}
