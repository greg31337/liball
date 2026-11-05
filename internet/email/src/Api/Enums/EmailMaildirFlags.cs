namespace email;
/// <summary>
/// Specifies the supported maildir flags values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMaildirFlags
{
    /// <summary>
    /// No maildir flags is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the draft choice or state.
    /// </summary>
    Draft = 1,
    /// <summary>
    /// Represents the flagged choice or state.
    /// </summary>
    Flagged = 2,
    /// <summary>
    /// Represents the passed choice or state.
    /// </summary>
    Passed = 4,
    /// <summary>
    /// Represents the replied choice or state.
    /// </summary>
    Replied = 8,
    /// <summary>
    /// Represents the seen choice or state.
    /// </summary>
    Seen = 16,
    /// <summary>
    /// Represents the trashed choice or state.
    /// </summary>
    Trashed = 32
}
