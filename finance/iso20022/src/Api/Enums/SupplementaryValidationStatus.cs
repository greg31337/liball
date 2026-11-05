namespace iso20022;

/// <summary>Identifies the discovery and validation outcome of supplementary message content.</summary>
public enum SupplementaryValidationStatus
{
    /// <summary>The supplementary-data outcome is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>No supplementary data was present.</summary>
    NotPresent = 1,
    /// <summary>The supplementary data was located and accepted.</summary>
    Valid = 2,
    /// <summary>The supplementary data was explicitly ignored under caller policy.</summary>
    Ignored = 3,
    /// <summary>The declared location expression is invalid.</summary>
    InvalidLocation = 4,
    /// <summary>The declared location expression is valid but unsupported.</summary>
    UnsupportedLocation = 5,
    /// <summary>The supplementary namespace is not registered or allowed.</summary>
    UnknownNamespace = 6,
    /// <summary>A required supplementary schema is unavailable.</summary>
    SchemaUnavailable = 7,
    /// <summary>The supplementary payload does not satisfy its selected schema.</summary>
    SchemaInvalid = 8,
    /// <summary>Content marked as requiring understanding could not be processed.</summary>
    MustUnderstandRejected = 9,
    /// <summary>A caller-selected resource limit stopped supplementary processing.</summary>
    ResourceLimitReached = 10,
    /// <summary>The supplementary content failed another validation requirement.</summary>
    Invalid = 11
}
