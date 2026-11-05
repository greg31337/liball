namespace uri;

/// <summary>
/// Selects the ASCII characters that percent encoding may preserve literally.
/// </summary>
public enum PercentEncodeSet
{
    /// <summary>Preserves only RFC 3986 unreserved characters.</summary>
    Unreserved = 0,

    /// <summary>Preserves characters allowed in URI user information and mapped IRI user information.</summary>
    UserInfo = 1,

    /// <summary>Preserves characters allowed in a URI registered name and mapped IRI registered name.</summary>
    RegisteredName = 2,

    /// <summary>Preserves characters allowed in one URI or mapped IRI path segment.</summary>
    PathSegment = 3,

    /// <summary>Preserves characters allowed in a URI or mapped IRI path.</summary>
    Path = 4,

    /// <summary>Preserves characters allowed in a URI or mapped IRI query.</summary>
    Query = 5,

    /// <summary>Preserves characters allowed in a URI or mapped IRI fragment.</summary>
    Fragment = 6,

    /// <summary>Preserves characters allowed in a URN namespace-specific string.</summary>
    UrnNamespaceSpecificString = 7,

    /// <summary>Preserves characters allowed in a URN r-component.</summary>
    UrnRComponent = 8,

    /// <summary>Preserves characters allowed in a URN q-component.</summary>
    UrnQComponent = 9,

    /// <summary>Preserves characters allowed by URI Template simple expansion.</summary>
    UriTemplateSimpleExpansion = 10,

    /// <summary>Preserves characters allowed by URI Template reserved expansion.</summary>
    UriTemplateReservedExpansion = 11,

    /// <summary>Preserves RFC 8187 attr-char characters in a Web Link extended value.</summary>
    WebLinkExtendedValue = 12
}
