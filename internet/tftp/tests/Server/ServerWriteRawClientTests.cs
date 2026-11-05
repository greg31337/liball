using System.Net;
using System.Text;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ServerWriteRawClientTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void SupportsEveryWrqOptionCombination(int optionMask)
    {
        byte[] content = Enumerable.Range(1, 19)
            .Select(value => (byte)value)
            .ToArray();
        RecordingServerStorage storage = WritableStorage();
        TftpServerOptions options = ServerOptions();
        options.MaximumBlockSize = 8;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();
        TftpPacket request = WriteRequest("options.bin", "OcTeT");
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
            request.Options.Add(Option("tsize", content.Length.ToString()));
            expectedNames.Add("tsize");
            expectedValues.Add(content.Length.ToString());
        }

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket initial = client.ReceivePacket();
        int blockSize = TftpProtocolValues.DefaultBlockSize;
        if (optionMask == 0)
        {
            Assert.Multiple(() =>
            {
                Assert.That(initial.Packet.Opcode,
                    Is.EqualTo(TftpOpcode.Acknowledgement));
                Assert.That(initial.Packet.BlockNumber, Is.Zero);
            });
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
            if ((optionMask & 1) != 0)
            {
                blockSize = 8;
            }
        }

        RawWriteUpload upload = Upload(
            client,
            initial.SourceEndPoint,
            content,
            blockSize);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(upload.AcknowledgedBlocks,
                Is.EqualTo(upload.SentBlocks));
            Assert.That(WrittenContent(storage), Is.EqualTo(content));
            Assert.That(storage.LastOperation, Is.EqualTo("CommitWrite"));
            Assert.That(completed.NegotiatedOptions,
                Has.Count.EqualTo(expectedNames.Count));
            Assert.That(completed.ServerRequest!.EffectiveOptions,
                Has.Count.EqualTo(expectedNames.Count));
            Assert.That(completed.ServerRequest.RequestedOptions,
                Has.Count.EqualTo(expectedNames.Count));
            Assert.That(completed.ServerRequest.HasAnnouncedSize,
                Is.EqualTo((optionMask & 4) != 0));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(511)]
    [TestCase(512)]
    [TestCase(513)]
    [TestCase(1024)]
    public void ReceivesEveryClassicOctetContentBoundary(int contentLength)
    {
        byte[] content = Enumerable.Range(0, contentLength)
            .Select(value => (byte)(value % 251))
            .ToArray();
        RecordingServerStorage storage = WritableStorage();
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            WriteRequest("boundary.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket acknowledgementZero = client.ReceivePacket();
        RawWriteUpload upload = Upload(
            client,
            acknowledgementZero.SourceEndPoint,
            content,
            TftpProtocolValues.DefaultBlockSize);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        int expectedPacketCount = contentLength /
            TftpProtocolValues.DefaultBlockSize + 1;
        Assert.Multiple(() =>
        {
            Assert.That(acknowledgementZero.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(acknowledgementZero.Packet.BlockNumber, Is.Zero);
            Assert.That(upload.SentBlocks,
                Has.Count.EqualTo(expectedPacketCount));
            Assert.That(upload.FinalPayloadLength,
                Is.EqualTo(contentLength %
                    TftpProtocolValues.DefaultBlockSize));
            Assert.That(WrittenContent(storage), Is.EqualTo(content));
        });
    }

    [Test]
    public void AcceptsAClassicManuallyEncodedWrq()
    {
        byte[] content = [4, 3, 2, 1];
        RecordingServerStorage storage = WritableStorage();
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();

        client.SendBytes(
            harness.ListenerEndPoint,
            EncodeClassicWriteRequest("legacy.bin", "OCTET"));
        RawReceivedPacket acknowledgementZero = client.ReceivePacket();
        Upload(
            client,
            acknowledgementZero.SourceEndPoint,
            content,
            TftpProtocolValues.DefaultBlockSize);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(acknowledgementZero.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(acknowledgementZero.Packet.BlockNumber, Is.Zero);
            Assert.That(WrittenContent(storage), Is.EqualTo(content));
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
    public void DecodesNetasciiAcrossContentAndBlockBoundaries(
        string expectedText,
        string wireText)
    {
        byte[] expected = Encoding.ASCII.GetBytes(expectedText);
        byte[] wireContent = Encoding.ASCII.GetBytes(wireText);
        RecordingServerStorage storage = WritableStorage();
        TftpServerOptions options = ServerOptions();
        options.MaximumBlockSize = 8;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();
        TftpPacket request = WriteRequest("text.txt", "NeTaScIi");
        request.Options.Add(Option("blksize", "8"));

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket oack = client.ReceivePacket();
        RawWriteUpload upload = Upload(
            client,
            oack.SourceEndPoint,
            wireContent,
            8);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(oack.Packet.Opcode,
                Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(upload.SentBlocks,
                Has.Count.EqualTo(wireContent.Length / 8 + 1));
            Assert.That(WrittenContent(storage), Is.EqualTo(expected));
            Assert.That(storage.Request!.Mode,
                Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(completed.BytesTransferred,
                Is.EqualTo(expected.Length));
            Assert.That(completed.EncodedBytesTransferred,
                Is.EqualTo(wireContent.Length));
        });
    }

    [Test]
    public void RetransmitsInitialResponseWhenDataIsLost()
    {
        RecordingServerStorage storage = WritableStorage();
        TftpServerOptions options = ServerOptions();
        options.MaximumRetries = 2;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            WriteRequest("lost-data.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket first = client.ReceivePacket();
        RawReceivedPacket repeated = client.ReceivePacket();
        Upload(client, repeated.SourceEndPoint, [1, 2, 3], 512);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(first.Packet.BlockNumber, Is.Zero);
            Assert.That(repeated.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(repeated.Packet.BlockNumber, Is.Zero);
            Assert.That(repeated.SourceEndPoint,
                Is.EqualTo(first.SourceEndPoint));
            Assert.That(completed.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReacknowledgesDuplicateDataAndRejectsWrongTransferIdsNonfatally()
    {
        byte[] firstPayload = new byte[512];
        byte[] finalPayload = Enumerable.Range(0, 88)
            .Select(value => (byte)value)
            .ToArray();
        RecordingServerStorage storage = WritableStorage();
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();
        using RawTftpClient wrongClient = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            WriteRequest("peer.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket initial = client.ReceivePacket();
        wrongClient.SendPacket(
            initial.SourceEndPoint,
            Data(1, firstPayload));
        RawReceivedPacket unknownTransfer = wrongClient.ReceivePacket();
        client.SendPacket(initial.SourceEndPoint, Data(1, firstPayload));
        RawReceivedPacket firstAck = client.ReceivePacket();
        client.SendPacket(initial.SourceEndPoint, Data(1, firstPayload));
        RawReceivedPacket duplicateAck = client.ReceivePacket();
        client.SendPacket(initial.SourceEndPoint, Data(2, finalPayload));
        RawReceivedPacket finalAck = client.ReceivePacket();
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        byte[] expected = new byte[600];
        finalPayload.CopyTo(expected, 512);
        Assert.Multiple(() =>
        {
            Assert.That(unknownTransfer.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Error));
            Assert.That(unknownTransfer.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            Assert.That(firstAck.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(duplicateAck.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(finalAck.Packet.BlockNumber, Is.EqualTo(2));
            Assert.That(WrittenContent(storage), Is.EqualTo(expected));
            Assert.That(completed.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(harness.Server.GetStatus().FailedTransferCount,
                Is.Zero);
        });
    }

    [Test]
    public void ReacknowledgesDuplicateFinalDataDuringDally()
    {
        byte[] content = [7, 8, 9];
        RecordingServerStorage storage = WritableStorage();
        TftpServerOptions options = ServerOptions();
        options.DallyDurationSeconds = 1;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            WriteRequest("dally.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket initial = client.ReceivePacket();
        TftpPacket finalData = Data(1, content);
        client.SendPacket(initial.SourceEndPoint, finalData);
        RawReceivedPacket firstAck = client.ReceivePacket();
        client.SendPacket(initial.SourceEndPoint, finalData);
        RawReceivedPacket repeatedAck = client.ReceivePacket();
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(firstAck.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(repeatedAck.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(WrittenContent(storage), Is.EqualTo(content));
            Assert.That(completed.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(storage.LastOperation, Is.EqualTo("CommitWrite"));
        });
    }

    [Test]
    public void SendsIllegalOperationAndAbortsForAWrongDataBlock()
    {
        RecordingServerStorage storage = WritableStorage();
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            WriteRequest("wrong-data.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket initial = client.ReceivePacket();
        client.SendPacket(initial.SourceEndPoint, Data(7, [1]));
        RawReceivedPacket error = client.ReceivePacket();
        harness.WaitForFailedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(WrittenContent(storage), Is.Empty);
        });
    }

    [Test]
    public void RollsDataAndAcknowledgementBlockNumbersOverTheWire()
    {
        int blockSize = TftpProtocolValues.MinimumBlockSize;
        int contentLength = blockSize * 65536;
        byte[] content = Enumerable.Range(0, contentLength)
            .Select(value => (byte)(value % 251))
            .ToArray();
        RecordingServerStorage storage = WritableStorage();
        TftpServerOptions options = ServerOptions();
        options.MaximumBlockSize = blockSize;
        options.MaximumSessionDurationSeconds = 30;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        using RawTftpClient client = new RawTftpClient();
        client.Socket.ReceiveTimeout = 5000;
        TftpPacket request = WriteRequest(
            "rollover.bin",
            TftpProtocolValues.OctetMode);
        request.Options.Add(Option("blksize", blockSize.ToString()));

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket oack = client.ReceivePacket();
        RawWriteUpload upload = Upload(
            client,
            oack.SourceEndPoint,
            content,
            blockSize);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(oack.Packet.Opcode,
                Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(upload.SentBlocks, Has.Count.EqualTo(65537));
            Assert.That(upload.SentBlocks[65534],
                Is.EqualTo(ushort.MaxValue));
            Assert.That(upload.SentBlocks[65535], Is.Zero);
            Assert.That(upload.SentBlocks[65536], Is.EqualTo(1));
            Assert.That(upload.AcknowledgedBlocks,
                Is.EqualTo(upload.SentBlocks));
            Assert.That(WrittenContent(storage), Is.EqualTo(content));
        });
    }

    [TestCase(4, 3)]
    [TestCase(2, 3)]
    public void RejectsAnnouncedTransferSizeMismatches(
        int announcedSize,
        int actualSize)
    {
        RecordingServerStorage storage = WritableStorage();
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();
        TftpPacket request = WriteRequest(
            "mismatch.bin",
            TftpProtocolValues.OctetMode);
        request.Options.Add(Option("tsize", announcedSize.ToString()));

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket oack = client.ReceivePacket();
        client.SendPacket(
            oack.SourceEndPoint,
            Data(1, new byte[actualSize]));
        RawReceivedPacket error = client.ReceivePacket();
        harness.WaitForFailedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(oack.Packet.Opcode,
                Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(WrittenContent(storage), Is.Empty);
        });
    }

    [Test]
    public void EnforcesAnnouncedAndRuntimeTransferSizeLimits()
    {
        RecordingServerStorage announcedStorage = WritableStorage();
        TftpServerOptions announcedOptions = ServerOptions();
        announcedOptions.MaximumTransferSize = 2;
        using (RawReadServerHarness announcedHarness =
            new RawReadServerHarness(announcedStorage, announcedOptions))
        using (RawTftpClient announcedClient = new RawTftpClient())
        {
            TftpPacket request = WriteRequest(
                "announced-limit.bin",
                TftpProtocolValues.OctetMode);
            request.Options.Add(Option("tsize", "3"));
            announcedClient.SendPacket(
                announcedHarness.ListenerEndPoint,
                request);
            RawReceivedPacket error = announcedClient.ReceivePacket();
            announcedHarness.WaitForFailedTransfers(1);
            announcedHarness.StopGracefully();
            Assert.Multiple(() =>
            {
                Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
                Assert.That(error.Packet.ErrorCode,
                    Is.EqualTo(TftpErrorCode.StorageExhausted));
                Assert.That(announcedStorage.CallCount, Is.Zero);
            });
        }

        RecordingServerStorage runtimeStorage = WritableStorage();
        TftpServerOptions runtimeOptions = ServerOptions();
        runtimeOptions.MaximumTransferSize = 2;
        using RawReadServerHarness runtimeHarness =
            new RawReadServerHarness(runtimeStorage, runtimeOptions);
        using RawTftpClient runtimeClient = new RawTftpClient();
        runtimeClient.SendPacket(
            runtimeHarness.ListenerEndPoint,
            WriteRequest("runtime-limit.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket initial = runtimeClient.ReceivePacket();
        runtimeClient.SendPacket(initial.SourceEndPoint, Data(1, [1, 2, 3]));
        RawReceivedPacket runtimeError = runtimeClient.ReceivePacket();
        runtimeHarness.WaitForFailedTransfers(1);
        runtimeHarness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(runtimeError.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Error));
            Assert.That(runtimeError.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(runtimeStorage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(WrittenContent(runtimeStorage), Is.Empty);
        });
    }

    [Test]
    public void ReportsCommitFailureAndAbortsPendingContent()
    {
        RecordingServerStorage storage = WritableStorage();
        storage.CommitResult.Success = false;
        storage.CommitResult.ErrorCode = TftpErrorCode.FileAlreadyExists;
        storage.CommitResult.ErrorMessage = "destination changed";
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            WriteRequest("commit.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket initial = client.ReceivePacket();
        client.SendPacket(initial.SourceEndPoint, Data(1, []));
        RawReceivedPacket finalAck = client.ReceivePacket();
        RawReceivedPacket error = client.ReceivePacket();
        harness.WaitForFailedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(finalAck.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(finalAck.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(error.Packet.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.FileAlreadyExists));
            Assert.That(error.Packet.ErrorMessage,
                Is.EqualTo("destination changed"));
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
        });
    }

    [Test]
    public void RemoteErrorAbortsPendingContentWithoutAReply()
    {
        RecordingServerStorage storage = WritableStorage();
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, ServerOptions());
        using RawTftpClient client = new RawTftpClient();

        client.SendPacket(
            harness.ListenerEndPoint,
            WriteRequest("remote-error.bin", TftpProtocolValues.OctetMode));
        RawReceivedPacket initial = client.ReceivePacket();
        TftpPacket remoteError = new TftpPacket();
        remoteError.Opcode = TftpOpcode.Error;
        remoteError.ErrorCode = TftpErrorCode.AccessViolation;
        remoteError.ErrorMessage = "client stopped";
        client.SendPacket(initial.SourceEndPoint, remoteError);
        harness.WaitForFailedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(WrittenContent(storage), Is.Empty);
            Assert.That(storage.WriteResult.Content!.CanWrite, Is.False);
            Assert.That(harness.Server.GetStatus().CompletedTransferCount,
                Is.Zero);
        });
    }

    private static RawWriteUpload Upload(
        RawTftpClient client,
        IPEndPoint transferEndPoint,
        byte[] content,
        int blockSize)
    {
        RawWriteUpload upload = new RawWriteUpload();
        int offset = 0;
        ushort blockNumber = 1;
        bool complete = false;
        while (!complete)
        {
            int payloadLength = content.Length - offset;
            if (payloadLength > blockSize)
            {
                payloadLength = blockSize;
            }

            byte[] payload = content.AsSpan(offset, payloadLength).ToArray();
            client.SendPacket(
                transferEndPoint,
                Data(blockNumber, payload));
            RawReceivedPacket acknowledgement = client.ReceivePacket();
            Assert.Multiple(() =>
            {
                Assert.That(acknowledgement.SourceEndPoint,
                    Is.EqualTo(transferEndPoint));
                Assert.That(acknowledgement.Packet.Opcode,
                    Is.EqualTo(TftpOpcode.Acknowledgement));
                Assert.That(acknowledgement.Packet.BlockNumber,
                    Is.EqualTo(blockNumber));
            });
            upload.SentBlocks.Add(blockNumber);
            upload.AcknowledgedBlocks.Add(
                acknowledgement.Packet.BlockNumber);
            upload.FinalPayloadLength = payloadLength;
            offset += payloadLength;
            complete = payloadLength < blockSize;
            blockNumber = (ushort)(blockNumber + 1);
        }

        return upload;
    }

    private static RecordingServerStorage WritableStorage()
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        storage.WriteResult.Success = true;
        storage.WriteResult.Content = new MemoryStream();
        storage.WriteResult.OwnsContent = true;
        storage.WriteResult.CompletionToken = new object();
        storage.CommitResult.Success = true;
        storage.AbortResult.Success = true;
        return storage;
    }

    private static byte[] WrittenContent(RecordingServerStorage storage)
    {
        return ((MemoryStream)storage.WriteResult.Content!).ToArray();
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
        options.DallyDurationSeconds = 0;
        return options;
    }

    private static TftpPacket WriteRequest(string filename, string mode)
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.WriteRequest;
        request.Filename = filename;
        request.Mode = mode;
        return request;
    }

    private static TftpPacket Data(ushort blockNumber, byte[] payload)
    {
        TftpPacket data = new TftpPacket();
        data.Opcode = TftpOpcode.Data;
        data.BlockNumber = blockNumber;
        data.Payload = payload;
        return data;
    }

    private static TftpOption Option(string name, string value)
    {
        TftpOption option = new TftpOption();
        option.Name = name;
        option.Value = value;
        return option;
    }

    private static byte[] EncodeClassicWriteRequest(
        string filename,
        string mode)
    {
        List<byte> bytes = [0, 2];
        bytes.AddRange(Encoding.ASCII.GetBytes(filename));
        bytes.Add(0);
        bytes.AddRange(Encoding.ASCII.GetBytes(mode));
        bytes.Add(0);
        return bytes.ToArray();
    }
}

internal class RawWriteUpload
{
    internal List<ushort> SentBlocks = [];
    internal List<ushort> AcknowledgedBlocks = [];
    internal int FinalPayloadLength = 0;
}
