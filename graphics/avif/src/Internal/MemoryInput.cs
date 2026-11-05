using System;

namespace avif;

internal class MemoryInput : IInputSource
{
    internal byte[] Data;
    internal int Start;
    internal int Length;
    internal bool Closed;
    internal AvifResultCode Code;

    internal static MemoryInput Create(byte[] data)
    {
        return new MemoryInput(data, 0, data == null ? 0 : data.Length);
    }

    internal MemoryInput(byte[] data, int start, int length)
    {
        Data = data;
        Start = 0;
        Length = 0;
        Closed = false;
        if (data == null || start < 0 || length < 0 || start > data.Length - length)
        {
            Code = AvifResultCode.InvalidArgument;
        }
        else
        {
            Start = start;
            Length = length;
            Code = AvifResultCode.Success;
        }
    }

    internal static MemoryInput Create(
        byte[] data,
        int start,
        int length)
    {
        return new MemoryInput(data, start, length);
    }

    internal long GetLength()
    {
        return Length;
    }

    /// <summary>Reads up to the requested byte count from the memory view.</summary>
    /// <param name="offset">The absolute view offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and transferred byte count.</returns>
    public InputReadResult Read(long offset, Span<byte> destination)
    {
        InputReadResult? unavailable = CheckAvailable(offset, destination.Length);
        if (unavailable != null)
        {
            return unavailable;
        }

        if (destination.Length == 0)
        {
            return new InputReadResult(AvifResultCode.Success, 0, 0, 0);
        }

        if (offset == Length)
        {
            return new InputReadResult(AvifResultCode.EndOfData, 0, offset, destination.Length);
        }

        int available = Length - (int)offset;
        int count = destination.Length < available ? destination.Length : available;
        Data.AsSpan(Start + (int)offset, count).CopyTo(destination);
        return new InputReadResult(AvifResultCode.Success, count, 0, 0);
    }

    /// <summary>Reads an entire range from the memory view atomically.</summary>
    /// <param name="offset">The absolute view offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and any missing range.</returns>
    public InputReadResult ReadExact(long offset, Span<byte> destination)
    {
        InputReadResult? unavailable = CheckAvailable(offset, destination.Length);
        if (unavailable != null)
        {
            if (unavailable.Code == AvifResultCode.EndOfData)
            {
                CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(offset, destination.Length);
                if (!requestedEnd.Success)
                {
                    return new InputReadResult(requestedEnd.Code, 0, 0, 0);
                }

                return new InputReadResult(
                    AvifResultCode.TruncatedData,
                    0,
                    Length,
                    requestedEnd.Value - Length);
            }

            return unavailable;
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(offset, 0, destination.Length, Length);
        if (!range.Success)
        {
            long missingOffset = offset < Length ? Length : offset;
            CheckedIntegerResult requestedEndResult = CheckedMath.AddInt64(offset, destination.Length);
            if (!requestedEndResult.Success)
            {
                return new InputReadResult(requestedEndResult.Code, 0, 0, 0);
            }

            long requestedEnd = requestedEndResult.Value;
            long missingLength = requestedEnd > Length ? requestedEnd - Length : 0;
            return new InputReadResult(range.Code, 0, missingOffset, missingLength);
        }

        Data.AsSpan(Start + (int)offset, destination.Length).CopyTo(destination);
        return new InputReadResult(AvifResultCode.Success, destination.Length, 0, 0);
    }

    /// <summary>Releases the retained memory view.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!Closed)
        {
            Closed = true;
            Data = System.Array.Empty<byte>();
            Start = 0;
            Length = 0;
        }

        return AvifResultCode.Success;
    }

    internal InputReadResult? CheckAvailable(long offset, int requestedLength)
    {
        if (Closed)
        {
            return new InputReadResult(AvifResultCode.Closed, 0, 0, 0);
        }

        if (Code != AvifResultCode.Success)
        {
            return new InputReadResult(Code, 0, 0, 0);
        }

        if (offset < 0 || requestedLength < 0)
        {
            return new InputReadResult(AvifResultCode.InvalidArgument, 0, 0, 0);
        }

        if (offset > Length)
        {
            CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(offset, requestedLength);
            if (!requestedEnd.Success)
            {
                return new InputReadResult(requestedEnd.Code, 0, 0, 0);
            }

            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                Length,
                requestedEnd.Value - Length);
        }

        return null;
    }
}
