namespace qrcode;

internal class ArrayPoolLease : global::System.IDisposable
{
    byte[]? Buffer;
    int Length;

    internal ArrayPoolLease(int minimumLength, int maximumLength)
    {
        if (minimumLength < 0)
        {
            throw new QrInputException("The minimum buffer length cannot be negative.", "minimumLength");
        }

        if (maximumLength < 0)
        {
            throw new QrInputException("The maximum buffer length cannot be negative.", "maximumLength");
        }

        CheckedDimensions.CalculateAllocationBytes(minimumLength, 1, maximumLength);
        Buffer = global::System.Buffers.ArrayPool<byte>.Shared.Rent(minimumLength);
        Length = minimumLength;
        global::System.Span<byte> logicalBuffer = new global::System.Span<byte>(Buffer, 0, Length);
        logicalBuffer.Clear();
    }

    internal global::System.Span<byte> GetSpan()
    {
        if (Buffer == null)
        {
            throw new global::System.ObjectDisposedException("ArrayPoolLease");
        }

        return new global::System.Span<byte>(Buffer, 0, Length);
    }

    internal global::System.ReadOnlySpan<byte> GetReadOnlySpan()
    {
        if (Buffer == null)
        {
            throw new global::System.ObjectDisposedException("ArrayPoolLease");
        }

        return new global::System.ReadOnlySpan<byte>(Buffer, 0, Length);
    }

    internal int GetLength()
    {
        if (Buffer == null)
        {
            throw new global::System.ObjectDisposedException("ArrayPoolLease");
        }

        return Length;
    }

    internal bool IsActive()
    {
        return Buffer != null;
    }

    internal void SetBytes(global::System.ReadOnlySpan<byte> source)
    {
        global::System.Span<byte> destination = GetSpan();
        if (source.Length > destination.Length)
        {
            throw new QrInputException("The source is longer than the leased buffer.", "source");
        }

        destination.Clear();
        source.CopyTo(destination);
    }

    internal void CopyBytes(global::System.Span<byte> destination)
    {
        global::System.ReadOnlySpan<byte> source = GetReadOnlySpan();
        if (destination.Length < source.Length)
        {
            throw new QrInputException("The destination is shorter than the leased buffer.", "destination");
        }

        source.CopyTo(destination);
    }

    internal byte[] CreateOwnedCopy()
    {
        global::System.ReadOnlySpan<byte> source = GetReadOnlySpan();
        byte[] result = new byte[source.Length];
        source.CopyTo(result);
        return result;
    }

    internal void ClearBuffer()
    {
        global::System.Span<byte> buffer = GetSpan();
        buffer.Clear();
    }

    public void Dispose()
    {
        byte[]? buffer = Buffer;
        if (buffer == null)
        {
            return;
        }

        Buffer = null;
        Length = 0;
        global::System.Buffers.ArrayPool<byte>.Shared.Return(buffer, true);
    }
}
