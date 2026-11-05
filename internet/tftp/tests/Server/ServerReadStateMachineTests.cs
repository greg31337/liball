using System.Net;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ServerReadStateMachineTests
{
    [Test]
    public void OptionlessReadOpensStorageSendsDataAndCleansOwnedContent()
    {
        RecordingServerStorage storage = CreateReadableStorage(
            new byte[] { 1, 2, 3 },
            true,
            true);
        MemoryStream content = (MemoryStream)storage.ReadResult.Content!;
        TftpServerReadStateMachine state = CreateState(
            CreateReadRequest("small.bin"),
            storage,
            CreateOptions(),
            new ManualTimeSource());

        TftpServerReadResult started = state.StartRead(
            CancellationToken.None);
        TftpServerReadResult completed = state.ProcessReadPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(started.Action,
                Is.EqualTo(TftpServerReadAction.SendData));
            Assert.That(started.Packet!.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(started.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(started.Packet.Payload,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(started.IsFinalData, Is.True);
            Assert.That(completed.Action,
                Is.EqualTo(TftpServerReadAction.Completed));
            Assert.That(completed.BytesTransferred, Is.EqualTo(3));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(3));
            Assert.That(storage.CallCount, Is.EqualTo(1));
            Assert.That(storage.Request!.Filename, Is.EqualTo("small.bin"));
            Assert.That(storage.Request.Direction,
                Is.EqualTo(TftpTransferDirection.Read));
            Assert.That(storage.Request.Mode,
                Is.EqualTo(TftpTransferMode.Octet));
            Assert.That(content.CanRead, Is.False);
        });
    }

    [Test]
    public void NegotiatesOackThenWaitsForAckZeroBeforeSendingData()
    {
        byte[] bytes = Enumerable.Range(0, 20).Select(value => (byte)value).ToArray();
        RecordingServerStorage storage = CreateReadableStorage(bytes, true, true);
        TftpPacket request = CreateReadRequest("options.bin");
        request.Mode = "OCTET";
        request.Options.Add(CreateOption("BLKSIZE", "32"));
        request.Options.Add(CreateOption("timeout", "2"));
        request.Options.Add(CreateOption("tsize", "0"));
        TftpServerOptions options = CreateOptions();
        options.MaximumBlockSize = 16;
        TftpServerReadStateMachine state = CreateState(
            request,
            storage,
            options,
            new ManualTimeSource());
        options.MaximumBlockSize = 8;

        TftpServerReadResult oack = state.StartRead(CancellationToken.None);
        TftpServerReadResult firstData = state.ProcessReadPacket(
            CreateAcknowledgement(0),
            CancellationToken.None);
        TftpServerReadResult secondData = state.ProcessReadPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);
        TftpServerReadResult completed = state.ProcessReadPacket(
            CreateAcknowledgement(2),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(oack.Action,
                Is.EqualTo(TftpServerReadAction.SendOptionAcknowledgement));
            Assert.That(oack.Packet!.Opcode,
                Is.EqualTo(TftpOpcode.OptionAcknowledgement));
            Assert.That(oack.Packet.Options.Select(option => option.Name),
                Is.EqualTo(new[] { "BLKSIZE", "timeout", "tsize" }));
            Assert.That(oack.Packet.Options.Select(option => option.Value),
                Is.EqualTo(new[] { "16", "2", "20" }));
            Assert.That(oack.EffectiveBlockSize, Is.EqualTo(16));
            Assert.That(oack.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(oack.HasTransferSize, Is.True);
            Assert.That(oack.TransferSize, Is.EqualTo(20));
            Assert.That(firstData.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(firstData.Packet.Payload, Has.Length.EqualTo(16));
            Assert.That(secondData.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(secondData.Packet.Payload, Has.Length.EqualTo(4));
            Assert.That(secondData.IsFinalData, Is.True);
            Assert.That(completed.Action,
                Is.EqualTo(TftpServerReadAction.Completed));
            Assert.That(storage.Request!.EffectiveOptions, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void OmitsUnknownAndDisabledOptionsAndUsesClassicDataResponse()
    {
        RecordingServerStorage storage = CreateReadableStorage([], false, false);
        TftpPacket request = CreateReadRequest("legacy.bin");
        request.Options.Add(CreateOption("blksize", "1024"));
        request.Options.Add(CreateOption("vendor", "enabled"));
        TftpServerOptions options = CreateOptions();
        options.EnableBlockSize = false;
        TftpServerReadStateMachine state = CreateState(
            request,
            storage,
            options,
            new ManualTimeSource());

        TftpServerReadResult result = state.StartRead(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action,
                Is.EqualTo(TftpServerReadAction.SendData));
            Assert.That(result.Packet!.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(result.Packet.Payload, Is.Empty);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.NegotiatedOptions, Is.Empty);
            Assert.That(storage.Request!.RequestedOptions, Has.Count.EqualTo(2));
            Assert.That(storage.Request.EffectiveOptions, Is.Empty);
        });

        state.ProcessReadPacket(CreateAcknowledgement(1), CancellationToken.None);
        Assert.That(((MemoryStream)storage.ReadResult.Content!).CanRead, Is.True);
    }

    [Test]
    public void NetasciiReadUsesEncodedWireBytesAndStorageMode()
    {
        RecordingServerStorage storage = CreateReadableStorage(
            new byte[] { 65, 10, 66, 13 },
            true,
            false);
        TftpPacket request = CreateReadRequest("text.txt");
        request.Mode = "NeTaScIi";
        TftpServerReadStateMachine state = CreateState(
            request,
            storage,
            CreateOptions(),
            new ManualTimeSource());

        TftpServerReadResult data = state.StartRead(CancellationToken.None);
        TftpServerReadResult completed = state.ProcessReadPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(data.Packet!.Payload,
                Is.EqualTo(new byte[] { 65, 13, 10, 66, 13, 0 }));
            Assert.That(storage.Request!.Mode,
                Is.EqualTo(TftpTransferMode.Netascii));
            Assert.That(completed.BytesTransferred, Is.EqualTo(4));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(6));
        });
    }

    [TestCase("mail")]
    [TestCase("binary")]
    [TestCase("")]
    public void RejectsUnsupportedModesBeforeOpeningStorage(string mode)
    {
        RecordingServerStorage storage = CreateReadableStorage([], true, true);
        TftpPacket request = CreateReadRequest("mode.bin");
        request.Mode = mode;
        TftpServerReadStateMachine state = CreateState(
            request,
            storage,
            CreateOptions(),
            new ManualTimeSource());

        TftpServerReadResult result = state.StartRead(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action,
                Is.EqualTo(TftpServerReadAction.SendError));
            Assert.That(result.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(result.HasFailure, Is.True);
            Assert.That(storage.CallCount, Is.Zero);
        });
    }

    [Test]
    public void RejectsMalformedRequestAndOptionsBeforeOpeningStorage()
    {
        RecordingServerStorage wrongOpcodeStorage =
            CreateReadableStorage([], true, true);
        TftpPacket wrongOpcode = CreateReadRequest("wrong.bin");
        wrongOpcode.Opcode = TftpOpcode.WriteRequest;
        TftpServerReadResult wrongOpcodeResult = CreateState(
            wrongOpcode,
            wrongOpcodeStorage,
            CreateOptions(),
            new ManualTimeSource()).StartRead(CancellationToken.None);
        RecordingServerStorage longNameStorage =
            CreateReadableStorage([], true, true);
        TftpServerOptions shortNameOptions = CreateOptions();
        shortNameOptions.MaximumFilenameLength = 3;
        TftpServerReadResult longNameResult = CreateState(
            CreateReadRequest("four"),
            longNameStorage,
            shortNameOptions,
            new ManualTimeSource()).StartRead(CancellationToken.None);
        RecordingServerStorage optionStorage =
            CreateReadableStorage([], true, true);
        TftpPacket invalidOption = CreateReadRequest("option.bin");
        invalidOption.Options.Add(CreateOption("tsize", "1"));
        TftpServerReadResult optionResult = CreateState(
            invalidOption,
            optionStorage,
            CreateOptions(),
            new ManualTimeSource()).StartRead(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(wrongOpcodeResult.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(longNameResult.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(optionResult.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(optionResult.IsOptionFailure, Is.True);
            Assert.That(optionResult.FailureOptionName, Is.EqualTo("tsize"));
            Assert.That(optionResult.FailureOptionValue, Is.EqualTo("1"));
            Assert.That(wrongOpcodeStorage.CallCount, Is.Zero);
            Assert.That(longNameStorage.CallCount, Is.Zero);
            Assert.That(optionStorage.CallCount, Is.Zero);
        });
    }

    [Test]
    public void ReportsStorageFailureAndEnforcesKnownAndRuntimeSizeLimits()
    {
        RecordingServerStorage missingStorage = new RecordingServerStorage();
        missingStorage.ReadResult.ErrorCode = TftpErrorCode.FileNotFound;
        missingStorage.ReadResult.ErrorMessage = "missing";
        TftpServerReadResult missing = CreateState(
            CreateReadRequest("missing.bin"),
            missingStorage,
            CreateOptions(),
            new ManualTimeSource()).StartRead(CancellationToken.None);

        RecordingServerStorage knownStorage = CreateReadableStorage(
            new byte[6],
            true,
            true);
        MemoryStream knownContent =
            (MemoryStream)knownStorage.ReadResult.Content!;
        TftpServerOptions knownOptions = CreateOptions();
        knownOptions.MaximumTransferSize = 5;
        TftpServerReadResult knownLimit = CreateState(
            CreateReadRequest("known.bin"),
            knownStorage,
            knownOptions,
            new ManualTimeSource()).StartRead(CancellationToken.None);

        RecordingServerStorage runtimeStorage = CreateReadableStorage(
            new byte[6],
            true,
            false);
        MemoryStream runtimeContent =
            (MemoryStream)runtimeStorage.ReadResult.Content!;
        TftpServerOptions runtimeOptions = CreateOptions();
        runtimeOptions.MaximumTransferSize = 5;
        TftpServerReadResult runtimeLimit = CreateState(
            CreateReadRequest("runtime.bin"),
            runtimeStorage,
            runtimeOptions,
            new ManualTimeSource()).StartRead(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Action,
                Is.EqualTo(TftpServerReadAction.SendError));
            Assert.That(missing.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.FileNotFound));
            Assert.That(missing.Packet.ErrorMessage, Is.EqualTo("missing"));
            Assert.That(knownLimit.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(runtimeLimit.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(knownContent.CanRead, Is.False);
            Assert.That(runtimeContent.CanRead, Is.False);
        });
    }

    [Test]
    public void RetransmitsRetainedPacketAndTimesOutAtConfiguredRetryLimit()
    {
        RecordingServerStorage storage = CreateReadableStorage(
            new byte[] { 9 },
            true,
            true);
        MemoryStream content = (MemoryStream)storage.ReadResult.Content!;
        TftpServerOptions options = CreateOptions();
        options.MaximumRetries = 1;
        ManualTimeSource time = new ManualTimeSource();
        TftpServerReadStateMachine state = CreateState(
            CreateReadRequest("retry.bin"),
            storage,
            options,
            time);
        TftpServerReadResult started = state.StartRead(CancellationToken.None);

        time.AdvanceMilliseconds(1000);
        TftpServerReadResult retransmit = state.EvaluateReadTimer(
            CancellationToken.None);
        time.AdvanceMilliseconds(1000);
        TftpServerReadResult wait = state.EvaluateReadTimer(
            CancellationToken.None);
        time.AdvanceMilliseconds(1000);
        TftpTimeoutException timeout = Assert.Throws<TftpTimeoutException>(
            () => state.EvaluateReadTimer(CancellationToken.None))!;

        Assert.Multiple(() =>
        {
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpServerReadAction.RetransmitPacket));
            Assert.That(retransmit.Packet, Is.SameAs(started.Packet));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
            Assert.That(wait.Action, Is.EqualTo(TftpServerReadAction.Wait));
            Assert.That(wait.WaitMilliseconds, Is.EqualTo(1000));
            Assert.That(timeout.RetryLimitReached, Is.True);
            Assert.That(content.CanRead, Is.False);
        });
    }

    [Test]
    public void RejectsInvalidOackConfirmationAndCleansContent()
    {
        RecordingServerStorage storage = CreateReadableStorage(
            new byte[] { 1 },
            true,
            true);
        MemoryStream content = (MemoryStream)storage.ReadResult.Content!;
        TftpPacket request = CreateReadRequest("oack.bin");
        request.Options.Add(CreateOption("blksize", "8"));
        TftpServerReadStateMachine state = CreateState(
            request,
            storage,
            CreateOptions(),
            new ManualTimeSource());
        TftpServerReadResult oack = state.StartRead(CancellationToken.None);

        TftpServerReadResult failure = state.ProcessReadPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(oack.Action,
                Is.EqualTo(TftpServerReadAction.SendOptionAcknowledgement));
            Assert.That(failure.Action,
                Is.EqualTo(TftpServerReadAction.SendError));
            Assert.That(failure.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(content.CanRead, Is.False);
            Assert.Throws<TftpProtocolException>(() => state.EvaluateReadTimer(
                CancellationToken.None));
        });
    }

    [Test]
    public void RetransmitsOackAndResetsBackoffAfterAckZeroProgress()
    {
        RecordingServerStorage storage = CreateReadableStorage(
            new byte[] { 1, 2 },
            true,
            true);
        TftpPacket request = CreateReadRequest("oack-retry.bin");
        request.Options.Add(CreateOption("timeout", "2"));
        ManualTimeSource time = new ManualTimeSource();
        TftpServerReadStateMachine state = CreateState(
            request,
            storage,
            CreateOptions(),
            time);
        TftpServerReadResult oack = state.StartRead(CancellationToken.None);

        time.AdvanceMilliseconds(2000);
        TftpServerReadResult repeatedOack = state.EvaluateReadTimer(
            CancellationToken.None);
        TftpServerReadResult data = state.ProcessReadPacket(
            CreateAcknowledgement(0),
            CancellationToken.None);
        time.AdvanceMilliseconds(2000);
        TftpServerReadResult repeatedData = state.EvaluateReadTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(repeatedOack.Action,
                Is.EqualTo(TftpServerReadAction.RetransmitPacket));
            Assert.That(repeatedOack.Packet, Is.SameAs(oack.Packet));
            Assert.That(data.Action, Is.EqualTo(TftpServerReadAction.SendData));
            Assert.That(repeatedData.Action,
                Is.EqualTo(TftpServerReadAction.RetransmitPacket));
            Assert.That(repeatedData.Packet, Is.SameAs(data.Packet));
            Assert.That(state.RetransmissionTimer!.ConsecutiveRetransmissionCount,
                Is.EqualTo(1));
            Assert.That(repeatedData.RetransmissionCount, Is.EqualTo(2));
        });

        state.ProcessReadPacket(CreateAcknowledgement(1), CancellationToken.None);
    }

    [Test]
    public void HandlesDuplicateAcknowledgementsExactBlocksAndRollover()
    {
        RecordingServerStorage storage = CreateReadableStorage(
            new byte[1024],
            true,
            true);
        TftpServerReadStateMachine state = CreateState(
            CreateReadRequest("rollover.bin"),
            storage,
            CreateOptions(),
            new ManualTimeSource());
        TftpServerReadResult first = state.StartRead(CancellationToken.None);
        TftpServerReadResult duplicate = state.ProcessReadPacket(
            CreateAcknowledgement(0),
            CancellationToken.None);
        state.Sender!.BlockCounter.CurrentBlock = ushort.MaxValue;
        state.Sender.LastDataPacket!.BlockNumber = ushort.MaxValue;

        TftpServerReadResult wrapped = state.ProcessReadPacket(
            CreateAcknowledgement(ushort.MaxValue),
            CancellationToken.None);
        TftpServerReadResult emptyFinal = state.ProcessReadPacket(
            CreateAcknowledgement(0),
            CancellationToken.None);
        TftpServerReadResult complete = state.ProcessReadPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet!.Payload, Has.Length.EqualTo(512));
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpServerReadAction.IgnoreDuplicateAcknowledgement));
            Assert.That(duplicate.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(wrapped.Packet!.BlockNumber, Is.EqualTo(0));
            Assert.That(wrapped.Packet.Payload, Has.Length.EqualTo(512));
            Assert.That(emptyFinal.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(emptyFinal.Packet.Payload, Is.Empty);
            Assert.That(emptyFinal.IsFinalData, Is.True);
            Assert.That(complete.Action,
                Is.EqualTo(TftpServerReadAction.Completed));
            Assert.That(complete.BytesTransferred, Is.EqualTo(1024));
        });
    }

    [Test]
    public void RemoteErrorAndCancellationTerminateAndCleanOwnedContent()
    {
        RecordingServerStorage remoteStorage = CreateReadableStorage(
            new byte[] { 1 },
            true,
            true);
        MemoryStream remoteContent =
            (MemoryStream)remoteStorage.ReadResult.Content!;
        TftpServerReadStateMachine remoteState = CreateState(
            CreateReadRequest("remote.bin"),
            remoteStorage,
            CreateOptions(),
            new ManualTimeSource());
        remoteState.StartRead(CancellationToken.None);
        TftpPacket remoteError = new TftpPacket();
        remoteError.Opcode = TftpOpcode.Error;
        remoteError.ErrorCode = TftpErrorCode.AccessViolation;
        remoteError.ErrorMessage = "stop";

        TftpRemoteErrorException remoteException =
            Assert.Throws<TftpRemoteErrorException>(() =>
                remoteState.ProcessReadPacket(
                    remoteError,
                    CancellationToken.None))!;

        RecordingServerStorage cancelledStorage = CreateReadableStorage(
            new byte[] { 2 },
            true,
            true);
        MemoryStream cancelledContent =
            (MemoryStream)cancelledStorage.ReadResult.Content!;
        TftpServerReadStateMachine cancelledState = CreateState(
            CreateReadRequest("cancelled.bin"),
            cancelledStorage,
            CreateOptions(),
            new ManualTimeSource());
        cancelledState.StartRead(CancellationToken.None);
        using CancellationTokenSource cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            cancelledState.EvaluateReadTimer(cancellation.Token));
        Assert.Multiple(() =>
        {
            Assert.That(remoteException.ErrorCode,
                Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(remoteContent.CanRead, Is.False);
            Assert.That(cancelledContent.CanRead, Is.False);
        });
    }

    private static TftpServerReadStateMachine CreateState(
        TftpPacket request,
        RecordingServerStorage storage,
        TftpServerOptions options,
        ManualTimeSource timeSource)
    {
        return new TftpServerReadStateMachine(
            request,
            new IPEndPoint(IPAddress.Loopback, 46000),
            storage,
            options,
            timeSource);
    }

    private static TftpPacket CreateReadRequest(string filename)
    {
        TftpPacket request = new TftpPacket();
        request.Opcode = TftpOpcode.ReadRequest;
        request.Filename = filename;
        request.Mode = TftpProtocolValues.OctetMode;
        return request;
    }

    private static TftpPacket CreateAcknowledgement(ushort blockNumber)
    {
        TftpPacket acknowledgement = new TftpPacket();
        acknowledgement.Opcode = TftpOpcode.Acknowledgement;
        acknowledgement.BlockNumber = blockNumber;
        return acknowledgement;
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
        return options;
    }

    private static RecordingServerStorage CreateReadableStorage(
        byte[] content,
        bool ownsContent,
        bool hasKnownSize)
    {
        RecordingServerStorage storage = new RecordingServerStorage();
        storage.ReadResult.Success = true;
        storage.ReadResult.Content = new MemoryStream(content);
        storage.ReadResult.OwnsContent = ownsContent;
        storage.ReadResult.HasKnownSize = hasKnownSize;
        storage.ReadResult.KnownSize = content.Length;
        return storage;
    }
}
