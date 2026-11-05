namespace email;
/// <summary>
/// Specifies the supported received clause values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailReceivedClauseKind
{
    /// <summary>
    /// Represents the from choice or state.
    /// </summary>
    From = 0,
    /// <summary>
    /// Represents the by choice or state.
    /// </summary>
    By = 1,
    /// <summary>
    /// Represents the via choice or state.
    /// </summary>
    Via = 2,
    /// <summary>
    /// Represents the with choice or state.
    /// </summary>
    With = 3,
    /// <summary>
    /// Represents the ID choice or state.
    /// </summary>
    Id = 4,
    /// <summary>
    /// Represents the for choice or state.
    /// </summary>
    For = 5,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 6
}
