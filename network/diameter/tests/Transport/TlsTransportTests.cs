using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;

namespace diameter.Tests;

public class TlsTransportTests
{
    private string TemporaryDirectory = string.Empty;

    [SetUp]
    public void CreateTemporaryDirectory()
    {
        TemporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "diameter-tls-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TemporaryDirectory);
    }

    [TearDown]
    public void RemoveTemporaryDirectory()
    {
        if (Directory.Exists(TemporaryDirectory))
        {
            Directory.Delete(TemporaryDirectory, true);
        }
    }

    [Test]
    public void MutualTlsAuthenticatesBeforeHandoffAndTransfersEncryptedBytes()
    {
        TlsTestCertificates certificates = CreateCertificates();
        int port = ReservePort();
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterTransportConfiguration listenerTransport = CreateTransport(port, true);
        DiameterSecurityConfiguration listenerSecurity = CreateSecurity(
            certificates.ServerPath,
            certificates.RootPath,
            "client.example");
        DiameterListenerResult created = factory.CreateDiameterListener(
            listenerTransport,
            listenerSecurity);
        IgnoreUnavailableMacOsKeychain(created);
        Assert.That(created.Status, Is.EqualTo(DiameterTransportStatus.Success),
            created.Code + ": " + created.Message);

        DiameterTransportConfiguration clientTransport = CreateTransport(port, false);
        DiameterSecurityConfiguration clientSecurity = CreateSecurity(
            certificates.ClientPath,
            certificates.RootPath,
            "server.example");
        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(port),
            clientTransport,
            clientSecurity,
            5000);
        DiameterConnectionResult accepted = created.Listener!
            .AcceptDiameterConnection(5000);
        Assert.That(connected.Status, Is.EqualTo(DiameterTransportStatus.Success),
            connected.Code + ": " + connected.Message);
        Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success),
            accepted.Code + ": " + accepted.Message);
        byte[] request = new byte[] { 1, 3, 5, 7, 9 };
        DiameterTransportIoResult sent = connected.Connection!
            .SendDiameterConnectionBytes(request, 2000);
        byte[] receivedBytes = new byte[request.Length];
        DiameterTransportIoResult received = accepted.Connection!
            .ReceiveDiameterConnectionBytes(receivedBytes, 2000);
        DiameterConnectionMetadata clientMetadata = connected.Connection!
            .ReadDiameterConnectionMetadata();
        DiameterConnectionMetadata serverMetadata = accepted.Connection!
            .ReadDiameterConnectionMetadata();
        accepted.Connection!.CloseDiameterConnection();
        connected.Connection!.CloseDiameterConnection();
        created.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(sent.TransferredCount, Is.EqualTo(request.Length));
            Assert.That(received.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(receivedBytes, Is.EqualTo(request));
            Assert.That(clientMetadata.Security.Mode, Is.EqualTo(DiameterSecurityMode.Tls));
            Assert.That(clientMetadata.Security.Authenticated, Is.True);
            Assert.That(clientMetadata.Security.Encrypted, Is.True);
            Assert.That(clientMetadata.Security.MutuallyAuthenticated, Is.True);
            Assert.That(clientMetadata.Security.Protocol,
                Is.AnyOf("Tls12", "Tls13"));
            Assert.That(clientMetadata.Security.CipherSuite, Is.Not.Empty);
            Assert.That(clientMetadata.Security.PeerCertificateSubject,
                Does.Contain("server.example"));
            Assert.That(clientMetadata.Security.PeerCertificateDnsNames,
                Does.Contain("server.example"));
            Assert.That(serverMetadata.Security.MutuallyAuthenticated, Is.True);
            Assert.That(serverMetadata.Security.PeerCertificateSubject,
                Does.Contain("client.example"));
            Assert.That(created.Metadata.SecurityMode,
                Is.EqualTo(DiameterSecurityMode.Tls));
        });
    }

    [Test]
    public void PeerLifecycleAndApplicationTrafficRoundTripOverTls()
    {
        TcpTlsPeerEndToEndTests.RunPeerLifecycleAndApplicationTraffic(
            true,
            CreateCertificates());
    }

    [Test]
    public void HostnameMismatchFailsAuthenticationAndDoesNotExposeAConnection()
    {
        TlsTestCertificates certificates = CreateCertificates();
        int port = ReservePort();
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterListenerResult listener = factory.CreateDiameterListener(
            CreateTransport(port, true),
            CreateSecurity(
                certificates.ServerPath,
                certificates.RootPath,
                "client.example"));
        IgnoreUnavailableMacOsKeychain(listener);
        Assert.That(listener.Status, Is.EqualTo(DiameterTransportStatus.Success));
        DiameterSecurityConfiguration clientSecurity = CreateSecurity(
            certificates.ClientPath,
            certificates.RootPath,
            "wrong.example");

        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(port),
            CreateTransport(port, false),
            clientSecurity,
            5000);
        DiameterConnectionResult accepted = listener.Listener!
            .AcceptDiameterConnection(5000);
        if (accepted.Connection != null)
        {
            accepted.Connection.CloseDiameterConnection();
        }

        listener.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(connected.Status,
                Is.EqualTo(DiameterTransportStatus.AuthenticationFailed));
            Assert.That(connected.Connection, Is.Null);
            Assert.That(connected.Code, Is.EqualTo("tls-authentication-failed"));
            Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(accepted.Connection, Is.Not.Null);
        });
    }

    [Test]
    public void UntrustedServerChainFailsAuthenticationBeforeClientHandoff()
    {
        TlsTestCertificates trusted = CreateCertificates("trusted");
        TlsTestCertificates rogue = CreateCertificates("rogue");
        int port = ReservePort();
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterListenerResult listener = factory.CreateDiameterListener(
            CreateTransport(port, true),
            CreateSecurity(
                rogue.ServerPath,
                trusted.RootPath,
                "client.example"));
        IgnoreUnavailableMacOsKeychain(listener);
        Assert.That(listener.Status, Is.EqualTo(DiameterTransportStatus.Success));
        DiameterSecurityConfiguration clientSecurity = CreateSecurity(
            trusted.ClientPath,
            trusted.RootPath,
            "server.example");

        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(port),
            CreateTransport(port, false),
            clientSecurity,
            5000);
        DiameterConnectionResult accepted = listener.Listener!
            .AcceptDiameterConnection(5000);
        if (connected.Connection != null)
        {
            connected.Connection.CloseDiameterConnection();
        }

        if (accepted.Connection != null)
        {
            accepted.Connection.CloseDiameterConnection();
        }

        listener.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(connected.Status,
                Is.EqualTo(DiameterTransportStatus.AuthenticationFailed));
            Assert.That(connected.Connection, Is.Null);
            Assert.That(connected.Code, Is.EqualTo("tls-authentication-failed"));
            Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(accepted.Connection, Is.Not.Null);
        });
    }

    [Test]
    public void ExpiredServerCertificateFailsAuthenticationBeforeHandoff()
    {
        TlsTestCertificates certificates = CreateExpiredServerCertificates();
        int port = ReservePort();
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterListenerResult listener = factory.CreateDiameterListener(
            CreateTransport(port, true),
            CreateSecurity(
                certificates.ServerPath,
                certificates.RootPath,
                "client.example"));
        IgnoreUnavailableMacOsKeychain(listener);
        Assert.That(listener.Status, Is.EqualTo(DiameterTransportStatus.Success),
            listener.Code + ": " + listener.Message);

        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(port),
            CreateTransport(port, false),
            CreateSecurity(
                certificates.ClientPath,
                certificates.RootPath,
                "server.example"),
            5000);
        DiameterConnectionResult accepted = listener.Listener!
            .AcceptDiameterConnection(5000);
        if (accepted.Connection != null)
        {
            accepted.Connection.CloseDiameterConnection();
        }

        listener.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(connected.Status,
                Is.EqualTo(DiameterTransportStatus.AuthenticationFailed));
            Assert.That(connected.Connection, Is.Null);
            Assert.That(connected.Code, Is.EqualTo("tls-authentication-failed"));
            Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(accepted.Connection, Is.Not.Null);
        });
    }

    [Test]
    public void ProbeAndCertificateLoadingReturnStableConfigurationFailures()
    {
        TlsTestCertificates certificates = CreateCertificates();
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterTransportConfiguration transport = CreateTransport(3868, false);
        DiameterSecurityConfiguration noMutualAuthentication = CreateSecurity(
            certificates.ClientPath,
            certificates.RootPath,
            "server.example");
        noMutualAuthentication.RequirePeerCertificate = false;
        DiameterTransportSupportResult noMutual = factory.ProbeDiameterTransportSupport(
            transport,
            noMutualAuthentication);
        DiameterSecurityConfiguration missingCertificate = CreateSecurity(
            Path.Combine(TemporaryDirectory, "missing.pfx"),
            certificates.RootPath,
            "server.example");
        DiameterTransportSupportResult missing = factory.ProbeDiameterTransportSupport(
            transport,
            missingCertificate);
        string invalidPath = Path.Combine(TemporaryDirectory, "invalid.pfx");
        File.WriteAllText(invalidPath, "not a certificate");
        DiameterSecurityConfiguration invalidCertificate = CreateSecurity(
            invalidPath,
            certificates.RootPath,
            "server.example");
        DiameterConnectionResult invalid = factory.ConnectDiameterEndpoint(
            CreateEndpoint(3868),
            transport,
            invalidCertificate,
            2000);
        DiameterSecurityConfiguration validSecurity = CreateSecurity(
            certificates.ClientPath,
            certificates.RootPath,
            "server.example");
        DiameterSecurityConfiguration noTrustAnchor = CreateSecurity(
            certificates.ClientPath,
            "",
            "server.example");
        DiameterTransportSupportResult noTrustSupport =
            factory.ProbeDiameterTransportSupport(
                transport,
                noTrustAnchor);
        DiameterTransportConfiguration invalidTcp = CreateTransport(3868, false);
        invalidTcp.ReceiveBufferSize = 1;
        DiameterTransportSupportResult underlyingFailure =
            factory.ProbeDiameterTransportSupport(
                invalidTcp,
                validSecurity);
        DiameterTransportConfiguration disabledListener =
            CreateTransport(3868, false);
        DiameterListenerResult disabled = factory.CreateDiameterListener(
            disabledListener,
            validSecurity);
        DiameterConnectionResult missingEndpoint =
            factory.ConnectDiameterEndpoint(
                null,
                transport,
                validSecurity,
                2000);
        DiameterConnectionResult invalidTimeout =
            factory.ConnectDiameterEndpoint(
                CreateEndpoint(3868),
                transport,
                validSecurity,
                0);
        DiameterListenerResult unsupportedListener =
            factory.CreateDiameterListener(null, validSecurity);
        DiameterListenerResult invalidCertificateListener =
            factory.CreateDiameterListener(transport, invalidCertificate);
        DiameterTransportConfiguration wrongTransport =
            CreateTransport(3868, false);
        wrongTransport.Kind = DiameterTransportKind.Sctp;
        DiameterConnectionResult unsupportedConnection =
            factory.ConnectDiameterEndpoint(
                CreateEndpoint(3868),
                wrongTransport,
                validSecurity,
                1000);

        Assert.Multiple(() =>
        {
            Assert.That(noMutual.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(noMutual.Code, Is.EqualTo("tls-configuration-invalid"));
            Assert.That(missing.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(missing.Code, Is.EqualTo("local-certificate-not-found"));
            Assert.That(invalid.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(invalid.Code, Is.EqualTo("local-certificate-invalid"));
            Assert.That(disabled.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(missingEndpoint.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(invalidTimeout.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(noTrustSupport.Status,
                Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(underlyingFailure.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(unsupportedListener.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(invalidCertificateListener.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(unsupportedConnection.Status,
                Is.EqualTo(DiameterTransportStatus.Unsupported));
        });
    }

    [Test]
    public void TlsProbeRejectsEveryConfigurationBoundaryBeforeCertificateLoading()
    {
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterTransportConfiguration transport = CreateTransport(3868, false);
        DiameterSecurityConfiguration security = new DiameterSecurityConfiguration();
        security.Mode = DiameterSecurityMode.Tls;
        security.LocalCertificatePath = "missing.pfx";
        security.RequirePeerCertificate = true;

        Assert.That(factory.ProbeDiameterTransportSupport(null, security).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        Assert.That(factory.ProbeDiameterTransportSupport(transport, null).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        DiameterTransportConfiguration sctp = CreateTransport(3868, false);
        sctp.Kind = DiameterTransportKind.Sctp;
        Assert.That(factory.ProbeDiameterTransportSupport(sctp, security).Status,
            Is.EqualTo(DiameterTransportStatus.Unsupported));
        DiameterSecurityConfiguration none = new DiameterSecurityConfiguration();
        Assert.That(factory.ProbeDiameterTransportSupport(transport, none).Status,
            Is.EqualTo(DiameterTransportStatus.Unsupported));

        DiameterSecurityConfiguration unsecured = CloneTlsSecurity(security);
        unsecured.AllowUnsecuredTransport = true;
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, unsecured).Code, Is.EqualTo("tls-configuration-invalid"));
        DiameterSecurityConfiguration noPeer = CloneTlsSecurity(security);
        noPeer.RequirePeerCertificate = false;
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, noPeer).Code, Is.EqualTo("tls-configuration-invalid"));
        DiameterSecurityConfiguration noPath = CloneTlsSecurity(security);
        noPath.LocalCertificatePath = "";
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, noPath).Code, Is.EqualTo("tls-configuration-invalid"));
        DiameterSecurityConfiguration noIdentity = CloneTlsSecurity(security);
        noIdentity.ExpectedPeerIdentity = null!;
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, noIdentity).Code,
            Is.EqualTo("peer-identity-configuration-invalid"));
        DiameterSecurityConfiguration noRealm = CloneTlsSecurity(security);
        noRealm.ExpectedPeerRealm = null!;
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, noRealm).Code,
            Is.EqualTo("peer-identity-configuration-invalid"));
        DiameterSecurityConfiguration badIdentity = CloneTlsSecurity(security);
        badIdentity.ExpectedPeerIdentity.Value = "bad identity";
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, badIdentity).Code,
            Is.EqualTo("peer-identity-configuration-invalid"));
        DiameterSecurityConfiguration badRealm = CloneTlsSecurity(security);
        badRealm.ExpectedPeerRealm.Value = "bad realm";
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, badRealm).Code,
            Is.EqualTo("peer-identity-configuration-invalid"));

        string existing = Path.Combine(TemporaryDirectory, "exists.pfx");
        File.WriteAllText(existing, "placeholder");
        DiameterSecurityConfiguration missingTrust = CloneTlsSecurity(security);
        missingTrust.LocalCertificatePath = existing;
        missingTrust.TrustAnchorPath = Path.Combine(
            TemporaryDirectory,
            "missing-root.pem");
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, missingTrust).Code, Is.EqualTo("trust-anchor-not-found"));
    }

    private static DiameterSecurityConfiguration CloneTlsSecurity(
        DiameterSecurityConfiguration source)
    {
        DiameterSecurityConfiguration result =
            new DiameterSecurityConfiguration();
        result.Mode = source.Mode;
        result.LocalCertificatePath = source.LocalCertificatePath;
        result.LocalCertificatePassword = source.LocalCertificatePassword;
        result.TrustAnchorPath = source.TrustAnchorPath;
        result.ExpectedPeerName = source.ExpectedPeerName;
        result.ExpectedPeerIdentity.Value = source.ExpectedPeerIdentity.Value;
        result.ExpectedPeerRealm.Value = source.ExpectedPeerRealm.Value;
        result.RequirePeerCertificate = source.RequirePeerCertificate;
        result.AllowUnsecuredTransport = source.AllowUnsecuredTransport;
        return result;
    }

    [Test]
    public void DefaultCertificateAuthorizationRejectsADifferentDiameterIdentity()
    {
        TlsTestCertificates certificates = CreateCertificates();
        int port = ReservePort();
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterListenerResult listener = factory.CreateDiameterListener(
            CreateTransport(port, true),
            CreateSecurity(
                certificates.ServerPath,
                certificates.RootPath,
                "client.example"));
        IgnoreUnavailableMacOsKeychain(listener);
        Assert.That(listener.Status, Is.EqualTo(DiameterTransportStatus.Success));
        DiameterSecurityConfiguration clientSecurity = CreateSecurity(
            certificates.ClientPath,
            certificates.RootPath,
            "server.example");
        clientSecurity.ExpectedPeerIdentity.Value = "different.example";
        clientSecurity.ExpectedPeerRealm.Value = "example";

        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(port),
            CreateTransport(port, false),
            clientSecurity,
            5000);
        DiameterConnectionResult accepted = listener.Listener!
            .AcceptDiameterConnection(5000);
        if (accepted.Connection != null)
        {
            accepted.Connection.CloseDiameterConnection();
        }

        listener.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(connected.Status,
                Is.EqualTo(DiameterTransportStatus.AuthenticationFailed));
            Assert.That(connected.Connection, Is.Null);
            Assert.That(connected.Code,
                Is.EqualTo("certificate-peer-identity-mismatch"));
            Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
        });
    }

    [Test]
    public void CustomCertificateAndPeerAdmissionHooksRunBeforeConnectionHandoff()
    {
        TlsTestCertificates certificates = CreateCertificates();
        int port = ReservePort();
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterListenerResult listener = factory.CreateDiameterListener(
            CreateTransport(port, true),
            CreateSecurity(
                certificates.ServerPath,
                certificates.RootPath,
                "client.example"));
        IgnoreUnavailableMacOsKeychain(listener);
        Assert.That(listener.Status, Is.EqualTo(DiameterTransportStatus.Success));
        RecordingCertificatePeerPolicy certificatePolicy =
            new RecordingCertificatePeerPolicy();
        certificatePolicy.Decision.Admitted = true;
        RecordingPeerAdmissionPolicy admissionPolicy =
            new RecordingPeerAdmissionPolicy();
        admissionPolicy.Decision.Admitted = true;
        DiameterSecurityConfiguration clientSecurity = CreateSecurity(
            certificates.ClientPath,
            certificates.RootPath,
            "server.example");
        clientSecurity.ExpectedPeerIdentity.Value = "DIAMETER.Example.";
        clientSecurity.ExpectedPeerRealm.Value = "EXAMPLE.";
        clientSecurity.CertificatePeerPolicy = certificatePolicy;
        clientSecurity.PeerAdmissionPolicy = admissionPolicy;

        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(port),
            CreateTransport(port, false),
            clientSecurity,
            5000);
        DiameterConnectionResult accepted = listener.Listener!
            .AcceptDiameterConnection(5000);
        Assert.That(connected.Status, Is.EqualTo(DiameterTransportStatus.Success),
            connected.Code + ": " + connected.Message);
        if (connected.Connection != null)
        {
            connected.Connection.CloseDiameterConnection();
        }

        if (accepted.Connection != null)
        {
            accepted.Connection.CloseDiameterConnection();
        }

        listener.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(certificatePolicy.CallCount, Is.EqualTo(1));
            Assert.That(admissionPolicy.CallCount, Is.EqualTo(1));
            Assert.That(certificatePolicy.Request, Is.Not.Null);
            Assert.That(certificatePolicy.Request!.PeerIdentity.Value,
                Is.EqualTo("diameter.example"));
            Assert.That(certificatePolicy.Request.PeerRealm.Value,
                Is.EqualTo("example"));
            Assert.That(certificatePolicy.Request.Connection.Direction,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(certificatePolicy.Request.Connection.Security.Authenticated,
                Is.True);
            Assert.That(certificatePolicy.Request.Connection.Security.PeerCertificateDnsNames,
                Does.Contain("server.example"));
            Assert.That(admissionPolicy.Request, Is.Not.SameAs(certificatePolicy.Request));
            Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
        });
    }

    [Test]
    public void PeerAdmissionRejectionClosesTheAuthenticatedConnectionWithPolicyCode()
    {
        TlsTestCertificates certificates = CreateCertificates();
        int port = ReservePort();
        DiameterTlsTransportFactory factory = new DiameterTlsTransportFactory();
        DiameterListenerResult listener = factory.CreateDiameterListener(
            CreateTransport(port, true),
            CreateSecurity(
                certificates.ServerPath,
                certificates.RootPath,
                "client.example"));
        IgnoreUnavailableMacOsKeychain(listener);
        Assert.That(listener.Status, Is.EqualTo(DiameterTransportStatus.Success));
        RecordingCertificatePeerPolicy certificatePolicy =
            new RecordingCertificatePeerPolicy();
        certificatePolicy.Decision.Admitted = true;
        RecordingPeerAdmissionPolicy admissionPolicy =
            new RecordingPeerAdmissionPolicy();
        admissionPolicy.Decision.Code = "configured-peer-denied";
        admissionPolicy.Decision.Message = "The configured peer is disabled.";
        DiameterSecurityConfiguration clientSecurity = CreateSecurity(
            certificates.ClientPath,
            certificates.RootPath,
            "server.example");
        clientSecurity.CertificatePeerPolicy = certificatePolicy;
        clientSecurity.PeerAdmissionPolicy = admissionPolicy;

        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            CreateEndpoint(port),
            CreateTransport(port, false),
            clientSecurity,
            5000);
        DiameterConnectionResult accepted = listener.Listener!
            .AcceptDiameterConnection(5000);
        if (accepted.Connection != null)
        {
            accepted.Connection.CloseDiameterConnection();
        }

        listener.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(connected.Status,
                Is.EqualTo(DiameterTransportStatus.AuthenticationFailed));
            Assert.That(connected.Connection, Is.Null);
            Assert.That(connected.Code, Is.EqualTo("configured-peer-denied"));
            Assert.That(connected.Message,
                Is.EqualTo("The configured peer is disabled."));
            Assert.That(certificatePolicy.CallCount, Is.EqualTo(1));
            Assert.That(admissionPolicy.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void DefaultCertificatePolicyChecksRealmMembershipAndWildcardDepth()
    {
        DiameterConnectionMetadata exactMetadata = new DiameterConnectionMetadata();
        exactMetadata.Security.PeerCertificateDnsNames.Add("peer.example");
        DiameterConnectionMetadata wildcardMetadata = new DiameterConnectionMetadata();
        wildcardMetadata.Security.PeerCertificateDnsNames.Add("*.example");
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "PEER.Example.";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "Example.";
        DiameterIdentity wrongRealm = new DiameterIdentity();
        wrongRealm.Value = "other.example";
        DiameterIdentity nestedIdentity = new DiameterIdentity();
        nestedIdentity.Value = "deep.peer.example";

        DiameterPeerAdmissionResult exact =
            TlsCertificateSupport.AuthorizeDefaultCertificatePeer(
                exactMetadata,
                identity,
                realm);
        DiameterPeerAdmissionResult wildcard =
            TlsCertificateSupport.AuthorizeDefaultCertificatePeer(
                wildcardMetadata,
                identity,
                realm);
        DiameterPeerAdmissionResult realmMismatch =
            TlsCertificateSupport.AuthorizeDefaultCertificatePeer(
                exactMetadata,
                identity,
                wrongRealm);
        DiameterPeerAdmissionResult deepWildcard =
            TlsCertificateSupport.AuthorizeDefaultCertificatePeer(
                wildcardMetadata,
                nestedIdentity,
                realm);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Admitted, Is.True);
            Assert.That(wildcard.Admitted, Is.True);
            Assert.That(realmMismatch.Admitted, Is.False);
            Assert.That(realmMismatch.Code,
                Is.EqualTo("certificate-peer-realm-mismatch"));
            Assert.That(deepWildcard.Admitted, Is.False);
            Assert.That(deepWildcard.Code,
                Is.EqualTo("certificate-peer-identity-mismatch"));
        });
    }

    private TlsTestCertificates CreateCertificates()
    {
        return CreateCertificates("default");
    }

    private TlsTestCertificates CreateCertificates(string certificateSet)
    {
        const string password = "test-password";
        string directory = Path.Combine(TemporaryDirectory, certificateSet);
        Directory.CreateDirectory(directory);
        string rootPath = Path.Combine(directory, "root.pem");
        string rootKeyPath = Path.Combine(directory, "root.key");
        RunOpenSsl(
            "req",
            "-x509",
            "-newkey",
            "rsa:2048",
            "-nodes",
            "-keyout",
            rootKeyPath,
            "-out",
            rootPath,
            "-subj",
            "/CN=Diameter Test Root",
            "-days",
            "30",
            "-sha256",
            "-addext",
            "basicConstraints=critical,CA:TRUE",
            "-addext",
            "keyUsage=critical,keyCertSign,cRLSign");
        string serverPath = CreateLeafCertificate(
            directory,
            rootPath,
            rootKeyPath,
            "server.example",
            true,
            password);
        string clientPath = CreateLeafCertificate(
            directory,
            rootPath,
            rootKeyPath,
            "client.example",
            false,
            password);
        TlsTestCertificates result = new TlsTestCertificates();
        result.RootPath = rootPath;
        result.ServerPath = serverPath;
        result.ClientPath = clientPath;
        result.Password = password;
        return result;
    }

    private string CreateLeafCertificate(
        string directory,
        string rootPath,
        string rootKeyPath,
        string dnsName,
        bool server,
        string password)
    {
        string keyPath = Path.Combine(directory, dnsName + ".key");
        string requestPath = Path.Combine(directory, dnsName + ".csr");
        string certificatePath = Path.Combine(directory, dnsName + ".pem");
        string extensionPath = Path.Combine(directory, dnsName + ".ext");
        File.WriteAllText(
            extensionPath,
            "subjectAltName=DNS:" + dnsName + Environment.NewLine +
            "basicConstraints=critical,CA:FALSE" + Environment.NewLine +
            "keyUsage=critical,digitalSignature,keyEncipherment" + Environment.NewLine +
            "extendedKeyUsage=" + (server ? "serverAuth" : "clientAuth") +
            Environment.NewLine);
        RunOpenSsl(
            "req",
            "-newkey",
            "rsa:2048",
            "-nodes",
            "-keyout",
            keyPath,
            "-out",
            requestPath,
            "-subj",
            "/CN=" + dnsName);
        RunOpenSsl(
            "x509",
            "-req",
            "-in",
            requestPath,
            "-CA",
            rootPath,
            "-CAkey",
            rootKeyPath,
            "-CAcreateserial",
            "-out",
            certificatePath,
            "-days",
            "14",
            "-sha256",
            "-extfile",
            extensionPath);
        string path = Path.Combine(directory, dnsName + ".bundle.pem");
        File.WriteAllText(
            path,
            File.ReadAllText(certificatePath) + File.ReadAllText(keyPath));
        return path;
    }

    private TlsTestCertificates CreateExpiredServerCertificates()
    {
        TlsTestCertificates result = CreateCertificates("expired");
        string directory = Path.Combine(TemporaryDirectory, "expired");
        string rootKeyPath = Path.Combine(directory, "root.key");
        result.ServerPath = CreateExpiredLeafCertificate(
            directory,
            result.RootPath,
            rootKeyPath,
            "server.example");
        return result;
    }

    private string CreateExpiredLeafCertificate(
        string directory,
        string rootPath,
        string rootKeyPath,
        string dnsName)
    {
        string prefix = Path.Combine(directory, "expired-" + dnsName);
        string keyPath = prefix + ".key";
        string requestPath = prefix + ".csr";
        string certificatePath = prefix + ".pem";
        string extensionPath = prefix + ".ext";
        string databasePath = Path.Combine(directory, "expired-index.txt");
        string serialPath = Path.Combine(directory, "expired-serial.txt");
        string issuedDirectory = Path.Combine(directory, "expired-issued");
        string configurationPath = Path.Combine(directory, "expired-ca.cnf");
        Directory.CreateDirectory(issuedDirectory);
        File.WriteAllText(databasePath, string.Empty);
        File.WriteAllText(serialPath, "1000" + Environment.NewLine);
        File.WriteAllText(
            extensionPath,
            "[leaf]" + Environment.NewLine +
            "subjectAltName=DNS:" + dnsName + Environment.NewLine +
            "basicConstraints=critical,CA:FALSE" + Environment.NewLine +
            "keyUsage=critical,digitalSignature,keyEncipherment" + Environment.NewLine +
            "extendedKeyUsage=serverAuth" + Environment.NewLine);
        File.WriteAllText(
            configurationPath,
            "[ca]" + Environment.NewLine +
            "default_ca=issuer" + Environment.NewLine +
            "[issuer]" + Environment.NewLine +
            "database=" + databasePath + Environment.NewLine +
            "new_certs_dir=" + issuedDirectory + Environment.NewLine +
            "certificate=" + rootPath + Environment.NewLine +
            "private_key=" + rootKeyPath + Environment.NewLine +
            "serial=" + serialPath + Environment.NewLine +
            "default_md=sha256" + Environment.NewLine +
            "default_days=14" + Environment.NewLine +
            "policy=policy_any" + Environment.NewLine +
            "unique_subject=no" + Environment.NewLine +
            "[policy_any]" + Environment.NewLine +
            "commonName=supplied" + Environment.NewLine);
        RunOpenSsl(
            "req",
            "-newkey",
            "rsa:2048",
            "-nodes",
            "-keyout",
            keyPath,
            "-out",
            requestPath,
            "-subj",
            "/CN=" + dnsName);
        RunOpenSsl(
            "ca",
            "-batch",
            "-notext",
            "-config",
            configurationPath,
            "-in",
            requestPath,
            "-out",
            certificatePath,
            "-startdate",
            "20200101000000Z",
            "-enddate",
            "20200102000000Z",
            "-extfile",
            extensionPath,
            "-extensions",
            "leaf");
        string path = prefix + ".bundle.pem";
        File.WriteAllText(
            path,
            File.ReadAllText(certificatePath) + File.ReadAllText(keyPath));
        return path;
    }

    private static void RunOpenSsl(params string[] arguments)
    {
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = "openssl";
        start.RedirectStandardError = true;
        start.UseShellExecute = false;
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.Zero, error);
    }

    private static void IgnoreUnavailableMacOsKeychain(DiameterListenerResult result)
    {
        if (OperatingSystem.IsMacOS() &&
            result.Status == DiameterTransportStatus.InvalidConfiguration &&
            result.Message.Contains("keychain", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("The test sandbox does not expose a writable macOS Keychain.");
        }
    }

    private static DiameterTransportConfiguration CreateTransport(
        int port,
        bool listener)
    {
        DiameterTransportConfiguration result = new DiameterTransportConfiguration();
        result.Host = "127.0.0.1";
        result.Port = port;
        result.EnableIpv4 = true;
        result.EnableIpv6 = false;
        result.EnableListener = listener;
        result.EnableActiveConnections = !listener;
        result.ListenBacklog = 4;
        return result;
    }

    private static DiameterSecurityConfiguration CreateSecurity(
        string certificatePath,
        string trustAnchorPath,
        string expectedPeerName)
    {
        DiameterSecurityConfiguration result = new DiameterSecurityConfiguration();
        result.Mode = DiameterSecurityMode.Tls;
        result.LocalCertificatePath = certificatePath;
        if (certificatePath.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase))
        {
            result.LocalCertificatePassword = "test-password";
        }

        result.TrustAnchorPath = trustAnchorPath;
        result.ExpectedPeerName = expectedPeerName;
        result.RequirePeerCertificate = true;
        return result;
    }

    private static DiameterEndpoint CreateEndpoint(int port)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = "server.example";
        result.Host = "server.example";
        result.Address = IPAddress.Loopback;
        result.Port = port;
        result.Transport = DiameterTransportKind.Tcp;
        result.Security = DiameterSecurityMode.Tls;
        return result;
    }

    private static int ReservePort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

public class TlsTestCertificates
{
    public string RootPath = string.Empty;
    public string ServerPath = string.Empty;
    public string ClientPath = string.Empty;
    public string Password = string.Empty;
}

public class RecordingCertificatePeerPolicy : ICertificatePeerPolicy
{
    public int CallCount;
    public DiameterPeerAdmissionRequest? Request;
    public DiameterPeerAdmissionResult Decision = new DiameterPeerAdmissionResult();

    public DiameterPeerAdmissionResult AuthorizeCertificatePeer(
        DiameterPeerAdmissionRequest request)
    {
        CallCount = CallCount + 1;
        Request = request;
        return Decision;
    }
}

public class RecordingPeerAdmissionPolicy : IPeerAdmissionPolicy
{
    public int CallCount;
    public DiameterPeerAdmissionRequest? Request;
    public DiameterPeerAdmissionResult Decision = new DiameterPeerAdmissionResult();

    public DiameterPeerAdmissionResult EvaluatePeerAdmission(
        DiameterPeerAdmissionRequest request)
    {
        CallCount = CallCount + 1;
        Request = request;
        return Decision;
    }
}
