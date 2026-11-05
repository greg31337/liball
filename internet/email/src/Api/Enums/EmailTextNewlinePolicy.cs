namespace email;
/// <summary>
/// Specifies the supported text newline values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTextNewlinePolicy
{
    /// <summary>
    /// Represents the preserve choice or state.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Represents the normalize to lf choice or state.
    /// </summary>
    NormalizeToLf = 1,
    /// <summary>
    /// Represents the normalize to CRLF choice or state.
    /// </summary>
    NormalizeToCrlf = 2
}
