namespace email;
/// <summary>
/// Specifies the supported attachment values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailAttachmentKind
{
    /// <summary>
    /// Represents the body choice or state.
    /// </summary>
    Body = 0,
    /// <summary>
    /// Represents the inline resource choice or state.
    /// </summary>
    InlineResource = 1,
    /// <summary>
    /// Represents the attachment choice or state.
    /// </summary>
    Attachment = 2,
    /// <summary>
    /// Represents the nested message choice or state.
    /// </summary>
    NestedMessage = 3,
    /// <summary>
    /// Represents the signature choice or state.
    /// </summary>
    Signature = 4,
    /// <summary>
    /// Represents the encryption control choice or state.
    /// </summary>
    EncryptionControl = 5,
    /// <summary>
    /// Represents the encrypted payload choice or state.
    /// </summary>
    EncryptedPayload = 6
}
