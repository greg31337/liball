namespace json;

/// <summary>Parses UTF-8 JSON into an owned document model.</summary>
public interface IJsonDocumentParser
{
    /// <summary>Parses one complete UTF-8 input.</summary>
    /// <param name="utf8">The complete UTF-8 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The parsed document.</returns>
    JsonDocument ParseUtf8(global::System.ReadOnlySpan<byte> utf8, JsonReaderOptions options);

    /// <summary>Attempts to parse one complete UTF-8 input without throwing for invalid JSON.</summary>
    /// <param name="utf8">The complete UTF-8 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The document parse outcome.</returns>
    JsonDocumentResult TryParseUtf8(global::System.ReadOnlySpan<byte> utf8, JsonReaderOptions options);

    /// <summary>Parses one complete UTF-16 string input.</summary>
    /// <param name="text">The complete UTF-16 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The parsed document.</returns>
    JsonDocument ParseString(string text, JsonReaderOptions options);

    /// <summary>Attempts to parse one complete UTF-16 string input without throwing for invalid JSON.</summary>
    /// <param name="text">The complete UTF-16 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The document parse outcome.</returns>
    JsonDocumentResult TryParseString(string text, JsonReaderOptions options);

    /// <summary>Parses JSON from a caller-owned stream.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The parsed document.</returns>
    JsonDocument ParseStream(global::System.IO.Stream stream, JsonReaderOptions options);

    /// <summary>Attempts to parse JSON from a caller-owned stream without throwing for invalid JSON.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The document parse outcome.</returns>
    JsonDocumentResult TryParseStream(global::System.IO.Stream stream, JsonReaderOptions options);
}
