namespace email;

internal class EmailEncodedWordCandidate
{
    int consumedByteCount;
    bool complete;
    int charsetOffset;
    int charsetByteCount;
    int encodingOffset;
    int encodingByteCount;
    int payloadOffset;
    int payloadByteCount;

    internal EmailEncodedWordCandidate(int consumedByteCount, bool complete, int charsetOffset, int charsetByteCount, int encodingOffset, int encodingByteCount, int payloadOffset, int payloadByteCount)
    {
        this.consumedByteCount = consumedByteCount;
        this.complete = complete;
        this.charsetOffset = charsetOffset;
        this.charsetByteCount = charsetByteCount;
        this.encodingOffset = encodingOffset;
        this.encodingByteCount = encodingByteCount;
        this.payloadOffset = payloadOffset;
        this.payloadByteCount = payloadByteCount;
    }

    internal int GetConsumedByteCount()
    {
        return consumedByteCount;
    }

    internal bool IsComplete()
    {
        return complete;
    }

    internal int GetCharsetOffset()
    {
        return charsetOffset;
    }

    internal int GetCharsetByteCount()
    {
        return charsetByteCount;
    }

    internal int GetEncodingOffset()
    {
        return encodingOffset;
    }

    internal int GetEncodingByteCount()
    {
        return encodingByteCount;
    }

    internal int GetPayloadOffset()
    {
        return payloadOffset;
    }

    internal int GetPayloadByteCount()
    {
        return payloadByteCount;
    }
}
