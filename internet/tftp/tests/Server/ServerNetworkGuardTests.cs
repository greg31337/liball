using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ServerNetworkGuardTests
{
    [Test]
    public void SuppressesLimitedBroadcastRequestsWhereSupported()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("broadcast.bin", [1]);
        TftpServerOptions options = Options(2, 64);
        options.Bindings.Add(Binding(IPAddress.Any));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        client.Socket.EnableBroadcast = true;
        byte[] request = Encode(ReadRequest("broadcast.bin"));
        IPEndPoint broadcastEndPoint = new IPEndPoint(
            IPAddress.Broadcast,
            harness.ListenerEndPoints[0].Port);

        try
        {
            client.Socket.SendTo(request, broadcastEndPoint);
        }
        catch (SocketException exception)
        {
            Assert.Ignore(
                "This host cannot send limited broadcasts: " +
                exception.SocketErrorCode);
        }

        AssertNoPacket(client);
        Thread.Sleep(100);
        Assert.Multiple(() =>
        {
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.Zero);
            Assert.That(storage.OpenReadCount, Is.Zero);
        });
        harness.StopGracefully();
    }

    [Test]
    public void SuppressesIpv4MulticastRequestsWhereSupported()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("multicast.bin", [2]);
        TftpServerOptions options = Options(2, 64);
        options.Bindings.Add(Binding(IPAddress.Any));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        IPAddress group = IPAddress.Parse("239.255.42.42");
        TftpServerListenerLifecycle lifecycle =
            harness.Server.CurrentLifecycle!;
        Socket listenerSocket = lifecycle.ListenerSockets[0].Socket;

        try
        {
            listenerSocket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.AddMembership,
                new MulticastOption(group, IPAddress.Loopback));
            client.Socket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.MulticastInterface,
                IPAddress.Loopback.GetAddressBytes());
            client.Socket.SetSocketOption(
                SocketOptionLevel.IP,
                SocketOptionName.MulticastTimeToLive,
                1);
            client.Socket.MulticastLoopback = true;
            client.Socket.SendTo(
                Encode(ReadRequest("multicast.bin")),
                new IPEndPoint(group, harness.ListenerEndPoints[0].Port));
        }
        catch (SocketException exception)
        {
            Assert.Ignore(
                "This host cannot exercise IPv4 multicast: " +
                exception.SocketErrorCode);
        }

        AssertNoPacket(client);
        Thread.Sleep(100);
        Assert.Multiple(() =>
        {
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.Zero);
            Assert.That(storage.OpenReadCount, Is.Zero);
        });
        harness.StopGracefully();
    }

    [Test]
    public void WrongTidErrorUsesTransferSourceAndConfiguredPacketCap()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("a", [3]);
        TftpServerOptions options = Options(1, 12);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient expectedClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        using LifecycleRawClient wrongClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        expectedClient.SendPacket(
            harness.ListenerEndPoints[0],
            ReadRequest("a"));
        GuardReceivedDatagram data = ReceiveDatagram(expectedClient);
        wrongClient.SendPacket(
            data.SourceEndPoint,
            Acknowledgement(1));
        GuardReceivedDatagram error = ReceiveDatagram(wrongClient);
        expectedClient.SendPacket(
            data.SourceEndPoint,
            Acknowledgement(1));
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(data.SourceEndPoint.Port,
                Is.Not.EqualTo(harness.ListenerEndPoints[0].Port));
            Assert.That(error.SourceEndPoint,
                Is.EqualTo(data.SourceEndPoint));
            Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            Assert.That(error.Bytes.Length, Is.LessThanOrEqualTo(12));
            Assert.That(wrongClient.Socket.LocalEndPoint, Is.Not.EqualTo(
                expectedClient.Socket.LocalEndPoint));
            Assert.That(harness.Server.GetStatus().FailedTransferCount,
                Is.Zero);
        });
    }

    [Test]
    public void EnforcesExactAndOversizedInitialRequestCaps()
    {
        string exactFilename = new string('a', 23);
        string oversizedFilename = new string('b', 24);
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add(exactFilename, [5]);
        TftpServerOptions options = Options(1, 32);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        byte[] oversized = Encode(ReadRequest(oversizedFilename));
        Assert.That(oversized, Has.Length.EqualTo(33));
        client.Socket.SendTo(oversized, harness.ListenerEndPoints[0]);
        GuardReceivedDatagram lengthError = ReceiveDatagram(client);
        client.Socket.SendTo(new byte[100], harness.ListenerEndPoints[0]);
        GuardReceivedDatagram truncationError = ReceiveDatagram(client);

        byte[] exact = Encode(ReadRequest(exactFilename));
        Assert.That(exact, Has.Length.EqualTo(32));
        client.Socket.SendTo(exact, harness.ListenerEndPoints[0]);
        GuardReceivedDatagram data = ReceiveDatagram(client);
        client.SendPacket(data.SourceEndPoint, Acknowledgement(1));
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(lengthError.SourceEndPoint,
                Is.EqualTo(harness.ListenerEndPoints[0]));
            Assert.That(lengthError.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(lengthError.Bytes.Length, Is.LessThanOrEqualTo(32));
            Assert.That(truncationError.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(truncationError.Bytes.Length, Is.LessThanOrEqualTo(32));
            Assert.That(data.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(data.Packet.Payload, Is.EqualTo(new byte[] { 5 }));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void BoundsListenerErrorsAtTheMinimumConfiguredPacketSize()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        TftpServerOptions options = Options(1, 5);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        client.Socket.SendTo(new byte[] { 0 }, harness.ListenerEndPoints[0]);
        GuardReceivedDatagram error = ReceiveDatagram(client);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(error.SourceEndPoint,
                Is.EqualTo(harness.ListenerEndPoints[0]));
            Assert.That(error.Bytes, Has.Length.EqualTo(5));
            Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(error.Packet.ErrorMessage, Is.Empty);
            Assert.That(storage.OpenReadCount, Is.Zero);
        });
    }

    [Test]
    public void RejectsMalformedInitialDatagramsWithoutCreatingSessions()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        TftpServerOptions options = Options(2, 64);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        byte[][] malformedDatagrams =
        [
            [],
            [0],
            [0, 4, 0, 0],
            [0, 1, 102, 0, 111],
            [0, 9]
        ];

        foreach (byte[] malformed in malformedDatagrams)
        {
            client.Socket.SendTo(malformed, harness.ListenerEndPoints[0]);
            GuardReceivedDatagram error = ReceiveDatagram(client);
            Assert.Multiple(() =>
            {
                Assert.That(error.SourceEndPoint,
                    Is.EqualTo(harness.ListenerEndPoints[0]));
                Assert.That(error.Packet.Opcode,
                    Is.EqualTo(TftpOpcode.Error));
                Assert.That(error.Packet.ErrorCode,
                    Is.EqualTo(TftpErrorCode.IllegalOperation));
                Assert.That(error.Bytes.Length, Is.LessThanOrEqualTo(64));
            });
        }

        client.Socket.SendTo(
            [0, 5, 0, 0, 0],
            harness.ListenerEndPoints[0]);
        AssertNoPacket(client);
        TftpServerStatus status = harness.Server.GetStatus();
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(status.IsRunning, Is.True);
            Assert.That(status.ActiveTransferCount, Is.Zero);
            Assert.That(status.AcceptedTransferCount, Is.Zero);
            Assert.That(status.CompletedTransferCount, Is.Zero);
            Assert.That(status.FailedTransferCount, Is.Zero);
            Assert.That(storage.OpenReadCount, Is.Zero);
        });
    }

    [Test]
    public void BoundsMalformedPeerErrorAndDoesNotRetransmitIt()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("a", [8]);
        TftpServerOptions options = Options(1, 12);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        client.SendPacket(harness.ListenerEndPoints[0], ReadRequest("a"));
        GuardReceivedDatagram data = ReceiveDatagram(client);
        client.Socket.SendTo(new byte[] { 0 }, data.SourceEndPoint);
        GuardReceivedDatagram error = ReceiveDatagram(client);
        harness.WaitForFailedTransfers(1);
        AssertNoPacket(client);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(error.SourceEndPoint, Is.EqualTo(data.SourceEndPoint));
            Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(error.Bytes.Length, Is.LessThanOrEqualTo(12));
            Assert.That(harness.Server.GetStatus().FailedTransferCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void EnforcesSessionAndTransferResourceLimits()
    {
        LifecycleReadStorage sessionStorage = new LifecycleReadStorage();
        sessionStorage.Files.Add("active.bin", new byte[600]);
        sessionStorage.Files.Add("limited.bin", [1]);
        TftpServerOptions sessionOptions = Options(1, 64);
        sessionOptions.Bindings.Add(Binding(IPAddress.Loopback));
        using (LifecycleServerHarness sessionHarness =
            new LifecycleServerHarness(
                sessionStorage,
                sessionOptions,
                1,
                CancellationToken.None))
        using (LifecycleRawClient activeClient =
            new LifecycleRawClient(AddressFamily.InterNetwork))
        using (LifecycleRawClient limitedClient =
            new LifecycleRawClient(AddressFamily.InterNetwork))
        {
            activeClient.SendPacket(
                sessionHarness.ListenerEndPoints[0],
                ReadRequest("active.bin"));
            GuardReceivedDatagram firstData = ReceiveDatagram(activeClient);
            limitedClient.SendPacket(
                sessionHarness.ListenerEndPoints[0],
                ReadRequest("limited.bin"));
            GuardReceivedDatagram limited = ReceiveDatagram(limitedClient);
            activeClient.SendPacket(
                firstData.SourceEndPoint,
                Acknowledgement(1));
            GuardReceivedDatagram finalData = ReceiveDatagram(activeClient);
            activeClient.SendPacket(
                finalData.SourceEndPoint,
                Acknowledgement(2));
            sessionHarness.WaitForCompletedTransfers(1);
            sessionHarness.StopGracefully();
            Assert.Multiple(() =>
            {
                Assert.That(limited.Packet.ErrorCode,
                    Is.EqualTo(TftpErrorCode.StorageExhausted));
                Assert.That(limited.SourceEndPoint,
                    Is.EqualTo(sessionHarness.ListenerEndPoints[0]));
                Assert.That(sessionHarness.Server.GetStatus().AcceptedTransferCount,
                    Is.EqualTo(1));
            });
        }

        LifecycleReadStorage transferStorage = new LifecycleReadStorage();
        transferStorage.Files.Add("large.bin", [1, 2, 3]);
        TftpServerOptions transferOptions = Options(1, 64);
        transferOptions.MaximumTransferSize = 2;
        transferOptions.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness transferHarness =
            new LifecycleServerHarness(
                transferStorage,
                transferOptions,
                1,
                CancellationToken.None);
        using LifecycleRawClient transferClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        transferClient.SendPacket(
            transferHarness.ListenerEndPoints[0],
            ReadRequest("large.bin"));
        GuardReceivedDatagram capacity = ReceiveDatagram(transferClient);
        transferHarness.WaitForFailedTransfers(1);
        transferHarness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(capacity.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(capacity.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(capacity.SourceEndPoint.Port,
                Is.Not.EqualTo(transferHarness.ListenerEndPoints[0].Port));
            Assert.That(transferHarness.Server.GetStatus().FailedTransferCount,
                Is.EqualTo(1));
        });
    }

    private static void AssertNoPacket(LifecycleRawClient client)
    {
        int previousTimeout = client.Socket.ReceiveTimeout;
        client.Socket.ReceiveTimeout = 250;
        SocketException exception = Assert.Throws<SocketException>(
            () => client.ReceivePacket())!;
        client.Socket.ReceiveTimeout = previousTimeout;
        Assert.That(
            exception.SocketErrorCode,
            Is.AnyOf(SocketError.TimedOut, SocketError.WouldBlock));
    }

    private static GuardReceivedDatagram ReceiveDatagram(
        LifecycleRawClient client)
    {
        byte[] buffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        IPAddress anyAddress = IPAddress.Any;
        if (client.Socket.AddressFamily == AddressFamily.InterNetworkV6)
        {
            anyAddress = IPAddress.IPv6Any;
        }

        EndPoint source = new IPEndPoint(anyAddress, 0);
        int length = client.Socket.ReceiveFrom(buffer, ref source);
        GuardReceivedDatagram received = new GuardReceivedDatagram();
        received.Bytes = buffer.AsSpan(0, length).ToArray();
        received.Packet = new TftpPacketCodec().Decode(
            received.Bytes,
            TftpProtocolValues.MaximumBlockSize);
        received.SourceEndPoint = (IPEndPoint)source;
        return received;
    }

    private static byte[] Encode(TftpPacket packet)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] bytes = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, bytes.AsSpan());
        return bytes;
    }

    private static TftpServerOptions Options(
        int maximumSessions,
        int maximumRequestPacketLength)
    {
        TftpServerOptions options = new TftpServerOptions();
        options.MaximumSessions = maximumSessions;
        options.MaximumRequestPacketLength = maximumRequestPacketLength;
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 1;
        options.MaximumRetries = 1;
        options.MaximumSessionDurationSeconds = 10;
        options.DallyDurationSeconds = 0;
        return options;
    }

    private static TftpServerBinding Binding(IPAddress address)
    {
        TftpServerBinding binding = new TftpServerBinding();
        binding.Address = address;
        binding.Port = 0;
        return binding;
    }

    private static TftpPacket ReadRequest(string filename)
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = filename;
        request.Mode = TftpProtocolValues.OctetMode;
        return request;
    }

    private static TftpPacket Acknowledgement(ushort blockNumber)
    {
        TftpPacket acknowledgement = new TftpPacket();
        acknowledgement.Opcode = TftpOpcode.Acknowledgement;
        acknowledgement.BlockNumber = blockNumber;
        return acknowledgement;
    }
}

internal class GuardReceivedDatagram
{
    internal byte[] Bytes = [];
    internal TftpPacket Packet = new TftpPacket();
    internal IPEndPoint SourceEndPoint =
        new IPEndPoint(IPAddress.Loopback, 0);
}
