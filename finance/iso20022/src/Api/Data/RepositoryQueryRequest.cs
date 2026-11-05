namespace iso20022;

/// <summary>Specifies conjunctive repository keys and version-selection criteria.</summary>
public class RepositoryQueryRequest
{
    /// <summary>The exact stable repository identifier, or an empty value for any identifier.</summary>
    public string Identifier = string.Empty;
    /// <summary>The exact repository name, or an empty value for any name.</summary>
    public string Name = string.Empty;
    /// <summary>Whether <see cref="Kind"/> is an active query criterion.</summary>
    public bool HasKind;
    /// <summary>The normalized entity kind criterion.</summary>
    public RepositoryEntityKind Kind;
    /// <summary>Whether <see cref="RegistrationStatus"/> is an active query criterion.</summary>
    public bool HasRegistrationStatus;
    /// <summary>The normalized registration-state criterion.</summary>
    public RegistrationStatus RegistrationStatus;
    /// <summary>The exact business-area code, or an empty value for any area.</summary>
    public string BusinessAreaCode = string.Empty;
    /// <summary>The exact message-set identifier, or an empty value for any set.</summary>
    public string MessageSetIdentifier = string.Empty;
    /// <summary>The exact four-part message identifier, or an empty value for any message.</summary>
    public string MessageIdentifier = string.Empty;
    /// <summary>The exact XML namespace URI, or an empty value for any namespace.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>The exact three-digit message functionality, or an empty value for any functionality.</summary>
    public string MessageFunctionality = string.Empty;
    /// <summary>The exact three-digit message variant, or an empty value for any variant.</summary>
    public string Variant = string.Empty;
    /// <summary>The exact artifact edition, or an empty value for any edition.</summary>
    public string ArtifactEdition = string.Empty;
    /// <summary>The version-selection policy, or <c>null</c> for every version.</summary>
    public VersionSelector? VersionSelector;
    /// <summary>The maximum number of matches returned after deterministic selection.</summary>
    public int MaximumResultCount = Iso20022Limits.MaxCollectionCount;
}
