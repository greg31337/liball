namespace iso20022;

/// <summary>Identifies the lexical encoding used for binary content.</summary>
public enum BinaryEncodingKind
{
    /// <summary>No encoding has been identified.</summary>
    Unknown = 0,
    /// <summary>Base64 encoding.</summary>
    Base64 = 1,
    /// <summary>Hexadecimal encoding.</summary>
    Hexadecimal = 2
}
