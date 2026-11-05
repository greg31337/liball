using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes the repository identity, XML root, structure, and derivations of a message definition.</summary>
public class MessageDefinitionMetadata
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The message definition name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the message definition.</summary>
    public string Definition = string.Empty;
    /// <summary>The exact four-part message identifier.</summary>
    public MessageIdentifier MessageIdentifier = new MessageIdentifier();
    /// <summary>The exact XML namespace URI.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The legacy XML complex-type name when present.</summary>
    public string XmlName = string.Empty;
    /// <summary>The legacy first-child XML tag when present.</summary>
    public string XmlTag = string.Empty;
    /// <summary>The message root element name.</summary>
    public string RootElement = "Document";
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business area that owns the message definition.</summary>
    public RepositoryReference BusinessArea = new RepositoryReference();
    /// <summary>The message sets containing the definition.</summary>
    public List<RepositoryReference> MessageSets = new List<RepositoryReference>();
    /// <summary>The ordered message building blocks.</summary>
    public List<RepositoryReference> BuildingBlocks = new List<RepositoryReference>();
    /// <summary>The modern choices and legacy XOR rules owned by the definition.</summary>
    public List<RepositoryReference> Choices = new List<RepositoryReference>();
    /// <summary>The choreographies that use the definition.</summary>
    public List<RepositoryReference> Choreographies = new List<RepositoryReference>();
    /// <summary>The business transmissions traced from the definition.</summary>
    public List<RepositoryReference> MessageTransmissions = new List<RepositoryReference>();
    /// <summary>The syntax message schemes derived from the definition.</summary>
    public List<RepositoryReference> SyntaxMessageSchemes = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the definition.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the message definition.</summary>
    public SourceLocation Location = new SourceLocation();
}
