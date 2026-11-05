namespace json;

/// <summary>Validates UTF-8 JSON without constructing a document model.</summary>
public interface IJsonUtf8Validator
{
    /// <summary>Validates one UTF-8 input using the supplied policies.</summary>
    /// <param name="utf8">The complete UTF-8 input.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The validation outcome.</returns>
    JsonOperationResult ValidateUtf8(global::System.ReadOnlySpan<byte> utf8, JsonReaderOptions options);

    /// <summary>Validates complete UTF-8 JSON from a caller-owned stream.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <param name="options">The input policies to apply.</param>
    /// <returns>The validation outcome.</returns>
    JsonOperationResult ValidateStream(global::System.IO.Stream stream, JsonReaderOptions options);
}
