namespace email;
/// <summary>
/// Specifies the supported quoted-printable newline values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailQuotedPrintableNewlinePolicy
{
    /// <summary>
    /// Represents the preserve choice or state.
    /// </summary>
    Preserve = 0,
    /// <summary>
    /// Represents the normalize to CRLF choice or state.
    /// </summary>
    NormalizeToCrlf = 1,
    /// <summary>
    /// Represents the encode choice or state.
    /// </summary>
    Encode = 2
}
