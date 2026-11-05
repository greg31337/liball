namespace json;

/// <summary>Serializes JSON document models through the UTF-8 writer.</summary>
public class JsonDocumentSerializer : IJsonDocumentSerializer
{
    /// <summary>Initializes a reusable JSON document serializer.</summary>
    public JsonDocumentSerializer()
    {
    }

    /// <summary>Serializes one document to an owned UTF-8 byte array.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="options">The output policies to apply.</param>
    /// <returns>The serialized UTF-8 bytes.</returns>
    public byte[] SerializeUtf8(
        JsonDocument document,
        JsonWriterOptions options)
    {
        global::System.Buffers.ArrayBufferWriter<byte> destination =
            new global::System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        JsonOperationResult result = SerializeTarget(
            document,
            options,
            target);
        ThrowIfSerializationFailed(result);

        byte[] utf8 = new byte[destination.WrittenCount];
        destination.WrittenSpan.CopyTo(utf8);
        return utf8;
    }

    /// <summary>Serializes one document to an owned UTF-16 string.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="options">The output policies to apply.</param>
    /// <returns>The serialized string.</returns>
    public string SerializeString(
        JsonDocument document,
        JsonWriterOptions options)
    {
        byte[] utf8 = SerializeUtf8(document, options);
        return global::System.Text.Encoding.UTF8.GetString(utf8);
    }

    /// <summary>Serializes one document to a caller-owned output destination.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="options">The output policies to apply.</param>
    /// <param name="target">The caller-owned output destination.</param>
    /// <returns>The serialization outcome.</returns>
    public JsonOperationResult SerializeTarget(
        JsonDocument document,
        JsonWriterOptions options,
        JsonWriterTarget target)
    {
        JsonWriterOptions normalized =
            JsonOptionNormalizer.NormalizeWriterOptions(options);
        JsonUtf8Writer writer = new JsonUtf8Writer(normalized, target);
        if (document == null || document.Root == null)
        {
            return CreateMissingDocumentFailure();
        }

        if (!normalized.SkipModelValidation)
        {
            JsonLimits limits = normalized.Limits
                ?? throw new global::System.InvalidOperationException(
                    "Normalized writer limits are required.");
            JsonModelValidator validator = new JsonModelValidator(limits);
            JsonOperationResult validation =
                validator.ValidateDocumentModel(document);
            if (!validation.Success)
            {
                return validation;
            }
        }

        JsonOperationResult write = writer.WriteJsonValue(document.Root);
        if (!write.Success)
        {
            write.BytesWritten = writer.GetBytesCommitted();
            return write;
        }

        return writer.CompleteWriter();
    }

    void ThrowIfSerializationFailed(JsonOperationResult result)
    {
        if (result.Success)
        {
            return;
        }

        JsonDiagnostic diagnostic = result.Diagnostic
            ?? throw new global::System.InvalidOperationException(
                "A failed serialization requires a diagnostic.");
        if (IsLimitCode(diagnostic.Code))
        {
            throw new JsonLimitException(diagnostic);
        }

        throw new JsonWriteException(diagnostic);
    }

    bool IsLimitCode(JsonDiagnosticCode code)
    {
        return code >= JsonDiagnosticCode.DocumentByteLimitExceeded &&
            code <= JsonDiagnosticCode.OutputByteLimitExceeded;
    }

    JsonOperationResult CreateMissingDocumentFailure()
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidModel;
        diagnostic.Message =
            "A JSON document with one root value is required for serialization.";
        diagnostic.ActualToken = "document";
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.Diagnostic = diagnostic;
        return result;
    }
}
