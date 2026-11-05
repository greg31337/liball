namespace email;
/// <summary>
/// Specifies the supported CMS content values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailCmsContentKind
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the data choice or state.
    /// </summary>
    Data = 1,
    /// <summary>
    /// Represents the signed data choice or state.
    /// </summary>
    SignedData = 2,
    /// <summary>
    /// Represents the enveloped data choice or state.
    /// </summary>
    EnvelopedData = 3,
    /// <summary>
    /// Represents the signed and enveloped data choice or state.
    /// </summary>
    SignedAndEnvelopedData = 4,
    /// <summary>
    /// Represents the digested data choice or state.
    /// </summary>
    DigestedData = 5,
    /// <summary>
    /// Represents the encrypted data choice or state.
    /// </summary>
    EncryptedData = 6,
    /// <summary>
    /// Represents the authenticated data choice or state.
    /// </summary>
    AuthenticatedData = 7,
    /// <summary>
    /// Represents the compressed data choice or state.
    /// </summary>
    CompressedData = 8,
    /// <summary>
    /// Represents the auth enveloped data choice or state.
    /// </summary>
    AuthEnvelopedData = 9
}
