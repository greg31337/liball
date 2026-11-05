using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class PublicTelemetryEndToEndTests
{
    [Test]
    public void SuccessfulClientTransferPopulatesEveryResultAndEventField()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        byte[] firstPayload = [1, 2, 3, 4, 5, 6, 7, 8];
        Task script = Task.Run(() =>
        {
            TftpPacket firstRequest = peer.ReceiveClientPacket();
            TftpPacket secondRequest = peer.ReceiveClientPacket();
            Assert.Multiple(() =>
            {
                Assert.That(firstRequest.Opcode,
                    Is.EqualTo(TftpOpcode.ReadRequest));
                Assert.That(secondRequest.Options, Has.Count.EqualTo(3));
            });
            peer.SendClientPacket(Oack(
                Option("blksize", "8"),
                Option("timeout", "2"),
                Option("tsize", "9")));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.Zero);
            peer.SendClientPacket(Data(1, firstPayload));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
            peer.SendClientPacket(Data(1, firstPayload));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(1));
            peer.SendClientPacket(Data(2, [9]));
            Assert.That(peer.ReceiveClientPacket().BlockNumber, Is.EqualTo(2));
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.Mode = TftpTransferMode.Netascii;
        request.Options.RequestBlockSize = true;
        request.Options.BlockSize = 8;
        request.Options.RequestTimeout = true;
        request.Options.TimeoutSeconds = 2;
        request.Options.RequestTransferSize = true;
        request.Options.InitialTimeoutSeconds = 1;
        request.Options.MaximumTimeoutSeconds = 2;
        request.Options.MaximumRetries = 2;
        request.Options.DallyDurationSeconds = 0;
        request.Endpoint.LocalAddress = IPAddress.Loopback;
        using MemoryStream destination = new MemoryStream();

        TftpTransferResult result = new TftpClient(observer).DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        script.GetAwaiter().GetResult();
        TftpTransferEvent created = observer.Events.Single(
            transferEvent => transferEvent.State == TftpTransferState.Created);
        TftpTransferEvent duplicate = observer.Events.Single(
            transferEvent => transferEvent.DuplicatePacketCount == 1 &&
                transferEvent.State == TftpTransferState.Receiving);

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(),
                Is.EqualTo(firstPayload.Concat(new byte[] { 9 }).ToArray()));
            Assert.That(result.Direction, Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(result.FinalEndpoint.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(result.FinalEndpoint.Port,
                Is.EqualTo(((IPEndPoint)peer.Listener.LocalEndPoint!).Port));
            Assert.That(result.FinalEndpoint.LocalAddress, Is.Null);
            Assert.That(result.FinalEndpoint.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(result.BytesTransferred, Is.EqualTo(9));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(9));
            Assert.That(result.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(1000));
            Assert.That(result.RetransmissionCount, Is.EqualTo(1));
            Assert.That(result.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(8));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(9));
            Assert.That(result.NegotiatedOptions.Select(option => option.Name),
                Is.EqualTo(new[] { "blksize", "timeout", "tsize" }));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Created));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Resolving));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Requesting));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Negotiating));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Receiving));
            Assert.That(observer.States,
                Does.Contain(TftpTransferState.Dallying));
            Assert.That(observer.States[^1],
                Is.EqualTo(TftpTransferState.Completed));

            Assert.That(created.Peer.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(created.Peer.LocalAddress, Is.EqualTo(IPAddress.Loopback));
            Assert.That(duplicate.Direction,
                Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(duplicate.State,
                Is.EqualTo(TftpTransferState.Receiving));
            Assert.That(duplicate.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(duplicate.Peer.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(duplicate.Peer.Port,
                Is.EqualTo(((IPEndPoint)peer.Listener.LocalEndPoint!).Port));
            Assert.That(duplicate.Peer.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(duplicate.BlockNumber, Is.EqualTo(1));
            Assert.That(duplicate.BytesTransferred, Is.EqualTo(8));
            Assert.That(duplicate.EncodedBytesTransferred, Is.EqualTo(8));
            Assert.That(duplicate.RetransmissionCount, Is.EqualTo(1));
            Assert.That(duplicate.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(duplicate.NegotiatedOptions, Has.Count.EqualTo(3));
            Assert.That(duplicate.HasError, Is.False);
            Assert.That(duplicate.ErrorCode,
                Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(duplicate.Message,
                Is.EqualTo("Client read transfer is receiving data."));
            Assert.That(duplicate.ServerRequest, Is.Null);
        });
    }

    [Test]
    public void RemoteFailurePopulatesExceptionAndErrorEventDetails()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        Task script = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(Error(
                TftpErrorCode.AccessViolation,
                "read forbidden"));
        });
        RecordingClientObserver observer = new RecordingClientObserver();
        TftpReadRequest request = peer.CreateReadRequest();
        request.Options.Mode = TftpTransferMode.Netascii;

        TftpRemoteErrorException exception =
            Assert.Throws<TftpRemoteErrorException>(() =>
                new TftpClient(observer).DownloadToStream(
                    request,
                    new MemoryStream(),
                    CancellationToken.None));
        script.GetAwaiter().GetResult();
        TftpTransferEvent failure = observer.Events[^1];

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("read forbidden"));
            Assert.That(exception.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(exception.RemoteMessage, Is.EqualTo("read forbidden"));
            Assert.That(failure.Direction,
                Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(failure.State, Is.EqualTo(TftpTransferState.Failed));
            Assert.That(failure.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(failure.Peer.Port,
                Is.EqualTo(((IPEndPoint)peer.Listener.LocalEndPoint!).Port));
            Assert.That(failure.BlockNumber, Is.Zero);
            Assert.That(failure.BytesTransferred, Is.Zero);
            Assert.That(failure.EncodedBytesTransferred, Is.Zero);
            Assert.That(failure.RetransmissionCount, Is.Zero);
            Assert.That(failure.DuplicatePacketCount, Is.Zero);
            Assert.That(failure.NegotiatedOptions, Is.Empty);
            Assert.That(failure.HasError, Is.True);
            Assert.That(failure.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(failure.Message, Is.EqualTo("read forbidden"));
            Assert.That(failure.ServerRequest, Is.Null);
        });
    }

    [Test]
    public void PublicOperationsPopulateProtocolOptionTimeoutAndStorageExceptions()
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        TftpProtocolException wireFailure =
            Assert.Throws<TftpProtocolException>(() =>
                codec.Decode([0, 4, 0], 0));
        TftpProtocolException localFailure =
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClient(null).DownloadToStream(
                    null!,
                    new MemoryStream(),
                    CancellationToken.None));

        using ScriptedWritePeer optionPeer = new ScriptedWritePeer();
        Task optionScript = Task.Run(() =>
        {
            optionPeer.ReceiveClientPacket();
            optionPeer.SendClientPacket(Oack(Option("blksize", "9")));
            Assert.That(optionPeer.ReceiveClientPacket().ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
        });
        TftpWriteRequest optionRequest = optionPeer.CreateWriteRequest();
        optionRequest.Options.RequestBlockSize = true;
        optionRequest.Options.BlockSize = 8;
        TftpOptionNegotiationException optionFailure =
            Assert.Throws<TftpOptionNegotiationException>(() =>
                new TftpClient(null).UploadFromStream(
                    optionRequest,
                    new MemoryStream([1]),
                    CancellationToken.None));
        optionScript.GetAwaiter().GetResult();

        using ScriptedReadPeer timeoutPeer = new ScriptedReadPeer();
        TftpReadRequest timeoutRequest = timeoutPeer.CreateReadRequest();
        timeoutRequest.Options.InitialTimeoutSeconds = 1;
        timeoutRequest.Options.MaximumTimeoutSeconds = 1;
        timeoutRequest.Options.MaximumRetries = 4;
        timeoutRequest.Options.MaximumTransferDurationSeconds = 1;
        TftpTimeoutException timeoutFailure =
            Assert.Throws<TftpTimeoutException>(() =>
                new TftpClient(null).DownloadToStream(
                    timeoutRequest,
                    new MemoryStream(),
                    CancellationToken.None));

        TftpServer storageServer = new TftpServer(null, null);
        TftpStorageException storageFailure =
            Assert.Throws<TftpStorageException>(() => storageServer.Run(
                ServerOptions(),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(wireFailure.Message,
                Does.Contain("exactly four bytes"));
            Assert.That(wireFailure.HasPacketOffset, Is.True);
            Assert.That(wireFailure.PacketOffset, Is.EqualTo(3));
            Assert.That(localFailure.Message,
                Is.EqualTo("The client read request cannot be null."));
            Assert.That(localFailure.HasPacketOffset, Is.False);
            Assert.That(localFailure.PacketOffset, Is.Zero);
            Assert.That(optionFailure.Message,
                Does.Contain("exceeds the requested value"));
            Assert.That(optionFailure.OptionName, Is.EqualTo("blksize"));
            Assert.That(optionFailure.OptionValue, Is.EqualTo("9"));
            Assert.That(timeoutFailure.Message,
                Does.Contain("maximum transfer duration"));
            Assert.That(timeoutFailure.RetryLimitReached, Is.False);
            Assert.That(timeoutFailure.RetransmissionCount, Is.Zero);
            Assert.That(timeoutFailure.ElapsedMilliseconds,
                Is.GreaterThanOrEqualTo(1000));
            Assert.That(storageFailure.Message,
                Does.Contain("requires custom or directory-backed storage"));
            Assert.That(storageFailure.Operation, Is.EqualTo("RunServer"));
            Assert.That(storageFailure.InnerException, Is.Null);
        });
    }

    [Test]
    public void ServerStatusAndCompletionEventExposeEveryField()
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        storage.WriteResult.Success = true;
        storage.WriteResult.Content = new MemoryStream();
        storage.WriteResult.OwnsContent = true;
        storage.WriteResult.CompletionToken = new object();
        storage.CommitResult.Success = true;
        storage.AbortResult.Success = true;
        TftpServerOptions options = ServerOptions();
        options.MaximumBlockSize = 8;
        options.DallyDurationSeconds = 1;
        using RawReadServerHarness harness =
            new RawReadServerHarness(storage, options);
        TftpServerStatus listening = harness.Server.GetStatus();
        using RawTftpClient client = new RawTftpClient();
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.WriteRequest;
        request.Filename = "telemetry.txt";
        request.Mode = "NeTaScIi";
        request.Options.Add(Option("BLKSIZE", "8"));
        request.Options.Add(Option("timeout", "1"));
        request.Options.Add(Option("tsize", "4"));

        client.SendPacket(harness.ListenerEndPoint, request);
        RawReceivedPacket firstOack = client.ReceivePacket();
        RawReceivedPacket repeatedOack = client.ReceivePacket();
        TftpServerStatus active = harness.Server.GetStatus();
        harness.Server.Stop(TftpServerStopMode.Graceful);
        TftpServerStatus draining = harness.Server.GetStatus();
        TftpPacket data = Data(1, [65, 13, 10, 66]);
        client.SendPacket(firstOack.SourceEndPoint, data);
        RawReceivedPacket acknowledgement = client.ReceivePacket();
        client.SendPacket(firstOack.SourceEndPoint, data);
        RawReceivedPacket repeatedAcknowledgement = client.ReceivePacket();
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpServerStatus completedStatus = harness.Server.GetStatus();
        TftpTransferEvent completed = harness.Observer.Events.Single(
            transferEvent => transferEvent.State == TftpTransferState.Completed);
        TftpServerRequest serverRequest = completed.ServerRequest!;
        IPEndPoint clientEndPoint = (IPEndPoint)client.Socket.LocalEndPoint!;

        Assert.Multiple(() =>
        {
            Assert.That(firstOack.Packet.Opcode,
                Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(repeatedOack.Packet.Options, Has.Count.EqualTo(3));
            Assert.That(acknowledgement.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(repeatedAcknowledgement.Packet.BlockNumber,
                Is.EqualTo(1));

            Assert.That(listening.IsRunning, Is.True);
            Assert.That(listening.IsDraining, Is.False);
            Assert.That(listening.BoundEndpoints, Has.Count.EqualTo(1));
            Assert.That(listening.ActiveTransferCount, Is.Zero);
            Assert.That(listening.AcceptedTransferCount, Is.Zero);
            Assert.That(listening.CompletedTransferCount, Is.Zero);
            Assert.That(listening.FailedTransferCount, Is.Zero);
            Assert.That(active.ActiveTransferCount, Is.EqualTo(1));
            Assert.That(active.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(draining.IsRunning, Is.True);
            Assert.That(draining.IsDraining, Is.True);
            Assert.That(draining.BoundEndpoints, Is.Empty);
            Assert.That(draining.ActiveTransferCount, Is.EqualTo(1));
            Assert.That(completedStatus.IsRunning, Is.False);
            Assert.That(completedStatus.IsDraining, Is.False);
            Assert.That(completedStatus.BoundEndpoints, Is.Empty);
            Assert.That(completedStatus.ActiveTransferCount, Is.Zero);
            Assert.That(completedStatus.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(completedStatus.CompletedTransferCount, Is.EqualTo(1));
            Assert.That(completedStatus.FailedTransferCount, Is.Zero);

            Assert.That(completed.Direction,
                Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(completed.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(completed.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(completed.Peer.Host,
                Is.EqualTo(clientEndPoint.Address.ToString()));
            Assert.That(completed.Peer.Port, Is.EqualTo(clientEndPoint.Port));
            Assert.That(completed.Peer.LocalAddress, Is.Null);
            Assert.That(completed.Peer.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(completed.BlockNumber, Is.Zero);
            Assert.That(completed.BytesTransferred, Is.EqualTo(3));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(4));
            Assert.That(completed.RetransmissionCount, Is.EqualTo(1));
            Assert.That(completed.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(completed.NegotiatedOptions, Has.Count.EqualTo(3));
            Assert.That(completed.HasError, Is.False);
            Assert.That(completed.ErrorCode,
                Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(completed.Message, Is.EqualTo("Server write completed."));

            Assert.That(serverRequest.Filename, Is.EqualTo("telemetry.txt"));
            Assert.That(serverRequest.Direction,
                Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(serverRequest.Mode, Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(serverRequest.ClientEndpoint.Host,
                Is.EqualTo(clientEndPoint.Address.ToString()));
            Assert.That(serverRequest.ClientEndpoint.Port,
                Is.EqualTo(clientEndPoint.Port));
            Assert.That(serverRequest.ClientEndpoint.LocalAddress, Is.Null);
            Assert.That(serverRequest.ClientEndpoint.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(serverRequest.RequestedOptions, Has.Count.EqualTo(3));
            Assert.That(serverRequest.EffectiveOptions, Has.Count.EqualTo(3));
            Assert.That(serverRequest.HasAnnouncedSize, Is.True);
            Assert.That(serverRequest.AnnouncedSize, Is.EqualTo(4));
        });
    }

    [Test]
    public void ObserverFailuresTerminateClientAndServerCallsDirectly()
    {
        using ScriptedReadPeer peer = new ScriptedReadPeer();
        Task clientScript = Task.Run(() =>
        {
            peer.ReceiveClientPacket();
            peer.SendClientPacket(Oack(Option("blksize", "8")));
        });
        ThrowingTelemetryObserver clientObserver =
            new ThrowingTelemetryObserver(TftpTransferState.Negotiating);
        TftpReadRequest readRequest = peer.CreateReadRequest();
        readRequest.Options.RequestBlockSize = true;
        readRequest.Options.BlockSize = 8;

        InvalidOperationException clientFailure =
            Assert.Throws<InvalidOperationException>(() =>
                new TftpClient(clientObserver).DownloadToStream(
                    readRequest,
                    new MemoryStream(),
                    CancellationToken.None));
        clientScript.GetAwaiter().GetResult();

        RecordingServerStorage storage = new RecordingServerStorage();
        storage.ReadResult.Success = true;
        storage.ReadResult.Content = new MemoryStream(new byte[1024]);
        storage.ReadResult.OwnsContent = true;
        storage.ReadResult.HasKnownSize = true;
        storage.ReadResult.KnownSize = 1024;
        ThrowingTelemetryObserver serverObserver =
            new ThrowingTelemetryObserver(TftpTransferState.Requesting);
        TftpServer server = new TftpServer(storage, serverObserver);
        Task runTask = Task.Run(() => server.Run(
            ServerOptions(),
            CancellationToken.None));
        IPEndPoint listener = WaitForListener(server);
        using RawTftpClient rawClient = new RawTftpClient();
        TftpPacket serverRequest = new TftpPacket();
        serverRequest.Opcode = TftpOpcode.ReadRequest;
        serverRequest.Filename = "observer.bin";
        rawClient.SendPacket(listener, serverRequest);
        Assert.That(SpinWait.SpinUntil(() => runTask.IsCompleted, 3000),
            Is.True);
        InvalidOperationException serverFailure =
            Assert.Throws<InvalidOperationException>(() =>
                runTask.GetAwaiter().GetResult());
        TftpServerStatus status = server.GetStatus();

        Assert.Multiple(() =>
        {
            Assert.That(clientFailure, Is.SameAs(clientObserver.Failure));
            Assert.That(clientObserver.ObservedStates,
                Does.Contain(TftpTransferState.Negotiating));
            Assert.That(serverFailure, Is.SameAs(serverObserver.Failure));
            Assert.That(serverObserver.ObservedStates,
                Is.EqualTo(new[] { TftpTransferState.Requesting }));
            Assert.That(status.IsRunning, Is.False);
            Assert.That(status.ActiveTransferCount, Is.Zero);
            Assert.That(status.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(status.CompletedTransferCount, Is.Zero);
            Assert.That(status.FailedTransferCount, Is.EqualTo(1));
        });
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
        options.MaximumRetries = 2;
        options.MaximumSessionDurationSeconds = 10;
        options.DallyDurationSeconds = 0;
        return options;
    }

    private static IPEndPoint WaitForListener(TftpServer server)
    {
        Assert.That(SpinWait.SpinUntil(() =>
        {
            TftpServerStatus status = server.GetStatus();
            return status.IsRunning && status.BoundEndpoints.Count == 1;
        }, 3000), Is.True);
        TftpServerBinding binding = server.GetStatus().BoundEndpoints[0];
        return new IPEndPoint(binding.Address, binding.Port);
    }

    private static TftpOption Option(string name, string value)
    {
        TftpOption option = new TftpOption();
        option.Name = name;
        option.Value = value;
        return option;
    }

    private static TftpPacket Oack(params TftpOption[] options)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        packet.Options.AddRange(options);
        return packet;
    }

    private static TftpPacket Data(ushort blockNumber, byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = blockNumber;
        packet.Payload = payload;
        return packet;
    }

    private static TftpPacket Error(
        TftpErrorCode errorCode,
        string message)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = errorCode;
        packet.ErrorMessage = message;
        return packet;
    }
}

internal class ThrowingTelemetryObserver : ITftpTransferObserver
{
    internal TftpTransferState ThrowOnState;
    internal InvalidOperationException Failure;
    internal List<TftpTransferState> ObservedStates = [];

    internal ThrowingTelemetryObserver(TftpTransferState throwOnState)
    {
        ThrowOnState = throwOnState;
        Failure = new InvalidOperationException("telemetry observer failed");
    }

    public void Observe(TftpTransferEvent transferEvent)
    {
        ObservedStates.Add(transferEvent.State);
        if (transferEvent.State == ThrowOnState)
        {
            throw Failure;
        }
    }
}
