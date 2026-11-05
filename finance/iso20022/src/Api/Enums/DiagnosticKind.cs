namespace iso20022;

/// <summary>Identifies the stable category of a validation diagnostic.</summary>
public enum DiagnosticKind
{
    /// <summary>The diagnostic kind is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>A general validation rule failed.</summary>
    ValidationFailure = 1,
    /// <summary>Required content is absent.</summary>
    MissingContent = 2,
    /// <summary>Content appears where it is not permitted.</summary>
    UnexpectedContent = 3,
    /// <summary>A supplied lexical or logical value is invalid.</summary>
    InvalidValue = 4,
    /// <summary>A collection or element cardinality is invalid.</summary>
    Cardinality = 5,
    /// <summary>A choice or exclusive-content rule is invalid.</summary>
    Choice = 6,
    /// <summary>A declared constraint cannot be executed automatically.</summary>
    UnsupportedConstraint = 7,
    /// <summary>A rule or constraint cannot be applied to the selected message context.</summary>
    Applicability = 8,
    /// <summary>Two selected definitions or overlays conflict.</summary>
    Conflict = 9,
    /// <summary>A caller-selected validation resource limit was reached.</summary>
    ResourceLimit = 10,
    /// <summary>The requested validation configuration is invalid or incomplete.</summary>
    Configuration = 11,
    /// <summary>A secure-processing or integrity requirement failed.</summary>
    Security = 12
}
