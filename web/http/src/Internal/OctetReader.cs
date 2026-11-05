using System;

namespace http;

internal static class OctetReader
{
    internal static OctetOperationResult ReadByte(ReadOnlySpan<byte> input, int offset, int limit)
    {
        OctetOperationResult result = new OctetOperationResult();
        if (offset < 0 || offset > input.Length || limit < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsMalformed = true;
            return result;
        }

        if (offset >= limit)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsLimitExceeded = true;
            result.RequiredSize = offset;
            if (offset < int.MaxValue)
            {
                result.RequiredSize = offset + 1;
            }

            return result;
        }

        if (offset >= input.Length)
        {
            result.Status = HttpOperationStatus.NeedInput;
            result.RequiredSize = offset + 1;
            return result;
        }

        result.Status = HttpOperationStatus.Done;
        result.Consumed = 1;
        result.RequiredSize = offset + 1;
        result.Value = input[offset];
        return result;
    }

    internal static OctetOperationResult ReadBytes(ReadOnlySpan<byte> input, int offset, int count, int limit)
    {
        OctetOperationResult result = new OctetOperationResult();
        if (offset < 0 || offset > input.Length || count < 0 || limit < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsMalformed = true;
            return result;
        }

        int requiredSize = int.MaxValue;
        if (count <= int.MaxValue - offset)
        {
            requiredSize = offset + count;
        }

        result.RequiredSize = requiredSize;
        if (offset > limit || count > limit - offset)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsLimitExceeded = true;
            return result;
        }

        if (count > input.Length - offset)
        {
            result.Status = HttpOperationStatus.NeedInput;
            return result;
        }

        byte[] bytes = new byte[count];
        int index = 0;
        while (index < count)
        {
            bytes[index] = input[offset + index];
            index = index + 1;
        }

        result.Status = HttpOperationStatus.Done;
        result.Consumed = count;
        result.Bytes = bytes;
        return result;
    }

    internal static OctetOperationResult ExpectByte(ReadOnlySpan<byte> input, int offset, byte expected, int limit)
    {
        OctetOperationResult result = ReadByte(input, offset, limit);
        if (result.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        if (result.Value != expected)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.Consumed = 0;
            result.IsMalformed = true;
        }

        return result;
    }
}
