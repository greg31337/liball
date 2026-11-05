using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ftp;

/// <summary>
/// Carries remote certificate diagnostics to a certificate validator.
/// </summary>
public class FtpCertificateValidationRequest
{
    /// <summary>
    /// The expected DNS target host.
    /// </summary>
    public string TargetHost;

    /// <summary>
    /// The optional remote certificate.
    /// </summary>
    public X509Certificate2? Certificate;

    /// <summary>
    /// The optional platform-built certificate chain.
    /// </summary>
    public X509Chain? Chain;

    /// <summary>
    /// The platform certificate policy errors.
    /// </summary>
    public SslPolicyErrors PolicyErrors;

    /// <summary>
    /// The optional remote endpoint.
    /// </summary>
    public FtpEndpoint? RemoteEndpoint;

    /// <summary>
    /// Indicates that the certificate belongs to an FTP server rather than a client.
    /// </summary>
    public bool ServerCertificate;

    /// <summary>
    /// Creates an empty server-certificate validation request.
    /// </summary>
    public FtpCertificateValidationRequest()
    {
        TargetHost = "";
        Certificate = null;
        Chain = null;
        PolicyErrors = SslPolicyErrors.None;
        RemoteEndpoint = null;
        ServerCertificate = true;
    }
}

