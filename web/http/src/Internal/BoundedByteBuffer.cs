using System;

namespace http;

internal class BoundedByteBuffer
{
    byte[] storage = Array.Empty<byte>();
    int count;
    int limit;

    internal BoundedByteBuffer(int maximumSize)
    {
        if (maximumSize < 0)
        {
            throw new ArgumentOutOfRangeException("maximumSize");
        }

        limit = maximumSize;
    }

    internal OctetOperationResult AppendByte(byte value)
    {
        OctetOperationResult result = new OctetOperationResult();
        result.RequiredSize = count;
        if (count < int.MaxValue)
        {
            result.RequiredSize = count + 1;
        }

        if (count >= limit)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsLimitExceeded = true;
            return result;
        }

        EnsureCapacity(count + 1);
        storage[count] = value;
        count = count + 1;
        result.Status = HttpOperationStatus.Done;
        result.Written = 1;
        return result;
    }

    internal OctetOperationResult AppendBytes(ReadOnlySpan<byte> value)
    {
        OctetOperationResult result = new OctetOperationResult();
        int requiredSize = int.MaxValue;
        if (value.Length <= int.MaxValue - count)
        {
            requiredSize = count + value.Length;
        }

        result.RequiredSize = requiredSize;
        if (value.Length > limit - count)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.IsLimitExceeded = true;
            return result;
        }

        EnsureCapacity(requiredSize);
        int index = 0;
        while (index < value.Length)
        {
            storage[count + index] = value[index];
            index = index + 1;
        }

        count = requiredSize;
        result.Status = HttpOperationStatus.Done;
        result.Written = value.Length;
        return result;
    }

    internal OctetOperationResult CopyTo(Span<byte> output, int offset)
    {
        OctetOperationResult result = new OctetOperationResult();
        if (offset < 0)
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
        if (offset > output.Length || count > output.Length - offset)
        {
            result.Status = HttpOperationStatus.NeedOutput;
            return result;
        }

        int index = 0;
        while (index < count)
        {
            output[offset + index] = storage[index];
            index = index + 1;
        }

        result.Status = HttpOperationStatus.Done;
        result.Written = count;
        return result;
    }

    internal int GetCount()
    {
        return count;
    }

    internal int GetLimit()
    {
        return limit;
    }

    internal byte[] ToArray()
    {
        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = storage[index];
            index = index + 1;
        }

        return result;
    }

    internal void Clear()
    {
        count = 0;
    }

    void EnsureCapacity(int requiredSize)
    {
        if (requiredSize <= storage.Length)
        {
            return;
        }

        int capacity = storage.Length;
        if (capacity == 0)
        {
            capacity = 16;
            if (limit < capacity)
            {
                capacity = limit;
            }
        }

        while (capacity < requiredSize)
        {
            if (capacity > limit / 2)
            {
                capacity = limit;
            }
            else
            {
                capacity = capacity * 2;
            }
        }

        byte[] replacement = new byte[capacity];
        int index = 0;
        while (index < count)
        {
            replacement[index] = storage[index];
            index = index + 1;
        }

        storage = replacement;
    }
}
