using System.IO;
using System.Net;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ClientWriteStateMachineTests
{
    [TestCase(TftpTransferMode.Octet, "octet")]
    [TestCase(TftpTransferMode.Netascii, "netascii")]
    public void StartsWrqAtOriginalServerEndpoint(
        TftpTransferMode mode,
        string expectedMode)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.Mode = mode;
        TftpClientWriteStateMachine state = CreateState(
            options,
            new MemoryStream(new byte[] { 1, 2 }),
            new ManualTimeSource());

        TftpClientWriteResult result = state.StartWrite(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientWriteAction.SendRequest));
            Assert.That(result.Packet!.Opcode, Is.EqualTo(TftpOpcode.WriteRequest));
            Assert.That(result.Packet.Filename, Is.EqualTo("remote.bin"));
            Assert.That(result.Packet.Mode, Is.EqualTo(expectedMode));
            Assert.That(result.Packet.Options, Is.Empty);
            Assert.That(result.TargetEndPoint, Is.EqualTo(ServerEndpoint()));
            Assert.That(state.PeerTracker.IsTransferPortLocked, Is.False);
            Assert.That(result.HasSourceSize, Is.True);
            Assert.That(result.SourceSize, Is.EqualTo(2));
        });
    }

    [Test]
    public void BuildsCombinedWrqOptionsUsingRemainingOctetSize()
    {
        TftpTransferOptions options = AllRequestedOptions();
        MemoryStream source = new MemoryStream(new byte[] { 0, 1, 2, 3 });
        source.Position = 1;
        TftpClientWriteStateMachine state = CreateState(
            options,
            source,
            new ManualTimeSource());

        TftpClientWriteResult result = state.StartWrite(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Packet!.Options.Count, Is.EqualTo(3));
            Assert.That(result.Packet.Options[0].Name, Is.EqualTo("blksize"));
            Assert.That(result.Packet.Options[0].Value, Is.EqualTo("1024"));
            Assert.That(result.Packet.Options[1].Name, Is.EqualTo("timeout"));
            Assert.That(result.Packet.Options[1].Value, Is.EqualTo("3"));
            Assert.That(result.Packet.Options[2].Name, Is.EqualTo("tsize"));
            Assert.That(result.Packet.Options[2].Value, Is.EqualTo("3"));
            Assert.That(result.HasEncodedSourceSize, Is.True);
            Assert.That(result.EncodedSourceSize, Is.EqualTo(3));
            Assert.That(source.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void CalculatesNetasciiTransferSizeAndRestoresSeekableSource()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.Mode = TftpTransferMode.Netascii;
        options.RequestTransferSize = true;
        MemoryStream source = new MemoryStream(new byte[] { 0, 10, 13, 65 });
        source.Position = 1;
        TftpClientWriteStateMachine state = CreateState(
            options,
            source,
            new ManualTimeSource());

        TftpClientWriteResult result = state.StartWrite(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.SourceSize, Is.EqualTo(3));
            Assert.That(result.EncodedSourceSize, Is.EqualTo(5));
            Assert.That(result.Packet!.Options, Has.Count.EqualTo(1));
            Assert.That(result.Packet.Options[0].Value, Is.EqualTo("5"));
            Assert.That(source.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public void UsesExplicitOctetSizeForNonSeekableSource()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTransferSize = true;
        TftpWriteRequest request = RequestWith(options);
        request.HasKnownSourceSize = true;
        request.KnownSourceSize = 3;
        TftpClientWriteStateMachine state = CreateStateWithRequest(
            request,
            new NonSeekableReadStream(new byte[] { 1, 2, 3 }),
            new ManualTimeSource());

        TftpClientWriteResult result = state.StartWrite(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Packet!.Options, Has.Count.EqualTo(1));
            Assert.That(result.Packet.Options[0].Name, Is.EqualTo("tsize"));
            Assert.That(result.Packet.Options[0].Value, Is.EqualTo("3"));
            Assert.That(result.HasSourceSize, Is.True);
            Assert.That(result.HasEncodedSourceSize, Is.True);
        });
    }

    [TestCase(TftpTransferMode.Octet, false)]
    [TestCase(TftpTransferMode.Netascii, true)]
    public void OmitsUnknownEncodedTransferSize(
        TftpTransferMode mode,
        bool supplyRawSize)
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.Mode = mode;
        options.RequestTransferSize = true;
        TftpWriteRequest request = RequestWith(options);
        request.HasKnownSourceSize = supplyRawSize;
        request.KnownSourceSize = 2;
        TftpClientWriteStateMachine state = CreateStateWithRequest(
            request,
            new NonSeekableReadStream(new byte[] { 10, 65 }),
            new ManualTimeSource());

        TftpClientWriteResult result = state.StartWrite(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Packet!.Options, Is.Empty);
            Assert.That(result.HasEncodedSourceSize, Is.False);
            Assert.That(result.HasSourceSize, Is.EqualTo(supplyRawSize));
        });
    }

    [Test]
    public void RejectsContradictorySeekableSizeBeforeRequestEmission()
    {
        TftpWriteRequest request = RequestWith(new TftpTransferOptions());
        request.HasKnownSourceSize = true;
        request.KnownSourceSize = 2;
        TftpClientWriteStateMachine state = CreateStateWithRequest(
            request,
            new MemoryStream(new byte[] { 1, 2, 3 }),
            new ManualTimeSource());

        Assert.Throws<TftpProtocolException>(() =>
            state.StartWrite(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(state.Started, Is.False);
            Assert.That(state.Failed, Is.True);
            Assert.That(state.PendingRequestPacket, Is.Null);
        });
    }

    [TestCase(2)]
    [TestCase(4)]
    public void RejectsNonSeekableContentThatContradictsDeclaredSize(
        long declaredSize)
    {
        TftpWriteRequest request = RequestWith(new TftpTransferOptions());
        request.HasKnownSourceSize = true;
        request.KnownSourceSize = declaredSize;
        TftpClientWriteStateMachine state = CreateStateWithRequest(
            request,
            new NonSeekableReadStream(new byte[] { 1, 2, 3 }),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);

        Assert.Throws<TftpProtocolException>(() =>
            state.ProcessWritePacket(
                CreateAck(0),
                TransferEndpoint(41000),
                CancellationToken.None));

        Assert.That(state.Failed, Is.True);
    }

    [Test]
    public void TimeoutRetransmitsSameWrqToOriginalEndpoint()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpClientWriteStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            timeSource);
        TftpClientWriteResult started = state.StartWrite(CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpClientWriteResult retransmit = state.EvaluateWriteTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpClientWriteAction.SendRequest));
            Assert.That(retransmit.Packet, Is.SameAs(started.Packet));
            Assert.That(retransmit.TargetEndPoint, Is.EqualTo(ServerEndpoint()));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void IgnoresUnexpectedAddressWithoutAdoptingItsTid()
    {
        TftpClientWriteStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);

        TftpClientWriteResult result = state.ProcessWritePacket(
            CreateAck(0),
            new IPEndPoint(IPAddress.Parse("127.0.0.2"), 41000),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action,
                Is.EqualTo(TftpClientWriteAction.IgnoreUnexpectedPeer));
            Assert.That(state.PeerTracker.IsTransferPortLocked, Is.False);
            Assert.That(state.EnteredSender, Is.False);
        });
    }

    [TestCase(TftpOpcode.Data, 0)]
    [TestCase(TftpOpcode.Acknowledgement, 1)]
    public void InvalidInitialPacketReturnsErrorFour(
        TftpOpcode opcode,
        int blockNumber)
    {
        TftpClientWriteStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);
        TftpPacket packet = new TftpPacket();
        packet.Opcode = opcode;
        packet.BlockNumber = (ushort)blockNumber;
        packet.Payload = [];
        IPEndPoint transferEndpoint = TransferEndpoint(41000);

        TftpClientWriteResult result = state.ProcessWritePacket(
            packet,
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientWriteAction.SendError));
            Assert.That(result.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.IllegalOperation));
            Assert.That(result.TargetEndPoint, Is.EqualTo(transferEndpoint));
            Assert.That(result.HasFailure, Is.True);
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(41000));
            Assert.That(state.Failed, Is.True);
        });
    }

    [Test]
    public void DirectAckZeroFallsBackToDefaultsAndEntersSender()
    {
        MemoryStream source = new MemoryStream(new byte[] { 1, 2 });
        TftpTransferOptions options = AllRequestedOptions();
        options.InitialTimeoutSeconds = 2;
        options.MaximumTimeoutSeconds = 8;
        TftpClientWriteStateMachine state = CreateState(
            options,
            source,
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);

        TftpClientWriteResult result = state.ProcessWritePacket(
            CreateAck(0),
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientWriteAction.SendData));
            Assert.That(result.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(result.Packet.Payload, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(result.TargetEndPoint, Is.EqualTo(transferEndpoint));
            Assert.That(result.EnteredSender, Is.True);
            Assert.That(result.IsFinalData, Is.True);
            Assert.That(result.NegotiatedOptions, Is.Empty);
            Assert.That(result.EffectiveBlockSize,
                Is.EqualTo(TftpProtocolValues.DefaultBlockSize));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(result.HasTransferSize, Is.False);
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(41000));
        });
    }

    [Test]
    public void DirectAckFlowUsesSharedSenderThroughCompletion()
    {
        byte[] content = new byte[TftpProtocolValues.DefaultBlockSize];
        content[0] = 7;
        TftpClientWriteStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(content),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);
        TftpClientWriteResult first = state.ProcessWritePacket(
            CreateAck(0),
            transferEndpoint,
            CancellationToken.None);
        TftpClientWriteResult final = state.ProcessWritePacket(
            CreateAck(1),
            transferEndpoint,
            CancellationToken.None);
        TftpClientWriteResult completed = state.ProcessWritePacket(
            CreateAck(2),
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet!.Payload, Has.Length.EqualTo(512));
            Assert.That(final.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(final.Packet.Payload, Is.Empty);
            Assert.That(final.IsFinalData, Is.True);
            Assert.That(completed.Action, Is.EqualTo(TftpClientWriteAction.Completed));
            Assert.That(completed.BytesTransferred, Is.EqualTo(512));
            Assert.That(state.Completed, Is.True);
        });
    }

    [Test]
    public void FullOackAdoptsTidSendsDataOneAndAppliesNegotiatedValues()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpTransferOptions options = AllRequestedOptions();
        options.InitialTimeoutSeconds = 1;
        options.MaximumTimeoutSeconds = 2;
        TftpClientWriteStateMachine state = CreateState(
            options,
            new MemoryStream(new byte[10]),
            timeSource);
        state.StartWrite(CancellationToken.None);
        timeSource.AdvanceMilliseconds(500);
        IPEndPoint transferEndpoint = TransferEndpoint(42000);

        TftpClientWriteResult result = state.ProcessWritePacket(
            CreateOack(
                CreateOption("blksize", "8"),
                CreateOption("timeout", "3"),
                CreateOption("tsize", "10")),
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientWriteAction.SendData));
            Assert.That(result.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(result.Packet.Payload, Has.Length.EqualTo(8));
            Assert.That(result.TargetEndPoint, Is.EqualTo(transferEndpoint));
            Assert.That(result.EnteredSender, Is.True);
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(8));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(3));
            Assert.That(result.HasTransferSize, Is.True);
            Assert.That(result.TransferSize, Is.EqualTo(10));
            Assert.That(result.NegotiatedOptions.Count, Is.EqualTo(3));
            Assert.That(state.RetransmissionTimer.InitialIntervalMilliseconds,
                Is.EqualTo(3000));
            Assert.That(state.RetransmissionTimer.MaximumIntervalMilliseconds,
                Is.EqualTo(3000));
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(42000));
            Assert.That(state.Sender!.LastDataPacket, Is.SameAs(result.Packet));
        });
    }

    [Test]
    public void PartialOackUsesDefaultsForOmittedOptions()
    {
        TftpTransferOptions options = AllRequestedOptions();
        options.InitialTimeoutSeconds = 2;
        options.MaximumTimeoutSeconds = 8;
        TftpClientWriteStateMachine state = CreateState(
            options,
            new MemoryStream(new byte[] { 1 }),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);

        TftpClientWriteResult result = state.ProcessWritePacket(
            CreateOack(CreateOption("BLKSIZE", "256")),
            TransferEndpoint(41000),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.EffectiveBlockSize, Is.EqualTo(256));
            Assert.That(result.EffectiveTimeoutSeconds, Is.EqualTo(2));
            Assert.That(result.HasTransferSize, Is.False);
            Assert.That(result.NegotiatedOptions.Count, Is.EqualTo(1));
            Assert.That(result.Packet!.Payload, Is.EqualTo(new byte[] { 1 }));
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
        TftpClientWriteStateMachine state = CreateState(
            options,
            new MemoryStream(),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);

        TftpClientWriteResult result = state.ProcessWritePacket(
            CreateOack(CreateOption("timeout", "3")),
            transferEndpoint,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientWriteAction.SendError));
            Assert.That(result.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(result.HasFailure, Is.True);
            Assert.That(result.IsOptionFailure, Is.True);
            Assert.That(result.FailureOptionName, Is.EqualTo("timeout"));
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(41000));
            Assert.That(state.Failed, Is.True);
        });
    }

    [Test]
    public void InexactAcknowledgedTransferSizeReturnsNegotiationError()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTransferSize = true;
        TftpClientWriteStateMachine state = CreateState(
            options,
            new MemoryStream(new byte[10]),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);

        TftpClientWriteResult result = state.ProcessWritePacket(
            CreateOack(CreateOption("tsize", "11")),
            TransferEndpoint(41000),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpClientWriteAction.SendError));
            Assert.That(result.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.OptionNegotiationFailed));
            Assert.That(result.IsOptionFailure, Is.True);
            Assert.That(result.FailureOptionValue, Is.EqualTo("11"));
        });
    }

    [Test]
    public void OptionRejectionRetriesOnceWithoutOptionsAtOriginalEndpoint()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestTimeout = true;
        options.TimeoutSeconds = 3;
        options.RetryWithoutOptions = true;
        TftpClientWriteStateMachine state = CreateState(
            options,
            new MemoryStream(new byte[] { 1 }),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);
        TftpPacket rejection = CreateError(
            TftpErrorCode.OptionNegotiationFailed,
            "options unsupported");

        TftpClientWriteResult retry = state.ProcessWritePacket(
            rejection,
            TransferEndpoint(41000),
            CancellationToken.None);
        TftpClientWriteResult response = state.ProcessWritePacket(
            CreateAck(0),
            TransferEndpoint(42000),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(retry.Action,
                Is.EqualTo(TftpClientWriteAction.SendRequest));
            Assert.That(retry.Packet!.Options, Is.Empty);
            Assert.That(retry.TargetEndPoint, Is.EqualTo(ServerEndpoint()));
            Assert.That(retry.RetriedWithoutOptions, Is.True);
            Assert.That(response.Action, Is.EqualTo(TftpClientWriteAction.SendData));
            Assert.That(response.TargetEndPoint, Is.EqualTo(TransferEndpoint(42000)));
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(42000));
        });
    }

    [Test]
    public void SecondOptionRejectionIsSurfacedAsRemoteError()
    {
        TftpTransferOptions options = new TftpTransferOptions();
        options.RequestBlockSize = true;
        options.BlockSize = 1024;
        options.RetryWithoutOptions = true;
        TftpClientWriteStateMachine state = CreateState(
            options,
            new MemoryStream(),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);
        TftpPacket rejection = CreateError(
            TftpErrorCode.IllegalOperation,
            "unsupported");
        state.ProcessWritePacket(
            rejection,
            TransferEndpoint(41000),
            CancellationToken.None);

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            state.ProcessWritePacket(
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
        TftpClientWriteStateMachine state = CreateState(
            AllRequestedOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(() =>
            state.ProcessWritePacket(
                CreateError(TftpErrorCode.AccessViolation, "denied"),
                TransferEndpoint(41000),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(exception.RemoteMessage, Is.EqualTo("denied"));
            Assert.That(state.PeerTracker.TransferPort, Is.EqualTo(41000));
            Assert.That(state.Failed, Is.True);
        });
    }

    [Test]
    public void LockedTransferRejectsWrongTidAndIgnoresWrongAddress()
    {
        TftpClientWriteStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(new byte[] { 1 }),
            new ManualTimeSource());
        state.StartWrite(CancellationToken.None);
        IPEndPoint selected = TransferEndpoint(41000);
        state.ProcessWritePacket(
            CreateAck(0),
            selected,
            CancellationToken.None);

        IPEndPoint wrongTid = TransferEndpoint(42000);
        TftpClientWriteResult rejected = state.ProcessWritePacket(
            CreateAck(1),
            wrongTid,
            CancellationToken.None);
        TftpClientWriteResult ignored = state.ProcessWritePacket(
            CreateAck(1),
            new IPEndPoint(IPAddress.Parse("127.0.0.2"), 41000),
            CancellationToken.None);
        TftpClientWriteResult completed = state.ProcessWritePacket(
            CreateAck(1),
            selected,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Action,
                Is.EqualTo(TftpClientWriteAction.RejectUnknownTransferId));
            Assert.That(rejected.Packet!.ErrorCode,
                Is.EqualTo(TftpErrorCode.UnknownTransferId));
            Assert.That(rejected.TargetEndPoint, Is.EqualTo(wrongTid));
            Assert.That(ignored.Action,
                Is.EqualTo(TftpClientWriteAction.IgnoreUnexpectedPeer));
            Assert.That(completed.Action, Is.EqualTo(TftpClientWriteAction.Completed));
        });
    }

    [Test]
    public void SenderRetransmissionAndDuplicateAckAreMapped()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpClientWriteStateMachine state = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(new byte[TftpProtocolValues.DefaultBlockSize + 1]),
            timeSource);
        state.StartWrite(CancellationToken.None);
        IPEndPoint transferEndpoint = TransferEndpoint(41000);
        TftpClientWriteResult first = state.ProcessWritePacket(
            CreateAck(0),
            transferEndpoint,
            CancellationToken.None);
        TftpClientWriteResult duplicate = state.ProcessWritePacket(
            CreateAck(0),
            transferEndpoint,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);
        TftpClientWriteResult retransmit = state.EvaluateWriteTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpClientWriteAction.IgnoreDuplicateAcknowledgement));
            Assert.That(duplicate.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpClientWriteAction.RetransmitData));
            Assert.That(retransmit.Packet, Is.SameAs(first.Packet));
            Assert.That(retransmit.TargetEndPoint, Is.EqualTo(transferEndpoint));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void InitialRetryAndTotalLimitsAreEnforced()
    {
        ManualTimeSource retryTime = new ManualTimeSource();
        TftpTransferOptions retryOptions = new TftpTransferOptions();
        retryOptions.MaximumRetries = 0;
        TftpClientWriteStateMachine retryState = CreateState(
            retryOptions,
            new MemoryStream(),
            retryTime);
        retryState.StartWrite(CancellationToken.None);
        retryTime.AdvanceMilliseconds(1000);
        TftpTimeoutException retryFailure = Assert.Throws<TftpTimeoutException>(() =>
            retryState.EvaluateWriteTimer(CancellationToken.None));

        ManualTimeSource totalTime = new ManualTimeSource();
        TftpTransferOptions totalOptions = new TftpTransferOptions();
        totalOptions.MaximumTransferDurationSeconds = 1;
        TftpClientWriteStateMachine totalState = CreateState(
            totalOptions,
            new MemoryStream(),
            totalTime);
        totalState.StartWrite(CancellationToken.None);
        totalTime.AdvanceMilliseconds(1000);
        TftpTimeoutException totalFailure = Assert.Throws<TftpTimeoutException>(() =>
            totalState.ProcessWritePacket(
                CreateAck(0),
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
    public void CancellationAndInvalidLifecycleAreTerminal()
    {
        TftpClientWriteStateMachine cancelled = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            cancelled.StartWrite(cancellation.Token));
        Assert.That(cancelled.Cancelled, Is.True);
        Assert.Throws<TftpProtocolException>(() =>
            cancelled.StartWrite(CancellationToken.None));

        TftpClientWriteStateMachine lifecycle = CreateState(
            new TftpTransferOptions(),
            new MemoryStream(),
            new ManualTimeSource());
        Assert.Throws<TftpProtocolException>(() =>
            lifecycle.EvaluateWriteTimer(CancellationToken.None));
        lifecycle.StartWrite(CancellationToken.None);
        Assert.Throws<TftpProtocolException>(() =>
            lifecycle.StartWrite(CancellationToken.None));
        Assert.Throws<TftpProtocolException>(() =>
            lifecycle.ProcessWritePacket(
                null!, TransferEndpoint(41000), CancellationToken.None));
    }

    [Test]
    public void RejectsInvalidConstructionAndSourceLimits()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpWriteRequest missingOptions = RequestWith(new TftpTransferOptions());
        missingOptions.Options = null!;
        MemoryStream closed = new MemoryStream();
        closed.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientWriteStateMachine(
                    null!, new MemoryStream(), ServerEndpoint(), timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientWriteStateMachine(
                    missingOptions, new MemoryStream(), ServerEndpoint(), timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientWriteStateMachine(
                    RequestWith(new TftpTransferOptions()),
                    null!, ServerEndpoint(), timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientWriteStateMachine(
                    RequestWith(new TftpTransferOptions()),
                    closed, ServerEndpoint(), timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientWriteStateMachine(
                    RequestWith(new TftpTransferOptions()),
                    new MemoryStream(), null!, timeSource));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpClientWriteStateMachine(
                    RequestWith(new TftpTransferOptions()),
                    new MemoryStream(), ServerEndpoint(), null!));
        });

        TftpTransferOptions invalidMode = new TftpTransferOptions();
        invalidMode.Mode = (TftpTransferMode)99;
        Assert.Throws<TftpProtocolException>(() => CreateState(
            invalidMode, new MemoryStream(), new ManualTimeSource()));
        TftpTransferOptions invalidMaximum = new TftpTransferOptions();
        invalidMaximum.MaximumTransferSize = -1;
        Assert.Throws<TftpProtocolException>(() => CreateState(
            invalidMaximum, new MemoryStream(), new ManualTimeSource()));

        TftpWriteRequest negativeSize = RequestWith(new TftpTransferOptions());
        negativeSize.HasKnownSourceSize = true;
        negativeSize.KnownSourceSize = -1;
        Assert.Throws<TftpProtocolException>(() => CreateStateWithRequest(
            negativeSize, new MemoryStream(), new ManualTimeSource()));

        TftpTransferOptions limitedOptions = new TftpTransferOptions();
        limitedOptions.MaximumTransferSize = 2;
        TftpClientWriteStateMachine limited = CreateState(
            limitedOptions,
            new MemoryStream(new byte[] { 1, 2, 3 }),
            new ManualTimeSource());
        Assert.Throws<TftpProtocolException>(() =>
            limited.StartWrite(CancellationToken.None));
    }

    private static TftpClientWriteStateMachine CreateState(
        TftpTransferOptions options,
        Stream source,
        ManualTimeSource timeSource)
    {
        return CreateStateWithRequest(RequestWith(options), source, timeSource);
    }

    private static TftpClientWriteStateMachine CreateStateWithRequest(
        TftpWriteRequest request,
        Stream source,
        ManualTimeSource timeSource)
    {
        return new TftpClientWriteStateMachine(
            request,
            source,
            ServerEndpoint(),
            timeSource);
    }

    private static TftpWriteRequest RequestWith(TftpTransferOptions options)
    {
        TftpWriteRequest request = new TftpWriteRequest();
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

    private static TftpPacket CreateAck(ushort blockNumber)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = blockNumber;
        return packet;
    }

    private static TftpPacket CreateError(
        TftpErrorCode errorCode,
        string message)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = errorCode;
        packet.ErrorMessage = message;
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

internal class NonSeekableReadStream : Stream
{
    private MemoryStream Inner;

    internal NonSeekableReadStream(byte[] content)
    {
        Inner = new MemoryStream(content);
    }

    public override bool CanRead
    {
        get { return true; }
    }

    public override bool CanSeek
    {
        get { return false; }
    }

    public override bool CanWrite
    {
        get { return false; }
    }

    public override long Length
    {
        get { throw new NotSupportedException(); }
    }

    public override long Position
    {
        get { throw new NotSupportedException(); }
        set { throw new NotSupportedException(); }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Inner.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
