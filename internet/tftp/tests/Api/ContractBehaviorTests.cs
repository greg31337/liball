using tftp;

namespace tftp.Tests;

public class ContractBehaviorTests
{
    [Test]
    public void PacketCodecContractCarriesPacketsAndSpanData()
    {
        ITftpPacketCodec codec = new RecordingPacketCodec();
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = 9;
        packet.Payload = new byte[] { 10, 20, 30 };
        byte[] destination = new byte[4];

        int encodedLength = codec.GetEncodedLength(packet);
        int written = codec.Encode(packet, destination.AsSpan());
        TftpPacket decoded = codec.Decode(destination.AsSpan(0, written), 32);
        TftpPacketDecodeResult decodeResult = codec.TryDecode(destination.AsSpan(0, written), 32);

        Assert.Multiple(() =>
        {
            Assert.That(encodedLength, Is.EqualTo(4));
            Assert.That(written, Is.EqualTo(4));
            Assert.That(destination, Is.EqualTo(new byte[] { 9, 10, 20, 30 }));
            Assert.That(decoded.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(decoded.BlockNumber, Is.EqualTo(9));
            Assert.That(decoded.Payload, Is.EqualTo(new byte[] { 10, 20, 30 }));
            Assert.That(decodeResult.Success, Is.True);
            Assert.That(decodeResult.Packet.BlockNumber, Is.EqualTo(9));
            Assert.That(decodeResult.ConsumedLength, Is.EqualTo(4));
        });
    }

    [Test]
    public void ClientContractSupportsStreamAndFileOperations()
    {
        RecordingClient client = new RecordingClient();
        ITftpClient contract = client;
        TftpReadRequest readRequest = new TftpReadRequest();
        TftpWriteRequest writeRequest = new TftpWriteRequest();
        using MemoryStream destination = new MemoryStream();
        using MemoryStream source = new MemoryStream(new byte[] { 1, 2, 3 });
        using CancellationTokenSource cancellation = new CancellationTokenSource();

        TftpTransferResult downloadStreamResult = contract.DownloadToStream(
            readRequest,
            destination,
            cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(client.LastOperation, Is.EqualTo("DownloadToStream"));
            Assert.That(client.LastReadRequest, Is.SameAs(readRequest));
            Assert.That(client.LastStream, Is.SameAs(destination));
            Assert.That(client.LastCancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(downloadStreamResult, Is.SameAs(client.Result));
        });

        TftpTransferResult uploadStreamResult = contract.UploadFromStream(
            writeRequest,
            source,
            cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(client.LastOperation, Is.EqualTo("UploadFromStream"));
            Assert.That(client.LastWriteRequest, Is.SameAs(writeRequest));
            Assert.That(client.LastStream, Is.SameAs(source));
            Assert.That(uploadStreamResult, Is.SameAs(client.Result));
        });

        TftpTransferResult downloadFileResult = contract.DownloadToFile(
            readRequest,
            "download.bin",
            cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(client.LastOperation, Is.EqualTo("DownloadToFile"));
            Assert.That(client.LastReadRequest, Is.SameAs(readRequest));
            Assert.That(client.LastPath, Is.EqualTo("download.bin"));
            Assert.That(downloadFileResult, Is.SameAs(client.Result));
        });

