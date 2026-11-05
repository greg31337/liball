using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a functional business role within a business process.</summary>
public class BusinessRoleDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The business role name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the role.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process that owns the role.</summary>
    public RepositoryReference BusinessProcess = new RepositoryReference();
    /// <summary>The transaction participants traced to this role.</summary>
    public List<RepositoryReference> Participants = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the role.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the role.</summary>
    public SourceLocation Location = new SourceLocation();
}
