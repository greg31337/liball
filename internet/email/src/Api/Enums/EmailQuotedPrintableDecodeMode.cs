namespace email;
/// <summary>
/// Specifies the supported quoted-printable decode values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailQuotedPrintableDecodeMode
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
