namespace json;

/// <summary>Parses and formats RFC 6901 JSON Pointer representations.</summary>
public interface IJsonPointerCodec
{
    /// <summary>Parses a JSON Pointer string representation.</summary>
    /// <param name="representation">The string representation.</param>
    /// <returns>The decoded pointer.</returns>
    JsonPointer ParseStringPointer(string representation);

    /// <summary>Formats a pointer as a JSON Pointer string representation.</summary>
    /// <param name="pointer">The decoded pointer.</param>
    /// <returns>The formatted string representation.</returns>
    string FormatStringPointer(JsonPointer pointer);

    /// <summary>Parses a JSON Pointer URI fragment representation.</summary>
    /// <param name="fragment">The URI fragment representation.</param>
    /// <returns>The decoded pointer.</returns>
    JsonPointer ParseUriFragment(string fragment);

    /// <summary>Formats a pointer as a JSON Pointer URI fragment representation.</summary>
    /// <param name="pointer">The decoded pointer.</param>
    /// <returns>The formatted URI fragment representation.</returns>
    string FormatUriFragment(JsonPointer pointer);
}
