using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpTlsSessionReusePolicyTests
{
    [Test]
    public void Client_and_server_authentication_options_control_TLS_resumption()
    {
        using MemoryStream clientTransport = new MemoryStream();
        FtpTlsClientStreamRequest clientRequest = new FtpTlsClientStreamRequest();
        clientRequest.TransportStream = clientTransport;
        clientRequest.TargetHost = "localhost";
        clientRequest.CertificateValidation =
            (sender, certificate, chain, errors) => true;
        FtpTlsClientStream client = new FtpTlsClientStream(clientRequest);
        SslClientAuthenticationOptions clientOptions =
            client.CreateAuthenticationOptions();

        using MemoryStream serverTransport = new MemoryStream();
        FtpTlsServerStreamRequest serverRequest = new FtpTlsServerStreamRequest();
        serverRequest.TransportStream = serverTransport;
        serverRequest.CertificateSelection = (sender, name) =>
            throw new InvalidOperationException("Certificate selection is not used by this test.");
        FtpTlsServerStream server = new FtpTlsServerStream(serverRequest);
        SslServerAuthenticationOptions serverOptions =
            server.CreateAuthenticationOptions();

        clientRequest.AllowTlsResume = false;
        serverRequest.AllowTlsResume = false;
        SslClientAuthenticationOptions disabledClient =
            client.CreateAuthenticationOptions();
        SslServerAuthenticationOptions disabledServer =
            server.CreateAuthenticationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(clientOptions.AllowTlsResume, Is.True);
            Assert.That(serverOptions.AllowTlsResume, Is.True);
            Assert.That(disabledClient.AllowTlsResume, Is.False);
            Assert.That(disabledServer.AllowTlsResume, Is.False);
        });
    }

    [Test]
    public void Strict_reuse_requires_available_reused_control_matching_evidence()
    {
        FtpTlsSessionReuseEvidence unavailable =
            new FtpTlsSessionReuseEvidence();
        FtpTlsSessionReuseEvidence fullHandshake =
            CreateEvidence(true, false, false);
        FtpTlsSessionReuseEvidence wrongSession =
            CreateEvidence(true, true, false);
        FtpTlsSessionReuseEvidence matching =
            CreateEvidence(true, true, true);

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() =>
                FtpTlsSessionReusePolicy.RequireSatisfied(
                    false, true, unavailable));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpTlsSessionReusePolicy.RequireSatisfied(
                    true, false, matching));
            Assert.Throws<FtpTlsException>(() =>
                FtpTlsSessionReusePolicy.RequireSatisfied(
                    true, true, unavailable));
            Assert.Throws<FtpTlsException>(() =>
                FtpTlsSessionReusePolicy.RequireSatisfied(
                    true, true, fullHandshake));
            Assert.Throws<FtpTlsException>(() =>
                FtpTlsSessionReusePolicy.RequireSatisfied(
                    true, true, wrongSession));
            Assert.DoesNotThrow(() =>
                FtpTlsSessionReusePolicy.RequireSatisfied(
                    true, true, matching));
        });
    }

    [Test]
    public void Strict_policy_aborts_unverifiable_data_but_accepts_matching_evidence()
    {
        using DataSocketPair rejectedPair = CreateDataSocketPair();
        FtpDataConnectionCoordinator rejectedCoordinator =
            new FtpDataConnectionCoordinator();
        rejectedCoordinator.SetPreopenedConnection(rejectedPair.Client);
        FtpProtectedDataStream rejected =
            new FtpProtectedDataStream(rejectedCoordinator);
        FtpProtectedDataStreamRequest rejectedRequest =
            CreateProtectedRequest(new MemoryStream());
        rejected.BeginReceive(rejectedRequest);

        FtpTransferException? failure = Assert.Throws<FtpTransferException>(() =>
            rejected.ExecuteReceiveData());

        using DataSocketPair acceptedPair = CreateDataSocketPair();
        byte[] content = new byte[] { 1, 3, 5, 7 };
        acceptedPair.Server.Send(content);
        acceptedPair.Server.Shutdown(SocketShutdown.Send);
        FtpDataConnectionCoordinator acceptedCoordinator =
            new FtpDataConnectionCoordinator();
        acceptedCoordinator.SetPreopenedConnection(acceptedPair.Client);
        MemoryStream destination = new MemoryStream();
        FtpProtectedDataStream accepted =
            new FtpProtectedDataStream(acceptedCoordinator);
        FtpProtectedDataStreamRequest acceptedRequest =
            CreateProtectedRequest(destination);
        acceptedRequest.SessionReuseEvidence =
            CreateEvidence(true, true, true);
        accepted.BeginReceive(acceptedRequest);
        accepted.ExecuteReceiveData();

        Assert.Multiple(() =>
        {
            Assert.That(failure!.InnerException, Is.TypeOf<FtpTlsException>());
            Assert.That(rejectedCoordinator.Aborted, Is.True);
            Assert.That(rejectedCoordinator.HasDataSetup(), Is.False);
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(accepted.SessionReuseEvidence,
                Is.SameAs(acceptedRequest.SessionReuseEvidence));
            Assert.That(acceptedCoordinator.Aborted, Is.False);
            Assert.That(acceptedCoordinator.HasDataSetup(), Is.False);
        });
    }

    private static FtpProtectedDataStreamRequest CreateProtectedRequest(
        Stream destination)
    {
        FtpProtectedDataStreamRequest request = new FtpProtectedDataStreamRequest();
        request.Role = FtpDataChannelRole.Client;
        request.TargetHost = "localhost";
        request.ClientAuthentication = tlsRequest => tlsRequest.TransportStream!;
        request.TlsOptions!.RequireDataSessionReuse = true;
        request.Transfer!.CommandKind = FtpCommandKind.Retr;
        request.Transfer.LocalStream = destination;
        request.Transfer.Direction = FtpTransferDirection.Download;
        request.Transfer.ConnectionTimeoutMilliseconds = 5000;
        request.Transfer.TransferTimeoutMilliseconds = 5000;
        return request;
    }

    private static FtpTlsSessionReuseEvidence CreateEvidence(
        bool available,
        bool reused,
        bool matchesControlSession)
    {
        FtpTlsSessionReuseEvidence evidence =
            new FtpTlsSessionReuseEvidence();
        evidence.Available = available;
        evidence.Reused = reused;
        evidence.MatchesControlSession = matchesControlSession;
        return evidence;
    }

    private static DataSocketPair CreateDataSocketPair()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Socket client = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(IPAddress.Loopback, port);
        Socket server = listener.AcceptSocket();
        listener.Stop();
        return new DataSocketPair(client, server);
    }

    private class DataSocketPair : IDisposable
    {
        internal Socket Client;
        internal Socket Server;

        internal DataSocketPair(Socket client, Socket server)
        {
            Client = client;
            Server = server;
        }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }
    }
}
