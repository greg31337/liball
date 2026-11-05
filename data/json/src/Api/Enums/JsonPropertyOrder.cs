namespace json;

/// <summary>Selects the ordering applied to object properties during writing.</summary>
public enum JsonPropertyOrder
{
    /// <summary>Preserves member order.</summary>
    Preserve = 0,

    /// <summary>Orders names by ordinal UTF-16 code units.</summary>
    OrdinalUtf16 = 1,

    /// <summary>Orders names by ordinal Unicode scalar values.</summary>
    OrdinalUnicodeScalar = 2,

    /// <summary>Uses the RFC 8785 unsigned UTF-16 ordering rule.</summary>
    Jcs = 3
}
