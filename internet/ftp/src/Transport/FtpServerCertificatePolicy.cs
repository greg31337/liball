using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ftp;

internal class FtpServerCertificatePolicy
{
    internal IFtpCertificateProvider Provider;
    internal string HostName;
    internal FtpEndpoint? LocalEndpoint;
    internal FtpEndpoint? RemoteEndpoint;
    internal bool ImplicitTls;
    internal bool RequestClientCertificate;
    internal SslProtocols Protocols;
    internal X509RevocationMode RevocationMode;
    internal RemoteCertificateValidationCallback? ClientCertificateValidation;
    internal int SelectionCalls;
    internal string? ServerName;
    internal FtpCertificateResult? SelectionResult;
    internal X509Certificate2[] SelectedIntermediates;
    internal SslStreamCertificateContext? CertificateContext;
    internal bool ProviderHandled;
    internal bool SelectionSucceeded;
    internal string? FailureMessage;
    internal Exception? ProviderFailure;
    internal int ClientValidationCalls;
    internal bool ClientCertificateAccepted;
    internal Exception? ClientValidatorFailure;

    internal FtpServerCertificatePolicy(
        IFtpCertificateProvider? provider,
        string? hostName,
        FtpEndpoint? localEndpoint,
        FtpEndpoint? remoteEndpoint,
        bool implicitTls,
        FtpTlsOptions? options,
        RemoteCertificateValidationCallback? clientCertificateValidation)
    {
        if (provider == null || options == null)
        {
            throw new FtpConfigurationException(
                "A certificate provider and TLS options are required for server certificate policy.", null);
        }
        if (hostName == null || FtpConfigurationValidator.HasInvalidText(hostName))
        {
            throw new FtpConfigurationException(
                "A server certificate host context cannot contain control characters.", null);
        }

        FtpConfigurationValidator.ValidateTlsOptions(options);
        ValidateOptionalEndpoints(localEndpoint, remoteEndpoint);
        Provider = provider;
        HostName = hostName;
        LocalEndpoint = CopyOptionalEndpoint(localEndpoint);
        RemoteEndpoint = CopyOptionalEndpoint(remoteEndpoint);
        ImplicitTls = implicitTls;
        RequestClientCertificate = options.RequireClientCertificate;
        Protocols = options.Protocols;
        RevocationMode = options.CheckCertificateRevocation ?
            X509RevocationMode.Online : X509RevocationMode.NoCheck;
        ClientCertificateValidation = clientCertificateValidation;
        SelectionCalls = 0;
        ServerName = null;
        SelectionResult = null;
        SelectedIntermediates = new X509Certificate2[0];
        CertificateContext = null;
        ProviderHandled = false;
        SelectionSucceeded = false;
        FailureMessage = null;
        ProviderFailure = null;
        ClientValidationCalls = 0;
        ClientCertificateAccepted = false;
        ClientValidatorFailure = null;
    }

