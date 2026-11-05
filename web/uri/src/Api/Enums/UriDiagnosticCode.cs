namespace uri;

/// <summary>
/// Identifies a stable, programmatically testable diagnostic condition.
/// </summary>
public enum UriDiagnosticCode
{
    /// <summary>No diagnostic condition.</summary>
    None = 0,

    /// <summary>The supplied input is not valid for the requested operation.</summary>
    InvalidInput = 1,

    /// <summary>A character is not allowed in its component.</summary>
    InvalidCharacter = 2,

    /// <summary>The input ended before a required grammar element was complete.</summary>
    UnexpectedEnd = 3,

    /// <summary>A percent triplet is incomplete or contains a non-hexadecimal digit.</summary>
    InvalidPercentEncoding = 4,

    /// <summary>UTF-16 text contains an invalid Unicode scalar representation.</summary>
    InvalidUnicode = 5,

    /// <summary>An octet sequence is not valid strict UTF-8.</summary>
    InvalidUtf8 = 6,

    /// <summary>An input, collection, nesting, component, or output limit was exceeded.</summary>
    LimitExceeded = 7,

    /// <summary>A component required by the selected operation is absent.</summary>
    MissingRequiredComponent = 8,

    /// <summary>A caller-created component combination is invalid.</summary>
    InvalidComponent = 9,

    /// <summary>The scheme is invalid.</summary>
    InvalidScheme = 10,

    /// <summary>The authority is invalid.</summary>
    InvalidAuthority = 11,

    /// <summary>The user information is invalid.</summary>
    InvalidUserInfo = 12,

    /// <summary>The host is invalid.</summary>
    InvalidHost = 13,

    /// <summary>The port is invalid.</summary>
    InvalidPort = 14,

    /// <summary>The path is invalid.</summary>
    InvalidPath = 15,

    /// <summary>The query is invalid.</summary>
    InvalidQuery = 16,

    /// <summary>The fragment is invalid.</summary>
    InvalidFragment = 17,

    /// <summary>An IPv4 address is invalid.</summary>
    InvalidIpv4Address = 18,

    /// <summary>An IPv6 address is invalid.</summary>
    InvalidIpv6Address = 19,

    /// <summary>An IPvFuture address is invalid.</summary>
    InvalidIpvFutureAddress = 20,

    /// <summary>An IP literal contains a zone identifier forbidden by the selected URI grammar.</summary>
    ZoneIdentifierNotAllowed = 21,

    /// <summary>The IRI is invalid.</summary>
    InvalidIri = 22,

    /// <summary>An internationalized domain-name label is invalid.</summary>
    InvalidIdnLabel = 23,

    /// <summary>Internationalized text violates a bidirectional-text requirement.</summary>
    InvalidBidiText = 24,

    /// <summary>Text does not have the requested Unicode normalization form.</summary>
    NormalizationRequired = 25,

    /// <summary>The URI Template is invalid.</summary>
    InvalidTemplate = 26,

    /// <summary>A URI Template expression is invalid.</summary>
    InvalidTemplateExpression = 27,

    /// <summary>A URI Template variable name is invalid.</summary>
    InvalidTemplateVariable = 28,

    /// <summary>A URI Template variable modifier is invalid.</summary>
    InvalidTemplateModifier = 29,

    /// <summary>A URI Template expression uses a reserved future operator.</summary>
    UnsupportedTemplateOperator = 30,

    /// <summary>The URN is invalid.</summary>
    InvalidUrn = 31,

    /// <summary>The URN namespace identifier is invalid.</summary>
    InvalidNamespaceIdentifier = 32,

    /// <summary>The URN namespace-specific string is invalid.</summary>
    InvalidNamespaceSpecificString = 33,

    /// <summary>URN optional components occur in an invalid order.</summary>
    InvalidUrnComponentOrder = 34,

    /// <summary>No namespace handler supports the requested namespace-specific operation.</summary>
    UnsupportedNamespace = 35,

    /// <summary>The Web Link field value is invalid.</summary>
    InvalidWebLink = 36,

    /// <summary>A Web Link target is invalid.</summary>
    InvalidLinkTarget = 37,

    /// <summary>A Web Link parameter is invalid.</summary>
    InvalidLinkParameter = 38,

    /// <summary>A Web Link relation type is invalid.</summary>
    InvalidRelationType = 39,

    /// <summary>An RFC 8187 extended parameter value is invalid.</summary>
    InvalidExtendedParameter = 40,

    /// <summary>An extended parameter uses an unsupported character set.</summary>
    UnsupportedCharacterSet = 41,

    /// <summary>A language tag is invalid.</summary>
    InvalidLanguageTag = 42,

    /// <summary>A repeated singleton parameter was ignored.</summary>
    DuplicateParameter = 43,

    /// <summary>A reference cannot be resolved under the selected policy.</summary>
    UnresolvableReference = 44,

    /// <summary>A Web Link anchor cannot be honored with the supplied context.</summary>
    UnhonoredAnchor = 45,

    /// <summary>Two or more selected options are incompatible.</summary>
    IncompatibleOptions = 46
}
