namespace xml;

/// <summary>
/// Escapes and unescapes text for an explicit XML-related context.
/// </summary>
public interface IXmlEscaper
{
    /// <summary>
    /// Escapes a value for an output context.
    /// </summary>
    /// <param name="value">The input value.</param><param name="version">The XML version.</param><param name="context">The escape context.</param><param name="policy">The invalid character policy.</param><param name="quote">The active quote character, or a null character when not applicable.</param>
    /// <returns>The escaped value.</returns>
    string EscapeXmlValue(string value, XmlVersion version, XmlCharacterEscapeContext context, XmlInvalidCharacterPolicy policy, char quote);
    /// <summary>
    /// Resolves XML character and predefined entity references in a value.
    /// </summary>
    /// <param name="value">The escaped value.</param><param name="version">The XML version.</param><param name="context">The source context.</param>
    /// <returns>The unescaped value.</returns>
    string UnescapeXmlValue(string value, XmlVersion version, XmlCharacterEscapeContext context);
}

