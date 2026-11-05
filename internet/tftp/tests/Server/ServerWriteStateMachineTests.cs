using System.Net;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ServerWriteStateMachineTests
{
    [Test]
    public void ClassicWriteAcknowledgesZeroDalliesThenCommitsPendingContent()
    {
        RecordingServerStorage storage = CreateWritableStorage();
        MemoryStream content = (MemoryStream)storage.WriteResult.Content!;
        object completionToken = storage.WriteResult.CompletionToken!;
        ManualTimeSource time = new ManualTimeSource();
        TftpServerWriteStateMachine state = CreateState(
            CreateWriteRequest("upload.bin"),
            storage,
            CreateOptions(),
            time);

        TftpServerWriteResult started = state.StartWrite(CancellationToken.None);
        TftpServerWriteResult finalAck = state.ProcessWritePacket(
            CreateData(1, new byte[] { 1, 2, 3 }),
            CancellationToken.None);
        time.AdvanceMilliseconds(999);
        TftpServerWriteResult dallyWait = state.EvaluateWriteTimer(
            CancellationToken.None);
        time.AdvanceMilliseconds(1);
        TftpServerWriteResult completed = state.EvaluateWriteTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(started.Action,
                Is.EqualTo(TftpServerWriteAction.SendAcknowledgement));
            Assert.That(started.Packet!.Opcode,
                Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(started.Packet.BlockNumber, Is.Zero);
            Assert.That(finalAck.Action,
                Is.EqualTo(TftpServerWriteAction.SendAcknowledgement));
            Assert.That(finalAck.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(finalAck.IsFinalData, Is.True);
            Assert.That(finalAck.IsDallying, Is.True);
            Assert.That(dallyWait.Action, Is.EqualTo(TftpServerWriteAction.Wait));
            Assert.That(dallyWait.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(completed.Action,
                Is.EqualTo(TftpServerWriteAction.Completed));
            Assert.That(completed.StorageCommitted, Is.True);
            Assert.That(completed.BytesTransferred, Is.EqualTo(3));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(3));
            Assert.That(storage.LastOperation, Is.EqualTo("CommitWrite"));
            Assert.That(storage.CompletionToken, Is.SameAs(completionToken));
            Assert.That(content.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(content.CanWrite, Is.False);
        });
    }

    [Test]
    public void NegotiatesOackAndEnforcesAnnouncedSizeAcrossDataBlocks()
    {
        RecordingServerStorage storage = CreateWritableStorage();
        TftpPacket request = CreateWriteRequest("options.bin");
        request.Options.Add(CreateOption("blksize", "8"));
        request.Options.Add(CreateOption("timeout", "2"));
        request.Options.Add(CreateOption("tsize", "10"));
        ManualTimeSource time = new ManualTimeSource();
        TftpServerWriteStateMachine state = CreateState(
            request,
            storage,
            CreateOptions(),
            time);

        TftpServerWriteResult oack = state.StartWrite(CancellationToken.None);
        TftpServerWriteResult firstAck = state.ProcessWritePacket(
            CreateData(1, new byte[8]),
            CancellationToken.None);
        TftpServerWriteResult finalAck = state.ProcessWritePacket(
            CreateData(2, new byte[2]),
            CancellationToken.None);
        time.AdvanceMilliseconds(1000);
        TftpServerWriteResult completed = state.EvaluateWriteTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(oack.Action,
                Is.EqualTo(TftpServerWriteAction.SendOptionAcknowledgement));
            Assert.That(oack.Packet!.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "blksize", "timeout", "tsize" }));
            Assert.That(oack.Packet.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "8", "2", "10" }));
            Assert.That(oack.EffectiveBlockSize, Is.EqualTo(8));
            Assert.That(oack.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(oack.HasTransferSize, Is.True);
            Assert.That(oack.TransferSize, Is.EqualTo(10));
            Assert.That(storage.Request!.HasAnnouncedSize, Is.True);
            Assert.That(storage.Request.AnnouncedSize, Is.EqualTo(10));
            Assert.That(storage.Request.EffectiveOptions, Has.Count.EqualTo(3));
            Assert.That(firstAck.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(finalAck.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(completed.StorageCommitted, Is.True);
        });
    }

    [Test]
    public void AnnouncedSizeMismatchAbortsBeforeAcknowledgingFinalData()
    {
        RecordingServerStorage shortStorage = CreateWritableStorage();
        TftpPacket shortRequest = CreateWriteRequest("short.bin");
        shortRequest.Options.Add(CreateOption("tsize", "4"));
        TftpServerWriteStateMachine shortState = CreateState(
            shortRequest,
            shortStorage,
            CreateOptions(),
            new ManualTimeSource());
        shortState.StartWrite(CancellationToken.None);
        TftpServerWriteResult shortResult = shortState.ProcessWritePacket(
            CreateData(1, new byte[3]),
            CancellationToken.None);

        RecordingServerStorage longStorage = CreateWritableStorage();
        TftpPacket longRequest = CreateWriteRequest("long.bin");
        longRequest.Options.Add(CreateOption("tsize", "2"));
        TftpServerWriteStateMachine longState = CreateState(
            longRequest,
            longStorage,
            CreateOptions(),
            new ManualTimeSource());
        longState.StartWrite(CancellationToken.None);
        TftpServerWriteResult longResult = longState.ProcessWritePacket(
            CreateData(1, new byte[3]),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(shortResult.Action,
                Is.EqualTo(TftpServerWriteAction.SendError));
            Assert.That(shortResult.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(shortResult.FailureMessage, Does.Contain("announced"));
            Assert.That(shortStorage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(
                ((MemoryStream)shortStorage.WriteResult.Content!).ToArray(),
                Is.Empty);
            Assert.That(longResult.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(longStorage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(
                ((MemoryStream)longStorage.WriteResult.Content!).ToArray(),
                Is.Empty);
        });
    }

    [Test]
    public void RuntimeCapacityFailureAbortsWithoutWritingOversizedData()
    {
        RecordingServerStorage storage = CreateWritableStorage();
        MemoryStream content = (MemoryStream)storage.WriteResult.Content!;
        TftpServerOptions options = CreateOptions();
        options.MaximumTransferSize = 2;
        TftpServerWriteStateMachine state = CreateState(
            CreateWriteRequest("large.bin"),
            storage,
            options,
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);

        TftpServerWriteResult result = state.ProcessWritePacket(
            CreateData(1, new byte[3]),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action,
                Is.EqualTo(TftpServerWriteAction.SendError));
            Assert.That(result.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(content.ToArray(), Is.Empty);
            Assert.That(content.CanWrite, Is.False);
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
        });
    }

    [Test]
    public void NetasciiSpansBlocksAndDuplicateFinalDataRestartsDally()
    {
        RecordingServerStorage storage = CreateWritableStorage();
        MemoryStream content = (MemoryStream)storage.WriteResult.Content!;
        TftpPacket request = CreateWriteRequest("text.txt");
        request.Mode = "NETASCII";
        request.Options.Add(CreateOption("blksize", "8"));
        ManualTimeSource time = new ManualTimeSource();
        TftpServerWriteStateMachine state = CreateState(
            request,
            storage,
            CreateOptions(),
            time);
        state.StartWrite(CancellationToken.None);
        byte[] firstPayload = [65, 65, 65, 65, 65, 65, 65, 13];
        byte[] finalPayload = [10, 66];

        state.ProcessWritePacket(
            CreateData(1, firstPayload),
            CancellationToken.None);
        TftpServerWriteResult finalAck = state.ProcessWritePacket(
            CreateData(2, finalPayload),
            CancellationToken.None);
        time.AdvanceMilliseconds(500);
        TftpServerWriteResult duplicateAck = state.ProcessWritePacket(
            CreateData(2, finalPayload),
            CancellationToken.None);
        time.AdvanceMilliseconds(500);
        TftpServerWriteResult stillDallying = state.EvaluateWriteTimer(
            CancellationToken.None);
        time.AdvanceMilliseconds(500);
        TftpServerWriteResult completed = state.EvaluateWriteTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(finalAck.IsDallying, Is.True);
            Assert.That(duplicateAck.Action,
                Is.EqualTo(TftpServerWriteAction.ReacknowledgeData));
            Assert.That(duplicateAck.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(stillDallying.Action,
                Is.EqualTo(TftpServerWriteAction.Wait));
            Assert.That(completed.Action,
                Is.EqualTo(TftpServerWriteAction.Completed));
            Assert.That(content.ToArray(),
                Is.EqualTo(new byte[] { 65, 65, 65, 65, 65, 65, 65, 10, 66 }));
            Assert.That(completed.BytesTransferred, Is.EqualTo(9));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(10));
            Assert.That(storage.Request!.Mode,
                Is.EqualTo(TftpTransferMode.Netascii));
        });
    }

    [Test]
    public void RejectsInvalidRequestOptionAndStorageOpenFailure()
    {
        RecordingServerStorage modeStorage = CreateWritableStorage();
        TftpPacket mailRequest = CreateWriteRequest("mail.bin");
        mailRequest.Mode = "mail";
        TftpServerWriteResult modeFailure = CreateState(
            mailRequest,
            modeStorage,
            CreateOptions(),
            new ManualTimeSource()).StartWrite(CancellationToken.None);

        RecordingServerStorage optionStorage = CreateWritableStorage();
        TftpPacket invalidOption = CreateWriteRequest("option.bin");
        invalidOption.Options.Add(CreateOption("tsize", "invalid"));
        TftpServerWriteResult optionFailure = CreateState(
            invalidOption,
            optionStorage,
            CreateOptions(),
            new ManualTimeSource()).StartWrite(CancellationToken.None);

        RecordingServerStorage deniedStorage = new RecordingServerStorage();
        deniedStorage.WriteResult.ErrorCode = TftpErrorCode.AccessViolation;
        deniedStorage.WriteResult.ErrorMessage = "denied";
        TftpServerWriteResult denied = CreateState(
            CreateWriteRequest("denied.bin"),
            deniedStorage,
            CreateOptions(),
            new ManualTimeSource()).StartWrite(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(modeFailure.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(modeStorage.CallCount, Is.Zero);
            Assert.That(optionFailure.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(optionFailure.IsOptionFailure, Is.True);
            Assert.That(optionFailure.FailureOptionName, Is.EqualTo("tsize"));
            Assert.That(optionStorage.CallCount, Is.Zero);
            Assert.That(denied.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(denied.Packet.ErrorMessage, Is.EqualTo("denied"));
            Assert.That(deniedStorage.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void RetransmitsInitialResponseThenAbortsAtRetryLimit()
    {
        RecordingServerStorage storage = CreateWritableStorage();
        MemoryStream content = (MemoryStream)storage.WriteResult.Content!;
        TftpServerOptions options = CreateOptions();
        options.MaximumRetries = 1;
        ManualTimeSource time = new ManualTimeSource();
        TftpServerWriteStateMachine state = CreateState(
            CreateWriteRequest("retry.bin"),
            storage,
            options,
            time);
        TftpServerWriteResult started = state.StartWrite(CancellationToken.None);

        time.AdvanceMilliseconds(1000);
        TftpServerWriteResult retransmit = state.EvaluateWriteTimer(
            CancellationToken.None);
        time.AdvanceMilliseconds(2000);
        TftpTimeoutException timeout = Assert.Throws<TftpTimeoutException>(
            () => state.EvaluateWriteTimer(CancellationToken.None))!;

        Assert.Multiple(() =>
        {
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpServerWriteAction.RetransmitPacket));
            Assert.That(retransmit.Packet, Is.SameAs(started.Packet));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
            Assert.That(timeout.RetryLimitReached, Is.True);
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(content.CanWrite, Is.False);
        });
    }

    [Test]
    public void RolloverAndExactBlockBoundaryRequireEmptyFinalData()
    {
        RecordingServerStorage storage = CreateWritableStorage();
        ManualTimeSource time = new ManualTimeSource();
        TftpServerWriteStateMachine state = CreateState(
            CreateWriteRequest("rollover.bin"),
            storage,
            CreateOptions(),
            time);
        state.StartWrite(CancellationToken.None);
        state.Receiver!.BlockCounter.CurrentBlock = ushort.MaxValue;

        TftpServerWriteResult maximumAck = state.ProcessWritePacket(
            CreateData(ushort.MaxValue, new byte[512]),
            CancellationToken.None);
        TftpServerWriteResult wrappedAck = state.ProcessWritePacket(
            CreateData(0, []),
            CancellationToken.None);
        time.AdvanceMilliseconds(1000);
        TftpServerWriteResult completed = state.EvaluateWriteTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(maximumAck.Packet!.BlockNumber,
                Is.EqualTo(ushort.MaxValue));
            Assert.That(maximumAck.IsFinalData, Is.False);
            Assert.That(wrappedAck.Packet!.BlockNumber, Is.Zero);
            Assert.That(wrappedAck.IsFinalData, Is.True);
            Assert.That(completed.Action,
                Is.EqualTo(TftpServerWriteAction.Completed));
            Assert.That(completed.BytesTransferred, Is.EqualTo(512));
        });
    }

    [Test]
    public void CommitFailureReportsErrorAndAbortsPendingStorage()
    {
        RecordingServerStorage storage = CreateWritableStorage();
        storage.CommitResult.Success = false;
        storage.CommitResult.ErrorCode = TftpErrorCode.FileAlreadyExists;
        storage.CommitResult.ErrorMessage = "destination changed";
        ManualTimeSource time = new ManualTimeSource();
        TftpServerWriteStateMachine state = CreateState(
            CreateWriteRequest("race.bin"),
            storage,
            CreateOptions(),
            time);
        state.StartWrite(CancellationToken.None);
        state.ProcessWritePacket(CreateData(1, []), CancellationToken.None);
        time.AdvanceMilliseconds(1000);

        TftpServerWriteResult failure = state.EvaluateWriteTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(failure.Action,
                Is.EqualTo(TftpServerWriteAction.SendError));
            Assert.That(failure.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.FileAlreadyExists));
            Assert.That(failure.Packet.ErrorMessage,
                Is.EqualTo("destination changed"));
            Assert.That(failure.StorageCommitted, Is.False);
            Assert.That(storage.LastOperation, Is.EqualTo("AbortWrite"));
        });
    }

    [Test]
    public void RemoteErrorAndCancellationAbortPendingStorage()
    {
        RecordingServerStorage remoteStorage = CreateWritableStorage();
        TftpServerWriteStateMachine remoteState = CreateState(
            CreateWriteRequest("remote.bin"),
            remoteStorage,
            CreateOptions(),
            new ManualTimeSource());
        remoteState.StartWrite(CancellationToken.None);
        TftpPacket remoteError = new TftpPacket();
        remoteError.Opcode = TftpOpcode.Error;
        remoteError.ErrorCode = TftpErrorCode.AccessViolation;
        remoteError.ErrorMessage = "stop";
        TftpRemoteErrorException remoteException =
            Assert.Throws<TftpRemoteErrorException>(() =>
                remoteState.ProcessWritePacket(
                    remoteError,
                    CancellationToken.None))!;

        RecordingServerStorage cancelledStorage = CreateWritableStorage();
        TftpServerWriteStateMachine cancelledState = CreateState(
            CreateWriteRequest("cancel.bin"),
            cancelledStorage,
            CreateOptions(),
            new ManualTimeSource());
        cancelledState.StartWrite(CancellationToken.None);
        using CancellationTokenSource cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            cancelledState.EvaluateWriteTimer(cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(remoteException.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(remoteStorage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(cancelledStorage.LastOperation, Is.EqualTo("AbortWrite"));
            Assert.That(((MemoryStream)remoteStorage.WriteResult.Content!).CanWrite,
                Is.False);
            Assert.That(((MemoryStream)cancelledStorage.WriteResult.Content!).CanWrite,
                Is.False);
        });
    }

    private static TftpServerWriteStateMachine CreateState(
        TftpPacket request,
        RecordingServerStorage storage,
        TftpServerOptions options,
        ManualTimeSource timeSource)
    {
        return new TftpServerWriteStateMachine(
            request,
            new IPEndPoint(IPAddress.Loopback, 47000),
            storage,
            options,
            timeSource);
    }

    private static TftpPacket CreateWriteRequest(string filename)
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.WriteRequest;
        request.Filename = filename;
        request.Mode = TftpProtocolValues.OctetMode;
        return request;
    }

    private static TftpPacket CreateData(ushort blockNumber, byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = blockNumber;
        packet.Payload = payload;
        return packet;
    }

    private static TftpOption CreateOption(string name, string value)
    {
        TftpOption option = new TftpOption();
        option.Name = name;
        option.Value = value;
        return option;
    }

    private static TftpServerOptions CreateOptions()
    {
        TftpServerOptions options = new TftpServerOptions();
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 4;
        options.MaximumRetries = 2;
        options.MaximumSessionDurationSeconds = 30;
        options.DallyDurationSeconds = 1;
        return options;
    }

    private static RecordingServerStorage CreateWritableStorage()
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
