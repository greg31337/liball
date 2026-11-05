using System.Security.Cryptography.X509Certificates;

namespace ftp;

/// <summary>
/// Carries an exception-less server certificate selection result.
/// </summary>
public class FtpCertificateResult
{
    /// <summary>
    /// Indicates that a provider handled the certificate request.
    /// </summary>
    public bool Handled;

    /// <summary>
    /// Indicates that certificate selection succeeded.
    /// </summary>
    public bool Succeeded;

    /// <summary>
    /// The optional selected certificate with its private key.
    /// </summary>
    public X509Certificate2? Certificate;

    /// <summary>
    /// Optional intermediate certificates supplied with the selected certificate.
    /// </summary>
    public X509Certificate2[] Intermediates;

    /// <summary>
    /// The optional public failure description.
    /// </summary>
    public string? FailureMessage;

    /// <summary>
    /// Creates an unhandled result without certificate material.
    /// </summary>
    public FtpCertificateResult()
    {
        Handled = false;
        Succeeded = false;
        Certificate = null;
        Intermediates = new X509Certificate2[0];
        FailureMessage = null;
    }
}

