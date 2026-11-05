namespace email;
/// <summary>
/// Specifies the supported transfer encoding values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTransferEncodingKind
{
    /// <summary>
    /// Represents the seven bit choice or state.
    /// </summary>
    SevenBit = 0,
    /// <summary>
    /// Represents the eight bit choice or state.
    /// </summary>
    EightBit = 1,
    /// <summary>
    /// Represents the binary choice or state.
    /// </summary>
    Binary = 2,
    /// <summary>
    /// Represents the quoted-printable choice or state.
    /// </summary>
    QuotedPrintable = 3,
    /// <summary>
    /// Represents the base64 choice or state.
    /// </summary>
    Base64 = 4
}
