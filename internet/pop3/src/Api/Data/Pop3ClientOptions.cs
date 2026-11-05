namespace pop3;

/// <summary>Provides options for establishing a POP3 client session.</summary>
public class Pop3ClientOptions
{
    /// <summary>The remote POP3 host name.</summary>
    public string Host = "";

    /// <summary>The remote TCP port.</summary>
    public int Port = Pop3Defaults.DefaultPort;

    /// <summary>The transport security policy to use for the connection.</summary>
    public Pop3SecurityMode SecurityMode;

    /// <summary>The maximum connection time in milliseconds.</summary>
    public int ConnectTimeoutMilliseconds = Pop3Defaults.DefaultConnectTimeoutMilliseconds;

    /// <summary>The maximum stream read time in milliseconds.</summary>
    public int ReadTimeoutMilliseconds = Pop3Defaults.DefaultReadTimeoutMilliseconds;

    /// <summary>The maximum stream write time in milliseconds.</summary>
    public int WriteTimeoutMilliseconds = Pop3Defaults.DefaultWriteTimeoutMilliseconds;

    /// <summary>The maximum authentication exchange time in milliseconds.</summary>
    public int AuthenticationTimeoutMilliseconds = Pop3Defaults.DefaultAuthenticationTimeoutMilliseconds;

    /// <summary>The internal transport buffer size in bytes.</summary>
    public int BufferSize = Pop3Defaults.DefaultBufferSize;

    /// <summary>The maximum accepted unbounded message-line size in octets.</summary>
    public int MaximumMessageLineOctets = Pop3Defaults.DefaultMaximumMessageLineOctets;

    /// <summary>The maximum accepted SASL payload-line size in octets.</summary>
    public int MaximumSaslLineOctets = Pop3Defaults.DefaultMaximumSaslLineOctets;

    /// <summary>The maximum byte-array message result size in bytes.</summary>
    public int MessageMemoryCeilingBytes = Pop3Defaults.DefaultMessageMemoryCeilingBytes;

    /// <summary>Indicates whether advertised command pipelining may be used.</summary>
    public bool EnablePipelining;

    /// <summary>Indicates whether pipelining may be used without a server advertisement.</summary>
    public bool ForcePipelining;

    /// <summary>Indicates whether USER support may be probed when CAPA is unavailable.</summary>
    public bool ProbeUserWhenCapaUnavailable;

    /// <summary>Indicates whether TOP support may be probed when CAPA is unavailable.</summary>
    public bool ProbeTopWhenCapaUnavailable;

    /// <summary>Indicates whether UIDL support may be probed when CAPA is unavailable.</summary>
    public bool ProbeUidlWhenCapaUnavailable;

    /// <summary>The authentication request and selection policy.</summary>
    public Pop3AuthenticationRequest Authentication = new Pop3AuthenticationRequest();

    /// <summary>The client TLS handshake options.</summary>
    public Pop3TlsClientOptions Tls = new Pop3TlsClientOptions();

    /// <summary>A custom TLS provider, or <c>null</c> to use the library default.</summary>
    public IPop3ClientTlsProvider? TlsProvider;

    /// <summary>A protocol logger, or <c>null</c> when protocol logging is disabled.</summary>
    public IPop3ProtocolLogger? Logger;

    /// <summary>A transfer observer, or <c>null</c> when progress reports are not required.</summary>
    public IPop3TransferObserver? TransferObserver;

    /// <summary>Indicates whether a caller-supplied connected stream remains caller-owned.</summary>
    public bool LeaveStreamOpen;
}
