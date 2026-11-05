namespace xml;

/// <summary>
/// Represents a dynamic XPath error.
/// </summary>
public class XPathDynamicException : System.Exception
{
    internal XmlName? ErrorName;
    internal XdmSequence? ErrorValue;

    /// <summary>
    /// The structured diagnostic associated with the failure.
    /// </summary>
    public XmlDiagnostic? Diagnostic;
    /// <summary>
    /// The primary source location associated with the failure.
    /// </summary>
    public SourceLocation? Location;

    /// <summary>
    /// Creates an exception with a message and optional processing context.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="diagnostic">The structured diagnostic, or null when unavailable.</param>
    /// <param name="location">The primary source location, or null when unavailable.</param>
    public XPathDynamicException(string message, XmlDiagnostic? diagnostic, SourceLocation? location) : base(message)
    {
        Diagnostic = diagnostic;
        Location = location;
        ErrorName = null;
        ErrorValue = null;
    }
}
