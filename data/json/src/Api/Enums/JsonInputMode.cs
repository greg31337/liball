namespace json;

/// <summary>Selects how JSON values are framed in an input or output stream.</summary>
public enum JsonInputMode
{
    /// <summary>Processes exactly one JSON value.</summary>
    SingleValue = 0,

    /// <summary>Processes JSON values separated by JSON whitespace.</summary>
    WhitespaceSeparated = 1,

    /// <summary>Processes an RFC 7464 JSON text sequence.</summary>
    JsonTextSequence = 2
}
