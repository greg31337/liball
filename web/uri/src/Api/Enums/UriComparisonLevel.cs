namespace uri;

/// <summary>
/// Identifies the equivalence level used to compare URI or IRI references.
/// </summary>
public enum UriComparisonLevel
{
    /// <summary>Compare serialized component text and delimiter presence exactly.</summary>
    Exact = 0,

    /// <summary>Compare after generic RFC syntax normalization.</summary>
    Syntax = 1,

    /// <summary>Compare after generic and scheme-profile normalization.</summary>
    SchemeProfile = 2,

    /// <summary>Compare IRI components after Unicode Normalization Form C.</summary>
    IriUnicode = 3,

    /// <summary>Compare IRIs after mapping to and syntax-normalizing URIs.</summary>
    IriMappedUri = 4
}
