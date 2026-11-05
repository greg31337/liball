using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes the sequence and correlation of message exchanges for a business transaction.</summary>
public class MessageChoreographyDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The choreography name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the choreography.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process catalogue that contains the choreography.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The business transaction to which the choreography is traced.</summary>
    public RepositoryReference BusinessTransaction = new RepositoryReference();
    /// <summary>The message definitions used by the choreography.</summary>
    public List<RepositoryReference> MessageDefinitions = new List<RepositoryReference>();
    /// <summary>The conversations described by the choreography.</summary>
    public List<RepositoryReference> Conversations = new List<RepositoryReference>();
    /// <summary>The ordered message transmissions in the choreography.</summary>
    public List<RepositoryReference> Transmissions = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the choreography.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the choreography.</summary>
    public SourceLocation Location = new SourceLocation();
}
