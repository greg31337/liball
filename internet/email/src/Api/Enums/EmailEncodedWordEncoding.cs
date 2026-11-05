namespace email;
/// <summary>
/// Specifies the supported encoded word values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailEncodedWordEncoding
{
    /// <summary>
    /// No encoded word is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the base64 choice or state.
    /// </summary>
    Base64 = 1,
    /// <summary>
    /// Represents the quoted-printable choice or state.
    /// </summary>
    QuotedPrintable = 2
}
