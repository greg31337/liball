namespace xml;

/// <summary>
/// Carries a mutable XML document and its document metadata.
/// </summary>
public class XmlDocument
{
    /// <summary>
    /// The document node.
    /// </summary>
    public XmlNode? DocumentNode;
    /// <summary>
    /// The document element.
    /// </summary>
    public XmlNode? DocumentElement;
    /// <summary>
    /// The retained document type definition.
    /// </summary>
    public XmlDocumentType? DocumentType;
    /// <summary>
    /// The XML version.
    /// </summary>
    public XmlVersion Version;
    /// <summary>
    /// The standalone declaration value.
    /// </summary>
    public XmlStandalone Standalone;
    /// <summary>
    /// The document URI.
    /// </summary>
    public string? DocumentUri;
    /// <summary>
    /// The document base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The source encoding name.
    /// </summary>
    public string? EncodingName;
    /// <summary>
    /// The diagnostics associated with the document.
    /// </summary>
    public XmlDiagnostic[]? Diagnostics;
    /// <summary>
    /// The current mutation version.
    /// </summary>
    public long MutationVersion;
    /// <summary>
    /// The next identity available for a node owned by this document.
    /// </summary>
    public long NextIdentity;
    /// <summary>
    /// Whether mutation of the document is prohibited.
    /// </summary>
    public bool IsReadOnly;
}
