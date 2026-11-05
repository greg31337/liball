namespace uri;

/// <summary>
/// Classifies a parsed reference by the RFC 3986 production and reference form it matched.
/// </summary>
public enum UriReferenceKind
{
    /// <summary>A URI containing a scheme and permitting a fragment.</summary>
    Uri = 0,

    /// <summary>An absolute-URI containing a scheme and no fragment.</summary>
    AbsoluteUri = 1,

    /// <summary>A relative-ref not covered by a more specific reference form.</summary>
    RelativeReference = 2,

    /// <summary>A relative reference beginning with an authority delimiter.</summary>
    NetworkPathReference = 3,

    /// <summary>A relative reference beginning with one slash and no authority.</summary>
    AbsolutePathReference = 4,

    /// <summary>A relative reference beginning with a non-colon path segment.</summary>
    RelativePathReference = 5,

    /// <summary>An empty reference.</summary>
    EmptyReference = 6,

    /// <summary>A fragment-only same-document reference.</summary>
    SameDocumentReference = 7
}
