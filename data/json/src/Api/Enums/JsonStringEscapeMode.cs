namespace json;

/// <summary>Selects how JSON string content is escaped.</summary>
public enum JsonStringEscapeMode
{
    /// <summary>Escapes only characters required by JSON.</summary>
    Minimal = 0,

    /// <summary>Escapes every non-ASCII character.</summary>
    AsciiOnly = 1,

    /// <summary>Additionally escapes characters significant in HTML content.</summary>
    HtmlSafe = 2
}
