namespace email;
/// <summary>
/// Specifies the supported redaction values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailRedactionKind
{
    /// <summary>
    /// Represents the address choice or state.
    /// </summary>
    Address = 0,
    /// <summary>
    /// Represents the message ID choice or state.
    /// </summary>
    MessageId = 1,
    /// <summary>
    /// Represents the received choice or state.
    /// </summary>
    Received = 2,
    /// <summary>
    /// Represents the ip address choice or state.
    /// </summary>
    IpAddress = 3,
    /// <summary>
    /// Represents the authentication choice or state.
    /// </summary>
    Authentication = 4,
    /// <summary>
    /// Represents the subject choice or state.
    /// </summary>
    Subject = 5,
    /// <summary>
    /// Represents the body choice or state.
    /// </summary>
    Body = 6,
    /// <summary>
    /// Represents the attachment name choice or state.
    /// </summary>
    AttachmentName = 7,
    /// <summary>
    /// Represents the attachment content choice or state.
    /// </summary>
    AttachmentContent = 8
}
