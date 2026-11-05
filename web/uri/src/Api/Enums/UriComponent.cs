namespace uri;

/// <summary>
/// Identifies the input or output component associated with a diagnostic.
/// </summary>
public enum UriComponent
{
    /// <summary>No particular component.</summary>
    None = 0,

    /// <summary>The complete operation input.</summary>
    Input = 1,

    /// <summary>The URI or IRI scheme.</summary>
    Scheme = 2,

    /// <summary>The authority.</summary>
    Authority = 3,

    /// <summary>The authority user-information component.</summary>
    UserInfo = 4,

    /// <summary>The authority host component.</summary>
    Host = 5,

    /// <summary>The authority port component.</summary>
    Port = 6,

    /// <summary>The path.</summary>
    Path = 7,

    /// <summary>An individual path segment.</summary>
    PathSegment = 8,

    /// <summary>The query.</summary>
    Query = 9,

    /// <summary>The fragment.</summary>
    Fragment = 10,

    /// <summary>The operation output.</summary>
    Output = 11,

    /// <summary>An entire IRI reference.</summary>
    Iri = 12,

    /// <summary>An entire URI Template.</summary>
    Template = 13,

    /// <summary>A URI Template literal part.</summary>
    TemplateLiteral = 14,

    /// <summary>A URI Template expression.</summary>
    TemplateExpression = 15,

    /// <summary>A URI Template variable specification.</summary>
    TemplateVariable = 16,

    /// <summary>An entire URN.</summary>
    Urn = 17,

    /// <summary>A URN namespace identifier.</summary>
    UrnNamespaceIdentifier = 18,

    /// <summary>A URN namespace-specific string.</summary>
    UrnNamespaceSpecificString = 19,

    /// <summary>A URN r-component.</summary>
    UrnRComponent = 20,

    /// <summary>A URN q-component.</summary>
    UrnQComponent = 21,

    /// <summary>A URN f-component.</summary>
    UrnFComponent = 22,

    /// <summary>An entire Web Link field value.</summary>
    WebLink = 23,

    /// <summary>A Web Link context.</summary>
    LinkContext = 24,

    /// <summary>A Web Link target.</summary>
    LinkTarget = 25,

    /// <summary>A Web Link relation type.</summary>
    LinkRelation = 26,

    /// <summary>A Web Link parameter.</summary>
    LinkParameter = 27,

    /// <summary>An RFC 8187 extended parameter value.</summary>
    ExtendedParameter = 28,

    /// <summary>An RFC 5646 language tag.</summary>
    LanguageTag = 29
}
