using System;
using System.Collections.Generic;

namespace avif;

internal class IncrementalRange
{
    internal int Offset;
    internal int End;

    internal IncrementalRange(int offset, int end)
    {
        Offset = offset;
        End = end;
    }
}

internal class IncrementalInput : IInputSource
{
    internal byte[] Buffer;
    internal int Length;
    internal int ContiguousLength;
    internal int ReceivedByteCount;
    internal long MaxBytes;
    internal int MaxRangeCount;
    internal List<IncrementalRange> Ranges;
    internal bool IsComplete;
    internal bool Closed;
    internal AvifResultCode Code;

    internal IncrementalInput(
        int initialCapacity,
        long maxBytes,
        int maxRangeCount)
    {
        Length = 0;
        ContiguousLength = 0;
        ReceivedByteCount = 0;
        MaxBytes = maxBytes;
        MaxRangeCount = maxRangeCount;
        Ranges = new List<IncrementalRange>();
        IsComplete = false;
        Closed = false;
        if (initialCapacity < 0
            || maxBytes < 0
            || maxRangeCount <= 0
            || initialCapacity > maxBytes
            || initialCapacity > System.Array.MaxLength)
        {
            Buffer = System.Array.Empty<byte>();
            Code = AvifResultCode.InvalidArgument;
        }
        else
        {
            Buffer = new byte[initialCapacity];
            Code = AvifResultCode.Success;
        }
    }

    internal AvifResultCode Append(ReadOnlySpan<byte> source)
    {
        return AddRange(ContiguousLength, source);
    }

    internal AvifResultCode AddRange(
        long offset,
        ReadOnlySpan<byte> source)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (IsComplete)
        {
            return AvifResultCode.InvalidState;
        }

        if (offset < 0 || offset > int.MaxValue)
        {
            return AvifResultCode.InvalidArgument;
        }
        CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(
            offset,
            source.Length);
        if (!requestedEnd.Success
            || requestedEnd.Value > MaxBytes
            || requestedEnd.Value > System.Array.MaxLength)
        {
            Code = AvifResultCode.LimitExceeded;
            return Code;
        }
        if (source.Length == 0)
        {
            return AvifResultCode.Success;
        }
        int start = (int)offset;
        int end = (int)requestedEnd.Value;
        if (!OverlapsMatch(start, end, source))
        {
            Code = AvifResultCode.InvalidData;
            return Code;
        }
        if (!CanInsertRange(start, end))
        {
            Code = AvifResultCode.LimitExceeded;
            return Code;
        }
        if (!EnsureCapacity(end))
        {
            return Code;
        }

