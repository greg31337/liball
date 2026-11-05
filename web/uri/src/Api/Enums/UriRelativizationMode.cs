namespace uri;

/// <summary>
/// Selects the preferred URI reference form produced during relativization.
/// </summary>
public enum UriRelativizationMode
{
    /// <summary>Prefer same-document, query, and relative-path forms before broader fallbacks.</summary>
    RelativePath = 0,

    /// <summary>Select the shortest verified reference text, retaining relative-form precedence for ties.</summary>
    Shortest = 1,

    /// <summary>Prefer an absolute-path reference, then broader fallbacks.</summary>
    AbsolutePath = 2,

    /// <summary>Prefer a network-path reference, then an absolute URI.</summary>
    NetworkPath = 3,

    /// <summary>Always produce an absolute URI reference.</summary>
    AbsoluteUri = 4
}
