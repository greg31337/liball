namespace email;
/// <summary>
/// Specifies the supported resent field values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailResentFieldKind
{
    /// <summary>
    /// Represents the date choice or state.
    /// </summary>
    Date = 0,
    /// <summary>
    /// Represents the from choice or state.
    /// </summary>
    From = 1,
    /// <summary>
    /// Represents the sender choice or state.
    /// </summary>
    Sender = 2,
    /// <summary>
    /// Represents the to choice or state.
    /// </summary>
    To = 3,
    /// <summary>
    /// Represents the cc choice or state.
    /// </summary>
    Cc = 4,
    /// <summary>
    /// Represents the bcc choice or state.
    /// </summary>
    Bcc = 5,
    /// <summary>
    /// Represents the message ID choice or state.
    /// </summary>
    MessageId = 6,
    /// <summary>
    /// Represents the reply to choice or state.
    /// </summary>
    ReplyTo = 7,
    /// <summary>
    /// Represents the extension choice or state.
    /// </summary>
    Extension = 8
}
