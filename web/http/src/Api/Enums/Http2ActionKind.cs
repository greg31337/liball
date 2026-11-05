namespace http;

/// <summary>
/// Identifies an action requested by HTTP/2 processing.
/// </summary>
public enum Http2ActionKind
{
    /// <summary>
    /// Send an encoded frame.
    /// </summary>
    SendFrame = 0,

    /// <summary>
    /// Acknowledge peer settings.
    /// </summary>
    AcknowledgeSettings = 1,

    /// <summary>
    /// Acknowledge a peer ping.
    /// </summary>
    AcknowledgePing = 2,

    /// <summary>
    /// Reset a stream.
    /// </summary>
    ResetStream = 3,

    /// <summary>
    /// Send a GOAWAY frame.
    /// </summary>
    SendGoAway = 4,

    /// <summary>
    /// Release receive flow-control capacity.
    /// </summary>
    ReleaseCapacity = 5,

    /// <summary>
    /// Initiate a pushed stream.
    /// </summary>
    InitiatePush = 6,

    /// <summary>
    /// Send an HTTP message operation.
    /// </summary>
    SendMessage = 7,

    /// <summary>
    /// Send the client connection preface octets before any frame.
    /// </summary>
    SendConnectionPreface = 8,

    /// <summary>
    /// Start a caller-owned timeout for an outstanding SETTINGS frame.
    /// </summary>
    StartSettingsTimeout = 9,

    /// <summary>
    /// Cancel a caller-owned timeout for an acknowledged SETTINGS frame.
    /// </summary>
    CancelSettingsTimeout = 10,

    /// <summary>
    /// Start a caller-owned timeout for an outstanding PING frame.
    /// </summary>
    StartPingTimeout = 11,

    /// <summary>
    /// Cancel a caller-owned timeout for an acknowledged PING frame.
    /// </summary>
    CancelPingTimeout = 12
}
