using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace ftp.Tests;

public class FtpServerCertificatePolicyTests
{
    [Test]
    public void Implicit_selection_supplies_SNI_endpoints_intermediates_and_mutual_TLS_options()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        using X509Certificate2 intermediate =
            FtpClientCertificatePolicyTests.LoadCertificate();
        FtpCertificateResult result = CreateSuccessfulResult(certificate);
        result.Intermediates = new[] { intermediate };
        RecordingProvider provider = new RecordingProvider(result, false);
        FtpTlsOptions options = new FtpTlsOptions();
        options.Protocols = SslProtocols.Tls12;
        options.CheckCertificateRevocation = true;
        options.RequireClientCertificate = true;
        FtpEndpoint local = CreateEndpoint("192.0.2.10", 990);
        FtpEndpoint remote = CreateEndpoint("198.51.100.20", 50123);
        FtpServerCertificatePolicy policy = new FtpServerCertificatePolicy(
            provider,
            "default.example.test",
            local,
            remote,
            true,
            options,
            null);
        local.Host = "changed-local";
        remote.Host = "changed-remote";
        options.RequireClientCertificate = false;
        using SslStream stream = new SslStream(new MemoryStream());

        SslServerAuthenticationOptions authentication = policy.SelectServerOptions(
            stream,
            new SslClientHelloInfo("tenant.example.test", SslProtocols.Tls12),
            policy,
            default(CancellationToken)).GetAwaiter().GetResult();
        result.Intermediates = new X509Certificate2[0];

        Assert.Multiple(() =>
        {
            Assert.That(provider.Request, Is.Not.Null);
            Assert.That(provider.Request!.HostName,
                Is.EqualTo("default.example.test"));
            Assert.That(provider.Request.ServerName,
                Is.EqualTo("tenant.example.test"));
            Assert.That(provider.Request.LocalEndpoint!.Host,
                Is.EqualTo("192.0.2.10"));
            Assert.That(provider.Request.RemoteEndpoint!.Host,
                Is.EqualTo("198.51.100.20"));
            Assert.That(provider.Request.ImplicitTls, Is.True);
            Assert.That(provider.Request.RequestClientCertificate, Is.True);
            Assert.That(authentication.ServerCertificateContext, Is.Not.Null);
            Assert.That(authentication.ServerCertificateContext!.TargetCertificate,
                Is.SameAs(certificate));
            Assert.That(authentication.ClientCertificateRequired, Is.True);
            Assert.That(authentication.EnabledSslProtocols,
                Is.EqualTo(SslProtocols.Tls12));
            Assert.That(authentication.CertificateRevocationCheckMode,
                Is.EqualTo(X509RevocationMode.Online));
            Assert.That(authentication.RemoteCertificateValidationCallback,
                Is.Not.Null);
            Assert.That(policy.SelectedIntermediates,
                Is.EqualTo(new[] { intermediate }));
            Assert.That(policy.SelectionSucceeded, Is.True);
            Assert.That(policy.SelectionCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void Explicit_selection_preserves_HOST_context_separately_from_TLS_SNI()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        RecordingProvider provider = new RecordingProvider(
            CreateSuccessfulResult(certificate), false);
        FtpServerCertificatePolicy policy = new FtpServerCertificatePolicy(
            provider,
            "selected-host.example.test",
            null,
            null,
            false,
            new FtpTlsOptions(),
            null);
        using SslStream stream = new SslStream(new MemoryStream());

        policy.SelectServerOptions(
            stream,
            new SslClientHelloInfo("tls-name.example.test", SslProtocols.Tls13),
            policy,
            default(CancellationToken)).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(provider.Request!.HostName,
                Is.EqualTo("selected-host.example.test"));
            Assert.That(provider.Request.ServerName,
                Is.EqualTo("tls-name.example.test"));
            Assert.That(provider.Request.ImplicitTls, Is.False);
            Assert.That(provider.Request.RequestClientCertificate, Is.False);
        });
    }

