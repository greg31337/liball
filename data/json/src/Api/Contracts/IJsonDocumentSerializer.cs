namespace json;

/// <summary>Serializes JSON values using explicit output policies.</summary>
public interface IJsonDocumentSerializer
{
    /// <summary>Serializes one document to an owned UTF-8 byte array.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="options">The output policies to apply.</param>
    /// <returns>The serialized UTF-8 bytes.</returns>
    byte[] SerializeUtf8(JsonDocument document, JsonWriterOptions options);

    /// <summary>Serializes one document to an owned UTF-16 string.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="options">The output policies to apply.</param>
    /// <returns>The serialized string.</returns>
    string SerializeString(JsonDocument document, JsonWriterOptions options);

    /// <summary>Serializes one document to a caller-owned output destination.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="options">The output policies to apply.</param>
    /// <param name="target">The caller-owned output destination.</param>
    /// <returns>The serialization outcome.</returns>
    JsonOperationResult SerializeTarget(JsonDocument document, JsonWriterOptions options, JsonWriterTarget target);
}
