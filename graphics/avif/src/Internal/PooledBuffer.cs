using System;
using System.Buffers;

namespace avif;

internal class PooledByteBuffer
{
    internal byte[] Data;
    internal int Length;
    internal long MaxBytes;
    internal bool ClearOnReturn;
    internal bool Pooled;
    internal bool Closed;
    internal AvifResultCode Code;
    internal object CloseGate;

    internal PooledByteBuffer(int length, long maxBytes, bool clearOnReturn)
    {
        Data = System.Array.Empty<byte>();
        Length = 0;
        MaxBytes = maxBytes;
        ClearOnReturn = clearOnReturn;
        Pooled = false;
        Closed = false;
        Code = AvifResultCode.Success;
        CloseGate = new object();

        CheckedIntegerResult size = CheckedMath.ComputeAllocationSize(length, 1, maxBytes);
        if (!size.Success)
        {
            Code = size.Code;
            return;
        }

        Length = length;
        if (length == 0)
        {
            return;
        }

        Data = ArrayPool<byte>.Shared.Rent(length);
        Pooled = true;
        Data.AsSpan(0, length).Clear();
    }

    internal static PooledByteBuffer FromCode(AvifResultCode code)
    {
        PooledByteBuffer result = new PooledByteBuffer(0, 0, true);
        result.Closed = code == AvifResultCode.Closed;
        result.Code = code;
        return result;
    }

    internal Span<byte> GetSpan()
    {
        if (Closed || Code != AvifResultCode.Success)
        {
            return Span<byte>.Empty;
        }

        return Data.AsSpan(0, Length);
    }

    internal AvifResultCode CopyFrom(ReadOnlySpan<byte> source, int destinationOffset)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (destinationOffset < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (destinationOffset > Length || source.Length > Length - destinationOffset)
        {
            return AvifResultCode.BufferTooSmall;
        }

        source.CopyTo(Data.AsSpan(destinationOffset, source.Length));
        return AvifResultCode.Success;
    }

    internal AvifResultCode CopyTo(int sourceOffset, Span<byte> destination)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (sourceOffset < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (sourceOffset > Length || destination.Length > Length - sourceOffset)
        {
            return AvifResultCode.BufferTooSmall;
        }

        Data.AsSpan(sourceOffset, destination.Length).CopyTo(destination);
        return AvifResultCode.Success;
    }

    internal PooledByteBuffer CreateCopy()
    {
        if (Closed)
        {
            return FromCode(AvifResultCode.Closed);
        }

        if (Code != AvifResultCode.Success)
        {
            return FromCode(Code);
        }

        PooledByteBuffer copy = new PooledByteBuffer(Length, MaxBytes, ClearOnReturn);
        if (copy.Code == AvifResultCode.Success)
        {
            Data.AsSpan(0, Length).CopyTo(copy.Data.AsSpan(0, Length));
        }

        return copy;
    }

    internal ByteBufferView CreateView(int offset, int length)
    {
        return new ByteBufferView(this, offset, length);
    }

    internal AvifResultCode Close()
    {
        lock (CloseGate)
        {
            if (Closed)
            {
                return AvifResultCode.Success;
            }

            byte[] returned = Data;
            bool returnToPool = Pooled;
            Data = System.Array.Empty<byte>();
            Length = 0;
            Pooled = false;
            Closed = true;

            if (returnToPool)
            {
                ArrayPool<byte>.Shared.Return(returned, ClearOnReturn);
            }
        }

        return AvifResultCode.Success;
    }
}

internal class ByteBufferView
{
    internal PooledByteBuffer? Owner;
    internal int Offset;
    internal int Length;
    internal bool Closed;
    internal AvifResultCode Code;

