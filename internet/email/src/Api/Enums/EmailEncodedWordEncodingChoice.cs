namespace email;
/// <summary>
/// Specifies the supported encoded word encoding choice values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailEncodedWordEncodingChoice
{
    /// <summary>
    /// Represents the automatic choice or state.
    /// </summary>
    Automatic = 0,
    /// <summary>
    /// Represents the base64 choice or state.
    /// </summary>
    Base64 = 1,
    /// <summary>
    /// Represents the quoted-printable choice or state.
    /// </summary>
    QuotedPrintable = 2
}
