using System.Collections.Generic;

namespace iso20022;

/// <summary>Represents a complete detected message layout without requiring a generated model.</summary>
public class MessageDocument
{
    /// <summary>The detected physical layout.</summary>
    public EnvelopeKind Layout;
    /// <summary>Whether an application header precedes its payload in a sibling-root layout.</summary>
    public bool HeaderPrecedesPayload;
    /// <summary>The business application header when present.</summary>
    public MessageHeader? Header;
    /// <summary>The ordered payloads outside a multi-message envelope.</summary>
    public List<MessagePayload> Payloads = new List<MessagePayload>();
    /// <summary>The standard or custom envelope when present.</summary>
    public MessageEnvelope? Envelope;
    /// <summary>The extracted message metadata.</summary>
    public MessageMetadata Metadata = new MessageMetadata();
    /// <summary>The namespace declarations from the physical document root.</summary>
    public List<NamespaceDeclarationData> NamespaceDeclarations = new List<NamespaceDeclarationData>();
    /// <summary>The source location of the physical document root.</summary>
    public SourceLocation Location = new SourceLocation();
}
