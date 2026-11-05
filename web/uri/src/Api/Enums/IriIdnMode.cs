namespace uri;

/// <summary>
/// Selects whether IRI registered names receive DNS-oriented platform IDN mapping.
/// </summary>
public enum IriIdnMode
{
    /// <summary>Do not apply DNS-oriented IDN mapping.</summary>
    None = 0,

    /// <summary>Use the current .NET platform IDN implementation after prevalidation.</summary>
    Platform = 1
}
