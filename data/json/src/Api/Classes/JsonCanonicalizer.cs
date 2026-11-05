namespace json;

/// <summary>Produces and verifies RFC 8785 canonical JSON.</summary>
public class JsonCanonicalizer : IJsonCanonicalizer
{
    /// <summary>Initializes a reusable JSON canonicalizer.</summary>
    public JsonCanonicalizer()
    {
    }

    /// <summary>Canonicalizes one complete UTF-8 JSON input.</summary>
    /// <param name="utf8">The complete UTF-8 input, which is not retained.</param>
    /// <param name="options">The input policies and limits.</param>
    /// <returns>The canonical UTF-8 bytes.</returns>
    public byte[] CanonicalizeUtf8(
        global::System.ReadOnlySpan<byte> utf8,
        JsonReaderOptions options)
    {
        JsonReaderOptions jcsOptions = CreateJcsReaderOptions(options);
        JsonDocumentParser parser = new JsonDocumentParser();
        JsonDocument document = parser.ParseUtf8(utf8, jcsOptions);
        JsonLimits limits = jcsOptions.Limits
            ?? throw new global::System.InvalidOperationException(
                "Normalized reader limits are required.");
        return WriteOwnedDocument(document, limits);
    }

    /// <summary>Canonicalizes one JSON document model.</summary>
    /// <param name="document">The document to canonicalize.</param>
    /// <returns>The canonical UTF-8 bytes.</returns>
    public byte[] CanonicalizeDocument(JsonDocument document)
    {
        JsonLimits limits = JsonOptionNormalizer.NormalizeLimits(null);
        return WriteOwnedDocument(document, limits);
    }

    /// <summary>Writes one canonicalized document to a caller-owned destination.</summary>
    /// <param name="document">The document to canonicalize.</param>
    /// <param name="target">The caller-owned output destination.</param>
    /// <returns>The canonicalization outcome.</returns>
    public JsonOperationResult WriteCanonicalTarget(
        JsonDocument document,
        JsonWriterTarget target)
    {
        JsonLimits limits = JsonOptionNormalizer.NormalizeLimits(null);
        JsonCanonicalWriter writer = new JsonCanonicalWriter(limits, target);
        return writer.WriteDocument(document);
    }

    /// <summary>Checks whether UTF-8 input already equals its canonical form.</summary>
    /// <param name="utf8">The complete UTF-8 input, which is not retained.</param>
    /// <param name="options">The input policies and limits.</param>
    /// <returns>The verification outcome.</returns>
    public JsonOperationResult VerifyCanonicalUtf8(
        global::System.ReadOnlySpan<byte> utf8,
        JsonReaderOptions options)
    {
        byte[] ownedInput = new byte[utf8.Length];
        utf8.CopyTo(ownedInput);
        byte[]? canonical = null;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
            canonical = CanonicalizeUtf8(ownedInput, options));
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            return CreateFailureFromException(failure);
        }

        byte[] output = canonical
            ?? throw new global::System.InvalidOperationException(
                "Canonical verification requires output bytes.");
        bool equal = output.Length == ownedInput.Length;
        int index = 0;
        while (equal && index < output.Length)
        {
            equal = output[index] == ownedInput[index];
            index++;
        }

        JsonOperationResult result = new JsonOperationResult();
        result.Success = equal;
        result.BytesConsumed = ownedInput.Length;
        if (!equal)
        {
            JsonDiagnostic diagnostic = new JsonDiagnostic();
            diagnostic.Code = JsonDiagnosticCode.InvalidRawValue;
            diagnostic.Message =
                "The input is valid JSON but does not use its canonical representation.";
            diagnostic.ActualToken = "CanonicalJson";
            result.Diagnostic = diagnostic;
        }

        return result;
    }

    byte[] WriteOwnedDocument(
        JsonDocument document,
        JsonLimits limits)
    {
        global::System.Buffers.ArrayBufferWriter<byte> destination =
            new global::System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        JsonCanonicalWriter writer = new JsonCanonicalWriter(limits, target);
        JsonOperationResult result = writer.WriteDocument(document);
        ThrowIfCanonicalizationFailed(result);

        byte[] output = new byte[destination.WrittenCount];
        destination.WrittenSpan.CopyTo(output);
        return output;
    }

    JsonReaderOptions CreateJcsReaderOptions(JsonReaderOptions options)
    {
        JsonReaderOptions source =
            JsonOptionNormalizer.NormalizeReaderOptions(options);
        JsonReaderOptions jcs = new JsonReaderOptions();
        jcs.ConformanceProfile = JsonConformanceProfile.Jcs;
        jcs.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        jcs.InputMode = JsonInputMode.SingleValue;
        jcs.SequenceErrorPolicy = JsonSequenceErrorPolicy.Stop;
        jcs.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        jcs.BomPolicy = JsonBomPolicy.Reject;
        jcs.TrackLineInformation = source.TrackLineInformation;
        jcs.TrackUnicodeScalarColumn = source.TrackUnicodeScalarColumn;
        jcs.CaptureRawTokens = source.CaptureRawTokens;
        jcs.CaptureSourceRanges = source.CaptureSourceRanges;
        jcs.StreamBufferBytes = source.StreamBufferBytes;
        jcs.Limits = source.Limits;
        return JsonOptionNormalizer.NormalizeReaderOptions(jcs);
    }

    void ThrowIfCanonicalizationFailed(JsonOperationResult result)
    {
        if (result.Success)
        {
            return;
        }

        JsonDiagnostic diagnostic = result.Diagnostic
            ?? throw new global::System.InvalidOperationException(
                "A failed canonicalization requires a diagnostic.");
        if (IsLimitCode(diagnostic.Code))
        {
            throw new JsonLimitException(diagnostic);
        }

        throw new JsonWriteException(diagnostic);
    }

    JsonOperationResult CreateFailureFromException(
        global::System.Exception failure)
    {
        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure != null)
        {
            return CreateFailure(parseFailure.Diagnostic);
        }

        JsonWriteException? writeFailure = failure as JsonWriteException;
        if (writeFailure != null)
        {
            return CreateFailure(writeFailure.Diagnostic);
        }

        JsonLimitException? limitFailure = failure as JsonLimitException;
        if (limitFailure != null)
        {
            return CreateFailure(limitFailure.Diagnostic);
        }

        throw failure;
    }

    bool IsLimitCode(JsonDiagnosticCode code)
    {
        return code >= JsonDiagnosticCode.DocumentByteLimitExceeded &&
            code <= JsonDiagnosticCode.OutputByteLimitExceeded;
    }

    JsonOperationResult CreateFailure(JsonDiagnostic diagnostic)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.Diagnostic = diagnostic;
        return result;
    }
}
