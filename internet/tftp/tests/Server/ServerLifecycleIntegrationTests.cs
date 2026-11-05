using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tftp;

namespace tftp.Tests;

public class ServerLifecycleIntegrationTests
{
    [Test]
    public void ServesRequestsAcrossMultipleDynamicIpv4Bindings()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("first.bin", [1, 2, 3]);
        storage.Files.Add("second.bin", [4, 5]);
        TftpServerOptions options = Options(4);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            2,
            CancellationToken.None);
        TftpServerStatus listening = harness.Server.GetStatus();

        using LifecycleRawClient firstClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        using LifecycleRawClient secondClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        IPEndPoint firstListener = EndPoint(listening.BoundEndpoints[0]);
        IPEndPoint secondListener = EndPoint(listening.BoundEndpoints[1]);
        RawReceivedPacket firstResponse = CompleteShortRead(
            firstClient,
            firstListener,
            "first.bin",
            [1, 2, 3]);
        RawReceivedPacket secondResponse = CompleteShortRead(
            secondClient,
            secondListener,
            "second.bin",
            [4, 5]);
        harness.WaitForCompletedTransfers(2);
        harness.StopGracefully();
        TftpServerStatus stopped = harness.Server.GetStatus();

        Assert.Multiple(() =>
        {
            Assert.That(firstListener.Port, Is.Not.EqualTo(secondListener.Port));
            Assert.That(firstResponse.SourceEndPoint.Port,
                Is.Not.EqualTo(firstListener.Port));
            Assert.That(secondResponse.SourceEndPoint.Port,
                Is.Not.EqualTo(secondListener.Port));
            Assert.That(stopped.AcceptedTransferCount, Is.EqualTo(2));
            Assert.That(stopped.CompletedTransferCount, Is.EqualTo(2));
            Assert.That(stopped.FailedTransferCount, Is.Zero);
        });
    }

    [Test]
    public void ServesRequestsOnADynamicIpv6BindingWhenSupported()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 sockets are unavailable on this host.");
        }

        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("ipv6.bin", [6, 7, 8]);
        TftpServerOptions options = Options(1);
        options.Bindings.Add(Binding(IPAddress.IPv6Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        TftpServerBinding binding =
            harness.Server.GetStatus().BoundEndpoints[0];
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetworkV6);

        RawReceivedPacket response = CompleteShortRead(
            client,
            EndPoint(binding),
            "ipv6.bin",
            [6, 7, 8]);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(binding.Address, Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(binding.Port, Is.GreaterThan(0));
            Assert.That(response.SourceEndPoint.AddressFamily,
                Is.EqualTo(AddressFamily.InterNetworkV6));
            Assert.That(harness.Server.GetStatus().CompletedTransferCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void RunsConcurrentSessionsOnIndependentTransferSockets()
    {
        byte[] firstContent = Enumerable.Repeat((byte)11, 600).ToArray();
        byte[] secondContent = Enumerable.Repeat((byte)22, 600).ToArray();
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("first.bin", firstContent);
        storage.Files.Add("second.bin", secondContent);
        TftpServerOptions options = Options(2);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient firstClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        using LifecycleRawClient secondClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        firstClient.SendPacket(
            harness.ListenerEndPoints[0],
            ReadRequest("first.bin"));
        secondClient.SendPacket(
            harness.ListenerEndPoints[0],
            ReadRequest("second.bin"));
        RawReceivedPacket firstData = firstClient.ReceivePacket();
        RawReceivedPacket secondData = secondClient.ReceivePacket();
        Assert.That(SpinWait.SpinUntil(
            () => harness.Server.GetStatus().ActiveTransferCount == 2,
            2000),
            Is.True);
        firstClient.SendPacket(
            firstData.SourceEndPoint,
            Acknowledgement(1));
        secondClient.SendPacket(
            secondData.SourceEndPoint,
            Acknowledgement(1));
        RawReceivedPacket firstFinal = firstClient.ReceivePacket();
        RawReceivedPacket secondFinal = secondClient.ReceivePacket();
        firstClient.SendPacket(
            firstFinal.SourceEndPoint,
            Acknowledgement(2));
        secondClient.SendPacket(
            secondFinal.SourceEndPoint,
            Acknowledgement(2));
        harness.WaitForCompletedTransfers(2);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(firstData.SourceEndPoint,
                Is.Not.EqualTo(secondData.SourceEndPoint));
            Assert.That(firstData.Packet.Payload,
                Is.EqualTo(firstContent.AsSpan(0, 512).ToArray()));
            Assert.That(secondData.Packet.Payload,
                Is.EqualTo(secondContent.AsSpan(0, 512).ToArray()));
            Assert.That(firstFinal.Packet.Payload,
                Is.EqualTo(firstContent.AsSpan(512).ToArray()));
            Assert.That(secondFinal.Packet.Payload,
                Is.EqualTo(secondContent.AsSpan(512).ToArray()));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void DuplicateInitialRequestReusesTheActiveSession()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("duplicate.bin", [3, 2, 1]);
        TftpServerOptions options = Options(1);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        TftpPacket request = ReadRequest("duplicate.bin");

        client.SendPacket(harness.ListenerEndPoints[0], request);
        RawReceivedPacket first = client.ReceivePacket();
        client.SendPacket(harness.ListenerEndPoints[0], request);
        RawReceivedPacket repeated = client.ReceivePacket();
        TftpServerStatus active = harness.Server.GetStatus();
        client.SendPacket(repeated.SourceEndPoint, Acknowledgement(1));
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent terminal = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Completed);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.SourceEndPoint,
                Is.EqualTo(first.SourceEndPoint));
            Assert.That(repeated.Packet.Payload,
                Is.EqualTo(first.Packet.Payload));
            Assert.That(active.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(active.ActiveTransferCount, Is.EqualTo(1));
            Assert.That(storage.OpenReadCount, Is.EqualTo(1));
            Assert.That(terminal.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(harness.Observer.Events.Count(
                transferEvent =>
                    transferEvent.Message.Contains("Duplicate")),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsRequestsBeyondTheConfiguredSessionLimit()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("active.bin", new byte[600]);
        storage.Files.Add("rejected.bin", [9]);
        TftpServerOptions options = Options(1);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient activeClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);
        using LifecycleRawClient rejectedClient =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        activeClient.SendPacket(
            harness.ListenerEndPoints[0],
            ReadRequest("active.bin"));
        RawReceivedPacket firstData = activeClient.ReceivePacket();
        rejectedClient.SendPacket(
            harness.ListenerEndPoints[0],
            ReadRequest("rejected.bin"));
        RawReceivedPacket rejection = rejectedClient.ReceivePacket();
        TftpServerStatus limited = harness.Server.GetStatus();
        activeClient.SendPacket(
            firstData.SourceEndPoint,
            Acknowledgement(1));
        RawReceivedPacket finalData = activeClient.ReceivePacket();
        activeClient.SendPacket(
            finalData.SourceEndPoint,
            Acknowledgement(2));
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();

        Assert.Multiple(() =>
        {
            Assert.That(rejection.SourceEndPoint,
                Is.EqualTo(harness.ListenerEndPoints[0]));
            Assert.That(rejection.Packet.Opcode,
                Is.EqualTo(TftpOpcode.Error));
            Assert.That(rejection.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(limited.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(limited.ActiveTransferCount, Is.EqualTo(1));
            Assert.That(storage.OpenReadCount, Is.EqualTo(1));
            Assert.That(harness.Server.GetStatus().FailedTransferCount,
                Is.Zero);
        });
    }

    [Test]
    public void ContinuesListeningAfterASessionFault()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.FaultFilename = "fault.bin";
        storage.Files.Add("good.bin", [7, 7]);
        TftpServerOptions options = Options(2);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        client.SendPacket(
            harness.ListenerEndPoints[0],
            ReadRequest("fault.bin"));
        harness.WaitForFailedTransfers(1);
        TftpServerStatus afterFault = harness.Server.GetStatus();
        RawReceivedPacket response = CompleteShortRead(
            client,
            harness.ListenerEndPoints[0],
            "good.bin",
            [7, 7]);
        harness.WaitForCompletedTransfers(1);
        harness.StopGracefully();
        TftpTransferEvent failure = harness.Observer.Events.Single(
            transferEvent =>
                transferEvent.State == TftpTransferState.Failed);

        Assert.Multiple(() =>
        {
            Assert.That(afterFault.IsRunning, Is.True);
            Assert.That(afterFault.FailedTransferCount, Is.EqualTo(1));
            Assert.That(response.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(2));
            Assert.That(harness.Server.GetStatus().CompletedTransferCount,
                Is.EqualTo(1));
            Assert.That(failure.HasError, Is.True);
            Assert.That(failure.ErrorCode,
                Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(failure.Message, Is.EqualTo("storage fault"));
        });
    }

    [Test]
    public void CancellationConfiguredForGracefulStopDrainsActiveTransfer()
    {
        LifecycleReadStorage storage = new LifecycleReadStorage();
        storage.Files.Add("drain.bin", new byte[600]);
        TftpServerOptions options = Options(1);
        options.StopMode = TftpServerStopMode.Graceful;
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using CancellationTokenSource cancellation =
            new CancellationTokenSource();
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            cancellation.Token);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        client.SendPacket(
            harness.ListenerEndPoints[0],
            ReadRequest("drain.bin"));
        RawReceivedPacket firstData = client.ReceivePacket();
        cancellation.Cancel();
        Assert.That(SpinWait.SpinUntil(() =>
        {
            TftpServerStatus status = harness.Server.GetStatus();
            return status.IsDraining &&
                status.ActiveTransferCount == 1 &&
                status.BoundEndpoints.Count == 0;
        }, 2000), Is.True);
        Assert.That(harness.RunTask.IsCompleted, Is.False);
        client.SendPacket(
            firstData.SourceEndPoint,
            Acknowledgement(1));
        RawReceivedPacket finalData = client.ReceivePacket();
        client.SendPacket(
            finalData.SourceEndPoint,
            Acknowledgement(2));
        harness.WaitForRunStopped();
        TftpServerStatus stopped = harness.Server.GetStatus();

        Assert.Multiple(() =>
        {
            Assert.That(stopped.IsRunning, Is.False);
            Assert.That(stopped.IsDraining, Is.False);
            Assert.That(stopped.ActiveTransferCount, Is.Zero);
            Assert.That(stopped.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(stopped.CompletedTransferCount, Is.EqualTo(1));
            Assert.That(stopped.FailedTransferCount, Is.Zero);
        });
    }

    [Test]
    public void ImmediateStopCancelsAndAbortsAnActiveWrite()
    {
        RecordingServerStorage storage = WritableStorage();
        TftpServerOptions options = Options(1);
        options.Bindings.Add(Binding(IPAddress.Loopback));
        using LifecycleServerHarness harness = new LifecycleServerHarness(
            storage,
            options,
            1,
            CancellationToken.None);
        using LifecycleRawClient client =
            new LifecycleRawClient(AddressFamily.InterNetwork);

        client.SendPacket(
            harness.ListenerEndPoints[0],
            WriteRequest("cancel.bin"));
        RawReceivedPacket acknowledgementZero = client.ReceivePacket();
        Assert.That(acknowledgementZero.Packet.Opcode,
            Is.EqualTo(TftpOpcode.Acknowledgement));
        Assert.That(harness.Server.GetStatus().ActiveTransferCount,
            Is.EqualTo(1));

        harness.Server.Stop(TftpServerStopMode.Immediate);
        harness.WaitForRunStopped();
        Assert.That(SpinWait.SpinUntil(
            () => storage.LastOperation == "AbortWrite",
            2000),
            Is.True);
        TftpServerStatus stopped = harness.Server.GetStatus();

        Assert.Multiple(() =>
        {
            Assert.That(stopped.IsRunning, Is.False);
            Assert.That(stopped.ActiveTransferCount, Is.Zero);
            Assert.That(stopped.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(stopped.CompletedTransferCount, Is.Zero);
            Assert.That(stopped.FailedTransferCount, Is.EqualTo(1));
            Assert.That(storage.WriteResult.Content!.CanWrite, Is.False);
        });
    }

    private static RawReceivedPacket CompleteShortRead(
        LifecycleRawClient client,
        IPEndPoint listenerEndPoint,
        string filename,
        byte[] expectedContent)
    {
        client.SendPacket(listenerEndPoint, ReadRequest(filename));
        RawReceivedPacket response = client.ReceivePacket();
        Assert.Multiple(() =>
        {
            Assert.That(response.Packet.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(response.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(response.Packet.Payload, Is.EqualTo(expectedContent));
        });
        client.SendPacket(response.SourceEndPoint, Acknowledgement(1));
        return response;
    }

    private static TftpServerOptions Options(int maximumSessions)
    {
        TftpServerOptions options = new TftpServerOptions();
        options.MaximumSessions = maximumSessions;
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

    private static IPEndPoint EndPoint(TftpServerBinding binding)
    {
        return new IPEndPoint(binding.Address, binding.Port);
    }

    private static TftpPacket ReadRequest(string filename)
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = filename;
        request.Mode = TftpProtocolValues.OctetMode;
        return request;
    }

    private static TftpPacket WriteRequest(string filename)
    {
        TftpPacket request = ReadRequest(filename);
        request.Opcode = TftpOpcode.WriteRequest;
        return request;
    }

    private static TftpPacket Acknowledgement(ushort blockNumber)
    {
        TftpPacket acknowledgement = new TftpPacket();
        acknowledgement.Opcode = TftpOpcode.Acknowledgement;
        acknowledgement.BlockNumber = blockNumber;
        return acknowledgement;
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
}

internal class LifecycleServerHarness : IDisposable
{
    internal TftpServer Server;
    internal RecordingServerObserver Observer;
    internal Task RunTask;
    internal List<IPEndPoint> ListenerEndPoints;

    internal LifecycleServerHarness(
        ITftpServerStorage storage,
        TftpServerOptions options,
        int expectedBindingCount,
        CancellationToken cancellationToken)
    {
        Observer = new RecordingServerObserver();
        Server = new TftpServer(storage, Observer);
        ListenerEndPoints = [];
        RunTask = Task.Run(() => Server.Run(options, cancellationToken));
        bool started = SpinWait.SpinUntil(() =>
        {
            TftpServerStatus status = Server.GetStatus();
            return status.IsRunning &&
                status.BoundEndpoints.Count == expectedBindingCount;
        }, 3000);
        if (!started)
        {
            Server.Stop(TftpServerStopMode.Immediate);
            throw new AssertionException("The lifecycle server did not start.");
        }

        TftpServerStatus listening = Server.GetStatus();
        foreach (TftpServerBinding binding in listening.BoundEndpoints)
        {
            ListenerEndPoints.Add(
                new IPEndPoint(binding.Address, binding.Port));
        }
    }

    internal void WaitForCompletedTransfers(long expectedCount)
    {
        Assert.That(SpinWait.SpinUntil(
            () => Server.GetStatus().CompletedTransferCount == expectedCount,
            5000),
            Is.True);
    }

    internal void WaitForFailedTransfers(long expectedCount)
    {
        Assert.That(SpinWait.SpinUntil(
            () => Server.GetStatus().FailedTransferCount == expectedCount,
            5000),
            Is.True);
    }

    internal void StopGracefully()
    {
        Server.Stop(TftpServerStopMode.Graceful);
        WaitForRunStopped();
    }

    internal void WaitForRunStopped()
    {
        Assert.That(SpinWait.SpinUntil(() => RunTask.IsCompleted, 5000),
            Is.True);
        RunTask.GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Server.Stop(TftpServerStopMode.Immediate);
        bool stopped = SpinWait.SpinUntil(() => RunTask.IsCompleted, 5000);
        if (!stopped)
        {
            throw new AssertionException("The lifecycle server did not stop.");
        }

        RunTask.GetAwaiter().GetResult();
    }
}

internal class LifecycleRawClient : IDisposable
{
    internal Socket Socket;
    internal TftpPacketCodec Codec;

    internal LifecycleRawClient(AddressFamily addressFamily)
    {
        Socket = new Socket(
            addressFamily,
            SocketType.Dgram,
            ProtocolType.Udp);
        IPAddress localAddress = IPAddress.Loopback;
        if (addressFamily == AddressFamily.InterNetworkV6)
        {
            Socket.DualMode = false;
            localAddress = IPAddress.IPv6Loopback;
        }

        Socket.Bind(new IPEndPoint(localAddress, 0));
        Socket.ReceiveTimeout = 2500;
        Codec = new TftpPacketCodec();
    }

    internal void SendPacket(IPEndPoint endPoint, TftpPacket packet)
    {
        byte[] buffer = new byte[Codec.GetEncodedLength(packet)];
        Codec.Encode(packet, buffer.AsSpan());
        Socket.SendTo(buffer, endPoint);
    }

    internal RawReceivedPacket ReceivePacket()
    {
        byte[] buffer = new byte[TftpProtocolValues.MaximumDataPacketLength];
        IPAddress anyAddress = IPAddress.Any;
        if (Socket.AddressFamily == AddressFamily.InterNetworkV6)
        {
            anyAddress = IPAddress.IPv6Any;
        }

        EndPoint source = new IPEndPoint(anyAddress, 0);
        int length = Socket.ReceiveFrom(buffer, ref source);
        RawReceivedPacket received = new RawReceivedPacket();
        received.Packet = Codec.Decode(
            buffer.AsSpan(0, length),
            TftpProtocolValues.MaximumBlockSize);
        received.SourceEndPoint = (IPEndPoint)source;
        return received;
    }

    public void Dispose()
    {
        Socket.Dispose();
    }
}

internal class LifecycleReadStorage : ITftpServerStorage
{
    internal Dictionary<string, byte[]> Files;
    internal string FaultFilename;
    internal int OpenReadCount;

    internal LifecycleReadStorage()
    {
        Files = new Dictionary<string, byte[]>();
        FaultFilename = "";
        OpenReadCount = 0;
    }

    public TftpStorageOpenResult OpenRead(
        TftpServerRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref OpenReadCount);
        if (request.Filename == FaultFilename)
        {
            throw new InvalidOperationException("storage fault");
        }

        TftpStorageOpenResult result = new TftpStorageOpenResult();
        if (!Files.ContainsKey(request.Filename))
        {
            result.ErrorCode = TftpErrorCode.FileNotFound;
            result.ErrorMessage = "missing";
            return result;
        }

        byte[] content = Files[request.Filename];
        result.Success = true;
        result.Content = new MemoryStream(content);
        result.OwnsContent = true;
        result.HasKnownSize = true;
        result.KnownSize = content.Length;
        return result;
    }

    public TftpStorageOpenResult OpenPendingWrite(
        TftpServerRequest request,
        CancellationToken cancellationToken)
    {
        TftpStorageOpenResult result = new TftpStorageOpenResult();
        result.ErrorCode = TftpErrorCode.AccessViolation;
        result.ErrorMessage = "writes disabled";
        return result;
    }

    public TftpStorageCommitResult CommitWrite(
        object completionToken,
        CancellationToken cancellationToken)
    {
        TftpStorageCommitResult result = new TftpStorageCommitResult();
        result.ErrorCode = TftpErrorCode.AccessViolation;
        result.ErrorMessage = "writes disabled";
        return result;
    }

    public TftpStorageCommitResult AbortWrite(
        object completionToken,
        CancellationToken cancellationToken)
    {
        TftpStorageCommitResult result = new TftpStorageCommitResult();
        result.Success = true;
        return result;
    }
}
