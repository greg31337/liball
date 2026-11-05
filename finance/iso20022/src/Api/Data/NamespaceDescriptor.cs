namespace iso20022;

/// <summary>Describes the identity and interpretation of an XML namespace.</summary>
public class NamespaceDescriptor
{
    /// <summary>The exact namespace URI.</summary>
    public string Uri = string.Empty;
    /// <summary>The namespace role.</summary>
    public NamespaceKind Kind;
    /// <summary>The namespace processing mode that produced this descriptor.</summary>
    public NamespaceMode Mode;
    /// <summary>The message identifier encoded by the namespace, when applicable.</summary>
    public MessageIdentifier? MessageIdentifier;
}
