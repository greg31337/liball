using System.Text;

namespace jmap.Tests;

public class EventSourceSerializerTests
{
    [Test]
    public void OversizedEventsAndFieldsAreRejectedBeforeTheyReachAResponseQueue()
    {
        EventSourceSerializer serializer = new EventSourceSerializer();
        JmapEvent data = new JmapEvent
        {
            Kind = JmapEventKind.Unknown,
            Name = "large",
            Data = new string('x', EventSourceParser.MaximumEventCharacters + 1)
        };
        JmapEvent identifier = new JmapEvent
        {
            Kind = JmapEventKind.Unknown,
            Id = new string('i', EventSourceParser.MaximumLineCharacters + 1),
            Data = "{}"
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => serializer.Serialize(data),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => serializer.Serialize(identifier),
                Throws.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void StateEventRoundTripsThroughIncrementalParser()
    {
        StateChange change = new StateChange();
        change.Changed["account-1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = "email-é😀"
        };
        JmapEvent value = new JmapEvent
        {
            Kind = JmapEventKind.State,
            Id = "event-42",
            StateChange = change
        };
        EventSourceSerializer serializer = new EventSourceSerializer();
        EventSourceParser parser = new EventSourceParser();

        byte[] bytes = serializer.Serialize(value);
        List<JmapEvent> events = parser.Append(bytes, 0, bytes.Length, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Encoding.UTF8.GetString(bytes), Does.StartWith("id: event-42\nevent: state\ndata: "));
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(JmapEventKind.State));
            Assert.That(events[0].Id, Is.EqualTo("event-42"));
            Assert.That(events[0].StateChange!.Changed["account-1"][JmapDataTypes.Email], Is.EqualTo("email-é😀"));
        }
    }

    [Test]
    public void UnknownMultilineEventAndCommentUseSafeSseFields()
    {
        EventSourceSerializer serializer = new EventSourceSerializer();
        EventSourceParser parser = new EventSourceParser();
        JmapEvent value = new JmapEvent
        {
            Kind = JmapEventKind.Unknown,
            Name = "future",
            Id = "next",
            Data = "first\r\nsecond\rthird\n",
            RetryMilliseconds = 2500,
            HasRetryMilliseconds = true
        };
        JmapEvent comment = new JmapEvent
        {
            Kind = JmapEventKind.Comment,
            Comment = "one\ntwo"
        };

        byte[] eventBytes = serializer.Serialize(value);
        byte[] commentBytes = serializer.Serialize(comment);
        List<JmapEvent> events = parser.Append(eventBytes, 0, eventBytes.Length, false);
        events.AddRange(parser.Append(commentBytes, 0, commentBytes.Length, false));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(events[0].Data, Is.EqualTo("first\nsecond\nthird\n"));
            Assert.That(events[0].RetryMilliseconds, Is.EqualTo(2500));
            Assert.That(events[0].HasRetryMilliseconds, Is.True);
            Assert.That(events[1].Comment, Is.EqualTo("one"));
            Assert.That(events[2].Comment, Is.EqualTo("two"));
        }
    }

    [Test]
    public void PingSerializationNeverSetsAnEventIdentifier()
    {
        EventSourceSerializer serializer = new EventSourceSerializer();
        JmapEvent ping = new JmapEvent
        {
            Kind = JmapEventKind.Ping,
            Id = "must-not-be-sent",
            PingIntervalSeconds = 30,
            HasPingIntervalSeconds = true
        };
        EventSourceParser parser = new EventSourceParser();

        byte[] bytes = serializer.Serialize(ping);
        List<JmapEvent> events = parser.Append(bytes, 0, bytes.Length, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Encoding.UTF8.GetString(bytes), Does.Not.Contain("id:"));
            Assert.That(events.Single().Kind, Is.EqualTo(JmapEventKind.Ping));
            Assert.That(events.Single().Id, Is.Null);
            Assert.That(events.Single().PingIntervalSeconds, Is.EqualTo(30));
        }
    }

    [Test]
    public void SerializerRejectsFieldInjectionAndNegativeIntervals()
    {
        EventSourceSerializer serializer = new EventSourceSerializer();

        Assert.That(
            () => serializer.Serialize(new JmapEvent { Kind = JmapEventKind.Unknown, Name = "future\ndata: injected", Data = "safe" }),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => serializer.Serialize(new JmapEvent { Kind = JmapEventKind.Unknown, Name = "future", Id = "bad\0id", Data = "safe" }),
            Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => serializer.Serialize(new JmapEvent { Kind = JmapEventKind.Ping, PingIntervalSeconds = -1, HasPingIntervalSeconds = true }),
            Throws.TypeOf<ArgumentException>());
    }
}