    [Test]
    public void Provider_unhandled_rejection_missing_material_and_faults_fail_closed()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        using X509Certificate2 publicOnly =
            FtpClientCertificatePolicyTests.LoadCertificate();
        FtpCertificateResult unhandledResult = new FtpCertificateResult();
        FtpCertificateResult rejectedResult = new FtpCertificateResult();
        rejectedResult.Handled = true;
        rejectedResult.FailureMessage = "No certificate for this host.";
        FtpCertificateResult missingKeyResult = CreateSuccessfulResult(publicOnly);
        FtpCertificateResult missingIntermediates = CreateSuccessfulResult(certificate);
        missingIntermediates.Intermediates = null!;
        FtpServerCertificatePolicy unhandled = CreatePolicy(
            new RecordingProvider(unhandledResult, false));
        FtpServerCertificatePolicy rejected = CreatePolicy(
            new RecordingProvider(rejectedResult, false));
        FtpServerCertificatePolicy missingKey = CreatePolicy(
            new RecordingProvider(missingKeyResult, false));
        FtpServerCertificatePolicy missingChain = CreatePolicy(
            new RecordingProvider(missingIntermediates, false));
        FtpServerCertificatePolicy faulted = CreatePolicy(
            new RecordingProvider(CreateSuccessfulResult(certificate), true));

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpTlsException>(() => Select(unhandled));
            Assert.That(unhandled.FailureMessage, Does.Contain("did not handle"));
            Assert.Throws<FtpTlsException>(() => Select(rejected));
            Assert.That(rejected.FailureMessage,
                Is.EqualTo("No certificate for this host."));
            Assert.Throws<FtpTlsException>(() => Select(missingKey));
            Assert.That(missingKey.FailureMessage, Does.Contain("private key"));
            Assert.Throws<FtpTlsException>(() => Select(missingChain));
            Assert.That(missingChain.FailureMessage, Does.Contain("intermediates"));
            Assert.Throws<FtpTlsException>(() => Select(faulted));
            Assert.That(faulted.ProviderFailure,
                Is.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void Required_client_certificate_is_enforced_and_custom_validation_is_fault_isolated()
    {
        using X509Certificate2 certificate =
            FtpClientCertificatePolicyTests.LoadCertificate();
        FtpTlsOptions requiredOptions = new FtpTlsOptions();
        requiredOptions.RequireClientCertificate = true;
        FtpServerCertificatePolicy strict = new FtpServerCertificatePolicy(
            new RecordingProvider(new FtpCertificateResult(), false),
            "host",
            null,
            null,
            false,
            requiredOptions,
            null);
        FtpServerCertificatePolicy custom = new FtpServerCertificatePolicy(
            new RecordingProvider(new FtpCertificateResult(), false),
            "host",
            null,
            null,
            false,
            requiredOptions,
            (sender, remote, chain, errors) => true);
        FtpServerCertificatePolicy faulted = new FtpServerCertificatePolicy(
            new RecordingProvider(new FtpCertificateResult(), false),
            "host",
            null,
            null,
            false,
            requiredOptions,
            (sender, remote, chain, errors) =>
                throw new InvalidOperationException("client validator fault"));

        bool missing = strict.ValidateClientCertificate(
            strict, null, null, SslPolicyErrors.RemoteCertificateNotAvailable);
        bool valid = strict.ValidateClientCertificate(
            strict, certificate, null, SslPolicyErrors.None);
        bool invalidChain = strict.ValidateClientCertificate(
            strict, certificate, null, SslPolicyErrors.RemoteCertificateChainErrors);
        bool customAccepted = custom.ValidateClientCertificate(
            custom, certificate, null, SslPolicyErrors.RemoteCertificateChainErrors);
        bool faultAccepted = faulted.ValidateClientCertificate(
            faulted, certificate, null, SslPolicyErrors.None);

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.False);
            Assert.That(valid, Is.True);
            Assert.That(invalidChain, Is.False);
            Assert.That(customAccepted, Is.True);
            Assert.That(faultAccepted, Is.False);
            Assert.That(faulted.ClientValidatorFailure,
                Is.TypeOf<InvalidOperationException>());
        });
    }

    [Test]
    public void Invalid_provider_context_is_rejected_before_TLS_selection()
    {
        RecordingProvider provider = new RecordingProvider(
            new FtpCertificateResult(), false);
        FtpEndpoint invalidRemote = CreateEndpoint("", 21);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpServerCertificatePolicy(
                    null, "host", null, null, false,
                    new FtpTlsOptions(), null));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpServerCertificatePolicy(
                    provider, "bad\rhost", null, null, false,
                    new FtpTlsOptions(), null));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpServerCertificatePolicy(
                    provider, "host", null, invalidRemote, false,
                    new FtpTlsOptions(), null));
        });
    }

    private static FtpServerCertificatePolicy CreatePolicy(
        IFtpCertificateProvider provider)
    {
        return new FtpServerCertificatePolicy(
            provider,
            "host.example.test",
            null,
            null,
            true,
            new FtpTlsOptions(),
            null);
    }

    private static void Select(FtpServerCertificatePolicy policy)
    {
        using SslStream stream = new SslStream(new MemoryStream());
        policy.SelectServerOptions(
            stream,
            new SslClientHelloInfo("host.example.test", SslProtocols.Tls12),
            policy,
            default(CancellationToken)).GetAwaiter().GetResult();
    }

    private static FtpCertificateResult CreateSuccessfulResult(
        X509Certificate2 certificate)
    {
        FtpCertificateResult result = new FtpCertificateResult();
        result.Handled = true;
        result.Succeeded = true;
        result.Certificate = certificate;
        return result;
    }

    private static FtpEndpoint CreateEndpoint(string host, int port)
    {
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.AddressFamily = FtpAddressFamily.IPv4;
        return endpoint;
    }

    private sealed class RecordingProvider : IFtpCertificateProvider
    {
        internal FtpCertificateResult Result;
        internal bool ThrowFailure;
        internal FtpCertificateRequest? Request;

        internal RecordingProvider(
            FtpCertificateResult result,
            bool throwFailure)
        {
            Result = result;
            ThrowFailure = throwFailure;
            Request = null;
        }

        public FtpCertificateResult SelectServerCertificate(
            FtpCertificateRequest request)
        {
            Request = request;
            if (ThrowFailure)
            {
                throw new InvalidOperationException("provider fault");
            }
            return Result;
        }
    }
}
