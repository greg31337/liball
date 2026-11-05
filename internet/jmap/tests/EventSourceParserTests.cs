using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class EventSourceParserTests
{
    [Test]
    public void ParsesStateEventWhenEveryUtf8ByteArrivesSeparately()
    {
        EventSourceParser parser = new EventSourceParser();
        string text = "\ufeffevent: state\r\nid: event-42\r\ndata: {\"@type\":\"StateChange\",\"changed\":{\"account-1\":{\"Email\":\"é😀\"}}}\r\n\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        List<JmapEvent> events = new List<JmapEvent>();

        int index = 0;
        while (index < bytes.Length)
        {
            events.AddRange(parser.Append(bytes, index, 1, false));
            index = index + 1;
        }

        events.AddRange(parser.Append(Array.Empty<byte>(), 0, 0, true));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(JmapEventKind.State));
            Assert.That(events[0].Name, Is.EqualTo("state"));
            Assert.That(events[0].Id, Is.EqualTo("event-42"));
            Assert.That(events[0].StateChange!.Changed["account-1"][JmapDataTypes.Email], Is.EqualTo("é😀"));
            Assert.That(parser.LastEventId, Is.EqualTo("event-42"));
        }
    }

    [Test]
    public void JoinsDataLinesAndCarriesIdWhileRetryMetadataAppliesToNextEvent()
    {
        EventSourceParser parser = new EventSourceParser();
        List<JmapEvent> events = ParseChunks(
            parser,
            new[]
            {
                "retry: 5000\nid: alpha\nevent: fut",
                "ure\ndata: first\r",
                "\ndata: second\r\n\r",
                "\ndata: third\n\n"
            },
            true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[0].Kind, Is.EqualTo(JmapEventKind.Unknown));
            Assert.That(events[0].Name, Is.EqualTo("future"));
            Assert.That(events[0].Id, Is.EqualTo("alpha"));
            Assert.That(events[0].Data, Is.EqualTo("first\nsecond"));
            Assert.That(events[0].RetryMilliseconds, Is.EqualTo(5000));
            Assert.That(events[0].HasRetryMilliseconds, Is.True);
            Assert.That(events[1].Name, Is.EqualTo("message"));
            Assert.That(events[1].Id, Is.EqualTo("alpha"));
            Assert.That(events[1].Data, Is.EqualTo("third"));
            Assert.That(events[1].HasRetryMilliseconds, Is.False);
            Assert.That(parser.ReconnectionDelayMilliseconds, Is.EqualTo(5000));
            Assert.That(parser.HasReconnectionDelay, Is.True);
        }
    }

    [Test]
    public void EmitsCommentsWithoutDisruptingAnEventAndAcceptsAllLineEndings()
    {
        EventSourceParser parser = new EventSourceParser();
        string text = ": first\r:second\n\r\nevent: future\ndata: before\n: between\rdata: after\r\r";

        List<JmapEvent> events = ParseChunks(parser, new[] { text }, true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(events.Select(item => item.Kind), Is.EqualTo(new[]
            {
                JmapEventKind.Comment,
                JmapEventKind.Comment,
                JmapEventKind.Comment,
                JmapEventKind.Unknown
            }));
            Assert.That(events[0].Comment, Is.EqualTo("first"));
            Assert.That(events[1].Comment, Is.EqualTo("second"));
            Assert.That(events[2].Comment, Is.EqualTo("between"));
            Assert.That(events[3].Data, Is.EqualTo("before\nafter"));
        }
    }

    [Test]
    public void ParsesPingAndEmptyDataEvents()
    {
        EventSourceParser parser = new EventSourceParser();
        List<JmapEvent> events = ParseChunks(
            parser,
            new[] { "event: ping\ndata: {\"interval\":30}\n\nevent: future\ndata\n\n" },
            true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[0].Kind, Is.EqualTo(JmapEventKind.Ping));
            Assert.That(events[0].PingIntervalSeconds, Is.EqualTo(30));
            Assert.That(events[0].HasPingIntervalSeconds, Is.True);
            Assert.That(events[1].Kind, Is.EqualTo(JmapEventKind.Unknown));
            Assert.That(events[1].Data, Is.EqualTo(""));
        }
    }

    [Test]
    public void IgnoresUnknownFieldsInvalidRetryAndIdsContainingNull()
    {
        EventSourceParser parser = new EventSourceParser();
        string text = "id: good\nevent: future\ndata: one\n\n" +
            "unknown: ignored\nretry: -1\nretry: 999999999999999999999999\nid: bad\0id\nevent: future\ndata: two\n\n";

        List<JmapEvent> events = ParseChunks(parser, new[] { text }, true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[1].Id, Is.EqualTo("good"));
            Assert.That(events[1].HasRetryMilliseconds, Is.False);
            Assert.That(parser.HasReconnectionDelay, Is.False);
        }
    }

    [Test]
    public void DiscardsAnIncompleteFinalEventAtEndOfStream()
    {
        EventSourceParser parser = new EventSourceParser();

        List<JmapEvent> events = ParseChunks(parser, new[] { "event: future\ndata: incomplete" }, true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(events, Is.Empty);
            Assert.That(parser.Completed, Is.True);
            Assert.That(
                () => parser.Append(Array.Empty<byte>(), 0, 0, false),
                Throws.TypeOf<InvalidOperationException>());
        }
    }

    [Test]
    public void MalformedUtf8AndRecognizedJsonPayloadsSurfaceProtocolFailures()
    {
        EventSourceParser utf8 = new EventSourceParser();
        EventSourceParser state = new EventSourceParser();

        Assert.That(
            () => utf8.Append(new byte[] { 0xF0, 0x28, 0x8C, 0x28 }, 0, 4, true),
            Throws.TypeOf<DecoderFallbackException>());
        Assert.That(
            () => ParseChunks(state, new[] { "event: state\ndata: not-json\n\n" }, false),
            Throws.InstanceOf<JsonException>());
    }

    [Test]
    public void OffsetAndCountLimitParsingToTheSelectedBytes()
    {
        EventSourceParser parser = new EventSourceParser();
        byte[] framed = Encoding.UTF8.GetBytes("ignoredata: selected\n\nignore");

        List<JmapEvent> events = parser.Append(framed, 6, framed.Length - 12, true);

        Assert.That(events.Single().Data, Is.EqualTo("selected"));
    }

    private static List<JmapEvent> ParseChunks(EventSourceParser parser, string[] chunks, bool finish)
    {
        List<JmapEvent> events = new List<JmapEvent>();
        foreach (string chunk in chunks)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(chunk);
            events.AddRange(parser.Append(bytes, 0, bytes.Length, false));
        }

        if (finish)
        {
            events.AddRange(parser.Append(Array.Empty<byte>(), 0, 0, true));
        }

        return events;
    }
}
