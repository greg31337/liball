using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ftp;

internal class FtpClientCertificatePolicy
{
    internal string TargetHost;
    internal FtpEndpoint RemoteEndpoint;
    internal bool RequireNameMatch;
    internal bool RequireChainTrust;
    internal X509RevocationMode RevocationMode;
    internal X509Certificate2? ClientCertificate;
    internal IFtpCertificateValidator? Validator;
    internal int ValidationCalls;
    internal bool ValidationAttempted;
    internal bool Accepted;
    internal bool CustomHandled;
    internal bool CustomAccepted;
    internal SslPolicyErrors PolicyErrors;
    internal string? FailureMessage;
    internal Exception? ValidatorFailure;

    internal FtpClientCertificatePolicy(
        FtpEndpoint? remoteEndpoint,
        FtpTlsOptions? options,
        IFtpCertificateValidator? validator)
    {
        if (remoteEndpoint == null || options == null)
        {
            throw new FtpConfigurationException(
                "A remote endpoint and TLS options are required for client certificate policy.", null);
        }

        FtpConfigurationValidator.ValidateRemoteEndpoint(remoteEndpoint);
        FtpConfigurationValidator.ValidateTlsOptions(options);
        TargetHost = ResolveTargetHost(remoteEndpoint, options);
        RemoteEndpoint = CopyEndpoint(remoteEndpoint);
        RequireNameMatch = options.RequireCertificateNameMatch;
        RequireChainTrust = options.RequireCertificateChainTrust;
        RevocationMode = options.CheckCertificateRevocation ?
            X509RevocationMode.Online : X509RevocationMode.NoCheck;
        ClientCertificate = options.ClientCertificate;
        Validator = validator;
        ValidationCalls = 0;
        ValidationAttempted = false;
        Accepted = false;
        CustomHandled = false;
        CustomAccepted = false;
        PolicyErrors = SslPolicyErrors.None;
        FailureMessage = null;
        ValidatorFailure = null;
    }

    internal RemoteCertificateValidationCallback CreateValidationCallback()
    {
        return ValidateServerCertificate;
    }

    internal bool ValidateServerCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors policyErrors)
    {
        ValidationCalls++;
        ValidationAttempted = true;
        Accepted = false;
        CustomHandled = false;
        CustomAccepted = false;
        PolicyErrors = policyErrors;
        FailureMessage = null;
        ValidatorFailure = null;

        X509Certificate2? certificateValue = certificate as X509Certificate2;
        if (certificateValue == null ||
            (policyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0)
        {
            FailureMessage = "The FTP TLS server did not provide a usable certificate.";
            return false;
        }

        FtpCertificateValidationRequest request = new FtpCertificateValidationRequest();
        request.TargetHost = TargetHost;
        request.Certificate = certificateValue;
        request.Chain = chain;
        request.PolicyErrors = policyErrors;
        request.RemoteEndpoint = CopyEndpoint(RemoteEndpoint);
        request.ServerCertificate = true;

        if (Validator != null)
        {
            FtpFaultIsolationResult validation = FtpFaultIsolationRunner.RunOperation(() =>
            {
                return Validator.ValidateRemoteCertificate(request);
            });
            if (!validation.Succeeded)
            {
                ValidatorFailure = validation.Failure;
                FailureMessage = "The custom FTP certificate validator failed.";
                return false;
            }

            FtpCertificateValidationResult? customResult =
                validation.Value as FtpCertificateValidationResult;
            if (customResult == null)
            {
                FailureMessage = "The custom FTP certificate validator returned no decision.";
                return false;
            }
            if (customResult.Handled)
            {
                CustomHandled = true;
                CustomAccepted = customResult.Accepted;
                Accepted = customResult.Accepted;
                if (!Accepted)
                {
                    FailureMessage = string.IsNullOrWhiteSpace(customResult.FailureMessage) ?
                        "The custom FTP certificate validator rejected the server certificate." :
                        customResult.FailureMessage;
                }
                return Accepted;
            }
        }

        SslPolicyErrors requiredErrors = policyErrors;
        if (!RequireNameMatch)
        {
            requiredErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;
        }
        if (!RequireChainTrust)
        {
            requiredErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
        }

        Accepted = requiredErrors == SslPolicyErrors.None;
        if (!Accepted)
        {
            FailureMessage = CreatePlatformFailureMessage(requiredErrors);
        }
        return Accepted;
    }

    internal static string ResolveTargetHost(
        FtpEndpoint remoteEndpoint,
        FtpTlsOptions options)
    {
        string targetHost = options.TargetHost == null ?
            remoteEndpoint.Host : options.TargetHost;
        if (string.IsNullOrWhiteSpace(targetHost) ||
            FtpConfigurationValidator.HasInvalidText(targetHost))
        {
            throw new FtpConfigurationException(
                "A TLS validation and SNI target host is required.", null);
        }

        if (targetHost.Length > 2 && targetHost[0] == '[' &&
            targetHost[targetHost.Length - 1] == ']')
        {
            return targetHost.Substring(1, targetHost.Length - 2);
        }
        return targetHost;
    }

    internal static string CreatePlatformFailureMessage(SslPolicyErrors errors)
    {
        bool nameMismatch =
            (errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0;
        bool chainErrors =
            (errors & SslPolicyErrors.RemoteCertificateChainErrors) != 0;
        if (nameMismatch && chainErrors)
        {
            return "The FTP TLS server certificate name and trust chain are invalid.";
        }
        if (nameMismatch)
        {
            return "The FTP TLS server certificate name does not match the target host.";
        }
        if (chainErrors)
        {
            return "The FTP TLS server certificate trust chain is invalid.";
        }
        return "The FTP TLS server certificate failed platform validation.";
    }

    internal static FtpEndpoint CopyEndpoint(FtpEndpoint endpoint)
    {
        FtpEndpoint copy = new FtpEndpoint();
        copy.Host = endpoint.Host;
        copy.Port = endpoint.Port;
        copy.AddressFamily = endpoint.AddressFamily;
        return copy;
    }
}
