namespace ftp;

/// <summary>
/// Applies custom remote-certificate validation without weakening platform checks by default.
/// </summary>
public interface IFtpCertificateValidator
{
    /// <summary>
    /// Validates one remote certificate and its platform diagnostics.
    /// </summary>
    /// <param name="request">The target host, certificate, chain, and policy errors.</param>
    /// <returns>The exception-less validation decision.</returns>
    FtpCertificateValidationResult ValidateRemoteCertificate(FtpCertificateValidationRequest request);
}

