using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using tftp;

namespace tftp.Tests;

public class FaultProxyEndToEndTests
{
    [Test]
    public void DownloadSurvivesDeterministicLossDuplicationAndReordering()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] content = Content(19);
        File.WriteAllBytes(
            Path.Combine(directory.RootPath, "download.bin"),
            content);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        FaultProxyRule[] rules =
        [
            Rule(FaultProxyDirection.ClientToServer,
                TftpOpcode.ReadRequest, 0, 1, FaultProxyAction.Drop),
            Rule(FaultProxyDirection.ServerToClient,
                TftpOpcode.OptionAcknowledgement, 0, 1,
                FaultProxyAction.Drop),
            Rule(FaultProxyDirection.ClientToServer,
                TftpOpcode.Acknowledgement, 0, 1, FaultProxyAction.Drop),
            Rule(FaultProxyDirection.ServerToClient,
                TftpOpcode.Data, 1, 1, FaultProxyAction.Drop),
            Rule(FaultProxyDirection.ServerToClient,
                TftpOpcode.Data, 1, 2, FaultProxyAction.Duplicate),
            Rule(FaultProxyDirection.ClientToServer,
                TftpOpcode.Acknowledgement, 1, 1,
                FaultProxyAction.Drop),
            Rule(FaultProxyDirection.ClientToServer,
                TftpOpcode.Acknowledgement, 2, 1,
                FaultProxyAction.PassAndHoldCopy)
        ];
        using DeterministicTftpFaultProxy proxy =
            new DeterministicTftpFaultProxy(
                harness.ListenerEndPoint,
                rules);
        TftpClient client = new TftpClient(null);
        TftpReadRequest request = ReadRequest(
            proxy.ProxyEndPoint,
            "download.bin");
        using MemoryStream destination = new MemoryStream();

        TftpTransferResult result = client.DownloadToStream(
            request,
            destination,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(1);
        proxy.AssertHealthy();

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.RetransmissionCount, Is.GreaterThan(0));
            Assert.That(result.DuplicatePacketCount, Is.GreaterThan(0));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(1));
            Assert.That(proxy.PacketCount, Is.LessThan(40));
            Assert.That(rules, Has.All.Matches<FaultProxyRule>(
                rule => rule.ApplicationCount == 1));
        });
    }

    [Test]
    public void UploadWritesOnceThroughDelayedDuplicatedAndReorderedPackets()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        byte[] content = Content(19);
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        FaultProxyRule[] rules =
        [
            Rule(FaultProxyDirection.ClientToServer,
                TftpOpcode.WriteRequest, 0, 1, FaultProxyAction.Drop),
            Rule(FaultProxyDirection.ServerToClient,
                TftpOpcode.OptionAcknowledgement, 0, 1,
                FaultProxyAction.Delay),
            Rule(FaultProxyDirection.ClientToServer,
                TftpOpcode.Data, 1, 1, FaultProxyAction.Drop),
            Rule(FaultProxyDirection.ClientToServer,
                TftpOpcode.Data, 1, 2, FaultProxyAction.Duplicate),
            Rule(FaultProxyDirection.ServerToClient,
                TftpOpcode.Acknowledgement, 1, 1,
                FaultProxyAction.Drop),
            Rule(FaultProxyDirection.ServerToClient,
                TftpOpcode.Acknowledgement, 2, 1,
                FaultProxyAction.PassAndHoldCopy)
        ];
        rules[1].DelayMilliseconds = 125;
        using DeterministicTftpFaultProxy proxy =
            new DeterministicTftpFaultProxy(
                harness.ListenerEndPoint,
                rules);
        TftpClient client = new TftpClient(null);
        TftpWriteRequest request = WriteRequest(
            proxy.ProxyEndPoint,
            "upload.bin",
            content.Length);
        using MemoryStream source = new MemoryStream(content);

        TftpTransferResult result = client.UploadFromStream(
            request,
            source,
            CancellationToken.None);
        harness.WaitForCompletedTransfers(1);
        proxy.AssertHealthy();

        Assert.Multiple(() =>
        {
            Assert.That(
                File.ReadAllBytes(Path.Combine(
                    directory.RootPath,
                    "upload.bin")),
                Is.EqualTo(content));
            Assert.That(
                Directory.GetFiles(directory.RootPath, "*.tmp"),
                Is.Empty);
            Assert.That(result.State, Is.EqualTo(TftpTransferState.Completed));
            Assert.That(result.RetransmissionCount, Is.GreaterThan(0));
            Assert.That(result.DuplicatePacketCount, Is.GreaterThan(0));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(1));
            Assert.That(proxy.PacketCount, Is.LessThan(40));
            Assert.That(rules, Has.All.Matches<FaultProxyRule>(
                rule => rule.ApplicationCount == 1));
        });
    }

    [Test]
    public void ErrorLossRetriesOnlyTheRequestAndErrorsRemainTerminal()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        FaultProxyRule dropError = Rule(
            FaultProxyDirection.ServerToClient,
            TftpOpcode.Error,
            0,
            1,
            FaultProxyAction.Drop);
        using DeterministicTftpFaultProxy proxy =
            new DeterministicTftpFaultProxy(
                harness.ListenerEndPoint,
                [dropError]);
        TftpClient client = new TftpClient(null);
        TftpReadRequest request = ReadRequest(
            proxy.ProxyEndPoint,
            "missing.bin");

        TftpRemoteErrorException error = Assert.Throws<TftpRemoteErrorException>(
            () => client.DownloadToStream(
                request,
                new MemoryStream(),
                CancellationToken.None));
        Assert.That(SpinWait.SpinUntil(
            () => harness.Server.GetStatus().FailedTransferCount == 2,
            5000),
            Is.True);
        int errorCount = proxy.CountPackets(
            FaultProxyDirection.ServerToClient,
            TftpOpcode.Error,
            0);
        Thread.Sleep(1100);
        proxy.AssertHealthy();

        Assert.Multiple(() =>
        {
            Assert.That(error.ErrorCode, Is.EqualTo(TftpErrorCode.FileNotFound));
            Assert.That(dropError.ApplicationCount, Is.EqualTo(1));
            Assert.That(errorCount, Is.EqualTo(2));
            Assert.That(proxy.CountPackets(
                FaultProxyDirection.ServerToClient,
                TftpOpcode.Error,
                0), Is.EqualTo(errorCount));
            Assert.That(proxy.CountPackets(
                FaultProxyDirection.ClientToServer,
                TftpOpcode.ReadRequest,
                0), Is.EqualTo(2));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(2));
            Assert.That(proxy.PacketCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void DelayedDuplicateErrorDoesNotCauseErrorRetransmission()
    {
        using DirectoryStorageWriteTestDirectory directory =
            new DirectoryStorageWriteTestDirectory();
        using EndToEndServerHarness harness = new EndToEndServerHarness(
            directory.RootPath,
            IPAddress.Loopback);
        FaultProxyRule delayedError = Rule(
            FaultProxyDirection.ServerToClient,
            TftpOpcode.Error,
            0,
            1,
            FaultProxyAction.DelayAndDuplicate);
        delayedError.DelayMilliseconds = 125;
        using DeterministicTftpFaultProxy proxy =
            new DeterministicTftpFaultProxy(
                harness.ListenerEndPoint,
                [delayedError]);
        TftpClient client = new TftpClient(null);
        TftpReadRequest request = ReadRequest(
            proxy.ProxyEndPoint,
            "missing.bin");

        TftpRemoteErrorException error = Assert.Throws<TftpRemoteErrorException>(
            () => client.DownloadToStream(
                request,
                new MemoryStream(),
                CancellationToken.None));
        Assert.That(SpinWait.SpinUntil(
            () => harness.Server.GetStatus().FailedTransferCount == 1,
            5000),
            Is.True);
        Thread.Sleep(1100);
        proxy.AssertHealthy();

        Assert.Multiple(() =>
        {
            Assert.That(error.ErrorCode, Is.EqualTo(TftpErrorCode.FileNotFound));
            Assert.That(delayedError.ApplicationCount, Is.EqualTo(1));
            Assert.That(proxy.CountPackets(
                FaultProxyDirection.ServerToClient,
                TftpOpcode.Error,
                0), Is.EqualTo(1));
            Assert.That(proxy.ForwardedPacketCount, Is.EqualTo(3));
            Assert.That(harness.Server.GetStatus().AcceptedTransferCount,
                Is.EqualTo(1));
        });
    }

    private static FaultProxyRule Rule(
        FaultProxyDirection direction,
        TftpOpcode opcode,
        ushort blockNumber,
        int occurrence,
        FaultProxyAction action)
    {
        return new FaultProxyRule(
            direction,
            opcode,
            blockNumber,
            occurrence,
            action);
    }

    private static TftpReadRequest ReadRequest(
        IPEndPoint proxyEndPoint,
        string filename)
    {
        TftpReadRequest request = new TftpReadRequest();
        SetEndpoint(request.Endpoint, proxyEndPoint);
        request.RemoteFilename = filename;
        request.Options = TransferOptions();
        return request;
    }

    private static TftpWriteRequest WriteRequest(
        IPEndPoint proxyEndPoint,
        string filename,
        long sourceSize)
    {
        TftpWriteRequest request = new TftpWriteRequest();
        SetEndpoint(request.Endpoint, proxyEndPoint);
        request.RemoteFilename = filename;
        request.Options = TransferOptions();
        request.HasKnownSourceSize = true;
        request.KnownSourceSize = sourceSize;
        return request;
    }

    private static TftpTransferOptions TransferOptions()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = TftpProtocolValues.MinimumBlockSize;
        options.RequestTimeout = true;
        options.TimeoutSeconds = 1;
        options.RequestTransferSize = true;
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 1;
        options.MaximumRetries = 6;
        options.MaximumTransferDurationSeconds = 30;
        options.DallyDurationSeconds = 0;
        return options;
    }

    private static void SetEndpoint(
        TftpEndpoint endpoint,
        IPEndPoint proxyEndPoint)
    {
        endpoint.Host = proxyEndPoint.Address.ToString();
        endpoint.Port = proxyEndPoint.Port;
        endpoint.AddressFamily = AddressFamily.InterNetwork;
        endpoint.LocalAddress = IPAddress.Loopback;
    }

    private static byte[] Content(int length)
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
}

