namespace dns;

/// <summary>Defines the standard ports used by DNS transports.</summary>
public static class DnsPorts
{
    /// <summary>The standard UDP and TCP port for DNS.</summary>
    public const int Dns = 53;

    /// <summary>The standard TCP port for DNS over TLS.</summary>
    public const int DnsOverTls = 853;

    /// <summary>The default TCP port for DNS over HTTPS.</summary>
    public const int DnsOverHttps = 443;

    /// <summary>The standard UDP port for DNS over QUIC.</summary>
    public const int DnsOverQuic = 853;
}
