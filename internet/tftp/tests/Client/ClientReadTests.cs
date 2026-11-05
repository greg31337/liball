using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ClientReadTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(511)]
    [TestCase(512)]
    [TestCase(513)]
    [TestCase(1024)]
    public void DownloadsEveryOptionlessContentBoundary(int contentLength)
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        byte[] content = CreateContent(contentLength);
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.Multiple(() =>
            {
                Assert.That(requestPacket.Opcode, Is.EqualTo(TftpOpcode.ReadRequest));
                Assert.That(requestPacket.Options, Is.Empty);
            });
            peer.SendContent(content, TftpProtocolValues.DefaultBlockSize);
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.DallyDurationSeconds = 0;
        MemoryStream destination = new MemoryStream();

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(result.BytesTransferred, Is.EqualTo(contentLength));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(contentLength));
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.NegotiatedOptions, Is.Empty);
        });
    }

    [Test]
    public void FallsBackToLegacyDataWhenOptionAwareRequestIsIgnored()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.That(requestPacket.Options, Has.Count.EqualTo(3));
            peer.SendClientPacket(CreateData(1, new byte[] { 1, 2 }));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 1024;
        request.Options.RequestTimeout = true;
        request.Options.TimeoutSeconds = 3;
        request.Options.RequestTransferSize = true;
        request.Options.DallyDurationSeconds = 0;
        MemoryStream destination = new MemoryStream();

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(result.NegotiatedOptions, Is.Empty);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.EffectiveTimeoutSeconds,
                Is.EqualTo(request.Options.InitialTimeoutSeconds));
            Assert.That(result.HasTransferSize, Is.False);
        });
    }

    [Test]
    public void RetriesRejectedOptionsWithoutReportingFalseFailure()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        Task script = Task.Run(() =>
        {
            TftpPacket firstRequest = peer.ReceiveClientPacket();
            Assert.That(firstRequest.Options, Is.Not.Empty);
            peer.SendClientPacket(CreateError(
                TftpErrorCode.OptionNegotiationFailed,
                "unsupported"));
            TftpPacket secondRequest = peer.ReceiveClientPacket();
            Assert.That(secondRequest.Options, Is.Empty);
            peer.SendClientPacket(CreateData(1, new byte[] { 7 }));
            peer.ReceiveClientPacket();
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 1024;
        request.Options.RetryWithoutOptions = true;
        request.Options.DallyDurationSeconds = 0;

        TftpTransferResult result = new TftpClient(observer).DownloadToStream(
            request,
            new MemoryStream(),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.NegotiatedOptions, Is.Empty);
            Assert.That(observer.States, Does.Not.Contain(TftpTransferState.Failed));
            Assert.That(observer.States[^1], Is.EqualTo(TftpTransferState.Completed));
        });
    }

    [Test]
    public void AppliesPartialOackAndDefaultsOmittedBlockSize()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.That(requestPacket.Options, Has.Count.EqualTo(2));
            peer.SendClientPacket(CreateOack(
                CreateOption("timeout", "2")));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.Zero);
            peer.SendClientPacket(CreateData(1, new byte[] { 1 }));
            peer.ReceiveClientPacket();
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 1024;
        request.Options.RequestTimeout = true;
        request.Options.TimeoutSeconds = 2;
        request.Options.DallyDurationSeconds = 0;

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            new MemoryStream(),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(result.NegotiatedOptions, Has.Count.EqualTo(1));
            Assert.That(result.NegotiatedOptions[0].Name,
                Is.EqualTo(TftpProtocolValues.TimeoutOption));
        });
    }

    [Test]
    public void RetransmitsLostAckZeroBeforeReceivingDataOne()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateOack(
                CreateOption("blksize", "8")));
            TftpPacket firstAck = peer.ReceiveClientPacket();
            TftpPacket repeatedAck = peer.ReceiveClientPacket();
            Assert.Multiple(() =>
            {
                Assert.That(firstAck.Opcode,
                    Is.EqualTo(TftpOpcode.Acknowledgement));
                Assert.That(firstAck.BlockNumber, Is.Zero);
                Assert.That(repeatedAck.BlockNumber, Is.Zero);
            });
            peer.SendClientPacket(CreateData(1, new byte[] { 6 }));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 8;
        request.Options.InitialTimeoutSeconds = 1;
        request.Options.MaximumTimeoutSeconds = 1;
        request.Options.MaximumRetries = 2;
        request.Options.DallyDurationSeconds = 0;

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            new MemoryStream(),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.That(result.RetransmissionCount, Is.EqualTo(1));
    }

    [Test]
    public void DecodesNetasciiAcrossNegotiatedDataBoundary()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        byte[] firstEncodedBlock =
            { 65, 65, 65, 65, 65, 65, 65, 13 };
        byte[] finalEncodedBlock = { 10, 13, 0, 66 };
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateOack(
                CreateOption("blksize", "8")));
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateData(1, firstEncodedBlock));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
            peer.SendClientPacket(CreateData(2, finalEncodedBlock));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(2));
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.Mode = TftpTransferMode.Netascii;
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 8;
        request.Options.DallyDurationSeconds = 0;
        MemoryStream destination = new MemoryStream();

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(
                new byte[] { 65, 65, 65, 65, 65, 65, 65, 10, 13, 66 }));
            Assert.That(result.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(result.BytesTransferred, Is.EqualTo(10));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(12));
        });
    }

    [Test]
    public void ReacknowledgesDuplicateDataWithoutWritingTwice()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        byte[] firstBlock = new byte[TftpProtocolValues.DefaultBlockSize];
        firstBlock[0] = 3;
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            TftpPacket firstData = CreateData(1, firstBlock);
            peer.SendClientPacket(firstData);
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
            peer.SendClientPacket(firstData);
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
            peer.SendClientPacket(CreateData(2, new byte[] { 4 }));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(2));
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.DallyDurationSeconds = 0;
        MemoryStream destination = new MemoryStream();

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(destination.Length, Is.EqualTo(513));
            Assert.That(result.BytesTransferred, Is.EqualTo(513));
            Assert.That(result.DuplicatePacketCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReacknowledgesRepeatedFinalDataDuringDally()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            TftpPacket finalData = CreateData(1, new byte[] { 5 });
            peer.SendClientPacket(finalData);
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
            peer.SendClientPacket(finalData);
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.DallyDurationSeconds = 1;

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            new MemoryStream(),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.BytesTransferred, Is.EqualTo(1));
            Assert.That(result.DuplicatePacketCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsUnexplainedFutureDataWithErrorFour()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        TftpPacket? clientError = null;
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateData(2, new byte[] { 1 }));
            clientError = peer.ReceiveClientPacket();
        });

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(() =>
            new TftpClient(null).DownloadToStream(
                peer.CreateReadRequest(),
                new MemoryStream(),
                CancellationToken.None));
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("DATA block 1"));
            Assert.That(clientError!.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(clientError.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
        });
    }

    [Test]
    public void RejectsWrongTidAndContinuesWithSelectedTransferSocket()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        using Socket transferSocket = peer.CreateSiblingSocket();
        using Socket wrongSocket = peer.CreateSiblingSocket();
        TftpPacket? wrongTidError = null;
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            byte[] fullBlock = new byte[TftpProtocolValues.DefaultBlockSize];
            peer.SendPacketFromSibling(
                transferSocket,
                CreateData(1, fullBlock));
            Assert.That(peer.ReceivePacketOnSibling(transferSocket).BlockNumber,
                Is.EqualTo(1));
            peer.SendPacketFromSibling(
                wrongSocket,
                CreateData(2, new byte[] { 8 }));
            wrongTidError = peer.ReceivePacketOnSibling(wrongSocket);
            peer.SendPacketFromSibling(
                transferSocket,
                CreateData(2, new byte[] { 9 }));
            Assert.That(peer.ReceivePacketOnSibling(transferSocket).BlockNumber,
                Is.EqualTo(2));
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.DallyDurationSeconds = 0;
        MemoryStream destination = new MemoryStream();

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(wrongTidError!.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(wrongTidError.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            Assert.That(destination.Length, Is.EqualTo(513));
            Assert.That(destination.ToArray()[512], Is.EqualTo(9));
            Assert.That(result.FinalEndpoint.Port,
                Is.EqualTo(((IPEndPoint)transferSocket.LocalEndPoint!).Port));
        });
    }

    [TestCase(TftpErrorCode.Undefined)]
    [TestCase(TftpErrorCode.FileNotFound)]
    [TestCase(TftpErrorCode.AccessViolation)]
    [TestCase(TftpErrorCode.StorageExhausted)]
    [TestCase(TftpErrorCode.IllegalOperation)]
    [TestCase(TftpErrorCode.UnknownTransferId)]
    [TestCase(TftpErrorCode.FileAlreadyExists)]
    [TestCase(TftpErrorCode.NoSuchUser)]
    [TestCase(TftpErrorCode.OptionNegotiationFailed)]
    public void SurfacesEveryRemoteErrorCode(TftpErrorCode errorCode)
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateError(errorCode, "remote failure"));
        });

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            new TftpClient(null).DownloadToStream(
                peer.CreateReadRequest(),
                new MemoryStream(),
                CancellationToken.None));
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(exception.RemoteMessage, Is.EqualTo("remote failure"));
        });
    }

    private static byte[] CreateContent(int length)
    {
        byte[] content = new byte[length];
        int index = 0;
        while (index < content.Length)
        {
            content[index] = (byte)(index % 251);
            index = index + 1;
        }

        return content;
    }

    private static TftpPacket CreateData(ushort blockNumber, byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = blockNumber;
        packet.Payload = payload;
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

internal class ScriptedReadPeer : IDisposable
{
    internal Socket Listener;
    internal EndPoint ClientEndPoint = new IPEndPoint(IPAddress.Any, 0);

    internal ScriptedReadPeer()
    {
        Listener = CreateBoundSocket();
    }

    internal TftpReadRequest CreateReadRequest()
    {
        TftpReadRequest request = new TftpReadRequest();
        request.Endpoint.Host = "127.0.0.1";
        request.Endpoint.Port = ((IPEndPoint)Listener.LocalEndPoint!).Port;
        request.RemoteFilename = "remote.bin";
        return request;
    }

    internal TftpPacket ReceiveClientPacket()
    {
        return ReceivePacketFromSocket(Listener, true);
    }

    internal void SendClientPacket(TftpPacket packet)
    {
        SendPacketToClient(Listener, packet);
    }

    internal void SendContent(byte[] content, int blockSize)
    {
        int offset = 0;
        ushort blockNumber = 1;
        while (content.Length - offset >= blockSize)
        {
            byte[] payload = new byte[blockSize];
            content.AsSpan(offset, blockSize).CopyTo(payload);
            SendClientPacket(CreateDataPacket(blockNumber, payload));
            TftpPacket acknowledgement = ReceiveClientPacket();
            Assert.That(acknowledgement.BlockNumber, Is.EqualTo(blockNumber));
            offset = offset + blockSize;
            blockNumber = (ushort)(blockNumber + 1);
        }

        int finalLength = content.Length - offset;
        byte[] finalPayload = new byte[finalLength];
        content.AsSpan(offset, finalLength).CopyTo(finalPayload);
        SendClientPacket(CreateDataPacket(blockNumber, finalPayload));
        TftpPacket finalAcknowledgement = ReceiveClientPacket();
        Assert.That(finalAcknowledgement.BlockNumber, Is.EqualTo(blockNumber));
    }

    internal Socket CreateSiblingSocket()
    {
        return CreateBoundSocket();
    }

    internal void SendPacketFromSibling(Socket socket, TftpPacket packet)
    {
        SendPacketToClient(socket, packet);
    }

    internal TftpPacket ReceivePacketOnSibling(Socket socket)
    {
        return ReceivePacketFromSocket(socket, false);
    }

    internal TftpPacket ReceivePacketFromSocket(
        Socket socket,
        bool updateClientEndpoint)
    {
        byte[] buffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        int length = socket.ReceiveFrom(buffer, ref remote);
        if (updateClientEndpoint)
        {
            ClientEndPoint = remote;
        }

        return new TftpPacketCodec().Decode(
            buffer.AsSpan(0, length),
            TftpProtocolValues.MaximumBlockSize);
    }

    internal void SendPacketToClient(Socket socket, TftpPacket packet)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, encoded.AsSpan());
        socket.SendTo(encoded, ClientEndPoint);
    }

    internal TftpPacket CreateDataPacket(
        ushort blockNumber,
        byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = blockNumber;
        packet.Payload = payload;
        return packet;
    }

    internal Socket CreateBoundSocket()
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.ReceiveTimeout = 5000;
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return socket;
    }

    public void Dispose()
    {
        Listener.Dispose();
    }
}
