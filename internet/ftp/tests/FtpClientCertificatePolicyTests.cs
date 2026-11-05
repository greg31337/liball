using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ftp.Tests;

public class FtpClientCertificatePolicyTests
{
    [Test]
    public void Configured_target_host_drives_SNI_and_validation_instead_of_the_connection_host()
    {
        FtpEndpoint endpoint = CreateEndpoint("control.example.test");
        FtpTlsOptions options = new FtpTlsOptions();
        options.TargetHost = "override.example.test";
        options.CheckCertificateRevocation = true;
        FtpTlsClientStreamRequest request = new FtpTlsClientStreamRequest();
        request.TransportStream = new MemoryStream();
        request.Options = options;
        request.RemoteEndpoint = endpoint;
        FtpTlsClientStream client = new FtpTlsClientStream(request);

        endpoint.Host = "mutated.example.test";
        options.TargetHost = "changed.example.test";
        options.CheckCertificateRevocation = false;
        SslClientAuthenticationOptions authentication = client.CreateAuthenticationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(client.CertificatePolicy, Is.Not.Null);
            Assert.That(client.CertificatePolicy!.TargetHost,
                Is.EqualTo("override.example.test"));
            Assert.That(client.CertificatePolicy.RemoteEndpoint.Host,
                Is.EqualTo("control.example.test"));
            Assert.That(authentication.TargetHost,
                Is.EqualTo("override.example.test"));
            Assert.That(authentication.RemoteCertificateValidationCallback, Is.Not.Null);
            Assert.That(authentication.CertificateRevocationCheckMode,
                Is.EqualTo(X509RevocationMode.Online));
        });
    }

    [Test]
    public void Endpoint_host_is_the_default_target_and_bracketed_IPV6_is_normalized()
    {
        FtpTlsOptions options = new FtpTlsOptions();
        FtpClientCertificatePolicy dns = new FtpClientCertificatePolicy(
            CreateEndpoint("files.example.test"), options, null);
        FtpClientCertificatePolicy ipv6 = new FtpClientCertificatePolicy(
            CreateEndpoint("[2001:db8::20]"), options, null);

        Assert.Multiple(() =>
        {
            Assert.That(dns.TargetHost, Is.EqualTo("files.example.test"));
            Assert.That(ipv6.TargetHost, Is.EqualTo("2001:db8::20"));
        });
    }

    [Test]
    public void Strict_platform_policy_accepts_only_a_present_name_matching_trusted_certificate()
    {
        using X509Certificate2 certificate = LoadCertificate();
        FtpClientCertificatePolicy accepted = CreatePolicy(new FtpTlsOptions(), null);
        FtpClientCertificatePolicy nameMismatch = CreatePolicy(new FtpTlsOptions(), null);
        FtpClientCertificatePolicy chainFailure = CreatePolicy(new FtpTlsOptions(), null);
        FtpClientCertificatePolicy combinedFailure = CreatePolicy(new FtpTlsOptions(), null);

        bool valid = accepted.ValidateServerCertificate(
            accepted, certificate, null, SslPolicyErrors.None);
        bool wrongName = nameMismatch.ValidateServerCertificate(
            nameMismatch, certificate, null,
            SslPolicyErrors.RemoteCertificateNameMismatch);
        bool untrusted = chainFailure.ValidateServerCertificate(
            chainFailure, certificate, null,
            SslPolicyErrors.RemoteCertificateChainErrors);
        bool combined = combinedFailure.ValidateServerCertificate(
            combinedFailure, certificate, null,
            SslPolicyErrors.RemoteCertificateNameMismatch |
                SslPolicyErrors.RemoteCertificateChainErrors);

        Assert.Multiple(() =>
        {
            Assert.That(valid, Is.True);
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(wrongName, Is.False);
            Assert.That(nameMismatch.FailureMessage, Does.Contain("name"));
            Assert.That(untrusted, Is.False);
            Assert.That(chainFailure.FailureMessage, Does.Contain("chain"));
            Assert.That(combined, Is.False);
            Assert.That(combinedFailure.FailureMessage,
                Does.Contain("name").And.Contain("chain"));
        });
    }

    [Test]
    public void Explicit_name_and_chain_opt_outs_never_allow_a_missing_certificate()
    {
        using X509Certificate2 certificate = LoadCertificate();
        FtpTlsOptions options = new FtpTlsOptions();
        options.RequireCertificateNameMatch = false;
        options.RequireCertificateChainTrust = false;
        FtpClientCertificatePolicy policy = CreatePolicy(options, null);

        bool ignoredErrors = policy.ValidateServerCertificate(
            policy, certificate, null,
            SslPolicyErrors.RemoteCertificateNameMismatch |
                SslPolicyErrors.RemoteCertificateChainErrors);
        bool missing = policy.ValidateServerCertificate(
            policy, null, null, SslPolicyErrors.RemoteCertificateNotAvailable);

        Assert.Multiple(() =>
        {
            Assert.That(ignoredErrors, Is.True);
            Assert.That(missing, Is.False);
            Assert.That(policy.FailureMessage, Does.Contain("did not provide"));
        });
    }

    [Test]
    public void Handled_custom_acceptance_receives_platform_context_and_can_override_errors()
    {
        using X509Certificate2 certificate = LoadCertificate();
        using X509Chain chain = new X509Chain();
        FtpCertificateValidationResult customResult = new FtpCertificateValidationResult();
        customResult.Handled = true;
        customResult.Accepted = true;
        CapturingValidator validator = new CapturingValidator(customResult, false);
        FtpEndpoint endpoint = CreateEndpoint("files.example.test");
        FtpClientCertificatePolicy policy = new FtpClientCertificatePolicy(
            endpoint, new FtpTlsOptions(), validator);
        endpoint.Host = "changed.example.test";
        SslPolicyErrors errors = SslPolicyErrors.RemoteCertificateNameMismatch |
            SslPolicyErrors.RemoteCertificateChainErrors;

        bool accepted = policy.ValidateServerCertificate(
            policy, certificate, chain, errors);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(policy.CustomHandled, Is.True);
            Assert.That(policy.CustomAccepted, Is.True);
            Assert.That(policy.ValidationCalls, Is.EqualTo(1));
            Assert.That(validator.Request, Is.Not.Null);
            Assert.That(validator.Request!.TargetHost,
                Is.EqualTo("files.example.test"));
            Assert.That(validator.Request.Certificate, Is.SameAs(certificate));
            Assert.That(validator.Request.Chain, Is.SameAs(chain));
            Assert.That(validator.Request.PolicyErrors, Is.EqualTo(errors));
            Assert.That(validator.Request.RemoteEndpoint!.Host,
                Is.EqualTo("files.example.test"));
            Assert.That(validator.Request.ServerCertificate, Is.True);
        });
    }

    [Test]
    public void Custom_rejection_message_is_retained_and_unhandled_decisions_use_strict_policy()
    {
        using X509Certificate2 certificate = LoadCertificate();
        FtpCertificateValidationResult rejectedResult = new FtpCertificateValidationResult();
        rejectedResult.Handled = true;
        rejectedResult.Accepted = false;
        rejectedResult.FailureMessage = "Pinned identity did not match.";
        FtpClientCertificatePolicy rejected = CreatePolicy(
            new FtpTlsOptions(), new CapturingValidator(rejectedResult, false));
        FtpCertificateValidationResult unhandledResult = new FtpCertificateValidationResult();
        FtpClientCertificatePolicy unhandled = CreatePolicy(
            new FtpTlsOptions(), new CapturingValidator(unhandledResult, false));

        bool customAccepted = rejected.ValidateServerCertificate(
            rejected, certificate, null, SslPolicyErrors.None);
        bool fallbackAccepted = unhandled.ValidateServerCertificate(
            unhandled, certificate, null,
            SslPolicyErrors.RemoteCertificateNameMismatch);

        Assert.Multiple(() =>
        {
            Assert.That(customAccepted, Is.False);
            Assert.That(rejected.FailureMessage,
                Is.EqualTo("Pinned identity did not match."));
            Assert.That(fallbackAccepted, Is.False);
            Assert.That(unhandled.CustomHandled, Is.False);
            Assert.That(unhandled.FailureMessage, Does.Contain("target host"));
        });
    }

    [Test]
    public void Validator_faults_and_missing_decisions_fail_closed_without_escaping_the_callback()
    {
        using X509Certificate2 certificate = LoadCertificate();
        FtpCertificateValidationResult unused = new FtpCertificateValidationResult();
        FtpClientCertificatePolicy faulted = CreatePolicy(
            new FtpTlsOptions(), new CapturingValidator(unused, true));
        FtpClientCertificatePolicy missing = CreatePolicy(
            new FtpTlsOptions(), new MissingDecisionValidator());

        bool faultAccepted = faulted.ValidateServerCertificate(
            faulted, certificate, null, SslPolicyErrors.None);
        bool missingAccepted = missing.ValidateServerCertificate(
            missing, certificate, null, SslPolicyErrors.None);

        Assert.Multiple(() =>
        {
            Assert.That(faultAccepted, Is.False);
            Assert.That(faulted.ValidatorFailure,
                Is.TypeOf<InvalidOperationException>());
            Assert.That(faulted.FailureMessage, Does.Contain("failed"));
            Assert.That(missingAccepted, Is.False);
            Assert.That(missing.FailureMessage, Does.Contain("no decision"));
        });
    }

    [Test]
    public void Revocation_mode_and_client_certificate_material_are_applied_before_handshake()
    {
        FtpTlsOptions noRevocationOptions = new FtpTlsOptions();
        noRevocationOptions.CheckCertificateRevocation = false;
        FtpTlsClientStreamRequest request = new FtpTlsClientStreamRequest();
        request.TransportStream = new MemoryStream();
        request.Options = noRevocationOptions;
        request.RemoteEndpoint = CreateEndpoint("files.example.test");
        FtpTlsClientStream client = new FtpTlsClientStream(request);
        SslClientAuthenticationOptions authentication = client.CreateAuthenticationOptions();

        using X509Certificate2 publicOnly = LoadCertificate();
        FtpTlsOptions invalidClientCertificate = new FtpTlsOptions();
        invalidClientCertificate.ClientCertificate = publicOnly;

        Assert.Multiple(() =>
        {
            Assert.That(authentication.CertificateRevocationCheckMode,
                Is.EqualTo(X509RevocationMode.NoCheck));
            Assert.That(authentication.ClientCertificates, Is.Null);
            Assert.Throws<FtpConfigurationException>(() =>
                FtpConfigurationValidator.ValidateTlsOptions(invalidClientCertificate));
        });
    }

    private static FtpClientCertificatePolicy CreatePolicy(
        FtpTlsOptions options,
        IFtpCertificateValidator? validator)
    {
        return new FtpClientCertificatePolicy(
            CreateEndpoint("files.example.test"), options, validator);
    }

    private static FtpEndpoint CreateEndpoint(string host)
    {
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = host;
        endpoint.Port = 21;
        endpoint.AddressFamily = FtpAddressFamily.Unspecified;
        return endpoint;
    }

    internal static X509Certificate2 LoadCertificate()
    {
        return X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(PublicCertificate));
    }

    private const string PublicCertificate =
        "MIIDUzCCAjugAwIBAgIUInuWv+gepHWRVNIhuyLfywufwJYwDQYJKoZIhvcNAQELBQAwHTEbMBkGA1UEAwwSZmlsZXMuZXhhbXBsZS50ZXN0MB4XDTI2MDgzMDE4NDAyNVoXDTM2MDgyNzE4NDAyNVowHTEbMBkGA1UEAwwSZmlsZXMuZXhhbXBsZS50ZXN0MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAuqwv4LJhVu9ktT7M+vnm9uwf9azX3zoxvZdVDW1Ul8UOQfiqsWdD05qyk45zrQJtPvUegTDwT6HNeO4eSolc4g1fzGtN6lIFXhRibWhMX0K0fGIdKnKltEQ61b17f4S883TNGmhs7YckNIwEBf3i5qhl4VzKiA0TcmcGT+hrP3NpqbiIie+M2X9gwYyxKS1NObmwBkXMKJYSy3nAPkWGbc4d9q90Ul6GDdGGwzGFn8+U1CvtvXEYMlsu1r5PdAuus3GWmElcKf0TlL3VF+nQ+3FvoxK9VAd2+Xu5Z2hg0BpvB6BDbfv+jGd72UyIOLVFad//K5U8HA/yx1IDMDq2jwIDAQABo4GKMIGHMB0GA1UdDgQWBBTxYVIX51Tpb2zXQh8AAqGb5EUK/jAfBgNVHSMEGDAWgBTxYVIX51Tpb2zXQh8AAqGb5EUK/jAPBgNVHRMBAf8EBTADAQH/MDQGA1UdEQQtMCuCEmZpbGVzLmV4YW1wbGUudGVzdIIVb3ZlcnJpZGUuZXhhbXBsZS50ZXN0MA0GCSqGSIb3DQEBCwUAA4IBAQAiRkToEXfN+n4eilBT4G9SKvlXRQGRzupLZtM0s9HMDZzOMOP5lsplTstUWxN2iVHVHt0/6RqLFHB07DxH3Xgm0UpKFIVY6mc4WxOYryAo5vJh5XAIdHzuMAhfwuTdta6dmSjsRCPgqzJdjaBji5WElosbQMod20Oo2DcPkguAz4S31spC62ydXNUscVH64SNymcDYTFTfNJ80P+FqnspJ+ig7ZWIN4/G0RoNBvn79z26PD3XfgCK5wS1B8DnI8lHYj8aPerN0RCd/lA+5l3bNwtlmGP+t1ohbdid7S8oYq49moHcFuXFXe9wTFNcXx3bHuxVoKfCOkX/gbzscpGr8";

    private sealed class CapturingValidator : IFtpCertificateValidator
    {
        internal FtpCertificateValidationResult Result;
        internal bool ThrowFailure;
        internal FtpCertificateValidationRequest? Request;

        internal CapturingValidator(
            FtpCertificateValidationResult result,
            bool throwFailure)
        {
            Result = result;
            ThrowFailure = throwFailure;
            Request = null;
        }

        public FtpCertificateValidationResult ValidateRemoteCertificate(
            FtpCertificateValidationRequest request)
        {
            Request = request;
            if (ThrowFailure)
            {
                throw new InvalidOperationException("validator fault");
            }
            return Result;
        }
    }

    private sealed class MissingDecisionValidator : IFtpCertificateValidator
    {
        public FtpCertificateValidationResult ValidateRemoteCertificate(
            FtpCertificateValidationRequest request)
        {
            return null!;
        }
    }
}
