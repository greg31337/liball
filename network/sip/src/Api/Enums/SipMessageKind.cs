namespace sip;

/// <summary>
/// Identifies the kind of a SIP message.
/// </summary>
public enum SipMessageKind
{
    /// <summary>
    /// The message kind has not been determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The message is a request.
    /// </summary>
    Request = 1,

    /// <summary>
    /// The message is a response.
    /// </summary>
    Response = 2
}
