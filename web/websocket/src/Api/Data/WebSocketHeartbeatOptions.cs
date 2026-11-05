namespace websocket;

/// <summary>
/// Holds caller-driven heartbeat timing and token settings.
/// </summary>
public class WebSocketHeartbeatOptions
{
    /// <summary>
    /// Indicates that idle heartbeat Ping generation is enabled.
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// Holds elapsed idle milliseconds before a heartbeat Ping is queued.
    /// </summary>
    public int IdleIntervalMilliseconds = WebSocketDefaults.HeartbeatIdleIntervalMilliseconds;

    /// <summary>
    /// Holds elapsed milliseconds to await a matching heartbeat Pong.
    /// </summary>
    public int PongTimeoutMilliseconds = WebSocketDefaults.HeartbeatPongTimeoutMilliseconds;

    /// <summary>
    /// Holds the number of unpredictable bytes in each heartbeat Ping token.
    /// </summary>
    public int TokenLength = WebSocketDefaults.HeartbeatTokenLength;
}
