using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ftp.Tests;

public class DefaultStrictCertificateTests
{
    [Test]
    public void Default_provider_returns_the_explicitly_configured_private_key_certificate()
    {
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        FtpTlsOptions options = new FtpTlsOptions();
        options.ServerCertificate = certificate;
        DefaultFtpCertificateProvider provider =
            new DefaultFtpCertificateProvider(options);
        options.ServerCertificate = null;
        FtpCertificateRequest request = CreateSelectionRequest();

        FtpCertificateResult result = provider.SelectServerCertificate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Certificate, Is.SameAs(certificate));
            Assert.That(result.Intermediates, Is.Empty);
            Assert.That(result.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Default_provider_rejects_missing_key_material_and_invalid_requests()
    {
        FtpTlsOptions missing = new FtpTlsOptions();
        using X509Certificate2 publicOnly =
            FtpClientCertificatePolicyTests.LoadCertificate();
        FtpTlsOptions publicOnlyOptions = new FtpTlsOptions();
        publicOnlyOptions.ServerCertificate = publicOnly;
        using X509Certificate2 certificate = FtpTlsStreamTests.CreateCertificate();
        FtpTlsOptions valid = new FtpTlsOptions();
        valid.ServerCertificate = certificate;
        DefaultFtpCertificateProvider provider =
            new DefaultFtpCertificateProvider(valid);
        FtpCertificateRequest invalidHost = CreateSelectionRequest();
        invalidHost.HostName = "bad\rhost";
        FtpCertificateRequest invalidServerName = CreateSelectionRequest();
        invalidServerName.ServerName = " ";

        FtpCertificateResult missingRequest =
            provider.SelectServerCertificate(null!);
        FtpCertificateResult badHost =
            provider.SelectServerCertificate(invalidHost);
        FtpCertificateResult badServerName =
            provider.SelectServerCertificate(invalidServerName);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultFtpCertificateProvider(null!));
            Assert.Throws<FtpConfigurationException>(() =>
                new DefaultFtpCertificateProvider(missing));
            Assert.Throws<FtpConfigurationException>(() =>
                new DefaultFtpCertificateProvider(publicOnlyOptions));
            AssertRejectedSelection(missingRequest);
            AssertRejectedSelection(badHost);
            AssertRejectedSelection(badServerName);
        });
    }

    [Test]
    public void Public_certificate_operations_fail_closed_after_certificate_disposal()
    {
        X509Certificate2 serverCertificate = FtpTlsStreamTests.CreateCertificate();
        FtpTlsOptions options = new FtpTlsOptions();
        options.ServerCertificate = serverCertificate;
        DefaultFtpCertificateProvider provider =
            new DefaultFtpCertificateProvider(options);
        serverCertificate.Dispose();

        FtpCertificateResult selection =
            provider.SelectServerCertificate(CreateSelectionRequest());

        X509Certificate2 remoteCertificate =
            FtpClientCertificatePolicyTests.LoadCertificate();
        using X509Chain chain = CreateTrustedChain(remoteCertificate);
        FtpCertificateValidationRequest validationRequest =
            CreateValidationRequest(
                remoteCertificate, chain, SslPolicyErrors.None);
        remoteCertificate.Dispose();
        StrictFtpCertificateValidator validator =
            new StrictFtpCertificateValidator();

        FtpCertificateValidationResult validation =
            validator.ValidateRemoteCertificate(validationRequest);

        Assert.Multiple(() =>
        {
            AssertRejectedSelection(selection);
            AssertRejectedValidation(validation);
        });
    }

    [Test]
    public void Strict_validator_accepts_a_matching_current_certificate_with_a_clean_chain()
    {
        using X509Certificate2 certificate =
            FtpClientCertificatePolicyTests.LoadCertificate();
        using X509Chain chain = CreateTrustedChain(certificate);
        StrictFtpCertificateValidator validator =
            new StrictFtpCertificateValidator();
        FtpCertificateValidationRequest request =
            CreateValidationRequest(certificate, chain, SslPolicyErrors.None);

        FtpCertificateValidationResult result =
            validator.ValidateRemoteCertificate(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.FailureMessage, Is.Null);
        });
    }

    [Test]
    public void Strict_validator_rejects_every_platform_certificate_error()
    {
        using X509Certificate2 certificate =
            FtpClientCertificatePolicyTests.LoadCertificate();
        using X509Chain chain = CreateTrustedChain(certificate);
        StrictFtpCertificateValidator validator =
            new StrictFtpCertificateValidator();

        FtpCertificateValidationResult missing = validator.ValidateRemoteCertificate(
            CreateValidationRequest(
                null, chain, SslPolicyErrors.RemoteCertificateNotAvailable));
        FtpCertificateValidationResult nameMismatch = validator.ValidateRemoteCertificate(
            CreateValidationRequest(
                certificate, chain, SslPolicyErrors.RemoteCertificateNameMismatch));
        FtpCertificateValidationResult chainError = validator.ValidateRemoteCertificate(
            CreateValidationRequest(
                certificate, chain, SslPolicyErrors.RemoteCertificateChainErrors));
        FtpCertificateValidationResult combined = validator.ValidateRemoteCertificate(
            CreateValidationRequest(
                certificate,
                chain,
                SslPolicyErrors.RemoteCertificateNameMismatch |
                    SslPolicyErrors.RemoteCertificateChainErrors));

        Assert.Multiple(() =>
        {
            AssertRejectedValidation(missing);
            AssertRejectedValidation(nameMismatch);
            AssertRejectedValidation(chainError);
            AssertRejectedValidation(combined);
        });
    }

    [Test]
    public void Strict_validator_rejects_invalid_context_missing_chain_and_untrusted_chain()
    {
        using X509Certificate2 certificate =
            FtpClientCertificatePolicyTests.LoadCertificate();
        using X509Chain untrustedChain = new X509Chain();
        untrustedChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        untrustedChain.Build(certificate);
        StrictFtpCertificateValidator validator =
            new StrictFtpCertificateValidator();
        FtpCertificateValidationRequest invalidTarget =
            CreateValidationRequest(certificate, untrustedChain, SslPolicyErrors.None);
        invalidTarget.TargetHost = "bad\nhost";

        FtpCertificateValidationResult nullRequest =
            validator.ValidateRemoteCertificate(null!);
        FtpCertificateValidationResult badTarget =
            validator.ValidateRemoteCertificate(invalidTarget);
        FtpCertificateValidationResult missingChain =
            validator.ValidateRemoteCertificate(
                CreateValidationRequest(certificate, null, SslPolicyErrors.None));
        FtpCertificateValidationResult untrusted =
            validator.ValidateRemoteCertificate(
                CreateValidationRequest(certificate, untrustedChain, SslPolicyErrors.None));

        Assert.Multiple(() =>
        {
            AssertRejectedValidation(nullRequest);
            AssertRejectedValidation(badTarget);
            AssertRejectedValidation(missingChain);
            AssertRejectedValidation(untrusted);
        });
    }

    [Test]
    public void Strict_validator_integrates_with_client_policy_without_overriding_errors()
    {
        using X509Certificate2 certificate =
            FtpClientCertificatePolicyTests.LoadCertificate();
        using X509Chain chain = CreateTrustedChain(certificate);
        FtpTlsOptions options = new FtpTlsOptions();
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = "files.example.test";
        endpoint.Port = 21;
        FtpClientCertificatePolicy acceptedPolicy =
            new FtpClientCertificatePolicy(
                endpoint, options, new StrictFtpCertificateValidator());
        FtpClientCertificatePolicy rejectedPolicy =
            new FtpClientCertificatePolicy(
                endpoint, options, new StrictFtpCertificateValidator());

        bool accepted = acceptedPolicy.ValidateServerCertificate(
            acceptedPolicy, certificate, chain, SslPolicyErrors.None);
        bool rejected = rejectedPolicy.ValidateServerCertificate(
            rejectedPolicy,
            certificate,
            chain,
            SslPolicyErrors.RemoteCertificateNameMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(acceptedPolicy.CustomHandled, Is.True);
            Assert.That(acceptedPolicy.CustomAccepted, Is.True);
            Assert.That(rejected, Is.False);
            Assert.That(rejectedPolicy.CustomHandled, Is.True);
            Assert.That(rejectedPolicy.CustomAccepted, Is.False);
            Assert.That(rejectedPolicy.FailureMessage, Does.Contain("target host"));
        });
    }

    private static FtpCertificateRequest CreateSelectionRequest()
    {
        FtpCertificateRequest request = new FtpCertificateRequest();
        request.HostName = "files.example.test";
        request.ServerName = "files.example.test";
        request.ImplicitTls = true;
        return request;
    }

    private static FtpCertificateValidationRequest CreateValidationRequest(
        X509Certificate2? certificate, X509Chain? chain,
        SslPolicyErrors policyErrors)
    {
        FtpCertificateValidationRequest request =
            new FtpCertificateValidationRequest();
        request.TargetHost = "files.example.test";
        request.Certificate = certificate;
        request.Chain = chain;
        request.PolicyErrors = policyErrors;
        request.ServerCertificate = true;
        return request;
    }

    private static X509Chain CreateTrustedChain(X509Certificate2 certificate)
    {
        X509Chain chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(certificate);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        bool built = chain.Build(certificate);
        Assert.That(built, Is.True, "The test certificate must build under custom-root trust.");
        return chain;
    }

    private static void AssertRejectedSelection(FtpCertificateResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Certificate, Is.Null);
            Assert.That(result.FailureMessage, Is.Not.Empty);
        });
    }

    private static void AssertRejectedValidation(
        FtpCertificateValidationResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FailureMessage, Is.Not.Empty);
        });
    }
}
