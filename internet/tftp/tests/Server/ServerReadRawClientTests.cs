using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tftp;

namespace tftp.Tests;

public class ServerReadRawClientTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void SupportsEveryRrqOptionCombination(int optionMask)
    {
        byte[] content = Enumerable.Range(1, 19)
            .Select(value => (byte)value)
            .ToArray();
        RecordingServerStorage storage = ReadStorage(content, true);
        TftpServerOptions options = ServerOptions();
        options.MaximumBlockSize = 8;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();
        TftpPacket request = ReadRequest("options.bin", "OcTeT");
        List<string> expectedNames = [];
        List<string> expectedValues = [];
        if ((optionMask & 1) != 0)
        {
            request.Options.Add(Option("BLKSIZE", "8"));
            expectedNames.Add("BLKSIZE");
            expectedValues.Add("8");
        }

        if ((optionMask & 2) != 0)
        {
            request.Options.Add(Option("timeout", "1"));
            expectedNames.Add("timeout");
            expectedValues.Add("1");
        }

        if ((optionMask & 4) != 0)
        {
            request.Options.Add(Option("tsize", "0"));
            expectedNames.Add("tsize");
            expectedValues.Add(content.Length.ToString());
        }

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket initial = client.ReceivePacket();
        int blockSize = TftpProtocolValues.DefaultBlockSize;
        if (optionMask == 0)
        {
            Assert.That(initial.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
        }
        else
        {
            Assert.Multiple(() =>
            {
                Assert.That(initial.Packet.Opcode,
                    Is.EqualTo(TftpOpcode.OptionAcknowledgement));
                Assert.That(
                    initial.Packet.Options.Select(option => option.Name),
                    Is.EqualTo(expectedNames));
                Assert.That(
                    initial.Packet.Options.Select(option => option.Value),
                    Is.EqualTo(expectedValues));
            });
            client.SendPacket(
                initial.SourceEndPoint,
                Acknowledgement(0));
            initial = client.ReceivePacket();
            if ((optionMask & 1) != 0)
            {
                blockSize = 8;
            }
        }

        RawReadDownload download = client.ReceiveRead(initial, blockSize);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(download.Content, Is.EqualTo(content));
            Assert.That(completed.NegotiatedOptions, Has.Count.EqualTo(
                expectedNames.Count));
            Assert.That(completed.ServerRequest!.EffectiveOptions,
                Has.Count.EqualTo(expectedNames.Count));
            Assert.That(completed.ServerRequest.RequestedOptions,
                Has.Count.EqualTo(expectedNames.Count));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(511)]
    [TestCase(512)]
    [TestCase(513)]
    [TestCase(1024)]
    public void TransfersEveryClassicOctetContentBoundary(int contentLength)
    {
        byte[] content = Enumerable.Range(0, contentLength)
            .Select(value => (byte)(value % 251))
            .ToArray();
        RecordingServerStorage storage = ReadStorage(content, true);
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            ReadRequest("boundary.bin", TftpProtocolValues.OctetMode));
        RawReadDownload download = client.ReceiveRead(
            client.ReceivePacket(),
            TftpProtocolValues.DefaultBlockSize);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        int expectedPacketCount = contentLength /
            TftpProtocolValues.DefaultBlockSize + 1;
        Assert.Multiple(() =>
        {
            Assert.That(download.Content, Is.EqualTo(content));
            Assert.That(download.BlockNumbers,
                Has.Count.EqualTo(expectedPacketCount));
            Assert.That(download.BlockNumbers[0], Is.EqualTo(1));
            Assert.That(download.FinalPayloadLength,
                Is.EqualTo(contentLength %
                    TftpProtocolValues.DefaultBlockSize));
            Assert.That(harness.Server.GetStatus().CompletedTransferCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void AcceptsAClassicManuallyEncodedRrq()
    {
        byte[] content = [4, 3, 2, 1];
        RecordingServerStorage storage = ReadStorage(content, true);
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();
        byte[] legacyRequest = EncodeClassicReadRequest(
            "legacy.bin",
            "OCTET");

        client.SendBytes(harness.ListenerEndPoint, legacyRequest);
        RawReceivedPacket response = client.ReceivePacket();
        RawReadDownload download = client.ReceiveRead(
            response,
            TftpProtocolValues.DefaultBlockSize);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(response.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(download.Content, Is.EqualTo(content));
            Assert.That(storage.Request!.Filename, Is.EqualTo("legacy.bin"));
            Assert.That(storage.Request.Mode,
                Is.EqualTo(TftpTransferMode.Octet));
        });
    }

    [TestCase("", "")]
    [TestCase("ABCDEFGH", "ABCDEFGH")]
    [TestCase("ABCDEFG\n", "ABCDEFG\r\n")]
    [TestCase("\r\n", "\r\0\r\n")]
    [TestCase("ABCDEFG\nH\rI", "ABCDEFG\r\nH\r\0I")]
    public void EncodesNetasciiAcrossContentAndBlockBoundaries(
        string sourceText,
        string expectedText)
    {
        byte[] content = Encoding.ASCII.GetBytes(sourceText);
        byte[] expected = Encoding.ASCII.GetBytes(expectedText);
        RecordingServerStorage storage = ReadStorage(content, false);
        TftpServerOptions options = ServerOptions();
        options.MaximumBlockSize = 8;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();
        TftpPacket request = ReadRequest(
            "text.txt",
            "NeTaScIi");
        request.Options.Add(Option("blksize", "8"));

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket oack = client.ReceivePacket();
        Assert.That(oack.Packet.Opcode,
            Is.EqualTo(TftpOpcode.OptionAcknowledgement));
        client.SendPacket(oack.SourceEndPoint, Acknowledgement(0));
        RawReadDownload download = client.ReceiveRead(
            client.ReceivePacket(),
            8);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);
        ushort[] expectedBlocks = Enumerable.Range(
                1,
                expected.Length / 8 + 1)
            .Select(value => (ushort)value)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(download.Content, Is.EqualTo(expected));
            Assert.That(download.BlockNumbers, Is.EqualTo(expectedBlocks));
            Assert.That(storage.Request!.Mode,
                Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(completed.BytesTransferred,
                Is.EqualTo(content.Length));
            Assert.That(completed.EncodedBytesTransferred,
                Is.EqualTo(expected.Length));
        });
    }

    [Test]
    public void RetransmitsDataWhenAnAcknowledgementIsLost()
    {
        byte[] content = [9, 8, 7];
        RecordingServerStorage storage = ReadStorage(content, true);
        TftpServerOptions options = ServerOptions();
        options.MaximumRetries = 2;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            ReadRequest("lost-ack.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket first = client.ReceivePacket();
        RawReceivedPacket repeated = client.ReceivePacket();
        client.SendPacket(repeated.SourceEndPoint, Acknowledgement(1));
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(first.SourceEndPoint,
                Is.EqualTo(repeated.SourceEndPoint));
            Assert.That(first.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(repeated.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(repeated.Packet.BlockNumber,
                Is.EqualTo(first.Packet.BlockNumber));
            Assert.That(repeated.Packet.Payload,
                Is.EqualTo(first.Packet.Payload));
            Assert.That(completed.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RetransmitsOackUntilAckZeroArrives()
    {
        byte[] content = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        RecordingServerStorage storage = ReadStorage(content, true);
        TftpServerOptions options = ServerOptions();
        options.MaximumBlockSize = 8;
        options.MaximumRetries = 2;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();
        TftpPacket request = ReadRequest(
            "lost-oack.bin",
            TftpProtocolValues.OctetMode);
        request.Options.Add(Option("blksize", "8"));

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket first = client.ReceivePacket();
        RawReceivedPacket repeated = client.ReceivePacket();
        client.SendPacket(repeated.SourceEndPoint, Acknowledgement(0));
        RawReadDownload download = client.ReceiveRead(
            client.ReceivePacket(),
            8);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet.Opcode,
                Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(repeated.Packet.Opcode,
                Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(repeated.SourceEndPoint,
                Is.EqualTo(first.SourceEndPoint));
            Assert.That(repeated.Packet.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "blksize" }));
            Assert.That(download.Content, Is.EqualTo(content));
            Assert.That(completed.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void IgnoresDuplicateAcksAndRejectsWrongTransferIdsNonfatally()
    {
        byte[] content = new byte[600];
        RecordingServerStorage storage = ReadStorage(content, true);
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();
        using RawTftpClient wrongClient = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            ReadRequest("peer.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket first = client.ReceivePacket();
        wrongClient.SendPacket(
            first.SourceEndPoint,
            Acknowledgement(1));
        RawReceivedPacket unknownTransfer = wrongClient.ReceivePacket();
        client.SendPacket(first.SourceEndPoint, Acknowledgement(0));
        client.SendPacket(first.SourceEndPoint, Acknowledgement(1));
        RawReceivedPacket second = client.ReceivePacket();
        client.SendPacket(second.SourceEndPoint, Acknowledgement(2));
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(unknownTransfer.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Error));
            Assert.That(unknownTransfer.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            Assert.That(unknownTransfer.SourceEndPoint,
                Is.EqualTo(first.SourceEndPoint));
            Assert.That(second.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(second.Packet.BlockNumber, Is.EqualTo(2));
            Assert.That(second.Packet.Payload, Has.Length.EqualTo(88));
            Assert.That(completed.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(harness.Server.GetStatus().FailedTransferCount,
                Is.Zero);
        });
    }

    [Test]
    public void SendsIllegalOperationForAWrongAcknowledgementBlock()
    {
        RecordingServerStorage storage = ReadStorage([1, 2, 3], true);
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            ReadRequest("wrong-ack.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket data = client.ReceivePacket();
        client.SendPacket(data.SourceEndPoint, Acknowledgement(7));
        RawReceivedPacket error = client.ReceivePacket();
        harness.WaitForFailedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(error.SourceEndPoint, Is.EqualTo(data.SourceEndPoint));
            Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(harness.Server.GetStatus().CompletedTransferCount,
                Is.Zero);
        });
    }

    [Test]
    public void RollsDataBlockNumbersOverTheWire()
    {
        int blockSize = TftpProtocolValues.MinimumBlockSize;
        int contentLength = blockSize * 65536;
        byte[] content = Enumerable.Range(0, contentLength)
            .Select(value => (byte)(value % 251))
            .ToArray();
        RecordingServerStorage storage = ReadStorage(content, true);
        TftpServerOptions options = ServerOptions();
        options.MaximumBlockSize = blockSize;
        options.MaximumSessionDurationSeconds = 30;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();
        client.Socket.ReceiveTimeout = 5000;
        TftpPacket request = ReadRequest(
            "rollover.bin",
            TftpProtocolValues.OctetMode);
        request.Options.Add(Option("blksize", blockSize.ToString()));

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket oack = client.ReceivePacket();
        Assert.That(oack.Packet.Opcode,
            Is.EqualTo(TftpOpcode.OptionAcknowledgement));
        client.SendPacket(oack.SourceEndPoint, Acknowledgement(0));
        using MemoryStream receivedContent = new MemoryStream();
        int packetIndex = 1;
        while (packetIndex <= 65536)
        {
            RawReceivedPacket data = client.ReceivePacket();
            ushort expectedBlock = (ushort)packetIndex;
            Assert.Multiple(() =>
            {
                Assert.That(data.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
                Assert.That(data.Packet.BlockNumber,
                    Is.EqualTo(expectedBlock));
                Assert.That(data.Packet.Payload,
                    Has.Length.EqualTo(blockSize));
            });
            receivedContent.Write(data.Packet.Payload);
            client.SendPacket(
                data.SourceEndPoint,
                Acknowledgement(expectedBlock));
            packetIndex += 1;
        }

        RawReceivedPacket finalData = client.ReceivePacket();
        client.SendPacket(finalData.SourceEndPoint, Acknowledgement(1));
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(finalData.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(finalData.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(finalData.Packet.Payload, Is.Empty);
            Assert.That(receivedContent.ToArray(), Is.EqualTo(content));
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
    public void SendsEveryStorageFailureCode(TftpErrorCode errorCode)
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        storage.ReadResult.ErrorCode = errorCode;
        storage.ReadResult.ErrorMessage = "storage rejected the read";
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            ReadRequest("rejected.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket error = client.ReceivePacket();
        harness.WaitForFailedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.Packet.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(error.Packet.ErrorMessage,
                Is.EqualTo("storage rejected the read"));
            Assert.That(storage.CallCount, Is.EqualTo(1));
            Assert.That(harness.Server.GetStatus().FailedTransferCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void EnforcesKnownAndRuntimeStorageSizeFailures()
    {
        RecordingServerStorage knownStorage = ReadStorage(new byte[513], true);
        TftpServerOptions knownOptions = ServerOptions();
        knownOptions.MaximumTransferSize = 512;
        using (RawReadServerHarness knownHarness =
            new RawReadServerHarness(knownStorage, knownOptions))
        using (RawTftpClient knownClient = new RawTftpClient())
        {
            knownClient.SendPacket(
                knownHarness.ListenerEndPoint,
                ReadRequest("known.bin", TftpProtocolValues.OctetMode));
            RawReceivedPacket knownError = knownClient.ReceivePacket();
            knownHarness.WaitForFailedTransfers(1);
            knownHarness.StopGracefully();
            Assert.Multiple(() =>
            {
                Assert.That(knownError.Packet.Opcode,
                    Is.EqualTo(TftpOpcode.Error));
                Assert.That(knownError.Packet.ErrorCode,
                    Is.EqualTo(TftpErrorCode.StorageExhausted));
                Assert.That(knownStorage.ReadResult.Content!.CanRead,
                    Is.False);
            });
        }

        RecordingServerStorage runtimeStorage = ReadStorage(
            new byte[513],
            false);
        TftpServerOptions runtimeOptions = ServerOptions();
        runtimeOptions.MaximumTransferSize = 512;
        using RawReadServerHarness runtimeHarness =
            new RawReadServerHarness(runtimeStorage, runtimeOptions);
        using RawTftpClient runtimeClient = new RawTftpClient();
        runtimeClient.SendPacket(
            runtimeHarness.ListenerEndPoint,
            ReadRequest("runtime.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket firstData = runtimeClient.ReceivePacket();
        runtimeClient.SendPacket(
            firstData.SourceEndPoint,
            Acknowledgement(1));
        RawReceivedPacket runtimeError = runtimeClient.ReceivePacket();
        runtimeHarness.WaitForFailedTransfers(1);
        runtimeHarness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(firstData.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(firstData.Packet.Payload, Has.Length.EqualTo(512));
            Assert.That(runtimeError.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Error));
            Assert.That(runtimeError.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
        });
    }

    private static RecordingServerStorage ReadStorage(
        byte[] content,
        bool hasKnownSize)
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        storage.ReadResult.Success = true;
        storage.ReadResult.Content = new MemoryStream(content);
        storage.ReadResult.OwnsContent = true;
        storage.ReadResult.HasKnownSize = hasKnownSize;
        storage.ReadResult.KnownSize = content.Length;
        return storage;
    }

    private static TftpServerOptions ServerOptions()
    {
        TftpServerOptions options = new TftpServerOptions();
        TftpServerBinding binding = new TftpServerBinding();
        binding.Address = IPAddress.Loopback;
        binding.Port = 0;
        options.Bindings.Add(binding);
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 1;
        options.MaximumRetries = 1;
        options.MaximumSessionDurationSeconds = 10;
        return options;
    }

    private static TftpPacket ReadRequest(string filename, string mode)
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = filename;
        request.Mode = mode;
        return request;
    }

    internal static TftpPacket Acknowledgement(ushort blockNumber)
    {
        TftpPacket acknowledgement = new TftpPacket();
        acknowledgement.Opcode = TftpOpcode.Acknowledgement;
        acknowledgement.BlockNumber = blockNumber;
        return acknowledgement;
    }

    private static TftpOption Option(string name, string value)
    {
        TftpOption option = new TftpOption();
        option.Name = name;
        option.Value = value;
        return option;
    }

    private static byte[] EncodeClassicReadRequest(
        string filename,
        string mode)
    {
        List<byte> bytes = [0, 1];
        bytes.AddRange(Encoding.ASCII.GetBytes(filename));
        bytes.Add(0);
        bytes.AddRange(Encoding.ASCII.GetBytes(mode));
        bytes.Add(0);
        return bytes.ToArray();
    }
}

internal class RawReadServerHarness : IDisposable
{
    internal TftpServer Server;
    internal RecordingServerObserver Observer;
    internal Task RunTask;
    internal IPEndPoint ListenerEndPoint;

    internal RawReadServerHarness(
        ITftpServerStorage storage,
        TftpServerOptions options)
    {
        Observer = new RecordingServerObserver();
        Server = new TftpServer(storage, Observer);
        RunTask = Task.Run(() => Server.Run(options, CancellationToken.None));
        bool started = SpinWait.SpinUntil(() =>
        {
            TftpServerStatus status = Server.GetStatus();
            return status.IsRunning && status.BoundEndpoints.Count == 1;
        }, 2000);
        if (!started)
        {
            Server.Stop(TftpServerStopMode.Immediate);
            throw new AssertionException("The raw-client server did not start.");
        }

        TftpServerBinding binding = Server.GetStatus().BoundEndpoints[0];
        ListenerEndPoint = new IPEndPoint(binding.Address, binding.Port);
    }

    internal void WaitForCompletedTransfers(long expectedCount)
    {
        bool completed = SpinWait.SpinUntil(
            () => Server.GetStatus().CompletedTransferCount == expectedCount,
            5000);
        Assert.That(completed, Is.True);
    }

    internal void WaitForFailedTransfers(long expectedCount)
    {
        bool completed = SpinWait.SpinUntil(
            () => Server.GetStatus().FailedTransferCount == expectedCount,
            5000);
        Assert.That(completed, Is.True);
    }

    internal void StopGracefully()
    {
        Server.Stop(TftpServerStopMode.Graceful);
        bool stopped = SpinWait.SpinUntil(() => RunTask.IsCompleted, 5000);
        Assert.That(stopped, Is.True);
        RunTask.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Server.Stop(TftpServerStopMode.Immediate);
        bool stopped = SpinWait.SpinUntil(() => RunTask.IsCompleted, 5000);
        if (!stopped)
        {
            throw new AssertionException("The raw-client server did not stop.");
        }

        RunTask.GetAwaiter().GetResult();
    }
}

internal class RawTftpClient : IDisposable
{
    internal Socket Socket;
    internal TftpPacketCodec Codec;

    internal RawTftpClient()
    {
        Socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        Socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        Socket.ReceiveTimeout = 2500;
        Codec = new TftpPacketCodec();
    }

    internal void SendPacket(IPEndPoint endPoint, TftpPacket packet)
    {
        byte[] buffer = new byte[Codec.GetEncodedLength(packet)];
        Codec.Encode(packet, buffer.AsSpan());
        SendBytes(endPoint, buffer);
    }

    internal void SendBytes(IPEndPoint endPoint, byte[] bytes)
    {
        Socket.SendTo(bytes, endPoint);
    }

    internal RawReceivedPacket ReceivePacket()
    {
        byte[] buffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        EndPoint source = new IPEndPoint(IPAddress.Any, 0);
        int length = Socket.ReceiveFrom(buffer, ref source);
        RawReceivedPacket received = new RawReceivedPacket();
        received.Packet = Codec.Decode(
            buffer.AsSpan(0, length),
            TftpProtocolValues.MaximumBlockSize);
        received.SourceEndPoint = (IPEndPoint)source;
        return received;
    }

    internal RawReadDownload ReceiveRead(
        RawReceivedPacket firstData,
        int blockSize)
    {
        RawReadDownload download = new RawReadDownload();
        using MemoryStream content = new MemoryStream();
        RawReceivedPacket current = firstData;
        ushort expectedBlock = 1;
        while (true)
        {
            Assert.Multiple(() =>
            {
                Assert.That(current.Packet.Opcode,
                    Is.EqualTo(TftpOpcode.Data));
                Assert.That(current.Packet.BlockNumber,
                    Is.EqualTo(expectedBlock));
                Assert.That(current.Packet.Payload.Length,
                    Is.LessThanOrEqualTo(blockSize));
            });
            content.Write(current.Packet.Payload);
            download.BlockNumbers.Add(current.Packet.BlockNumber);
            download.FinalPayloadLength = current.Packet.Payload.Length;
            SendPacket(
                current.SourceEndPoint,
                ServerReadRawClientTests.Acknowledgement(expectedBlock));
            if (current.Packet.Payload.Length < blockSize)
            {
                break;
            }

            expectedBlock = (ushort)(expectedBlock + 1);
            current = ReceivePacket();
        }

        download.Content = content.ToArray();
        return download;
    }

    public void Dispose()
    {
        Socket.Dispose();
    }
}

internal class RawReceivedPacket
{
    internal TftpPacket Packet = new TftpPacket();
    internal IPEndPoint SourceEndPoint =
        new IPEndPoint(IPAddress.Loopback, 0);
}

internal class RawReadDownload
{
    internal byte[] Content = [];
    internal List<ushort> BlockNumbers = [];
    internal int FinalPayloadLength = 0;
}
