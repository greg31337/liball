using websocket;

namespace websocket.Tests;

public class DeflateSlidingWindowTests
{
    [TestCase(8, 256)]
    [TestCase(15, 32768)]
    public void WindowBitsSelectBoundedHistoryCapacity(
        int windowBits,
        int expectedSize)
    {
        var window = new DeflateSlidingWindow(windowBits);

        Assert.Multiple(() =>
        {
            Assert.That(window.GetWindowSize(), Is.EqualTo(expectedSize));
            Assert.That(window.GetHistoryCount(), Is.Zero);
        });
    }

    [TestCase(7)]
    [TestCase(16)]
    public void WindowBitsOutsideNegotiatedRangeAreRejected(int windowBits)
    {
        Assert.Throws<WebSocketStateException>(
            () => new DeflateSlidingWindow(windowBits));
    }

    [Test]
    public void LiteralBytesResumeAtExactOutputBoundaryAndEnterHistory()
    {
        var window = new DeflateSlidingWindow(8);
        byte[] source = [10, 20, 30, 40, 50];
        var firstOutput = new byte[2];
        var secondOutput = new byte[3];

        var first = window.WriteBytes(source, firstOutput);
        var second = window.WriteBytes(
            source.AsSpan(first.ConsumedCount),
            secondOutput);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(first.ConsumedCount, Is.EqualTo(2));
            Assert.That(first.ProducedCount, Is.EqualTo(2));
            Assert.That(firstOutput, Is.EqualTo(new byte[] { 10, 20 }));
            Assert.That(second.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.ConsumedCount, Is.EqualTo(3));
            Assert.That(secondOutput, Is.EqualTo(new byte[] { 30, 40, 50 }));
            Assert.That(window.GetHistoryCount(), Is.EqualTo(5));
        });
    }

    [Test]
    public void LiteralBytesSupportOverlappingSourceAndDestinationSpans()
    {
        var window = new DeflateSlidingWindow(8);
        byte[] buffer = [1, 2, 3, 4];

        var write = window.WriteBytes(
            buffer.AsSpan(0, 3),
            buffer.AsSpan(1, 3));
        var begin = window.BeginCopy(3, 3);
        var history = new byte[3];
        window.ContinueCopy(history);

        Assert.Multiple(() =>
        {
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(buffer, Is.EqualTo(new byte[] { 1, 1, 2, 3 }));
            Assert.That(begin, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(history, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void OverlappingCopyResumesWithoutLosingItsSourcePosition()
    {
        var window = new DeflateSlidingWindow(8);
        window.WriteBytes(new byte[] { (byte)'X', (byte)'Y' }, new byte[2]);
        var begin = window.BeginCopy(2, 7);
        var firstOutput = new byte[3];
        var secondOutput = new byte[4];

        var first = window.ContinueCopy(firstOutput);
        var second = window.ContinueCopy(secondOutput);

        Assert.Multiple(() =>
        {
            Assert.That(begin, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(first.RemainingCopyLength, Is.EqualTo(4));
            Assert.That(firstOutput,
                Is.EqualTo(new byte[] { (byte)'X', (byte)'Y', (byte)'X' }));
            Assert.That(second.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.RemainingCopyLength, Is.Zero);
            Assert.That(secondOutput,
                Is.EqualTo(new byte[]
                {
                    (byte)'Y', (byte)'X', (byte)'Y', (byte)'X'
                }));
            Assert.That(window.GetPendingCopyLength(), Is.Zero);
            Assert.That(window.GetHistoryCount(), Is.EqualTo(9));
        });
    }

    [Test]
    public void MaximumOverlappingCopyRemainsBoundedByWindowCapacity()
    {
        var window = new DeflateSlidingWindow(8);
        window.WriteBytes(new byte[] { 0x5a }, new byte[1]);
        window.BeginCopy(1, 258);
        var output = new byte[258];

        var copy = window.ContinueCopy(output);

        Assert.Multiple(() =>
        {
            Assert.That(copy.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(output, Is.All.EqualTo(0x5a));
            Assert.That(window.GetHistoryCount(), Is.EqualTo(256));
        });
    }

    [Test]
    public void CopyValidatesLengthDistanceAndAvailableHistory()
    {
        var empty = new DeflateSlidingWindow(8);
        var window = new DeflateSlidingWindow(8);
        window.WriteBytes(new byte[] { 1, 2, 3 }, new byte[3]);

        var noHistory = empty.BeginCopy(1, 3);
        var zeroDistance = window.BeginCopy(0, 3);
        var distant = window.BeginCopy(4, 3);
        var shortLength = window.BeginCopy(1, 2);
        var longLength = window.BeginCopy(1, 259);

        Assert.Multiple(() =>
        {
            Assert.That(noHistory,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(zeroDistance,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(distant,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(shortLength,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(longLength,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(window.GetPendingCopyLength(), Is.Zero);
            Assert.That(window.GetHistoryCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void RingWrapPreservesExactlyTheNegotiatedWindow()
    {
        var window = new DeflateSlidingWindow(8);
        var source = new byte[300];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = (byte)index;
            index++;
        }

        window.WriteBytes(source, new byte[source.Length]);
        var tooFar = window.BeginCopy(257, 3);
        var boundary = window.BeginCopy(256, 3);
        var output = new byte[3];
        var copy = window.ContinueCopy(output);

        Assert.Multiple(() =>
        {
            Assert.That(window.GetHistoryCount(), Is.EqualTo(256));
            Assert.That(tooFar,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(boundary, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(copy.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(output, Is.EqualTo(new byte[] { 44, 45, 46 }));
        });
    }

    [Test]
    public void ResetCanRetainOrDiscardHistoryAndAlwaysCancelsPendingCopy()
    {
        var window = new DeflateSlidingWindow(8);
        byte[] seed = [(byte)'A', (byte)'B', (byte)'C'];
        window.WriteBytes(seed, new byte[seed.Length]);
        window.BeginCopy(3, 3);

        window.ResetSlidingWindow(true);
        var retained = window.BeginCopy(3, 3);
        var output = new byte[3];
        var copy = window.ContinueCopy(output);
        window.ResetSlidingWindow(false);
        var discarded = window.BeginCopy(1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(retained, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(copy.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(output, Is.EqualTo(seed));
            Assert.That(discarded,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(window.GetHistoryCount(), Is.Zero);
            Assert.That(window.GetPendingCopyLength(), Is.Zero);
        });
    }

    [Test]
    public void LiteralWritesAreRejectedUntilPendingCopyIsDrained()
    {
        var window = new DeflateSlidingWindow(8);
        window.WriteBytes(new byte[] { 1 }, new byte[1]);
        window.BeginCopy(1, 3);

        var blocked = window.WriteBytes(new byte[] { 2 }, new byte[1]);
        var copy = window.ContinueCopy(new byte[3]);

        Assert.Multiple(() =>
        {
            Assert.That(blocked.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(blocked.ConsumedCount, Is.Zero);
            Assert.That(blocked.RemainingCopyLength, Is.EqualTo(3));
            Assert.That(copy.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }
}
