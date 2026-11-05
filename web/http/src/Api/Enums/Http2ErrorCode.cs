namespace http;

/// <summary>
/// Identifies a registered HTTP/2 error code or an unknown extension error.
/// </summary>
public enum Http2ErrorCode
{
    /// <summary>
    /// An unrecognized error whose unsigned wire code is preserved separately.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// NO_ERROR: the associated condition is not the result of an error.
    /// </summary>
    NoError = 0x00,

    /// <summary>
    /// PROTOCOL_ERROR: a protocol error was detected.
    /// </summary>
    ProtocolError = 0x01,

    /// <summary>
    /// INTERNAL_ERROR: an implementation fault occurred.
    /// </summary>
    InternalError = 0x02,

    /// <summary>
    /// FLOW_CONTROL_ERROR: flow-control limits were exceeded.
    /// </summary>
    FlowControlError = 0x03,

    /// <summary>
    /// SETTINGS_TIMEOUT: settings were not acknowledged in time.
    /// </summary>
    SettingsTimeout = 0x04,

    /// <summary>
    /// STREAM_CLOSED: a frame was received for a closed stream.
    /// </summary>
    StreamClosed = 0x05,

    /// <summary>
    /// FRAME_SIZE_ERROR: a frame size was invalid.
    /// </summary>
    FrameSizeError = 0x06,

    /// <summary>
    /// REFUSED_STREAM: a stream was not processed.
    /// </summary>
    RefusedStream = 0x07,

    /// <summary>
    /// CANCEL: a stream was cancelled.
    /// </summary>
    Cancel = 0x08,

    /// <summary>
    /// COMPRESSION_ERROR: compression state could not be maintained.
    /// </summary>
    CompressionError = 0x09,

    /// <summary>
    /// CONNECT_ERROR: a CONNECT tunnel encountered a connection error.
    /// </summary>
    ConnectError = 0x0a,

    /// <summary>
    /// ENHANCE_YOUR_CALM: the peer exceeded processing capacity.
    /// </summary>
    EnhanceYourCalm = 0x0b,

    /// <summary>
    /// INADEQUATE_SECURITY: negotiated security parameters were unacceptable.
    /// </summary>
    InadequateSecurity = 0x0c,

    /// <summary>
    /// HTTP_1_1_REQUIRED: the peer requires HTTP/1.1.
    /// </summary>
    Http11Required = 0x0d
}
