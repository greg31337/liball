namespace xml;

/// <summary>
/// Carries the current state exposed by an XML pull reader.
/// </summary>
public class XmlReaderState
{
    /// <summary>
    /// The reader status.
    /// </summary>
    public XmlParserStatus Status;
    /// <summary>
    /// The current event kind.
    /// </summary>
    public XmlEventKind Kind;
    /// <summary>
    /// The current node kind.
    /// </summary>
    public XmlNodeKind NodeKind;
    /// <summary>
    /// The current name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The current value.
    /// </summary>
    public string? Value;
    /// <summary>
    /// The current ordered attributes.
    /// </summary>
    public XmlAttributeData[]? Attributes;
    /// <summary>
    /// The current namespace bindings.
    /// </summary>
    public XmlNamespaceBinding[]? NamespaceBindings;
    /// <summary>
    /// The current zero-based element depth.
    /// </summary>
    public int Depth;
    /// <summary>
    /// The effective base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The effective XML language.
    /// </summary>
    public string? Language;
    /// <summary>
    /// The effective XML whitespace mode.
    /// </summary>
    public XmlSpaceMode SpaceMode;
    /// <summary>
    /// Whether the current element used empty-element syntax.
    /// </summary>
    public bool IsEmptyElement;
    /// <summary>
    /// Whether the current text event is ignorable whitespace.
    /// </summary>
    public bool IsIgnorableWhitespace;
    /// <summary>
    /// The current source range.
    /// </summary>
    public SourceRange? Range;
    /// <summary>
    /// The current diagnostic.
    /// </summary>
    public XmlDiagnostic? Diagnostic;
    /// <summary>
    /// The current element declaration payload.
    /// </summary>
    public XmlElementDeclaration? ElementDeclaration;
    /// <summary>
    /// The current attribute declaration payload.
    /// </summary>
    public XmlAttributeDeclaration? AttributeDeclaration;
    /// <summary>
    /// The current entity declaration payload.
    /// </summary>
    public XmlEntityDeclaration? EntityDeclaration;
    /// <summary>
    /// The current notation declaration payload.
    /// </summary>
    public XmlNotationDeclaration? NotationDeclaration;
    /// <summary>
    /// The current public identifier.
    /// </summary>
    public string? PublicIdentifier;
    /// <summary>
    /// The current system identifier.
    /// </summary>
    public string? SystemIdentifier;
    /// <summary>
    /// The parser options associated with the reader.
    /// </summary>
    public XmlParserOptions? Options;
}
