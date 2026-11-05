namespace email;
/// <summary>
/// Specifies the supported maildir folder values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailMaildirFolder
{
    /// <summary>
    /// Represents the new choice or state.
    /// </summary>
    New = 0,
    /// <summary>
    /// Represents the current choice or state.
    /// </summary>
    Current = 1,
    /// <summary>
    /// Represents the temporary choice or state.
    /// </summary>
    Temporary = 2
}
