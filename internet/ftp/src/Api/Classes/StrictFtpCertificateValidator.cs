using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ftp;

/// <summary>
/// Enforces platform name, trust-chain, validity, and revocation decisions for remote certificates.
/// </summary>
public class StrictFtpCertificateValidator : IFtpCertificateValidator
{
    /// <summary>
    /// Creates a fail-closed certificate validator.
    /// </summary>
    public StrictFtpCertificateValidator()
    {
    }

    /// <summary>
    /// Accepts a remote certificate only when its target context and platform-built chain are fully valid.
    /// </summary>
    /// <param name="request">The target host, certificate, chain, and platform policy errors.</param>
    /// <returns>A handled strict validation decision.</returns>
    public FtpCertificateValidationResult ValidateRemoteCertificate(
        FtpCertificateValidationRequest request)
    {
        if (request == null)
        {
            return CreateRejection("A remote certificate validation request is required.");
        }

        FtpFaultIsolationResult inspection = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return InspectRemoteCertificate(request);
        });
        if (!inspection.Succeeded)
        {
            return CreateRejection(
                "The remote certificate material could not be inspected safely.");
        }
        FtpCertificateValidationResult? result =
            inspection.Value as FtpCertificateValidationResult;
        if (result == null)
        {
            return CreateRejection(
                "Strict certificate validation produced no decision.");
        }
        return result;
    }

    internal FtpCertificateValidationResult InspectRemoteCertificate(
        FtpCertificateValidationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetHost) ||
            FtpConfigurationValidator.HasInvalidText(request.TargetHost))
        {
            return CreateRejection(
                "A valid target host is required for strict certificate validation.");
        }
        if (request.Certificate == null ||
            (request.PolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
        {
            return CreateRejection(
                "The remote endpoint did not provide a usable certificate.");
        }
        if ((request.PolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
        {
            return CreateRejection(
                "The remote certificate name does not match the target host.");
        }
        if ((request.PolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
        {
            return CreateRejection(
                "The remote certificate trust chain or revocation status is invalid.");
        }
        if (request.PolicyErrors != SslPolicyErrors.None)
        {
            return CreateRejection(
                "The remote certificate failed platform validation.");
        }
        if (request.Chain == null || request.Chain.ChainElements.Count == 0)
        {
            return CreateRejection(
                "Strict certificate validation requires a platform-built trust chain.");
        }
        if (!request.Certificate.Equals(
            request.Chain.ChainElements[0].Certificate))
        {
            return CreateRejection(
                "The platform trust chain does not begin with the remote certificate.");
        }

        X509ChainStatus[] chainStatus = request.Chain.ChainStatus;
        int statusIndex = 0;
        while (statusIndex < chainStatus.Length)
        {
            if (chainStatus[statusIndex].Status != X509ChainStatusFlags.NoError)
            {
                return CreateRejection(
                    "The remote certificate trust chain contains a validation error.");
            }
            statusIndex++;
        }

        DateTime now = DateTime.UtcNow;
        if (DateTime.Compare(now, request.Certificate.NotBefore.ToUniversalTime()) < 0)
        {
            return CreateRejection(
                "The remote certificate is not valid yet.");
        }
        if (DateTime.Compare(now, request.Certificate.NotAfter.ToUniversalTime()) > 0)
        {
            return CreateRejection(
                "The remote certificate has expired.");
        }

        FtpCertificateValidationResult result = new FtpCertificateValidationResult();
        result.Handled = true;
        result.Accepted = true;
        return result;
    }

    internal FtpCertificateValidationResult CreateRejection(string message)
    {
        FtpCertificateValidationResult result = new FtpCertificateValidationResult();
        result.Handled = true;
        result.Accepted = false;
        result.FailureMessage = message;
        return result;
    }
}
