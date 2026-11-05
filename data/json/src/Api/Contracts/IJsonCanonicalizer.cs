namespace json;

/// <summary>Produces and verifies RFC 8785 JSON canonical form.</summary>
public interface IJsonCanonicalizer
{
    /// <summary>Canonicalizes one complete UTF-8 JSON input.</summary>
    /// <param name="utf8">The complete UTF-8 input, which is not retained.</param>
    /// <param name="options">The input policies and limits.</param>
    /// <returns>The canonical UTF-8 bytes.</returns>
    byte[] CanonicalizeUtf8(global::System.ReadOnlySpan<byte> utf8, JsonReaderOptions options);

    /// <summary>Canonicalizes one JSON document model.</summary>
    /// <param name="document">The document to canonicalize.</param>
    /// <returns>The canonical UTF-8 bytes.</returns>
    byte[] CanonicalizeDocument(JsonDocument document);

    /// <summary>Writes one canonicalized document to a caller-owned destination.</summary>
    /// <param name="document">The document to canonicalize.</param>
    /// <param name="target">The caller-owned output destination.</param>
    /// <returns>The canonicalization outcome.</returns>
    JsonOperationResult WriteCanonicalTarget(JsonDocument document, JsonWriterTarget target);

    /// <summary>Checks whether UTF-8 input already equals its canonical form.</summary>
    /// <param name="utf8">The complete UTF-8 input, which is not retained.</param>
    /// <param name="options">The input policies and limits.</param>
    /// <returns>The verification outcome.</returns>
    JsonOperationResult VerifyCanonicalUtf8(global::System.ReadOnlySpan<byte> utf8, JsonReaderOptions options);
}
