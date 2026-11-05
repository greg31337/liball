namespace iso20022;

/// <summary>Contains an ISO 20022 business application header and its extracted identity.</summary>
public class MessageHeader
{
    /// <summary>The exact header identifier when known.</summary>
    public MessageIdentifier? Identifier;
    /// <summary>The header namespace descriptor when known.</summary>
    public NamespaceDescriptor? Namespace;
    /// <summary>The business message identifier extracted from the header.</summary>
    public string BusinessMessageIdentifier = string.Empty;
    /// <summary>The business service or profile extracted from the header.</summary>
    public string BusinessService = string.Empty;
    /// <summary>The generic XML representation when materialized.</summary>
    public DynamicXmlNode? GenericContent;
    /// <summary>The preserved raw representation when requested.</summary>
    public RawXmlFragment? RawContent;
}
