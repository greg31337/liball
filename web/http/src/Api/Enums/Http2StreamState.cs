namespace http;

/// <summary>
/// Identifies an HTTP/2 stream state.
/// </summary>
public enum Http2StreamState
{
    /// <summary>
    /// The stream is idle.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// The stream is reserved by the local endpoint.
    /// </summary>
    ReservedLocal = 1,

    /// <summary>
    /// The stream is reserved by the remote endpoint.
    /// </summary>
    ReservedRemote = 2,

    /// <summary>
    /// The stream is open in both directions.
    /// </summary>
    Open = 3,

    /// <summary>
    /// The local endpoint has closed its sending direction.
    /// </summary>
    HalfClosedLocal = 4,

    /// <summary>
    /// The remote endpoint has closed its sending direction.
    /// </summary>
    HalfClosedRemote = 5,

    /// <summary>
    /// The stream is closed.
    /// </summary>
    Closed = 6
}
