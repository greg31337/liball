using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class PublicApiCoverageBehaviorTests
{
    [Test]
    public void ClientRejectsEveryInvalidFileAndStreamShapeBeforeNetworkWork()
    {
        TftpClient client = new TftpClient(null);
        TftpReadRequest readWithoutEndpoint = new TftpReadRequest();
        readWithoutEndpoint.Endpoint = null!;
        TftpReadRequest readWithoutOptions = new TftpReadRequest();
        readWithoutOptions.Options = null!;
        TftpWriteRequest writeWithoutEndpoint = new TftpWriteRequest();
        writeWithoutEndpoint.Endpoint = null!;
        TftpWriteRequest writeWithoutOptions = new TftpWriteRequest();
        writeWithoutOptions.Options = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() => client.DownloadToFile(
                readWithoutEndpoint, "unused", CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.DownloadToFile(
                readWithoutOptions, "unused", CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.DownloadToFile(
                new TftpReadRequest(), null!, CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.DownloadToFile(
                new TftpReadRequest(), "   ", CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.DownloadToFile(
                new TftpReadRequest(), Path.DirectorySeparatorChar.ToString(),
                CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.UploadFromFile(
                writeWithoutEndpoint, "unused", CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.UploadFromFile(
                writeWithoutOptions, "unused", CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.UploadFromFile(
                new TftpWriteRequest(), null!, CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.UploadFromFile(
                new TftpWriteRequest(), "   ", CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.UploadFromFile(
                new TftpWriteRequest(), Path.DirectorySeparatorChar.ToString(),
                CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.DownloadToStream(
                readWithoutOptions, new MemoryStream(), CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() => client.UploadFromStream(
                writeWithoutEndpoint, new MemoryStream(), CancellationToken.None));
        });
    }

    [Test]
    public void ClientInternalResultsPreservePublicFailureDetails()
    {
        TftpClient client = new TftpClient(null);
        TftpClientReadResult readFailure = new TftpClientReadResult();
        readFailure.HasFailure = true;
        readFailure.IsOptionFailure = true;
        readFailure.FailureMessage = "bad read option";
        readFailure.FailureOptionName = "blksize";
        readFailure.FailureOptionValue = "7";
        TftpClientWriteResult writeFailure = new TftpClientWriteResult();
        writeFailure.HasFailure = true;
        writeFailure.IsOptionFailure = true;
        writeFailure.FailureMessage = "bad write option";
        writeFailure.FailureOptionName = "timeout";
        writeFailure.FailureOptionValue = "0";

        TftpOptionNegotiationException readException =
            Assert.Throws<TftpOptionNegotiationException>(() =>
                client.ThrowForReadActionFailure(readFailure));
        TftpOptionNegotiationException writeException =
            Assert.Throws<TftpOptionNegotiationException>(() =>
                client.ThrowForWriteActionFailure(writeFailure));
        writeFailure.IsOptionFailure = false;
        TftpProtocolException writeProtocolException =
            Assert.Throws<TftpProtocolException>(() =>
                client.ThrowForWriteActionFailure(writeFailure));
        TftpPacket error = client.CreateClientErrorPacket(
            TftpErrorCode.AccessViolation,
            "denied");

        TftpSystemTimeSource timeSource = new TftpSystemTimeSource();
        TftpClientReadStateMachine readState = CreateReadState(timeSource);
        TftpClientWriteStateMachine writeState = CreateWriteState(timeSource);

        Assert.Multiple(() =>
        {
            Assert.That(readException.OptionName, Is.EqualTo("blksize"));
            Assert.That(readException.OptionValue, Is.EqualTo("7"));
            Assert.That(writeException.OptionName, Is.EqualTo("timeout"));
            Assert.That(writeException.OptionValue, Is.EqualTo("0"));
            Assert.That(writeProtocolException.Message,
                Is.EqualTo("bad write option"));
            Assert.That(error.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(error.ErrorCode, Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(error.ErrorMessage, Is.EqualTo("denied"));
            Assert.Throws<TftpProtocolException>(() =>
                client.CreateReadTransferResult(readState, timeSource, 0));
            Assert.Throws<TftpProtocolException>(() =>
                client.CreateWriteTransferResult(writeState, timeSource, 0));
            Assert.That(client.GetElapsedMilliseconds(timeSource, long.MaxValue),
                Is.Zero);
        });

        TftpPacket data = new TftpPacket();
        data.Opcode = TftpOpcode.Data;
        client.ObserveRemoteErrorPacket(
            TftpTransferDirection.Read,
            TftpTransferMode.Octet,
            new IPEndPoint(IPAddress.Loopback, 69),
            data,
            0,
            0,
            0,
            0,
            []);

        TftpPacket errorWithoutMessage = new TftpPacket();
        errorWithoutMessage.Opcode = TftpOpcode.Error;
        errorWithoutMessage.ErrorMessage = null!;
        client.ObserveRemoteErrorPacket(
            TftpTransferDirection.Read,
            TftpTransferMode.Octet,
            new IPEndPoint(IPAddress.Loopback, 69),
            errorWithoutMessage,
            0,
            0,
            0,
            0,
            []);
    }

    [Test]
    public void ClientReadRejectsMalformedExpectedPeerDatagrams()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        TftpPacket? clientError = null;
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            byte[] datagram = new byte[] { 0, 4, 0 };
            peer.Listener.SendTo(datagram, peer.ClientEndPoint);
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
            Assert.That(clientError!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(exception.Message, Does.Contain("ACK packet"));
        });
    }

    [Test]
    public void ClientWriteRejectsMalformedExpectedPeerDatagrams()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        TftpPacket? clientError = null;
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            byte[] datagram = new byte[] { 0, 3, 0 };
            peer.Listener.SendTo(datagram, peer.ClientEndPoint);
            clientError = peer.ReceiveClientPacket();
        });

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(() =>
            new TftpClient(null).UploadFromStream(
                peer.CreateWriteRequest(),
                new MemoryStream(),
                CancellationToken.None));
        script.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(clientError!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(exception.Message, Does.Contain("DATA packet"));
        });
    }

    [Test]
    public void ClientDatagramDecoderReportsTransportTruncation()
    {
        TftpPacketDecodeResult result = TftpClientDatagramDecoder.Decode(
            new TftpPacketCodec(),
            ReadOnlySpan<byte>.Empty,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureMessage,
                Does.Contain("maximum packet size"));
        });
    }

    [Test]
    public void ClientWriteRetransmitsARequestBeforeCompleting()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        Task script = Task.Run(() =>
        {
            TftpPacket first = peer.ReceiveClientPacket();
            TftpPacket second = peer.ReceiveClientPacket();
            Assert.That(first.Opcode, Is.EqualTo(TftpOpcode.WriteRequest));
            Assert.That(second.Opcode, Is.EqualTo(TftpOpcode.WriteRequest));
            peer.SendClientPacket(peer.CreateAcknowledgement(0));
            TftpPacket data = peer.ReceiveClientPacket();
            peer.SendClientPacket(peer.CreateAcknowledgement(data.BlockNumber));
        });
        TftpWriteRequest request = peer.CreateWriteRequest();
        request.Options.InitialTimeoutSeconds = 1;
        request.Options.MaximumTimeoutSeconds = 1;
        request.Options.MaximumRetries = 2;

        TftpTransferResult result = new TftpClient(null).UploadFromStream(
            request,
            new MemoryStream(),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.That(result.RetransmissionCount, Is.EqualTo(1));
    }

    [Test]
    public void ClientReadRejectsMalformedWrongTidAndContinues()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        using Socket transfer = peer.CreateSiblingSocket();
        using Socket wrongTid = peer.CreateSiblingSocket();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendPacketFromSibling(
                transfer,
                peer.CreateDataPacket(1, new byte[512]));
            peer.ReceivePacketOnSibling(transfer);
            wrongTid.SendTo(new byte[] { 0 }, peer.ClientEndPoint);
            TftpPacket error = peer.ReceivePacketOnSibling(wrongTid);
            Assert.That(error.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            peer.SendPacketFromSibling(
                transfer,
                peer.CreateDataPacket(2, new byte[] { 7 }));
            peer.ReceivePacketOnSibling(transfer);
        });
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.DallyDurationSeconds = 0;

        TftpTransferResult result = new TftpClient(null).DownloadToStream(
            request,
            new MemoryStream(),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.That(result.BytesTransferred, Is.EqualTo(513));
    }

    [Test]
    public void ClientWriteRejectsMalformedWrongTidAndContinues()
    {
        using ScriptedWritePeer peer = new ScriptedWritePeer();
        using Socket transfer = peer.CreateSiblingSocket();
        using Socket wrongTid = peer.CreateSiblingSocket();
        byte[] content = new byte[513];
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendPacketFromSibling(
                transfer,
                peer.CreateAcknowledgement(0));
            TftpPacket first = peer.ReceivePacketOnSibling(transfer);
            wrongTid.SendTo(new byte[] { 0 }, peer.ClientEndPoint);
            TftpPacket error = peer.ReceivePacketOnSibling(wrongTid);
            Assert.That(error.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            peer.SendPacketFromSibling(
                transfer,
                peer.CreateAcknowledgement(first.BlockNumber));
            TftpPacket final = peer.ReceivePacketOnSibling(transfer);
            peer.SendPacketFromSibling(
                transfer,
                peer.CreateAcknowledgement(final.BlockNumber));
        });

        TftpTransferResult result = new TftpClient(null).UploadFromStream(
            peer.CreateWriteRequest(),
            new MemoryStream(content),
            CancellationToken.None);
        script.GetAwaiter().GetResult();

        Assert.That(result.BytesTransferred, Is.EqualTo(513));
    }

    [Test]
    public void ClientWriteProcessesAnImmediatelyElapsedTimerResult()
    {
        using Socket sender = CreateBoundSocket();
        using Socket receiver = CreateBoundSocket();
        TftpWriteRequest request = new TftpWriteRequest();
        request.RemoteFilename = "remote.bin";
        request.Options.MaximumRetries = 1;
        TftpClientWriteStateMachine state = new TftpClientWriteStateMachine(
            request,
            new MemoryStream(),
            (IPEndPoint)receiver.LocalEndPoint!,
            new AdvancingTimeSource());
        UdpDatagramSocket datagramSocket = new UdpDatagramSocket(
            sender,
            (IPEndPoint)sender.LocalEndPoint!);

        TftpTimeoutException exception = Assert.Throws<TftpTimeoutException>(() =>
            new TftpClient(null).ExecuteWriteTransfer(
                state,
                datagramSocket,
                new TftpSystemTimeSource(),
                Environment.TickCount64,
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.RetryLimitReached, Is.True);
            Assert.That(exception.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedPeerHandlingIgnoresAddressesAndRejectsUnknownPorts()
    {
        TftpClient client = new TftpClient(null);
        TftpSystemTimeSource timeSource = new TftpSystemTimeSource();
        TftpClientReadStateMachine readState = CreateReadState(timeSource);
        TftpClientWriteStateMachine writeState = CreateWriteState(timeSource);
        using Socket sender = CreateBoundSocket();
        using Socket receiver = CreateBoundSocket();
        UdpDatagramSocket datagramSocket = new UdpDatagramSocket(
            sender,
            (IPEndPoint)sender.LocalEndPoint!);
        IPEndPoint unexpected = new IPEndPoint(
            IPAddress.Parse("127.0.0.2"),
            45000);
        IPEndPoint unknown = (IPEndPoint)receiver.LocalEndPoint!;
        readState.PeerTracker.IsTransferPortLocked = true;
        readState.PeerTracker.TransferPort = 45001;
        writeState.PeerTracker.IsTransferPortLocked = true;
        writeState.PeerTracker.TransferPort = 45001;

        client.HandleMalformedReadDatagram(
            readState, datagramSocket, unexpected, "ignored", CancellationToken.None);
        client.HandleMalformedWriteDatagram(
            writeState, datagramSocket, unexpected, "ignored", CancellationToken.None);
        client.HandleMalformedReadDatagram(
            readState, datagramSocket, unknown, "wrong TID", CancellationToken.None);
        TftpPacket readError = ReceivePacket(receiver);
        client.HandleMalformedWriteDatagram(
            writeState, datagramSocket, unknown, "wrong TID", CancellationToken.None);
        TftpPacket writeError = ReceivePacket(receiver);

        Assert.Multiple(() =>
        {
            Assert.That(readError.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            Assert.That(writeError.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
        });
    }

    [Test]
    public void ClientPacketSendRequiresATargetForRealPackets()
    {
        using Socket socket = CreateBoundSocket();
        UdpDatagramSocket datagramSocket = new UdpDatagramSocket(
            socket,
            (IPEndPoint)socket.LocalEndPoint!);
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(() =>
            new TftpClient(null).SendClientPacket(
                datagramSocket,
                packet,
                null,
                CancellationToken.None));

        Assert.That(exception.Message, Does.Contain("target endpoint"));
    }

    [Test]
    public void CodecRejectsAnOversizedOptionAcknowledgement()
    {
        byte[] datagram = new byte[TftpProtocolValues.MaximumRequestPacketLength + 1];
        datagram[1] = (byte)TftpOpcode.OptionAcknowledgement;

        TftpPacketDecodeResult result = new TftpPacketCodec().TryDecode(datagram, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureMessage, Does.Contain("OACK"));
            Assert.That(result.FailureMessage, Does.Contain("512-byte"));
        });
    }

    [Test]
    public void DirectoryStorageHandlesNullOptionsAndCancellationWhileCounting()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "text.txt");
        File.WriteAllBytes(path, new byte[] { 10, 13 });
        try
        {
            TftpDirectoryStorageOptions options = new TftpDirectoryStorageOptions();
            options.RootPath = root;
            TftpDirectoryStorage storage = new TftpDirectoryStorage(options);
            List<TftpOption> requested = [null!];
            TftpOption transferSize = new TftpOption();
            transferSize.Name = "tsize";
            requested.Add(transferSize);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            FileStream content = File.OpenRead(path);

            Assert.That(storage.RequestsTransferSize(requested), Is.True);
            Assert.Throws<OperationCanceledException>(() =>
                storage.CountNetasciiSize(content, cancellation.Token));
            Assert.That(content.CanRead, Is.False);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void StorageCancellationCleansUpOpenedReadAndPendingWriteResources()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string readPath = Path.Combine(root, "read.bin");
        string destinationPath = Path.Combine(root, "write.bin");
        string temporaryPath = Path.Combine(root, ".write.tmp");
        File.WriteAllBytes(readPath, new byte[] { 1 });
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try
        {
            FileStream read = File.OpenRead(readPath);
            Assert.Throws<OperationCanceledException>(() =>
                TftpStorageCancellation.ThrowIfReadCancellationRequested(
                    cancellation.Token,
                    read));
            Assert.That(read.CanRead, Is.False);

            TftpPendingWrite pending = new TftpPendingWrite(
                "write.bin",
                destinationPath,
                temporaryPath,
                true,
                false,
                false,
                0,
                TftpTransferMode.Octet);
            Dictionary<object, TftpPendingWrite> writes = [];
            writes.Add(pending.Token, pending);
            Assert.Throws<OperationCanceledException>(() =>
                TftpStorageCancellation.ThrowIfPendingWriteCancellationRequested(
                    cancellation.Token,
                    pending,
                    writes));
            Assert.Multiple(() =>
            {
                Assert.That(writes, Is.Empty);
                Assert.That(File.Exists(temporaryPath), Is.False);
                Assert.That(pending.Stream.CanWrite, Is.False);
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ServerRejectsNullOptionsAndNullBindingsBeforeBindingSockets()
    {
        TftpServer server = new TftpServer(new CoverageStorage(), null);
        TftpServerOptions missingBindings = new TftpServerOptions();
        missingBindings.Bindings = null!;
        TftpServerOptions nullBinding = new TftpServerOptions();
        nullBinding.Bindings.Add(null!);

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                server.Run(null!, CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                server.Run(missingBindings, CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                server.Run(nullBinding, CancellationToken.None));
        });
    }

    [Test]
    public void StoppingServerRejectsAPolledRequestAndCopiesEndpointStatus()
    {
        using Socket listenerSocket = CreateBoundSocket();
        using Socket clientSocket = CreateBoundSocket();
        UdpDatagramSocket listener = new UdpDatagramSocket(
            listenerSocket,
            (IPEndPoint)listenerSocket.LocalEndPoint!);
        TftpServerOptions options = new TftpServerOptions();
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        TftpSessionManager manager = new TftpSessionManager(lifecycle, null);
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = "file.bin";
        request.Mode = TftpProtocolValues.OctetMode;
        byte[] encoded = Encode(request);
        TftpListenerPollResult poll = new TftpListenerPollResult();
        poll.HasDatagram = true;
        poll.Listener = listener;
        poll.Buffer = encoded;
        poll.Datagram = new UdpDatagramReceiveResult();
        poll.Datagram.HasDatagram = true;
        poll.Datagram.Length = encoded.Length;
        poll.Datagram.RemoteEndPoint = (IPEndPoint)clientSocket.LocalEndPoint!;
        poll.Datagram.DestinationAddress = IPAddress.Loopback;

        new TftpServer(new CoverageStorage(), null).ProcessListenerDatagram(
            poll,
            manager,
            new CoverageStorage(),
            options);
        TftpPacket error = ReceivePacket(clientSocket);

        TftpServerStatus status = new TftpServerStatus();
        TftpServerBinding endpoint = new TftpServerBinding();
        endpoint.Address = IPAddress.IPv6Loopback;
        endpoint.Port = 1069;
        status.BoundEndpoints.Add(endpoint);
        TftpServerStatus copy = TftpServer.CopyStatus(status);

        Assert.Multiple(() =>
        {
            Assert.That(error.ErrorCode, Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(error.ErrorMessage, Does.Contain("stopping"));
            Assert.That(copy.BoundEndpoints, Has.Count.EqualTo(1));
            Assert.That(copy.BoundEndpoints[0].Address,
                Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(copy.BoundEndpoints[0].Port, Is.EqualTo(1069));
            Assert.That(copy.BoundEndpoints[0], Is.Not.SameAs(endpoint));
        });
    }

    [Test]
    public void ListenerDoesNotSendWhenThereIsNoErrorPacket()
    {
        using Socket listenerSocket = CreateBoundSocket();
        using Socket clientSocket = CreateBoundSocket();
        UdpDatagramSocket listener = new UdpDatagramSocket(
            listenerSocket,
            (IPEndPoint)listenerSocket.LocalEndPoint!);

        new TftpServer(new CoverageStorage(), null).SendListenerError(
            listener,
            (IPEndPoint)clientSocket.LocalEndPoint!,
            null);

        Assert.That(clientSocket.Poll(1000, SelectMode.SelectRead), Is.False);
    }

    [Test]
    public void ServerIgnoresIncompletePollResults()
    {
        using Socket listenerSocket = CreateBoundSocket();
        UdpDatagramSocket listener = new UdpDatagramSocket(
            listenerSocket,
            (IPEndPoint)listenerSocket.LocalEndPoint!);
        TftpServer server = new TftpServer(new CoverageStorage(), null);
        TftpServerOptions options = new TftpServerOptions();
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        TftpSessionManager manager = new TftpSessionManager(lifecycle, null);
        TftpListenerPollResult missingListener = new TftpListenerPollResult();
        missingListener.Datagram = new UdpDatagramReceiveResult();
        TftpListenerPollResult missingDatagram = new TftpListenerPollResult();
        missingDatagram.Listener = listener;

        Assert.DoesNotThrow(() => server.ProcessListenerDatagram(
            missingListener,
            manager,
            new CoverageStorage(),
            options));
        Assert.DoesNotThrow(() => server.ProcessListenerDatagram(
            missingDatagram,
            manager,
            new CoverageStorage(),
            options));
    }

    private static TftpClientReadStateMachine CreateReadState(
        TftpSystemTimeSource timeSource)
    {
        TftpReadRequest request = new TftpReadRequest();
        request.RemoteFilename = "remote.bin";
        return new TftpClientReadStateMachine(
            request,
            new MemoryStream(),
            new IPEndPoint(IPAddress.Loopback, 69),
            timeSource);
    }

    private static TftpClientWriteStateMachine CreateWriteState(
        TftpSystemTimeSource timeSource)
    {
        TftpWriteRequest request = new TftpWriteRequest();
        request.RemoteFilename = "remote.bin";
        return new TftpClientWriteStateMachine(
            request,
            new MemoryStream(),
            new IPEndPoint(IPAddress.Loopback, 69),
            timeSource);
    }

    private static Socket CreateBoundSocket()
    {
        Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        socket.ReceiveTimeout = 5000;
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return socket;
    }

    private static TftpPacket ReceivePacket(Socket socket)
    {
        byte[] buffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        int length = socket.Receive(buffer);
        return new TftpPacketCodec().Decode(
            buffer.AsSpan(0, length),
            TftpProtocolValues.MaximumBlockSize);
    }

    private static byte[] Encode(TftpPacket packet)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        byte[] encoded = new byte[codec.GetEncodedLength(packet)];
        codec.Encode(packet, encoded);
        return encoded;
    }

    private sealed class CoverageStorage : ITftpServerStorage
    {
        public TftpStorageOpenResult OpenRead(
            TftpServerRequest request,
            CancellationToken cancellationToken)
        {
            return new TftpStorageOpenResult();
        }

        public TftpStorageOpenResult OpenPendingWrite(
            TftpServerRequest request,
            CancellationToken cancellationToken)
        {
            return new TftpStorageOpenResult();
        }

        public TftpStorageCommitResult CommitWrite(
            object completionToken,
            CancellationToken cancellationToken)
        {
            return new TftpStorageCommitResult();
        }

        public TftpStorageCommitResult AbortWrite(
            object completionToken,
            CancellationToken cancellationToken)
        {
            return new TftpStorageCommitResult();
        }
    }

    private sealed class AdvancingTimeSource : ITftpTimeSource
    {
        private long current = -1000;

        public long GetTimestampMilliseconds()
        {
            current += 1000;
            return current;
        }
    }
}
