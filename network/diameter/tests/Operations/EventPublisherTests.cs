using System.Collections.Generic;
using System.Threading;

namespace diameter.Tests;

public class EventPublisherTests
{
    [Test]
    public void PublicationAssignsEnvelopeAndRedactsOwnedEvent()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        RecordingEventSink sink = new RecordingEventSink();
        EventRedactionPolicyProbe redaction =
            new EventRedactionPolicyProbe();
        EventPublisher publisher =
            new EventPublisher(clock, sink, redaction);
        DiameterOperationalEvent source = new DiameterOperationalEvent();
        source.Source = "  request-client  ";
        source.Code = "request.sent";
        source.Description = "private payload";
        source.SequenceNumber = 99;

        publisher.PublishDiameterEvent(source);

        Assert.That(publisher.WaitForDiameterEvents(5000), Is.True);
        DiameterOperationalEvent published = sink.ReadEvents()[0];
        Assert.Multiple(() =>
        {
            Assert.That(published.Source, Is.EqualTo("request-client"));
            Assert.That(published.Code, Is.EqualTo("request.sent"));
            Assert.That(published.Description, Is.EqualTo("[redacted]"));
            Assert.That(published.Redacted, Is.True);
            Assert.That(published.SequenceNumber, Is.EqualTo(1));
            Assert.That(published.OccurredAt, Is.EqualTo(clock.UtcTime));
            Assert.That(source.Source, Is.EqualTo("  request-client  "));
            Assert.That(source.Description, Is.EqualTo("private payload"));
            Assert.That(source.SequenceNumber, Is.EqualTo(99));
            Assert.That(source.OccurredAt, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(publisher.ReadDiameterEventPublicationCount(),
                Is.EqualTo(1));
            Assert.That(publisher.ReadDiameterEventDeliveryCount(),
                Is.EqualTo(1));
            Assert.That(publisher.ReadDiameterEventFailureCount(), Is.Zero);
        });
    }

