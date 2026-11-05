namespace email;
/// <summary>
/// Specifies the supported thread identity values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailThreadIdentitySource
{
    /// <summary>
    /// No thread identity is selected or present.
    /// </summary>
    None = 0,
    /// <summary>
    /// Represents the references choice or state.
    /// </summary>
    References = 1,
    /// <summary>
    /// Represents the in reply to choice or state.
    /// </summary>
    InReplyTo = 2,
    /// <summary>
    /// Represents the message ID choice or state.
    /// </summary>
    MessageId = 3,
    /// <summary>
    /// Represents the normalized subject choice or state.
    /// </summary>
    NormalizedSubject = 4
}
