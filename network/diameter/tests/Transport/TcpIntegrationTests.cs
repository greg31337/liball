using System;
using System.Net;
using System.Net.Sockets;

namespace diameter.Tests;

public class TcpIntegrationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void LoopbackFramingSurvivesIdlePeerCloseLimitsAndReconnect(bool ipv6)
    {
        if (ipv6 && !Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are not supported on this host.");
        }

        IPAddress address = ipv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
        int port = ReserveTcpPort(address);
        DiameterTcpTransportFactory factory = new DiameterTcpTransportFactory();
        DiameterSecurityConfiguration security = CreateUnsecuredSecurity();
        DiameterTransportConfiguration listenerConfiguration =
            CreateTcpConfiguration(address, port, true);
        DiameterListenerResult created = factory.CreateDiameterListener(
            listenerConfiguration,
            security);
        Assert.That(created.Status, Is.EqualTo(DiameterTransportStatus.Success),
            created.Code + ": " + created.Message);
        IDiameterListener listener = created.Listener!;
        DiameterEndpoint endpoint = CreateTcpEndpoint(address, port);
        DiameterTransportConfiguration activeConfiguration =
            CreateTcpConfiguration(address, DiameterProtocolConstants.Port, false);

        DiameterConnectionResult firstActive = factory.ConnectDiameterEndpoint(
            endpoint,
            activeConfiguration,
            security,
            3000);
        Assert.That(firstActive.Status, Is.EqualTo(DiameterTransportStatus.Success),
            firstActive.Code + ": " + firstActive.Message);
        DiameterConnectionResult firstAccepted = listener.AcceptDiameterConnection(3000);
        Assert.That(firstAccepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
        QueuedDiameterConnection framed = new QueuedDiameterConnection(
            firstAccepted.Connection!,
            2,
            2,
            8,
            64);

        DiameterTransportIoResult idle = framed.ReceiveDiameterConnectionBytes(
            new byte[64],
            250);
        byte[] fragmented = CreateDiameterFrame(101U, 20);
        DiameterTransportIoResult fragmentOne = firstActive.Connection!
            .SendDiameterConnectionBytes(fragmented.AsSpan(0, 3), 1000);
        DiameterTransportIoResult fragmentTwo = firstActive.Connection
            .SendDiameterConnectionBytes(fragmented.AsSpan(3, 5), 1000);
        DiameterTransportIoResult fragmentThree = firstActive.Connection
            .SendDiameterConnectionBytes(fragmented.AsSpan(8), 1000);
        byte[] coalescedFirst = CreateDiameterFrame(102U, 20);
        byte[] coalescedSecond = CreateDiameterFrame(103U, 24);
        byte[] coalesced = new byte[coalescedFirst.Length + coalescedSecond.Length];
        Array.Copy(coalescedFirst, 0, coalesced, 0, coalescedFirst.Length);
        Array.Copy(
            coalescedSecond,
            0,
            coalesced,
            coalescedFirst.Length,
            coalescedSecond.Length);
        DiameterTransportIoResult coalescedSend = firstActive.Connection
            .SendDiameterConnectionBytes(coalesced, 1000);
        DiameterTransportOperationResult peerClose = firstActive.Connection
            .CloseDiameterConnection();

        byte[] receivedFragmented = new byte[64];
        DiameterTransportIoResult fragmentedResult = framed
            .ReceiveDiameterConnectionBytes(receivedFragmented, 3000);
        byte[] receivedCoalescedFirst = new byte[64];
        DiameterTransportIoResult coalescedFirstResult = framed
            .ReceiveDiameterConnectionBytes(receivedCoalescedFirst, 3000);
        byte[] receivedCoalescedSecond = new byte[64];
        DiameterTransportIoResult coalescedSecondResult = framed
            .ReceiveDiameterConnectionBytes(receivedCoalescedSecond, 3000);
        DiameterTransportIoResult peerEnd = framed.ReceiveDiameterConnectionBytes(
            new byte[64],
            3000);
        DiameterTransportOperationResult framedClose = framed.CloseDiameterConnection();

        DiameterConnectionResult reconnected = factory.ConnectDiameterEndpoint(
            endpoint,
            activeConfiguration,
            security,
            3000);
        Assert.That(reconnected.Status, Is.EqualTo(DiameterTransportStatus.Success),
            reconnected.Code + ": " + reconnected.Message);
        DiameterConnectionResult reaccepted = listener.AcceptDiameterConnection(3000);
        Assert.That(reaccepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
        QueuedDiameterConnection limited = new QueuedDiameterConnection(
            reaccepted.Connection!,
            1,
            1,
            8,
            20);
        byte[] overLimit = CreateDiameterFrame(104U, 24);
        DiameterTransportIoResult overLimitSend = reconnected.Connection!
            .SendDiameterConnectionBytes(overLimit, 1000);
        DiameterTransportIoResult limit = limited.ReceiveDiameterConnectionBytes(
            new byte[24],
            3000);
        DiameterConnectionMetadata reconnectedMetadata =
            reconnected.Connection.ReadDiameterConnectionMetadata();
        DiameterListenerMetadata openListenerMetadata =
            listener.ReadDiameterListenerMetadata();
        reconnected.Connection.CloseDiameterConnection();
        limited.CloseDiameterConnection();
        DiameterTransportOperationResult listenerClose = listener.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(idle.Status, Is.EqualTo(DiameterTransportStatus.TimedOut));
            Assert.That(fragmentOne.TransferredCount, Is.EqualTo(3));
            Assert.That(fragmentTwo.TransferredCount, Is.EqualTo(5));
            Assert.That(fragmentThree.TransferredCount, Is.EqualTo(12));
            Assert.That(coalescedSend.TransferredCount, Is.EqualTo(coalesced.Length));
            Assert.That(peerClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(fragmentedResult.Status,
                Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(fragmentedResult.TransferredCount, Is.EqualTo(20));
            Assert.That(receivedFragmented.AsSpan(0, 20).ToArray(),
                Is.EqualTo(fragmented));
            Assert.That(coalescedFirstResult.Status,
                Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(coalescedFirstResult.TransferredCount, Is.EqualTo(20));
            Assert.That(receivedCoalescedFirst.AsSpan(0, 20).ToArray(),
                Is.EqualTo(coalescedFirst));
            Assert.That(coalescedSecondResult.Status,
                Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(coalescedSecondResult.TransferredCount, Is.EqualTo(24));
            Assert.That(receivedCoalescedSecond.AsSpan(0, 24).ToArray(),
                Is.EqualTo(coalescedSecond));
            Assert.That(peerEnd.Status, Is.EqualTo(DiameterTransportStatus.EndOfStream));
            Assert.That(peerEnd.EndOfStream, Is.True);
            Assert.That(framedClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(framed.IngressWorker.IsAlive, Is.False);
            Assert.That(framed.EgressWorker.IsAlive, Is.False);
            Assert.That(overLimitSend.TransferredCount, Is.EqualTo(24));
            Assert.That(limit.Status, Is.EqualTo(DiameterTransportStatus.Failed));
            Assert.That(limit.Code, Is.EqualTo("configured-frame-size-limit"));
            Assert.That(reconnectedMetadata.Open, Is.True);
            Assert.That(reconnectedMetadata.RemoteEndpoint.Address,
                Is.EqualTo(address));
            Assert.That(reaccepted.Metadata.LocalEndpoint.Address,
                Is.EqualTo(address));
            Assert.That(openListenerMetadata.Open, Is.True);
            Assert.That(listenerClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(listener.ReadDiameterListenerMetadata().Open, Is.False);
        });
    }

    private static int ReserveTcpPort(IPAddress address)
    {
        TcpListener listener = new TcpListener(address, 0);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            listener.Server.DualMode = false;
        }

        listener.Start();
        int result = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return result;
    }

    private static DiameterTransportConfiguration CreateTcpConfiguration(
        IPAddress address,
        int port,
        bool listener)
    {
        DiameterTransportConfiguration result = new DiameterTransportConfiguration();
        result.Host = address.ToString();
        result.Port = port;
        result.EnableIpv4 = address.AddressFamily == AddressFamily.InterNetwork;
        result.EnableIpv6 = address.AddressFamily == AddressFamily.InterNetworkV6;
        result.EnableListener = listener;
        result.EnableActiveConnections = !listener;
        result.ReceiveBufferSize = 1024;
        result.SendBufferSize = 1024;
        result.ListenBacklog = 4;
        return result;
    }

    private static DiameterSecurityConfiguration CreateUnsecuredSecurity()
    {
        DiameterSecurityConfiguration result = new DiameterSecurityConfiguration();
        result.AllowUnsecuredTransport = true;
        return result;
    }

    private static DiameterEndpoint CreateTcpEndpoint(IPAddress address, int port)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = "peer.example";
        result.Host = address.ToString();
        result.Address = address;
        result.Port = port;
        return result;
    }

    private static byte[] CreateDiameterFrame(uint identifier, int length)
    {
        byte[] result = new byte[length];
        result[0] = DiameterProtocolConstants.Version;
        result[1] = (byte)(length >> 16);
        result[2] = (byte)(length >> 8);
        result[3] = (byte)length;
        result[12] = (byte)(identifier >> 24);
        result[13] = (byte)(identifier >> 16);
        result[14] = (byte)(identifier >> 8);
        result[15] = (byte)identifier;
        return result;
    }
}
