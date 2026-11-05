namespace json;

/// <summary>Contains the tagged payload of one JSON value.</summary>
public class JsonValue
{
    /// <summary>The kind of JSON payload stored by this value.</summary>
    public JsonValueKind Kind;

    /// <summary>The boolean payload when <see cref="Kind"/> is <see cref="JsonValueKind.Boolean"/>.</summary>
    public bool BooleanValue;

    /// <summary>The exact number payload when <see cref="Kind"/> is <see cref="JsonValueKind.Number"/>.</summary>
    public JsonNumber? Number;

    /// <summary>The decoded string payload when <see cref="Kind"/> is <see cref="JsonValueKind.String"/>.</summary>
    public string? StringValue;

    /// <summary>The ordered items when <see cref="Kind"/> is <see cref="JsonValueKind.Array"/>.</summary>
    public global::System.Collections.Generic.List<JsonValue>? ArrayItems;

    /// <summary>The ordered, duplicate-preserving members when <see cref="Kind"/> is <see cref="JsonValueKind.Object"/>.</summary>
    public global::System.Collections.Generic.List<JsonMember>? ObjectMembers;

    /// <summary>The source range of the value when source capture is enabled.</summary>
    public JsonSourceRange? Range;
}
