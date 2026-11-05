namespace email;
/// <summary>
/// Specifies the supported charset fallback values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailCharsetFallbackPolicy
{
    /// <summary>
    /// Represents the strict choice or state.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Represents the replacement choice or state.
    /// </summary>
    Replacement = 1
}
