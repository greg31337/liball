namespace svg;

/// <summary>Defines stable XML 1.0 well-formedness validation failures.</summary>
public enum SvgXmlValidationError
{
    /// <summary>No validation failure occurred.</summary>
    None = 0,
    /// <summary>An argument or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The source contained a character forbidden by XML 1.0.</summary>
    InvalidCharacter = 2,
    /// <summary>An XML name did not follow the XML 1.0 Name production.</summary>
    InvalidName = 3,
    /// <summary>An XML name exceeded the configured character bound.</summary>
    NameTooLong = 4,
    /// <summary>A character or entity reference was invalid or unsupported.</summary>
    InvalidReference = 5,
    /// <summary>An XML comment contained a forbidden double hyphen.</summary>
    InvalidComment = 6,
    /// <summary>A processing-instruction target used the reserved XML name.</summary>
    InvalidProcessingInstruction = 7,
    /// <summary>The XML declaration pseudo-attributes were malformed.</summary>
    InvalidDeclaration = 8,
    /// <summary>Character data contained the forbidden CDATA closing sequence.</summary>
    InvalidCharacterData = 9,
    /// <summary>A document type declaration lacked a valid root name.</summary>
    InvalidDoctype = 10
}
