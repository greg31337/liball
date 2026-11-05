namespace email;
/// <summary>
/// Specifies the supported issue values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailIssueSeverity
{
    /// <summary>
    /// Represents the information choice or state.
    /// </summary>
    Information = 0,
    /// <summary>
    /// Represents the warning choice or state.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Represents the error choice or state.
    /// </summary>
    Error = 2,
    /// <summary>
    /// Represents the fatal choice or state.
    /// </summary>
    Fatal = 3
}
