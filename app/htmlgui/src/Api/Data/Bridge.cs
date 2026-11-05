namespace htmlgui;

/// <summary>Describes bridge protocol validation and resource limits.</summary>
public class UiBridgeOptions
{
    /// <summary>The protocol version.</summary>
    public int Version = BridgeProtocolVersions.Current;
    /// <summary>The expected origin.</summary>
    public string Origin = string.Empty;
    /// <summary>Whether messages must originate in the main frame.</summary>
    public bool RequireMainFrame = true;
    /// <summary>The maximum encoded message length.</summary>
    public int MaximumMessageLength = 1048576;
    /// <summary>The maximum pending request count.</summary>
    public int MaximumPendingRequests = 128;
    /// <summary>The timeout in milliseconds.</summary>
    public int TimeoutMilliseconds = 30000;
}

/// <summary>Describes the versioned envelope shared by every bridge message.</summary>
public class UiBridgeEnvelope
{
    /// <summary>The protocol version.</summary>
    public int Version = BridgeProtocolVersions.Current;
    /// <summary>The session identifier.</summary>
    public string Session = string.Empty;
    /// <summary>The monotonically increasing sequence.</summary>
    public int Sequence;
    /// <summary>The optional correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>The origin token.</summary>
    public string Origin = string.Empty;
    /// <summary>The message kind.</summary>
    public UiBridgeMessageKind Kind;
    /// <summary>The portable payload.</summary>
    public UiValue? Payload;
    /// <summary>The optional typed request payload.</summary>
    public UiBridgeRequest? Request;
    /// <summary>The optional typed response payload.</summary>
    public UiBridgeResponse? Response;
    /// <summary>The optional typed error payload.</summary>
    public UiBridgeError? Error;
}

/// <summary>Describes a correlated bridge request.</summary>
public class UiBridgeRequest
{
    /// <summary>The correlation identifier.</summary>
    public string CorrelationId = string.Empty;
    /// <summary>The operation token.</summary>
    public string Operation = string.Empty;
    /// <summary>The portable arguments.</summary>
    public UiValueEntry[] Arguments = System.Array.Empty<UiValueEntry>();
    /// <summary>The timeout in milliseconds.</summary>
    public int TimeoutMilliseconds;
}

/// <summary>Describes a correlated exceptionless bridge response.</summary>
public class UiBridgeResponse
{
    /// <summary>The correlation identifier.</summary>
    public string CorrelationId = string.Empty;
    /// <summary>Whether the request succeeded.</summary>
    public bool Success;
    /// <summary>The optional result value.</summary>
    public UiValue? Value;
    /// <summary>The optional protocol error.</summary>
    public UiBridgeError? Error;
}

/// <summary>Describes a bounded bridge protocol error.</summary>
public class UiBridgeError
{
    /// <summary>The stable error code.</summary>
    public string Code = string.Empty;
    /// <summary>The redacted error message.</summary>
    public string Message = string.Empty;
    /// <summary>The optional correlation identifier.</summary>
    public string? CorrelationId;
    /// <summary>Whether retry may succeed.</summary>
    public bool IsRetryable;
}

/// <summary>Describes negotiated transport state and capabilities.</summary>
public class UiTransportCapabilities
{
    /// <summary>The transport state.</summary>
    public UiTransportState State;
    /// <summary>The negotiated protocol version.</summary>
    public int Version;
    /// <summary>The named capabilities.</summary>
    public UiCapability[] Capabilities = System.Array.Empty<UiCapability>();
    /// <summary>Whether binary payloads are supported.</summary>
    public bool SupportsBinary;
    /// <summary>Whether chunking is supported.</summary>
    public bool SupportsChunking;
}
