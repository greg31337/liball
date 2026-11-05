using System.Collections.Generic;

namespace iso20022;

/// <summary>Describes a reusable choice component or a legacy XOR constraint.</summary>
public class ChoiceDefinition
{
    /// <summary>The stable XMI or repository object identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The exact source choice type, such as <c>ChoiceComponent</c> or <c>Xor</c>.</summary>
    public string ChoiceType = string.Empty;
    /// <summary>The choice name.</summary>
    public string Name = string.Empty;
    /// <summary>The business meaning or rule description.</summary>
    public string Definition = string.Empty;
    /// <summary>The registration and lifecycle metadata.</summary>
    public RegistrationMetadata Registration = new RegistrationMetadata();
    /// <summary>The data dictionary containing a reusable choice component.</summary>
    public RepositoryReference DataDictionary = new RepositoryReference();
    /// <summary>Whether a reusable choice has no semantic business-component trace.</summary>
    public bool IsTechnical;
    /// <summary>The business component from which a reusable choice is derived when present.</summary>
    public RepositoryReference? BusinessComponentTrace;
    /// <summary>The message elements contained by a reusable choice component.</summary>
    public List<RepositoryReference> MessageElements = new List<RepositoryReference>();
    /// <summary>The occurrence bounds declared for an imported inline XSD choice group.</summary>
    public MultiplicityDefinition Multiplicity = new MultiplicityDefinition();
    /// <summary>The message elements impacted by a legacy XOR rule.</summary>
    public List<RepositoryReference> ImpactedMessageElements = new List<RepositoryReference>();
    /// <summary>The building blocks impacted by a legacy XOR rule.</summary>
    public List<RepositoryReference> ImpactedBuildingBlocks = new List<RepositoryReference>();
    /// <summary>The message component owning a legacy XOR rule when applicable.</summary>
    public RepositoryReference? OwnerMessageComponent;
    /// <summary>The message definition owning a legacy XOR rule when applicable.</summary>
    public RepositoryReference? OwnerMessageDefinition;
    /// <summary>Unrecognized fields attached to the choice.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
    /// <summary>The source location of the choice.</summary>
    public SourceLocation Location = new SourceLocation();
}