        TftpTransferResult uploadFileResult = contract.UploadFromFile(
            writeRequest,
            "upload.bin",
            cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(client.LastOperation, Is.EqualTo("UploadFromFile"));
            Assert.That(client.LastWriteRequest, Is.SameAs(writeRequest));
            Assert.That(client.LastPath, Is.EqualTo("upload.bin"));
            Assert.That(uploadFileResult, Is.SameAs(client.Result));
        });
    }

    [Test]
    public void ServerContractCarriesRunStopAndStatusData()
    {
        RecordingServer server = new RecordingServer();
        ITftpServer contract = server;
        TftpServerOptions options = new TftpServerOptions();
        using CancellationTokenSource cancellation = new CancellationTokenSource();

        contract.Run(options, cancellation.Token);
        contract.Stop(TftpServerStopMode.Immediate);
        TftpServerStatus status = contract.GetStatus();

        Assert.Multiple(() =>
        {
            Assert.That(server.Options, Is.SameAs(options));
            Assert.That(server.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(server.StopMode, Is.EqualTo(TftpServerStopMode.Immediate));
            Assert.That(status, Is.SameAs(server.Status));
        });
    }

    [Test]
    public void StorageContractCarriesRequestsTokensAndResults()
    {
        RecordingStorage storage = new RecordingStorage();
        ITftpServerStorage contract = storage;
        TftpServerRequest readRequest = new TftpServerRequest();
        readRequest.Direction = TftpTransferDirection.Read;
        TftpServerRequest writeRequest = new TftpServerRequest();
        writeRequest.Direction = TftpTransferDirection.Write;
        object token = new object();
        using CancellationTokenSource cancellation = new CancellationTokenSource();

        TftpStorageOpenResult readResult = contract.OpenRead(readRequest, cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("OpenRead"));
            Assert.That(storage.Request, Is.SameAs(readRequest));
            Assert.That(storage.CancellationToken, Is.EqualTo(cancellation.Token));
            Assert.That(readResult, Is.SameAs(storage.OpenResult));
        });

        TftpStorageOpenResult writeResult = contract.OpenPendingWrite(writeRequest, cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("OpenPendingWrite"));
            Assert.That(storage.Request, Is.SameAs(writeRequest));
            Assert.That(writeResult, Is.SameAs(storage.OpenResult));
        });

        TftpStorageCommitResult commitResult = contract.CommitWrite(token, cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("CommitWrite"));
            Assert.That(storage.CompletionToken, Is.SameAs(token));
            Assert.That(commitResult, Is.SameAs(storage.CommitResult));
        });

        TftpStorageCommitResult abortResult = contract.AbortWrite(token, cancellation.Token);
        Assert.Multiple(() =>
        {
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(storage.CompletionToken, Is.SameAs(token));
            Assert.That(abortResult, Is.SameAs(storage.CommitResult));
        });
    }

    [Test]
    public void ObserverContractDeliversTheEventSnapshot()
    {
        RecordingObserver observer = new RecordingObserver();
        ITftpTransferObserver contract = observer;
        TftpTransferEvent transferEvent = new TftpTransferEvent();
        transferEvent.State = TftpTransferState.Sending;

        contract.Observe(transferEvent);

        Assert.That(observer.TransferEvent, Is.SameAs(transferEvent));
    }

    private sealed class RecordingPacketCodec : ITftpPacketCodec
    {
        public int GetEncodedLength(TftpPacket packet)
        {
            return packet.Payload.Length + 1;
        }

        public int Encode(TftpPacket packet, Span<byte> destination)
        {
            destination[0] = (byte)packet.BlockNumber;
            packet.Payload.AsSpan().CopyTo(destination.Slice(1));
            return packet.Payload.Length + 1;
        }

        public TftpPacket Decode(ReadOnlySpan<byte> source, int maximumDataLength)
        {
            TftpPacket packet = new TftpPacket();
            packet.Opcode = TftpOpcode.Data;
            packet.BlockNumber = source[0];
            packet.Payload = source.Slice(1).ToArray();
            return packet;
        }

        public TftpPacketDecodeResult TryDecode(ReadOnlySpan<byte> source, int maximumDataLength)
        {
            TftpPacketDecodeResult result = new TftpPacketDecodeResult();
            result.Success = true;
            result.Packet = Decode(source, maximumDataLength);
            result.ConsumedLength = source.Length;
            return result;
        }
    }

    private sealed class RecordingClient : ITftpClient
    {
        public string LastOperation = "";
        public TftpReadRequest? LastReadRequest = null;
        public TftpWriteRequest? LastWriteRequest = null;
        public Stream? LastStream = null;
        public string LastPath = "";
        public CancellationToken LastCancellationToken;
        public TftpTransferResult Result = new TftpTransferResult();

        public TftpTransferResult DownloadToStream(
            TftpReadRequest request,
            Stream destination,
            CancellationToken cancellationToken)
        {
            LastOperation = "DownloadToStream";
            LastReadRequest = request;
            LastStream = destination;
            LastCancellationToken = cancellationToken;
            return Result;
        }

        public TftpTransferResult UploadFromStream(
            TftpWriteRequest request,
            Stream source,
            CancellationToken cancellationToken)
        {
            LastOperation = "UploadFromStream";
            LastWriteRequest = request;
            LastStream = source;
            LastCancellationToken = cancellationToken;
            return Result;
        }

        public TftpTransferResult DownloadToFile(
            TftpReadRequest request,
            string localPath,
            CancellationToken cancellationToken)
        {
            LastOperation = "DownloadToFile";
            LastReadRequest = request;
            LastPath = localPath;
            LastCancellationToken = cancellationToken;
            return Result;
        }

        public TftpTransferResult UploadFromFile(
            TftpWriteRequest request,
            string localPath,
            CancellationToken cancellationToken)
        {
            LastOperation = "UploadFromFile";
            LastWriteRequest = request;
            LastPath = localPath;
            LastCancellationToken = cancellationToken;
            return Result;
        }
    }

    private sealed class RecordingServer : ITftpServer
    {
        public TftpServerOptions? Options = null;
        public CancellationToken CancellationToken;
        public TftpServerStopMode StopMode = TftpServerStopMode.Graceful;
        public TftpServerStatus Status = new TftpServerStatus();

        public void Run(TftpServerOptions options, CancellationToken cancellationToken)
        {
            Options = options;
            CancellationToken = cancellationToken;
        }

        public void Stop(TftpServerStopMode mode)
        {
            StopMode = mode;
        }

        public TftpServerStatus GetStatus()
        {
            return Status;
        }
    }

    private sealed class RecordingStorage : ITftpServerStorage
    {
        public string LastOperation = "";
        public TftpServerRequest? Request = null;
        public object? CompletionToken = null;
        public CancellationToken CancellationToken;
        public TftpStorageOpenResult OpenResult = new TftpStorageOpenResult();
        public TftpStorageCommitResult CommitResult = new TftpStorageCommitResult();

        public TftpStorageOpenResult OpenRead(
            TftpServerRequest request,
            CancellationToken cancellationToken)
        {
            LastOperation = "OpenRead";
            Request = request;
            CancellationToken = cancellationToken;
            return OpenResult;
        }

        public TftpStorageOpenResult OpenPendingWrite(
            TftpServerRequest request,
            CancellationToken cancellationToken)
        {
            LastOperation = "OpenPendingWrite";
            Request = request;
            CancellationToken = cancellationToken;
            return OpenResult;
        }

        public TftpStorageCommitResult CommitWrite(
            object completionToken,
            CancellationToken cancellationToken)
        {
            LastOperation = "CommitWrite";
            CompletionToken = completionToken;
            CancellationToken = cancellationToken;
            return CommitResult;
        }

        public TftpStorageCommitResult AbortWrite(
            object completionToken,
            CancellationToken cancellationToken)
        {
            LastOperation = "AbortWrite";
            CompletionToken = completionToken;
            CancellationToken = cancellationToken;
            return CommitResult;
        }
    }

    private sealed class RecordingObserver : ITftpTransferObserver
    {
        public TftpTransferEvent? TransferEvent = null;

        public void Observe(TftpTransferEvent transferEvent)
        {
            TransferEvent = transferEvent;
        }
    }
}
