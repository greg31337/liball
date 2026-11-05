namespace ftp;

/// <summary>
/// Selects server certificates for implicit or explicit FTP-over-TLS.
/// </summary>
public interface IFtpCertificateProvider
{
    /// <summary>
    /// Selects certificate material for one TLS handshake.
    /// </summary>
    /// <param name="request">The virtual-host, SNI, endpoint, and TLS context.</param>
    /// <returns>The exception-less certificate result.</returns>
    FtpCertificateResult SelectServerCertificate(FtpCertificateRequest request);
}

