namespace email;
/// <summary>
/// Specifies the supported sensitivity values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailSensitivityKind
{
    /// <summary>
    /// Represents the normal choice or state.
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Represents the personal choice or state.
    /// </summary>
    Personal = 1,
    /// <summary>
    /// Represents the private choice or state.
    /// </summary>
    Private = 2,
    /// <summary>
    /// Represents the company confidential choice or state.
    /// </summary>
    CompanyConfidential = 3,
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 4
}
