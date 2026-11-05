namespace uri;

/// <summary>
/// Identifies an RFC 6570 URI Template expression operator.
/// </summary>
public enum UriTemplateOperator
{
    /// <summary>Simple string expansion with no operator character.</summary>
    Simple = 0,

    /// <summary>Reserved expansion introduced by <c>+</c>.</summary>
    Reserved = 1,

    /// <summary>Fragment expansion introduced by <c>#</c>.</summary>
    Fragment = 2,

    /// <summary>Dot-prefixed label expansion introduced by <c>.</c>.</summary>
    Label = 3,

    /// <summary>Slash-prefixed path-segment expansion introduced by <c>/</c>.</summary>
    PathSegment = 4,

    /// <summary>Semicolon-prefixed path-parameter expansion introduced by <c>;</c>.</summary>
    PathParameter = 5,

    /// <summary>Question-mark-prefixed query expansion introduced by <c>?</c>.</summary>
    Query = 6,

    /// <summary>Ampersand-prefixed query continuation introduced by <c>&amp;</c>.</summary>
    QueryContinuation = 7
}
