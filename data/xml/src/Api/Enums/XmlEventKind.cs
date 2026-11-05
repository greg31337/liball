namespace xml;

/// <summary>
/// Identifies a normalized XML processing event.
/// </summary>
public enum XmlEventKind
{
    /// <summary>
    /// Starts a document.
    /// </summary>
    StartDocument = 0,
    /// <summary>
    /// Ends a document.
    /// </summary>
    EndDocument = 1,
    /// <summary>
    /// Starts an entity.
    /// </summary>
    StartEntity = 2,
    /// <summary>
    /// Ends an entity.
    /// </summary>
    EndEntity = 3,
    /// <summary>
    /// Starts a document type definition.
    /// </summary>
    StartDtd = 4,
    /// <summary>
    /// Ends a document type definition.
    /// </summary>
    EndDtd = 5,
    /// <summary>
    /// Starts a namespace mapping.
    /// </summary>
    StartNamespace = 6,
    /// <summary>
    /// Ends a namespace mapping.
    /// </summary>
    EndNamespace = 7,
    /// <summary>
    /// Starts an element.
    /// </summary>
    StartElement = 8,
    /// <summary>
    /// Ends an element.
    /// </summary>
    EndElement = 9,
    /// <summary>
    /// Reports character data.
    /// </summary>
    Text = 10,
    /// <summary>
    /// Reports ignorable whitespace.
    /// </summary>
    IgnorableWhitespace = 11,
    /// <summary>
    /// Reports a CDATA section.
    /// </summary>
    CData = 12,
    /// <summary>
    /// Reports a comment.
    /// </summary>
    Comment = 13,
    /// <summary>
    /// Reports a processing instruction.
    /// </summary>
    ProcessingInstruction = 14,
    /// <summary>
    /// Reports an element declaration.
    /// </summary>
    ElementDeclaration = 15,
    /// <summary>
    /// Reports an attribute declaration.
    /// </summary>
    AttributeDeclaration = 16,
    /// <summary>
    /// Reports an entity declaration.
    /// </summary>
    EntityDeclaration = 17,
    /// <summary>
    /// Reports a notation declaration.
    /// </summary>
    NotationDeclaration = 18,
    /// <summary>
    /// Reports a skipped entity.
    /// </summary>
    SkippedEntity = 19,
    /// <summary>
    /// Reports a diagnostic.
    /// </summary>
    Diagnostic = 20
}
