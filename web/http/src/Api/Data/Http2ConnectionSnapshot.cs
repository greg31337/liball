using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents immutable-by-copy diagnostic state for an HTTP/2 connection.
/// </summary>
public class Http2ConnectionSnapshot
{
    /// <summary>Indicates that the connection has been initialized.</summary>
    public bool IsInitialized;
    /// <summary>Indicates that the connection preface and initial settings exchange permit message traffic.</summary>
    public bool IsReady;
    /// <summary>The local endpoint role.</summary>
    public Http2EndpointRole EndpointRole;
    /// <summary>The effective local settings.</summary>
    public Http2Settings LocalSettings = new Http2Settings();
    /// <summary>The effective remote settings.</summary>
    public Http2Settings RemoteSettings = new Http2Settings();
    /// <summary>The inbound connection flow-control window.</summary>
    public int ReceiveWindow = Http2Constants.DefaultInitialWindowSize;
    /// <summary>The outbound connection flow-control window.</summary>
    public int SendWindow = Http2Constants.DefaultInitialWindowSize;
    /// <summary>The delivered DATA capacity that the application has not released.</summary>
    public int UnreleasedReceiveCapacity;
    /// <summary>The released connection capacity waiting for a batched WINDOW_UPDATE.</summary>
    public int PendingReceiveWindowCredit;
    /// <summary>Indicates that outbound DATA is blocked by the connection flow-control window.</summary>
    public bool IsFlowControlBlocked;
    /// <summary>The effective initial inbound stream flow-control window.</summary>
    public int LocalInitialStreamWindow = Http2Constants.DefaultInitialWindowSize;
    /// <summary>The effective initial outbound stream flow-control window.</summary>
    public int RemoteInitialStreamWindow = Http2Constants.DefaultInitialWindowSize;
    /// <summary>The next locally initiated stream identifier.</summary>
    public int NextLocalStreamId;
    /// <summary>The highest peer-initiated stream identifier observed.</summary>
    public int HighestRemoteStreamId;
    /// <summary>The highest peer-initiated stream successfully processed by the connection.</summary>
    public int LastProcessedRemoteStreamId;
    /// <summary>The number of active locally initiated streams.</summary>
    public int ActiveLocalStreams;
    /// <summary>The number of active remotely initiated streams.</summary>
    public int ActiveRemoteStreams;
    /// <summary>The peer limit on concurrently active locally initiated streams.</summary>
    public long MaximumLocalConcurrentStreams = long.MaxValue;
    /// <summary>The local limit on concurrently active remotely initiated streams.</summary>
    public long MaximumRemoteConcurrentStreams = long.MaxValue;
    /// <summary>Indicates that a client can attempt to open its next request stream.</summary>
    public bool CanCreateLocalRequestStream;
    /// <summary>The number of priority signals retained for idle streams.</summary>
    public int PendingPriorityCount;
    /// <summary>The number of input bytes retained for a fragmented preface or frame.</summary>
    public int BufferedInputLength;
    /// <summary>The configured maximum retained input size.</summary>
    public int MaximumBufferedSize;
    /// <summary>The number of sent SETTINGS frames awaiting acknowledgement.</summary>
    public int PendingLocalSettingsCount;
    /// <summary>The number of sent PING frames awaiting acknowledgement.</summary>
    public int PendingLocalPingCount;
    /// <summary>Indicates that the initial local SETTINGS frame awaits acknowledgement.</summary>
    public bool InitialLocalSettingsPending;
    /// <summary>The zero-based graceful GOAWAY stage, up to two completed sends.</summary>
    public int GracefulShutdownStage;
    /// <summary>Indicates that an inbound compressed header block is incomplete.</summary>
    public bool InboundHeaderBlockActive;
    /// <summary>The stream carrying the incomplete inbound header block.</summary>
    public int InboundHeaderBlockStreamId;
    /// <summary>The retained compressed size of the incomplete inbound header block.</summary>
    public int InboundHeaderBlockLength;
    /// <summary>Indicates that an outbound compressed header block is incomplete.</summary>
    public bool OutboundHeaderBlockActive;
    /// <summary>The stream carrying the incomplete outbound header block.</summary>
    public int OutboundHeaderBlockStreamId;
    /// <summary>The emitted compressed size of the incomplete outbound header block.</summary>
    public int OutboundHeaderBlockLength;
    /// <summary>Indicates that the local connection preface has been sent.</summary>
    public bool SentPreface;
    /// <summary>Indicates that the remote connection preface has been received.</summary>
    public bool ReceivedPreface;
    /// <summary>Indicates that the peer initial SETTINGS frame has been received.</summary>
    public bool ReceivedInitialSettings;
    /// <summary>Indicates that local initial settings have been acknowledged.</summary>
    public bool InitialSettingsAcknowledged;
    /// <summary>Indicates that an HTTP/1 message was detected instead of the peer preface.</summary>
    public bool UnexpectedHttp1;
    /// <summary>Indicates that the unexpected HTTP/1 version was identified.</summary>
    public bool HasUnexpectedHttpVersion;
    /// <summary>The identified unexpected HTTP/1 protocol version.</summary>
    public HttpProtocolVersion UnexpectedHttpVersion;
    /// <summary>Indicates that a GOAWAY frame has been sent.</summary>
    public bool SentGoAway;
    /// <summary>Indicates that a GOAWAY frame has been received.</summary>
    public bool ReceivedGoAway;
    /// <summary>Indicates that an applicable ORIGIN frame initialized the Origin Set.</summary>
    public bool OriginSetInitialized;
    /// <summary>The canonical origins currently present in the connection Origin Set.</summary>
    public List<string> Origins = new List<string>();
    /// <summary>Indicates that a sent GOAWAY last-stream identifier is available.</summary>
    public bool HasSentLastStreamId;
    /// <summary>The last-stream identifier sent in GOAWAY.</summary>
    public int SentLastStreamId;
    /// <summary>Indicates that a sent GOAWAY error code is available.</summary>
    public bool HasSentGoAwayErrorCode;
    /// <summary>The recognized error code sent in GOAWAY.</summary>
    public Http2ErrorCode SentGoAwayErrorCode;
    /// <summary>The exact error code sent in GOAWAY.</summary>
    public uint SentGoAwayErrorCodeValue;
    /// <summary>Indicates that the sent GOAWAY error code is recognized.</summary>
    public bool IsKnownSentGoAwayErrorCode;
    /// <summary>The copied debug data sent in the latest GOAWAY.</summary>
    public byte[] SentGoAwayDebugData = System.Array.Empty<byte>();
    /// <summary>Indicates that a received GOAWAY last-stream identifier is available.</summary>
    public bool HasReceivedLastStreamId;
    /// <summary>The last-stream identifier received in GOAWAY.</summary>
    public int ReceivedLastStreamId;
    /// <summary>Indicates that a received GOAWAY error code is available.</summary>
    public bool HasReceivedGoAwayErrorCode;
    /// <summary>The recognized error code received in GOAWAY.</summary>
    public Http2ErrorCode ReceivedGoAwayErrorCode;
    /// <summary>The exact error code received in GOAWAY.</summary>
    public uint ReceivedGoAwayErrorCodeValue;
    /// <summary>Indicates that the received GOAWAY error code is recognized.</summary>
    public bool IsKnownReceivedGoAwayErrorCode;
    /// <summary>The copied debug data received in the latest GOAWAY.</summary>
    public byte[] ReceivedGoAwayDebugData = System.Array.Empty<byte>();
    /// <summary>Indicates that the connection is in terminal failure or shutdown state.</summary>
    public bool IsTerminal;
    /// <summary>Copies of retained stream states ordered by stream identifier.</summary>
    public List<Http2StreamSnapshot> Streams = new List<Http2StreamSnapshot>();
}
