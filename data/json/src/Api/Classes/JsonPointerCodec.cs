namespace json;

/// <summary>Parses and formats RFC 6901 JSON Pointer representations.</summary>
public class JsonPointerCodec : IJsonPointerCodec
{
    /// <summary>Initializes a reusable JSON Pointer codec.</summary>
    public JsonPointerCodec()
    {
    }

    /// <summary>Parses a JSON Pointer string representation.</summary>
    /// <param name="representation">The string representation.</param>
    /// <returns>The decoded pointer.</returns>
    public JsonPointer ParseStringPointer(string representation)
    {
        return JsonPointerCodecCore.ParseStringRepresentation(representation);
    }

    /// <summary>Formats a pointer as a JSON Pointer string representation.</summary>
    /// <param name="pointer">The decoded pointer.</param>
    /// <returns>The formatted string representation.</returns>
    public string FormatStringPointer(JsonPointer pointer)
    {
        return JsonPointerCodecCore.FormatStringRepresentation(pointer);
    }

    /// <summary>Parses a JSON Pointer URI fragment representation.</summary>
    /// <param name="fragment">The URI fragment representation.</param>
    /// <returns>The decoded pointer.</returns>
    public JsonPointer ParseUriFragment(string fragment)
    {
        return JsonPointerCodecCore.ParseFragmentRepresentation(fragment);
    }

    /// <summary>Formats a pointer as a JSON Pointer URI fragment representation.</summary>
    /// <param name="pointer">The decoded pointer.</param>
    /// <returns>The formatted URI fragment representation.</returns>
    public string FormatUriFragment(JsonPointer pointer)
    {
        return JsonPointerCodecCore.FormatFragmentRepresentation(pointer);
    }
}
