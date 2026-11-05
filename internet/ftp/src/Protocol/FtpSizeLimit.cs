namespace ftp;

internal class FtpSizeLimit
{
    internal long MaximumBytes;
    internal long ConsumedBytes;
    internal bool Exceeded;

    internal FtpSizeLimit(long maximumBytes)
    {
        if (maximumBytes < 0)
        {
            throw new FtpConfigurationException("A size limit must be non-negative.", null);
        }

        MaximumBytes = maximumBytes;
        ConsumedBytes = 0;
        Exceeded = false;
    }

    internal bool TryAddBytes(long byteCount)
    {
        if (byteCount < 0)
        {
            return false;
        }

        if (Exceeded)
        {
            return false;
        }

        if (byteCount > MaximumBytes - ConsumedBytes)
        {
            Exceeded = true;
            return false;
        }

        ConsumedBytes = ConsumedBytes + byteCount;
        return true;
    }

    internal long GetRemainingBytes()
    {
        return MaximumBytes - ConsumedBytes;
    }
}
