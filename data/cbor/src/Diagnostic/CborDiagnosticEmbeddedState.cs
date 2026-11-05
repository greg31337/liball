namespace cbor;

internal class CborDiagnosticEmbeddedState
{
    internal int MaxDepth;
    internal int MaxItems;
    internal long MaxBytes;
    internal int Depth;
    internal int Items;
    internal long Bytes;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal int ErrorOffset = -1;
    internal string? ErrorPath;

    internal CborDiagnosticEmbeddedState(
        int maxDepth,
        int maxItems,
        long maxBytes)
    {
        MaxDepth = maxDepth;
        MaxItems = maxItems;
        MaxBytes = maxBytes;
    }

    internal bool Validate()
    {
        if (MaxDepth < 0)
        {
            return Fail(
                "CBOR_DIAGNOSTIC_EMBEDDED_DEPTH_INVALID",
                "The embedded diagnostic depth limit cannot be negative.",
                -1,
                "$"
            );
        }
        if (MaxItems < 0)
        {
            return Fail(
                "CBOR_DIAGNOSTIC_EMBEDDED_ITEM_LIMIT_INVALID",
                "The embedded diagnostic item limit cannot be negative.",
                -1,
                "$"
            );
        }
        if (MaxBytes < 0)
        {
            return Fail(
                "CBOR_DIAGNOSTIC_EMBEDDED_BYTE_LIMIT_INVALID",
                "The embedded diagnostic byte limit cannot be negative.",
                -1,
                "$"
            );
        }
        return true;
    }

    internal bool Enter(int offset, string path)
    {
        if (Depth >= MaxDepth)
        {
            return Fail(
                "CBOR_DIAGNOSTIC_MAX_EMBEDDED_DEPTH",
                "The diagnostic value exceeds the embedded CBOR depth limit.",
                offset,
                path
            );
        }
        Depth = Depth + 1;
        return true;
    }

    internal void Leave()
    {
        if (Depth > 0)
        {
            Depth = Depth - 1;
        }
    }

    internal bool AddItem(
        int itemCount,
        long byteCount,
        int offset,
        string path)
    {
        if (itemCount < 0 || itemCount > MaxItems - Items)
        {
            return Fail(
                "CBOR_DIAGNOSTIC_MAX_EMBEDDED_ITEMS",
                "The diagnostic value exceeds the embedded CBOR item limit.",
                offset,
                path
            );
        }
        if (byteCount < 0 || byteCount > MaxBytes - Bytes)
        {
            return Fail(
                "CBOR_DIAGNOSTIC_MAX_EMBEDDED_BYTES",
                "The diagnostic value exceeds the embedded CBOR byte limit.",
                offset,
                path
            );
        }
        Items = Items + itemCount;
        Bytes = Bytes + byteCount;
        return true;
    }

    internal bool Fail(
        string code,
        string message,
        int offset,
        string path)
    {
        if (ErrorCode == null)
        {
            ErrorCode = code;
            ErrorMessage = message;
            ErrorOffset = offset;
            ErrorPath = path;
        }
        return false;
    }
}
