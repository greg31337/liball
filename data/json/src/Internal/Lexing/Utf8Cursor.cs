namespace json;

internal class Utf8Cursor
{
    JsonByteQueue queue;
    Utf8Decoder decoder;
    int currentScalarByteCount;
    long currentScalarByteOffset;
    int lastScalarByteCount;
    long lastScalarByteOffset;

    internal Utf8Cursor(JsonByteQueue queue)
    {
        this.queue = queue;
        decoder = new Utf8Decoder();
    }

    internal int ReadScalar(bool finalInput)
    {
        while (queue.GetBufferedCount() > 0)
        {
            if (currentScalarByteCount == 0)
            {
                currentScalarByteOffset = queue.GetConsumedByteCount();
            }

            byte value = queue.GetByteAt(0);
            queue.ConsumeBytes(1);
            currentScalarByteCount++;
            int decodedScalar = decoder.PushByte(value);
            if (decodedScalar != Utf8Decoder.NeedMoreData)
            {
                lastScalarByteCount = currentScalarByteCount;
                lastScalarByteOffset = currentScalarByteOffset;
                currentScalarByteCount = 0;
                return decodedScalar;
            }
        }

        if (finalInput)
        {
            decoder.CompleteDecoder();
        }

        return Utf8Decoder.NeedMoreData;
    }

    internal bool HasPendingScalarBytes()
    {
        return decoder.HasPendingBytes();
    }

    internal int GetPendingScalarByteCount()
    {
        return currentScalarByteCount;
    }

    internal int GetLastScalarByteCount()
    {
        return lastScalarByteCount;
    }

    internal long GetLastScalarByteOffset()
    {
        return lastScalarByteOffset;
    }

    internal void ResetCursor()
    {
        queue.ResetQueue();
        decoder.ResetDecoder();
        currentScalarByteCount = 0;
        currentScalarByteOffset = 0;
        lastScalarByteCount = 0;
        lastScalarByteOffset = 0;
    }
}
