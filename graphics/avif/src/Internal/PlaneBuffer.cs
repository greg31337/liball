using System;

namespace avif;

internal class PlaneBuffer
{
    internal PooledByteBuffer? Buffer;
    internal int Width;
    internal int Height;
    internal int BytesPerSample;
    internal int Stride;
    internal int Alignment;
    internal int Offset;
    internal long MaxBytes;
    internal bool ClearOnReturn;
    internal bool OwnsBuffer;
    internal bool Closed;
    internal AvifResultCode Code;

    internal PlaneBuffer(
        int width,
        int height,
        int bytesPerSample,
        int alignment,
        long maxBytes,
        bool clearOnReturn)
    {
        Buffer = null;
        Width = 0;
        Height = 0;
        BytesPerSample = 0;
        Stride = 0;
        Alignment = alignment;
        Offset = 0;
        MaxBytes = maxBytes;
        ClearOnReturn = clearOnReturn;
        OwnsBuffer = false;
        Closed = false;
        Code = AvifResultCode.Success;

        CheckedIntegerResult stride = CheckedMath.ComputeStride(width, 1, bytesPerSample, alignment, maxBytes);
        if (!stride.Success)
        {
            Code = stride.Code;
            return;
        }

        CheckedIntegerResult size = CheckedMath.ComputePlaneSize((int)stride.Value, height, maxBytes);
        if (!size.Success)
        {
            Code = size.Code;
            return;
        }

        if (size.Value > System.Array.MaxLength)
        {
            Code = AvifResultCode.LimitExceeded;
            return;
        }

        PooledByteBuffer buffer = new PooledByteBuffer((int)size.Value, maxBytes, clearOnReturn);
        if (buffer.Code != AvifResultCode.Success)
        {
            Code = buffer.Code;
            return;
        }

        Buffer = buffer;
        Width = width;
        Height = height;
        BytesPerSample = bytesPerSample;
        Stride = (int)stride.Value;
        OwnsBuffer = true;
    }

    internal static PlaneBuffer CreateView(
        PooledByteBuffer buffer,
        int width,
        int height,
        int bytesPerSample,
        int stride,
        int alignment,
        int offset,
        long maxBytes,
        bool clearOnReturn)
    {
        PlaneBuffer result = new PlaneBuffer(
            0, 0, 1, 1, maxBytes, clearOnReturn);
        result.Buffer = buffer;
        result.Width = width;
        result.Height = height;
        result.BytesPerSample = bytesPerSample;
        result.Stride = stride;
        result.Alignment = alignment;
        result.Offset = offset;
        result.MaxBytes = maxBytes;
        result.ClearOnReturn = clearOnReturn;
        result.OwnsBuffer = false;
        result.Closed = false;
        result.Code = AvifResultCode.Success;
        return result;
    }

    internal static PlaneBuffer FromCode(AvifResultCode code)
    {
        PlaneBuffer result = new PlaneBuffer(0, 0, 1, 1, 0, true);
        result.Closed = code == AvifResultCode.Closed;
        result.Code = code;
        return result;
    }

    internal Span<byte> GetRowSpan(int row)
    {
        AvifResultCode state = GetStateCode();
        if (state != AvifResultCode.Success || row < 0 || row >= Height)
        {
            return Span<byte>.Empty;
        }

        int rowBytes = Width * BytesPerSample;
        int rowOffset = Offset + row * Stride;
        return (Buffer ?? throw new InvalidOperationException("Invalid internal state.")).Data.AsSpan(rowOffset, rowBytes);
    }

    internal PlaneBuffer CreateView(int x, int y, int width, int height)
    {
        AvifResultCode state = GetStateCode();
        if (state != AvifResultCode.Success)
        {
            return FromCode(state);
        }

        if (x < 0 || y < 0 || width <= 0 || height <= 0)
        {
            return FromCode(AvifResultCode.InvalidArgument);
        }

        if (x > Width || y > Height || width > Width - x || height > Height - y)
        {
            return FromCode(AvifResultCode.InvalidArgument);
        }

        long viewOffset = (long)Offset + (long)y * Stride + (long)x * BytesPerSample;
        long rowBytes = (long)width * BytesPerSample;
        long end = viewOffset + (long)(height - 1) * Stride + rowBytes;
        if (viewOffset < 0 || viewOffset > int.MaxValue || end < viewOffset || end > (Buffer ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            return FromCode(AvifResultCode.LimitExceeded);
        }

        return CreateView(
            Buffer,
            width,
            height,
            BytesPerSample,
            Stride,
            Alignment,
            (int)viewOffset,
            MaxBytes,
            ClearOnReturn);
    }

    internal PlaneBuffer CreateCopy()
    {
        AvifResultCode state = GetStateCode();
        if (state != AvifResultCode.Success)
        {
            return FromCode(state);
        }

        PlaneBuffer copy = new PlaneBuffer(
            Width,
            Height,
            BytesPerSample,
            Alignment,
            MaxBytes,
            ClearOnReturn);
        if (copy.Code != AvifResultCode.Success)
        {
            return copy;
        }

        AvifResultCode result = copy.CopyFrom(this);
        if (result != AvifResultCode.Success)
        {
            copy.Close();
            return FromCode(result);
        }

        return copy;
    }

    internal AvifResultCode CopyFrom(PlaneBuffer source)
    {
        AvifResultCode destinationState = GetStateCode();
        if (destinationState != AvifResultCode.Success)
        {
            return destinationState;
        }

        if (source == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        AvifResultCode sourceState = source.GetStateCode();
        if (sourceState != AvifResultCode.Success)
        {
            return sourceState;
        }

        if (Width != source.Width || Height != source.Height || BytesPerSample != source.BytesPerSample)
        {
            return AvifResultCode.InvalidArgument;
        }

        int rowBytes = Width * BytesPerSample;
        if (Buffer == source.Buffer)
        {
            CheckedIntegerResult scratchSize = CheckedMath.ComputeAllocationSize(
                (long)rowBytes * Height,
                1,
                MaxBytes);
            if (!scratchSize.Success)
            {
                return scratchSize.Code;
            }

            PooledByteBuffer scratch = new PooledByteBuffer((int)scratchSize.Value, MaxBytes, true);
            if (scratch.Code != AvifResultCode.Success)
            {
                return scratch.Code;
            }

            int row = 0;
            while (row < Height)
            {
                source.GetRowSpan(row).CopyTo(scratch.Data.AsSpan(row * rowBytes, rowBytes));
                row++;
            }

            row = 0;
            while (row < Height)
            {
                scratch.Data.AsSpan(row * rowBytes, rowBytes).CopyTo(GetRowSpan(row));
                row++;
            }

            scratch.Close();
            return AvifResultCode.Success;
        }

        int copyRow = 0;
        while (copyRow < Height)
        {
            source.GetRowSpan(copyRow).CopyTo(GetRowSpan(copyRow));
            copyRow++;
        }

        return AvifResultCode.Success;
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }

        PooledByteBuffer? buffer = Buffer;
        bool closeBuffer = OwnsBuffer;
        Buffer = null;
        Width = 0;
        Height = 0;
        BytesPerSample = 0;
        Stride = 0;
        Offset = 0;
        OwnsBuffer = false;
        Closed = true;

        if (closeBuffer && buffer != null)
        {
            return buffer.Close();
        }

        return AvifResultCode.Success;
    }

    internal AvifResultCode GetStateCode()
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (Buffer == null || Buffer.Closed)
        {
            return AvifResultCode.Closed;
        }

        return Buffer.Code;
    }
}