    [Test]
    public void ConcurrentPublicationPreservesSequenceOrderPerSource()
    {
        RecordingEventSink sink = new RecordingEventSink();
        EventPublisher publisher = new EventPublisher(
            new FakeDiscoveryClock(),
            sink,
            null);

        Parallel.For(
            0,
            400,
            index =>
            {
                DiameterOperationalEvent eventData =
                    new DiameterOperationalEvent();
                if (index % 2 == 0)
                {
                    eventData.Source = "peer";
                }
                else
                {
                    eventData.Source = "session";
                }

                eventData.Code = "state.changed";
                publisher.PublishDiameterEvent(eventData);
            });

        Assert.That(publisher.WaitForDiameterEvents(5000), Is.True);
        List<DiameterOperationalEvent> events = sink.ReadEvents();
        long peerSequence = 0;
        long sessionSequence = 0;
        int eventIndex = 0;
        while (eventIndex < events.Count)
        {
            DiameterOperationalEvent eventData = events[eventIndex];
            if (eventData.Source == "peer")
            {
                peerSequence = peerSequence + 1L;
                Assert.That(eventData.SequenceNumber,
                    Is.EqualTo(peerSequence));
            }
            else
            {
                sessionSequence = sessionSequence + 1L;
                Assert.That(eventData.Source, Is.EqualTo("session"));
                Assert.That(eventData.SequenceNumber,
                    Is.EqualTo(sessionSequence));
            }

            eventIndex = eventIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(400));
            Assert.That(peerSequence, Is.EqualTo(200));
            Assert.That(sessionSequence, Is.EqualTo(200));
            Assert.That(publisher.ReadDiameterEventDeliveryCount(),
                Is.EqualTo(400));
        });
    }

    [Test]
    public void SinkFailureIsIsolatedAndLaterEventsContinue()
    {
        RecordingEventSink sink = new RecordingEventSink();
        sink.ThrowCode = "event.fail";
        EventPublisher publisher = new EventPublisher(
            new FakeDiscoveryClock(),
            sink,
            null);
        DiameterOperationalEvent failing = CreateEvent(
            "routing",
            "event.fail");
        DiameterOperationalEvent succeeding = CreateEvent(
            "routing",
            "event.succeed");

        publisher.PublishDiameterEvent(failing);
        publisher.PublishDiameterEvent(succeeding);

        Assert.That(publisher.WaitForDiameterEvents(5000), Is.True);
        List<DiameterOperationalEvent> delivered = sink.ReadEvents();
        Assert.Multiple(() =>
        {
            Assert.That(delivered, Has.Count.EqualTo(1));
            Assert.That(delivered[0].Code, Is.EqualTo("event.succeed"));
            Assert.That(delivered[0].SequenceNumber, Is.EqualTo(2));
            Assert.That(publisher.ReadDiameterEventPublicationCount(),
                Is.EqualTo(2));
            Assert.That(publisher.ReadDiameterEventDeliveryCount(),
                Is.EqualTo(1));
            Assert.That(publisher.ReadDiameterEventFailureCount(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void RedactionFailuresAreIsolatedAndLaterEventsContinue()
    {
        RecordingEventSink sink = new RecordingEventSink();
        EventRedactionPolicyProbe redaction =
            new EventRedactionPolicyProbe();
        redaction.ThrowCode = "redaction.throw";
        redaction.NullCode = "redaction.null";
        EventPublisher publisher = new EventPublisher(
            new FakeDiscoveryClock(),
            sink,
            redaction);

        publisher.PublishDiameterEvent(CreateEvent(
            "privacy",
            "redaction.throw"));
        publisher.PublishDiameterEvent(CreateEvent(
            "privacy",
            "redaction.null"));
        publisher.PublishDiameterEvent(CreateEvent(
            "privacy",
            "redaction.succeed"));

        Assert.That(publisher.WaitForDiameterEvents(5000), Is.True);
        List<DiameterOperationalEvent> delivered = sink.ReadEvents();
        Assert.Multiple(() =>
        {
            Assert.That(delivered, Has.Count.EqualTo(1));
            Assert.That(delivered[0].Code,
                Is.EqualTo("redaction.succeed"));
            Assert.That(delivered[0].SequenceNumber, Is.EqualTo(3));
            Assert.That(delivered[0].Redacted, Is.True);
            Assert.That(publisher.ReadDiameterEventPublicationCount(),
                Is.EqualTo(3));
            Assert.That(publisher.ReadDiameterEventDeliveryCount(),
                Is.EqualTo(1));
            Assert.That(publisher.ReadDiameterEventFailureCount(),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void PublicationWithoutSinkCompletesDeterministically()
    {
        EventPublisher publisher = new EventPublisher(
            new FakeDiscoveryClock(),
            null,
            null);

        publisher.PublishDiameterEvent(CreateEvent(
            "metrics",
            "snapshot.created"));

        Assert.Multiple(() =>
        {
            Assert.That(publisher.WaitForDiameterEvents(5000), Is.True);
            Assert.That(publisher.ReadDiameterEventPublicationCount(),
                Is.EqualTo(1));
            Assert.That(publisher.ReadDiameterEventDeliveryCount(),
                Is.EqualTo(1));
            Assert.That(publisher.ReadDiameterEventFailureCount(),
                Is.Zero);
        });
    }

    [Test]
    public void RedactionAndSinkCallbacksAreOutsideLockAndCanReenter()
    {
        RecordingEventSink sink = new RecordingEventSink();
        EventRedactionPolicyProbe redaction =
            new EventRedactionPolicyProbe();
        EventPublisher publisher = new EventPublisher(
            new FakeDiscoveryClock(),
            sink,
            redaction);
        sink.Publisher = publisher;
        sink.PublisherLock = publisher.EventLock;
        sink.ReenterCode = "event.outer";
        redaction.PublisherLock = publisher.EventLock;

        publisher.PublishDiameterEvent(CreateEvent(
            "peer",
            "event.outer"));

        Assert.That(publisher.WaitForDiameterEvents(5000), Is.True);
        List<DiameterOperationalEvent> delivered = sink.ReadEvents();
        Assert.Multiple(() =>
        {
            Assert.That(redaction.CallbackObservedLock, Is.False);
            Assert.That(sink.CallbackObservedLock, Is.False);
            Assert.That(delivered, Has.Count.EqualTo(2));
            Assert.That(delivered[0].Code, Is.EqualTo("event.outer"));
            Assert.That(delivered[0].SequenceNumber, Is.EqualTo(1));
            Assert.That(delivered[1].Code, Is.EqualTo("event.inner"));
            Assert.That(delivered[1].SequenceNumber, Is.EqualTo(2));
        });
    }

    [Test]
    public void InvalidPublisherInputsAreRejected()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        EventPublisher publisher = new EventPublisher(clock, null, null);
        DiameterOperationalEvent missingSource =
            new DiameterOperationalEvent();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                new EventPublisher(null!, null, null));
            Assert.Throws<ArgumentException>(() =>
                publisher.PublishDiameterEvent(null));
            Assert.Throws<ArgumentException>(() =>
                publisher.PublishDiameterEvent(missingSource));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                publisher.WaitForDiameterEvents(-1));
        });
    }

    private static DiameterOperationalEvent CreateEvent(
        string source,
        string code)
    {
        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.Source = source;
        eventData.Code = code;
        return eventData;
    }
}

internal class RecordingEventSink : IDiameterEventSink
{
    internal object SinkLock = new object();
    internal List<DiameterOperationalEvent> Events =
        new List<DiameterOperationalEvent>();
    internal string ThrowCode = string.Empty;
    internal string ReenterCode = string.Empty;
    internal EventPublisher? Publisher;
    internal object? PublisherLock;
    internal bool CallbackObservedLock;

    public void HandleDiameterEvent(DiameterOperationalEvent eventData)
    {
        if (PublisherLock != null && Monitor.IsEntered(PublisherLock))
        {
            CallbackObservedLock = true;
        }

        if (eventData.Code == ThrowCode)
        {
            throw new InvalidOperationException("Sink failure.");
        }

        lock (SinkLock)
        {
            Events.Add(eventData);
        }

        if (eventData.Code == ReenterCode && Publisher != null)
        {
            DiameterOperationalEvent reentrant =
                new DiameterOperationalEvent();
            reentrant.Source = eventData.Source;
            reentrant.Code = "event.inner";
            Publisher.PublishDiameterEvent(reentrant);
        }
    }

    internal List<DiameterOperationalEvent> ReadEvents()
    {
        lock (SinkLock)
        {
            return new List<DiameterOperationalEvent>(Events);
        }
    }
}

internal class EventRedactionPolicyProbe : IRedactionPolicy
{
    internal object? PublisherLock;
    internal bool CallbackObservedLock;
    internal string ThrowCode = string.Empty;
    internal string NullCode = string.Empty;

    public DiameterOperationalEvent RedactDiameterEvent(
        DiameterOperationalEvent eventData)
    {
        if (PublisherLock != null && Monitor.IsEntered(PublisherLock))
        {
            CallbackObservedLock = true;
        }

        if (eventData.Code == ThrowCode)
        {
            throw new InvalidOperationException(
                "Redaction failure.");
        }

        if (eventData.Code == NullCode)
        {
            return null!;
        }

        DiameterOperationalEvent redacted =
            eventData.CloneDiameterOperationalEvent();
        redacted.Description = "[redacted]";
        redacted.Redacted = true;
        return redacted;
    }
}
