namespace email;
/// <summary>
/// Specifies the supported write values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailWriteMode
{
    /// <summary>
    /// Represents the exact preservation choice or state.
    /// </summary>
    ExactPreservation = 0,
    /// <summary>
    /// Represents the minimal regeneration choice or state.
    /// </summary>
    MinimalRegeneration = 1,
    /// <summary>
    /// Represents the canonical choice or state.
    /// </summary>
    Canonical = 2
}
