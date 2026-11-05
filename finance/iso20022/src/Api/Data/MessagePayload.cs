namespace iso20022;

/// <summary>Contains one ISO 20022 message payload in typed, generic, or raw form.</summary>
public class MessagePayload
{
    /// <summary>The exact message identifier when known.</summary>
    public MessageIdentifier? Identifier;
    /// <summary>The payload namespace descriptor when known.</summary>
    public NamespaceDescriptor? Namespace;
    /// <summary>The generic XML representation when materialized.</summary>
    public DynamicXmlNode? GenericContent;
    /// <summary>The generated typed representation when available.</summary>
    public object? TypedContent;
    /// <summary>The preserved raw representation when requested.</summary>
    public RawXmlFragment? RawContent;
}
