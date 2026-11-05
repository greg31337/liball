namespace websocket;

/// <summary>
/// Holds a public snapshot of connection vocabulary and negotiation state.
/// </summary>
public class WebSocketConnectionInfo
{
    /// <summary>
    /// Holds an independent snapshot of the applied connection options.
    /// </summary>
    public WebSocketConnectionOptions Options = new WebSocketConnectionOptions();

    /// <summary>
    /// Identifies the local endpoint role.
    /// </summary>
    public WebSocketRole Role;

    /// <summary>
    /// Identifies the current connection lifecycle state.
    /// </summary>
    public WebSocketConnectionState State;

    /// <summary>
    /// Identifies how received messages are delivered.
    /// </summary>
    public WebSocketMessageDeliveryMode DeliveryMode;

    /// <summary>
    /// Identifies the negotiated handshake transport.
    /// </summary>
    public WebSocketHandshakeTransport Transport;

    /// <summary>
    /// Indicates whether <see cref="SelectedSubprotocol"/> contains a negotiated subprotocol.
    /// </summary>
    public bool HasSelectedSubprotocol;

    /// <summary>
    /// Holds the negotiated subprotocol when <see cref="HasSelectedSubprotocol"/> is true.
    /// </summary>
    public string SelectedSubprotocol = "";

    /// <summary>
    /// Holds negotiated extension snapshots in transformation order.
    /// </summary>
    public WebSocketExtensionNegotiation[] Extensions = [];

    /// <summary>
    /// Holds the opening-handshake result used to initialize the connection.
    /// </summary>
    public WebSocketHandshakeResult Handshake = new WebSocketHandshakeResult();

    /// <summary>
    /// Holds the number of encoded bytes waiting to be drained.
    /// </summary>
    public int PendingOutputBytes;

    /// <summary>
    /// Holds the number of queued events waiting to be drained.
    /// </summary>
    public int PendingEventCount;

    /// <summary>
    /// Holds the number of complete frames received.
    /// </summary>
    public long ReceivedFrameCount;

    /// <summary>
    /// Holds the number of complete frames sent.
    /// </summary>
    public long SentFrameCount;

    /// <summary>
    /// Holds the number of complete application messages received.
    /// </summary>
    public long ReceivedMessageCount;

    /// <summary>
    /// Holds the number of complete application messages sent.
    /// </summary>
    public long SentMessageCount;

    /// <summary>
    /// Holds the latest caller-supplied monotonic elapsed milliseconds.
    /// </summary>
    public long ElapsedMilliseconds;

    /// <summary>
    /// Indicates that a tracked heartbeat Ping is awaiting acknowledgement.
    /// </summary>
    public bool HeartbeatPingOutstanding;

    /// <summary>
    /// Indicates whether <see cref="HeartbeatPingSentMilliseconds"/> contains the outstanding Ping send time.
    /// </summary>
    public bool HasHeartbeatPingSentMilliseconds;

    /// <summary>
    /// Holds the elapsed send time of the outstanding heartbeat Ping.
    /// </summary>
    public long HeartbeatPingSentMilliseconds;

    /// <summary>
    /// Indicates whether <see cref="LastPingRoundTripMilliseconds"/> contains a measured round trip.
    /// </summary>
    public bool HasLastPingRoundTripMilliseconds;

    /// <summary>
    /// Holds the most recently measured Ping round-trip milliseconds.
    /// </summary>
    public long LastPingRoundTripMilliseconds;

    /// <summary>
    /// Indicates whether <see cref="LocalClose"/> contains local close details.
    /// </summary>
    public bool HasLocalClose;

    /// <summary>
    /// Holds local close details when <see cref="HasLocalClose"/> is true.
    /// </summary>
    public WebSocketCloseInfo LocalClose = new WebSocketCloseInfo();

    /// <summary>
    /// Indicates whether <see cref="RemoteClose"/> contains remote close details.
    /// </summary>
    public bool HasRemoteClose;

    /// <summary>
    /// Holds remote close details when <see cref="HasRemoteClose"/> is true.
    /// </summary>
    public WebSocketCloseInfo RemoteClose = new WebSocketCloseInfo();

    /// <summary>
    /// Indicates that the complete close handshake and transport termination were clean.
    /// </summary>
    public bool CleanClose;

    /// <summary>
    /// Indicates that the transport input ended.
    /// </summary>
    public bool TransportEnded;

    /// <summary>
    /// Indicates that the transport reported a reset or equivalent failure.
    /// </summary>
    public bool TransportReset;

    /// <summary>
    /// Indicates that the caller should end its local transport direction.
    /// </summary>
    public bool ShouldEndTransport;

    /// <summary>
    /// Indicates whether <see cref="Error"/> contains a terminal error.
    /// </summary>
    public bool HasError;

    /// <summary>
    /// Holds the terminal error classification when <see cref="HasError"/> is true.
    /// </summary>
    public WebSocketError Error = new WebSocketError();
}
