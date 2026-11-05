namespace sip;

/// <summary>
/// Identifies a standard SIP Content-Disposition disposition type.
/// </summary>
/// <remarks>Extension disposition types are preserved by their original token in message data.</remarks>
public enum SipBodyDisposition
{
    /// <summary>
    /// The disposition is unknown or extension-defined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The body describes a communications session.
    /// </summary>
    Session = 1,

    /// <summary>
    /// The body should be rendered to the user.
    /// </summary>
    Render = 2,

    /// <summary>
    /// The body is an icon representation.
    /// </summary>
    Icon = 3,

    /// <summary>
    /// The body supplies alerting information.
    /// </summary>
    Alert = 4,

    /// <summary>
    /// The body carries signaling information.
    /// </summary>
    Signal = 5,

    /// <summary>
    /// The body is referenced rather than transferred inline.
    /// </summary>
    ByReference = 6,

    /// <summary>
    /// The body contains session-recording metadata.
    /// </summary>
    RecordingSession = 7
}
