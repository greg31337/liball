namespace websocket;

/// <summary>
/// Provides integer WebSocket close codes and registered range boundaries.
/// </summary>
public static class WebSocketCloseCodes
{
    /// <summary>
    /// Indicates normal closure.
    /// </summary>
    public const int NormalClosure = 1000;

    /// <summary>
    /// Indicates that an endpoint is going away.
    /// </summary>
    public const int GoingAway = 1001;

    /// <summary>
    /// Indicates a protocol error.
    /// </summary>
    public const int ProtocolError = 1002;

    /// <summary>
    /// Indicates receipt of unsupported data.
    /// </summary>
    public const int UnsupportedData = 1003;

    /// <summary>
    /// Identifies the reserved close code 1004, which must not be transmitted.
    /// </summary>
    public const int Reserved = 1004;

    /// <summary>
    /// Represents the absence of a status code and must not be transmitted.
    /// </summary>
    public const int NoStatusReceived = 1005;

    /// <summary>
    /// Represents abnormal closure and must not be transmitted.
    /// </summary>
    public const int AbnormalClosure = 1006;

    /// <summary>
    /// Indicates invalid frame payload data.
    /// </summary>
    public const int InvalidFramePayloadData = 1007;

    /// <summary>
    /// Indicates a policy violation.
    /// </summary>
    public const int PolicyViolation = 1008;

    /// <summary>
    /// Indicates that a message was too large to process.
    /// </summary>
    public const int MessageTooBig = 1009;

    /// <summary>
    /// Indicates that a client expected an extension that was not negotiated.
    /// </summary>
    public const int MandatoryExtension = 1010;

    /// <summary>
    /// Indicates an unexpected server condition.
    /// </summary>
    public const int InternalError = 1011;

    /// <summary>
    /// Indicates that a service is restarting.
    /// </summary>
    public const int ServiceRestart = 1012;

    /// <summary>
    /// Indicates that a temporary condition requires retrying later.
    /// </summary>
    public const int TryAgainLater = 1013;

    /// <summary>
    /// Indicates that a gateway or proxy received an invalid upstream response.
    /// </summary>
    public const int BadGateway = 1014;

    /// <summary>
    /// Represents TLS handshake failure and must not be transmitted.
    /// </summary>
    public const int TlsHandshake = 1015;

    /// <summary>
    /// Indicates an unauthorized application request.
    /// </summary>
    public const int Unauthorized = 3000;

    /// <summary>
    /// Indicates a forbidden application request.
    /// </summary>
    public const int Forbidden = 3003;

    /// <summary>
    /// Indicates an application timeout.
    /// </summary>
    public const int Timeout = 3008;

    /// <summary>
    /// The first code in the range available for library and application registration.
    /// </summary>
    public const int RegisteredApplicationRangeMinimum = 3000;

    /// <summary>
    /// The last code in the range available for library and application registration.
    /// </summary>
    public const int RegisteredApplicationRangeMaximum = 3999;

    /// <summary>
    /// The first code in the private-use range.
    /// </summary>
    public const int PrivateUseRangeMinimum = 4000;

    /// <summary>
    /// The last code in the private-use range.
    /// </summary>
    public const int PrivateUseRangeMaximum = 4999;
}
