using System.Net;
using System.Net.Sockets;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ServerSessionManagerTests
{
    [Test]
    public void CreatesAnIsolatedDynamicSocketForEveryConcurrentSession()
    {
        TftpServerListenerLifecycle lifecycle = CreateBoundLifecycle(2);
        TftpSessionManager manager = new TftpSessionManager(lifecycle, null);
        ControlledSessionHandler firstHandler =
            new ControlledSessionHandler([]);
        ControlledSessionHandler secondHandler =
            new ControlledSessionHandler([]);

        TftpServerSessionStartResult first = manager.StartSession(
            new IPEndPoint(IPAddress.Loopback, 42001),
            IPAddress.Loopback,
            CreateRequest("first.bin"),
            firstHandler,
            CancellationToken.None);
        TftpServerSessionStartResult second = manager.StartSession(
            new IPEndPoint(IPAddress.Loopback, 42002),
            IPAddress.Loopback,
            CreateRequest("second.bin"),
            secondHandler,
            CancellationToken.None);
        Assert.That(firstHandler.Started.Wait(1000), Is.True);
        Assert.That(secondHandler.Started.Wait(1000), Is.True);
        int listenerPort = lifecycle.GetStatusSnapshot().BoundEndpoints[0].Port;

        Assert.Multiple(() =>
        {
            Assert.That(first.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Accepted));
            Assert.That(second.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Accepted));
            Assert.That(first.Session, Is.Not.Null);
            Assert.That(second.Session, Is.Not.Null);
            Assert.That(first.Session!.TransferSocket.LocalEndPoint.Port,
                Is.GreaterThan(0));
            Assert.That(first.Session.TransferSocket.LocalEndPoint.Port,
                Is.Not.EqualTo(listenerPort));
            Assert.That(second.Session!.TransferSocket.LocalEndPoint.Port,
                Is.Not.EqualTo(listenerPort));
            Assert.That(second.Session.TransferSocket.LocalEndPoint.Port,
                Is.Not.EqualTo(first.Session.TransferSocket.LocalEndPoint.Port));
            Assert.That(manager.GetActiveSessionCount(), Is.EqualTo(2));
        });

        manager.StopSessions(TftpServerStopMode.Graceful);
        TftpServerStatus drainingStatus = lifecycle.GetStatusSnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(drainingStatus.IsRunning, Is.True);
            Assert.That(drainingStatus.IsDraining, Is.True);
            Assert.That(drainingStatus.ActiveTransferCount, Is.EqualTo(2));
            Assert.That(first.Session!.Cancellation.IsCancellationRequested,
                Is.False);
            Assert.That(second.Session!.Cancellation.IsCancellationRequested,
                Is.False);
        });

        firstHandler.Release.Set();
        secondHandler.Release.Set();
        Assert.That(SpinWait.SpinUntil(
            () => first.Session!.IsExecutionComplete() &&
                second.Session!.IsExecutionComplete(),
            1000),
            Is.True);
        Assert.That(manager.ProcessCompletedSessions(), Is.EqualTo(2));
        TftpServerStatus status = lifecycle.GetStatusSnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(manager.GetActiveSessionCount(), Is.Zero);
            Assert.That(first.Session!.ResourcesClosed, Is.True);
            Assert.That(second.Session!.ResourcesClosed, Is.True);
            Assert.That(status.ActiveTransferCount, Is.Zero);
            Assert.That(status.AcceptedTransferCount, Is.EqualTo(2));
            Assert.That(status.CompletedTransferCount, Is.EqualTo(2));
            Assert.That(status.IsRunning, Is.False);
        });
    }

    [Test]
    public void DuplicateInitialRequestResendsFromTheExistingTransferSocket()
    {
        TftpServerListenerLifecycle lifecycle = CreateBoundLifecycle(2);
        RecordingServerObserver observer = new RecordingServerObserver();
        TftpSessionManager manager = new TftpSessionManager(lifecycle, observer);
        byte[] response = [0, 3, 0, 1, 42];
        ControlledSessionHandler handler =
            new ControlledSessionHandler(response);
        using Socket client = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp);
        client.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        client.ReceiveTimeout = 1000;
        IPEndPoint clientEndPoint = (IPEndPoint)client.LocalEndPoint!;
        TftpPacket request = CreateRequest("duplicate.bin");
        TftpOption option = new TftpOption();
        option.Name = "BLKSIZE";
        option.Value = "1024";
        request.Options.Add(option);

        TftpServerSessionStartResult accepted = manager.StartSession(
            clientEndPoint,
            IPAddress.Loopback,
            request,
            handler,
            CancellationToken.None);
        Assert.That(handler.Started.Wait(1000), Is.True);
        EndPoint firstSource = new IPEndPoint(IPAddress.Any, 0);
        byte[] firstBuffer = new byte[32];
        int firstLength = client.ReceiveFrom(firstBuffer, ref firstSource);
        request.Filename = "mutated.bin";
        option.Value = "8";
        TftpPacket repeatedRequest = CreateRequest("duplicate.bin");
        repeatedRequest.Mode = "OCTET";
        TftpOption repeatedOption = new TftpOption();
        repeatedOption.Name = "blksize";
        repeatedOption.Value = "1024";
        repeatedRequest.Options.Add(repeatedOption);
        ControlledSessionHandler unusedHandler =
            new ControlledSessionHandler([]);

        TftpServerSessionStartResult duplicate = manager.StartSession(
            clientEndPoint,
            IPAddress.Loopback,
            repeatedRequest,
            unusedHandler,
            CancellationToken.None);
        EndPoint secondSource = new IPEndPoint(IPAddress.Any, 0);
        byte[] secondBuffer = new byte[32];
        int secondLength = client.ReceiveFrom(secondBuffer, ref secondSource);
        TftpServerStatus status = lifecycle.GetStatusSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Duplicate));
            Assert.That(duplicate.Session, Is.SameAs(accepted.Session));
            Assert.That(duplicate.ResponseResent, Is.True);
            Assert.That(unusedHandler.Started.IsSet, Is.False);
            Assert.That(firstLength, Is.EqualTo(response.Length));
            Assert.That(secondLength, Is.EqualTo(response.Length));
            Assert.That(firstBuffer.AsSpan(0, firstLength).ToArray(),
                Is.EqualTo(response));
            Assert.That(secondBuffer.AsSpan(0, secondLength).ToArray(),
                Is.EqualTo(response));
            Assert.That(secondSource, Is.EqualTo(firstSource));
            Assert.That(status.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(status.ActiveTransferCount, Is.EqualTo(1));
            Assert.That(observer.Events, Has.Count.EqualTo(2));
            Assert.That(observer.Events[1].DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(observer.Events[1].Message,
                Does.Contain("Duplicate"));
            Assert.That(observer.Events[0].ServerRequest!.Filename,
                Is.EqualTo("duplicate.bin"));
            Assert.That(observer.Events[0].ServerRequest!.RequestedOptions[0].Value,
                Is.EqualTo("1024"));
        });

        handler.Release.Set();
        Assert.That(SpinWait.SpinUntil(
            () => accepted.Session!.IsExecutionComplete(),
            1000),
            Is.True);
        manager.ProcessCompletedSessions();
        TftpTransferEvent completedEvent = observer.Events.Single(
            transferEvent => transferEvent.State == TftpTransferState.Completed);
        Assert.That(completedEvent.DuplicatePacketCount, Is.EqualTo(1));
        lifecycle.Stop(TftpServerStopMode.Immediate);
    }

    [Test]
    public void FaultedSessionIsReportedAndDoesNotDisturbOtherWork()
    {
        TftpServerListenerLifecycle lifecycle = CreateBoundLifecycle(2);
        RecordingServerObserver observer = new RecordingServerObserver();
        TftpSessionManager manager = new TftpSessionManager(lifecycle, observer);
        ThrowingSessionHandler throwingHandler =
            new ThrowingSessionHandler("session exploded");
        ControlledSessionHandler activeHandler =
            new ControlledSessionHandler([]);

        TftpServerSessionStartResult faulted = manager.StartSession(
            new IPEndPoint(IPAddress.Loopback, 43001),
            IPAddress.Loopback,
            CreateRequest("faulted.bin"),
            throwingHandler,
            CancellationToken.None);
        TftpServerSessionStartResult active = manager.StartSession(
            new IPEndPoint(IPAddress.Loopback, 43002),
            IPAddress.Loopback,
            CreateRequest("active.bin"),
            activeHandler,
            CancellationToken.None);
        Assert.That(activeHandler.Started.Wait(1000), Is.True);
        Assert.That(SpinWait.SpinUntil(
            () => faulted.Session!.IsExecutionComplete(),
            1000),
            Is.True);

        Assert.That(manager.ProcessCompletedSessions(), Is.EqualTo(1));
        TftpServerStatus afterFault = lifecycle.GetStatusSnapshot();
        ControlledSessionHandler replacementHandler =
            new ControlledSessionHandler([]);
        TftpServerSessionStartResult replacement = manager.StartSession(
            new IPEndPoint(IPAddress.Loopback, 43003),
            IPAddress.Loopback,
            CreateRequest("replacement.bin"),
            replacementHandler,
            CancellationToken.None);
        Assert.That(replacementHandler.Started.Wait(1000), Is.True);

        TftpTransferEvent failureEvent = observer.Events.Single(
            transferEvent => transferEvent.State == TftpTransferState.Failed);
        Assert.Multiple(() =>
        {
            Assert.That(afterFault.IsRunning, Is.True);
            Assert.That(afterFault.ActiveTransferCount, Is.EqualTo(1));
            Assert.That(afterFault.FailedTransferCount, Is.EqualTo(1));
            Assert.That(active.Session!.IsExecutionComplete(), Is.False);
            Assert.That(replacement.Action,
                Is.EqualTo(TftpSessionAdmissionAction.Accepted));
            Assert.That(failureEvent.HasError, Is.True);
            Assert.That(failureEvent.ErrorCode,
                Is.EqualTo(TftpErrorCode.Undefined));
            Assert.That(failureEvent.Message, Is.EqualTo("session exploded"));
            Assert.That(failureEvent.ServerRequest!.Filename,
                Is.EqualTo("faulted.bin"));
        });

        activeHandler.Release.Set();
        replacementHandler.Release.Set();
        Assert.That(SpinWait.SpinUntil(
            () => active.Session!.IsExecutionComplete() &&
                replacement.Session!.IsExecutionComplete(),
            1000),
            Is.True);
        Assert.That(manager.ProcessCompletedSessions(), Is.EqualTo(2));
        TftpServerStatus completedStatus = lifecycle.GetStatusSnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(completedStatus.ActiveTransferCount, Is.Zero);
            Assert.That(completedStatus.CompletedTransferCount, Is.EqualTo(2));
            Assert.That(completedStatus.FailedTransferCount, Is.EqualTo(1));
        });
        lifecycle.Stop(TftpServerStopMode.Immediate);
    }

    [Test]
    public void CompletionPublishesOutcomeMetricsAndCleansTheRegistry()
    {
        TftpServerListenerLifecycle lifecycle = CreateBoundLifecycle(1);
        RecordingServerObserver observer = new RecordingServerObserver();
        TftpSessionManager manager = new TftpSessionManager(lifecycle, observer);
        OutcomeSessionHandler handler = new OutcomeSessionHandler();

        TftpServerSessionStartResult started = manager.StartSession(
            new IPEndPoint(IPAddress.Loopback, 44001),
            IPAddress.Loopback,
            CreateWriteRequest("upload.bin"),
            handler,
            CancellationToken.None);
        Assert.That(SpinWait.SpinUntil(
            () => started.Session!.IsExecutionComplete(),
            1000),
            Is.True);

        Assert.That(manager.ProcessCompletedSessions(), Is.EqualTo(1));
        TftpTransferEvent completed = observer.Events.Single(
            transferEvent => transferEvent.State == TftpTransferState.Completed);
        TftpServerStatus status = lifecycle.GetStatusSnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(manager.GetActiveSessionCount(), Is.Zero);
            Assert.That(manager.FindSession(started.Session!.RequestIdentity),
                Is.Null);
            Assert.That(started.Session.ResourcesClosed, Is.True);
            Assert.That(status.AcceptedTransferCount, Is.EqualTo(1));
            Assert.That(status.CompletedTransferCount, Is.EqualTo(1));
            Assert.That(status.FailedTransferCount, Is.Zero);
            Assert.That(completed.Direction,
                Is.EqualTo(TftpTransferDirection.Write));
            Assert.That(completed.BytesTransferred, Is.EqualTo(1200));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(1204));
            Assert.That(completed.RetransmissionCount, Is.EqualTo(2));
            Assert.That(completed.NegotiatedOptions, Has.Count.EqualTo(1));
            Assert.That(completed.NegotiatedOptions[0].Name,
                Is.EqualTo(TftpProtocolValues.BlockSizeOption));
            Assert.That(completed.Message, Is.EqualTo("Transfer complete."));
        });
        lifecycle.Stop(TftpServerStopMode.Immediate);
    }

    [Test]
    public void ImmediateStopCancelsSessionsWithoutDoubleCountingCleanup()
    {
        TftpServerListenerLifecycle lifecycle = CreateBoundLifecycle(1);
        RecordingServerObserver observer = new RecordingServerObserver();
        TftpSessionManager manager = new TftpSessionManager(lifecycle, observer);
        CancellationSessionHandler handler =
            new CancellationSessionHandler();
        TftpServerSessionStartResult started = manager.StartSession(
            new IPEndPoint(IPAddress.Loopback, 45001),
            IPAddress.Loopback,
            CreateRequest("cancel.bin"),
            handler,
            CancellationToken.None);
        Assert.That(handler.Started.Wait(1000), Is.True);

        manager.StopSessions(TftpServerStopMode.Immediate);

        Assert.That(handler.CancellationObserved.Wait(1000), Is.True);
        Assert.That(SpinWait.SpinUntil(
            () => started.Session!.IsExecutionComplete(),
            1000),
            Is.True);
        Assert.That(manager.ProcessCompletedSessions(), Is.EqualTo(1));
        TftpServerStatus status = lifecycle.GetStatusSnapshot();
        TftpTransferEvent cancelled = observer.Events.Single(
            transferEvent => transferEvent.State == TftpTransferState.Cancelled);
        Assert.Multiple(() =>
        {
            Assert.That(status.IsRunning, Is.False);
            Assert.That(status.ActiveTransferCount, Is.Zero);
            Assert.That(status.FailedTransferCount, Is.EqualTo(1));
            Assert.That(manager.GetActiveSessionCount(), Is.Zero);
            Assert.That(started.Session!.ResourcesClosed, Is.True);
            Assert.That(cancelled.Message, Is.EqualTo("Transfer cancelled."));
        });
    }

    private static TftpServerListenerLifecycle CreateBoundLifecycle(
        int maximumSessions)
    {
        TftpServerOptions options = new TftpServerOptions();
        options.MaximumSessions = maximumSessions;
        TftpServerBinding binding = new TftpServerBinding();
        binding.Address = IPAddress.Loopback;
        binding.Port = 0;
        options.Bindings.Add(binding);
        TftpServerListenerLifecycle lifecycle =
            new TftpServerListenerLifecycle(options);
        lifecycle.BindListeners(CancellationToken.None);
        return lifecycle;
    }

    private static TftpPacket CreateRequest(string filename)
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = filename;
        request.Mode = TftpProtocolValues.OctetMode;
        return request;
    }

    private static TftpPacket CreateWriteRequest(string filename)
    {
        TftpPacket request = CreateRequest(filename);
        request.Opcode = TftpOpcode.WriteRequest;
        return request;
    }
}

