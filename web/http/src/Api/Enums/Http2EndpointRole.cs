namespace http;

/// <summary>
/// Identifies the local endpoint role on an HTTP/2 connection.
/// </summary>
public enum Http2EndpointRole
{
    /// <summary>
    /// The local endpoint is the client.
    /// </summary>
    Client = 0,

    /// <summary>
    /// The local endpoint is the server.
    /// </summary>
    Server = 1
}
