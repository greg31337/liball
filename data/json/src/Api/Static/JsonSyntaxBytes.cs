namespace json;

/// <summary>Provides UTF-8 byte values used by JSON syntax and framing.</summary>
public static class JsonSyntaxBytes
{
    /// <summary>The byte that begins a JSON object.</summary>
    public const byte BeginObject = 0x7B;

    /// <summary>The byte that ends a JSON object.</summary>
    public const byte EndObject = 0x7D;

    /// <summary>The byte that begins a JSON array.</summary>
    public const byte BeginArray = 0x5B;

    /// <summary>The byte that ends a JSON array.</summary>
    public const byte EndArray = 0x5D;

    /// <summary>The byte that separates an object name from its value.</summary>
    public const byte NameSeparator = 0x3A;

    /// <summary>The byte that separates values or object members.</summary>
    public const byte ValueSeparator = 0x2C;

    /// <summary>The byte that delimits a JSON string.</summary>
    public const byte StringDelimiter = 0x22;

    /// <summary>The byte that begins a JSON string escape.</summary>
    public const byte Escape = 0x5C;

    /// <summary>The JSON space whitespace byte.</summary>
    public const byte Space = 0x20;

    /// <summary>The JSON horizontal tab whitespace byte.</summary>
    public const byte HorizontalTab = 0x09;

    /// <summary>The JSON line feed whitespace byte.</summary>
    public const byte LineFeed = 0x0A;

    /// <summary>The JSON carriage return whitespace byte.</summary>
    public const byte CarriageReturn = 0x0D;

    /// <summary>The RFC 7464 record separator byte.</summary>
    public const byte RecordSeparator = 0x1E;
}
