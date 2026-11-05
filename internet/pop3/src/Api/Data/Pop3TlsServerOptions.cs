namespace pop3;

/// <summary>Provides certificate and protocol policy for a POP3 server TLS handshake.</summary>
public class Pop3TlsServerOptions
{
    /// <summary>The certificate presented by the server, or <c>null</c> until configured.</summary>
    public System.Security.Cryptography.X509Certificates.X509Certificate2? ServerCertificate;

    /// <summary>The TLS protocol versions permitted for the handshake.</summary>
    public System.Security.Authentication.SslProtocols EnabledProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;

    /// <summary>Indicates whether a client certificate is required.</summary>
    public bool RequireClientCertificate;

    /// <summary>Indicates whether client-certificate revocation information must be checked.</summary>
    public bool CheckCertificateRevocation = true;

    /// <summary>Indicates whether disposing the TLS stream must leave the underlying transport open.</summary>
    public bool LeaveInnerStreamOpen;
}
