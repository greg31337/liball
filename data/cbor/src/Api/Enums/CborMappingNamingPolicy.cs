namespace cbor;

/// <summary>Selects how reflected member names become CBOR map keys.</summary>
public enum CborMappingNamingPolicy
{
    /// <summary>Uses the runtime member name without modification.</summary>
    Exact = 0,
    /// <summary>Converts the first member-name character to lower case.</summary>
    CamelCase = 1
}
