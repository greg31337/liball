using System;

namespace avif;

internal class ParsedDataView
{
    long sourceOffset;
    long length;

    internal ParsedDataView(long sourceOffset, long length)
    {
        this.sourceOffset = sourceOffset;
        this.length = length;
    }

    internal long GetSourceOffset()
    {
        return sourceOffset;
    }

    internal long GetLength()
    {
        return length;
    }

    internal InputReadResult Read(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        AvifResultCode argumentCode = ValidateArguments(offset, destination.Length);
        if (argumentCode != AvifResultCode.Success)
        {
            return new InputReadResult(argumentCode, 0, 0, 0);
        }

        if (offset > length)
        {
            CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(
                offset,
                destination.Length);
            if (!requestedEnd.Success)
            {
                return new InputReadResult(requestedEnd.Code, 0, 0, 0);
            }

            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                length,
                requestedEnd.Value - length);
        }

        if (destination.Length == 0)
        {
            return new InputReadResult(AvifResultCode.Success, 0, 0, 0);
        }

        if (offset == length)
        {
            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                offset,
                destination.Length);
        }

        long available = length - offset;
        int count = destination.Length;
        if (available < count)
        {
            count = (int)available;
        }

        CheckedIntegerResult absoluteOffset = CheckedMath.AddInt64(sourceOffset, offset);
        if (!absoluteOffset.Success)
        {
            return new InputReadResult(absoluteOffset.Code, 0, 0, 0);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            absoluteOffset.Value,
            0,
            count,
            source.Length);
        if (!range.Success)
        {
            return new InputReadResult(
                range.Code,
                0,
                source.Length,
                range.End > source.Length ? range.End - source.Length : 0);
        }

        source.Slice((int)absoluteOffset.Value, count).CopyTo(destination);
        return new InputReadResult(AvifResultCode.Success, count, 0, 0);
    }

    internal InputReadResult ReadExact(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        AvifResultCode argumentCode = ValidateArguments(offset, destination.Length);
        if (argumentCode != AvifResultCode.Success)
        {
            return new InputReadResult(argumentCode, 0, 0, 0);
        }

        if (offset > length)
        {
            CheckedIntegerResult unavailableEnd = CheckedMath.AddInt64(
                offset,
                destination.Length);
            if (!unavailableEnd.Success)
            {
                return new InputReadResult(unavailableEnd.Code, 0, 0, 0);
            }

            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                length,
                unavailableEnd.Value - length);
        }

        if (destination.Length == 0)
        {
            return new InputReadResult(AvifResultCode.Success, 0, 0, 0);
        }

        CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(offset, destination.Length);
        if (!requestedEnd.Success)
        {
            return new InputReadResult(requestedEnd.Code, 0, 0, 0);
        }

        if (requestedEnd.Value > length)
        {
            long missingOffset = offset < length ? length : offset;
            return new InputReadResult(
                AvifResultCode.TruncatedData,
                0,
                missingOffset,
                requestedEnd.Value - length);
        }

        CheckedIntegerResult absoluteOffset = CheckedMath.AddInt64(sourceOffset, offset);
        if (!absoluteOffset.Success)
        {
            return new InputReadResult(absoluteOffset.Code, 0, 0, 0);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            absoluteOffset.Value,
            0,
            destination.Length,
            source.Length);
        if (!range.Success)
        {
            return new InputReadResult(
                range.Code,
                0,
                source.Length,
                range.End > source.Length ? range.End - source.Length : 0);
        }

        source.Slice((int)absoluteOffset.Value, destination.Length).CopyTo(destination);
        return new InputReadResult(
            AvifResultCode.Success,
            destination.Length,
            0,
            0);
    }

    internal AvifResultCode ValidateArguments(long offset, int requestedLength)
    {
        if (sourceOffset < 0 || length < 0 || offset < 0 || requestedLength < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        return AvifResultCode.Success;
    }
}

internal class ParsedDataBox
{
    string boxType;
    ParsedDataView payload;

    internal ParsedDataBox(string boxType, long payloadOffset, long payloadLength)
    {
        this.boxType = boxType;
        payload = new ParsedDataView(payloadOffset, payloadLength);
    }

    internal string GetBoxType()
    {
        return boxType;
    }

    internal long GetPayloadOffset()
    {
        return payload.GetSourceOffset();
    }

    internal long GetPayloadLength()
    {
        return payload.GetLength();
    }

    internal InputReadResult Read(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        return payload.Read(source, offset, destination);
    }

    internal InputReadResult ReadExact(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        return payload.ReadExact(source, offset, destination);
    }
}
