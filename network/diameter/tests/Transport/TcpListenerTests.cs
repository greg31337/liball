using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class TcpListenerTests
{
    [Test]
    public void Ipv4ListenerTimesOutThenHandsOffAnAcceptedConnection()
    {
        int port = ReservePort(IPAddress.Loopback);
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterTransportConfiguration listenerConfiguration =
            CreateListenerConfiguration(IPAddress.Loopback, port);
        DiameterListenerResult created = factory.CreateDiameterListener(
            listenerConfiguration,
            CreateUnsecuredSecurity());
        Assert.That(created.Status, Is.EqualTo(DiameterTransportStatus.Success),
            created.Code + ": " + created.Message);
        IDiameterListener listener = created.Listener!;
        DiameterConnectionResult timeout = listener.AcceptDiameterConnection(30);

        DiameterConnectionResult active = factory.ConnectDiameterEndpoint(
            CreateEndpoint(IPAddress.Loopback, port),
            CreateActiveConfiguration(false),
            CreateUnsecuredSecurity(),
            2000);
        Assert.That(active.Status, Is.EqualTo(DiameterTransportStatus.Success));
        DiameterConnectionResult accepted = listener.AcceptDiameterConnection(2000);
        Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
        byte[] request = new byte[] { 1, 2, 3, 4 };
        DiameterTransportIoResult sent = active.Connection!
            .SendDiameterConnectionBytes(request, 1000);
        byte[] receivedRequest = new byte[request.Length];
        DiameterTransportIoResult received = accepted.Connection!
            .ReceiveDiameterConnectionBytes(receivedRequest, 1000);
        byte[] answer = new byte[] { 9, 8, 7 };
        DiameterTransportIoResult answerSent = accepted.Connection!
            .SendDiameterConnectionBytes(answer, 1000);
        byte[] receivedAnswer = new byte[answer.Length];
        DiameterTransportIoResult answerReceived = active.Connection!
            .ReceiveDiameterConnectionBytes(receivedAnswer, 1000);
        TcpSocketDiameterConnection acceptedTcp =
            (TcpSocketDiameterConnection)accepted.Connection!;
        bool noDelay = acceptedTcp.OwnedSocket.NoDelay;
        int keepAlive = Convert.ToInt32(acceptedTcp.OwnedSocket.GetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.KeepAlive));
        accepted.Connection!.CloseDiameterConnection();
        active.Connection!.CloseDiameterConnection();
        DiameterTransportOperationResult close = listener.CloseDiameterListener();
        DiameterTransportOperationResult repeatedClose = listener.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(timeout.Status, Is.EqualTo(DiameterTransportStatus.TimedOut));
            Assert.That(timeout.Code, Is.EqualTo("accept-timeout"));
            Assert.That(created.Metadata.Open, Is.True);
            Assert.That(created.Metadata.LocalEndpoint.Address,
                Is.EqualTo(IPAddress.Loopback));
            Assert.That(created.Metadata.LocalEndpoint.Port, Is.EqualTo(port));
            Assert.That(accepted.Metadata.Direction,
                Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(active.Metadata.Direction,
                Is.EqualTo(DiameterConnectionDirection.Outbound));
            Assert.That(sent.TransferredCount, Is.EqualTo(request.Length));
            Assert.That(received.TransferredCount, Is.EqualTo(request.Length));
            Assert.That(receivedRequest, Is.EqualTo(request));
            Assert.That(answerSent.TransferredCount, Is.EqualTo(answer.Length));
            Assert.That(answerReceived.TransferredCount, Is.EqualTo(answer.Length));
            Assert.That(receivedAnswer, Is.EqualTo(answer));
            Assert.That(noDelay, Is.True);
            Assert.That(keepAlive, Is.Not.Zero);
            Assert.That(close.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(repeatedClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(listener.ReadDiameterListenerMetadata().Open, Is.False);
        });
    }

    [Test]
    public void Ipv6ListenerAcceptsWhenIpv6IsAvailable()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are not supported on this host.");
        }

        int port = ReservePort(IPAddress.IPv6Loopback);
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterListenerResult created = factory.CreateDiameterListener(
            CreateListenerConfiguration(IPAddress.IPv6Loopback, port),
            CreateUnsecuredSecurity());
        Assert.That(created.Status, Is.EqualTo(DiameterTransportStatus.Success));
        DiameterConnectionResult active = factory.ConnectDiameterEndpoint(
            CreateEndpoint(IPAddress.IPv6Loopback, port),
            CreateActiveConfiguration(true),
            CreateUnsecuredSecurity(),
            2000);
        Assert.That(active.Status, Is.EqualTo(DiameterTransportStatus.Success));
        DiameterConnectionResult accepted = created.Listener!
            .AcceptDiameterConnection(2000);
        Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
        byte[] data = new byte[] { 2, 4, 6, 8 };
        DiameterTransportIoResult sent = active.Connection!
            .SendDiameterConnectionBytes(data, 1000);
        byte[] received = new byte[data.Length];
        DiameterTransportIoResult read = accepted.Connection!
            .ReceiveDiameterConnectionBytes(received, 1000);
        accepted.Connection!.CloseDiameterConnection();
        active.Connection!.CloseDiameterConnection();
        created.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(active.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(sent.TransferredCount, Is.EqualTo(data.Length));
            Assert.That(read.TransferredCount, Is.EqualTo(data.Length));
            Assert.That(received, Is.EqualTo(data));
            Assert.That(accepted.Metadata.LocalEndpoint.Address!.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(accepted.Metadata.RemoteEndpoint.Address!.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetworkV6));
        });
    }

    [Test]
    public void ListenerCloseUnblocksWaitersAndStopsAcceptWorkersIdempotently()
    {
        int port = ReservePort(IPAddress.Loopback);
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterListenerResult created = factory.CreateDiameterListener(
            CreateListenerConfiguration(IPAddress.Loopback, port),
            CreateUnsecuredSecurity());
        Assert.That(created.Status, Is.EqualTo(DiameterTransportStatus.Success));
        TcpSocketDiameterListener listener =
            (TcpSocketDiameterListener)created.Listener!;
        Task<DiameterConnectionResult> waiting = Task.Run(() =>
            listener.AcceptDiameterConnection(5000));
        Assert.That(SpinWait.SpinUntil(() => listener.AcceptWaiterCount == 1, 1000),
            Is.True);

        Task<DiameterTransportOperationResult> firstClose = Task.Run(() =>
            listener.CloseDiameterListener());
        Task<DiameterTransportOperationResult> secondClose = Task.Run(() =>
            listener.CloseDiameterListener());
        Assert.That(Task.WaitAll(
            new Task[] { waiting, firstClose, secondClose },
            2000), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Result.Status, Is.EqualTo(DiameterTransportStatus.Closed));
            Assert.That(firstClose.Result.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(secondClose.Result.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(listener.StoppedWorkerCount, Is.EqualTo(listener.Workers.Count));
            Assert.That(listener.Workers.TrueForAll(worker => !worker.IsAlive), Is.True);
            Assert.That(listener.ReadDiameterListenerMetadata().Open, Is.False);
            Assert.That(listener.IsTransientTcpAcceptError(SocketError.Interrupted), Is.True);
            Assert.That(listener.IsTransientTcpAcceptError(SocketError.WouldBlock), Is.True);
            Assert.That(listener.IsTransientTcpAcceptError(
                SocketError.NoBufferSpaceAvailable), Is.True);
            Assert.That(listener.IsTransientTcpAcceptError(
                SocketError.AddressAlreadyInUse), Is.False);
        });
    }

    [Test]
    public void ListenerRetainsOwnershipOfUnclaimedConnectionsUntilHandoff()
    {
        int port = ReservePort(IPAddress.Loopback);
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterListenerResult created = factory.CreateDiameterListener(
            CreateListenerConfiguration(IPAddress.Loopback, port),
            CreateUnsecuredSecurity());
        Assert.That(created.Status, Is.EqualTo(DiameterTransportStatus.Success));
        TcpSocketDiameterListener listener =
            (TcpSocketDiameterListener)created.Listener!;
        DiameterConnectionResult active = factory.ConnectDiameterEndpoint(
            CreateEndpoint(IPAddress.Loopback, port),
            CreateActiveConfiguration(false),
            CreateUnsecuredSecurity(),
            2000);
        Assert.That(active.Status, Is.EqualTo(DiameterTransportStatus.Success));
        Assert.That(SpinWait.SpinUntil(
            () => listener.AcceptedConnections.Count == 1,
            1000), Is.True);
        TcpSocketDiameterConnection unclaimed = listener.AcceptedConnections.Peek();

        DiameterTransportOperationResult close = listener.CloseDiameterListener();
        DiameterTransportIoResult peerEnd = active.Connection!
            .ReceiveDiameterConnectionBytes(new byte[4], 2000);
        active.Connection!.CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(close.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(listener.AcceptedConnections, Is.Empty);
            Assert.That(unclaimed.ReadDiameterConnectionMetadata().Open, Is.False);
            Assert.That(peerEnd.Status, Is.EqualTo(DiameterTransportStatus.EndOfStream));
            Assert.That(peerEnd.EndOfStream, Is.True);
        });
    }

    private static int ReservePort(IPAddress address)
    {
        TcpListener listener = new TcpListener(address, 0);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            listener.Server.DualMode = false;
        }

        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static DiameterTransportConfiguration CreateListenerConfiguration(
        IPAddress address,
        int port)
    {
        bool ipv6 = address.AddressFamily == AddressFamily.InterNetworkV6;
        DiameterTransportConfiguration configuration = CreateActiveConfiguration(ipv6);
        configuration.Host = address.ToString();
        configuration.Port = port;
        configuration.EnableListener = true;
        configuration.ListenBacklog = 2;
        return configuration;
    }

    private static DiameterTransportConfiguration CreateActiveConfiguration(bool ipv6)
    {
        DiameterTransportConfiguration configuration = new DiameterTransportConfiguration();
        configuration.EnableIpv4 = !ipv6;
        configuration.EnableIpv6 = ipv6;
        configuration.NoDelay = true;
        configuration.KeepAlive = true;
        return configuration;
    }

    private static DiameterSecurityConfiguration CreateUnsecuredSecurity()
    {
        DiameterSecurityConfiguration security = new DiameterSecurityConfiguration();
        security.AllowUnsecuredTransport = true;
        return security;
    }

    private static DiameterEndpoint CreateEndpoint(IPAddress address, int port)
    {
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = address.ToString();
        endpoint.Address = address;
        endpoint.Port = port;
        return endpoint;
    }
}
