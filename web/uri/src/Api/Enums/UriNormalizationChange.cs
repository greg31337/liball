namespace uri;

/// <summary>
/// Identifies an individual transformation found by URI normalization analysis.
/// </summary>
public enum UriNormalizationChange
{
    /// <summary>No transformation is required.</summary>
    None = 0,

    /// <summary>The scheme contains uppercase ASCII letters.</summary>
    SchemeCase = 1,

    /// <summary>The user-information component requires percent normalization.</summary>
    UserInfoPercentEncoding = 2,

    /// <summary>The registered-name host requires ASCII case normalization.</summary>
    HostCase = 3,

    /// <summary>The registered-name host requires percent normalization.</summary>
    HostPercentEncoding = 4,

    /// <summary>An IP address host requires canonical serialization.</summary>
    HostAddress = 5,

    /// <summary>The path requires percent normalization.</summary>
    PathPercentEncoding = 6,

    /// <summary>The path contains removable dot segments.</summary>
    PathDotSegments = 7,

    /// <summary>The query component requires percent normalization.</summary>
    QueryPercentEncoding = 8,

    /// <summary>The fragment component requires percent normalization.</summary>
    FragmentPercentEncoding = 9,

    /// <summary>A scheme profile changes an empty authority path to <c>/</c>.</summary>
    AuthorityPath = 10,

    /// <summary>A scheme profile removes a present empty port.</summary>
    EmptyPort = 11,

    /// <summary>A scheme profile removes a declared default port.</summary>
    DefaultPort = 12,

    /// <summary>A scheme profile removes a present empty query.</summary>
    EmptyQuery = 13,

    /// <summary>A scheme profile removes a present empty fragment.</summary>
    EmptyFragment = 14
}
