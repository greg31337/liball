using System;

namespace http;

internal class HpackIntegerResult
{
    internal HttpOperationStatus Status;
    internal long Value;
    internal int Consumed;
    internal int Written;
    internal int RequiredSize;
    internal byte[] Bytes = Array.Empty<byte>();
    internal bool HasErrorPosition;
    internal int ErrorPosition;
    internal bool IsMalformed;
    internal bool IsOverflow;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class HpackIntegerCodec
{
    const int MaximumEncodedSize = 10;

    internal static HpackIntegerResult Decode(
        ReadOnlySpan<byte> input,
        int prefixWidth)
    {
        HpackIntegerResult result = new HpackIntegerResult();
        if (!IsValidPrefixWidth(prefixWidth))
        {
            return RejectInvalidPrefixWidth(result);
        }

        if (input.Length == 0)
        {
            result.Status = HttpOperationStatus.NeedInput;
            result.RequiredSize = 1;
            return result;
        }

        int prefixMaximum = (1 << prefixWidth) - 1;
        long value = input[0] & prefixMaximum;
        if (value < prefixMaximum)
        {
            result.Status = HttpOperationStatus.Done;
            result.Value = value;
            result.Consumed = 1;
            result.RequiredSize = 1;
            return result;
        }

        int position = 1;
        int shift = 0;
        while (position < input.Length)
        {
            byte current = input[position];
            int payload = current & 0x7f;
            if (!CanAddPayload(value, payload, shift))
            {
                return RejectIntegerOverflow(result, position, position + 1);
            }

            if (position >= MaximumEncodedSize)
            {
                return RejectIntegerLength(result, position, position + 1);
            }

            if (payload != 0 && shift < 63)
            {
                value = value + (long)payload * (1L << shift);
            }

            position = position + 1;
            if ((current & 0x80) == 0)
            {
                result.Status = HttpOperationStatus.Done;
                result.Value = value;
                result.Consumed = position;
                result.RequiredSize = position;
                return result;
            }

            if (shift <= int.MaxValue - 7)
            {
                shift = shift + 7;
            }
        }

        result.Status = HttpOperationStatus.NeedInput;
        result.Consumed = input.Length;
        result.RequiredSize = input.Length == int.MaxValue ?
            int.MaxValue : input.Length + 1;
        return result;
    }

    internal static HpackIntegerResult Encode(
        long value,
        int prefixWidth,
        byte prefixBits)
    {
        HpackIntegerResult validation = ValidateEncoding(
            value,
            prefixWidth,
            prefixBits);
        if (validation.Status == HttpOperationStatus.Rejected)
        {
            return validation;
        }

        int requiredSize = CalculateEncodedSize(value, prefixWidth);
        byte[] output = new byte[requiredSize];
        HpackIntegerResult written = Write(
            value,
            prefixWidth,
            prefixBits,
            output,
            0);
        written.Bytes = output;
        return written;
    }

    internal static HpackIntegerResult Write(
        long value,
        int prefixWidth,
        byte prefixBits,
        Span<byte> output,
        int offset)
    {
        HpackIntegerResult validation = ValidateEncoding(
            value,
            prefixWidth,
            prefixBits);
        if (validation.Status == HttpOperationStatus.Rejected)
        {
            return validation;
        }

        HpackIntegerResult result = new HpackIntegerResult();
        if (offset < 0)
        {
            result.IsMalformed = true;
            result.ErrorCode = "invalid-hpack-integer-offset";
            result.ErrorMessage = "An HPACK integer output offset cannot be negative.";
            result.Status = HttpOperationStatus.Rejected;
            return result;
        }

        int encodedSize = CalculateEncodedSize(value, prefixWidth);
        int requiredSize = int.MaxValue;
        if (encodedSize <= int.MaxValue - offset)
        {
            requiredSize = offset + encodedSize;
        }

        result.RequiredSize = requiredSize;
        if (offset > output.Length || encodedSize > output.Length - offset)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            return result;
        }

        int prefixMaximum = (1 << prefixWidth) - 1;
        if (value < prefixMaximum)
        {
            output[offset] = (byte)(prefixBits | (byte)value);
            result.Status = HttpOperationStatus.Done;
            result.Value = value;
            result.Written = 1;
            return result;
        }

        output[offset] = (byte)(prefixBits | (byte)prefixMaximum);
        long remaining = value - prefixMaximum;
        int position = offset + 1;
        while (remaining >= 128)
        {
            output[position] = (byte)(remaining % 128 + 128);
            remaining = remaining / 128;
            position = position + 1;
        }

        output[position] = (byte)remaining;
        result.Status = HttpOperationStatus.Done;
        result.Value = value;
        result.Written = encodedSize;
        return result;
    }

    static HpackIntegerResult ValidateEncoding(
        long value,
        int prefixWidth,
        byte prefixBits)
    {
        HpackIntegerResult result = new HpackIntegerResult();
        if (!IsValidPrefixWidth(prefixWidth))
        {
            return RejectInvalidPrefixWidth(result);
        }

        if (value < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsMalformed = true;
            result.ErrorCode = "invalid-hpack-integer-value";
            result.ErrorMessage = "An HPACK integer cannot be negative.";
            return result;
        }

        int prefixMaximum = (1 << prefixWidth) - 1;
        if ((prefixBits & prefixMaximum) != 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsMalformed = true;
            result.ErrorCode = "invalid-hpack-integer-prefix-bits";
            result.ErrorMessage =
                "HPACK representation bits cannot overlap the integer prefix.";
            return result;
        }

        result.Status = HttpOperationStatus.Done;
        return result;
    }

    static int CalculateEncodedSize(long value, int prefixWidth)
    {
        int prefixMaximum = (1 << prefixWidth) - 1;
        if (value < prefixMaximum)
        {
            return 1;
        }

        int size = 2;
        long remaining = value - prefixMaximum;
        while (remaining >= 128)
        {
            remaining = remaining / 128;
            size = size + 1;
        }

        return size;
    }

    static bool CanAddPayload(long value, int payload, int shift)
    {
        if (payload == 0)
        {
            return true;
        }

        if (shift >= 63)
        {
            return false;
        }

        long maximumPayload = (long.MaxValue - value) >> shift;
        return payload <= maximumPayload;
    }

    static bool IsValidPrefixWidth(int prefixWidth)
    {
        return prefixWidth >= 1 && prefixWidth <= 8;
    }

    static HpackIntegerResult RejectInvalidPrefixWidth(HpackIntegerResult result)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.IsMalformed = true;
        result.ErrorCode = "invalid-hpack-integer-prefix-width";
        result.ErrorMessage = "An HPACK integer prefix width must be from one to eight bits.";
        return result;
    }

    static HpackIntegerResult RejectIntegerOverflow(
        HpackIntegerResult result,
        int position,
        int consumed)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = consumed;
        result.RequiredSize = consumed;
        result.HasErrorPosition = true;
        result.ErrorPosition = position;
        result.IsOverflow = true;
        result.ErrorCode = "hpack-integer-overflow";
        result.ErrorMessage = "The HPACK integer exceeds the supported signed range.";
        return result;
    }

    static HpackIntegerResult RejectIntegerLength(
        HpackIntegerResult result,
        int position,
        int consumed)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = consumed;
        result.RequiredSize = consumed;
        result.HasErrorPosition = true;
        result.ErrorPosition = position;
        result.IsMalformed = true;
        result.ErrorCode = "hpack-integer-length-exceeded";
        result.ErrorMessage =
            "The HPACK integer encoding exceeds the supported length.";
        return result;
    }
}
