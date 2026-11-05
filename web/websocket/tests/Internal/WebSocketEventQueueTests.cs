using websocket;

namespace websocket.Tests;

public class WebSocketEventQueueTests
{
    [TestCase(0)]
    [TestCase(-1)]
    public void ConstructorRejectsNonPositiveCapacity(int capacity)
    {
        Assert.Throws<WebSocketLimitException>(() => new WebSocketEventQueue(capacity));
    }

    [Test]
    public void NewQueueReportsItsFixedCapacity()
    {
        var queue = new WebSocketEventQueue(3);

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetEventCapacity(), Is.EqualTo(3));
            Assert.That(queue.GetEventCount(), Is.Zero);
            Assert.That(queue.GetEventAvailableCapacity(), Is.EqualTo(3));
            Assert.That(queue.PeekEvent(), Is.Null);
            Assert.That(queue.DequeueEvent(), Is.Null);
        });
    }

    [Test]
    public void FullQueueReportsPressureWithoutDroppingOrReorderingEvents()
    {
        var queue = new WebSocketEventQueue(2);
        var first = CreateEvent(1);
        var second = CreateEvent(2);
        var rejected = CreateEvent(3);

        var firstAccepted = queue.TryEnqueueEvent(first);
        var secondAccepted = queue.TryEnqueueEvent(second);
        var thirdAccepted = queue.TryEnqueueEvent(rejected);

        Assert.Multiple(() =>
        {
            Assert.That(firstAccepted, Is.True);
            Assert.That(secondAccepted, Is.True);
            Assert.That(thirdAccepted, Is.False);
            Assert.That(queue.GetEventCount(), Is.EqualTo(2));
            Assert.That(queue.GetEventAvailableCapacity(), Is.Zero);
            Assert.That(queue.DequeueEvent(), Is.SameAs(first));
            Assert.That(queue.DequeueEvent(), Is.SameAs(second));
            Assert.That(queue.DequeueEvent(), Is.Null);
        });
    }

    [Test]
    public void PeekDoesNotRemoveTheEvent()
    {
        var queue = new WebSocketEventQueue(1);
        var connectionEvent = CreateEvent(7);
        queue.TryEnqueueEvent(connectionEvent);

        var peeked = queue.PeekEvent();

        Assert.Multiple(() =>
        {
            Assert.That(peeked, Is.SameAs(connectionEvent));
            Assert.That(queue.GetEventCount(), Is.EqualTo(1));
            Assert.That(queue.DequeueEvent(), Is.SameAs(connectionEvent));
        });
    }

    [Test]
    public void QueuePreservesFifoOrderAcrossWraparound()
    {
        var queue = new WebSocketEventQueue(3);
        var first = CreateEvent(1);
        var second = CreateEvent(2);
        var third = CreateEvent(3);
        var fourth = CreateEvent(4);
        queue.TryEnqueueEvent(first);
        queue.TryEnqueueEvent(second);
        queue.TryEnqueueEvent(third);

        Assert.That(queue.DequeueEvent(), Is.SameAs(first));
        Assert.That(queue.DequeueEvent(), Is.SameAs(second));
        Assert.That(queue.TryEnqueueEvent(fourth), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(queue.DequeueEvent(), Is.SameAs(third));
            Assert.That(queue.DequeueEvent(), Is.SameAs(fourth));
            Assert.That(queue.GetEventCount(), Is.Zero);
        });
    }

    [Test]
    public void ClearEmptiesQueueAndRestoresCapacity()
    {
        var queue = new WebSocketEventQueue(2);
        queue.TryEnqueueEvent(CreateEvent(1));
        queue.TryEnqueueEvent(CreateEvent(2));

        queue.ClearEvents();

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetEventCount(), Is.Zero);
            Assert.That(queue.GetEventAvailableCapacity(), Is.EqualTo(2));
            Assert.That(queue.PeekEvent(), Is.Null);
            Assert.That(queue.DequeueEvent(), Is.Null);
        });
    }

    [Test]
    public void NullEventIsRejectedWithoutChangingQueue()
    {
        var queue = new WebSocketEventQueue(2);

        Assert.Throws<WebSocketStateException>(() => queue.TryEnqueueEvent(null));
        Assert.That(queue.GetEventCount(), Is.Zero);
    }

    static WebSocketEvent CreateEvent(int payloadCount)
    {
        var connectionEvent = new WebSocketEvent();
        connectionEvent.PayloadCount = payloadCount;
        return connectionEvent;
    }
}