    internal ValueTask<SslServerAuthenticationOptions> SelectServerOptions(
        SslStream stream,
        SslClientHelloInfo clientHelloInfo,
        object? state,
        CancellationToken cancellationToken)
    {
        FtpCancellation.ThrowIfCancellationRequested(cancellationToken);
        SelectionCalls++;
        ServerName = string.IsNullOrEmpty(clientHelloInfo.ServerName) ?
            null : clientHelloInfo.ServerName;
        SelectionResult = null;
        SelectedIntermediates = new X509Certificate2[0];
        CertificateContext = null;
        ProviderHandled = false;
        SelectionSucceeded = false;
        FailureMessage = null;
        ProviderFailure = null;

        FtpCertificateRequest request = new FtpCertificateRequest();
        request.HostName = HostName;
        request.ServerName = ServerName;
        request.LocalEndpoint = CopyOptionalEndpoint(LocalEndpoint);
        request.RemoteEndpoint = CopyOptionalEndpoint(RemoteEndpoint);
        request.ImplicitTls = ImplicitTls;
        request.RequestClientCertificate = RequestClientCertificate;
        FtpFaultIsolationResult providerResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return Provider.SelectServerCertificate(request);
        });
        if (!providerResult.Succeeded)
        {
            ProviderFailure = providerResult.Failure;
            FailSelection("The FTP server certificate provider failed.", ProviderFailure);
        }

        FtpCertificateResult? selected = providerResult.Value as FtpCertificateResult;
        SelectionResult = selected;
        if (selected == null)
        {
            FailSelection("The FTP server certificate provider returned no result.", null);
        }
        if (selected == null)
        {
            throw new FtpTlsException(
                "The FTP server certificate provider returned no result.", null);
        }

        ProviderHandled = selected.Handled;
        if (!selected.Handled)
        {
            FailSelection("The FTP server certificate provider did not handle the request.", null);
        }
        if (!selected.Succeeded)
        {
            string message = string.IsNullOrWhiteSpace(selected.FailureMessage) ?
                "The FTP server certificate provider rejected the request." :
                selected.FailureMessage;
            FailSelection(message, null);
        }
        if (selected.Certificate == null || !selected.Certificate.HasPrivateKey)
        {
            FailSelection(
                "The selected FTP server certificate must include its private key.", null);
        }
        if (selected.Intermediates == null)
        {
            FailSelection(
                "The selected FTP server certificate intermediates are unavailable.", null);
        }
        if (selected.Certificate == null || selected.Intermediates == null)
        {
            throw new FtpTlsException(
                "The selected FTP server certificate material is incomplete.", null);
        }

        X509Certificate2Collection intermediates = new X509Certificate2Collection();
        int index = 0;
        while (index < selected.Intermediates.Length)
        {
            X509Certificate2 intermediate = selected.Intermediates[index];
            if (intermediate == null)
            {
                FailSelection(
                    "The selected FTP server certificate intermediates contain a missing certificate.", null);
                throw new FtpTlsException(
                    "The selected FTP server certificate intermediates contain a missing certificate.", null);
            }
            intermediates.Add(intermediate);
            index++;
        }
        SelectedIntermediates = CopyCertificates(selected.Intermediates);

        FtpFaultIsolationResult contextResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return SslStreamCertificateContext.Create(
                selected.Certificate, intermediates, true);
        });
        if (!contextResult.Succeeded)
        {
            FailSelection(
                "The selected FTP server certificate chain could not be prepared.",
                contextResult.Failure);
        }
        SslStreamCertificateContext? context =
            contextResult.Value as SslStreamCertificateContext;
        if (context == null)
        {
            FailSelection(
                "The selected FTP server certificate chain produced no TLS context.", null);
        }
        if (context == null)
        {
            throw new FtpTlsException(
                "The selected FTP server certificate chain produced no TLS context.", null);
        }

        CertificateContext = context;
        SelectionSucceeded = true;
        SslServerAuthenticationOptions authentication = CreateAuthenticationOptions(context);
        return new ValueTask<SslServerAuthenticationOptions>(authentication);
    }

    internal SslServerAuthenticationOptions CreateAuthenticationOptions(
        SslStreamCertificateContext context)
    {
        SslServerAuthenticationOptions authentication = new SslServerAuthenticationOptions();
        authentication.ServerCertificateContext = context;
        authentication.ClientCertificateRequired = RequestClientCertificate;
        authentication.EnabledSslProtocols = Protocols;
        authentication.CertificateRevocationCheckMode = RevocationMode;
        if (RequestClientCertificate || ClientCertificateValidation != null)
        {
            authentication.RemoteCertificateValidationCallback = ValidateClientCertificate;
        }
        return authentication;
    }

    internal bool ValidateClientCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors policyErrors)
    {
        ClientValidationCalls++;
        ClientCertificateAccepted = false;
        ClientValidatorFailure = null;
        if (RequestClientCertificate &&
            (certificate == null ||
                (policyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0))
        {
            return false;
        }
        if (ClientCertificateValidation == null)
        {
            ClientCertificateAccepted = policyErrors == SslPolicyErrors.None;
            return ClientCertificateAccepted;
        }

        FtpFaultIsolationResult validation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            return ClientCertificateValidation(sender, certificate, chain, policyErrors);
        });
        if (!validation.Succeeded)
        {
            ClientValidatorFailure = validation.Failure;
            return false;
        }
        if (validation.Value == null)
        {
            return false;
        }
        ClientCertificateAccepted = (bool)validation.Value;
        return ClientCertificateAccepted;
    }

    internal void FailSelection(string message, Exception? failure)
    {
        FailureMessage = message;
        SelectionSucceeded = false;
        throw new FtpTlsException(message, failure);
    }

    internal static void ValidateOptionalEndpoints(
        FtpEndpoint? localEndpoint,
        FtpEndpoint? remoteEndpoint)
    {
        if (localEndpoint != null)
        {
            FtpConfigurationValidator.ValidateListenerEndpoint(localEndpoint);
        }
        if (remoteEndpoint != null)
        {
            FtpConfigurationValidator.ValidateRemoteEndpoint(remoteEndpoint);
        }
    }

    internal static FtpEndpoint? CopyOptionalEndpoint(FtpEndpoint? endpoint)
    {
        if (endpoint == null)
        {
            return null;
        }
        FtpEndpoint copy = new FtpEndpoint();
        copy.Host = endpoint.Host;
        copy.Port = endpoint.Port;
        copy.AddressFamily = endpoint.AddressFamily;
        return copy;
    }

    internal static X509Certificate2[] CopyCertificates(X509Certificate2[] certificates)
    {
        X509Certificate2[] copy = new X509Certificate2[certificates.Length];
        int index = 0;
        while (index < certificates.Length)
        {
            copy[index] = certificates[index];
            index++;
        }
        return copy;
    }
}
