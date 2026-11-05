namespace svg;

/// <summary>Defines stable XML-to-document construction failures.</summary>
public enum SvgXmlBuildError
{
    /// <summary>No construction failure occurred.</summary>
    None = 0,
    /// <summary>An argument or prerequisite token field was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A document type declaration was prohibited by policy.</summary>
    DtdProhibited = 2,
    /// <summary>The configured element nesting depth was exceeded.</summary>
    DepthLimitExceeded = 3,
    /// <summary>The configured total node count was exceeded.</summary>
    NodeLimitExceeded = 4,
    /// <summary>The configured per-element attribute count was exceeded.</summary>
    ElementAttributeLimitExceeded = 5,
    /// <summary>The configured total attribute count was exceeded.</summary>
    TotalAttributeLimitExceeded = 6,
    /// <summary>The configured cumulative text character count was exceeded.</summary>
    TextLimitExceeded = 7,
    /// <summary>An end tag had no open element.</summary>
    UnexpectedEndElement = 8,
    /// <summary>An end tag did not match the current open element.</summary>
    MismatchedEndElement = 9,
    /// <summary>An element remained open at the end of the source.</summary>
    UnclosedElement = 10,
    /// <summary>The source contained more than one document element.</summary>
    MultipleDocumentElements = 11,
    /// <summary>Non-whitespace character content occurred outside the document element.</summary>
    CharacterDataOutsideElement = 12,
    /// <summary>The source contained more than one document type declaration.</summary>
    MultipleDoctypes = 13,
    /// <summary>A document type declaration occurred after the document element.</summary>
    MisplacedDoctype = 14,
    /// <summary>No document element was constructed.</summary>
    MissingDocumentElement = 15,
    /// <summary>An unresolved reader entity was encountered.</summary>
    UnresolvedEntity = 16,
    /// <summary>Construction was cancelled.</summary>
    Cancelled = 17,
    /// <summary>An XML reader name exceeded the configured character count.</summary>
    NameLimitExceeded = 18
}
