namespace json;

/// <summary>Identifies the payload stored by a JSON value.</summary>
public enum JsonValueKind
{
    /// <summary>Indicates that a mutable value has not been initialized.</summary>
    Uninitialized = 0,

    /// <summary>Identifies the JSON null value.</summary>
    Null = 1,

    /// <summary>Identifies a JSON boolean value.</summary>
    Boolean = 2,

    /// <summary>Identifies a JSON number value.</summary>
    Number = 3,

    /// <summary>Identifies a JSON string value.</summary>
    String = 4,

    /// <summary>Identifies a JSON array value.</summary>
    Array = 5,

    /// <summary>Identifies a JSON object value.</summary>
    Object = 6
}
