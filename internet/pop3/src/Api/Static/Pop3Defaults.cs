namespace pop3;

/// <summary>Provides standard ports and conservative POP3 limits and timeouts.</summary>
public static class Pop3Defaults
{
    /// <summary>The standard TCP port for plaintext POP3 and explicit STLS.</summary>
    public const int DefaultPort = 110;

    /// <summary>The standard TCP port for implicit TLS POP3.</summary>
    public const int ImplicitTlsPort = 995;

    /// <summary>The maximum command line size in octets, including its terminating CRLF.</summary>
    public const int MaximumCommandLineOctets = 255;

    /// <summary>The maximum response or capability line size in octets, including its terminating CRLF.</summary>
    public const int MaximumResponseLineOctets = 512;

    /// <summary>The smallest enabled server inactivity timeout, in milliseconds.</summary>
    public const int MinimumServerInactivityTimeoutMilliseconds = 600000;

    /// <summary>The default server inactivity timeout, in milliseconds.</summary>
    public const int DefaultServerInactivityTimeoutMilliseconds = 600000;

    /// <summary>The default connection timeout, in milliseconds.</summary>
    public const int DefaultConnectTimeoutMilliseconds = 30000;

    /// <summary>The default stream read timeout, in milliseconds.</summary>
    public const int DefaultReadTimeoutMilliseconds = 30000;

    /// <summary>The default stream write timeout, in milliseconds.</summary>
    public const int DefaultWriteTimeoutMilliseconds = 30000;

    /// <summary>The default authentication timeout, in milliseconds.</summary>
    public const int DefaultAuthenticationTimeoutMilliseconds = 30000;

    /// <summary>The default internal stream buffer size in bytes.</summary>
    public const int DefaultBufferSize = 8192;

    /// <summary>The default safety ceiling for an unbounded message line, in octets.</summary>
    public const int DefaultMaximumMessageLineOctets = 1048576;

    /// <summary>The default safety ceiling for one SASL payload line, in octets.</summary>
    public const int DefaultMaximumSaslLineOctets = 1048576;

    /// <summary>The default ceiling for a message-returning byte-array convenience API, in bytes.</summary>
    public const int DefaultMessageMemoryCeilingBytes = 67108864;
}
