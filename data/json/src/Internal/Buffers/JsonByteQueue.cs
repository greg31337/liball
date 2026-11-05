namespace json;

internal class JsonByteQueue
{
    byte[] buffer;
    int start;
    int count;
    int maximumBufferedBytes;
    long acceptedByteCount;
    long consumedByteCount;

    internal JsonByteQueue(int maximumBufferedBytes)
    {
        if (maximumBufferedBytes < 0)
        {
            throw new JsonLimitException(JsonOptionNormalizer.CreateLimitDiagnostic("MaximumInputBufferBytes cannot be negative."));
        }

        this.maximumBufferedBytes = maximumBufferedBytes == 0 ? JsonDefaults.MaximumInputBufferBytes : maximumBufferedBytes;
        int initialCapacity = JsonDefaults.StreamBufferBytes;
        if (initialCapacity > this.maximumBufferedBytes)
        {
            initialCapacity = this.maximumBufferedBytes;
        }

        buffer = new byte[initialCapacity];
    }

    internal void EnqueueBytes(global::System.ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > maximumBufferedBytes - count)
        {
            throw new JsonLimitException(CreateQueueLimitDiagnostic("The buffered input byte limit was exceeded."));
        }

        if (acceptedByteCount > long.MaxValue - bytes.Length)
        {
            throw new JsonLimitException(CreateQueueLimitDiagnostic("The accepted input byte counter would overflow."));
        }

        EnsureAppendCapacity(bytes.Length);
        bytes.CopyTo(new global::System.Span<byte>(buffer, start + count, bytes.Length));
        count += bytes.Length;
        acceptedByteCount += bytes.Length;
    }

    internal void ConsumeBytes(int length)
    {
        if (length < 0 || length > count)
        {
            throw new global::System.ArgumentOutOfRangeException("length");
        }

        start += length;
        count -= length;
        consumedByteCount += length;
        if (count == 0)
        {
            start = 0;
        }
    }

    internal int GetBufferedCount()
    {
        return count;
    }

    internal int GetCapacity()
    {
        return buffer.Length;
    }

    internal int GetMaximumBufferedBytes()
    {
        return maximumBufferedBytes;
    }

    internal long GetAcceptedByteCount()
    {
        return acceptedByteCount;
    }

    internal long GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    internal byte GetByteAt(int index)
    {
        if (index < 0 || index >= count)
        {
            throw new global::System.ArgumentOutOfRangeException("index");
        }

        return buffer[start + index];
    }

    internal byte[] CopyBytes(int offset, int length)
    {
        if (offset < 0 || offset > count)
        {
            throw new global::System.ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > count - offset)
        {
            throw new global::System.ArgumentOutOfRangeException("length");
        }

        byte[] result = new byte[length];
        if (length > 0)
        {
            global::System.Buffer.BlockCopy(buffer, start + offset, result, 0, length);
        }

        return result;
    }

    internal global::System.ReadOnlySpan<byte> GetBufferedSpan()
    {
        return new global::System.ReadOnlySpan<byte>(buffer, start, count);
    }

    internal void CompactBuffer()
    {
        if (start == 0)
        {
            return;
        }

        if (count > 0)
        {
            global::System.Buffer.BlockCopy(buffer, start, buffer, 0, count);
        }

        start = 0;
    }

    internal void ResetQueue()
    {
        start = 0;
        count = 0;
        acceptedByteCount = 0;
        consumedByteCount = 0;
    }

    internal void EnsureAppendCapacity(int appendLength)
    {
        int required = count + appendLength;
        if (required <= buffer.Length)
        {
            if (appendLength <= buffer.Length - start - count)
            {
                return;
            }

            CompactBuffer();
            return;
        }

        int newCapacity = buffer.Length;
        if (newCapacity == 0)
        {
            newCapacity = 1;
        }

        while (newCapacity < required)
        {
            if (newCapacity > maximumBufferedBytes / 2)
            {
                newCapacity = maximumBufferedBytes;
            }
            else
            {
                newCapacity *= 2;
            }
        }

        byte[] replacement = new byte[newCapacity];
        if (count > 0)
        {
            global::System.Buffer.BlockCopy(buffer, start, replacement, 0, count);
        }

        buffer = replacement;
        start = 0;
    }

    internal static JsonDiagnostic CreateQueueLimitDiagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InputBufferLimitExceeded;
        diagnostic.Message = message;
        return diagnostic;
    }
}
