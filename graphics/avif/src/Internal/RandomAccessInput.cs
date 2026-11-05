using System;

namespace avif;

internal class RandomAccessInput : IInputSource
{
    internal IAvifRandomAccessSource? Source;
    internal long Length;
    internal AvifResultCode Code;
    internal bool Closed;

    internal RandomAccessInput(
        IAvifRandomAccessSource? source,
        long maximumSourceBytes)
    {
        Source = source;
        Length = 0;
        Closed = false;
        if (source == null || maximumSourceBytes <= 0)
        {
            Code = AvifResultCode.InvalidArgument;
            return;
        }
        Length = source.GetLength();
        if (Length < 0)
        {
            Code = AvifResultCode.InvalidArgument;
        }
        else if (Length > maximumSourceBytes
            || Length > System.Array.MaxLength)
        {
            Code = AvifResultCode.LimitExceeded;
        }
        else
        {
            Code = AvifResultCode.Success;
        }
    }

    public InputReadResult Read(
        long offset,
        Span<byte> destination)
    {
        InputReadResult? unavailable = CheckAvailable(offset);
        if (unavailable != null)
        {
            return unavailable;
        }
        if (offset > Length)
        {
            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                offset,
                destination.Length);
        }
        if (destination.Length == 0)
        {
            return new InputReadResult(
                AvifResultCode.Success,
                0,
                0,
                0);
        }
        if (offset == Length)
        {
            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                offset,
                destination.Length);
        }
        long available = Length - offset;
        int count = available < destination.Length
            ? (int)available : destination.Length;
        return ReadAtomic(offset, destination.Slice(0, count));
    }

    public InputReadResult ReadExact(
        long offset,
        Span<byte> destination)
    {
        InputReadResult? unavailable = CheckAvailable(offset);
        if (unavailable != null)
        {
            return unavailable;
        }
        CheckedRangeResult range = CheckedMath.ResolveRange(
            offset,
            0,
            destination.Length,
            Length);
        if (!range.Success)
        {
            CheckedIntegerResult end = CheckedMath.AddInt64(
                offset,
                destination.Length);
            if (!end.Success)
            {
                return new InputReadResult(end.Code, 0, 0, 0);
            }
            long missingOffset = offset > Length ? offset : Length;
            return new InputReadResult(
                AvifResultCode.TruncatedData,
                0,
                missingOffset,
                end.Value - missingOffset);
        }
        return ReadAtomic(offset, destination);
    }

    public AvifResultCode Close()
    {
        Closed = true;
        Source = null;
        return AvifResultCode.Success;
    }

    internal InputReadResult ReadAtomic(
        long offset,
        Span<byte> destination)
    {
        byte[] scratch = new byte[destination.Length];
        AvifRandomAccessReadResult? result =
            (Source ?? throw new InvalidOperationException("Invalid internal state.")).ReadRange(
                offset,
                scratch);
        AvifResultCode validation = ValidateResult(
            result,
            offset,
            destination.Length);
        if (validation != AvifResultCode.Success)
        {
            if (validation == AvifResultCode.NeedMoreData)
            {
                return new InputReadResult(
                    validation,
                    0,
                    (result ?? throw new InvalidOperationException("Invalid internal state.")).MissingOffset,
                    result.MissingLength);
            }
            return new InputReadResult(validation, 0, 0, 0);
        }
        scratch.AsSpan().CopyTo(destination);
        return new InputReadResult(
            AvifResultCode.Success,
            destination.Length,
            0,
            0);
    }

    internal AvifResultCode ValidateResult(
        AvifRandomAccessReadResult? result,
        long requestedOffset,
        int requestedLength)
    {
        if (result == null)
        {
            return AvifResultCode.InvalidData;
        }
        if (result.Code == AvifResultCode.Success)
        {
            return result.BytesRead == requestedLength
                    && result.MissingOffset == 0
                    && result.MissingLength == 0
                ? AvifResultCode.Success
                : AvifResultCode.InvalidData;
        }
        if (result.Code != AvifResultCode.NeedMoreData
            || result.BytesRead != 0
            || result.MissingLength <= 0)
        {
            return result.Code == AvifResultCode.NeedMoreData
                ? AvifResultCode.InvalidData : result.Code;
        }
        CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(
            requestedOffset,
            requestedLength);
        CheckedIntegerResult missingEnd = CheckedMath.AddInt64(
            result.MissingOffset,
            result.MissingLength);
        if (!requestedEnd.Success
            || !missingEnd.Success
            || result.MissingOffset < requestedOffset
            || result.MissingOffset >= requestedEnd.Value
            || missingEnd.Value > requestedEnd.Value)
        {
            return AvifResultCode.InvalidData;
        }
        return AvifResultCode.NeedMoreData;
    }

    internal InputReadResult? CheckAvailable(long offset)
    {
        if (Closed)
        {
            return new InputReadResult(
                AvifResultCode.Closed,
                0,
                0,
                0);
        }
        if (Code != AvifResultCode.Success)
        {
            return new InputReadResult(Code, 0, 0, 0);
        }
        if (offset < 0)
        {
            return new InputReadResult(
                AvifResultCode.InvalidArgument,
                0,
                0,
                0);
        }
        return null;
    }
}
