using System.IO;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class SharedStateMachineTransitionTests
{
    [TestCase(TftpTransferMode.Octet, 0, 4, 1)]
    [TestCase(TftpTransferMode.Octet, 3, 4, 1)]
    [TestCase(TftpTransferMode.Octet, 4, 4, 2)]
    [TestCase(TftpTransferMode.Octet, 5, 4, 2)]
    [TestCase(TftpTransferMode.Octet, 8, 4, 3)]
    [TestCase(TftpTransferMode.Netascii, 0, 4, 1)]
    [TestCase(TftpTransferMode.Netascii, 3, 4, 1)]
    [TestCase(TftpTransferMode.Netascii, 4, 4, 2)]
    [TestCase(TftpTransferMode.Netascii, 5, 4, 2)]
    [TestCase(TftpTransferMode.Netascii, 8, 4, 3)]
    public void RoundTripsEveryContentBoundary(
        TftpTransferMode mode,
        int contentLength,
        int blockSize,
        int expectedPacketCount)
    {
        byte[] content = new byte[contentLength];
        int contentOffset = 0;
        while (contentOffset < content.Length)
        {
            content[contentOffset] = (byte)('A' + contentOffset % 20);
            contentOffset = contentOffset + 1;
        }

        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            content,
            mode,
            blockSize,
            timeSource,
            4,
            60);
        MemoryStream destination = new MemoryStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            mode,
            blockSize,
            timeSource,
            4,
            60,
            0);
        StartReceiver(receiver, TftpOpcode.ReadRequest);

        TftpSenderResult senderResult = sender.StartSending(CancellationToken.None);
        int packetCount = 0;
        while (senderResult.Action != TftpSenderAction.Completed)
        {
            TftpReceiverResult receiverResult = receiver.ProcessDataPacket(
                senderResult.Packet!,
                CancellationToken.None);
            packetCount = packetCount + 1;
            senderResult = sender.ProcessPacket(
                receiverResult.Packet!,
                CancellationToken.None);
        }

        TftpReceiverResult receiverCompleted = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(packetCount, Is.EqualTo(expectedPacketCount));
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(senderResult.BytesTransferred, Is.EqualTo(contentLength));
            Assert.That(receiverCompleted.BytesTransferred, Is.EqualTo(contentLength));
            Assert.That(senderResult.EncodedBytesTransferred, Is.EqualTo(contentLength));
            Assert.That(receiverCompleted.EncodedBytesTransferred,
                Is.EqualTo(contentLength));
            Assert.That(receiverCompleted.Action,
                Is.EqualTo(TftpReceiverAction.Completed));
        });
    }

    [Test]
    public void RoundTripsNetasciiExpansionAtExactWireBoundary()
    {
        byte[] content = { (byte)'A', 10, 13 };
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            content,
            TftpTransferMode.Netascii,
            4,
            timeSource,
            4,
            60);
        MemoryStream destination = new MemoryStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Netascii,
            4,
            timeSource,
            4,
            60,
            0);
        StartReceiver(receiver, TftpOpcode.ReadRequest);

        TftpSenderResult first = sender.StartSending(CancellationToken.None);
        TftpReceiverResult firstAck = receiver.ProcessDataPacket(
            first.Packet!,
            CancellationToken.None);
        TftpSenderResult second = sender.ProcessPacket(
            firstAck.Packet!,
            CancellationToken.None);
        TftpReceiverResult finalAck = receiver.ProcessDataPacket(
            second.Packet!,
            CancellationToken.None);
        TftpSenderResult completed = sender.ProcessPacket(
            finalAck.Packet!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet!.Payload,
                Is.EqualTo(new byte[] { (byte)'A', 13, 10, 13 }));
            Assert.That(second.Packet!.Payload, Is.EqualTo(new byte[] { 0 }));
            Assert.That(second.IsFinalData, Is.True);
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(5));
            Assert.That(destination.ToArray(), Is.EqualTo(content));
        });
    }

    [TestCase(TftpOpcode.ReadRequest)]
    [TestCase(TftpOpcode.WriteRequest)]
    [TestCase(TftpOpcode.Acknowledgement)]
    [TestCase(TftpOpcode.OptionAcknowledgement)]
    public void LostInitialPacketKindIsRetransmitted(TftpOpcode opcode)
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            4,
            timeSource,
            4,
            60,
            2);
        TftpPacket retained = StartReceiver(receiver, opcode);
        timeSource.AdvanceMilliseconds(1000);

        TftpReceiverResult retransmit = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpReceiverAction.RetransmitPacket));
            Assert.That(retransmit.Packet, Is.SameAs(retained));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void LostDataIsRetransmittedAndTransferContinues()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new byte[] { 1 },
            TftpTransferMode.Octet,
            2,
            timeSource,
            4,
            60);
        MemoryStream destination = new MemoryStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            timeSource,
            4,
            60,
            0);
        StartReceiver(receiver, TftpOpcode.ReadRequest);
        TftpSenderResult original = sender.StartSending(CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpSenderResult retransmit = sender.EvaluateTimer(CancellationToken.None);
        TftpReceiverResult acknowledgement = receiver.ProcessDataPacket(
            retransmit.Packet!,
            CancellationToken.None);
        TftpSenderResult completed = sender.ProcessPacket(
            acknowledgement.Packet!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(retransmit.Packet, Is.SameAs(original.Packet));
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpSenderAction.RetransmitData));
            Assert.That(completed.Action, Is.EqualTo(TftpSenderAction.Completed));
            Assert.That(completed.RetransmissionCount, Is.EqualTo(1));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1 }));
        });
    }

    [Test]
    public void LostNonfinalAckCausesDataReplayWithoutDuplicateWrite()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new byte[] { 1, 2, 3 },
            TftpTransferMode.Octet,
            2,
            timeSource,
            4,
            60);
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            timeSource,
            4,
            60,
            0);
        StartReceiver(receiver, TftpOpcode.ReadRequest);
        TftpSenderResult first = sender.StartSending(CancellationToken.None);
        TftpReceiverResult lostAck = receiver.ProcessDataPacket(
            first.Packet!,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpSenderResult replay = sender.EvaluateTimer(CancellationToken.None);
        TftpReceiverResult replacementAck = receiver.ProcessDataPacket(
            replay.Packet!,
            CancellationToken.None);
        TftpSenderResult second = sender.ProcessPacket(
            replacementAck.Packet!,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(replay.Packet, Is.SameAs(first.Packet));
            Assert.That(replacementAck.Action,
                Is.EqualTo(TftpReceiverAction.ReacknowledgeDuplicateData));
            Assert.That(replacementAck.Packet, Is.SameAs(lostAck.Packet));
            Assert.That(second.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
        });
    }

    [Test]
    public void LostFinalAckIsRecoveredDuringDally()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new byte[] { 7 },
            TftpTransferMode.Octet,
            2,
            timeSource,
            4,
            60);
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            timeSource,
            4,
            60,
            2);
        StartReceiver(receiver, TftpOpcode.ReadRequest);
        TftpSenderResult finalData = sender.StartSending(CancellationToken.None);
        TftpReceiverResult lostFinalAck = receiver.ProcessDataPacket(
            finalData.Packet!,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpSenderResult replay = sender.EvaluateTimer(CancellationToken.None);
        TftpReceiverResult replacementAck = receiver.ProcessDataPacket(
            replay.Packet!,
            CancellationToken.None);
        TftpSenderResult senderCompleted = sender.ProcessPacket(
            replacementAck.Packet!,
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(2000);
        TftpReceiverResult receiverCompleted = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(replacementAck.Action,
                Is.EqualTo(TftpReceiverAction.ReacknowledgeDuplicateData));
            Assert.That(replacementAck.Packet, Is.SameAs(lostFinalAck.Packet));
            Assert.That(senderCompleted.Action, Is.EqualTo(TftpSenderAction.Completed));
            Assert.That(receiverCompleted.Action,
                Is.EqualTo(TftpReceiverAction.Completed));
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
            Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 7 }));
        });
    }

    [Test]
    public void DuplicateStormsDoNotMultiplyPacketsOrResetTimeouts()
    {
        ManualTimeSource senderTime = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new byte[] { 1, 2, 3 },
            TftpTransferMode.Octet,
            2,
            senderTime,
            4,
            60);
        TftpSenderResult firstData = sender.StartSending(CancellationToken.None);
        TftpSenderResult secondData = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);
        senderTime.AdvanceMilliseconds(999);
        int duplicateIndex = 0;
        while (duplicateIndex < 1000)
        {
            TftpSenderResult ignored = sender.ProcessPacket(
                CreateAcknowledgement(1),
                CancellationToken.None);
            Assert.That(ignored.Action,
                Is.EqualTo(TftpSenderAction.IgnoreDuplicateAcknowledgement));
            duplicateIndex = duplicateIndex + 1;
        }

        TftpSenderResult senderWaiting = sender.EvaluateTimer(CancellationToken.None);
        senderTime.AdvanceMilliseconds(1);
        TftpSenderResult senderReplay = sender.EvaluateTimer(CancellationToken.None);

        ManualTimeSource receiverTime = new ManualTimeSource();
        CountingWriteStream destination = new CountingWriteStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            2,
            receiverTime,
            4,
            60,
            2);
        StartReceiver(receiver, TftpOpcode.ReadRequest);
        TftpReceiverResult firstAck = receiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1, 2 }),
            CancellationToken.None);
        receiverTime.AdvanceMilliseconds(999);
        duplicateIndex = 0;
        while (duplicateIndex < 1000)
        {
            TftpReceiverResult reack = receiver.ProcessDataPacket(
                CreateData(1, new byte[] { 1, 2 }),
                CancellationToken.None);
            Assert.That(reack.Packet, Is.SameAs(firstAck.Packet));
            duplicateIndex = duplicateIndex + 1;
        }

        TftpReceiverResult receiverWaiting = receiver.EvaluateReceiverTimer(
            CancellationToken.None);
        receiverTime.AdvanceMilliseconds(1);
        TftpReceiverResult receiverReplay = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(firstData.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(senderWaiting.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(senderReplay.Packet, Is.SameAs(secondData.Packet));
            Assert.That(senderReplay.RetransmissionCount, Is.EqualTo(1));
            Assert.That(senderReplay.DuplicatePacketCount, Is.EqualTo(1000));
            Assert.That(receiverWaiting.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(receiverReplay.Packet, Is.SameAs(firstAck.Packet));
            Assert.That(receiverReplay.RetransmissionCount, Is.EqualTo(1));
            Assert.That(receiverReplay.DuplicatePacketCount, Is.EqualTo(1000));
            Assert.That(destination.WriteCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SenderAndReceiverApplyCappedAdaptiveBackoffUntilExhausted()
    {
        ManualTimeSource senderTime = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new byte[] { 1 },
            TftpTransferMode.Octet,
            2,
            senderTime,
            4,
            60);
        TftpSenderResult originalData = sender.StartSending(CancellationToken.None);
        long[] intervals = { 1000, 2000, 4000, 4000 };
        int intervalIndex = 0;
        while (intervalIndex < intervals.Length)
        {
            senderTime.AdvanceMilliseconds(intervals[intervalIndex]);
            TftpSenderResult replay = sender.EvaluateTimer(CancellationToken.None);
            Assert.That(replay.Packet, Is.SameAs(originalData.Packet));
            intervalIndex = intervalIndex + 1;
        }

        senderTime.AdvanceMilliseconds(4000);
        TftpTimeoutException senderFailure = Assert.Throws<TftpTimeoutException>(
            () => sender.EvaluateTimer(CancellationToken.None));

        ManualTimeSource receiverTime = new ManualTimeSource();
        TftpReceiverStateMachine receiver = CreateReceiver(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            receiverTime,
            4,
            60,
            2);
        TftpPacket initial = StartReceiver(receiver, TftpOpcode.OptionAcknowledgement);
        intervalIndex = 0;
        while (intervalIndex < intervals.Length)
        {
            receiverTime.AdvanceMilliseconds(intervals[intervalIndex]);
            TftpReceiverResult replay = receiver.EvaluateReceiverTimer(
                CancellationToken.None);
            Assert.That(replay.Packet, Is.SameAs(initial));
            intervalIndex = intervalIndex + 1;
        }

        receiverTime.AdvanceMilliseconds(4000);
        TftpTimeoutException receiverFailure = Assert.Throws<TftpTimeoutException>(
            () => receiver.EvaluateReceiverTimer(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(senderFailure.RetryLimitReached, Is.True);
            Assert.That(senderFailure.RetransmissionCount, Is.EqualTo(4));
            Assert.That(sender.RetransmissionTimer.CurrentIntervalMilliseconds,
                Is.EqualTo(4000));
            Assert.That(receiverFailure.RetryLimitReached, Is.True);
            Assert.That(receiverFailure.RetransmissionCount, Is.EqualTo(4));
            Assert.That(receiver.RetransmissionTimer.CurrentIntervalMilliseconds,
                Is.EqualTo(4000));
        });
    }

    [Test]
    public void OlderDelayedAndFutureBlocksAreRejected()
    {
        ManualTimeSource senderTime = new ManualTimeSource();
        TftpSenderStateMachine delayedSender = CreateSender(
            new byte[] { 1, 2, 3, 4, 5 },
            TftpTransferMode.Octet,
            2,
            senderTime,
            4,
            60);
        delayedSender.StartSending(CancellationToken.None);
        delayedSender.ProcessPacket(CreateAcknowledgement(1), CancellationToken.None);
        delayedSender.ProcessPacket(CreateAcknowledgement(2), CancellationToken.None);
        TftpProtocolException delayedAck = Assert.Throws<TftpProtocolException>(() =>
            delayedSender.ProcessPacket(
                CreateAcknowledgement(1),
                CancellationToken.None));

        TftpSenderStateMachine futureSender = CreateSender(
            new byte[] { 1 },
            TftpTransferMode.Octet,
            2,
            new ManualTimeSource(),
            4,
            60);
        futureSender.StartSending(CancellationToken.None);
        TftpProtocolException futureAck = Assert.Throws<TftpProtocolException>(() =>
            futureSender.ProcessPacket(
                CreateAcknowledgement(2),
                CancellationToken.None));

        CountingWriteStream delayedDestination = new CountingWriteStream();
        TftpReceiverStateMachine delayedReceiver = CreateReceiver(
            delayedDestination,
            TftpTransferMode.Octet,
            2,
            new ManualTimeSource(),
            4,
            60,
            2);
        StartReceiver(delayedReceiver, TftpOpcode.ReadRequest);
        delayedReceiver.ProcessDataPacket(
            CreateData(1, new byte[] { 1, 2 }), CancellationToken.None);
        delayedReceiver.ProcessDataPacket(
            CreateData(2, new byte[] { 3, 4 }), CancellationToken.None);
        TftpProtocolException delayedData = Assert.Throws<TftpProtocolException>(() =>
            delayedReceiver.ProcessDataPacket(
                CreateData(1, new byte[] { 1, 2 }), CancellationToken.None));

        CountingWriteStream futureDestination = new CountingWriteStream();
        TftpReceiverStateMachine futureReceiver = CreateReceiver(
            futureDestination,
            TftpTransferMode.Octet,
            2,
            new ManualTimeSource(),
            4,
            60,
            2);
        StartReceiver(futureReceiver, TftpOpcode.ReadRequest);
        TftpProtocolException futureData = Assert.Throws<TftpProtocolException>(() =>
            futureReceiver.ProcessDataPacket(
                CreateData(2, new byte[] { 1 }), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(delayedAck.Message, Does.Contain("unexpected ACK"));
            Assert.That(futureAck.Message, Does.Contain("unexpected ACK"));
            Assert.That(delayedData.Message, Does.Contain("unexpected DATA"));
            Assert.That(futureData.Message, Does.Contain("unexpected DATA"));
            Assert.That(delayedDestination.ToArray(),
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(delayedDestination.WriteCallCount, Is.EqualTo(2));
            Assert.That(futureDestination.WriteCallCount, Is.Zero);
        });
    }

    [Test]
    public void ContinuousProgressDoesNotBypassSenderTotalDuration()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new byte[] { 1, 2, 3, 4, 5 },
            TftpTransferMode.Octet,
            1,
            timeSource,
            4,
            3);
        sender.StartSending(CancellationToken.None);
        int block = 1;
        while (block < 4)
        {
            timeSource.AdvanceMilliseconds(900);
            sender.ProcessPacket(
                CreateAcknowledgement((ushort)block),
                CancellationToken.None);
            block = block + 1;
        }

        timeSource.AdvanceMilliseconds(300);
        TftpTimeoutException exception = Assert.Throws<TftpTimeoutException>(() =>
            sender.ProcessPacket(
                CreateAcknowledgement(4),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.RetryLimitReached, Is.False);
            Assert.That(exception.ElapsedMilliseconds, Is.EqualTo(3000));
            Assert.That(sender.BytesTransferred, Is.EqualTo(3));
            Assert.That(sender.Failed, Is.True);
        });
    }

    [Test]
    public void SenderAndReceiverStaySynchronizedAcrossRollover()
    {
        byte[] content = { 1, 2 };
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            content,
            TftpTransferMode.Octet,
            1,
            timeSource,
            4,
            60);
        MemoryStream destination = new MemoryStream();
        TftpReceiverStateMachine receiver = CreateReceiver(
            destination,
            TftpTransferMode.Octet,
            1,
            timeSource,
            4,
            60,
            0);
        sender.BlockCounter.CurrentBlock = ushort.MaxValue;
        receiver.BlockCounter.CurrentBlock = ushort.MaxValue;
        StartReceiver(receiver, TftpOpcode.ReadRequest);
        List<ushort> blocks = new List<ushort>();

        TftpSenderResult senderResult = sender.StartSending(CancellationToken.None);
        while (senderResult.Action != TftpSenderAction.Completed)
        {
            blocks.Add(senderResult.Packet!.BlockNumber);
            TftpReceiverResult receiverResult = receiver.ProcessDataPacket(
                senderResult.Packet,
                CancellationToken.None);
            senderResult = sender.ProcessPacket(
                receiverResult.Packet!,
                CancellationToken.None);
        }

        TftpReceiverResult receiverCompleted = receiver.EvaluateReceiverTimer(
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(blocks,
                Is.EqualTo(new ushort[] { ushort.MaxValue, 0, 1 }));
            Assert.That(destination.ToArray(), Is.EqualTo(content));
            Assert.That(senderResult.Action, Is.EqualTo(TftpSenderAction.Completed));
            Assert.That(receiverCompleted.Action,
                Is.EqualTo(TftpReceiverAction.Completed));
        });
    }

    private static TftpSenderStateMachine CreateSender(
        byte[] content,
        TftpTransferMode mode,
        int blockSize,
        ManualTimeSource timeSource,
        int maximumRetries,
        int maximumDurationSeconds)
    {
        return new TftpSenderStateMachine(
            new MemoryStream(content),
            mode,
            blockSize,
            CreateTimer(timeSource, maximumRetries, maximumDurationSeconds));
    }

    private static TftpReceiverStateMachine CreateReceiver(
        Stream destination,
        TftpTransferMode mode,
        int blockSize,
        ManualTimeSource timeSource,
        int maximumRetries,
        int maximumDurationSeconds,
        int dallyDurationSeconds)
    {
        return new TftpReceiverStateMachine(
            destination,
            mode,
            blockSize,
            long.MaxValue,
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

    private static TftpPacket StartReceiver(
        TftpReceiverStateMachine receiver,
        TftpOpcode opcode)
    {
        TftpPacket retained = new TftpPacket();
        retained.Opcode = opcode;
        receiver.StartReceiving(retained, CancellationToken.None);
        return retained;
    }

    private static TftpPacket CreateAcknowledgement(ushort blockNumber)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = blockNumber;
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
