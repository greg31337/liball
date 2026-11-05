using System.Collections.Generic;

namespace iso20022;

/// <summary>Represents a standard or caller-configured message envelope.</summary>
public class MessageEnvelope
{
    /// <summary>The envelope layout.</summary>
    public EnvelopeKind Kind;
    /// <summary>The qualified wrapper name.</summary>
    public QualifiedNameData Name = new QualifiedNameData();
    /// <summary>The wrapper namespace descriptor when known.</summary>
    public NamespaceDescriptor? Namespace;
    /// <summary>The envelope header when present.</summary>
    public MessageHeader? Header;
    /// <summary>The payloads contained directly by the envelope.</summary>
    public List<MessagePayload> Payloads = new List<MessagePayload>();
    /// <summary>The message documents contained by a multi-message envelope.</summary>
    public List<MessageDocument> Documents = new List<MessageDocument>();
    /// <summary>The envelope namespace declarations.</summary>
    public List<NamespaceDeclarationData> NamespaceDeclarations = new List<NamespaceDeclarationData>();
    /// <summary>The generic wrapper representation when materialized.</summary>
    public DynamicXmlNode? GenericContent;
    /// <summary>The preserved raw wrapper when requested.</summary>
    public RawXmlFragment? RawContent;
}