internal enum FaultProxyDirection
{
    ClientToServer,
    ServerToClient
}

internal enum FaultProxyAction
{
    Pass,
    Drop,
    Duplicate,
    Delay,
    DelayAndDuplicate,
    PassAndHoldCopy
}

internal class FaultProxyRule
{
    internal FaultProxyDirection Direction;
    internal TftpOpcode Opcode;
    internal ushort BlockNumber;
    internal int Occurrence;
    internal FaultProxyAction Action;
    internal int DelayMilliseconds;
    internal int MatchCount;
    internal int ApplicationCount;

    internal FaultProxyRule(
        FaultProxyDirection direction,
        TftpOpcode opcode,
        ushort blockNumber,
        int occurrence,
        FaultProxyAction action)
    {
        Direction = direction;
        Opcode = opcode;
        BlockNumber = blockNumber;
        Occurrence = occurrence;
        Action = action;
        DelayMilliseconds = 0;
        MatchCount = 0;
        ApplicationCount = 0;
    }

    internal bool Matches(FaultProxyDirection direction, TftpPacket packet)
    {
        if (Direction != direction || Opcode != packet.Opcode)
        {
            return false;
        }

        if (Opcode == TftpOpcode.Data ||
            Opcode == TftpOpcode.Acknowledgement)
        {
            return BlockNumber == packet.BlockNumber;
        }

        return true;
    }
}

