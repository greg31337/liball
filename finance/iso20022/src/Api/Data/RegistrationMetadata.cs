using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains repository registration, lifecycle, documentation, and version metadata.</summary>
public class RegistrationMetadata
{
    /// <summary>The normalized ISO 20022 registration status.</summary>
    public RegistrationStatus Status;
    /// <summary>The exact source registration-status value for forward compatibility.</summary>
    public string StatusValue = string.Empty;
    /// <summary>The date on which the concept was registered when known.</summary>
    public IsoDateValue? RegistrationDate;
    /// <summary>The date on which the concept ceased or will cease to be part of the repository.</summary>
    public IsoDateValue? RemovalDate;
    /// <summary>Whether the concept is explicitly deprecated independently of its registration status.</summary>
    public bool IsDeprecated;
    /// <summary>The source deprecation or removal explanation.</summary>
    public string DeprecationReason = string.Empty;
    /// <summary>The concept that replaces this concept when declared.</summary>
    public RepositoryReference? ReplacedBy;
    /// <summary>The preceding version from which this concept stems when declared.</summary>
    public RepositoryReference? PreviousVersion;
    /// <summary>The ordered later versions that stem from this concept.</summary>
    public List<RepositoryReference> NextVersions = new List<RepositoryReference>();
    /// <summary>Representative examples attached to the concept.</summary>
    public List<string> Examples = new List<string>();
    /// <summary>Categorized documentation items attached to the concept.</summary>
    public List<SemanticMarkupData> Doclets = new List<SemanticMarkupData>();
    /// <summary>Structured semantic markup, including synonyms and their contexts.</summary>
    public List<SemanticMarkupData> SemanticMarkup = new List<SemanticMarkupData>();
    /// <summary>The constraints owned by the concept.</summary>
    public List<ConstraintDefinition> Constraints = new List<ConstraintDefinition>();
    /// <summary>Unrecognized lifecycle or documentation content.</summary>
    public List<RepositoryUnknownData> UnknownContent = new List<RepositoryUnknownData>();
}
