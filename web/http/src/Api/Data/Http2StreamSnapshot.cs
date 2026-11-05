namespace http;

/// <summary>
/// Represents immutable-by-copy diagnostic state for one HTTP/2 stream.
/// </summary>
public class Http2StreamSnapshot
{
    /// <summary>
    /// The stream identifier.
    /// </summary>
    public int StreamId;

    /// <summary>
    /// The current stream state.
    /// </summary>
    public Http2StreamState StreamState;

    /// <summary>The current inbound flow-control window.</summary>
    public int ReceiveWindow = Http2Constants.DefaultInitialWindowSize;
    /// <summary>The current outbound flow-control window, which can be negative after settings changes.</summary>
    public int SendWindow = Http2Constants.DefaultInitialWindowSize;
    /// <summary>The delivered DATA capacity that the application has not released.</summary>
    public int UnreleasedReceiveCapacity;
    /// <summary>The released capacity waiting for a batched stream WINDOW_UPDATE.</summary>
    public int PendingReceiveWindowCredit;
    /// <summary>Indicates that outbound DATA is blocked by a connection or stream flow-control window.</summary>
    public bool IsFlowControlBlocked;
    /// <summary>Indicates that an initial inbound header section has been received.</summary>
    public bool ReceivedHeaders;
    /// <summary>Indicates that an initial outbound header section has been sent.</summary>
    public bool SentHeaders;
    /// <summary>Indicates that the local sending direction has ended.</summary>
    public bool LocalEndStream;
    /// <summary>Indicates that the remote sending direction has ended.</summary>
    public bool RemoteEndStream;
    /// <summary>The total received DATA payload size excluding padding.</summary>
    public long ReceivedContentLength;
    /// <summary>The total sent DATA payload size excluding padding.</summary>
    public long SentContentLength;
    /// <summary>Indicates that the stream was reset.</summary>
    public bool IsReset;
    /// <summary>Indicates that the local endpoint sent the reset.</summary>
    public bool ResetByLocal;
    /// <summary>Indicates that the local endpoint initiated or completed stream closure.</summary>
    public bool ClosedByLocal;
    /// <summary>Indicates that a reset error code is available.</summary>
    public bool HasErrorCode;
    /// <summary>The recognized reset error code.</summary>
    public Http2ErrorCode ErrorCode;
    /// <summary>The exact unsigned reset error code.</summary>
    public uint ErrorCodeValue;
    /// <summary>Indicates that <see cref="ErrorCodeValue"/> maps to <see cref="ErrorCode"/>.</summary>
    public bool IsKnownErrorCode;
    /// <summary>Indicates that this is a pushed stream.</summary>
    public bool IsPushed;
    /// <summary>Indicates that an associated stream identifier is available.</summary>
    public bool HasAssociatedStreamId;
    /// <summary>The associated stream identifier for a pushed stream.</summary>
    public int AssociatedStreamId;
    /// <summary>The validated request that identifies the pushed response, or <c>null</c>.</summary>
    public HttpRequest? PushRequest;
    /// <summary>Indicates that the promised request method permits storing its response.</summary>
    public bool IsPushCacheable;
    /// <summary>Indicates that retry can be considered because the peer did not process the stream.</summary>
    public bool IsRetryEligible;
    /// <summary>The client-provided extensible priority, or <c>null</c> when absent.</summary>
    public HttpPriority? ClientPriority;
    /// <summary>The server-provided extensible priority, or <c>null</c> when absent.</summary>
    public HttpPriority? ServerPriority;
    /// <summary>The latest complete PRIORITY_UPDATE value, or <c>null</c> when absent.</summary>
    public HttpPriority? PriorityUpdate;
    /// <summary>The effective merged or updated extensible priority, or <c>null</c>.</summary>
    public HttpPriority? Priority;
}
