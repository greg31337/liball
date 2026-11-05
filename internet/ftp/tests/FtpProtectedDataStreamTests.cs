using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;

namespace ftp.Tests;

[NonParallelizable]
public class FtpProtectedDataStreamTests
{
    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, true)]
    [TestCase(true, false, true)]
    public void Protected_transfer_uses_FTP_TLS_roles_independent_of_TCP_direction(
        bool active, bool clientSends, bool modeZ)
    {
        using DataSocketPair pair = CreateDataSocketPair(active);
        FtpDataConnectionCoordinator clientCoordinator =
            new FtpDataConnectionCoordinator();
        FtpDataConnectionCoordinator serverCoordinator =
            new FtpDataConnectionCoordinator();
        clientCoordinator.SetPreopenedConnection(pair.Client);
        serverCoordinator.SetPreopenedConnection(pair.Server);
        FtpProtectedDataStream client =
            new FtpProtectedDataStream(clientCoordinator);
        FtpProtectedDataStream server =
            new FtpProtectedDataStream(serverCoordinator);
        byte[] content = new byte[] { 2, 4, 6, 8, 10, 12 };
        MemoryStream clientStream = clientSends ?
            new MemoryStream(content) : new MemoryStream();
        MemoryStream serverStream = clientSends ?
            new MemoryStream() : new MemoryStream(content);
        FtpCommandKind commandKind = clientSends ?
            FtpCommandKind.Stor : FtpCommandKind.Retr;
        FtpTransferDirection direction = clientSends ?
            FtpTransferDirection.Upload : FtpTransferDirection.Download;
        FtpProtectedDataStreamRequest clientRequest = CreateClientRequest(
            clientStream, commandKind, direction);
        FtpProtectedDataStreamRequest serverRequest = CreateServerRequest(
            serverStream, commandKind, direction);
        if (modeZ)
        {
            clientRequest.Transfer!.Options!.Mode = FtpTransferMode.Deflate;
            serverRequest.Transfer!.Options!.Mode = FtpTransferMode.Deflate;
        }

        if (clientSends)
        {
            client.BeginSend(clientRequest);
            server.BeginReceive(serverRequest);
        }
        else
        {
            client.BeginReceive(clientRequest);
            server.BeginSend(serverRequest);
        }

        Task<FtpTransferPumpResult> serverTask = Task.Run(() => clientSends ?
            server.ExecuteReceiveData() : server.ExecuteSendData());
        FtpTransferPumpResult clientPump = clientSends ?
            client.ExecuteSendData() : client.ExecuteReceiveData();
        FtpTransferPumpResult serverPump = serverTask.GetAwaiter().GetResult();
        FtpReply clientFinal = client.CreateFinalReply();
        FtpReply serverFinal = server.CreateFinalReply();
        byte[] received = clientSends ?
            serverStream.ToArray() : clientStream.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(pair.ClientAcceptedTcp, Is.EqualTo(active));
            Assert.That(received, Is.EqualTo(content));
            Assert.That(clientPump.SourceBytes + clientPump.DestinationBytes,
                Is.GreaterThanOrEqualTo(content.Length));
            Assert.That(serverPump.SourceBytes + serverPump.DestinationBytes,
                Is.GreaterThanOrEqualTo(content.Length));
            Assert.That(client.ClientRoleAuthenticated, Is.True);
            Assert.That(client.ServerRoleAuthenticated, Is.False);
            Assert.That(client.ClientTls, Is.Null);
            Assert.That(server.ClientRoleAuthenticated, Is.False);
            Assert.That(server.ServerRoleAuthenticated, Is.True);
            Assert.That(server.ServerTls, Is.Null);
            Assert.That(clientFinal.Code,
                Is.EqualTo(FtpReplyCodes.ClosingDataConnection));
            Assert.That(serverFinal.Code,
                Is.EqualTo(FtpReplyCodes.ClosingDataConnection));
            Assert.That(clientCoordinator.HasDataSetup(), Is.False);
            Assert.That(serverCoordinator.HasDataSetup(), Is.False);
        });
    }

    [Test]
    public void Protected_stream_requires_PROT_P_and_role_specific_TLS_settings()
    {
        FtpProtectedDataStreamRequest clear = CreateClientRequest(
            new MemoryStream(), FtpCommandKind.Retr, FtpTransferDirection.Download);
        clear.TlsOptions!.DataProtectionLevel = FtpDataProtectionLevel.Clear;
        FtpProtectedDataStreamRequest invalidRole = CreateClientRequest(
            new MemoryStream(), FtpCommandKind.Retr, FtpTransferDirection.Download);
        invalidRole.Role = (FtpDataChannelRole)99;
        FtpProtectedDataStreamRequest missingClientHost = CreateClientRequest(
            new MemoryStream(), FtpCommandKind.Retr, FtpTransferDirection.Download);
        missingClientHost.TargetHost = "";
        FtpProtectedDataStreamRequest missingServerCertificate =
            new FtpProtectedDataStreamRequest();
        missingServerCertificate.Role = FtpDataChannelRole.Server;
        missingServerCertificate.TlsOptions!.Protocols = SslProtocols.Tls12;
        missingServerCertificate.TlsOptions.CheckCertificateRevocation = false;
        ConfigureTransfer(
            missingServerCertificate.Transfer!,
            new MemoryStream(),
            FtpCommandKind.Stor,
            FtpTransferDirection.Upload);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                FtpProtectedDataStream.ValidateRequest(clear, false));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpProtectedDataStream.ValidateRequest(invalidRole, false));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpProtectedDataStream.ValidateRequest(missingClientHost, false));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpProtectedDataStream.ValidateRequest(
                    missingServerCertificate, false));
        });
    }

    [Test]
    public void Failed_data_handshake_aborts_and_closes_the_data_connection()
    {
        using DataSocketPair pair = CreateDataSocketPair(false);
        FtpDataConnectionCoordinator coordinator =
            new FtpDataConnectionCoordinator();
        coordinator.SetPreopenedConnection(pair.Client);
        FtpProtectedDataStream transfer = new FtpProtectedDataStream(coordinator);
        FtpProtectedDataStreamRequest request = CreateClientRequest(
            new MemoryStream(), FtpCommandKind.Retr, FtpTransferDirection.Download);
        request.ClientAuthentication = null;
        request.HandshakeTimeoutMilliseconds = 150;
        transfer.BeginReceive(request);

        FtpTransferException? failure = Assert.Throws<FtpTransferException>(() =>
            transfer.ExecuteReceiveData());

        Assert.Multiple(() =>
        {
            Assert.That(failure!.InnerException, Is.TypeOf<TimeoutException>());
            Assert.That(coordinator.Aborted, Is.True);
            Assert.That(coordinator.HasDataSetup(), Is.False);
            Assert.That(transfer.Transfer.DataComplete, Is.False);
            Assert.That(transfer.Transfer.Result.FinalReply, Is.Null);
            Assert.Throws<FtpProtocolException>(() =>
                transfer.CreateFinalReply());
        });
    }

    private static FtpProtectedDataStreamRequest CreateClientRequest(
        Stream localStream,
        FtpCommandKind commandKind,
        FtpTransferDirection direction)
    {
        FtpProtectedDataStreamRequest request = new FtpProtectedDataStreamRequest();
        request.Role = FtpDataChannelRole.Client;
        request.TargetHost = "localhost";
        request.HandshakeTimeoutMilliseconds = 5000;
        request.CertificateValidation = (sender, remote, chain, errors) => true;
        request.ClientAuthentication = tlsRequest => tlsRequest.TransportStream!;
        request.ServerAuthentication = tlsRequest =>
            throw new InvalidOperationException(
                "The FTP client must never take the TLS-server role.");
        request.TlsOptions!.Protocols = SslProtocols.Tls12;
        request.TlsOptions.CheckCertificateRevocation = false;
        ConfigureTransfer(request.Transfer!, localStream, commandKind, direction);
        return request;
    }

    private static FtpProtectedDataStreamRequest CreateServerRequest(
        Stream localStream,
        FtpCommandKind commandKind,
        FtpTransferDirection direction)
    {
        FtpProtectedDataStreamRequest request = new FtpProtectedDataStreamRequest();
        request.Role = FtpDataChannelRole.Server;
        request.HandshakeTimeoutMilliseconds = 5000;
        request.CertificateValidation = (sender, remote, chain, errors) => true;
        request.ClientAuthentication = tlsRequest =>
            throw new InvalidOperationException(
                "The FTP server must never take the TLS-client role.");
        request.ServerAuthentication = tlsRequest => tlsRequest.TransportStream!;
        request.TlsOptions!.Protocols = SslProtocols.Tls12;
        request.TlsOptions.CheckCertificateRevocation = false;
        ConfigureTransfer(request.Transfer!, localStream, commandKind, direction);
        return request;
    }

    private static void ConfigureTransfer(
        FtpClearDataStreamRequest transfer,
        Stream localStream,
        FtpCommandKind commandKind,
        FtpTransferDirection direction)
    {
        transfer.CommandKind = commandKind;
        transfer.LocalStream = localStream;
        transfer.Direction = direction;
        transfer.ConnectionTimeoutMilliseconds = 5000;
        transfer.TransferTimeoutMilliseconds = 5000;
        transfer.LeaveLocalStreamOpen = true;
    }

    private static DataSocketPair CreateDataSocketPair(bool active)
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Socket connector = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        connector.Connect(IPAddress.Loopback, port);
        Socket accepted = listener.AcceptSocket();
        listener.Stop();
        if (active)
        {
            return new DataSocketPair(accepted, connector, true);
        }
        return new DataSocketPair(connector, accepted, false);
    }

    private class DataSocketPair : IDisposable
    {
        internal Socket Client;
        internal Socket Server;
        internal bool ClientAcceptedTcp;

        internal DataSocketPair(
            Socket client,
            Socket server,
            bool clientAcceptedTcp)
        {
            Client = client;
            Server = server;
            ClientAcceptedTcp = clientAcceptedTcp;
        }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }
    }
}
