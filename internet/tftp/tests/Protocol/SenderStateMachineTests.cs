using System.IO;
using System.Threading;
using tftp;

namespace tftp.Tests;

public class SenderStateMachineTests
{
    [Test]
    public void EmptySourceSendsAndCompletesWithEmptyDataOne()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(),
            TftpTransferMode.Octet,
            4,
            timeSource,
            3,
            60);

        TftpSenderResult started = sender.StartSending(CancellationToken.None);
        TftpSenderResult completed = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(started.Action, Is.EqualTo(TftpSenderAction.SendData));
            Assert.That(started.Packet, Is.Not.Null);
            Assert.That(started.Packet!.Opcode, Is.EqualTo(TftpOpcode.Data));
            Assert.That(started.Packet.BlockNumber, Is.EqualTo(1));
            Assert.That(started.Packet.Payload, Is.Empty);
            Assert.That(started.IsFinalData, Is.True);
            Assert.That(sender.LastDataPacket, Is.Null);
            Assert.That(completed.Action, Is.EqualTo(TftpSenderAction.Completed));
            Assert.That(completed.BytesTransferred, Is.Zero);
            Assert.That(completed.EncodedBytesTransferred, Is.Zero);
            Assert.That(sender.Completed, Is.True);
        });
    }

    [Test]
    public void ShortFinalBlockReportsAcknowledgedMetrics()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        byte[] content = { 1, 2, 3 };
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(content),
            TftpTransferMode.Octet,
            4,
            timeSource,
            3,
            60);

        TftpSenderResult started = sender.StartSending(CancellationToken.None);
        TftpSenderResult completed = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(started.Packet!.Payload, Is.EqualTo(content));
            Assert.That(started.IsFinalData, Is.True);
            Assert.That(started.BytesTransferred, Is.Zero);
            Assert.That(completed.BytesTransferred, Is.EqualTo(3));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(3));
        });
    }

    [Test]
    public void ExactMultipleEndsWithAdditionalEmptyDataBlock()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1, 2, 3, 4 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);

        TftpSenderResult first = sender.StartSending(CancellationToken.None);
        TftpSenderResult second = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);
        TftpSenderResult final = sender.ProcessPacket(
            CreateAcknowledgement(2),
            CancellationToken.None);
        TftpSenderResult completed = sender.ProcessPacket(
            CreateAcknowledgement(3),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet!.BlockNumber, Is.EqualTo(1));
            Assert.That(first.Packet.Payload, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(first.IsFinalData, Is.False);
            Assert.That(second.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(second.Packet.Payload, Is.EqualTo(new byte[] { 3, 4 }));
            Assert.That(second.IsFinalData, Is.False);
            Assert.That(second.BytesTransferred, Is.EqualTo(2));
            Assert.That(final.Packet!.BlockNumber, Is.EqualTo(3));
            Assert.That(final.Packet.Payload, Is.Empty);
            Assert.That(final.IsFinalData, Is.True);
            Assert.That(final.BytesTransferred, Is.EqualTo(4));
            Assert.That(completed.Action, Is.EqualTo(TftpSenderAction.Completed));
            Assert.That(completed.BytesTransferred, Is.EqualTo(4));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(4));
        });
    }

    [Test]
    public void FillsBlockAcrossShortSourceReads()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        ShortReadStream source = new ShortReadStream(new byte[] { 1, 2, 3, 4 }, 1);
        TftpSenderStateMachine sender = CreateSender(
            source,
            TftpTransferMode.Octet,
            3,
            timeSource,
            3,
            60);

        TftpSenderResult first = sender.StartSending(CancellationToken.None);
        TftpSenderResult second = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet!.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(first.IsFinalData, Is.False);
            Assert.That(second.Packet!.Payload, Is.EqualTo(new byte[] { 4 }));
            Assert.That(second.IsFinalData, Is.True);
            Assert.That(source.ReadCallCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void PreservesNetasciiExpansionAcrossDataBoundaries()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        byte[] content = { (byte)'A', 10, 13, (byte)'B' };
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(content),
            TftpTransferMode.Netascii,
            2,
            timeSource,
            3,
            60);

        TftpSenderResult first = sender.StartSending(CancellationToken.None);
        TftpSenderResult second = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);
        TftpSenderResult third = sender.ProcessPacket(
            CreateAcknowledgement(2),
            CancellationToken.None);
        TftpSenderResult final = sender.ProcessPacket(
            CreateAcknowledgement(3),
            CancellationToken.None);
        TftpSenderResult completed = sender.ProcessPacket(
            CreateAcknowledgement(4),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet!.Payload,
                Is.EqualTo(new byte[] { (byte)'A', 13 }));
            Assert.That(second.Packet!.Payload, Is.EqualTo(new byte[] { 10, 13 }));
            Assert.That(third.Packet!.Payload,
                Is.EqualTo(new byte[] { 0, (byte)'B' }));
            Assert.That(final.Packet!.Payload, Is.Empty);
            Assert.That(final.IsFinalData, Is.True);
            Assert.That(completed.BytesTransferred, Is.EqualTo(4));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(6));
        });
    }

    [Test]
    public void FlushesPendingNetasciiByteBeforeReadingMoreSourceData()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        ShortReadStream source = new ShortReadStream(
            new byte[] { 10, (byte)'A' },
            1);
        TftpSenderStateMachine sender = CreateSender(
            source,
            TftpTransferMode.Netascii,
            1,
            timeSource,
            3,
            60);

        TftpSenderResult first = sender.StartSending(CancellationToken.None);
        TftpSenderResult second = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Packet!.Payload, Is.EqualTo(new byte[] { 13 }));
            Assert.That(second.Packet!.Payload, Is.EqualTo(new byte[] { 10 }));
            Assert.That(source.ReadCallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void NetasciiPartialBlockIsFinalWithoutExtraEmptyBlock()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 10 }),
            TftpTransferMode.Netascii,
            3,
            timeSource,
            3,
            60);

        TftpSenderResult started = sender.StartSending(CancellationToken.None);
        TftpSenderResult completed = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(started.Packet!.Payload, Is.EqualTo(new byte[] { 13, 10 }));
            Assert.That(started.IsFinalData, Is.True);
            Assert.That(completed.BytesTransferred, Is.EqualTo(1));
            Assert.That(completed.EncodedBytesTransferred, Is.EqualTo(2));
        });
    }

    [Test]
    public void DuplicateHandshakeAcknowledgementIsIgnoredWithoutResettingTimer()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);
        TftpSenderResult started = sender.StartSending(CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpSenderResult duplicate = sender.ProcessPacket(
            CreateAcknowledgement(0),
            CancellationToken.None);
        TftpSenderResult retransmit = sender.EvaluateTimer(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpSenderAction.IgnoreDuplicateAcknowledgement));
            Assert.That(duplicate.Packet, Is.Null);
            Assert.That(duplicate.DuplicatePacketCount, Is.EqualTo(1));
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpSenderAction.RetransmitData));
            Assert.That(retransmit.Packet, Is.SameAs(started.Packet));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void PreviousAcknowledgementIsIgnoredWithoutResettingTimer()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1, 2, 3 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);
        sender.StartSending(CancellationToken.None);
        TftpSenderResult second = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(999);

        TftpSenderResult duplicate = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);
        TftpSenderResult waiting = sender.EvaluateTimer(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(second.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(duplicate.Action,
                Is.EqualTo(TftpSenderAction.IgnoreDuplicateAcknowledgement));
            Assert.That(waiting.Action, Is.EqualTo(TftpSenderAction.Wait));
            Assert.That(waiting.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(waiting.DuplicatePacketCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ValidAcknowledgementResetsBackoffAndRetryBudget()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1, 2, 3 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            1,
            60);
        sender.StartSending(CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);
        sender.EvaluateTimer(CancellationToken.None);

        TftpSenderResult second = sender.ProcessPacket(
            CreateAcknowledgement(1),
            CancellationToken.None);
        timeSource.AdvanceMilliseconds(999);
        TftpSenderResult waiting = sender.EvaluateTimer(CancellationToken.None);
        timeSource.AdvanceMilliseconds(1);
        TftpSenderResult retransmit = sender.EvaluateTimer(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(second.Packet!.BlockNumber, Is.EqualTo(2));
            Assert.That(waiting.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpSenderAction.RetransmitData));
            Assert.That(retransmit.RetransmissionCount, Is.EqualTo(2));
            Assert.That(sender.RetransmissionTimer.ConsecutiveRetransmissionCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void ArmsInitialIntervalAfterSourceBlockIsPrepared()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        AdvancingReadStream source = new AdvancingReadStream(
            new byte[] { 1, 2 },
            timeSource,
            1500);
        TftpSenderStateMachine sender = CreateSender(
            source,
            TftpTransferMode.Octet,
            1,
            timeSource,
            3,
            60);

        sender.StartSending(CancellationToken.None);
        TftpSenderResult waiting = sender.EvaluateTimer(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(timeSource.CurrentMilliseconds, Is.EqualTo(1500));
            Assert.That(waiting.Action, Is.EqualTo(TftpSenderAction.Wait));
            Assert.That(waiting.WaitMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void RetransmissionReturnsSameRetainedPacketAndEnforcesRetryLimit()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            1,
            60);
        TftpSenderResult started = sender.StartSending(CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpSenderResult retransmit = sender.EvaluateTimer(CancellationToken.None);
        timeSource.AdvanceMilliseconds(2000);
        TftpTimeoutException exception = Assert.Throws<TftpTimeoutException>(
            () => sender.EvaluateTimer(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpSenderAction.RetransmitData));
            Assert.That(retransmit.Packet, Is.SameAs(started.Packet));
            Assert.That(exception.RetryLimitReached, Is.True);
            Assert.That(exception.RetransmissionCount, Is.EqualTo(1));
            Assert.That(sender.Failed, Is.True);
        });
    }

    [Test]
    public void TotalDurationFailureIsSurfacedBySender()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            1);
        sender.StartSending(CancellationToken.None);
        timeSource.AdvanceMilliseconds(1000);

        TftpTimeoutException exception = Assert.Throws<TftpTimeoutException>(
            () => sender.EvaluateTimer(CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.RetryLimitReached, Is.False);
            Assert.That(exception.ElapsedMilliseconds, Is.EqualTo(1000));
            Assert.That(sender.Failed, Is.True);
        });
    }

    [Test]
    public void UnexpectedAcknowledgementTerminatesSender()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);
        sender.StartSending(CancellationToken.None);

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => sender.ProcessPacket(
                CreateAcknowledgement(2),
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("unexpected ACK"));
            Assert.That(sender.Failed, Is.True);
            Assert.Throws<TftpProtocolException>(
                () => sender.EvaluateTimer(CancellationToken.None));
        });
    }

    [TestCase(TftpOpcode.Data)]
    [TestCase(TftpOpcode.OptionAcknowledgement)]
    [TestCase(TftpOpcode.ReadRequest)]
    public void NonAcknowledgementPacketTerminatesSender(TftpOpcode opcode)
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);
        sender.StartSending(CancellationToken.None);
        TftpPacket packet = new TftpPacket();
        packet.Opcode = opcode;

        TftpProtocolException exception = Assert.Throws<TftpProtocolException>(
            () => sender.ProcessPacket(packet, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Does.Contain("other than ACK or ERROR"));
            Assert.That(sender.Failed, Is.True);
        });
    }

    [Test]
    public void RemoteErrorIsSurfacedImmediatelyAndTerminatesSender()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1 }),
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);
        sender.StartSending(CancellationToken.None);
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Error;
        packet.ErrorCode = TftpErrorCode.StorageExhausted;
        packet.ErrorMessage = "no space";

        TftpRemoteErrorException exception = Assert.Throws<TftpRemoteErrorException>(
            () => sender.ProcessPacket(packet, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo(TftpErrorCode.StorageExhausted));
            Assert.That(exception.RemoteMessage, Is.EqualTo("no space"));
            Assert.That(sender.Failed, Is.True);
            Assert.That(sender.RetransmissionTimer.TotalRetransmissionCount, Is.Zero);
        });
    }

    [Test]
    public void AdvancesBlockNumberAcrossRollover()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine sender = CreateSender(
            new MemoryStream(new byte[] { 1, 2 }),
            TftpTransferMode.Octet,
            1,
            timeSource,
            3,
            60);
        sender.BlockCounter.CurrentBlock = ushort.MaxValue;

        TftpSenderResult maximum = sender.StartSending(CancellationToken.None);
        TftpSenderResult wrapped = sender.ProcessPacket(
            CreateAcknowledgement(ushort.MaxValue),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Packet!.BlockNumber, Is.EqualTo(ushort.MaxValue));
            Assert.That(wrapped.Packet!.BlockNumber, Is.Zero);
            Assert.That(wrapped.Packet.Payload, Is.EqualTo(new byte[] { 2 }));
        });
    }

    [Test]
    public void CancellationBeforeStartDoesNotReadSource()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        ShortReadStream source = new ShortReadStream(new byte[] { 1 }, 1);
        TftpSenderStateMachine sender = CreateSender(
            source,
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => sender.StartSending(cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(source.ReadCallCount, Is.Zero);
            Assert.That(sender.Started, Is.False);
            Assert.That(sender.Cancelled, Is.True);
            Assert.Throws<TftpProtocolException>(
                () => sender.StartSending(CancellationToken.None));
        });
    }

    [Test]
    public void CancellationRequestedBySourceReadTerminatesFilling()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        CancelAfterReadStream source = new CancelAfterReadStream(
            new byte[] { 1, 2 },
            cancellation);
        TftpSenderStateMachine sender = CreateSender(
            source,
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);

        Assert.Throws<OperationCanceledException>(
            () => sender.StartSending(cancellation.Token));

        Assert.Multiple(() =>
        {
            Assert.That(source.ReadCallCount, Is.EqualTo(1));
            Assert.That(sender.Cancelled, Is.True);
            Assert.That(sender.LastDataPacket, Is.Null);
        });
    }

    [Test]
    public void RejectsInvalidConstructionInputs()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 3, 60);
        MemoryStream closed = new MemoryStream();
        closed.Dispose();

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                new TftpSenderStateMachine(
                    null!, TftpTransferMode.Octet, 1, timer));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpSenderStateMachine(
                    closed, TftpTransferMode.Octet, 1, timer));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpSenderStateMachine(
                    new MemoryStream(), (TftpTransferMode)99, 1, timer));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpSenderStateMachine(
                    new MemoryStream(), TftpTransferMode.Octet, 0, timer));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpSenderStateMachine(
                    new MemoryStream(),
                    TftpTransferMode.Octet,
                    TftpProtocolValues.MaximumBlockSize + 1,
                    timer));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpSenderStateMachine(
                    new MemoryStream(), TftpTransferMode.Octet, 1, null!));
        });
    }

    [Test]
    public void RejectsInvalidLifecycleCallsAndNullPacket()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpSenderStateMachine notStarted = CreateSender(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            timeSource,
            3,
            60);
        TftpSenderStateMachine completed = CreateSender(
            new MemoryStream(),
            TftpTransferMode.Octet,
            2,
            new ManualTimeSource(),
            3,
            60);

        Assert.Throws<TftpProtocolException>(
            () => notStarted.EvaluateTimer(CancellationToken.None));
        notStarted.StartSending(CancellationToken.None);
        Assert.Throws<TftpProtocolException>(
            () => notStarted.StartSending(CancellationToken.None));
        Assert.Throws<TftpProtocolException>(
            () => notStarted.ProcessPacket(null!, CancellationToken.None));
        completed.StartSending(CancellationToken.None);
        completed.ProcessPacket(CreateAcknowledgement(1), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(notStarted.Failed, Is.True);
            Assert.Throws<TftpProtocolException>(() =>
                completed.ProcessPacket(
                    CreateAcknowledgement(1),
                    CancellationToken.None));
            Assert.Throws<TftpProtocolException>(() =>
                completed.EvaluateTimer(CancellationToken.None));
        });
    }

    private static TftpSenderStateMachine CreateSender(
        Stream source,
        TftpTransferMode mode,
        int blockSize,
        ManualTimeSource timeSource,
        int maximumRetries,
        int maximumDurationSeconds)
    {
        return new TftpSenderStateMachine(
            source,
            mode,
            blockSize,
            CreateTimer(timeSource, maximumRetries, maximumDurationSeconds));
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

    private static TftpPacket CreateAcknowledgement(ushort blockNumber)
    {
        TftpPacket packet = new TftpPacket();
        packet.Opcode = TftpOpcode.Acknowledgement;
        packet.BlockNumber = blockNumber;
        return packet;
    }
}

