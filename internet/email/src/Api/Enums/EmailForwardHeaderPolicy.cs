namespace email;
/// <summary>
/// Specifies the supported forward header values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailForwardHeaderPolicy
{
    /// <summary>
    /// Represents the minimal choice or state.
    /// </summary>
    Minimal = 0,
    /// <summary>
    /// Represents the safe choice or state.
    /// </summary>
    Safe = 1,
    /// <summary>
    /// Represents the preserve choice or state.
    /// </summary>
    Preserve = 2
}
