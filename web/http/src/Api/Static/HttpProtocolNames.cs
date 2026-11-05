namespace http;

/// <summary>
/// Provides canonical HTTP version, scheme, ALPN, and upgrade protocol names.
/// </summary>
public static class HttpProtocolNames
{
    /// <summary>
    /// The HTTP/1.0 version token.
    /// </summary>
    public const string Http10 = "HTTP/1.0";

    /// <summary>
    /// The HTTP/1.1 version token.
    /// </summary>
    public const string Http11 = "HTTP/1.1";

    /// <summary>
    /// The HTTP/2 protocol version name.
    /// </summary>
    public const string Http2 = "HTTP/2";

    /// <summary>
    /// The HTTP/2 ALPN protocol identifier.
    /// </summary>
    public const string H2 = "h2";

    /// <summary>
    /// The cleartext HTTP/2 upgrade token.
    /// </summary>
    public const string H2c = "h2c";

    /// <summary>
    /// The cleartext HTTP URI scheme.
    /// </summary>
    public const string Http = "http";

    /// <summary>
    /// The secure HTTP URI scheme.
    /// </summary>
    public const string Https = "https";

    /// <summary>
    /// The WebSocket upgrade protocol token.
    /// </summary>
    public const string WebSocket = "websocket";
}
