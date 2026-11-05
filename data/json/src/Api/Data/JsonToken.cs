namespace json;

/// <summary>Contains one token published by a JSON reader.</summary>
public class JsonToken
{
    /// <summary>The token category.</summary>
    public JsonTokenKind Kind;

    /// <summary>The decoded string or property-name payload.</summary>
    public string? StringValue;

    /// <summary>The exact number payload for a number token.</summary>
    public JsonNumber? Number;

    /// <summary>The boolean payload for a boolean token.</summary>
    public bool BooleanValue;

    /// <summary>An owned copy of the raw UTF-8 token when raw capture is enabled.</summary>
    public byte[]? RawUtf8;

    /// <summary>The container depth at which the token begins.</summary>
    public int Depth;

    /// <summary>The pointer identifying the token's value or property.</summary>
    public JsonPointer? Pointer;

    /// <summary>The token's source range.</summary>
    public JsonSourceRange? Range;
}
