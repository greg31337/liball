using System;

namespace http;

internal static class OctetWriter
{
    internal static OctetOperationResult WriteByte(byte value, Span<byte> output, int offset, int limit)
    {
        OctetOperationResult result = new OctetOperationResult();
        if (offset < 0 || limit < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsMalformed = true;
            return result;
        }

        result.RequiredSize = offset;
        if (offset < int.MaxValue)
        {
            result.RequiredSize = offset + 1;
        }

        if (offset >= limit)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsLimitExceeded = true;
            return result;
        }

        if (offset >= output.Length)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            return result;
        }

        output[offset] = value;
        result.Status = HttpOperationStatus.Done;
        result.Written = 1;
        return result;
    }

    internal static OctetOperationResult WriteBytes(ReadOnlySpan<byte> input, Span<byte> output, int offset, int limit)
    {
        OctetOperationResult result = new OctetOperationResult();
        if (offset < 0 || limit < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsMalformed = true;
            return result;
        }

        int requiredSize = int.MaxValue;
        if (input.Length <= int.MaxValue - offset)
        {
            requiredSize = offset + input.Length;
        }

        result.RequiredSize = requiredSize;
        if (offset > limit || input.Length > limit - offset)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsLimitExceeded = true;
            return result;
        }

        if (offset > output.Length || input.Length > output.Length - offset)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            return result;
        }

        int index = 0;
        while (index < input.Length)
        {
            output[offset + index] = input[index];
            index = index + 1;
        }

        result.Status = HttpOperationStatus.Done;
        result.Written = input.Length;
        return result;
    }

    internal static OctetOperationResult WriteAsciiString(string value, Span<byte> output, int offset, int limit)
    {
        OctetOperationResult result = new OctetOperationResult();
        if (offset < 0 || limit < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsMalformed = true;
            return result;
        }

        int requiredSize = int.MaxValue;
        if (value.Length <= int.MaxValue - offset)
        {
            requiredSize = offset + value.Length;
        }

        result.RequiredSize = requiredSize;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                result.Status = HttpOperationStatus.Rejected;
                result.IsMalformed = true;
                return result;
            }

            index = index + 1;
        }

        if (offset > limit || value.Length > limit - offset)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsLimitExceeded = true;
            return result;
        }

        if (offset > output.Length || value.Length > output.Length - offset)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            return result;
        }

        index = 0;
        while (index < value.Length)
        {
            output[offset + index] = (byte)value[index];
            index = index + 1;
        }

        result.Status = HttpOperationStatus.Done;
        result.Written = value.Length;
        return result;
    }
}
