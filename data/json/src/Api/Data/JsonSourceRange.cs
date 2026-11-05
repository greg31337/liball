namespace json;

/// <summary>Identifies an inclusive start and exclusive end position in JSON input.</summary>
public class JsonSourceRange
{
    /// <summary>The inclusive start position.</summary>
    public JsonSourcePosition? Start;

    /// <summary>The exclusive end position.</summary>
    public JsonSourcePosition? End;
}