        source.CopyTo(Buffer.AsSpan(start, source.Length));
        MergeRange(start, end);
        if (end > Length)
        {
            Length = end;
        }
        UpdateCounts();
        return Code;
    }

    /// <summary>Reads up to the currently available byte count.</summary>
    /// <param name="offset">The absolute source offset.</param>
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

        if (offset >= Length)
        {
            AvifResultCode status = IsComplete
                ? AvifResultCode.EndOfData
                : AvifResultCode.NeedMoreData;
            return new InputReadResult(status, 0, offset, destination.Length);
        }

        IncrementalRange? range = FindRange((int)offset);
        if (range == null)
        {
            long missingLength = FindMissingLength(
                (int)offset,
                destination.Length);
            return new InputReadResult(
                IsComplete
                    ? AvifResultCode.TruncatedData
                    : AvifResultCode.NeedMoreData,
                0,
                offset,
                missingLength);
        }
        int available = range.End - (int)offset;
        int count = destination.Length < available ? destination.Length : available;
        Buffer.AsSpan((int)offset, count).CopyTo(destination);
        return new InputReadResult(AvifResultCode.Success, count, 0, 0);
    }

    /// <summary>Reads an entire available range or reports the next missing suffix.</summary>
    /// <param name="offset">The absolute source offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and any missing range.</returns>
    public InputReadResult ReadExact(long offset, Span<byte> destination)
    {
        if (Closed)
        {
            return new InputReadResult(AvifResultCode.Closed, 0, 0, 0);
        }

        if (Code != AvifResultCode.Success)
        {
            return new InputReadResult(Code, 0, 0, 0);
        }

        if (offset < 0)
        {
            return new InputReadResult(AvifResultCode.InvalidArgument, 0, 0, 0);
        }

        CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(offset, destination.Length);
        if (!requestedEnd.Success)
        {
            return new InputReadResult(requestedEnd.Code, 0, 0, 0);
        }

        if (requestedEnd.Value > Length)
        {
            AvifResultCode status = IsComplete ? AvifResultCode.TruncatedData : AvifResultCode.NeedMoreData;
            return new InputReadResult(
                status,
                0,
                Length,
                requestedEnd.Value - Length);
        }
        IncrementalRange? range = FindRange((int)offset);
        if (range == null || requestedEnd.Value > range.End)
        {
            int missingOffset = range == null
                ? (int)offset : range.End;
            long missingLength = FindMissingLength(
                missingOffset,
                (int)(requestedEnd.Value - missingOffset));
            return new InputReadResult(
                IsComplete
                    ? AvifResultCode.TruncatedData
                    : AvifResultCode.NeedMoreData,
                0,
                missingOffset,
                missingLength);
        }
        Buffer.AsSpan((int)offset, destination.Length).CopyTo(
            destination);
        return new InputReadResult(AvifResultCode.Success, destination.Length, 0, 0);
    }

    internal AvifResultCode CompleteInput()
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        IsComplete = true;
        return Code;
    }

    /// <summary>Releases the retained incremental buffer.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!Closed)
        {
            Closed = true;
            Buffer = System.Array.Empty<byte>();
            Length = 0;
            ContiguousLength = 0;
            ReceivedByteCount = 0;
            Ranges.Clear();
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

            AvifResultCode status = IsComplete ? AvifResultCode.EndOfData : AvifResultCode.NeedMoreData;
            return new InputReadResult(status, 0, Length, requestedEnd.Value - Length);
        }

        return null;
    }

    internal bool EnsureCapacity(int required)
    {
        if (required < 0)
        {
            Code = AvifResultCode.LimitExceeded;
            return false;
        }
        if (required > MaxBytes || required > System.Array.MaxLength)
        {
            Code = AvifResultCode.LimitExceeded;
            return false;
        }

        if (required <= Buffer.Length)
        {
            return true;
        }

        long doubled = Buffer.Length == 0 ? 256 : (long)Buffer.Length * 2;
        long capacity = doubled < required ? required : doubled;
        if (capacity > MaxBytes)
        {
            capacity = MaxBytes;
        }

        if (capacity > System.Array.MaxLength)
        {
            capacity = System.Array.MaxLength;
        }

        if (capacity < required)
        {
            Code = AvifResultCode.LimitExceeded;
            return false;
        }

        byte[] expanded = new byte[(int)capacity];
        Buffer.AsSpan(0, Length).CopyTo(expanded);
        Buffer = expanded;
        return true;
    }

    internal bool OverlapsMatch(
        int start,
        int end,
        ReadOnlySpan<byte> source)
    {
        int index = 0;
        while (index < Ranges.Count)
        {
            IncrementalRange range = Ranges[index];
            if (range.End <= start)
            {
                index++;
                continue;
            }
            if (range.Offset >= end)
            {
                break;
            }
            int overlapStart = range.Offset > start
                ? range.Offset : start;
            int overlapEnd = range.End < end ? range.End : end;
            int position = overlapStart;
            while (position < overlapEnd)
            {
                if (Buffer[position] != source[position - start])
                {
                    return false;
                }
                position++;
            }
            index++;
        }
        return true;
    }

    internal bool CanInsertRange(int start, int end)
    {
        if (Ranges.Count < MaxRangeCount)
        {
            return true;
        }
        int index = 0;
        while (index < Ranges.Count)
        {
            IncrementalRange range = Ranges[index];
            if (range.End >= start && range.Offset <= end)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal void MergeRange(int start, int end)
    {
        int index = 0;
        while (index < Ranges.Count && Ranges[index].End < start)
        {
            index++;
        }
        while (index < Ranges.Count && Ranges[index].Offset <= end)
        {
            IncrementalRange current = Ranges[index];
            if (current.Offset < start)
            {
                start = current.Offset;
            }
            if (current.End > end)
            {
                end = current.End;
            }
            Ranges.RemoveAt(index);
        }
        Ranges.Insert(index, new IncrementalRange(start, end));
    }

    internal void UpdateCounts()
    {
        ReceivedByteCount = 0;
        int index = 0;
        while (index < Ranges.Count)
        {
            ReceivedByteCount += Ranges[index].End
                - Ranges[index].Offset;
            index++;
        }
        ContiguousLength = Ranges.Count > 0
                && Ranges[0].Offset == 0
            ? Ranges[0].End : 0;
    }

    internal IncrementalRange? FindRange(int offset)
    {
        int index = 0;
        while (index < Ranges.Count)
        {
            IncrementalRange range = Ranges[index];
            if (offset >= range.Offset && offset < range.End)
            {
                return range;
            }
            if (range.Offset > offset)
            {
                break;
            }
            index++;
        }
        return null;
    }

    internal long FindMissingLength(
        int missingOffset,
        int maximumLength)
    {
        int end = missingOffset + maximumLength;
        int index = 0;
        while (index < Ranges.Count)
        {
            IncrementalRange range = Ranges[index];
            if (range.Offset > missingOffset)
            {
                int missingEnd = range.Offset < end
                    ? range.Offset : end;
                return missingEnd - missingOffset;
            }
            index++;
        }
        return end - missingOffset;
    }

    internal bool IsRangeAvailable(long offset, long length)
    {
        if (Closed || Code != AvifResultCode.Success
            || offset < 0 || length < 0
            || offset > int.MaxValue || length > int.MaxValue)
        {
            return false;
        }
        CheckedIntegerResult end = CheckedMath.AddInt64(offset, length);
        if (!end.Success || end.Value > Length)
        {
            return false;
        }
        if (length == 0)
        {
            return true;
        }
        IncrementalRange? range = FindRange((int)offset);
        return range != null && end.Value <= range.End;
    }
}