internal class ControlledSessionHandler : ITftpServerSessionHandler
{
    internal byte[] Response;
    internal ManualResetEventSlim Started;
    internal ManualResetEventSlim Release;

    internal ControlledSessionHandler(byte[] response)
    {
        Response = response;
        Started = new ManualResetEventSlim(false);
        Release = new ManualResetEventSlim(false);
    }

    public TftpSessionOutcome ExecuteSession(
        TftpServerSession session,
        CancellationToken cancellationToken)
    {
        if (Response.Length > 0)
        {
            session.SendAndRetainResponse(Response, cancellationToken);
        }

        Started.Set();
        Release.Wait(cancellationToken);
        TftpSessionOutcome outcome = new TftpSessionOutcome();
        outcome.Succeeded = true;
        outcome.TerminalState = TftpTransferState.Completed;
        outcome.Message = "Transfer complete.";
        return outcome;
    }
}

internal class ThrowingSessionHandler : ITftpServerSessionHandler
{
    internal string FailureMessage;

    internal ThrowingSessionHandler(string failureMessage)
    {
        FailureMessage = failureMessage;
    }

    public TftpSessionOutcome ExecuteSession(
        TftpServerSession session,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(FailureMessage);
    }
}

internal class OutcomeSessionHandler : ITftpServerSessionHandler
{
    public TftpSessionOutcome ExecuteSession(
        TftpServerSession session,
        CancellationToken cancellationToken)
    {
        TftpSessionOutcome outcome = new TftpSessionOutcome();
        outcome.Succeeded = true;
        outcome.TerminalState = TftpTransferState.Completed;
        outcome.BytesTransferred = 1200;
        outcome.EncodedBytesTransferred = 1204;
        outcome.RetransmissionCount = 2;
        TftpOption option = new TftpOption();
        option.Name = TftpProtocolValues.BlockSizeOption;
        option.Value = "1024";
        outcome.NegotiatedOptions.Add(option);
        outcome.Message = "Transfer complete.";
        return outcome;
    }
}

internal class CancellationSessionHandler : ITftpServerSessionHandler
{
    internal ManualResetEventSlim Started;
    internal ManualResetEventSlim CancellationObserved;

    internal CancellationSessionHandler()
    {
        Started = new ManualResetEventSlim(false);
        CancellationObserved = new ManualResetEventSlim(false);
    }

    public TftpSessionOutcome ExecuteSession(
        TftpServerSession session,
        CancellationToken cancellationToken)
    {
        Started.Set();
        while (!cancellationToken.IsCancellationRequested)
        {
            Thread.Sleep(1);
        }

        CancellationObserved.Set();
        TftpSessionOutcome outcome = new TftpSessionOutcome();
        outcome.TerminalState = TftpTransferState.Cancelled;
        outcome.Message = "Transfer cancelled.";
        return outcome;
    }
}

internal class RecordingServerObserver : ITftpTransferObserver
{
    internal List<TftpTransferEvent> Events;

    internal RecordingServerObserver()
    {
        Events = [];
    }

    public void Observe(TftpTransferEvent transferEvent)
    {
        Events.Add(transferEvent);
    }
}
