namespace email;
/// <summary>
/// Specifies the supported received parse values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailReceivedParseMode
{
    /// <summary>
    /// Represents the strict choice or state.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Represents the tolerant choice or state.
    /// </summary>
    Tolerant = 1
}
