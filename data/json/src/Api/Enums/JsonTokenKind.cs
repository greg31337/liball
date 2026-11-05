namespace json;

/// <summary>Identifies a token produced while reading JSON.</summary>
public enum JsonTokenKind
{
    /// <summary>Indicates that no token is available.</summary>
    None = 0,

    /// <summary>Identifies the start of an object.</summary>
    StartObject = 1,

    /// <summary>Identifies the end of an object.</summary>
    EndObject = 2,

    /// <summary>Identifies the start of an array.</summary>
    StartArray = 3,

    /// <summary>Identifies the end of an array.</summary>
    EndArray = 4,

    /// <summary>Identifies an object property name.</summary>
    PropertyName = 5,

    /// <summary>Identifies the JSON null token.</summary>
    Null = 6,

    /// <summary>Identifies a JSON boolean token.</summary>
    Boolean = 7,

    /// <summary>Identifies a JSON number token.</summary>
    Number = 8,

    /// <summary>Identifies a JSON string token.</summary>
    String = 9,

    /// <summary>Identifies a boundary between framed JSON documents.</summary>
    DocumentBoundary = 10
}
