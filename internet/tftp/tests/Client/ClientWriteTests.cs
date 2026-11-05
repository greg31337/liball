using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ClientWriteTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(511)]
    [TestCase(512)]
    [TestCase(513)]
    [TestCase(1024)]
    public void UploadsEveryOptionlessContentBoundary(int contentLength)
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        byte[] content = CreateContent(contentLength);
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.Multiple(() =>
            {
                Assert.That(requestPacket.Opcode, Is.EqualTo(TftpOpcode.WriteRequest));
                Assert.That(requestPacket.Options, Is.Empty);
            });
            peer.SendClientPacket(CreateAck(0));
            peer.ReceiveContentAndAcknowledge(
                content,
                TftpProtocolValues.DefaultBlockSize);
        });
        MemoryStream source = new MemoryStream(content);

        TftpTransferResult result = new TftpClient(null).UploadFromStream(
            peer.CreateWriteRequest(),
            source,
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(source.CanRead, Is.True);
            Assert.That(result.BytesTransferred, Is.EqualTo(contentLength));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(contentLength));
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.NegotiatedOptions, Is.Empty);
        });
    }

    [Test]
    public void AppliesCompleteOackAndReportsMetricsAndObserverEvents()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        byte[] content = CreateContent(10);
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.Multiple(() =>
            {
                Assert.That(requestPacket.Options, Has.Count.EqualTo(3));
                Assert.That(OptionValue(requestPacket, "blksize"), Is.EqualTo("8"));
                Assert.That(OptionValue(requestPacket, "timeout"), Is.EqualTo("2"));
                Assert.That(OptionValue(requestPacket, "tsize"), Is.EqualTo("10"));
            });
            peer.SendClientPacket(CreateOack(
                CreateOption("blksize", "8"),
                CreateOption("timeout", "2"),
                CreateOption("tsize", "10")));
            peer.ReceiveContentAndAcknowledge(content, 8);
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpWriteRequest request = peer.CreateWriteRequest();
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 8;
        request.Options.RequestTimeout = true;
        request.Options.TimeoutSeconds = 2;
        request.Options.RequestTransferSize = true;

        TftpTransferResult result = new TftpClient(observer).UploadFromStream(
            request,
            new MemoryStream(content),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Direction, Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.BytesTransferred, Is.EqualTo(10));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(10));
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(8));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(10));
            Assert.That(result.NegotiatedOptions, Has.Count.EqualTo(3));
            Assert.That(observer.States, Does.Contain(TftpTransferState.Created));
            Assert.That(observer.States, Does.Contain(TftpTransferState.Resolving));
            Assert.That(observer.States, Does.Contain(TftpTransferState.Requesting));
            Assert.That(observer.States, Does.Contain(TftpTransferState.Negotiating));
            Assert.That(observer.States[^1], Is.EqualTo(TftpTransferState.Completed));
            Assert.That(observer.Events[^1].BytesTransferred, Is.EqualTo(10));
            Assert.That(observer.Events[^1].EncodedBytesTransferred, Is.EqualTo(10));
        });
    }

    [Test]
    public void AppliesPartialOackAndDefaultsOmittedBlockSize()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.That(requestPacket.Options, Has.Count.EqualTo(2));
            peer.SendClientPacket(CreateOack(
                CreateOption("timeout", "2")));
            peer.ReceiveContentAndAcknowledge(new byte[] { 1 },
                TftpProtocolValues.DefaultBlockSize);
        });
        TftpWriteRequest request = peer.CreateWriteRequest();
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 1024;
        request.Options.RequestTimeout = true;
        request.Options.TimeoutSeconds = 2;

        TftpTransferResult result = new TftpClient(null).UploadFromStream(
            request,
            new MemoryStream(new byte[] { 1 }),
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
    public void FallsBackToLegacyAckZeroWhenOptionAwareRequestIsIgnored()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.That(requestPacket.Options, Has.Count.EqualTo(3));
            peer.SendClientPacket(CreateAck(0));
            peer.ReceiveContentAndAcknowledge(new byte[] { 1, 2 },
                TftpProtocolValues.DefaultBlockSize);
        });
        TftpWriteRequest request = peer.CreateWriteRequest();
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 1024;
        request.Options.RequestTimeout = true;
        request.Options.TimeoutSeconds = 3;
        request.Options.RequestTransferSize = true;

        TftpTransferResult result = new TftpClient(null).UploadFromStream(
            request,
            new MemoryStream(new byte[] { 1, 2 }),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
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
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        Task script = Task.Run(() =>
        {
            TftpPacket firstRequest = peer.ReceiveClientPacket();
            Assert.That(firstRequest.Options, Is.Not.Empty);
            peer.SendClientPacket(CreateError(
                TftpErrorCode.OptionNegotiationFailed,
                "unsupported"));
            TftpPacket secondRequest = peer.ReceiveClientPacket();
            Assert.That(secondRequest.Options, Is.Empty);
            peer.SendClientPacket(CreateAck(0));
            peer.ReceiveContentAndAcknowledge(new byte[] { 7 },
                TftpProtocolValues.DefaultBlockSize);
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpWriteRequest request = peer.CreateWriteRequest();
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 1024;
        request.Options.RetryWithoutOptions = true;

        TftpTransferResult result = new TftpClient(observer).UploadFromStream(
            request,
            new MemoryStream(new byte[] { 7 }),
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

    [TestCase(TftpTransferMode.Octet, true, true)]
    [TestCase(TftpTransferMode.Octet, false, false)]
    [TestCase(TftpTransferMode.Netascii, true, false)]
    public void HandlesTransferSizeForNonSeekableSources(
        TftpTransferMode mode,
        bool hasKnownSourceSize,
        bool expectsTransferSize)
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        byte[] content = { 10, 65 };
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.That(requestPacket.Mode,
                Is.EqualTo(mode == TftpTransferMode.Netascii ? "netascii" : "octet"));
            Assert.That(requestPacket.Options.Count,
                Is.EqualTo(expectsTransferSize ? 1 : 0));
            if (expectsTransferSize)
            {
                Assert.That(OptionValue(requestPacket, "tsize"), Is.EqualTo("2"));
            }

            peer.SendClientPacket(CreateAck(0));
            TftpPacket data = peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(data.BlockNumber));
            Assert.That(data.Payload, Is.EqualTo(
                mode == TftpTransferMode.Netascii
                    ? new byte[] { 13, 10, 65 }
                    : content));
        });
        TftpWriteRequest request = peer.CreateWriteRequest();
        request.Options.Mode = mode;
        request.Options.RequestTransferSize = true;
        request.HasKnownSourceSize = hasKnownSourceSize;
        request.KnownSourceSize = content.Length;
        NonSeekableWriteSource source = new NonSeekableWriteSource(content);

        TftpTransferResult result = new TftpClient(null).UploadFromStream(
            request,
            source,
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(source.CanRead, Is.True);
            Assert.That(result.BytesTransferred, Is.EqualTo(2));
            Assert.That(result.EncodedBytesTransferred,
                Is.EqualTo(mode == TftpTransferMode.Netascii ? 3 : 2));
        });
    }

    [Test]
    public void EncodesNetasciiAcrossNegotiatedDataBoundary()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        byte[] content = { 65, 65, 65, 65, 65, 65, 65, 10, 13, 66 };
        byte[] firstEncodedBlock = { 65, 65, 65, 65, 65, 65, 65, 13 };
        byte[] finalEncodedBlock = { 10, 13, 0, 66 };
        Task script = Task.Run(() =>
        {
            TftpPacket requestPacket = peer.ReceiveClientPacket();
            Assert.That(requestPacket.Mode, Is.EqualTo("netascii"));
            peer.SendClientPacket(CreateOack(
                CreateOption("blksize", "8")));
            TftpPacket firstData = peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(1));
            TftpPacket finalData = peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(2));
            Assert.Multiple(() =>
            {
                Assert.That(firstData.BlockNumber, Is.EqualTo(1));
                Assert.That(firstData.Payload, Is.EqualTo(firstEncodedBlock));
                Assert.That(finalData.BlockNumber, Is.EqualTo(2));
                Assert.That(finalData.Payload, Is.EqualTo(finalEncodedBlock));
            });
        });
        TftpWriteRequest request = peer.CreateWriteRequest();
        request.Options.Mode = TftpTransferMode.Netascii;
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 8;

        TftpTransferResult result = new TftpClient(null).UploadFromStream(
            request,
            new MemoryStream(content),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(result.BytesTransferred, Is.EqualTo(10));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(12));
        });
    }

    [Test]
    public void RetransmitsLostDataAndReportsMetrics()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(0));
            TftpPacket firstData = peer.ReceiveClientPacket();
            TftpPacket repeatedData = peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(repeatedData.BlockNumber));
            Assert.Multiple(() =>
            {
                Assert.That(firstData.BlockNumber, Is.EqualTo(1));
                Assert.That(repeatedData.BlockNumber, Is.EqualTo(1));
                Assert.That(repeatedData.Payload, Is.EqualTo(firstData.Payload));
            });
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpWriteRequest request = peer.CreateWriteRequest();
        request.Options.InitialTimeoutSeconds = 1;
        request.Options.MaximumTimeoutSeconds = 1;
        request.Options.MaximumRetries = 2;

        TftpTransferResult result = new TftpClient(observer).UploadFromStream(
            request,
            new MemoryStream(new byte[] { 9 }),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.RetransmissionCount, Is.EqualTo(1));
            Assert.That(observer.Events[^1].RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void IgnoresDuplicateAckAndReportsMetrics()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        byte[] content = CreateContent(513);
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(0));
            TftpPacket firstData = peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(0));
            peer.SendClientPacket(CreateAck(1));
            TftpPacket finalData = peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(2));
            Assert.Multiple(() =>
            {
                Assert.That(firstData.Payload, Has.Length.EqualTo(512));
                Assert.That(finalData.Payload, Has.Length.EqualTo(1));
            });
        });
        RecordingClientObserver observer = new RecordingClientObserver();

        TftpTransferResult result = new TftpClient(observer).UploadFromStream(
            peer.CreateWriteRequest(),
            new MemoryStream(content),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(result.BytesTransferred, Is.EqualTo(513));
            Assert.That(observer.Events[^1].DuplicatePacketCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsOutOfOrderAcknowledgement()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(0));
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(2));
        });

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(() =>
            new TftpClient(null).UploadFromStream(
                peer.CreateWriteRequest(),
                new MemoryStream(new byte[] { 1 }),
                CancellationToken.None));
        script.GetAwaiter().GetResult();

        Assert.That(exception.Message, Does.Contain("unexpected ACK block"));
    }

    [Test]
    public void RejectsWrongTidAndContinuesWithSelectedTransferSocket()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        using Socket transferSocket = peer.CreateSiblingSocket();
        using Socket wrongSocket = peer.CreateSiblingSocket();
        TftpPacket? wrongTidError = null;
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendPacketFromSibling(transferSocket, CreateAck(0));
            TftpPacket data = peer.ReceivePacketOnSibling(transferSocket);
            peer.SendPacketFromSibling(wrongSocket, CreateAck(1));
            wrongTidError = peer.ReceivePacketOnSibling(wrongSocket);
            peer.SendPacketFromSibling(transferSocket, CreateAck(1));
            Assert.That(data.Payload, Is.EqualTo(new byte[] { 8 }));
        });

        TftpTransferResult result = new TftpClient(null).UploadFromStream(
            peer.CreateWriteRequest(),
            new MemoryStream(new byte[] { 8 }),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(wrongTidError!.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(wrongTidError.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
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
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateError(errorCode, "remote failure"));
        });

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            new TftpClient(null).UploadFromStream(
                peer.CreateWriteRequest(),
                new MemoryStream(),
                CancellationToken.None));
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(exception.RemoteMessage, Is.EqualTo("remote failure"));
        });
    }

    [Test]
    public void CancellationInterruptsAckWaitingAndLeavesSourceOpen()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(CreateAck(0));
            peer.ReceiveClientPacket();
            cancellation.Cancel();
        });
        MemoryStream source = new MemoryStream(new byte[] { 1 });

        Assert.Throws<OperationCanceledException>(() =>
            new TftpClient(null).UploadFromStream(
                peer.CreateWriteRequest(),
                source,
                cancellation.Token));
        script.GetAwaiter().GetResult();

        Assert.That(source.CanRead, Is.True);
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

    private static string OptionValue(TftpPacket packet, string name)
    {
        foreach (TftpOption option in packet.Options)
        {
            if (string.Equals(option.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return option.Value;
            }
        }

        return "";
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

    private static TftpPacket CreateOack(params TftpOption[] options)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        foreach (TftpOption option in options)
        {
            packet.Options.Add(option);
        }

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

internal class ScriptedWritePeer : IDisposable
{
    internal Socket Listener;
    internal EndPoint ClientEndPoint = new IPEndPoint(IPAddress.Any, 0);

    internal ScriptedWritePeer()
    {
        Listener = CreateBoundSocket();
    }

    internal TftpWriteRequest CreateWriteRequest()
    {
        TftpWriteRequest request = new TftpWriteRequest();
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

    internal void ReceiveContentAndAcknowledge(byte[] expected, int blockSize)
    {
        int offset = 0;
        ushort blockNumber = 1;
        while (expected.Length - offset >= blockSize)
        {
            TftpPacket data = ReceiveClientPacket();
            SendClientPacket(CreateAcknowledgement(data.BlockNumber));
            byte[] expectedPayload = new byte[blockSize];
            expected.AsSpan(offset, blockSize).CopyTo(expectedPayload);
            Assert.Multiple(() =>
            {
                Assert.That(data.Opcode, Is.EqualTo(TftpOpcode.Data));
                Assert.That(data.BlockNumber, Is.EqualTo(blockNumber));
                Assert.That(data.Payload, Is.EqualTo(expectedPayload));
            });
            offset = offset + blockSize;
            blockNumber = (ushort)(blockNumber + 1);
        }

        TftpPacket finalData = ReceiveClientPacket();
        SendClientPacket(CreateAcknowledgement(finalData.BlockNumber));
        int finalLength = expected.Length - offset;
        byte[] finalPayload = new byte[finalLength];
        expected.AsSpan(offset, finalLength).CopyTo(finalPayload);
        Assert.Multiple(() =>
        {
            Assert.That(finalData.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(finalData.BlockNumber, Is.EqualTo(blockNumber));
            Assert.That(finalData.Payload, Is.EqualTo(finalPayload));
        });
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

    internal TftpPacket CreateAcknowledgement(ushort blockNumber)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = blockNumber;
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

internal class NonSeekableWriteSource : Stream
{
    internal byte[] Content;
    internal int Offset = 0;

    internal NonSeekableWriteSource(byte[] content)
    {
        Content = content;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int remaining = Content.Length - Offset;
        int length = Math.Min(remaining, count);
        Content.AsSpan(Offset, length).CopyTo(buffer.AsSpan(offset, length));
        Offset = Offset + length;
        return length;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
