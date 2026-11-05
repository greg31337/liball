using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ftp.Tests;

public class FtpGeneratedCertificateTlsTests
{
    [Test]
    public void Generated_chain_is_trusted_and_target_host_drives_SNI_and_validation()
    {
        using GeneratedCertificateChain certificates =
            GeneratedCertificateChain.Create("files.example.test");
        using X509Chain chain = CreateChain(
            certificates.RootCertificate,
            certificates.IntermediateCertificate);
        bool chainBuilt = chain.Build(certificates.ServerCertificate);
        FtpTlsOptions options = new FtpTlsOptions();
        options.CheckCertificateRevocation = false;
        FtpEndpoint endpoint = CreateEndpoint("files.example.test", 21);
        FtpClientCertificatePolicy policy = new FtpClientCertificatePolicy(
            endpoint,
            options,
            new StrictFtpCertificateValidator());
        FtpTlsClientStreamRequest streamRequest = new FtpTlsClientStreamRequest();
        streamRequest.TransportStream = new MemoryStream();
        streamRequest.RemoteEndpoint = endpoint;
        streamRequest.Options = options;
        streamRequest.CertificateValidator = new StrictFtpCertificateValidator();
        FtpTlsClientStream client = new FtpTlsClientStream(streamRequest);

        bool accepted = policy.ValidateServerCertificate(
            policy,
            certificates.ServerCertificate,
            chain,
            SslPolicyErrors.None);
        SslClientAuthenticationOptions authentication =
            client.CreateAuthenticationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(chainBuilt, Is.True);
            Assert.That(chain.ChainElements.Count, Is.EqualTo(3));
            Assert.That(accepted, Is.True);
            Assert.That(policy.CustomHandled, Is.True);
            Assert.That(policy.CustomAccepted, Is.True);
            Assert.That(policy.Accepted, Is.True);
            Assert.That(policy.TargetHost, Is.EqualTo("files.example.test"));
            Assert.That(authentication.TargetHost,
                Is.EqualTo("files.example.test"));
            Assert.That(authentication.CertificateRevocationCheckMode,
                Is.EqualTo(X509RevocationMode.NoCheck));
        });
    }

    [Test]
    public void Generated_chain_name_and_trust_failures_are_rejected_with_redacted_messages()
    {
        using GeneratedCertificateChain certificates =
            GeneratedCertificateChain.Create("files.example.test");
        using GeneratedCertificateChain unrelated =
            GeneratedCertificateChain.Create("unrelated.example.test");
        using X509Chain trustedChain = CreateChain(
            certificates.RootCertificate,
            certificates.IntermediateCertificate);
        using X509Chain untrustedChain = CreateChain(
            unrelated.RootCertificate,
            certificates.IntermediateCertificate);
        Assert.That(trustedChain.Build(certificates.ServerCertificate), Is.True);
        Assert.That(untrustedChain.Build(certificates.ServerCertificate), Is.False);
        FtpClientCertificatePolicy wrongName = CreatePolicy(
            "wrong.example.test",
            new StrictFtpCertificateValidator());
        FtpClientCertificatePolicy wrongTrust = CreatePolicy(
            "files.example.test",
            new StrictFtpCertificateValidator());

        bool nameAccepted = wrongName.ValidateServerCertificate(
            wrongName,
            certificates.ServerCertificate,
            trustedChain,
            SslPolicyErrors.RemoteCertificateNameMismatch);
        bool trustAccepted = wrongTrust.ValidateServerCertificate(
            wrongTrust,
            certificates.ServerCertificate,
            untrustedChain,
            SslPolicyErrors.RemoteCertificateChainErrors);
        string fingerprint = certificates.ServerCertificate.GetCertHashString(
            HashAlgorithmName.SHA256);
        string certificateBody = Convert.ToBase64String(
            certificates.ServerCertificate.RawData);

        Assert.Multiple(() =>
        {
            Assert.That(nameAccepted, Is.False);
            Assert.That(trustAccepted, Is.False);
            Assert.That(wrongName.FailureMessage, Does.Contain("target host"));
            Assert.That(wrongTrust.FailureMessage,
                Does.Contain("trust chain").Or.Contain("revocation"));
            Assert.That(wrongName.FailureMessage,
                Does.Not.Contain("wrong.example.test"));
            Assert.That(wrongTrust.FailureMessage,
                Does.Not.Contain("files.example.test"));
            Assert.That(wrongName.FailureMessage, Does.Not.Contain(fingerprint));
            Assert.That(wrongTrust.FailureMessage, Does.Not.Contain(fingerprint));
            Assert.That(wrongName.FailureMessage,
                Does.Not.Contain(certificateBody));
            Assert.That(wrongTrust.FailureMessage,
                Does.Not.Contain(certificateBody));
        });
    }

    [Test]
    public void Generated_client_certificate_is_required_and_validated_against_its_chain()
    {
        using GeneratedCertificateChain certificates =
            GeneratedCertificateChain.Create("files.example.test");
        ClientCertificateValidator validator = new ClientCertificateValidator(
            certificates.RootCertificate,
            certificates.IntermediateCertificate,
            certificates.ClientCertificate);
        FtpTlsOptions options = new FtpTlsOptions();
        options.RequireClientCertificate = true;
        FtpServerCertificatePolicy policy = new FtpServerCertificatePolicy(
            new UnusedProvider(),
            "files.example.test",
            null,
            null,
            true,
            options,
            validator.Validate);

        bool missing = policy.ValidateClientCertificate(
            policy,
            null,
            null,
            SslPolicyErrors.RemoteCertificateNotAvailable);
        bool accepted = policy.ValidateClientCertificate(
            policy,
            certificates.ClientCertificate,
            null,
            SslPolicyErrors.RemoteCertificateChainErrors);

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.False);
            Assert.That(accepted, Is.True);
            Assert.That(validator.Calls, Is.EqualTo(1));
            Assert.That(validator.ChainBuilt, Is.True);
            Assert.That(validator.ChainElementCount, Is.EqualTo(3));
            Assert.That(validator.Accepted, Is.True);
            Assert.That(policy.ClientValidationCalls, Is.EqualTo(2));
            Assert.That(policy.ClientCertificateAccepted, Is.True);
            Assert.That(policy.ClientValidatorFailure, Is.Null);
        });
    }

    [Test]
    public void Revocation_modes_are_applied_and_disjoint_protocols_fail_without_secret_material()
    {
        using GeneratedCertificateChain certificates =
            GeneratedCertificateChain.Create("files.example.test");
        FtpTlsOptions policyOptions = new FtpTlsOptions();
        policyOptions.CheckCertificateRevocation = true;
        FtpClientCertificatePolicy certificatePolicy = new FtpClientCertificatePolicy(
            CreateEndpoint("files.example.test", 21),
            policyOptions,
            null);
        FtpTlsServerStreamRequest serverRequest = new FtpTlsServerStreamRequest();
        serverRequest.TransportStream = new MemoryStream();
        serverRequest.CertificateSelection = (sender, host) =>
            certificates.ServerCertificate;
        FtpTlsOptions serverOptions = new FtpTlsOptions();
        serverOptions.Protocols = SslProtocols.Tls12;
        serverOptions.CheckCertificateRevocation = true;
        serverRequest.Options = serverOptions;
        FtpTlsServerStream server = new FtpTlsServerStream(serverRequest);
        SslServerAuthenticationOptions serverAuthentication =
            server.CreateAuthenticationOptions();
        FtpTlsClientStreamRequest clientRequest = new FtpTlsClientStreamRequest();
        clientRequest.TransportStream = new MemoryStream();
        clientRequest.RemoteEndpoint = CreateEndpoint("files.example.test", 21);
        clientRequest.CertificateValidator = new StrictFtpCertificateValidator();
        clientRequest.Options!.Protocols = SslProtocols.Tls13;
        clientRequest.Options.CheckCertificateRevocation = false;
        FtpTlsClientStream client = new FtpTlsClientStream(clientRequest);
        SslClientAuthenticationOptions clientAuthentication =
            client.CreateAuthenticationOptions();
        string fingerprint = certificates.ServerCertificate.GetCertHashString(
            HashAlgorithmName.SHA256);
        string certificateBody = Convert.ToBase64String(
            certificates.ServerCertificate.RawData);
        FtpFaultIsolationResult handshake = new FtpFaultIsolationResult();
        handshake.Failure = new InvalidOperationException(
            "Sensitive TLS detail " + fingerprint + " " + certificateBody);
        FtpFaultIsolationResult cleanup = new FtpFaultIsolationResult();
        cleanup.Succeeded = true;

        FtpTlsException? failure = Assert.Throws<FtpTlsException>(() =>
            FtpTlsHandshake.ThrowHandshakeFailure(
                handshake,
                cleanup,
                false,
                false,
                default(CancellationToken),
                "client"));

        Assert.Multiple(() =>
        {
            Assert.That(certificatePolicy.RevocationMode,
                Is.EqualTo(X509RevocationMode.Online));
            Assert.That(serverAuthentication.CertificateRevocationCheckMode,
                Is.EqualTo(X509RevocationMode.Online));
            Assert.That(clientAuthentication.CertificateRevocationCheckMode,
                Is.EqualTo(X509RevocationMode.NoCheck));
            Assert.That(serverAuthentication.EnabledSslProtocols,
                Is.EqualTo(SslProtocols.Tls12));
            Assert.That(clientAuthentication.EnabledSslProtocols,
                Is.EqualTo(SslProtocols.Tls13));
            Assert.That(
                serverAuthentication.EnabledSslProtocols &
                    clientAuthentication.EnabledSslProtocols,
                Is.EqualTo((SslProtocols)0));
            Assert.That(failure, Is.Not.Null);
            Assert.That(failure!.Message,
                Is.EqualTo("The FTP TLS client handshake failed."));
            Assert.That(failure.Message, Does.Not.Contain("files.example.test"));
            Assert.That(failure.Message, Does.Not.Contain(fingerprint));
            Assert.That(failure.Message, Does.Not.Contain(certificateBody));
        });
    }

    private static FtpClientCertificatePolicy CreatePolicy(
        string host,
        IFtpCertificateValidator validator)
    {
        return new FtpClientCertificatePolicy(
            CreateEndpoint(host, 21),
            new FtpTlsOptions(),
            validator);
    }

    private static FtpEndpoint CreateEndpoint(string host, int port)
    {
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = host;
        endpoint.Port = port;
        endpoint.AddressFamily = FtpAddressFamily.Unspecified;
        return endpoint;
    }

    private static X509Chain CreateChain(
        X509Certificate2 root,
        X509Certificate2 intermediate)
    {
        X509Chain chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(root);
        chain.ChainPolicy.ExtraStore.Add(intermediate);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
        return chain;
    }

    private sealed class ClientCertificateValidator
    {
        internal X509Certificate2 Root;
        internal X509Certificate2 Intermediate;
        internal X509Certificate2 ExpectedCertificate;
        internal int Calls;
        internal bool ChainBuilt;
        internal int ChainElementCount;
        internal bool Accepted;

        internal ClientCertificateValidator(
            X509Certificate2 root,
            X509Certificate2 intermediate,
            X509Certificate2 expectedCertificate)
        {
            Root = root;
            Intermediate = intermediate;
            ExpectedCertificate = expectedCertificate;
            Calls = 0;
            ChainBuilt = false;
            ChainElementCount = 0;
            Accepted = false;
        }

        internal bool Validate(
            object sender,
            X509Certificate? certificate,
            X509Chain? platformChain,
            SslPolicyErrors policyErrors)
        {
            Calls++;
            ChainBuilt = false;
            ChainElementCount = 0;
            Accepted = false;
            X509Certificate2? certificateValue = certificate as X509Certificate2;
            if (certificateValue == null)
            {
                return false;
            }

            using X509Chain chain = CreateChain(Root, Intermediate);
            ChainBuilt = chain.Build(certificateValue);
            ChainElementCount = chain.ChainElements.Count;
            Accepted = ChainBuilt && certificateValue.GetCertHashString() ==
                ExpectedCertificate.GetCertHashString();
            return Accepted;
        }
    }

    private sealed class UnusedProvider : IFtpCertificateProvider
    {
        public FtpCertificateResult SelectServerCertificate(
            FtpCertificateRequest request)
        {
            throw new InvalidOperationException(
                "Certificate selection is not used by this policy test.");
        }
    }

    private sealed class GeneratedCertificateChain : IDisposable
    {
        internal RSA RootKey;
        internal RSA IntermediateKey;
        internal RSA ServerKey;
        internal RSA ClientKey;
        internal X509Certificate2 RootCertificate;
        internal X509Certificate2 IntermediateCertificate;
        internal X509Certificate2 ServerCertificate;
        internal X509Certificate2 ClientCertificate;

        private GeneratedCertificateChain(
            RSA rootKey,
            RSA intermediateKey,
            RSA serverKey,
            RSA clientKey,
            X509Certificate2 rootCertificate,
            X509Certificate2 intermediateCertificate,
            X509Certificate2 serverCertificate,
            X509Certificate2 clientCertificate)
        {
            RootKey = rootKey;
            IntermediateKey = intermediateKey;
            ServerKey = serverKey;
            ClientKey = clientKey;
            RootCertificate = rootCertificate;
            IntermediateCertificate = intermediateCertificate;
            ServerCertificate = serverCertificate;
            ClientCertificate = clientCertificate;
        }

        internal static GeneratedCertificateChain Create(string serverName)
        {
            DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddDays(-1);
            DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddDays(30);
            RSA rootKey = RSA.Create(2048);
            RSA intermediateKey = RSA.Create(2048);
            RSA serverKey = RSA.Create(2048);
            RSA clientKey = RSA.Create(2048);

            X500DistinguishedName rootName = new X500DistinguishedName(
                "CN=FTP Test Root");
            CertificateRequest rootRequest = new CertificateRequest(
                rootName,
                rootKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            AddAuthorityExtensions(rootRequest, 1);
            X509Certificate2 rootCertificate = rootRequest.Create(
                rootName,
                X509SignatureGenerator.CreateForRSA(
                    rootKey, RSASignaturePadding.Pkcs1),
                notBefore,
                notAfter.AddDays(1),
                CreateSerialNumber());

            X500DistinguishedName intermediateName =
                new X500DistinguishedName("CN=FTP Test Intermediate");
            CertificateRequest intermediateRequest = new CertificateRequest(
                intermediateName,
                intermediateKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            AddAuthorityExtensions(intermediateRequest, 0);
            X509Certificate2 intermediateCertificate = intermediateRequest.Create(
                rootName,
                X509SignatureGenerator.CreateForRSA(
                    rootKey, RSASignaturePadding.Pkcs1),
                notBefore,
                notAfter,
                CreateSerialNumber());

            CertificateRequest serverRequest = CreateLeafRequest(
                serverKey,
                "CN=" + serverName,
                serverName,
                false);
            using X509Certificate2 serverPublic = serverRequest.Create(
                intermediateName,
                X509SignatureGenerator.CreateForRSA(
                    intermediateKey, RSASignaturePadding.Pkcs1),
                notBefore,
                notAfter,
                CreateSerialNumber());
            X509Certificate2 serverCertificate =
                X509CertificateLoader.LoadCertificate(serverPublic.RawData);

            CertificateRequest clientRequest = CreateLeafRequest(
                clientKey,
                "CN=FTP Test Client",
                null,
                true);
            using X509Certificate2 clientPublic = clientRequest.Create(
                intermediateName,
                X509SignatureGenerator.CreateForRSA(
                    intermediateKey, RSASignaturePadding.Pkcs1),
                notBefore,
                notAfter,
                CreateSerialNumber());
            X509Certificate2 clientCertificate =
                X509CertificateLoader.LoadCertificate(clientPublic.RawData);

            return new GeneratedCertificateChain(
                rootKey,
                intermediateKey,
                serverKey,
                clientKey,
                rootCertificate,
                intermediateCertificate,
                serverCertificate,
                clientCertificate);
        }

        private static void AddAuthorityExtensions(
            CertificateRequest request,
            int pathLength)
        {
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, true, pathLength, true));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign |
                        X509KeyUsageFlags.CrlSign,
                    true));
            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        }

        private static CertificateRequest CreateLeafRequest(
            RSA key,
            string subject,
            string? dnsName,
            bool clientCertificate)
        {
            CertificateRequest request = new CertificateRequest(
                subject,
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature |
                        X509KeyUsageFlags.KeyEncipherment,
                    true));
            OidCollection usages = new OidCollection();
            usages.Add(new Oid(
                clientCertificate ?
                    "1.3.6.1.5.5.7.3.2" :
                    "1.3.6.1.5.5.7.3.1"));
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(usages, true));
            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            if (dnsName != null)
            {
                SubjectAlternativeNameBuilder names =
                    new SubjectAlternativeNameBuilder();
                names.AddDnsName(dnsName);
                request.CertificateExtensions.Add(names.Build());
            }
            return request;
        }

        private static byte[] CreateSerialNumber()
        {
            byte[] serial = RandomNumberGenerator.GetBytes(16);
            serial[0] &= 127;
            serial[0] |= 1;
            return serial;
        }

        public void Dispose()
        {
            ClientCertificate.Dispose();
            ServerCertificate.Dispose();
            IntermediateCertificate.Dispose();
            RootCertificate.Dispose();
            ClientKey.Dispose();
            ServerKey.Dispose();
            IntermediateKey.Dispose();
            RootKey.Dispose();
        }
    }

}
