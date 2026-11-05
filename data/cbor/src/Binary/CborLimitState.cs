namespace cbor;

internal class CborLimitState
{
    internal CborDecodeOptions Options;
    internal long TotalBytes;
    internal int TotalItems;
    internal long RetainedBytes;
    internal int EmbeddedDepth;

    internal CborLimitState(CborDecodeOptions options)
    {
        Options = options;
    }

    internal bool AllowsPendingBytes(int pendingBytes)
    {
        return Options.MaxTotalBytes >= 0
            && TotalBytes <= Options.MaxTotalBytes - pendingBytes;
    }

    internal int LimitPendingSourceBytes(int pendingBytes, int sourceBytes)
    {
        if (!AllowsPendingBytes(pendingBytes))
        {
            return -1;
        }
        long remainingBytes = Options.MaxTotalBytes - TotalBytes - pendingBytes;
        if (remainingBytes < sourceBytes)
        {
            return (int)remainingBytes;
        }
        return sourceBytes;
    }

    internal bool AllowsEncodedBytes(int encodedBytes)
    {
        return Options.MaxTotalBytes >= 0
            && TotalBytes <= Options.MaxTotalBytes - encodedBytes;
    }

    internal bool AllowsItem()
    {
        return Options.MaxTotalItems >= 0 && TotalItems < Options.MaxTotalItems;
    }

    internal bool AllowsNesting(int depth)
    {
        return Options.MaxNestingDepth >= 0 && depth <= Options.MaxNestingDepth;
    }

    internal bool AllowsStringBytes(ulong length)
    {
        return Options.MaxStringBytes >= 0 && length <= (ulong)Options.MaxStringBytes;
    }

    internal bool AllowsStringTotal(ulong currentLength, ulong additionalLength)
    {
        if (!AllowsStringBytes(additionalLength))
        {
            return false;
        }
        return currentLength <= (ulong)Options.MaxStringBytes - additionalLength;
    }

    internal bool AllowsArrayItems(ulong itemCount)
    {
        return Options.MaxArrayItems >= 0 && itemCount <= (ulong)Options.MaxArrayItems;
    }

    internal bool AllowsMapPairs(ulong pairCount)
    {
        return Options.MaxMapPairs >= 0 && pairCount <= (ulong)Options.MaxMapPairs;
    }

    internal bool AllowsRetainedBytes(int encodedBytes)
    {
        return Options.MaxRetainedSourceBytes >= 0
            && RetainedBytes <= Options.MaxRetainedSourceBytes - encodedBytes;
    }

    internal bool AllowsEmbeddedEntry()
    {
        return Options.MaxEmbeddedDepth >= 0 && EmbeddedDepth < Options.MaxEmbeddedDepth;
    }

    internal void CommitEncodedBytes(int encodedBytes)
    {
        TotalBytes = TotalBytes + encodedBytes;
    }

    internal void CommitItem()
    {
        TotalItems = TotalItems + 1;
    }

    internal void CommitRetainedBytes(int encodedBytes)
    {
        RetainedBytes = RetainedBytes + encodedBytes;
    }

    internal void EnterEmbedded()
    {
        EmbeddedDepth = EmbeddedDepth + 1;
    }

    internal void ExitEmbedded()
    {
        if (EmbeddedDepth > 0)
        {
            EmbeddedDepth = EmbeddedDepth - 1;
        }
    }

    internal void Reset()
    {
        TotalBytes = 0;
        TotalItems = 0;
        RetainedBytes = 0;
        EmbeddedDepth = 0;
    }
}
