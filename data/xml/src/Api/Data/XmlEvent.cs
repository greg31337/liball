namespace xml;

/// <summary>
/// Carries one normalized XML push or pull event.
/// </summary>
public class XmlEvent
{
    /// <summary>
    /// The event kind.
    /// </summary>
    public XmlEventKind Kind;
    /// <summary>
    /// The related tree node kind.
    /// </summary>
    public XmlNodeKind NodeKind;
    /// <summary>
    /// The event name.
    /// </summary>
    public XmlName? Name;
    /// <summary>
    /// The event value.
    /// </summary>
    public string? Value;
    /// <summary>
    /// The ordered attributes for a start-element event.
    /// </summary>
    public XmlAttributeData[]? Attributes;
    /// <summary>
    /// The namespace bindings associated with the event.
    /// </summary>
    public XmlNamespaceBinding[]? NamespaceBindings;
    /// <summary>
    /// The zero-based element depth.
    /// </summary>
    public int Depth;
    /// <summary>
    /// Whether the event represents lexical empty-element syntax.
    /// </summary>
    public bool IsEmptyElement;
    /// <summary>
    /// Whether text is ignorable whitespace.
    /// </summary>
    public bool IsIgnorableWhitespace;
    /// <summary>
    /// The source range.
    /// </summary>
    public SourceRange? Range;
    /// <summary>
    /// The diagnostic for a diagnostic event.
    /// </summary>
    public XmlDiagnostic? Diagnostic;
    /// <summary>
    /// The element declaration for an element-declaration event.
    /// </summary>
    public XmlElementDeclaration? ElementDeclaration;
    /// <summary>
    /// The attribute declaration for an attribute-declaration event.
    /// </summary>
    public XmlAttributeDeclaration? AttributeDeclaration;
    /// <summary>
    /// The entity declaration for an entity-declaration event.
    /// </summary>
    public XmlEntityDeclaration? EntityDeclaration;
    /// <summary>
    /// The notation declaration for a notation-declaration event.
    /// </summary>
    public XmlNotationDeclaration? NotationDeclaration;
    /// <summary>
    /// The related public identifier.
    /// </summary>
    public string? PublicIdentifier;
    /// <summary>
    /// The related system identifier.
    /// </summary>
    public string? SystemIdentifier;
}
