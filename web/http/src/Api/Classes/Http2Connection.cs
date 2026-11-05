namespace http;

/// <summary>
/// Processes transport-independent HTTP/2 connection state and protocol actions.
/// </summary>
public class Http2Connection : IHttp2Connection
{
    internal Http2ConnectionState state = new Http2ConnectionState();

    /// <summary>Creates an uninitialized HTTP/2 connection driver.</summary>
    public Http2Connection()
    {
    }

    /// <summary>
    /// Processes one transport-independent connection input operation.
    /// </summary>
    /// <param name="input">Inbound bytes, a decoded frame, or an outbound operation.</param>
    /// <returns>The ordered events, actions, progress, and connection state.</returns>
    public Http2ConnectionResult ProcessHttp2Input(Http2ConnectionInput input)
    {
        return state.Process(input);
    }

    /// <summary>
    /// Queues an outbound protocol action.
    /// </summary>
    /// <param name="action">The action to queue.</param>
    /// <returns>The resulting ordered protocol actions and connection state.</returns>
    public Http2ConnectionResult QueueHttp2Action(Http2Action action)
    {
        return state.Queue(action);
    }

    /// <summary>
    /// Releases application-consumed inbound flow-control capacity.
    /// </summary>
    /// <param name="streamId">The stream identifier, or zero for connection-only capacity.</param>
    /// <param name="capacity">The positive number of consumed octets.</param>
    /// <returns>The resulting WINDOW_UPDATE actions and state.</returns>
    public Http2ConnectionResult ReleaseHttp2Capacity(
        int streamId,
        int capacity)
    {
        return state.Release(streamId, capacity);
    }

    /// <summary>
    /// Inspects a copy of current connection and retained stream state.
    /// </summary>
    /// <returns>The connection snapshot.</returns>
    public Http2ConnectionSnapshot InspectHttp2Connection()
    {
        return state.CreateSnapshot();
    }

    /// <summary>
    /// Evaluates how the connection Origin Set constrains authority and coalescing.
    /// </summary>
    /// <param name="origin">The canonical ASCII serialization of an HTTP origin.</param>
    /// <returns>Origin Set advice that leaves external validation and selection to the caller.</returns>
    public Http2OriginAdvice EvaluateHttp2Origin(string origin)
    {
        return state.EvaluateOrigin(origin);
    }

    /// <summary>
    /// Resets connection state using the supplied options.
    /// </summary>
    /// <param name="options">The options for the new connection state.</param>
    /// <returns>The initialized connection state and startup actions.</returns>
    public Http2ConnectionResult ResetHttp2Connection(
        Http2ConnectionOptions options)
    {
        return state.Reset(options);
    }
}
