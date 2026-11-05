using System.IO;
using System.Net;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ClientReadStateMachineTests
{
    [TestCase(TftpTransferMode.Octet, "octet")]
    [TestCase(TftpTransferMode.Netascii, "netascii")]
    public void StartsRrqAtOriginalServerEndpoint(
        TftpTransferMode mode,
        string expectedMode)
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpTransferOptions options = new TftpTransferOptions();
        options.Mode = mode;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            timeSource);

        TftpClientReadResult result = state.StartRead(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientReadAction.SendRequest));
            Assert.That(result.Packet!.Opcode, Is.EqualTo(TftpOpcode.ReadRequest));
            Assert.That(result.Packet.Filename, Is.EqualTo("remote.bin"));
            Assert.That(result.Packet.Mode, Is.EqualTo(expectedMode));
            Assert.That(result.Packet.Options, Is.Empty);
            Assert.That(result.TargetEndPoint, Is.EqualTo(ServerEndpoint()));
            Assert.That(state.PeerTracker.IsTransferPortLocked, Is.False);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuildsCombinedRrqOptionsInProtocolOrder()
    {
        TftpTransferOptions options = AllRequestedOptions();
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            new ManualTimeSource());

        TftpClientReadResult result = state.StartRead(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Packet!.Options.Count, Is.EqualTo(3));
            Assert.That(result.Packet.Options[0].Name, Is.EqualTo("blksize"));
            Assert.That(result.Packet.Options[0].Value, Is.EqualTo("1024"));
            Assert.That(result.Packet.Options[1].Name, Is.EqualTo("timeout"));
            Assert.That(result.Packet.Options[1].Value, Is.EqualTo("3"));
            Assert.That(result.Packet.Options[2].Name, Is.EqualTo("tsize"));
            Assert.That(result.Packet.Options[2].Value, Is.EqualTo("0"));
        });
    }

    [Test]
    public void TimeoutRetransmitsSameRrqToOriginalEndpoint()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpClientReadStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            timeSource);
        TftpClientReadResult started = state.StartRead(CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpClientReadResult retransmit = state.EvaluateReadTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(retransmit.Action, Is.EqualTo(TftpClientReadAction.SendRequest));
            Assert.That(retransmit.Packet, Is.SameAs(started.Packet));
            Assert.That(retransmit.TargetEndPoint, Is.EqualTo(ServerEndpoint()));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void IgnoresUnexpectedAddressWithoutAdoptingItsTid()
    {
        TftpClientReadStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        state.StartRead(CancellationToken.None);
        IPEndPoint unexpected = new IPEndPoint(
            IPAddress.Parse("127.0.0.2"),
            41000);

        TftpClientReadResult result = state.ProcessReadPacket(
            CreateData(1, new byte[] { 1 }),
            unexpected,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action,
                Is.EqualTo(TftpClientReadAction.IgnoreUnexpectedPeer));
            Assert.That(result.Packet, Is.Null);
            Assert.That(state.PeerTracker.IsTransferPortLocked, Is.False);
            Assert.That(state.EnteredReceiver, Is.False);
        });
    }

    [Test]
    public void InvalidInitialPacketLocksCandidateAndReturnsErrorFour()
    {
        TftpClientReadStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        state.StartRead(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);
        TftpPacket acknowledgement = new TftpPacket();
        acknowledgement.Opcode = TftpOpcode.Acknowledgement;
        acknowledgement.BlockNumber = 0;

        TftpClientReadResult result = state.ProcessReadPacket(
            acknowledgement,
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientReadAction.SendError));
            Assert.That(result.Packet!.Opcode, Is.EqualTo(TftpOpcode.Error));
            Assert.That(result.Packet.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(result.TargetEndPoint, Is.EqualTo(transferEndpoint));
            Assert.That(result.HasFailure, Is.True);
            Assert.That(result.IsOptionFailure, Is.False);
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(41000));
            Assert.That(state.Failed, Is.True);
        });
    }

    [Test]
    public void DirectDataOneFallsBackToDefaultsAndEntersReceiver()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        MemoryStream destination = new MemoryStream();
        TftpTransferOptions options = AllRequestedOptions();
        options.InitialTimeoutSeconds = 2;
        options.MaximumTimeoutSeconds = 8;
        options.DallyDurationSeconds = 0;
        TftpClientReadStateMachine state = CreateState(
            options,
            destination,
            timeSource);
        state.StartRead(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);

        TftpClientReadResult acknowledgement = state.ProcessReadPacket(
            CreateData(1, new byte[] { 1, 2 }),
            transferEndpoint,
            CancellationToken.None);
        TftpClientReadResult completed = state.EvaluateReadTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(acknowledgement.Action,
                Is.EqualTo(TftpClientReadAction.SendAcknowledgement));
            Assert.That(acknowledgement.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(acknowledgement.TargetEndPoint, Is.EqualTo(transferEndpoint));
            Assert.That(acknowledgement.EnteredReceiver, Is.True);
            Assert.That(acknowledgement.IsFinalData, Is.True);
            Assert.That(acknowledgement.NegotiatedOptions, Is.Empty);
            Assert.That(acknowledgement.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(acknowledgement.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(acknowledgement.HasTransferSize, Is.False);
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(41000));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(completed.Action, Is.EqualTo(TftpClientReadAction.Completed));
        });
    }

    [Test]
    public void DirectFullDataContinuesThroughSharedReceiver()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        MemoryStream destination = new MemoryStream();
        TftpTransferOptions options = new TftpTransferOptions();
        options.DallyDurationSeconds = 0;
        TftpClientReadStateMachine state = CreateState(
            options,
            destination,
            timeSource);
        state.StartRead(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);
        byte[] fullBlock = new byte[TftpProtocolValues.DefaultBlockSize];
        fullBlock[0] = 7;

        TftpClientReadResult firstAck = state.ProcessReadPacket(
            CreateData(1, fullBlock),
            transferEndpoint,
            CancellationToken.None);
        TftpClientReadResult finalAck = state.ProcessReadPacket(
            CreateData(2, new byte[] { 8 }),
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(firstAck.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(firstAck.IsFinalData, Is.False);
            Assert.That(finalAck.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(finalAck.IsFinalData, Is.True);
            Assert.That(finalAck.BytesTransferred,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize + 1));
            Assert.That(destination.Length,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize + 1));
        });
    }

    [Test]
    public void FullOackAdoptsTidSendsAckZeroAndAppliesNegotiatedValues()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpTransferOptions options = AllRequestedOptions();
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 2;
        options.MaximumTransferSize = 1000;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            timeSource);
        state.StartRead(CancellationToken.None);
        timeSource.AdvanceMilliseconds(500);
        IPEndPoint transferEndpoint = TransferEndpoint(42000);
        TftpPacket oack = CreateOack(
            CreateOption("blksize", "512"),
            CreateOption("timeout", "3"),
            CreateOption("tsize", "900"));

        TftpClientReadResult result = state.ProcessReadPacket(
            oack,
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action,
                Is.EqualTo(TftpClientReadAction.SendAcknowledgement));
            Assert.That(result.Packet!.BlockNumber, Is.Zero);
            Assert.That(result.TargetEndPoint, Is.EqualTo(transferEndpoint));
            Assert.That(result.EnteredReceiver, Is.True);
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(512));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(3));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(900));
            Assert.That(result.NegotiatedOptions.Count, Is.EqualTo(3));
            Assert.That(state.RetransmissionTimer.InitialIntervalMilliseconds,
                Is.EqualTo(3000));
            Assert.That(state.RetransmissionTimer.MaximumIntervalMilliseconds,
                Is.EqualTo(3000));
            Assert.That(state.RetransmissionTimer.TransferStartedMilliseconds,
                Is.Zero);
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(42000));
            Assert.That(state.Receiver!.RetainedPacket, Is.SameAs(result.Packet));
        });
    }

    [Test]
    public void OackHandshakeContinuesWithDataOneInSharedReceiver()
    {
        MemoryStream destination = new MemoryStream();
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = 1024;
        TftpClientReadStateMachine state = CreateState(
            options,
            destination,
            new ManualTimeSource());
        state.StartRead(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);
        state.ProcessReadPacket(
            CreateOack(CreateOption("blksize", "8")),
            transferEndpoint,
            CancellationToken.None);

        TftpClientReadResult result = state.ProcessReadPacket(
            CreateData(1, new byte[] { 1, 2, 3 }),
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action,
                Is.EqualTo(TftpClientReadAction.SendAcknowledgement));
            Assert.That(result.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(result.IsFinalData, Is.True);
            Assert.That(result.BytesTransferred, Is.EqualTo(3));
            Assert.That(result.EncodedBytesTransferred, Is.EqualTo(3));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void PartialOackUsesDefaultsForOmittedOptions()
    {
        TftpTransferOptions options = AllRequestedOptions();
        options.InitialTimeoutSeconds = 2;
        options.MaximumTimeoutSeconds = 8;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            new ManualTimeSource());
        state.StartRead(CancellationToken.None);

        TftpClientReadResult result = state.ProcessReadPacket(
            CreateOack(CreateOption("BLKSIZE", "256")),
            TransferEndpoint(41000),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(256));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(result.HasTransferSize, Is.False);
            Assert.That(result.NegotiatedOptions.Count, Is.EqualTo(1));
            Assert.That(state.RetransmissionTimer.InitialIntervalMilliseconds,
                Is.EqualTo(2000));
        });
    }

    [Test]
    public void InvalidOackReturnsNegotiationErrorAndLocksTid()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = 1024;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            new ManualTimeSource());
        state.StartRead(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);

        TftpClientReadResult result = state.ProcessReadPacket(
            CreateOack(CreateOption("timeout", "3")),
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientReadAction.SendError));
            Assert.That(result.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(result.HasFailure, Is.True);
            Assert.That(result.IsOptionFailure, Is.True);
            Assert.That(result.FailureOptionName, Is.EqualTo("timeout"));
            Assert.That(result.FailureOptionValue, Is.EqualTo("3"));
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(41000));
            Assert.That(state.Failed, Is.True);
        });
    }

    [Test]
    public void OversizedAcknowledgedTransferSizeReturnsCapacityError()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTransferSize = true;
        options.MaximumTransferSize = 10;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            new ManualTimeSource());
        state.StartRead(CancellationToken.None);

        TftpClientReadResult result = state.ProcessReadPacket(
            CreateOack(CreateOption("tsize", "11")),
            TransferEndpoint(41000),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientReadAction.SendError));
            Assert.That(result.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(result.FailureErrorCode,
                Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(result.IsOptionFailure, Is.True);
        });
    }

    [Test]
    public void OptionRejectionRetriesOnceWithoutOptionsAtOriginalEndpoint()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTimeout = true;
        options.TimeoutSeconds = 3;
        options.RetryWithoutOptions = true;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            timeSource);
        state.StartRead(CancellationToken.None);
        timeSource.AdvanceMilliseconds(500);
        TftpPacket rejection = new TftpPacket();
        rejection.Opcode = TftpOpcode.Error;
        rejection.ErrorCode = TftpErrorCode.OptionNegotiationFailed;
        rejection.ErrorMessage = "options unsupported";

        TftpClientReadResult retry = state.ProcessReadPacket(
            rejection,
            TransferEndpoint(41000),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Action, Is.EqualTo(TftpClientReadAction.SendRequest));
            Assert.That(retry.Packet!.Options, Is.Empty);
            Assert.That(retry.Packet.Filename, Is.EqualTo("remote.bin"));
            Assert.That(retry.TargetEndPoint, Is.EqualTo(ServerEndpoint()));
            Assert.That(retry.RetriedWithoutOptions, Is.True);
            Assert.That(state.PeerTracker.IsTransferPortLocked, Is.False);
            Assert.That(state.RetransmissionTimer.IntervalStartedMilliseconds,
                Is.EqualTo(500));
        });

        TftpClientReadResult response = state.ProcessReadPacket(
            CreateData(1, new byte[] { 1 }),
            TransferEndpoint(42000),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.Action,
                Is.EqualTo(TftpClientReadAction.SendAcknowledgement));
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(42000));
            Assert.That(response.RetriedWithoutOptions, Is.True);
        });
    }

    [Test]
    public void SecondOptionRejectionIsSurfacedAsRemoteError()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = 1024;
        options.RetryWithoutOptions = true;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            new ManualTimeSource());
        state.StartRead(CancellationToken.None);
        TftpPacket rejection = new TftpPacket();
        rejection.Opcode = TftpOpcode.Error;
        rejection.ErrorCode = TftpErrorCode.IllegalOperation;
        rejection.ErrorMessage = "unsupported";
        state.ProcessReadPacket(
            rejection,
            TransferEndpoint(41000),
            CancellationToken.None);

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            state.ProcessReadPacket(
                rejection,
                TransferEndpoint(42000),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(exception.RemoteMessage, Is.EqualTo("unsupported"));
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(42000));
            Assert.That(state.Failed, Is.True);
        });
    }

    [Test]
    public void NonOptionRemoteErrorIsSurfacedImmediately()
    {
        TftpClientReadStateMachine state = CreateState(
            AllRequestedOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        state.StartRead(CancellationToken.None);
        TftpPacket error = new TftpPacket();
        error.Opcode = TftpOpcode.Error;
        error.ErrorCode = TftpErrorCode.FileNotFound;
        error.ErrorMessage = "missing";

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            state.ProcessReadPacket(
                error,
                TransferEndpoint(41000),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(TftpErrorCode.FileNotFound));
            Assert.That(exception.RemoteMessage, Is.EqualTo("missing"));
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(41000));
            Assert.That(state.Failed, Is.True);
        });
    }

    [Test]
    public void LockedTransferRejectsWrongTidAndIgnoresWrongAddress()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        MemoryStream destination = new MemoryStream();
        TftpClientReadStateMachine state = CreateState(
            new TftpTransferOptions(),
            destination,
            timeSource);
        state.StartRead(CancellationToken.None);
        IPEndPoint selected = TransferEndpoint(41000);
        byte[] fullBlock = new byte[TftpProtocolValues.DefaultBlockSize];
        state.ProcessReadPacket(
            CreateData(1, fullBlock),
            selected,
            CancellationToken.None);

        IPEndPoint wrongTid = TransferEndpoint(42000);
        TftpClientReadResult rejected = state.ProcessReadPacket(
            CreateData(2, new byte[] { 1 }),
            wrongTid,
            CancellationToken.None);
        TftpClientReadResult ignored = state.ProcessReadPacket(
            CreateData(2, new byte[] { 1 }),
            new IPEndPoint(IPAddress.Parse("127.0.0.2"), 41000),
            CancellationToken.None);
        TftpClientReadResult accepted = state.ProcessReadPacket(
            CreateData(2, new byte[] { 1 }),
            selected,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Action,
                Is.EqualTo(TftpClientReadAction.RejectUnknownTransferId));
            Assert.That(rejected.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            Assert.That(rejected.TargetEndPoint, Is.EqualTo(wrongTid));
            Assert.That(ignored.Action,
                Is.EqualTo(TftpClientReadAction.IgnoreUnexpectedPeer));
            Assert.That(accepted.Action,
                Is.EqualTo(TftpClientReadAction.SendAcknowledgement));
            Assert.That(destination.Length,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize + 1));
        });
    }

    [Test]
    public void LostAckZeroIsRetransmittedToAdoptedTid()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = 1024;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            timeSource);
        state.StartRead(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);
        TftpClientReadResult acknowledgement = state.ProcessReadPacket(
            CreateOack(CreateOption("blksize", "512")),
            transferEndpoint,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpClientReadResult retransmit = state.EvaluateReadTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpClientReadAction.RetransmitPacket));
            Assert.That(retransmit.Packet, Is.SameAs(acknowledgement.Packet));
            Assert.That(retransmit.TargetEndPoint, Is.EqualTo(transferEndpoint));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReceiverDallyAndRepeatedFinalDataAreMapped()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpTransferOptions options = new TftpTransferOptions();
        options.DallyDurationSeconds = 2;
        TftpClientReadStateMachine state = CreateState(
            options,
            new MemoryStream(),
            timeSource);
        state.StartRead(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);
        TftpPacket finalData = CreateData(1, new byte[] { 1 });
        TftpClientReadResult finalAck = state.ProcessReadPacket(
            finalData,
            transferEndpoint,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpClientReadResult repeated = state.ProcessReadPacket(
            finalData,
            transferEndpoint,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(2000);
        TftpClientReadResult completed = state.EvaluateReadTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(finalAck.IsDallying, Is.True);
            Assert.That(repeated.Action,
                Is.EqualTo(TftpClientReadAction.ReacknowledgeData));
            Assert.That(repeated.Packet, Is.SameAs(finalAck.Packet));
            Assert.That(repeated.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(completed.Action, Is.EqualTo(TftpClientReadAction.Completed));
            Assert.That(state.Completed, Is.True);
        });
    }

    [Test]
    public void InitialRetryAndTotalLimitsAreEnforced()
    {
        ManualTimeSource retryTime = new ManualTimeSource();
        TftpTransferOptions retryOptions = new TftpTransferOptions();
        retryOptions.MaximumRetries = 0;
        TftpClientReadStateMachine retryState = CreateState(
            retryOptions,
            new MemoryStream(),
            retryTime);
        retryState.StartRead(CancellationToken.None);
        retryTime.AdvanceMilliseconds(1000);
        TftpTimeoutException retryFailure = Assert.Throws<TftpTimeoutException>(() =>
            retryState.EvaluateReadTimer(CancellationToken.None));

        ManualTimeSource totalTime = new ManualTimeSource();
        TftpTransferOptions totalOptions = new TftpTransferOptions();
        totalOptions.MaximumTransferDurationSeconds = 1;
        TftpClientReadStateMachine totalState = CreateState(
            totalOptions,
            new MemoryStream(),
            totalTime);
        totalState.StartRead(CancellationToken.None);
        totalTime.AdvanceMilliseconds(1000);
        TftpTimeoutException totalFailure = Assert.Throws<TftpTimeoutException>(() =>
            totalState.ProcessReadPacket(
                CreateData(1, new byte[] { 1 }),
                TransferEndpoint(41000),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(retryFailure.RetryLimitReached, Is.True);
            Assert.That(retryState.Failed, Is.True);
            Assert.That(totalFailure.RetryLimitReached, Is.False);
            Assert.That(totalState.PeerTracker.IsTransferPortLocked, Is.False);
            Assert.That(totalState.Failed, Is.True);
        });
    }

    [Test]
    public void CancellationBeforeStartIsTerminal()
    {
        TftpClientReadStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            state.StartRead(cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(state.Cancelled, Is.True);
            Assert.That(state.Started, Is.False);
            Assert.Throws<TftpProtocolException>(() =>
                state.StartRead(CancellationToken.None));
        });
    }

    [Test]
    public void RejectsInvalidConstructionAndLifecycleInputs()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReadRequest missingOptions = RequestWith(new TftpTransferOptions());
        missingOptions.Options = null!;
        MemoryStream closed = new MemoryStream();
        closed.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientReadStateMachine(
                    null!, new MemoryStream(), ServerEndpoint(), timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientReadStateMachine(
                    missingOptions, new MemoryStream(), ServerEndpoint(), timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientReadStateMachine(
                    RequestWith(new TftpTransferOptions()),
                    null!, ServerEndpoint(), timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientReadStateMachine(
                    RequestWith(new TftpTransferOptions()),
                    closed, ServerEndpoint(), timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientReadStateMachine(
                    RequestWith(new TftpTransferOptions()),
                    new MemoryStream(), null!, timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientReadStateMachine(
                    RequestWith(new TftpTransferOptions()),
                    new MemoryStream(), ServerEndpoint(), null!));
        });

        TftpTransferOptions invalidMode = new TftpTransferOptions();
        invalidMode.Mode = (TftpTransferMode)99;
        Assert.Throws<TftpProtocolException>(() => CreateState(
            invalidMode, new MemoryStream(), new ManualTimeSource()));
        TftpTransferOptions invalidSize = new TftpTransferOptions();
        invalidSize.MaximumTransferSize = -1;
        Assert.Throws<TftpProtocolException>(() => CreateState(
            invalidSize, new MemoryStream(), new ManualTimeSource()));
        TftpTransferOptions invalidDally = new TftpTransferOptions();
        invalidDally.DallyDurationSeconds = -1;
        Assert.Throws<TftpProtocolException>(() => CreateState(
            invalidDally, new MemoryStream(), new ManualTimeSource()));
        TftpTransferOptions invalidBlock = new TftpTransferOptions();
        invalidBlock.RequestBlockSize = true;
        invalidBlock.BlockSize = 7;
        Assert.Throws<TftpOptionNegotiationException>(() => CreateState(
            invalidBlock, new MemoryStream(), new ManualTimeSource()));

        TftpClientReadStateMachine lifecycle = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        Assert.Throws<TftpProtocolException>(() =>
            lifecycle.EvaluateReadTimer(CancellationToken.None));
        lifecycle.StartRead(CancellationToken.None);
        Assert.Throws<TftpProtocolException>(() =>
            lifecycle.StartRead(CancellationToken.None));
        Assert.Throws<TftpProtocolException>(() =>
            lifecycle.ProcessReadPacket(
                null!, TransferEndpoint(41000), CancellationToken.None));
    }

    private static TftpClientReadStateMachine CreateState(
        TftpTransferOptions options,
        Stream destination,
        ManualTimeSource timeSource)
    {
        return new TftpClientReadStateMachine(
            RequestWith(options),
            destination,
            ServerEndpoint(),
            timeSource);
    }

    private static TftpReadRequest RequestWith(TftpTransferOptions options)
    {
        TftpReadRequest request = new TftpReadRequest();
        request.Endpoint.Host = "127.0.0.1";
        request.Endpoint.Port = 69;
        request.RemoteFilename = "remote.bin";
        request.Options = options;
        return request;
    }

    private static TftpTransferOptions AllRequestedOptions()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = 1024;
        options.RequestTimeout = true;
        options.TimeoutSeconds = 3;
        options.RequestTransferSize = true;
        return options;
    }

    private static IPEndPoint ServerEndpoint()
    {
        return new IPEndPoint(IPAddress.Loopback, 69);
    }

    private static IPEndPoint TransferEndpoint(int port)
    {
        return new IPEndPoint(IPAddress.Loopback, port);
    }

    private static TftpPacket CreateData(ushort blockNumber, byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = blockNumber;
        packet.Payload = payload;
        return packet;
    }

    private static TftpPacket CreateOack(
        TftpOption first,
        TftpOption second,
        TftpOption third)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        packet.Options.Add(first);
        packet.Options.Add(second);
        packet.Options.Add(third);
        return packet;
    }

    private static TftpPacket CreateOack(TftpOption option)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.OptionAcknowledgement;
        packet.Options.Add(option);
        return packet;
    }

    private static TftpOption CreateOption(string name, string value)
    {
        TftpOption option = new TftpOption();
        option.Name = name;
        option.Value = value;
        return option;
    }
}
