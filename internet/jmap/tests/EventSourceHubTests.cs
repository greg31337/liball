namespace jmap.Tests;

public class EventSourceHubTests
{
    [Test]
    public void PublicationAppliesSubscriberAndPublicationTypeFilters()
    {
        FixedEventSourceClock clock = new FixedEventSourceClock();
        EventSourceHub hub = Hub(clock, 4, 8);
        EventSourceSubscriber email = hub.Subscribe(
            new EventSourceOptions { Types = new List<string> { JmapDataTypes.Email } },
            CancellationToken.None);
        EventSourceSubscriber all = hub.Subscribe(
            new EventSourceOptions { Types = new List<string> { "*" } },
            CancellationToken.None);
        JmapStatePublication publication = Publication("event-1", "email-1");
        publication.Types = new List<string> { JmapDataTypes.Email, JmapDataTypes.Mailbox };
        publication.StateChange.Changed["account-1"][JmapDataTypes.Mailbox] = "mailbox-1";
        publication.StateChange.Changed["account-1"][JmapDataTypes.Identity] = "identity-1";

        hub.Publish(publication);
        EventSourceReadResult emailResult = hub.TryReadNext(email);
        EventSourceReadResult allResult = hub.TryReadNext(all);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(emailResult.Event!.StateChange!.Changed["account-1"], Is.EqualTo(new Dictionary<string, string>
            {
                [JmapDataTypes.Email] = "email-1"
            }));
            Assert.That(allResult.Event!.StateChange!.Changed["account-1"], Is.EqualTo(new Dictionary<string, string>
            {
                [JmapDataTypes.Email] = "email-1",
                [JmapDataTypes.Mailbox] = "mailbox-1"
            }));
            Assert.That(hub.GetSubscriberCount(), Is.EqualTo(2));
        }
    }

    [Test]
    public void CloseAfterStateDrainsFinalEventAndCleansUpSubscriber()
    {
        EventSourceHub hub = Hub(new FixedEventSourceClock(), 2, 4);
        EventSourceSubscriber subscriber = hub.Subscribe(
            new EventSourceOptions { CloseAfterState = true },
            CancellationToken.None);

        hub.Publish(Publication("event-1", "email-1"));
        EventSourceReadResult state = hub.TryReadNext(subscriber);
        EventSourceReadResult complete = hub.TryReadNext(subscriber);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.HasEvent, Is.True);
            Assert.That(state.Event!.Id, Is.EqualTo("event-1"));
            Assert.That(complete.IsComplete, Is.True);
            Assert.That(hub.GetSubscriberCount(), Is.Zero);
        }
    }

    [Test]
    public void PingIntervalIsClampedAndEmittedAfterInactivityWithoutAnId()
    {
        FixedEventSourceClock clock = new FixedEventSourceClock();
        JmapServerOptions options = new JmapServerOptions
        {
            MinimumEventSourcePingSeconds = 15,
            MaximumEventSourcePingSeconds = 400
        };
        EventSourceHub hub = new EventSourceHub(options, clock, 2, 4);
        EventSourceSubscriber subscriber = hub.Subscribe(
            new EventSourceOptions { PingIntervalSeconds = 1 },
            CancellationToken.None);

        clock.Now = clock.Now.AddSeconds(14);
        hub.EmitDuePings();
        Assert.That(hub.TryReadNext(subscriber).HasEvent, Is.False);
        clock.Now = clock.Now.AddSeconds(1);
        hub.EmitDuePings();
        EventSourceReadResult result = hub.TryReadNext(subscriber);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(subscriber.Options.PingIntervalSeconds, Is.EqualTo(15));
            Assert.That(result.Event!.Kind, Is.EqualTo(JmapEventKind.Ping));
            Assert.That(result.Event.Id, Is.Null);
            Assert.That(result.Event.PingIntervalSeconds, Is.EqualTo(15));
        }
    }

    [Test]
    public void LastEventIdReplaysMissedStateAndUnknownIdGetsCurrentSnapshot()
    {
        EventSourceHub hub = Hub(new FixedEventSourceClock(), 4, 8);
        hub.Publish(Publication("event-1", "email-1"));
        JmapStatePublication second = Publication("event-2", "email-2");
        second.StateChange.Changed["account-1"][JmapDataTypes.Mailbox] = "mailbox-2";
        hub.Publish(second);

        EventSourceSubscriber resumed = hub.Subscribe(
            new EventSourceOptions { LastEventId = "event-1" },
            CancellationToken.None);
        EventSourceSubscriber stale = hub.Subscribe(
            new EventSourceOptions { LastEventId = "expired-event" },
            CancellationToken.None);
        EventSourceSubscriber current = hub.Subscribe(
            new EventSourceOptions { LastEventId = "event-2" },
            CancellationToken.None);
        EventSourceSubscriber fresh = hub.Subscribe(
            new EventSourceOptions(),
            CancellationToken.None);
        EventSourceReadResult resumedResult = hub.TryReadNext(resumed);
        EventSourceReadResult staleResult = hub.TryReadNext(stale);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(resumedResult.Event!.Id, Is.EqualTo("event-2"));
            Assert.That(resumedResult.Event.StateChange!.Changed["account-1"][JmapDataTypes.Email], Is.EqualTo("email-2"));
            Assert.That(staleResult.Event!.Id, Is.EqualTo("event-2"));
            Assert.That(staleResult.Event.StateChange!.Changed["account-1"], Is.EqualTo(new Dictionary<string, string>
            {
                [JmapDataTypes.Email] = "email-2",
                [JmapDataTypes.Mailbox] = "mailbox-2"
            }));
            Assert.That(hub.TryReadNext(current).HasEvent, Is.False);
            Assert.That(hub.TryReadNext(fresh).HasEvent, Is.False);
        }
    }

    [Test]
    public void CancellationCompletesAndRemovesSubscriber()
    {
        EventSourceHub hub = Hub(new FixedEventSourceClock(), 2, 4);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        EventSourceSubscriber subscriber = hub.Subscribe(new EventSourceOptions(), cancellation.Token);

        cancellation.Cancel();
        EventSourceReadResult result = hub.TryReadNext(subscriber);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsComplete, Is.True);
            Assert.That(hub.GetSubscriberCount(), Is.Zero);
        }
    }

    [Test]
    public void BoundedBufferCoalescesStateToLatestValues()
    {
        FixedEventSourceClock clock = new FixedEventSourceClock();
        EventSourceHub hub = Hub(clock, 1, 8);
        EventSourceSubscriber subscriber = hub.Subscribe(new EventSourceOptions(), CancellationToken.None);

        hub.Publish(Publication("event-1", "email-1"));
        JmapStatePublication second = Publication("event-2", "email-2");
        second.StateChange.Changed["account-1"][JmapDataTypes.Mailbox] = "mailbox-2";
        hub.Publish(second);
        EventSourceReadResult result = hub.TryReadNext(subscriber);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(hub.GetBufferedEventCount(subscriber), Is.Zero);
            Assert.That(result.Event!.Id, Is.EqualTo("event-2"));
            Assert.That(result.Event.StateChange!.Changed["account-1"], Is.EqualTo(new Dictionary<string, string>
            {
                [JmapDataTypes.Email] = "email-2",
                [JmapDataTypes.Mailbox] = "mailbox-2"
            }));
        }
    }

    [Test]
    public void InvalidFiltersAndIntervalsAreRejected()
    {
        EventSourceHub hub = Hub(new FixedEventSourceClock(), 2, 4);

        Assert.That(
            () => hub.Subscribe(new EventSourceOptions { Types = new List<string>() }, CancellationToken.None),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => hub.Subscribe(new EventSourceOptions { Types = new List<string> { "*", JmapDataTypes.Email } }, CancellationToken.None),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => hub.Subscribe(new EventSourceOptions { PingIntervalSeconds = -1 }, CancellationToken.None),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => hub.Subscribe(new EventSourceOptions { LastEventId = "bad\nid" }, CancellationToken.None),
            Throws.TypeOf<ArgumentException>());
    }

    private static EventSourceHub Hub(FixedEventSourceClock clock, int buffer, int history)
    {
        return new EventSourceHub(new JmapServerOptions(), clock, buffer, history);
    }

    private static JmapStatePublication Publication(string eventId, string emailState)
    {
        JmapStatePublication publication = new JmapStatePublication { EventId = eventId };
        publication.StateChange.Changed["account-1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = emailState
        };
        return publication;
    }

    private class FixedEventSourceClock : IJmapClock
    {
        public DateTimeOffset Now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

        public DateTimeOffset GetCurrentUtcDateTime()
        {
            return Now;
        }
    }
}
