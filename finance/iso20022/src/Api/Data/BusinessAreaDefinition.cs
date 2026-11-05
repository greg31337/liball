using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes one top-level ISO 20022 business area.</summary>
public class BusinessAreaDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The business area name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning of the area.</summary>
    public string Definition = string.Empty;
    /// <summary>The four-letter business area code.</summary>
    public string Code = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The business process catalogue that contains the area.</summary>
    public RepositoryReference BusinessProcessCatalogue = new RepositoryReference();
    /// <summary>The message definitions assigned to the area.</summary>
    public List<RepositoryReference> MessageDefinitions = new List<RepositoryReference>();
    /// <summary>Unrecognized fields attached to the area.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the area.</summary>
    public SourceLocation Location = new SourceLocation();
}
