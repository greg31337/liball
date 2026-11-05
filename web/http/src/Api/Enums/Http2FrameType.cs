namespace http;

/// <summary>
/// Identifies a registered HTTP/2 frame type or an unknown extension frame.
/// </summary>
public enum Http2FrameType
{
    /// <summary>
    /// An unrecognized frame type whose wire code is preserved separately.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The DATA frame type.
    /// </summary>
    Data = 0x00,

    /// <summary>
    /// The HEADERS frame type.
    /// </summary>
    Headers = 0x01,

    /// <summary>
    /// The deprecated PRIORITY frame type.
    /// </summary>
    Priority = 0x02,

    /// <summary>
    /// The RST_STREAM frame type.
    /// </summary>
    RstStream = 0x03,

    /// <summary>
    /// The SETTINGS frame type.
    /// </summary>
    Settings = 0x04,

    /// <summary>
    /// The PUSH_PROMISE frame type.
    /// </summary>
    PushPromise = 0x05,

    /// <summary>
    /// The PING frame type.
    /// </summary>
    Ping = 0x06,

    /// <summary>
    /// The GOAWAY frame type.
    /// </summary>
    GoAway = 0x07,

    /// <summary>
    /// The WINDOW_UPDATE frame type.
    /// </summary>
    WindowUpdate = 0x08,

    /// <summary>
    /// The CONTINUATION frame type.
    /// </summary>
    Continuation = 0x09,

    /// <summary>
    /// The ORIGIN extension frame type defined by RFC 8336.
    /// </summary>
    Origin = 0x0c,

    /// <summary>
    /// The PRIORITY_UPDATE extension frame type defined by RFC 9218.
    /// </summary>
    PriorityUpdate = 0x10
}