internal class ShortReadStream : MemoryStream
{
    internal int MaximumReadLength;
    internal int ReadCallCount = 0;

    internal ShortReadStream(byte[] content, int maximumReadLength)
        : base(content)
    {
        MaximumReadLength = maximumReadLength;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCallCount = ReadCallCount + 1;
        int limitedCount = count;
        if (limitedCount > MaximumReadLength)
        {
            limitedCount = MaximumReadLength;
        }

        return base.Read(buffer, offset, limitedCount);
    }
}

internal class CancelAfterReadStream : MemoryStream
{
    internal CancellationTokenSource Cancellation;
    internal int ReadCallCount = 0;

    internal CancelAfterReadStream(
        byte[] content,
        CancellationTokenSource cancellation)
        : base(content)
    {
        Cancellation = cancellation;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ReadCallCount = ReadCallCount + 1;
        int readLength = base.Read(buffer, offset, count);
        Cancellation.Cancel();
        return readLength;
    }
}

internal class AdvancingReadStream : MemoryStream
{
    internal ManualTimeSource TimeSource;
    internal long AdvanceLength;

    internal AdvancingReadStream(
        byte[] content,
        ManualTimeSource timeSource,
        long advanceLength)
        : base(content)
    {
        TimeSource = timeSource;
        AdvanceLength = advanceLength;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int readLength = base.Read(buffer, offset, count);
        TimeSource.AdvanceMilliseconds(AdvanceLength);
        return readLength;
    }
}
