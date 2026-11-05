namespace email;
/// <summary>
/// Specifies the supported reply attachment values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailReplyAttachmentPolicy
{
    /// <summary>
    /// No reply attachment is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the attachments choice or state.
    /// </summary>
    Attachments = 1,
    /// <summary>
    /// Represents the attachments and inline resources choice or state.
    /// </summary>
    AttachmentsAndInlineResources = 2
}
