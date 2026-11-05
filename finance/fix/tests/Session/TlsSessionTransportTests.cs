using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class TlsSessionTransportTests
{
    private string CertificateDirectory = "";
    private string CertificatePath = "";
    private const string CertificatePassword = "test-password";
    private const string TestCertificate = "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";
    private bool CertificateImportAvailable;
    private string CertificateImportError = "";
    private readonly List<TlsSessionTransport> Transports = [];
    private readonly List<TcpClient> Clients = [];

    [SetUp]
    public void CreateCertificate()
    {
        CertificateDirectory = Path.Combine(Path.GetTempPath(), "fix-tls-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(CertificateDirectory);
        CertificatePath = Path.Combine(CertificateDirectory, "localhost.pfx");
        File.WriteAllBytes(CertificatePath, Convert.FromBase64String(TestCertificate));
        try
        {
            using X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(CertificatePath, CertificatePassword, X509KeyStorageFlags.DefaultKeySet);
            CertificateImportAvailable = certificate.HasPrivateKey;
        }
        catch (Exception exception)
        {
            CertificateImportError = exception.Message;
        }
    }

    [TearDown]
    public void CloseTlsResources()
    {
        foreach (TlsSessionTransport transport in Transports)
        {
            transport.CloseTransport();
        }
        foreach (TcpClient client in Clients)
        {
            client.Close();
        }
        if (Directory.Exists(CertificateDirectory))
        {
            Directory.Delete(CertificateDirectory, true);
        }
    }

    [Test]
    public void ConstructorAndTlsConfigurationValidationAreExplicit()
    {
        TlsSessionTransport disabledTransport = Track(new TlsSessionTransport(new MemoryStream(), SessionRole.Initiator));
        TlsSessionTransport missingHostTransport = Track(new TlsSessionTransport(new MemoryStream(), SessionRole.Initiator));
        TlsSessionTransport missingCertificateTransport = Track(new TlsSessionTransport(new MemoryStream(), SessionRole.Acceptor));
        TlsSessionTransport missingAlpnTransport = Track(new TlsSessionTransport(new MemoryStream(), SessionRole.Acceptor));
        TlsSessionTransport unknownCertificateTransport = Track(new TlsSessionTransport(new MemoryStream(), SessionRole.Acceptor));

        TransportWriteResult disabled = disabledTransport.OpenTransport(new SessionEndpoint(), new SessionTlsSettings());
        TransportWriteResult missingHost = missingHostTransport.OpenTransport(new SessionEndpoint(), ClientSettings(""));
        SessionTlsSettings serverWithoutCertificate = ServerSettings();
        serverWithoutCertificate.CertificatePath = "";
        TransportWriteResult missingCertificate = missingCertificateTransport.OpenTransport(new SessionEndpoint(), serverWithoutCertificate);
        SessionTlsSettings serverWithoutAlpn = ServerSettings();
        serverWithoutAlpn.Alpn = "";
        TransportWriteResult missingAlpn = missingAlpnTransport.OpenTransport(new SessionEndpoint(), serverWithoutAlpn);
        SessionTlsSettings serverWithUnknownCertificate = ServerSettings();
        serverWithUnknownCertificate.CertificatePath = Path.Combine(CertificateDirectory, "missing.pfx");
        TransportWriteResult unknownCertificate = unknownCertificateTransport.OpenTransport(new SessionEndpoint(), serverWithUnknownCertificate);

        Assert.Multiple(() =>
        {
            Assert.Throws<FixTransportException>(() => new TlsSessionTransport(null!, SessionRole.Initiator));
            Assert.Throws<FixTransportException>(() => new TlsSessionTransport(new ReadOnlyMemoryStream(), SessionRole.Initiator));
            Assert.Throws<FixTransportException>(() => new TlsSessionTransport(new MemoryStream(), (SessionRole)99));
            AssertFailure(disabled, "tls-disabled");
            AssertFailure(missingHost, "missing-tls-target-host");
            AssertFailure(missingCertificate, "missing-tls-certificate");
            AssertFailure(missingAlpn, "missing-tls-alpn");
            AssertFailure(unknownCertificate, "tls-certificate-not-found");
        });
    }

    [Test]
    public void ClientAndServerNegotiateTls12OrNewerWithFixAlpn()
    {
        RequireCertificateImport();
        TlsPair pair = CreatePair();

        AuthenticatePair(pair, ClientSettings("localhost"), ServerSettings());

        Assert.Multiple(() =>
        {
            Assert.That(pair.ClientTransport.SecureStream.IsAuthenticated, Is.True);
            Assert.That(pair.ClientTransport.SecureStream.IsServer, Is.False);
            Assert.That(pair.ServerTransport.SecureStream.IsAuthenticated, Is.True);
            Assert.That(pair.ServerTransport.SecureStream.IsServer, Is.True);
            Assert.That(pair.ClientTransport.SecureStream.SslProtocol, Is.AnyOf(SslProtocols.Tls12, SslProtocols.Tls13));
            Assert.That(pair.ServerTransport.SecureStream.SslProtocol, Is.AnyOf(SslProtocols.Tls12, SslProtocols.Tls13));
            Assert.That(pair.ClientTransport.SecureStream.NegotiatedApplicationProtocol.ToString(), Is.EqualTo("fix"));
            Assert.That(pair.ServerTransport.SecureStream.NegotiatedApplicationProtocol.ToString(), Is.EqualTo("fix"));
            Assert.That(pair.ClientTransport.AuthenticatedTargetHost, Is.EqualTo("localhost"));
            Assert.That(pair.ServerTransport.LocalCertificate, Is.Not.Null);
        });
    }

    [Test]
    public void EmptyConfiguredTargetHostFallsBackToEndpointHostname()
    {
        RequireCertificateImport();
        TlsPair pair = CreatePair();
        SessionTlsSettings client = ClientSettings("");

        AuthenticatePair(pair, client, ServerSettings());

        Assert.Multiple(() =>
        {
            Assert.That(pair.ClientResult.State, Is.EqualTo(TransportState.Connected));
            Assert.That(pair.ClientTransport.AuthenticatedTargetHost, Is.EqualTo("localhost"));
            Assert.That(pair.ClientTransport.SecureStream.TargetHostName, Is.EqualTo("localhost"));
        });
    }

    [Test]
    public void PeerCertificateHostnameFailureReturnsExplicitOutcome()
    {
        RequireCertificateImport();
        TlsPair pair = CreatePair();
        SessionTlsSettings client = ClientSettings("wrong-host.invalid");
        client.RequirePeerCertificate = true;

        OpenPair(pair, client, ServerSettings());

        Assert.Multiple(() =>
        {
            Assert.That(pair.ClientResult.State, Is.EqualTo(TransportState.Faulted));
            Assert.That(pair.ClientResult.ErrorCode, Is.EqualTo("tls-authentication-failed"));
            Assert.That(pair.ClientResult.ErrorMessage, Is.Not.Empty);
            Assert.That(pair.ClientTransport.ReadTransport(new byte[1]).ErrorCode, Is.EqualTo("transport-faulted"));
        });
    }

    [Test]
    public void AlpnMismatchReturnsExplicitOutcomeOnBothPeers()
    {
        RequireCertificateImport();
        TlsPair pair = CreatePair();
        SessionTlsSettings server = ServerSettings();
        server.Alpn = "other";

        OpenPair(pair, ClientSettings("localhost"), server);

        Assert.Multiple(() =>
        {
            Assert.That(pair.ClientResult.State, Is.EqualTo(TransportState.Faulted));
            Assert.That(pair.ClientResult.ErrorCode, Is.AnyOf("tls-alpn-not-negotiated", "tls-authentication-failed"), pair.ClientResult.ErrorMessage);
            Assert.That(pair.ServerResult.State, Is.EqualTo(TransportState.Faulted));
            Assert.That(pair.ServerResult.ErrorCode, Is.AnyOf("tls-alpn-not-negotiated", "tls-authentication-failed"), pair.ServerResult.ErrorMessage);
        });
    }

    [Test]
    public void AuthenticatedTransportExchangesPlaintextAndReportsPeerEof()
    {
        RequireCertificateImport();
        TlsPair pair = CreatePair();
        AuthenticatePair(pair, ClientSettings("localhost"), ServerSettings());
        byte[] clientPayload = [10, 11, 12];
        byte[] serverBuffer = new byte[8];

        TransportWriteResult reopened = pair.ClientTransport.OpenTransport(pair.Endpoint, ClientSettings("localhost"));
        TransportReadResult emptyRead = pair.ServerTransport.ReadTransport(Span<byte>.Empty);
        TransportWriteResult emptyWrite = pair.ClientTransport.WriteTransport(ReadOnlySpan<byte>.Empty);
        TransportWriteResult clientWrite = pair.ClientTransport.WriteTransport(clientPayload);
        TransportReadResult serverRead = pair.ServerTransport.ReadTransport(serverBuffer);
        TransportWriteResult serverWrite = pair.ServerTransport.WriteTransport([20, 21, 22, 23]);
        byte[] clientBuffer = new byte[8];
        TransportReadResult clientRead = pair.ClientTransport.ReadTransport(clientBuffer);
        TransportWriteResult clientClose = pair.ClientTransport.CloseTransport();
        TransportReadResult serverEof = pair.ServerTransport.ReadTransport(serverBuffer);

        Assert.Multiple(() =>
        {
            AssertSuccess(reopened, 0);
            AssertRead(emptyRead, TransportState.Connected, 0, false);
            AssertSuccess(emptyWrite, 0);
            AssertSuccess(clientWrite, 3);
            AssertRead(serverRead, TransportState.Connected, 3, false);
            Assert.That(serverBuffer.AsSpan(0, serverRead.BytesRead).ToArray(), Is.EqualTo(clientPayload));
            AssertSuccess(serverWrite, 4);
            AssertRead(clientRead, TransportState.Connected, 4, false);
            Assert.That(clientBuffer.AsSpan(0, clientRead.BytesRead).ToArray(), Is.EqualTo(new byte[] { 20, 21, 22, 23 }));
            Assert.That(clientClose.State, Is.EqualTo(TransportState.Closed));
            AssertRead(serverEof, TransportState.Closed, 0, true);
        });
    }

    [Test]
    public void ClosedOrUnauthenticatedTransportRejectsFurtherIo()
    {
        TlsSessionTransport transport = Track(new TlsSessionTransport(new MemoryStream(), SessionRole.Initiator));

        TransportReadResult beforeRead = transport.ReadTransport(new byte[1]);
        TransportWriteResult beforeWrite = transport.WriteTransport([1]);
        TransportWriteResult closed = transport.CloseTransport();
        TransportWriteResult reopened = transport.OpenTransport(new SessionEndpoint { Host = "localhost" }, ClientSettings("localhost"));
        TransportReadResult afterRead = transport.ReadTransport(new byte[1]);

        Assert.Multiple(() =>
        {
            Assert.That(beforeRead.ErrorCode, Is.EqualTo("transport-not-connected"));
            Assert.That(beforeWrite.ErrorCode, Is.EqualTo("transport-not-connected"));
            Assert.That(closed.State, Is.EqualTo(TransportState.Closed));
            Assert.That(reopened.ErrorCode, Is.EqualTo("transport-closed"));
            Assert.That(afterRead.State, Is.EqualTo(TransportState.Closed));
        });
    }

    [Test]
    public void ProtocolAndCertificatePolicyHelpersAreStable()
    {
        List<SslApplicationProtocol> protocols = TlsSessionTransport.CreateApplicationProtocols("fix");

        Assert.Multiple(() =>
        {
            Assert.That(TlsSessionTransport.AllowedProtocols, Is.EqualTo(SslProtocols.Tls12 | SslProtocols.Tls13));
            Assert.That(protocols, Has.Count.EqualTo(1));
            Assert.That(protocols[0].ToString(), Is.EqualTo("fix"));
            Assert.That(TlsSessionTransport.ResolveRevocationMode(true), Is.EqualTo(X509RevocationMode.Online));
            Assert.That(TlsSessionTransport.ResolveRevocationMode(false), Is.EqualTo(X509RevocationMode.NoCheck));
        });
    }

    private void RequireCertificateImport()
    {
        if (!CertificateImportAvailable)
        {
            Assert.Ignore("This host does not permit private-key certificate import: " + CertificateImportError);
        }
    }

    private TlsPair CreatePair()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        TcpClient client = new TcpClient(AddressFamily.InterNetwork);
        client.Connect(IPAddress.Loopback, port);
        TcpClient server = listener.AcceptTcpClient();
        listener.Stop();
        Clients.Add(client);
        Clients.Add(server);
        TlsPair pair = new TlsPair();
        pair.ClientTransport = Track(new TlsSessionTransport(client.GetStream(), SessionRole.Initiator));
        pair.ServerTransport = Track(new TlsSessionTransport(server.GetStream(), SessionRole.Acceptor));
        pair.Endpoint = new SessionEndpoint { Host = "localhost", Port = port, UseTls = true };
        return pair;
    }

    private static void AuthenticatePair(TlsPair pair, SessionTlsSettings client, SessionTlsSettings server)
    {
        OpenPair(pair, client, server);
        Assert.Multiple(() =>
        {
            Assert.That(pair.ServerResult.State, Is.EqualTo(TransportState.Connected), pair.ServerResult.ErrorMessage);
            Assert.That(pair.ClientResult.State, Is.EqualTo(TransportState.Connected), pair.ClientResult.ErrorMessage);
        });
    }

    private static void OpenPair(TlsPair pair, SessionTlsSettings client, SessionTlsSettings server)
    {
        Task<TransportWriteResult> serverHandshake = Task.Factory.StartNew(() => pair.ServerTransport.OpenTransport(pair.Endpoint, server), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Task<TransportWriteResult> clientHandshake = Task.Factory.StartNew(() => pair.ClientTransport.OpenTransport(pair.Endpoint, client), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        Assert.That(Task.WaitAll([serverHandshake, clientHandshake], TimeSpan.FromSeconds(5)), Is.True);
        pair.ServerResult = serverHandshake.Result;
        pair.ClientResult = clientHandshake.Result;
    }

    private TlsSessionTransport Track(TlsSessionTransport transport)
    {
        Transports.Add(transport);
        return transport;
    }

    private SessionTlsSettings ServerSettings()
    {
        return new SessionTlsSettings
        {
            Enabled = true,
            CertificatePath = CertificatePath,
            CertificatePassword = CertificatePassword,
            RequirePeerCertificate = false,
            CheckCertificateRevocation = false,
            Alpn = "fix"
        };
    }

    private static SessionTlsSettings ClientSettings(string targetHost)
    {
        return new SessionTlsSettings
        {
            Enabled = true,
            TargetHost = targetHost,
            RequirePeerCertificate = false,
            CheckCertificateRevocation = false,
            Alpn = "fix"
        };
    }

    private static void AssertSuccess(TransportWriteResult result, int count)
    {
        Assert.That(result.State, Is.EqualTo(TransportState.Connected));
        Assert.That(result.BytesWritten, Is.EqualTo(count));
        Assert.That(result.ErrorCode, Is.Empty);
    }

    private static void AssertRead(TransportReadResult result, TransportState state, int count, bool eof)
    {
        Assert.That(result.State, Is.EqualTo(state));
        Assert.That(result.BytesRead, Is.EqualTo(count));
        Assert.That(result.EndOfStream, Is.EqualTo(eof));
        Assert.That(result.ErrorCode, Is.Empty);
    }

    private static void AssertFailure(TransportWriteResult result, string code)
    {
        Assert.That(result.State, Is.EqualTo(TransportState.Faulted));
        Assert.That(result.ErrorCode, Is.EqualTo(code));
        Assert.That(result.ErrorMessage, Is.Not.Empty);
    }

    private class TlsPair
    {
        public TlsSessionTransport ClientTransport = null!;
        public TlsSessionTransport ServerTransport = null!;
        public SessionEndpoint Endpoint = null!;
        public TransportWriteResult ClientResult = new TransportWriteResult();
        public TransportWriteResult ServerResult = new TransportWriteResult();
    }

    private class ReadOnlyMemoryStream : MemoryStream
    {
        public override bool CanWrite => false;
    }
}
