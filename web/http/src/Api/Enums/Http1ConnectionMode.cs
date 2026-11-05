namespace http;

/// <summary>
/// Identifies the connection disposition after an HTTP/1 message.
/// </summary>
public enum Http1ConnectionMode
{
    /// <summary>
    /// The connection can remain persistent.
    /// </summary>
    Persistent = 0,

    /// <summary>
    /// The connection must close after the message.
    /// </summary>
    Close = 1,

    /// <summary>
    /// The connection was upgraded to another protocol.
    /// </summary>
    Upgraded = 2,

    /// <summary>
    /// The connection became an opaque tunnel.
    /// </summary>
    Tunneled = 3,

    /// <summary>
    /// The connection cannot safely carry another HTTP message.
    /// </summary>
    Unusable = 4
}
