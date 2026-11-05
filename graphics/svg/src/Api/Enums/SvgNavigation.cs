namespace svg;

/// <summary>Identifies the semantic result of activating SVG link content.</summary>
public enum SvgActivationKind
{
    /// <summary>No activation was produced.</summary>
    None = 0,
    /// <summary>An external or resource navigation request was produced.</summary>
    Link = 1,
    /// <summary>A same-document fragment activation was produced.</summary>
    Fragment = 2
}

/// <summary>Identifies the host action represented by a navigation request.</summary>
public enum SvgNavigationRequestKind
{
    /// <summary>No host action is requested.</summary>
    None = 0,
    /// <summary>The host may activate a same-document fragment.</summary>
    SameDocumentFragment = 1,
    /// <summary>The host may navigate to a URI.</summary>
    Navigate = 2
}

/// <summary>Defines stable focus-order and activation failures.</summary>
public enum SvgNavigationError
{
    /// <summary>No navigation failure occurred.</summary>
    None = 0,
    /// <summary>The document, node, hit, option, or mutable state was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>No link ancestor with a usable reference was available.</summary>
    NotActivatable = 2,
    /// <summary>The selected link metadata was invalid.</summary>
    InvalidLink = 3,
    /// <summary>The selected link URI could not be normalized.</summary>
    InvalidUri = 4,
    /// <summary>A same-document fragment target was not found.</summary>
    FragmentNotFound = 5,
    /// <summary>The configured resolved-node limit was exceeded.</summary>
    LimitExceeded = 6
}
