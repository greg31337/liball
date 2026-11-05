using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a business activity and its included, extended, role, and transaction relationships.</summary>
public class BusinessProcessDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The business process name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the process.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process catalogue that contains the process.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The processes that extend this process.</summary>
    public List<RepositoryReference> Extenders = new List<RepositoryReference>();
    /// <summary>The processes extended by this process.</summary>
    public List<RepositoryReference> ExtendedProcesses = new List<RepositoryReference>();
    /// <summary>The processes included by this process.</summary>
    public List<RepositoryReference> IncludedProcesses = new List<RepositoryReference>();
    /// <summary>The processes that include this process.</summary>
    public List<RepositoryReference> Includers = new List<RepositoryReference>();
    /// <summary>The business roles owned by the process.</summary>
    public List<RepositoryReference> BusinessRoles = new List<RepositoryReference>();
    /// <summary>The transactions traced to the process.</summary>
    public List<RepositoryReference> BusinessTransactions = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the process.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the process.</summary>
    public SourceLocation Location = new SourceLocation();
}