internal class FaultProxyDatagram
{
    internal byte[] Bytes;
    internal IPEndPoint Destination;

    internal FaultProxyDatagram(byte[] bytes, IPEndPoint destination)
    {
        Bytes = bytes;
        Destination = destination;
    }
}

internal class FaultProxyObservation
{
    internal FaultProxyDirection Direction;
    internal TftpOpcode Opcode;
    internal ushort BlockNumber;

    internal FaultProxyObservation(
        FaultProxyDirection direction,
        TftpOpcode opcode,
        ushort blockNumber)
    {
        Direction = direction;
        Opcode = opcode;
        BlockNumber = blockNumber;
    }
}

internal class DeterministicTftpFaultProxy : IDisposable
{
    internal Socket ProxySocket;
    internal IPEndPoint ServerListenerEndPoint;
    internal IPEndPoint ProxyEndPoint;
    internal IPEndPoint? ClientEndPoint;
    internal IPEndPoint? ServerTransferEndPoint;
    internal FaultProxyRule[] Rules;
    internal List<FaultProxyObservation> Observations = [];
    internal object SyncRoot = new object();
    internal CancellationTokenSource Cancellation =
        new CancellationTokenSource();
    internal Task RunTask;
    internal Exception? Failure;
    internal FaultProxyDatagram? HeldDatagram;
    internal int ForwardedPacketCount;

