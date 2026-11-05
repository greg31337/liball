namespace iso20022;

/// <summary>Identifies the effect of a validation diagnostic on the validation outcome.</summary>
public enum ValidationSeverity
{
    /// <summary>The diagnostic severity is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The diagnostic supplies information without making the message invalid.</summary>
    Information = 1,
    /// <summary>The diagnostic identifies a warning that does not make the message invalid by default.</summary>
    Warning = 2,
    /// <summary>The diagnostic identifies an error that makes the message invalid.</summary>
    Error = 3
}
