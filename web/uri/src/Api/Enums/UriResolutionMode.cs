namespace uri;

/// <summary>
/// Selects strict or same-scheme-compatible URI reference resolution.
/// </summary>
public enum UriResolutionMode
{
    /// <summary>Apply RFC 3986 strict reference transformation.</summary>
    Strict = 0,

    /// <summary>Treat a reference scheme identical to the base scheme as absent.</summary>
    Compatible = 1
}
