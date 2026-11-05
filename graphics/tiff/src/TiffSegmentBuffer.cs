using System;
using System.Buffers;

namespace tiff;

internal class TiffSegmentBuffer : IDisposable
{
    internal byte[] Bytes;
    internal int Length;
    internal bool IsPooled;
    internal ArrayPool<byte>? Pool;

    internal TiffSegmentBuffer(int length, TiffLimits limits)
    {
        Length = length;
        IsPooled = limits != null && limits.UsePooledSegmentBuffers;
        Pool = null;
        if (IsPooled)
        {
            ArrayPool<byte>? configuredPool = null;
            if (limits != null)
            {
                configuredPool = limits.SegmentBufferPool;
            }

            if (configuredPool == null)
            {
                configuredPool = ArrayPool<byte>.Shared;
            }

            Pool = configuredPool;
            Bytes = configuredPool.Rent(length);
        }
        else
        {
            Bytes = new byte[length];
        }
    }

    internal Span<byte> GetSpan()
    {
        return Bytes.AsSpan(0, Length);
    }

    internal ReadOnlySpan<byte> GetReadOnlySpan()
    {
        return Bytes.AsSpan(0, Length);
    }

    /// <summary>Returns a rented buffer to its pool.</summary>
    public void Dispose()
    {
        ArrayPool<byte>? pool = Pool;
        if (IsPooled && pool != null)
        {
            pool.Return(Bytes, false);
        }

        Bytes = new byte[0];
        Length = 0;
        IsPooled = false;
        Pool = null;
    }
}
