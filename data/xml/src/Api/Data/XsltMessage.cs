namespace xml;

/// <summary>
/// Carries one xsl:message or assertion message.
/// </summary>
public class XsltMessage
{
    /// <summary>
    /// The message text.
    /// </summary>
    public string? Text;
    /// <summary>
    /// The message error code.
    /// </summary>
    public XmlName? Code;
    /// <summary>
    /// Whether the message requests termination.
    /// </summary>
    public bool Terminate;
    /// <summary>
    /// The stylesheet source range.
    /// </summary>
    public SourceRange? Range;
    /// <summary>
    /// The message content sequence.
    /// </summary>
    public XdmSequence? Content;
}

