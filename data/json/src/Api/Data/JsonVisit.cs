namespace json;

/// <summary>Contains the JSON value and pointer for one traversal visit.</summary>
public class JsonVisit
{
    /// <summary>The value being visited.</summary>
    public JsonValue? Value;

    /// <summary>The containing value, or <c>null</c> for the root visit.</summary>
    public JsonValue? Parent;

    /// <summary>The containing object member name, or <c>null</c> outside an object member.</summary>
    public string? MemberName;

    /// <summary>The containing array index, or -1 outside an array item.</summary>
    public int ArrayIndex = -1;

    /// <summary>The pointer identifying the value being visited.</summary>
    public JsonPointer? Pointer;

    /// <summary>The value's depth from the document root.</summary>
    public int Depth;

    /// <summary>The value's source range when source capture is enabled.</summary>
    public JsonSourceRange? Range;
}
