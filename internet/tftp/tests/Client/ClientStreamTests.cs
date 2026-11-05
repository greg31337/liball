using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ClientStreamTests
{
    [Test]
    public void DownloadsNegotiatedContentAndReturnsIndependentSnapshots()
    {
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            TftpPacket requestPacket = ReceivePacket(server, ref client);
            Assert.That(requestPacket.Opcode, Is.EqualTo(TftpOpcode.ReadRequest));
            Assert.That(requestPacket.Options, Has.Count.EqualTo(2));
            SendPacket(server, CreateOack(
                CreateOption("blksize", "8"),
                CreateOption("tsize", "3")), client);
            TftpPacket ackZero = ReceivePacket(server, ref client);
            Assert.That(ackZero.BlockNumber, Is.Zero);
            SendPacket(server, CreateData(1, new byte[] { 1, 2, 3 }), client);
            TftpPacket finalAck = ReceivePacket(server, ref client);
            Assert.That(finalAck.BlockNumber, Is.EqualTo(1));
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpClient client = new TftpClient(observer);
        MemoryStream destination = new MemoryStream();
        TftpReadRequest request = CreateReadRequest(server);
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 8;
        request.Options.RequestTransferSize = true;
        request.Options.DallyDurationSeconds = 0;

        TftpTransferResult result = client.DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(destination.CanWrite, Is.True);
            Assert.That(result.Direction, Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.BytesTransferred, Is.EqualTo(3));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(3));
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(8));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(3));
            Assert.That(result.NegotiatedOptions, Has.Count.EqualTo(2));
            Assert.That(result.FinalEndpoint.Port,
                Is.EqualTo(((IPEndPoint)server.LocalEndPoint!).Port));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Created));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Resolving));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Requesting));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Negotiating));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Dallying));
            Assert.That(observer.States[^1],
                Is.EqualTo(TftpTransferState.Completed));
            Assert.That(observer.Events[^1].BytesTransferred, Is.EqualTo(3));
        });

        TftpTransferEvent negotiationEvent = observer.Events[
            observer.States.IndexOf(TftpTransferState.Negotiating)];
        negotiationEvent.NegotiatedOptions[0].Value = "changed";
        negotiationEvent.Peer.Port = 1;
        Assert.Multiple(() =>
        {
            Assert.That(result.NegotiatedOptions[0].Value, Is.EqualTo("8"));
            Assert.That(result.FinalEndpoint.Port,
                Is.EqualTo(((IPEndPoint)server.LocalEndPoint!).Port));
        });
    }

    [Test]
    public void UploadsContentThroughLegacyAckZeroAndLeavesSourceOpen()
    {
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            TftpPacket requestPacket = ReceivePacket(server, ref client);
            Assert.That(requestPacket.Opcode, Is.EqualTo(TftpOpcode.WriteRequest));
            SendPacket(server, CreateAck(0), client);
            TftpPacket data = ReceivePacket(server, ref client);
            Assert.Multiple(() =>
            {
                Assert.That(data.BlockNumber, Is.EqualTo(1));
                Assert.That(data.Payload, Is.EqualTo(new byte[] { 4, 5, 6 }));
            });
            SendPacket(server, CreateAck(1), client);
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpClient client = new TftpClient(observer);
        MemoryStream source = new MemoryStream(new byte[] { 4, 5, 6 });
        TftpWriteRequest request = CreateWriteRequest(server);

        TftpTransferResult result = client.UploadFromStream(
            request,
            source,
            CancellationToken.None);
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(source.CanRead, Is.True);
            Assert.That(result.Direction, Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.BytesTransferred, Is.EqualTo(3));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(3));
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.HasTransferSize, Is.False);
            Assert.That(observer.Events[^1].Direction,
                Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(observer.Events[^1].State,
                Is.EqualTo(TftpTransferState.Completed));
        });
    }

    [Test]
    public void RetransmitsReadRequestAndReportsMetrics()
    {
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            TftpPacket first = ReceivePacket(server, ref client);
            TftpPacket second = ReceivePacket(server, ref client);
            Assert.Multiple(() =>
            {
                Assert.That(first.Opcode, Is.EqualTo(TftpOpcode.ReadRequest));
                Assert.That(second.Opcode, Is.EqualTo(TftpOpcode.ReadRequest));
            });
            SendPacket(server, CreateData(1, new byte[] { 9 }), client);
            ReceivePacket(server, ref client);
        });
        TftpClient client = new TftpClient(null);
        TftpReadRequest request = CreateReadRequest(server);
        request.Options.InitialTimeoutSeconds = 1;
        request.Options.MaximumTimeoutSeconds = 1;
        request.Options.MaximumRetries = 2;
        request.Options.DallyDurationSeconds = 0;

        TftpTransferResult result = client.DownloadToStream(
            request,
            new MemoryStream(),
            CancellationToken.None);
        peer.GetAwaiter().GetResult();

        Assert.That(result.RetransmissionCount, Is.EqualTo(1));
    }

    [Test]
    public void SurfacesRemoteErrorWithoutWrappingIt()
    {
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
            SendPacket(server, CreateError(
                TftpErrorCode.FileNotFound,
                "missing"), client);
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpClient client = new TftpClient(observer);

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            client.DownloadToStream(
                CreateReadRequest(server),
                new MemoryStream(),
                CancellationToken.None));
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(TftpErrorCode.FileNotFound));
            Assert.That(exception.RemoteMessage, Is.EqualTo("missing"));
            Assert.That(observer.Events[^1].State,
                Is.EqualTo(TftpTransferState.Failed));
            Assert.That(observer.Events[^1].HasError, Is.True);
            Assert.That(observer.Events[^1].ErrorCode,
                Is.EqualTo(TftpErrorCode.FileNotFound));
        });
    }

    [Test]
    public void SendsErrorEightAndSurfacesInvalidOptionAcknowledgement()
    {
        using Socket server = CreateServerSocket();
        TftpPacket? clientError = null;
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
            SendPacket(server, CreateOack(
                CreateOption("blksize", "8")), client);
            clientError = ReceivePacket(server, ref client);
        });
        TftpClient client = new TftpClient(null);

        TftpOptionNegotiationException exception =
            Assert.Throws<TftpOptionNegotiationException>(() =>
                client.UploadFromStream(
                    CreateWriteRequest(server),
                    new MemoryStream(),
                    CancellationToken.None));
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(exception.OptionName, Is.EqualTo("blksize"));
            Assert.That(clientError!.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(clientError.ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
        });
    }

    [Test]
    public void CancellationInterruptsSocketWaitingAndLeavesDestinationOpen()
    {
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
        });
        TftpClient client = new TftpClient(null);
        MemoryStream destination = new MemoryStream();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(100);

        Assert.Throws<OperationCanceledException>(() =>
            client.DownloadToStream(
                CreateReadRequest(server),
                destination,
                cancellation.Token));
        peer.GetAwaiter().GetResult();

        Assert.That(destination.CanWrite, Is.True);
    }

    [Test]
    public void SurfacesTimeoutWithoutWrappingIt()
    {
        using Socket server = CreateServerSocket();
        TftpClient client = new TftpClient(null);
        TftpReadRequest request = CreateReadRequest(server);
        request.Options.InitialTimeoutSeconds = 1;
        request.Options.MaximumTimeoutSeconds = 1;
        request.Options.MaximumRetries = 0;

        TftpTimeoutException exception = Assert.Throws<TftpTimeoutException>(() =>
            client.DownloadToStream(
                request,
                new MemoryStream(),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.RetryLimitReached, Is.True);
            Assert.That(exception.RetransmissionCount, Is.Zero);
            Assert.That(exception.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(1000));
        });
    }

    [Test]
    public void SurfacesSourceReadFailureWithoutWrappingOrClosingStream()
    {
        using Socket server = CreateServerSocket();
        Task peer = Task.Run(() =>
        {
            EndPoint client = new IPEndPoint(IPAddress.Any, 0);
            ReceivePacket(server, ref client);
            SendPacket(server, CreateAck(0), client);
        });
        TftpClient client = new TftpClient(null);
        ThrowingReadClientStream source = new ThrowingReadClientStream();

        IOException exception = Assert.Throws<IOException>(() =>
            client.UploadFromStream(
                CreateWriteRequest(server),
                source,
                CancellationToken.None));
        peer.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("source failed"));
            Assert.That(source.CanRead, Is.True);
        });
    }

    [Test]
    public void ValidatesRequestsAndStreamCapabilitiesBeforeTransfer()
    {
        TftpClient client = new TftpClient(null);
        TftpReadRequest read = new TftpReadRequest();
        read.Endpoint = null!;
        TftpWriteRequest write = new TftpWriteRequest();
        write.Options = null!;
        MemoryStream closed = new MemoryStream();
        closed.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                client.DownloadToStream(
                    null!, new MemoryStream(), CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.DownloadToStream(
                    read, new MemoryStream(), CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.DownloadToStream(
                    new TftpReadRequest(), null!, CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.DownloadToStream(
                    new TftpReadRequest(), closed, CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.UploadFromStream(
                    null!, new MemoryStream(), CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.UploadFromStream(
                    write, new MemoryStream(), CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.UploadFromStream(
                    new TftpWriteRequest(), null!, CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                client.UploadFromStream(
                    new TftpWriteRequest(), closed, CancellationToken.None));
        });
    }

    [Test]
    public void ObserverFailurePropagatesDirectlyBeforeNetworkWork()
    {
        TftpClient client = new TftpClient(new ThrowingClientObserver());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            client.DownloadToStream(
                new TftpReadRequest(),
                new MemoryStream(),
                CancellationToken.None));

        Assert.That(exception.Message, Is.EqualTo("observer failed"));
    }

    private static Socket CreateServerSocket()
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.ReceiveTimeout = 5000;
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return socket;
    }

    private static TftpReadRequest CreateReadRequest(Socket server)
    {
        TftpReadRequest request = new TftpReadRequest();
        request.Endpoint.Host = "127.0.0.1";
        request.Endpoint.Port = ((IPEndPoint)server.LocalEndPoint!).Port;
        request.RemoteFilename = "remote.bin";
        return request;
    }

    private static TftpWriteRequest CreateWriteRequest(Socket server)
    {
        TftpWriteRequest request = new TftpWriteRequest();
        request.Endpoint.Host = "127.0.0.1";
        request.Endpoint.Port = ((IPEndPoint)server.LocalEndPoint!).Port;
        request.RemoteFilename = "remote.bin";
        return request;
    }

    private static TftpPacket ReceivePacket(Socket socket, ref EndPoint remote)
    {
        byte[] buffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        int length = socket.ReceiveFrom(buffer, ref remote);
        return new TftpPacketCodec().Decode(
            buffer.AsSpan(0, length),
            TftpProtocolValues.MaximumBlockSize);
    }

    private static void SendPacket(
        Socket socket,
        TftpPacket packet,
        EndPoint remote)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, encoded.AsSpan());
        socket.SendTo(encoded, remote);
    }

    private static TftpPacket CreateData(ushort blockNumber, byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = blockNumber;
        packet.Payload = payload;
        return packet;
    }

    private static TftpPacket CreateAck(ushort blockNumber)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = blockNumber;
        return packet;
    }

    private static TftpPacket CreateError(
        TftpErrorCode errorCode,
        string message)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = errorCode;
        packet.ErrorMessage = message;
        return packet;
    }

    private static TftpPacket CreateOack(
        TftpOption first,
        TftpOption second)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        packet.Options.Add(first);
        packet.Options.Add(second);
        return packet;
    }

    private static TftpPacket CreateOack(TftpOption option)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        packet.Options.Add(option);
        return packet;
    }

    private static TftpOption CreateOption(string name, string value)
    {
        TftpOption option = new TftpOption();
        option.Name = name;
        option.Value = value;
        return option;
    }
}

internal class RecordingClientObserver : ITftpTransferObserver
{
    internal List<TftpTransferEvent> Events = new List<TftpTransferEvent>();
    internal List<TftpTransferState> States = new List<TftpTransferState>();

    public void Observe(TftpTransferEvent transferEvent)
    {
        Events.Add(transferEvent);
        States.Add(transferEvent.State);
    }
}

internal class ThrowingClientObserver : ITftpTransferObserver
{
    public void Observe(TftpTransferEvent transferEvent)
    {
        throw new InvalidOperationException("observer failed");
    }
}

internal class ThrowingReadClientStream : MemoryStream
{
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new IOException("source failed");
    }
}
