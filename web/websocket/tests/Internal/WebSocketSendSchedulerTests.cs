using websocket;

namespace websocket.Tests;

public class WebSocketSendSchedulerTests
{
    [TestCase(0)]
    [TestCase(-1)]
    public void ConstructorRejectsNonPositiveCapacity(int capacity)
    {
        Assert.Throws<WebSocketLimitException>(() =>
            new WebSocketSendScheduler(capacity));
    }

    [Test]
    public void NewSchedulerReportsItsBoundedCapacity()
    {
        var scheduler = new WebSocketSendScheduler(7);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.GetPendingByteCapacity(), Is.EqualTo(7));
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
            Assert.That(scheduler.GetPendingDataByteCount(), Is.Zero);
            Assert.That(scheduler.GetPendingControlByteCount(), Is.Zero);
            Assert.That(
                scheduler.GetPendingByteAvailableCapacity(),
                Is.EqualTo(7));
        });
    }

    [Test]
    public void DataFramesRemainInOrderAcrossPartialDrains()
    {
        var scheduler = new WebSocketSendScheduler(10);
        bool queued = scheduler.TryQueueDataFrames(
            [[1, 2], [3], [4, 5]]);
        var first = new byte[3];
        int firstCount = scheduler.WritePendingBytes(first);
        var second = new byte[4];
        int secondCount = scheduler.WritePendingBytes(second);

        Assert.Multiple(() =>
        {
            Assert.That(queued, Is.True);
            Assert.That(firstCount, Is.EqualTo(3));
            Assert.That(first, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(secondCount, Is.EqualTo(2));
            Assert.That(second, Is.EqualTo(new byte[] { 4, 5, 0, 0 }));
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
        });
    }

    [Test]
    public void QueuedControlWaitsForCurrentDataFrameThenPrecedesNextFrame()
    {
        var scheduler = new WebSocketSendScheduler(12);
        scheduler.TryQueueDataFrames([[1, 2, 3], [4, 5]]);
        var firstByte = new byte[1];
        scheduler.WritePendingBytes(firstByte);

        scheduler.TryQueueControlFrame([9, 8]);
        var remaining = new byte[6];
        int remainingCount = scheduler.WritePendingBytes(remaining);

        Assert.Multiple(() =>
        {
            Assert.That(firstByte, Is.EqualTo(new byte[] { 1 }));
            Assert.That(remainingCount, Is.EqualTo(6));
            Assert.That(
                remaining,
                Is.EqualTo(new byte[] { 2, 3, 9, 8, 4, 5 }));
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
        });
    }

    [Test]
    public void QueuedControlsAreFifoAndHavePriorityAtAnIdleBoundary()
    {
        var scheduler = new WebSocketSendScheduler(8);
        scheduler.TryQueueDataFrame([1, 2]);
        scheduler.TryQueueControlFrame([7, 8]);
        scheduler.TryQueueControlFrame([9]);
        var destination = new byte[5];

        int written = scheduler.WritePendingBytes(destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(5));
            Assert.That(
                destination,
                Is.EqualTo(new byte[] { 7, 8, 9, 1, 2 }));
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
        });
    }

    [Test]
    public void CapacityRejectionLeavesTheExistingScheduleUnchanged()
    {
        var scheduler = new WebSocketSendScheduler(5);
        Assert.That(scheduler.TryQueueDataFrame([1, 2]), Is.True);

        bool batchAccepted = scheduler.TryQueueDataFrames(
            [[3, 4], [5, 6]]);
        bool controlAccepted = scheduler.TryQueueControlFrame([7, 8, 9, 10]);
        var destination = new byte[5];
        int written = scheduler.WritePendingBytes(destination);

        Assert.Multiple(() =>
        {
            Assert.That(batchAccepted, Is.False);
            Assert.That(controlAccepted, Is.False);
            Assert.That(written, Is.EqualTo(2));
            Assert.That(
                destination,
                Is.EqualTo(new byte[] { 1, 2, 0, 0, 0 }));
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
        });
    }

    [Test]
    public void PendingCountsTrackDataControlAndAvailableBytes()
    {
        var scheduler = new WebSocketSendScheduler(6);
        scheduler.TryQueueDataFrame([1, 2, 3]);
        scheduler.TryQueueControlFrame([8, 9]);

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.GetPendingByteCount(), Is.EqualTo(5));
            Assert.That(scheduler.GetPendingDataByteCount(), Is.EqualTo(3));
            Assert.That(
                scheduler.GetPendingControlByteCount(),
                Is.EqualTo(2));
            Assert.That(
                scheduler.GetPendingByteAvailableCapacity(),
                Is.EqualTo(1));
        });

        var first = new byte[1];
        scheduler.WritePendingBytes(first);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(new byte[] { 8 }));
            Assert.That(scheduler.GetPendingByteCount(), Is.EqualTo(4));
            Assert.That(scheduler.GetPendingDataByteCount(), Is.EqualTo(3));
            Assert.That(
                scheduler.GetPendingControlByteCount(),
                Is.EqualTo(1));
            Assert.That(
                scheduler.GetPendingByteAvailableCapacity(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void DrainingImmediatelyReclaimsCapacity()
    {
        var scheduler = new WebSocketSendScheduler(4);
        scheduler.TryQueueDataFrame([1, 2, 3, 4]);
        var prefix = new byte[2];
        scheduler.WritePendingBytes(prefix);

        bool queued = scheduler.TryQueueDataFrame([5, 6]);
        var remainder = new byte[4];
        int written = scheduler.WritePendingBytes(remainder);

        Assert.Multiple(() =>
        {
            Assert.That(prefix, Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(queued, Is.True);
            Assert.That(written, Is.EqualTo(4));
            Assert.That(remainder, Is.EqualTo(new byte[] { 3, 4, 5, 6 }));
            Assert.That(
                scheduler.GetPendingByteAvailableCapacity(),
                Is.EqualTo(4));
        });
    }

    [Test]
    public void QueuedFramesAreIndependentOfCallerArrays()
    {
        var scheduler = new WebSocketSendScheduler(6);
        var data = new byte[] { 1, 2 };
        var control = new byte[] { 8, 9 };
        scheduler.TryQueueDataFrame(data);
        scheduler.TryQueueControlFrame(control);
        data[0] = 40;
        control[0] = 50;
        var destination = new byte[4];

        scheduler.WritePendingBytes(destination);

        Assert.That(destination, Is.EqualTo(new byte[] { 8, 9, 1, 2 }));
    }

    [Test]
    public void EmptyDestinationDoesNotChangeTheSchedule()
    {
        var scheduler = new WebSocketSendScheduler(4);
        scheduler.TryQueueDataFrame([1, 2]);

        int written = scheduler.WritePendingBytes(Span<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.Zero);
            Assert.That(scheduler.GetPendingByteCount(), Is.EqualTo(2));
            Assert.That(scheduler.GetPendingDataByteCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ClearRemovesCurrentDataAndQueuedControlBytes()
    {
        var scheduler = new WebSocketSendScheduler(8);
        scheduler.TryQueueDataFrames([[1, 2, 3], [4, 5]]);
        var prefix = new byte[1];
        scheduler.WritePendingBytes(prefix);
        scheduler.TryQueueControlFrame([8, 9]);

        scheduler.ClearPendingBytes();
        var destination = new byte[2];

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.WritePendingBytes(destination), Is.Zero);
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
            Assert.That(scheduler.GetPendingDataByteCount(), Is.Zero);
            Assert.That(scheduler.GetPendingControlByteCount(), Is.Zero);
            Assert.That(
                scheduler.GetPendingByteAvailableCapacity(),
                Is.EqualTo(8));
        });
    }

    [Test]
    public void InvalidFrameInputsAreRejectedWithoutChangingCounts()
    {
        var scheduler = new WebSocketSendScheduler(8);

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() =>
                scheduler.TryQueueDataFrame(null));
            Assert.Throws<WebSocketStateException>(() =>
                scheduler.TryQueueControlFrame([]));
            Assert.Throws<WebSocketStateException>(() =>
                scheduler.TryQueueDataFrames(null));
            Assert.Throws<WebSocketStateException>(() =>
                scheduler.TryQueueDataFrames([]));
            Assert.Throws<WebSocketStateException>(() =>
                scheduler.TryQueueDataFrames([[1], null!]));
            Assert.That(scheduler.GetPendingByteCount(), Is.Zero);
        });
    }
}
