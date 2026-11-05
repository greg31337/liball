using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes the involvement of a business role in a business transaction.</summary>
public class ParticipantDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The participant name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the participation.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business transaction containing the participant.</summary>
    public RepositoryReference BusinessTransaction = new RepositoryReference();
    /// <summary>The business role to which the participant is traced.</summary>
    public RepositoryReference BusinessRole = new RepositoryReference();
    /// <summary>The minimum participation multiplicity.</summary>
    public int MinimumOccurs;
    /// <summary>Whether a finite maximum participation multiplicity is present.</summary>
    public bool HasMaximumOccurs;
    /// <summary>The maximum participation multiplicity when finite.</summary>
    public int MaximumOccurs;
    /// <summary>The transmissions sent by the participant.</summary>
    public List<RepositoryReference> Sends = new List<RepositoryReference>();
    /// <summary>The transmissions received by the participant.</summary>
    public List<RepositoryReference> Receives = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the participant.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the participant.</summary>
    public SourceLocation Location = new SourceLocation();
}
