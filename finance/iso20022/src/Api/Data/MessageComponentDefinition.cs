using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a reusable message component and its ordered message elements.</summary>
public class MessageComponentDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The message component name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the component.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The data dictionary that contains the component.</summary>
    public RepositoryReference DataDictionary = new RepositoryReference();
    /// <summary>Whether the component has no semantic trace to a business component.</summary>
    public bool IsTechnical;
    /// <summary>The business component from which this component is derived when present.</summary>
    public RepositoryReference? BusinessComponentTrace;
    /// <summary>The building blocks typed by this component.</summary>
    public List<RepositoryReference> MessageBuildingBlocks = new List<RepositoryReference>();
    /// <summary>The ordered message elements contained by the component.</summary>
    public List<RepositoryReference> MessageElements = new List<RepositoryReference>();
    /// <summary>The legacy XOR rules owned by the component.</summary>
    public List<RepositoryReference> Choices = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the component.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the component.</summary>
    public SourceLocation Location = new SourceLocation();
}
