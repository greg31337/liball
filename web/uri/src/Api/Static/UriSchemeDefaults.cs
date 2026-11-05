namespace uri;

/// <summary>
/// Provides fixed scheme names and default ports used by built-in normalization profiles.
/// </summary>
public static class UriSchemeDefaults
{
    /// <summary>The HTTP scheme name.</summary>
    public const string HttpScheme = "http";

    /// <summary>The default HTTP port.</summary>
    public const int HttpDefaultPort = 80;

    /// <summary>The HTTPS scheme name.</summary>
    public const string HttpsScheme = "https";

    /// <summary>The default HTTPS port.</summary>
    public const int HttpsDefaultPort = 443;

    /// <summary>The WebSocket scheme name.</summary>
    public const string WebSocketScheme = "ws";

    /// <summary>The default WebSocket port.</summary>
    public const int WebSocketDefaultPort = 80;

    /// <summary>The secure WebSocket scheme name.</summary>
    public const string SecureWebSocketScheme = "wss";

    /// <summary>The default secure WebSocket port.</summary>
    public const int SecureWebSocketDefaultPort = 443;

    /// <summary>The FTP scheme name.</summary>
    public const string FtpScheme = "ftp";

    /// <summary>The default FTP control port.</summary>
    public const int FtpDefaultPort = 21;
}
