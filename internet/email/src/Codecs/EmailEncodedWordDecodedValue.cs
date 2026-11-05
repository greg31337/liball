namespace email;

internal class EmailEncodedWordDecodedValue
{
    byte[] bytes;
    bool invalidPayload;
    bool limitReached;

    internal EmailEncodedWordDecodedValue(byte[] bytes, bool invalidPayload, bool limitReached)
    {
        this.bytes = bytes;
        this.invalidPayload = invalidPayload;
        this.limitReached = limitReached;
    }

    internal byte[] GetBytes()
    {
        return bytes;
    }

    internal bool HasInvalidPayload()
    {
        return invalidPayload;
    }

    internal bool HasReachedLimit()
    {
        return limitReached;
    }
}
