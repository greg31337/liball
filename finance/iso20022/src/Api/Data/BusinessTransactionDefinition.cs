using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a business transaction, its participants, transmissions, and decomposition.</summary>
public class BusinessTransactionDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The business transaction name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the transaction.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process catalogue that contains the transaction.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The business process to which the transaction is traced.</summary>
    public RepositoryReference BusinessProcess = new RepositoryReference();
    /// <summary>The participants contained by the transaction.</summary>
    public List<RepositoryReference> Participants = new List<RepositoryReference>();
    /// <summary>The message transmissions contained by the transaction.</summary>
    public List<RepositoryReference> Transmissions = new List<RepositoryReference>();
    /// <summary>The message transport mode applied to the transaction.</summary>
    public RepositoryReference TransportMode = new RepositoryReference();
    /// <summary>The parent transaction when this transaction is a decomposition member.</summary>
    public RepositoryReference? ParentTransaction;
    /// <summary>The ordered subtransactions.</summary>
    public List<RepositoryReference> SubTransactions = new List<RepositoryReference>();
    /// <summary>The message choreographies traced from the transaction.</summary>
    public List<RepositoryReference> Choreographies = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the transaction.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the transaction.</summary>
    public SourceLocation Location = new SourceLocation();
}
