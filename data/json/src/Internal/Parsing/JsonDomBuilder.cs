namespace json;

internal class JsonDomBuilder
{
    JsonValueCaptureBuilder valueBuilder;
    JsonConformanceProfile conformanceProfile;
    JsonDuplicateKeyPolicy duplicateKeyPolicy;
    JsonUnicodePolicy unicodePolicy;
    JsonInputMode inputMode;
    JsonBomPolicy bomPolicy;
    JsonValue? root;
    JsonDocument? document;
    bool complete;

    internal JsonDomBuilder(JsonReaderOptions? options)
    {
        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException("Normalized limits are required.");
        valueBuilder = new JsonValueCaptureBuilder(
            normalized.CaptureSourceRanges,
            limits.MaximumDomNodes);
        conformanceProfile = normalized.ConformanceProfile;
        duplicateKeyPolicy = normalized.DuplicateKeyPolicy;
        unicodePolicy = normalized.UnicodePolicy;
        inputMode = normalized.InputMode;
        bomPolicy = normalized.BomPolicy;
    }

    internal bool AcceptDomToken(
        JsonToken token,
        JsonDuplicateDecision? duplicateDecision)
    {
        if (complete)
        {
            throw new global::System.InvalidOperationException("The document value is already complete.");
        }

        complete = valueBuilder.AcceptCaptureToken(token, duplicateDecision);
        if (complete)
        {
            root = valueBuilder.GetCapturedValue();
        }

        return complete;
    }

    internal bool IsDomComplete()
    {
        return complete;
    }

    internal JsonDocument GetDomDocument(long sourceByteCount)
    {
        if (sourceByteCount < 0)
        {
            throw new global::System.ArgumentOutOfRangeException("sourceByteCount");
        }

        if (!complete || root == null)
        {
            throw new global::System.InvalidOperationException("The document value is not complete.");
        }

        if (document == null)
        {
            document = new JsonDocument();
            document.Root = root;
            document.ConformanceProfile = conformanceProfile;
            document.DuplicateKeyPolicy = duplicateKeyPolicy;
            document.UnicodePolicy = unicodePolicy;
            document.InputMode = inputMode;
            document.BomPolicy = bomPolicy;
            document.SourceByteCount = sourceByteCount;
        }
        else if (document.SourceByteCount != sourceByteCount)
        {
            throw new global::System.InvalidOperationException("The document source byte count is already fixed.");
        }

        return document;
    }

    internal void ResetDomBuilder()
    {
        valueBuilder.ResetCaptureBuilder();
        root = null;
        document = null;
        complete = false;
    }
}
