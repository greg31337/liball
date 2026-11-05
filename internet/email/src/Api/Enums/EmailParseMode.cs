namespace email;
/// <summary>
/// Specifies the supported parse values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailParseMode
{
    /// <summary>
    /// Represents the strict choice or state.
    /// </summary>
    Strict = 0,
    /// <summary>
    /// Represents the conforming tolerant choice or state.
    /// </summary>
    ConformingTolerant = 1,
    /// <summary>
    /// Represents the forensic choice or state.
    /// </summary>
    Forensic = 2
}
