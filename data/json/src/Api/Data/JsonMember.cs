namespace json;

/// <summary>Contains one decoded, ordered JSON object member.</summary>
public class JsonMember
{
    /// <summary>The decoded object member name.</summary>
    public string? Name;

    /// <summary>The member value.</summary>
    public JsonValue? Value;

    /// <summary>The source range of the encoded member name.</summary>
    public JsonSourceRange? NameRange;

    /// <summary>The source range of the complete member.</summary>
    public JsonSourceRange? MemberRange;

    /// <summary>The zero-based source ordinal, or -1 when no source ordinal is available.</summary>
    public int SourceOrdinal = -1;
}
