namespace sip;

/// <summary>Holds common transport configuration and limits.</summary>
public class SipTransportOptions
{
    /// <summary>The transport kind.</summary>
    public SipTransportKind Kind;
    /// <summary>The local endpoint to bind.</summary>
    public SipEndpointAddress? LocalAddress;
    /// <summary>The maximum accepted datagram size.</summary>
    public int MaxDatagramBytes = 65507;
    /// <summary>The maximum queued item count.</summary>
    public int MaxQueueCount = SipDefaults.MaxQueueCount;
    /// <summary>The reliable connection idle timeout in milliseconds.</summary>
    public int IdleTimeoutMilliseconds = 60000;
    /// <summary>The keepalive interval in milliseconds, or zero when disabled.</summary>
    public int KeepAliveMilliseconds;
    /// <summary>Enables reliable connection reuse.</summary>
    public bool ReuseConnections = true;
}
