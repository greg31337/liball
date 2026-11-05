using System.IO;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class ReceiverStateMachineTests
{
    [Test]
    public void StartsWaitingWithRetainedInitialPacket()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            4,
            long.MaxValue,
            timeSource,
            3,
            60,
            2);
        TftpPacket request = CreateInitialPacket(TftpOpcode.ReadRequest);

        TftpReceiverResult started = receiver.StartReceiving(
            request,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);
        TftpReceiverResult retransmit = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(started.Action, Is.EqualTo(TftpReceiverAction.Wait));
            Assert.That(started.WaitMilliseconds, Is.EqualTo(1000));
            Assert.That(receiver.RetainedPacket, Is.SameAs(request));
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpReceiverAction.RetransmitPacket));
            Assert.That(retransmit.Packet, Is.SameAs(request));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [TestCase(TftpOpcode.ReadRequest)]
    [TestCase(TftpOpcode.WriteRequest)]
    [TestCase(TftpOpcode.Acknowledgement)]
    [TestCase(TftpOpcode.OptionAcknowledgement)]
    public void AcceptsSupportedInitialRetainedPacketKinds(TftpOpcode opcode)
    {
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            4,
            long.MaxValue,
            new ManualTimeSource(),
            3,
            60,
            2);
        TftpPacket retained = CreateInitialPacket(opcode);

        TftpReceiverResult result = receiver.StartReceiving(
            retained,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpReceiverAction.Wait));
            Assert.That(receiver.RetainedPacket, Is.SameAs(retained));
        });
    }

    [Test]
    public void ReceivesFullAndFinalOctetBlocksThenCompletesAfterDally()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        MemoryStream destination = new MemoryStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            2);
        StartReceiver(receiver);

        TftpReceiverResult first = receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1, 2 }),
            CancellationToken.None);
        TftpReceiverResult final = receiver.ProcessDataPacket(
            CreateData(2, new byte[] { 3 }),
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1999);
        TftpReceiverResult dally = receiver.EvaluateReceiverTimer(
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1);
        TftpReceiverResult completed = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Action,
                Is.EqualTo(TftpReceiverAction.SendAcknowledgement));
            Assert.That(first.Packet!.Opcode, Is.EqualTo(TftpOpcode.Acknowledgement));
            Assert.That(first.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(first.IsFinalData, Is.False);
            Assert.That(first.IsDallying, Is.False);
            Assert.That(first.BytesTransferred, Is.EqualTo(2));
            Assert.That(final.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(final.IsFinalData, Is.True);
            Assert.That(final.IsDallying, Is.True);
            Assert.That(final.BytesTransferred, Is.EqualTo(3));
            Assert.That(final.EncodedBytesTransferred, Is.EqualTo(3));
            Assert.That(dally.Action, Is.EqualTo(TftpReceiverAction.Wait));
            Assert.That(dally.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(completed.Action, Is.EqualTo(TftpReceiverAction.Completed));
            Assert.That(completed.IsDallying, Is.False);
            Assert.That(receiver.RetainedPacket, Is.Null);
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(destination.CanWrite, Is.True);
        });
    }

    [Test]
    public void EmptyDataOneIsFinalAndDoesNotWriteContent()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            4,
            long.MaxValue,
            timeSource,
            3,
            60,
            0);
        StartReceiver(receiver);

        TftpReceiverResult final = receiver.ProcessDataPacket(
            CreateData(1, []),
            CancellationToken.None);
        TftpReceiverResult completed = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(final.IsFinalData, Is.True);
            Assert.That(final.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(final.BytesTransferred, Is.Zero);
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
            Assert.That(destination.Length, Is.Zero);
            Assert.That(completed.Action, Is.EqualTo(TftpReceiverAction.Completed));
        });
    }

    [Test]
    public void DuplicatePreviousDataIsReacknowledgedWithoutWritingOrTimerReset()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            2);
        StartReceiver(receiver);
        TftpReceiverResult accepted = receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1, 2 }),
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpReceiverResult duplicate = receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 9, 9 }),
            CancellationToken.None);
        TftpReceiverResult retransmit = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpReceiverAction.ReacknowledgeDuplicateData));
            Assert.That(duplicate.Packet, Is.SameAs(accepted.Packet));
            Assert.That(duplicate.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpReceiverAction.RetransmitPacket));
            Assert.That(retransmit.Packet, Is.SameAs(accepted.Packet));
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    [Test]
    public void ValidDataResetsBackoffAndReplacesRetainedPacketWithAck()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            1,
            60,
            2);
        TftpPacket initial = StartReceiver(receiver);
        timeSource.AdvanceMilliseconds(1000);
        receiver.EvaluateReceiverTimer(CancellationToken.None);

        TftpReceiverResult acknowledgement = receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1, 2 }),
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(999);
        TftpReceiverResult waiting = receiver.EvaluateReceiverTimer(
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1);
        TftpReceiverResult retransmit = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(receiver.RetainedPacket, Is.Not.SameAs(initial));
            Assert.That(receiver.RetainedPacket, Is.SameAs(acknowledgement.Packet));
            Assert.That(waiting.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpReceiverAction.RetransmitPacket));
            Assert.That(retransmit.Packet, Is.SameAs(acknowledgement.Packet));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(2));
            Assert.That(receiver.RetransmissionTimer.ConsecutiveRetransmissionCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void DecodesNetasciiAcrossDataBlocks()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        MemoryStream destination = new MemoryStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Netascii,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            1);
        StartReceiver(receiver);

        receiver.ProcessDataPacket(
            CreateData(1, new byte[] { (byte)'A', 13 }),
            CancellationToken.None);
        receiver.ProcessDataPacket(
            CreateData(2, new byte[] { 10, 13 }),
            CancellationToken.None);
        TftpReceiverResult final = receiver.ProcessDataPacket(
            CreateData(3, new byte[] { 0 }),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(),
                Is.EqualTo(new byte[] { (byte)'A', 10, 13 }));
            Assert.That(final.IsFinalData, Is.True);
            Assert.That(final.BytesTransferred, Is.EqualTo(3));
            Assert.That(final.EncodedBytesTransferred, Is.EqualTo(5));
        });
    }

    [Test]
    public void InvalidNetasciiPairFailsWithoutWritingInvalidBlock()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Netascii,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            1);
        StartReceiver(receiver);
        receiver.ProcessDataPacket(
            CreateData(1, new byte[] { (byte)'A', 13 }),
            CancellationToken.None);

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => receiver.ProcessDataPacket(
                CreateData(2, new byte[] { (byte)'X' }),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("line feed or NUL"));
            Assert.That(exception.HasPacketOffset, Is.True);
            Assert.That(exception.PacketOffset,
                Is.EqualTo(TftpProtocolValues.DataHeaderLength));
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { (byte)'A' }));
            Assert.That(receiver.Failed, Is.True);
        });
    }

    [Test]
    public void DanglingFinalNetasciiCarriageReturnFailsBeforeWriteOrAck()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Netascii,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            1);
        TftpPacket initial = StartReceiver(receiver);

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => receiver.ProcessDataPacket(
                CreateData(1, new byte[] { 13 }),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("incomplete"));
            Assert.That(destination.WriteCallCount, Is.Zero);
            Assert.That(receiver.RetainedPacket, Is.SameAs(initial));
            Assert.That(receiver.Failed, Is.True);
        });
    }

    [Test]
    public void EnforcesMaximumDecodedTransferSizeBeforeWritingBlock()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            2,
            timeSource,
            3,
            60,
            1);
        StartReceiver(receiver);
        receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1, 2 }),
            CancellationToken.None);

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => receiver.ProcessDataPacket(
                CreateData(2, new byte[] { 3 }),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("maximum transfer size"));
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(receiver.BytesTransferred, Is.EqualTo(2));
        });
    }

    [Test]
    public void RepeatedFinalDataIsReacknowledgedAndRestartsDallyOnly()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            2);
        StartReceiver(receiver);
        TftpPacket finalData = CreateData(1, new byte[] { 7 });
        TftpReceiverResult final = receiver.ProcessDataPacket(
            finalData,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1500);

        TftpReceiverResult repeated = receiver.ProcessDataPacket(
            finalData,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1999);
        TftpReceiverResult waiting = receiver.EvaluateReceiverTimer(
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1);
        TftpReceiverResult completed = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Action,
                Is.EqualTo(TftpReceiverAction.ReacknowledgeDuplicateData));
            Assert.That(repeated.Packet, Is.SameAs(final.Packet));
            Assert.That(repeated.IsDallying, Is.True);
            Assert.That(repeated.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(repeated.RetransmissionCount, Is.Zero);
            Assert.That(waiting.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(completed.Action, Is.EqualTo(TftpReceiverAction.Completed));
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 7 }));
        });
    }

    [Test]
    public void UnexpectedDataDuringDallyTerminatesReceiver()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            2);
        StartReceiver(receiver);
        receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1 }),
            CancellationToken.None);

        TftpProtocolException blockFailure = Assert.Throws<TftpProtocolException>(
            () => receiver.ProcessDataPacket(
                CreateData(2, new byte[] { 1 }),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(blockFailure.Message, Does.Contain("while dallying"));
            Assert.That(receiver.Failed, Is.True);
        });
    }

    [Test]
    public void OverallDurationStillBoundsRepeatedFinalDataDally()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            3,
            2);
        StartReceiver(receiver);
        TftpPacket finalData = CreateData(1, new byte[] { 1 });
        receiver.ProcessDataPacket(finalData, CancellationToken.None);
        timeSource.AdvanceMilliseconds(1500);
        receiver.ProcessDataPacket(finalData, CancellationToken.None);
        timeSource.AdvanceMilliseconds(1500);

        TftpTimeoutException exception = Assert.Throws<TftpTimeoutException>(
            () => receiver.EvaluateReceiverTimer(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.RetryLimitReached, Is.False);
            Assert.That(exception.ElapsedMilliseconds, Is.EqualTo(3000));
            Assert.That(receiver.Failed, Is.True);
        });
    }

    [Test]
    public void DallyWaitIsLimitedByRemainingOverallDuration()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            3,
            2);
        StartReceiver(receiver);
        timeSource.AdvanceMilliseconds(2000);
        receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1 }),
            CancellationToken.None);

        TftpReceiverResult waiting = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Action, Is.EqualTo(TftpReceiverAction.Wait));
            Assert.That(waiting.WaitMilliseconds, Is.EqualTo(1000));
            Assert.That(waiting.IsDallying, Is.True);
        });
    }

    [Test]
    public void PacketAtOverallDurationLimitFailsBeforeWriting()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            1,
            2);
        StartReceiver(receiver);
        timeSource.AdvanceMilliseconds(1000);

        TftpTimeoutException exception = Assert.Throws<TftpTimeoutException>(() =>
            receiver.ProcessDataPacket(
                CreateData(1, new byte[] { 1 }),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.RetryLimitReached, Is.False);
            Assert.That(exception.ElapsedMilliseconds, Is.EqualTo(1000));
            Assert.That(destination.WriteCallCount, Is.Zero);
            Assert.That(receiver.Failed, Is.True);
        });
    }

    [Test]
    public void DataArrivingAfterDallyExpiryCompletesWithoutAnotherAck()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            1);
        StartReceiver(receiver);
        TftpPacket finalData = CreateData(1, new byte[] { 1 });
        receiver.ProcessDataPacket(finalData, CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpReceiverResult completed = receiver.ProcessDataPacket(
            finalData,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(completed.Action, Is.EqualTo(TftpReceiverAction.Completed));
            Assert.That(completed.Packet, Is.Null);
            Assert.That(completed.DuplicatePacketCount, Is.Zero);
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
            Assert.That(receiver.RetainedPacket, Is.Null);
        });
    }

    [Test]
    public void InitialWaitEnforcesRetryLimit()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            1,
            60,
            2);
        StartReceiver(receiver);
        timeSource.AdvanceMilliseconds(1000);
        receiver.EvaluateReceiverTimer(CancellationToken.None);
        timeSource.AdvanceMilliseconds(2000);

        TftpTimeoutException exception = Assert.Throws<TftpTimeoutException>(
            () => receiver.EvaluateReceiverTimer(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.RetryLimitReached, Is.True);
            Assert.That(exception.RetransmissionCount, Is.EqualTo(1));
            Assert.That(receiver.Failed, Is.True);
        });
    }

    [Test]
    public void RemoteErrorIsSurfacedImmediately()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            long.MaxValue,
            timeSource,
            3,
            60,
            2);
        StartReceiver(receiver);
        TftpPacket error = new TftpPacket();
        error.Opcode = TftpOpcode.Error;
        error.ErrorCode = TftpErrorCode.AccessViolation;
        error.ErrorMessage = "denied";

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(
            () => receiver.ProcessDataPacket(error, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(TftpErrorCode.AccessViolation));
            Assert.That(exception.RemoteMessage, Is.EqualTo("denied"));
            Assert.That(receiver.Failed, Is.True);
            Assert.That(receiver.RetransmissionTimer.TotalRetransmissionCount,
                Is.Zero);
        });
    }

    [Test]
    public void RejectsUnexpectedPacketBlockAndPayload()
    {
        ManualTimeSource firstTime = new ManualTimeSource();
        TftpReceiverStateMachine wrongOpcode = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            firstTime, 3, 60, 2);
        StartReceiver(wrongOpcode);
        TftpPacket acknowledgement = CreateInitialPacket(TftpOpcode.Acknowledgement);

        Assert.Throws<TftpProtocolException>(() =>
            wrongOpcode.ProcessDataPacket(acknowledgement, CancellationToken.None));

        TftpReceiverStateMachine wrongBlock = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            new ManualTimeSource(), 3, 60, 2);
        StartReceiver(wrongBlock);
        TftpProtocolException blockFailure = Assert.Throws<TftpProtocolException>(() =>
            wrongBlock.ProcessDataPacket(
                CreateData(2, new byte[] { 1 }), CancellationToken.None));

        TftpReceiverStateMachine oversized = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            new ManualTimeSource(), 3, 60, 2);
        StartReceiver(oversized);
        TftpProtocolException sizeFailure = Assert.Throws<TftpProtocolException>(() =>
            oversized.ProcessDataPacket(
                CreateData(1, new byte[] { 1, 2, 3 }), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(wrongOpcode.Failed, Is.True);
            Assert.That(blockFailure.Message, Does.Contain("unexpected DATA"));
            Assert.That(sizeFailure.Message, Does.Contain("active block size"));
            Assert.Throws<TftpProtocolException>(() =>
                wrongBlock.EvaluateReceiverTimer(CancellationToken.None));
        });
    }

    [Test]
    public void RejectsNullPacketAndNullDataPayload()
    {
        TftpReceiverStateMachine nullPacket = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            new ManualTimeSource(), 3, 60, 2);
        StartReceiver(nullPacket);
        Assert.Throws<TftpProtocolException>(() =>
            nullPacket.ProcessDataPacket(null!, CancellationToken.None));

        TftpReceiverStateMachine nullPayload = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            new ManualTimeSource(), 3, 60, 2);
        StartReceiver(nullPayload);
        TftpPacket data = CreateData(1, []);
        data.Payload = null!;

        Assert.Throws<TftpProtocolException>(() =>
            nullPayload.ProcessDataPacket(data, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(nullPacket.Failed, Is.True);
            Assert.That(nullPayload.Failed, Is.True);
        });
    }

    [Test]
    public void AdvancesExpectedBlockAcrossRollover()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            1,
            long.MaxValue,
            timeSource,
            3,
            60,
            1);
        receiver.BlockCounter.CurrentBlock = ushort.MaxValue;
        StartReceiver(receiver);

        TftpReceiverResult maximum = receiver.ProcessDataPacket(
            CreateData(ushort.MaxValue, new byte[] { 1 }),
            CancellationToken.None);
        TftpReceiverResult wrapped = receiver.ProcessDataPacket(
            CreateData(0, []),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Packet!.BlockNumber, Is.EqualTo(ushort.MaxValue));
            Assert.That(receiver.BlockCounter.CurrentBlock, Is.Zero);
            Assert.That(wrapped.Packet!.BlockNumber, Is.Zero);
            Assert.That(wrapped.IsFinalData, Is.True);
        });
    }

    [Test]
    public void CancellationBeforeStartIsTerminal()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            timeSource, 3, 60, 2);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            receiver.StartReceiving(
                CreateInitialPacket(TftpOpcode.ReadRequest),
                cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(receiver.Started, Is.False);
            Assert.That(receiver.Cancelled, Is.True);
            Assert.Throws<TftpProtocolException>(() =>
                receiver.StartReceiving(
                    CreateInitialPacket(TftpOpcode.ReadRequest),
                    CancellationToken.None));
        });
    }

    [Test]
    public void CancellationRequestedDuringWriteTerminatesWithoutAck()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        CancelAfterWriteStream destination = new CancelAfterWriteStream(cancellation);
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination, TftpTransferMode.Octet, 2, 10,
            timeSource, 3, 60, 2);
        TftpPacket initial = StartReceiver(receiver);

        Assert.Throws<OperationCanceledException>(() =>
            receiver.ProcessDataPacket(
                CreateData(1, new byte[] { 1 }),
                cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1 }));
            Assert.That(receiver.Cancelled, Is.True);
            Assert.That(receiver.BytesTransferred, Is.Zero);
            Assert.That(receiver.RetainedPacket, Is.SameAs(initial));
            Assert.Throws<TftpProtocolException>(() =>
                receiver.EvaluateReceiverTimer(CancellationToken.None));
        });
    }

    [Test]
    public void CancellationDuringDallyIsTerminal()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            timeSource, 3, 60, 2);
        StartReceiver(receiver);
        receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1 }),
            CancellationToken.None);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            receiver.EvaluateReceiverTimer(cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(receiver.Cancelled, Is.True);
            Assert.That(receiver.Completed, Is.False);
            Assert.Throws<TftpProtocolException>(() =>
                receiver.EvaluateReceiverTimer(CancellationToken.None));
        });
    }

    [Test]
    public void RejectsInvalidConstructionAndStartInputs()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 3, 60);
        MemoryStream closed = new MemoryStream();
        closed.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                new TftpReceiverStateMachine(
                    null!, TftpTransferMode.Octet, 1, 1, timer, 1));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpReceiverStateMachine(
                    closed, TftpTransferMode.Octet, 1, 1, timer, 1));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpReceiverStateMachine(
                    new MemoryStream(), (TftpTransferMode)99, 1, 1, timer, 1));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpReceiverStateMachine(
                    new MemoryStream(), TftpTransferMode.Octet, 0, 1, timer, 1));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpReceiverStateMachine(
                    new MemoryStream(), TftpTransferMode.Octet,
                    TftpProtocolValues.MaximumBlockSize + 1, 1, timer, 1));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpReceiverStateMachine(
                    new MemoryStream(), TftpTransferMode.Octet, 1, -1, timer, 1));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpReceiverStateMachine(
                    new MemoryStream(), TftpTransferMode.Octet, 1, 1, null!, 1));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpReceiverStateMachine(
                    new MemoryStream(), TftpTransferMode.Octet, 1, 1, timer, -1));
        });

        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            new ManualTimeSource(), 3, 60, 2);
        Assert.Throws<TftpProtocolException>(() =>
            receiver.StartReceiving(null!, CancellationToken.None));
        TftpPacket invalid = CreateInitialPacket(TftpOpcode.Data);
        Assert.Throws<TftpProtocolException>(() =>
            receiver.StartReceiving(invalid, CancellationToken.None));

        TftpReceiverStateMachine started = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            new ManualTimeSource(), 3, 60, 2);
        StartReceiver(started);
        Assert.Throws<TftpProtocolException>(() =>
            started.StartReceiving(
                CreateInitialPacket(TftpOpcode.ReadRequest),
                CancellationToken.None));
    }

    [Test]
    public void RejectsInvalidLifecycleCalls()
    {
        TftpReceiverStateMachine notStarted = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            new ManualTimeSource(), 3, 60, 0);
        Assert.Throws<TftpProtocolException>(() =>
            notStarted.EvaluateReceiverTimer(CancellationToken.None));

        TftpReceiverStateMachine completed = CreateReceiver(
            new MemoryStream(), TftpTransferMode.Octet, 2, 10,
            new ManualTimeSource(), 3, 60, 0);
        StartReceiver(completed);
        completed.ProcessDataPacket(CreateData(1, []), CancellationToken.None);
        completed.EvaluateReceiverTimer(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                completed.ProcessDataPacket(CreateData(1, []), CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                completed.EvaluateReceiverTimer(CancellationToken.None));
        });
    }

    private static TftpReceiverStateMachine CreateReceiver(
        Stream destination,
        TftpTransferMode mode,
        int blockSize,
        long maximumTransferSize,
        ManualTimeSource timeSource,
        int maximumRetries,
        int maximumDurationSeconds,
        int dallyDurationSeconds)
    {
        return new TftpReceiverStateMachine(
            destination,
            mode,
            blockSize,
            maximumTransferSize,
            CreateTimer(timeSource, maximumRetries, maximumDurationSeconds),
            dallyDurationSeconds);
    }

    private static TftpRetransmissionTimer CreateTimer(
        ManualTimeSource timeSource,
        int maximumRetries,
        int maximumDurationSeconds)
    {
        return new TftpRetransmissionTimer(
            timeSource,
            1,
            4,
            maximumRetries,
            maximumDurationSeconds,
            false,
            0);
    }

    private static TftpPacket StartReceiver(TftpReceiverStateMachine receiver)
    {
        TftpPacket initial = CreateInitialPacket(TftpOpcode.ReadRequest);
        receiver.StartReceiving(initial, CancellationToken.None);
        return initial;
    }

    private static TftpPacket CreateInitialPacket(TftpOpcode opcode)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = opcode;
        return packet;
    }

    private static TftpPacket CreateData(ushort blockNumber, byte[] payload)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Data;
        packet.BlockNumber = blockNumber;
        packet.Payload = payload;
        return packet;
    }
}

internal class CountingWriteStream : MemoryStream
{
    internal int WriteCallCount = 0;

    public override void Write(byte[] buffer, int offset, int count)
    {
        WriteCallCount = WriteCallCount + 1;
        base.Write(buffer, offset, count);
    }
}

internal class CancelAfterWriteStream : MemoryStream
{
    internal CancellationTokenSource Cancellation;

    internal CancelAfterWriteStream(CancellationTokenSource cancellation)
    {
        Cancellation = cancellation;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        base.Write(buffer, offset, count);
        Cancellation.Cancel();
    }
}
