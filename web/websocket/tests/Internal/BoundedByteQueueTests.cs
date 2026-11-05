using websocket;

namespace websocket.Tests;

public class BoundedByteQueueTests
{
    [TestCase(0)]
    [TestCase(-1)]
    public void ConstructorRejectsNonPositiveCapacity(int capacity)
    {
        Assert.Throws<WebSocketLimitException>(() => new BoundedByteQueue(capacity));
    }

    [Test]
    public void NewQueueReportsItsFixedCapacity()
    {
        var queue = new BoundedByteQueue(5);

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetByteCapacity(), Is.EqualTo(5));
            Assert.That(queue.GetByteCount(), Is.Zero);
            Assert.That(queue.GetByteAvailableCapacity(), Is.EqualTo(5));
        });
    }

    [Test]
    public void OversizedWriteReportsAcceptedPrefixWithoutGrowing()
    {
        var queue = new BoundedByteQueue(4);
        var source = new byte[] { 1, 2, 3, 4, 5, 6 };

        var written = queue.WriteBytes(source);
        var writtenWhileFull = queue.WriteBytes(new byte[] { 9 });
        var destination = new byte[6];
        var read = queue.ReadBytes(destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(4));
            Assert.That(writtenWhileFull, Is.Zero);
            Assert.That(read, Is.EqualTo(4));
            Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 3, 4, 0, 0 }));
            Assert.That(queue.GetByteCapacity(), Is.EqualTo(4));
            Assert.That(queue.GetByteCount(), Is.Zero);
        });
    }

    [Test]
    public void QueuePreservesFifoOrderAcrossWraparound()
    {
        var queue = new BoundedByteQueue(5);
        Assert.That(queue.WriteBytes(new byte[] { 1, 2, 3, 4 }), Is.EqualTo(4));
        var firstDestination = new byte[3];
        Assert.That(queue.ReadBytes(firstDestination), Is.EqualTo(3));
        Assert.That(queue.WriteBytes(new byte[] { 5, 6, 7, 8 }), Is.EqualTo(4));
        var secondDestination = new byte[5];

        var read = queue.ReadBytes(secondDestination);

        Assert.Multiple(() =>
        {
            Assert.That(firstDestination, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(read, Is.EqualTo(5));
            Assert.That(secondDestination, Is.EqualTo(new byte[] { 4, 5, 6, 7, 8 }));
            Assert.That(queue.GetByteAvailableCapacity(), Is.EqualTo(5));
        });
    }

    [Test]
    public void PeekDoesNotConsumeAndHonorsDestinationLength()
    {
        var queue = new BoundedByteQueue(4);
        queue.WriteBytes(new byte[] { 10, 20, 30 });
        var peekDestination = new byte[2];

        var peeked = queue.PeekBytes(peekDestination);
        var readDestination = new byte[4];
        var read = queue.ReadBytes(readDestination);

        Assert.Multiple(() =>
        {
            Assert.That(peeked, Is.EqualTo(2));
            Assert.That(peekDestination, Is.EqualTo(new byte[] { 10, 20 }));
            Assert.That(read, Is.EqualTo(3));
            Assert.That(readDestination, Is.EqualTo(new byte[] { 10, 20, 30, 0 }));
        });
    }

    [Test]
    public void EmptyOperationsAndClearDoNotLoseCapacity()
    {
        var queue = new BoundedByteQueue(3);
        queue.WriteBytes(new byte[] { 1, 2 });

        Assert.That(queue.WriteBytes(ReadOnlySpan<byte>.Empty), Is.Zero);
        Assert.That(queue.ReadBytes(Span<byte>.Empty), Is.Zero);
        Assert.That(queue.GetByteCount(), Is.EqualTo(2));

        queue.ClearBytes();
        var destination = new byte[1];

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetByteCount(), Is.Zero);
            Assert.That(queue.GetByteAvailableCapacity(), Is.EqualTo(3));
            Assert.That(queue.ReadBytes(destination), Is.Zero);
            Assert.That(queue.WriteBytes(new byte[] { 9, 8, 7 }), Is.EqualTo(3));
        });
    }
}
