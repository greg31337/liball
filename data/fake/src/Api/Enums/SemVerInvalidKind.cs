namespace fake;

/// <summary>Selects a deliberate Semantic Versioning 2.0.0 syntax defect.</summary>
public enum SemVerInvalidKind
{
    /// <summary>Adds a forbidden leading zero to a core numeric component.</summary>
    LeadingZero = 0,

    /// <summary>Omits one required core numeric component.</summary>
    MissingComponent = 1,

    /// <summary>Places an empty identifier in a pre-release sequence.</summary>
    EmptyIdentifier = 2,

    /// <summary>Places a character outside the ASCII identifier alphabet.</summary>
    InvalidCharacter = 3,

    /// <summary>Adds a forbidden leading zero to a numeric pre-release identifier.</summary>
    NumericPreReleaseLeadingZero = 4,

    /// <summary>Adds a fourth numeric component to the version core.</summary>
    ExtraCoreComponent = 5
}
