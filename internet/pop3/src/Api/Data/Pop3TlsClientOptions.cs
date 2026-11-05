namespace pop3;

/// <summary>Provides identity and certificate policy for a POP3 client TLS handshake.</summary>
public class Pop3TlsClientOptions
{
    /// <summary>The DNS host or service identity expected from the server certificate.</summary>
    public string TargetHost = "";

    /// <summary>The TLS protocol versions permitted for the handshake.</summary>
    public System.Security.Authentication.SslProtocols EnabledProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;

    /// <summary>Indicates whether certificate revocation information must be checked.</summary>
    public bool CheckCertificateRevocation = true;

    /// <summary>The optional client certificates available for mutual TLS authentication.</summary>
    public System.Security.Cryptography.X509Certificates.X509Certificate2Collection ClientCertificates = new System.Security.Cryptography.X509Certificates.X509Certificate2Collection();

    /// <summary>Indicates whether disposing the TLS stream must leave the underlying transport open.</summary>
    public bool LeaveInnerStreamOpen;
}
