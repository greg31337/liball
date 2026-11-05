namespace websocket;

/// <summary>
/// Identifies close codes defined by RFC 6455 and the IANA WebSocket registry.
/// </summary>
/// <remarks>
/// APIs also retain the raw integer close code so future registered and private-use values remain representable.
/// </remarks>
public enum WebSocketCloseCode
{
    /// <summary>
    /// Indicates normal closure.
    /// </summary>
    NormalClosure = 1000,

    /// <summary>
    /// Indicates that an endpoint is going away.
    /// </summary>
    GoingAway = 1001,

    /// <summary>
    /// Indicates a protocol error.
    /// </summary>
    ProtocolError = 1002,

    /// <summary>
    /// Indicates receipt of unsupported data.
    /// </summary>
    UnsupportedData = 1003,

    /// <summary>
    /// Identifies the reserved close code 1004, which must not be transmitted.
    /// </summary>
    Reserved = 1004,

    /// <summary>
    /// Represents the absence of a status code and must not be transmitted.
    /// </summary>
    NoStatusReceived = 1005,

    /// <summary>
    /// Represents abnormal closure and must not be transmitted.
    /// </summary>
    AbnormalClosure = 1006,

    /// <summary>
    /// Indicates invalid frame payload data.
    /// </summary>
    InvalidFramePayloadData = 1007,

    /// <summary>
    /// Indicates a policy violation.
    /// </summary>
    PolicyViolation = 1008,

    /// <summary>
    /// Indicates that a message was too large to process.
    /// </summary>
    MessageTooBig = 1009,

    /// <summary>
    /// Indicates that the client expected an extension the server did not negotiate.
    /// </summary>
    MandatoryExtension = 1010,

    /// <summary>
    /// Indicates an unexpected server condition.
    /// </summary>
    InternalError = 1011,

    /// <summary>
    /// Indicates that a service is restarting.
    /// </summary>
    ServiceRestart = 1012,

    /// <summary>
    /// Indicates that a temporary condition requires retrying later.
    /// </summary>
    TryAgainLater = 1013,

    /// <summary>
    /// Indicates that a gateway or proxy received an invalid upstream response.
    /// </summary>
    BadGateway = 1014,

    /// <summary>
    /// Represents TLS handshake failure and must not be transmitted.
    /// </summary>
    TlsHandshake = 1015,

    /// <summary>
    /// Indicates an unauthorized application request.
    /// </summary>
    Unauthorized = 3000,

    /// <summary>
    /// Indicates a forbidden application request.
    /// </summary>
    Forbidden = 3003,

    /// <summary>
    /// Indicates an application timeout.
    /// </summary>
    Timeout = 3008
}