    internal DeterministicTftpFaultProxy(
        IPEndPoint serverListenerEndPoint,
        FaultProxyRule[] rules)
    {
        ServerListenerEndPoint = new IPEndPoint(
            serverListenerEndPoint.Address,
            serverListenerEndPoint.Port);
        Rules = rules;
        ProxySocket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        ProxySocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        ProxyEndPoint = (IPEndPoint)ProxySocket.LocalEndPoint!;
        RunTask = Task.Run(RunProxy);
    }

    internal int PacketCount
    {
        get
        {
            lock (SyncRoot)
            {
                return Observations.Count;
            }
        }
    }

    internal int CountPackets(
        FaultProxyDirection direction,
        TftpOpcode opcode,
        ushort blockNumber)
    {
        int count = 0;
        lock (SyncRoot)
        {
            foreach (FaultProxyObservation observation in Observations)
            {
                if (observation.Direction == direction &&
                    observation.Opcode == opcode &&
                    (opcode != TftpOpcode.Data &&
                        opcode != TftpOpcode.Acknowledgement ||
                        observation.BlockNumber == blockNumber))
                {
                    count = count + 1;
                }
            }
        }

        return count;
    }

    internal void AssertHealthy()
    {
        Assert.That(Failure, Is.Null);
    }

    internal void RunProxy()
    {
        try
        {
            byte[] buffer = new byte[TftpProtocolValues.MaximumBlockSize + 4];
            while (!Cancellation.IsCancellationRequested)
            {
                if (!ProxySocket.Poll(50000, SelectMode.SelectRead))
                {
                    continue;
                }

                EndPoint source = new IPEndPoint(IPAddress.Any, 0);
                int length = ProxySocket.ReceiveFrom(buffer, ref source);
                byte[] datagram = buffer.AsSpan(0, length).ToArray();
                ProcessDatagram(datagram, (IPEndPoint)source);
            }
        }
        catch (ObjectDisposedException)
        {
            if (!Cancellation.IsCancellationRequested)
            {
                Failure = new AssertionException(
                    "The fault proxy socket was disposed unexpectedly.");
            }
        }
        catch (SocketException exception)
        {
            if (!Cancellation.IsCancellationRequested)
            {
                Failure = exception;
            }
        }
        catch (Exception exception)
        {
            Failure = exception;
        }
    }

