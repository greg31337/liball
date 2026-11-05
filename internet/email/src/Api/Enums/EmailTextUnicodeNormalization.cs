namespace email;
/// <summary>
/// Specifies the supported text unicode values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTextUnicodeNormalization
{
    /// <summary>
    /// No text unicode is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the nfc choice or state.
    /// </summary>
    Nfc = 1,
    /// <summary>
    /// Represents the nfkc choice or state.
    /// </summary>
    Nfkc = 2
}
