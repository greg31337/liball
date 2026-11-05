namespace xml;

/// <summary>
/// Receives XML, XPath, schema, and XSLT diagnostics.
/// </summary>
public interface IXmlErrorReporter
{
    /// <summary>
    /// Reports one diagnostic.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    void ReportXmlDiagnostic(XmlDiagnostic diagnostic);
}

