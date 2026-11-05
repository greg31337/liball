namespace iso20022;

/// <summary>Identifies the structural level of an external-code format disagreement.</summary>
public enum ExternalCodeDisagreementKind
{
    /// <summary>No disagreement kind has been assigned.</summary>
    Unknown = 0,
    /// <summary>The compared catalogues declare different editions.</summary>
    Edition = 1,
    /// <summary>A code set is not present in every format.</summary>
    CodeSetPresence = 2,
    /// <summary>A code value has a different occurrence count between formats.</summary>
    CodeValueMultiplicity = 3,
    /// <summary>A comparable normalized field has different values between formats.</summary>
    FieldValue = 4
}
