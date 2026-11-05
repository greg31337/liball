namespace cbor;

internal class CborSequenceState
{
    internal int MaxItems;
    internal long MaxBytes;
    internal int ItemCount;
    internal long ByteCount;
    internal string? ErrorCode;
    internal string? ErrorMessage;

    internal CborSequenceState(int maxItems, long maxBytes)
    {
        MaxItems = maxItems;
        MaxBytes = maxBytes;
    }

    internal bool CanStartItem()
    {
        ErrorCode = null;
        ErrorMessage = null;
        if (MaxItems < 0 || ItemCount >= MaxItems)
        {
            return Fail(
                "CBOR_MAX_SEQUENCE_ITEMS",
                "The CBOR Sequence exceeds the configured item limit."
            );
        }
        if (MaxBytes < 0 || ByteCount >= MaxBytes)
        {
            return Fail(
                "CBOR_MAX_SEQUENCE_BYTES",
                "The CBOR Sequence exceeds the configured byte limit."
            );
        }
        return true;
    }

    internal bool AddItem(int encodedLength)
    {
        ErrorCode = null;
        ErrorMessage = null;
        if (encodedLength <= 0)
        {
            return Fail(
                "CBOR_SEQUENCE_ITEM_LENGTH_INVALID",
                "A CBOR Sequence item must contain at least one encoded byte."
            );
        }
        if (MaxItems < 0 || ItemCount >= MaxItems)
        {
            return Fail(
                "CBOR_MAX_SEQUENCE_ITEMS",
                "The CBOR Sequence exceeds the configured item limit."
            );
        }
        if (MaxBytes < 0 || ByteCount > MaxBytes - encodedLength)
        {
            return Fail(
                "CBOR_MAX_SEQUENCE_BYTES",
                "The CBOR Sequence exceeds the configured byte limit."
            );
        }
        if (ItemCount == int.MaxValue)
        {
            return Fail(
                "CBOR_SEQUENCE_ITEM_COUNT_OVERFLOW",
                "The CBOR Sequence item count exceeds the supported range."
            );
        }
        if (ByteCount > long.MaxValue - encodedLength)
        {
            return Fail(
                "CBOR_SEQUENCE_BYTE_COUNT_OVERFLOW",
                "The CBOR Sequence byte count exceeds the supported range."
            );
        }
        ItemCount = ItemCount + 1;
        ByteCount = ByteCount + encodedLength;
        return true;
    }

    internal bool ValidateEmpty()
    {
        ErrorCode = null;
        ErrorMessage = null;
        if (MaxItems < 0)
        {
            return Fail(
                "CBOR_MAX_SEQUENCE_ITEMS",
                "The CBOR Sequence item limit cannot be negative."
            );
        }
        if (MaxBytes < 0)
        {
            return Fail(
                "CBOR_MAX_SEQUENCE_BYTES",
                "The CBOR Sequence byte limit cannot be negative."
            );
        }
        return true;
    }

    internal bool Fail(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        return false;
    }
}
