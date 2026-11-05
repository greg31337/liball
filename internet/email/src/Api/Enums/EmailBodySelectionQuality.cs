namespace email;
/// <summary>
/// Specifies the supported body selection quality values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailBodySelectionQuality
{
    /// <summary>
    /// Represents the preferred choice or state.
    /// </summary>
    Preferred = 0,
    /// <summary>
    /// Represents the fallback choice or state.
    /// </summary>
    Fallback = 1,
    /// <summary>
    /// Represents the unavailable choice or state.
    /// </summary>
    Unavailable = 2
}
