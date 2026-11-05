using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ftp;

/// <summary>
/// Configures strict FTP-over-TLS behavior and certificate material.
/// </summary>
public class FtpTlsOptions
{
    /// <summary>
    /// The allowed TLS protocols, or <see cref="SslProtocols.None"/> for system defaults.
    /// </summary>
    public SslProtocols Protocols;

    /// <summary>
    /// Indicates that certificate revocation is checked.
    /// </summary>
    public bool CheckCertificateRevocation;

    /// <summary>
    /// Indicates that the remote certificate name must match the target host.
    /// </summary>
    public bool RequireCertificateNameMatch;

    /// <summary>
    /// Indicates that the remote certificate chain must be trusted.
    /// </summary>
    public bool RequireCertificateChainTrust;

    /// <summary>
    /// Indicates that a server requires a client certificate.
    /// </summary>
    public bool RequireClientCertificate;

    /// <summary>
    /// Indicates that protected data connections must reuse a TLS session when supported.
    /// </summary>
    public bool RequireDataSessionReuse;

    /// <summary>
    /// Indicates that CCC may clear a protected control connection.
    /// </summary>
    public bool AllowClearCommandChannel;

    /// <summary>
    /// The default data-channel protection level after TLS negotiation.
    /// </summary>
    public FtpDataProtectionLevel DataProtectionLevel;

    /// <summary>
    /// The optional target host used for certificate validation and SNI.
    /// </summary>
    public string? TargetHost;

    /// <summary>
    /// The optional client certificate.
    /// </summary>
    public X509Certificate2? ClientCertificate;

    /// <summary>
    /// The optional server certificate with its private key.
    /// </summary>
    public X509Certificate2? ServerCertificate;

    /// <summary>
    /// Creates strict TLS options using system protocol selection.
    /// </summary>
    public FtpTlsOptions()
    {
        Protocols = SslProtocols.None;
        CheckCertificateRevocation = true;
        RequireCertificateNameMatch = true;
        RequireCertificateChainTrust = true;
        RequireClientCertificate = false;
        RequireDataSessionReuse = false;
        AllowClearCommandChannel = FtpDefaults.AllowClearCommandChannel;
        DataProtectionLevel = FtpDataProtectionLevel.Private;
        TargetHost = null;
        ClientCertificate = null;
        ServerCertificate = null;
    }
}

