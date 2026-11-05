using System.Security.Cryptography.X509Certificates;

namespace ftp;

/// <summary>
/// Supplies one explicitly configured server certificate for FTP-over-TLS handshakes.
/// </summary>
public class DefaultFtpCertificateProvider : IFtpCertificateProvider
{
    internal X509Certificate2 Certificate;

    /// <summary>
    /// Creates a provider from TLS options containing a server certificate with its private key.
    /// </summary>
    /// <param name="options">The validated TLS options containing the server certificate.</param>
    /// <exception cref="System.ArgumentNullException">The options are null.</exception>
    /// <exception cref="FtpConfigurationException">The options do not contain usable server certificate material.</exception>
    public DefaultFtpCertificateProvider(FtpTlsOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        FtpConfigurationValidator.ValidateTlsOptions(options);
        if (options.ServerCertificate == null || !options.ServerCertificate.HasPrivateKey)
        {
            throw new FtpConfigurationException(
                "The default FTP certificate provider requires a server certificate with its private key.",
                null);
        }

        Certificate = options.ServerCertificate;
    }

    /// <summary>
    /// Selects the configured certificate after validating the supplied handshake context.
    /// </summary>
    /// <param name="request">The virtual-host, SNI, endpoint, and TLS context.</param>
    /// <returns>A handled selection result that succeeds only when the request and configured certificate are usable.</returns>
    public FtpCertificateResult SelectServerCertificate(FtpCertificateRequest request)
    {
        if (request == null)
        {
            return CreateFailure("A server certificate request is required.");
        }
        if (request.HostName == null ||
            FtpConfigurationValidator.HasInvalidText(request.HostName))
        {
            return CreateFailure(
                "The server certificate HOST context is invalid.");
        }
        if (request.ServerName != null &&
            (string.IsNullOrWhiteSpace(request.ServerName) ||
                FtpConfigurationValidator.HasInvalidText(request.ServerName)))
        {
            return CreateFailure(
                "The server certificate SNI context is invalid.");
        }
        FtpFaultIsolationResult keyInspection = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return Certificate.HasPrivateKey;
        });
        if (!keyInspection.Succeeded || keyInspection.Value == null ||
            !(bool)keyInspection.Value)
        {
            return CreateFailure(
                "The configured server certificate no longer has an available private key.");
        }

        FtpCertificateResult result = new FtpCertificateResult();
        result.Handled = true;
        result.Succeeded = true;
        result.Certificate = Certificate;
        result.Intermediates = new X509Certificate2[0];
        return result;
    }

    internal FtpCertificateResult CreateFailure(string message)
    {
        FtpCertificateResult result = new FtpCertificateResult();
        result.Handled = true;
        result.Succeeded = false;
        result.FailureMessage = message;
        return result;
    }
}
