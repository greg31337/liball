using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a reusable business notion and its business elements and derivations.</summary>
public class BusinessComponentDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The business component name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the component.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The data dictionary that contains the component.</summary>
    public RepositoryReference DataDictionary = new RepositoryReference();
    /// <summary>The generalized business component when one is declared.</summary>
    public RepositoryReference? SuperType;
    /// <summary>The components that specialize this component.</summary>
    public List<RepositoryReference> SubTypes = new List<RepositoryReference>();
    /// <summary>The business elements owned by this component.</summary>
    public List<RepositoryReference> Elements = new List<RepositoryReference>();
    /// <summary>The message component types derived from this component.</summary>
    public List<RepositoryReference> DerivedMessageComponents = new List<RepositoryReference>();
    /// <summary>The association ends whose type is this component.</summary>
    public List<RepositoryReference> AssociationDomains = new List<RepositoryReference>();
    /// <summary>The message elements derived directly from this component.</summary>
    public List<RepositoryReference> DerivedMessageElements = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the component.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the component.</summary>
    public SourceLocation Location = new SourceLocation();
}