    internal ByteBufferView(PooledByteBuffer owner, int offset, int length)
    {
        Owner = owner;
        Offset = 0;
        Length = 0;
        Closed = false;
        Code = AvifResultCode.Success;

        if (owner == null || offset < 0 || length < 0)
        {
            Code = AvifResultCode.InvalidArgument;
            return;
        }

        if (owner.Closed)
        {
            Code = AvifResultCode.Closed;
            return;
        }

        if (owner.Code != AvifResultCode.Success)
        {
            Code = owner.Code;
            return;
        }

        if (offset > owner.Length || length > owner.Length - offset)
        {
            Code = AvifResultCode.InvalidArgument;
            return;
        }

        Offset = offset;
        Length = length;
    }

    internal Span<byte> GetSpan()
    {
        if (Closed || Code != AvifResultCode.Success || Owner == null || Owner.Closed)
        {
            return Span<byte>.Empty;
        }

        return Owner.Data.AsSpan(Offset, Length);
    }

    internal AvifResultCode CopyFrom(ReadOnlySpan<byte> source, int destinationOffset)
    {
        AvifResultCode state = GetStateCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        if (destinationOffset < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (destinationOffset > Length || source.Length > Length - destinationOffset)
        {
            return AvifResultCode.BufferTooSmall;
        }

        source.CopyTo((Owner ?? throw new InvalidOperationException("Invalid internal state.")).Data.AsSpan(Offset + destinationOffset, source.Length));
        return AvifResultCode.Success;
    }

    internal AvifResultCode CopyTo(int sourceOffset, Span<byte> destination)
    {
        AvifResultCode state = GetStateCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        if (sourceOffset < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (sourceOffset > Length || destination.Length > Length - sourceOffset)
        {
            return AvifResultCode.BufferTooSmall;
        }

        (Owner ?? throw new InvalidOperationException("Invalid internal state.")).Data.AsSpan(Offset + sourceOffset, destination.Length).CopyTo(destination);
        return AvifResultCode.Success;
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }

        Owner = null;
        Offset = 0;
        Length = 0;
        Closed = true;
        return AvifResultCode.Success;
    }

    internal AvifResultCode GetStateCode()
    {
        if (Closed || Owner == null || Owner.Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        return Owner.Code;
    }
}

internal class PooledSampleBuffer
{
    internal ushort[] Data;
    internal int Length;
    internal long MaxBytes;
    internal bool ClearOnReturn;
    internal bool Pooled;
    internal bool Closed;
    internal AvifResultCode Code;
    internal object CloseGate;

    internal PooledSampleBuffer(int length, long maxBytes, bool clearOnReturn)
    {
        Data = System.Array.Empty<ushort>();
        Length = 0;
        MaxBytes = maxBytes;
        ClearOnReturn = clearOnReturn;
        Pooled = false;
        Closed = false;
        Code = AvifResultCode.Success;
        CloseGate = new object();

        CheckedIntegerResult size = CheckedMath.ComputeAllocationSize(length, 2, maxBytes);
        if (!size.Success)
        {
            Code = size.Code;
            return;
        }

        Length = length;
        if (length == 0)
        {
            return;
        }

        Data = ArrayPool<ushort>.Shared.Rent(length);
        Pooled = true;
        Data.AsSpan(0, length).Clear();
    }

    internal static PooledSampleBuffer FromCode(AvifResultCode code)
    {
        PooledSampleBuffer result = new PooledSampleBuffer(0, 0, true);
        result.Closed = code == AvifResultCode.Closed;
        result.Code = code;
        return result;
    }

    internal Span<ushort> GetSpan()
    {
        if (Closed || Code != AvifResultCode.Success)
        {
            return Span<ushort>.Empty;
        }

        return Data.AsSpan(0, Length);
    }

    internal AvifResultCode CopyFrom(ReadOnlySpan<ushort> source, int destinationOffset)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (destinationOffset < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (destinationOffset > Length || source.Length > Length - destinationOffset)
        {
            return AvifResultCode.BufferTooSmall;
        }

        source.CopyTo(Data.AsSpan(destinationOffset, source.Length));
        return AvifResultCode.Success;
    }

    internal AvifResultCode CopyTo(int sourceOffset, Span<ushort> destination)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (sourceOffset < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (sourceOffset > Length || destination.Length > Length - sourceOffset)
        {
            return AvifResultCode.BufferTooSmall;
        }

        Data.AsSpan(sourceOffset, destination.Length).CopyTo(destination);
        return AvifResultCode.Success;
    }

    internal PooledSampleBuffer CreateCopy()
    {
        if (Closed)
        {
            return FromCode(AvifResultCode.Closed);
        }

        if (Code != AvifResultCode.Success)
        {
            return FromCode(Code);
        }

        PooledSampleBuffer copy = new PooledSampleBuffer(Length, MaxBytes, ClearOnReturn);
        if (copy.Code == AvifResultCode.Success)
        {
            Data.AsSpan(0, Length).CopyTo(copy.Data.AsSpan(0, Length));
        }

        return copy;
    }

    internal SampleBufferView CreateView(int offset, int length)
    {
        return new SampleBufferView(this, offset, length);
    }

    internal AvifResultCode Close()
    {
        lock (CloseGate)
        {
            if (Closed)
            {
                return AvifResultCode.Success;
            }

            ushort[] returned = Data;
            bool returnToPool = Pooled;
            Data = System.Array.Empty<ushort>();
            Length = 0;
            Pooled = false;
            Closed = true;

            if (returnToPool)
            {
                ArrayPool<ushort>.Shared.Return(returned, ClearOnReturn);
            }
        }

        return AvifResultCode.Success;
    }
}

internal class SampleBufferView
{
    internal PooledSampleBuffer? Owner;
    internal int Offset;
    internal int Length;
    internal bool Closed;
    internal AvifResultCode Code;

    internal SampleBufferView(PooledSampleBuffer owner, int offset, int length)
    {
        Owner = owner;
        Offset = 0;
        Length = 0;
        Closed = false;
        Code = AvifResultCode.Success;

        if (owner == null || offset < 0 || length < 0)
        {
            Code = AvifResultCode.InvalidArgument;
            return;
        }

        if (owner.Closed)
        {
            Code = AvifResultCode.Closed;
            return;
        }

        if (owner.Code != AvifResultCode.Success)
        {
            Code = owner.Code;
            return;
        }

        if (offset > owner.Length || length > owner.Length - offset)
        {
            Code = AvifResultCode.InvalidArgument;
            return;
        }

        Offset = offset;
        Length = length;
    }

    internal Span<ushort> GetSpan()
    {
        if (Closed || Code != AvifResultCode.Success || Owner == null || Owner.Closed)
        {
            return Span<ushort>.Empty;
        }

        return Owner.Data.AsSpan(Offset, Length);
    }

    internal AvifResultCode CopyFrom(ReadOnlySpan<ushort> source, int destinationOffset)
    {
        AvifResultCode state = GetStateCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        if (destinationOffset < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (destinationOffset > Length || source.Length > Length - destinationOffset)
        {
            return AvifResultCode.BufferTooSmall;
        }

        source.CopyTo((Owner ?? throw new InvalidOperationException("Invalid internal state.")).Data.AsSpan(Offset + destinationOffset, source.Length));
        return AvifResultCode.Success;
    }

    internal AvifResultCode CopyTo(int sourceOffset, Span<ushort> destination)
    {
        AvifResultCode state = GetStateCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        if (sourceOffset < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (sourceOffset > Length || destination.Length > Length - sourceOffset)
        {
            return AvifResultCode.BufferTooSmall;
        }

        (Owner ?? throw new InvalidOperationException("Invalid internal state.")).Data.AsSpan(Offset + sourceOffset, destination.Length).CopyTo(destination);
        return AvifResultCode.Success;
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }

        Owner = null;
        Offset = 0;
        Length = 0;
        Closed = true;
        return AvifResultCode.Success;
    }

    internal AvifResultCode GetStateCode()
    {
        if (Closed || Owner == null || Owner.Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        return Owner.Code;
    }
}
