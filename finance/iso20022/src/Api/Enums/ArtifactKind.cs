namespace iso20022;

/// <summary>Identifies the kind of imported or generated artifact.</summary>
public enum ArtifactKind
{
    /// <summary>The artifact kind is not known.</summary>
    Unknown = 0,
    /// <summary>An ISO 20022 repository artifact.</summary>
    Repository = 1,
    /// <summary>An XML or JSON schema artifact.</summary>
    Schema = 2,
    /// <summary>An external code-set artifact.</summary>
    ExternalCodeSet = 3,
    /// <summary>An example message artifact.</summary>
    Example = 4,
    /// <summary>A documentation artifact.</summary>
    Documentation = 5
}
