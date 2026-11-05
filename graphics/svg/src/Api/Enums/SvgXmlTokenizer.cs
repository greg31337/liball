namespace svg;

/// <summary>Defines source-preserving XML lexical token kinds.</summary>
public enum SvgXmlTokenKind
{
    /// <summary>An XML declaration.</summary>
    Declaration = 0,
    /// <summary>A non-empty start-element tag.</summary>
    StartElement = 1,
    /// <summary>An end-element tag.</summary>
    EndElement = 2,
    /// <summary>An empty-element tag.</summary>
    EmptyElement = 3,
    /// <summary>Ordinary character data.</summary>
    Text = 4,
    /// <summary>A character or entity reference in character data.</summary>
    Reference = 5,
    /// <summary>A CDATA section.</summary>
    CData = 6,
    /// <summary>An XML comment.</summary>
    Comment = 7,
    /// <summary>A processing instruction other than the XML declaration.</summary>
    ProcessingInstruction = 8,
    /// <summary>A document type declaration.</summary>
    Doctype = 9
}

/// <summary>Defines stable XML lexical failures.</summary>
public enum SvgXmlTokenizeError
{
    /// <summary>No lexical failure occurred.</summary>
    None = 0,
    /// <summary>An argument or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The configured total token count was exceeded.</summary>
    TokenLimitExceeded = 2,
    /// <summary>One raw token exceeded its configured character count.</summary>
    TokenTooLong = 3,
    /// <summary>A markup construct ended before its required delimiter.</summary>
    UnexpectedEnd = 4,
    /// <summary>A markup opener or delimiter sequence was malformed.</summary>
    InvalidMarkup = 5,
    /// <summary>A qualified-name token was missing.</summary>
    ExpectedName = 6,
    /// <summary>An attribute assignment or quoted value was malformed.</summary>
    InvalidAttribute = 7,
    /// <summary>A character or entity reference lacked its terminating delimiter.</summary>
    InvalidReference = 8
}
