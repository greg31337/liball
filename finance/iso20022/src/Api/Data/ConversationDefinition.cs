using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes an exchange of message instances among messaging endpoints.</summary>
public class ConversationDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The conversation name when supplied by the source adapter.</summary>
    public string Name = string.Empty;
    /// <summary>The conversation description when supplied by the source adapter.</summary>
    public string Definition = string.Empty;
    /// <summary>The lifecycle and model-version metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The participants involved in the conversation.</summary>
    public List<RepositoryReference> Participants = new List<RepositoryReference>();
    /// <summary>The choreographies governing the conversation.</summary>
    public List<RepositoryReference> Choreographies = new List<RepositoryReference>();
    /// <summary>The transmissions exchanged in the conversation.</summary>
    public List<RepositoryReference> Transmissions = new List<RepositoryReference>();
    /// <summary>The messaging endpoints involved in the conversation.</summary>
    public List<RepositoryReference> Endpoints = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the conversation.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the conversation.</summary>
    public SourceLocation Location = new SourceLocation();
}
