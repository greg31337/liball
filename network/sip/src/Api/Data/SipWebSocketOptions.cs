namespace sip;

/// <summary>Holds SIP WebSocket handshake and framing options.</summary>
public class SipWebSocketOptions
{
    /// <summary>The request path.</summary>
    public string? Path = "/";
    /// <summary>The expected or supplied Origin value.</summary>
    public string? Origin;
    /// <summary>The WebSocket subprotocol.</summary>
    public string? Subprotocol = SipTransportNames.WebSocketSubprotocol;
    /// <summary>Permits binary SIP frames.</summary>
    public bool AllowBinaryFrames = true;
    /// <summary>The maximum assembled frame payload size.</summary>
    public int MaxMessageBytes = SipDefaults.MaxMessageBytes;
}
