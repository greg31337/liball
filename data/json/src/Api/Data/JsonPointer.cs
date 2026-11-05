namespace json;

/// <summary>Contains a decoded JSON Pointer and its source representation kind.</summary>
public class JsonPointer
{
    /// <summary>The representation used to parse or format the pointer.</summary>
    public JsonPointerEncoding Encoding;

    /// <summary>The original string or URI-fragment representation.</summary>
    public string? OriginalRepresentation;

    /// <summary>The decoded reference tokens in evaluation order.</summary>
    public global::System.Collections.Generic.List<string>? ReferenceTokens;
}
