namespace email;
/// <summary>
/// Specifies the supported importance values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailImportanceKind
{
    /// <summary>
    /// Represents the low choice or state.
    /// </summary>
    Low = 0,
    /// <summary>
    /// Represents the normal choice or state.
    /// </summary>
    Normal = 1,
    /// <summary>
    /// Represents the high choice or state.
    /// </summary>
    High = 2,
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 3
}