    internal void ProcessDatagram(byte[] datagram, IPEndPoint source)
    {
        TftpPacketCodec codec = new TftpPacketCodec();
        TftpPacket packet = codec.Decode(
            datagram,
            TftpProtocolValues.MaximumBlockSize);
        FaultProxyDirection direction;
        IPEndPoint destination;
        if (ClientEndPoint == null || EndPointsEqual(source, ClientEndPoint))
        {
            direction = FaultProxyDirection.ClientToServer;
            if (ClientEndPoint == null)
            {
                ClientEndPoint = CopyEndpoint(source);
            }

            if (packet.Opcode == TftpOpcode.ReadRequest ||
                packet.Opcode == TftpOpcode.WriteRequest)
            {
                destination = ServerListenerEndPoint;
            }
            else
            {
                IPEndPoint? transferEndPoint = ServerTransferEndPoint;
                if (transferEndPoint == null)
                {
                    throw new AssertionException(
                        "A client transfer packet arrived before a server TID was known.");
                }

                destination = transferEndPoint;
            }
        }
        else
        {
            direction = FaultProxyDirection.ServerToClient;
            ServerTransferEndPoint = CopyEndpoint(source);
            IPEndPoint? clientEndPoint = ClientEndPoint;
            if (clientEndPoint == null)
            {
                throw new AssertionException(
                    "A server packet arrived before the client endpoint was known.");
            }

            destination = clientEndPoint;
        }

        lock (SyncRoot)
        {
            Observations.Add(new FaultProxyObservation(
                direction,
                packet.Opcode,
                packet.BlockNumber));
        }

        FaultProxyDatagram? heldAtStart = HeldDatagram;
        FaultProxyRule? selectedRule = SelectRule(direction, packet);
        FaultProxyAction action = FaultProxyAction.Pass;
        if (selectedRule != null)
        {
            action = selectedRule.Action;
        }

        bool forwarded = ApplyAction(
            action,
            selectedRule,
            datagram,
            destination);
        if (forwarded && heldAtStart != null &&
            object.ReferenceEquals(HeldDatagram, heldAtStart))
        {
            SendDatagram(heldAtStart.Bytes, heldAtStart.Destination);
            HeldDatagram = null;
        }
    }

    internal FaultProxyRule? SelectRule(
        FaultProxyDirection direction,
        TftpPacket packet)
    {
        FaultProxyRule? selectedRule = null;
        foreach (FaultProxyRule rule in Rules)
        {
            if (rule.Matches(direction, packet))
            {
                rule.MatchCount = rule.MatchCount + 1;
                if (selectedRule == null &&
                    rule.ApplicationCount == 0 &&
                    rule.MatchCount == rule.Occurrence)
                {
                    selectedRule = rule;
                    rule.ApplicationCount = rule.ApplicationCount + 1;
                }
            }
        }

        return selectedRule;
    }

    internal bool ApplyAction(
        FaultProxyAction action,
        FaultProxyRule? rule,
        byte[] datagram,
        IPEndPoint destination)
    {
        if (action == FaultProxyAction.Drop)
        {
            return false;
        }

        if (action == FaultProxyAction.Delay ||
            action == FaultProxyAction.DelayAndDuplicate)
        {
            int delayMilliseconds = 0;
            if (rule != null)
            {
                delayMilliseconds = rule.DelayMilliseconds;
            }

            Thread.Sleep(delayMilliseconds);
        }

        SendDatagram(datagram, destination);
        if (action == FaultProxyAction.Duplicate ||
            action == FaultProxyAction.DelayAndDuplicate)
        {
            SendDatagram(datagram, destination);
        }
        else if (action == FaultProxyAction.PassAndHoldCopy)
        {
            HeldDatagram = new FaultProxyDatagram(
                datagram.ToArray(),
                CopyEndpoint(destination));
        }

        return true;
    }

    internal void SendDatagram(byte[] datagram, IPEndPoint destination)
    {
        ProxySocket.SendTo(datagram, destination);
        lock (SyncRoot)
        {
            ForwardedPacketCount = ForwardedPacketCount + 1;
        }
    }

    internal bool EndPointsEqual(IPEndPoint first, IPEndPoint second)
    {
        return first.Port == second.Port && first.Address.Equals(second.Address);
    }

    internal IPEndPoint CopyEndpoint(IPEndPoint endpoint)
    {
        return new IPEndPoint(endpoint.Address, endpoint.Port);
    }

    public void Dispose()
    {
        Cancellation.Cancel();
        ProxySocket.Dispose();
        bool stopped = RunTask.Wait(3000);
        Cancellation.Dispose();
        if (!stopped)
        {
            throw new AssertionException("The fault proxy did not stop.");
        }
    }
}
