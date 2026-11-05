namespace http;

/// <summary>
/// Represents the connection disposition after an HTTP/1 message.
/// </summary>
public class Http1ConnectionDecision
{
    /// <summary>
    /// The selected connection mode.
    /// </summary>
    public Http1ConnectionMode ConnectionMode;

    /// <summary>
    /// Indicates that the connection can remain open after the message.
    /// </summary>
    public bool IsPersistent;

    /// <summary>
    /// Indicates that the connection must close after the message.
    /// </summary>
    public bool MustClose;

    /// <summary>
    /// Indicates that an Upgrade response changed the connection protocol.
    /// </summary>
    public bool IsUpgrade;

    /// <summary>
    /// Indicates that a successful CONNECT established a tunnel.
    /// </summary>
    public bool IsTunnel;

    /// <summary>
    /// Indicates that the connection can carry another HTTP message.
    /// </summary>
    public bool IsReusable;

    /// <summary>
    /// The upgraded protocol token, or <c>null</c> when no upgrade occurred.
    /// </summary>
    public string? ProtocolName;

    /// <summary>
    /// A caller-readable disposition explanation, or <c>null</c> when omitted.
    /// </summary>
    public string? Reason;
}
