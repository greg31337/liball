namespace odf;

/// <summary>
/// Describes a streaming reader event.
/// </summary>
public class OdfStreamEvent
{
    /// <summary>
    /// The event kind.
    /// </summary>
    public OdfStreamEventKind Kind;
    /// <summary>
    /// The package-relative path associated with the event, or an empty string.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The package entry associated with a package boundary event.
    /// </summary>
    public OdfPackageEntry? PackageEntry;
    /// <summary>
    /// The selected binary entry content carried by an entry-end event.
    /// </summary>
    public byte[] BinaryContent = [];
    /// <summary>
    /// Whether the current package entry was explicitly skipped.
    /// </summary>
    public bool WasSkipped;
    /// <summary>
    /// The source location associated with the event.
    /// </summary>
    public OdfLocation Location = new OdfLocation();
    /// <summary>
    /// The qualified element or processing-instruction name, when applicable.
    /// </summary>
    public OdfQualifiedName? Name;
    /// <summary>
    /// Namespace declarations introduced by an element-start event.
    /// </summary>
    public OdfNamespaceDeclaration[] NamespaceDeclarations = [];
    /// <summary>
    /// The namespaces in scope at an XML event.
    /// </summary>
    public OdfNamespaceDeclaration[] NamespaceScope = [];
    /// <summary>
    /// The non-namespace attributes on an element-start event.
    /// </summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>
    /// The text, comment, or processing-instruction value.
    /// </summary>
    public string Value = "";
    /// <summary>
    /// The zero-based XML element depth associated with the event.
    /// </summary>
    public int Depth;
    /// <summary>
    /// Whether an element-start event used empty-element syntax.
    /// </summary>
    public bool IsEmptyElement;
    /// <summary>
    /// Whether a text event contains whitespace preserved by XML space rules.
    /// </summary>
    public bool IsSignificantWhitespace;
}
