namespace json;

/// <summary>Selects the JSON conformance rules applied by an operation.</summary>
public enum JsonConformanceProfile
{
    /// <summary>Accepts escaped lone UTF-16 surrogates while enforcing JSON syntax.</summary>
    SyntaxCompatible = 0,

    /// <summary>Applies the portable Unicode-scalar and unique-name preset.</summary>
    Portable = 1,

    /// <summary>Applies the RFC 7493 I-JSON profile.</summary>
    IJson = 2,

    /// <summary>Applies the RFC 8785 JSON Canonicalization Scheme profile.</summary>
    Jcs = 3
}
