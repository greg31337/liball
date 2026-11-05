namespace sip;

/// <summary>
/// Identifies a standard SIP request method.
/// </summary>
/// <remarks>Extension methods are represented by their original token in message data.</remarks>
public enum SipMethod
{
    /// <summary>
    /// The method is unknown or is an extension method.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The ACK method.
    /// </summary>
    Ack = 1,

    /// <summary>
    /// The BYE method.
    /// </summary>
    Bye = 2,

    /// <summary>
    /// The CANCEL method.
    /// </summary>
    Cancel = 3,

    /// <summary>
    /// The INFO method.
    /// </summary>
    Info = 4,

    /// <summary>
    /// The INVITE method.
    /// </summary>
    Invite = 5,

    /// <summary>
    /// The MESSAGE method.
    /// </summary>
    Message = 6,

    /// <summary>
    /// The NOTIFY method.
    /// </summary>
    Notify = 7,

    /// <summary>
    /// The OPTIONS method.
    /// </summary>
    Options = 8,

    /// <summary>
    /// The PRACK method.
    /// </summary>
    Prack = 9,

    /// <summary>
    /// The PUBLISH method.
    /// </summary>
    Publish = 10,

    /// <summary>
    /// The REFER method.
    /// </summary>
    Refer = 11,

    /// <summary>
    /// The REGISTER method.
    /// </summary>
    Register = 12,

    /// <summary>
    /// The SUBSCRIBE method.
    /// </summary>
    Subscribe = 13,

    /// <summary>
    /// The UPDATE method.
    /// </summary>
    Update = 14
}
