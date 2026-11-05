namespace xml;

/// <summary>
/// Identifies the context in which characters are escaped.
/// </summary>
public enum XmlCharacterEscapeContext
{
    /// <summary>
    /// Escapes XML character data.
    /// </summary>
    Text = 0,
    /// <summary>
    /// Escapes a double-quoted XML attribute value.
    /// </summary>
    AttributeDoubleQuoted = 1,
    /// <summary>
    /// Escapes a single-quoted XML attribute value.
    /// </summary>
    AttributeSingleQuoted = 2,
    /// <summary>
    /// Validates or escapes comment content.
    /// </summary>
    Comment = 3,
    /// <summary>
    /// Validates or escapes processing instruction content.
    /// </summary>
    ProcessingInstruction = 4,
    /// <summary>
    /// Validates or splits CDATA content.
    /// </summary>
    CData = 5,
    /// <summary>
    /// Escapes a public identifier.
    /// </summary>
    PublicIdentifier = 6,
    /// <summary>
    /// Escapes a double-quoted system literal.
    /// </summary>
    SystemLiteralDoubleQuoted = 7,
    /// <summary>
    /// Escapes a single-quoted system literal.
    /// </summary>
    SystemLiteralSingleQuoted = 8,
    /// <summary>
    /// Escapes a double-quoted XPath string.
    /// </summary>
    XPathStringDoubleQuoted = 9,
    /// <summary>
    /// Escapes a single-quoted XPath string.
    /// </summary>
    XPathStringSingleQuoted = 10,
    /// <summary>
    /// Escapes a URI component.
    /// </summary>
    Uri = 11,
    /// <summary>
    /// Escapes a JSON string.
    /// </summary>
    Json = 12
}
