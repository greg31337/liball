using System.Text;

namespace http.Tests;

public class Http1FragmentationMatrixTests
{
    [TestCase("none")]
    [TestCase("fixed")]
    [TestCase("chunked")]
    public void RequestFramingsResumeAtEveryTwoFeedBoundary(string selected)
    {
        byte[] wire;
        byte[] expectedContent;
        bool expectsTrailers;
        switch (selected)
        {
            case "fixed":
                wire = Encoding.ASCII.GetBytes(
                    "POST /fixed HTTP/1.1\r\nHost: example.test\r\n" +
                    "Content-Length: 6\r\nX-Test: value\r\n\r\nabcdef");
                expectedContent = "abcdef"u8.ToArray();
                expectsTrailers = false;
                break;
            case "chunked":
                wire = Encoding.ASCII.GetBytes(
                    "POST /chunked HTTP/1.1\r\nHost: example.test\r\n" +
                    "Transfer-Encoding: chunked\r\nTrailer: Digest\r\n\r\n" +
                    "3;name=value\r\nabc\r\n3\r\ndef\r\n0\r\nDigest: final\r\n\r\n");
                expectedContent = "abcdef"u8.ToArray();
                expectsTrailers = true;
                break;
            default:
                wire = Encoding.ASCII.GetBytes(
                    "GET /none?q=1 HTTP/1.1\r\nHost: example.test\r\n" +
                    "X-Test: value\r\n\r\n");
                expectedContent = Array.Empty<byte>();
                expectsTrailers = false;
                break;
        }

        int split = 0;
        while (split <= wire.Length)
        {
            IHttp1Parser parser = new Http1Parser();
            Http1ParseResult first = parser.ParseHttp1Bytes(
                CopyRange(wire, 0, split));
            Http1ParseResult second = parser.ParseHttp1Bytes(
                CopyRange(wire, split, wire.Length - split));
            List<Http1ParseEvent> events = CombinedEvents(first, second);

            Assert.Multiple(() =>
            {
                Assert.That(first.Consumed, Is.EqualTo(split),
                    selected + " first feed at " + split);
                Assert.That(second.Consumed, Is.EqualTo(wire.Length - split),
                    selected + " second feed at " + split);
                Assert.That(first.Unconsumed, Is.Empty,
                    selected + " first surplus at " + split);
                Assert.That(second.Unconsumed, Is.Empty,
                    selected + " second surplus at " + split);
                Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.Done),
                    selected + " status at " + split);
                Assert.That(events.Count(item =>
                    item.EventKind == Http1ParseEventKind.MessageHead), Is.EqualTo(1),
                    selected + " head events at " + split);
                Assert.That(events.Count(item =>
                    item.EventKind == Http1ParseEventKind.Complete), Is.EqualTo(1),
                    selected + " complete events at " + split);
                Assert.That(events.Last().Request!.Content,
                    Is.EqualTo(expectedContent), selected + " content at " + split);
                Assert.That(events.Any(item =>
                    item.EventKind == Http1ParseEventKind.Trailers),
                    Is.EqualTo(expectsTrailers), selected + " trailers at " + split);
                if (expectsTrailers)
                {
                    Assert.That(Value(events.Last().Request!.Trailers, "Digest"),
                        Is.EqualTo("final"), selected + " trailer value at " + split);
                }
            });

            split = split + 1;
        }
    }

    [TestCase("fixed")]
    [TestCase("chunked")]
    [TestCase("informational")]
    public void ResponseFramingsResumeAtEveryTwoFeedBoundary(string selected)
    {
        byte[] wire;
        byte[] expectedContent;
        int expectedStatus;
        HttpMessageKind expectedKind;
        switch (selected)
        {
            case "chunked":
                wire = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n" +
                    "Trailer: Digest\r\n\r\n2\r\nab\r\n2\r\ncd\r\n" +
                    "0\r\nDigest: final\r\n\r\n");
                expectedContent = "abcd"u8.ToArray();
                expectedStatus = HttpStatusCodes.Ok;
                expectedKind = HttpMessageKind.Response;
                break;
            case "informational":
                wire = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 103 Early Hints\r\n" +
                    "Link: </style.css>; rel=preload\r\n\r\n");
                expectedContent = Array.Empty<byte>();
                expectedStatus = HttpStatusCodes.EarlyHints;
                expectedKind = HttpMessageKind.InformationalResponse;
                break;
            default:
                wire = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\nContent-Length: 4\r\n" +
                    "X-Test: value\r\n\r\nabcd");
                expectedContent = "abcd"u8.ToArray();
                expectedStatus = HttpStatusCodes.Ok;
                expectedKind = HttpMessageKind.Response;
                break;
        }

        int split = 0;
        while (split <= wire.Length)
        {
            IHttp1Parser parser = ResponseParser(HttpMethods.Get);
            Http1ParseResult first = parser.ParseHttp1Bytes(
                CopyRange(wire, 0, split));
            Http1ParseResult second = parser.ParseHttp1Bytes(
                CopyRange(wire, split, wire.Length - split));
            List<Http1ParseEvent> events = CombinedEvents(first, second);
            Http1ParseEvent head = events.Single(item =>
                item.EventKind == Http1ParseEventKind.MessageHead);

            Assert.Multiple(() =>
            {
                Assert.That(first.Consumed, Is.EqualTo(split),
                    selected + " first feed at " + split);
                Assert.That(second.Consumed, Is.EqualTo(wire.Length - split),
                    selected + " second feed at " + split);
                Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.Done),
                    selected + " status at " + split);
                Assert.That(events.Count(item =>
                    item.EventKind == Http1ParseEventKind.MessageHead), Is.EqualTo(1),
                    selected + " head events at " + split);
                Assert.That(events.Count(item =>
                    item.EventKind == Http1ParseEventKind.Complete), Is.EqualTo(1),
                    selected + " complete events at " + split);
                Assert.That(head.Response!.StatusCode, Is.EqualTo(expectedStatus),
                    selected + " response status at " + split);
                Assert.That(head.MessageKind, Is.EqualTo(expectedKind),
                    selected + " message kind at " + split);
                Assert.That(head.Response.Content, Is.EqualTo(expectedContent),
                    selected + " content at " + split);
            });

            split = split + 1;
        }
    }

    [TestCase("request-fixed")]
    [TestCase("request-chunked")]
    [TestCase("response-fixed")]
    [TestCase("response-chunked")]
    public void RepresentativeBodiesParseOneByteAtATime(string selected)
    {
        bool responseMode = selected.StartsWith("response", StringComparison.Ordinal);
        bool chunked = selected.EndsWith("chunked", StringComparison.Ordinal);
        string startLine = responseMode ?
            "HTTP/1.1 200 OK\r\n" : "POST / HTTP/1.1\r\nHost: example.test\r\n";
        string framing = chunked ?
            "Transfer-Encoding: chunked\r\n\r\n1\r\na\r\n2\r\nbc\r\n0\r\n\r\n" :
            "Content-Length: 3\r\n\r\nabc";
        byte[] wire = Encoding.ASCII.GetBytes(startLine + framing);
        IHttp1Parser parser = responseMode ?
            ResponseParser(HttpMethods.Get) : new Http1Parser();
        List<Http1ParseEvent> events = new List<Http1ParseEvent>();

        int index = 0;
        Http1ParseResult result = new Http1ParseResult();
        while (index < wire.Length)
        {
            result = parser.ParseHttp1Bytes(new byte[] { wire[index] });
            events.AddRange(result.Events);
            Assert.That(result.Consumed, Is.EqualTo(1),
                selected + " consumed byte " + index);
            Assert.That(result.Unconsumed, Is.Empty,
                selected + " surplus byte " + index);
            index = index + 1;
        }

        Http1ParseEvent head = events.Single(item =>
            item.EventKind == Http1ParseEventKind.MessageHead);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(events.Count(item =>
                item.EventKind == Http1ParseEventKind.MessageHead), Is.EqualTo(1));
            Assert.That(events.Count(item =>
                item.EventKind == Http1ParseEventKind.Complete), Is.EqualTo(1));
            Assert.That(responseMode ? head.Response!.Content : head.Request!.Content,
                Is.EqualTo("abc"u8.ToArray()));
        });
    }

    [Test]
    public void CloseDelimitedResponsesResumeAtEveryBoundaryAndCompleteAtEndInput()
    {
        byte[] wire = Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nServer: example\r\n\r\nabcdef");

        int split = 0;
        while (split <= wire.Length)
        {
            IHttp1Parser parser = ResponseParser(HttpMethods.Get);
            Http1ParseResult first = parser.ParseHttp1Bytes(
                CopyRange(wire, 0, split));
            Http1ParseResult second = parser.ParseHttp1Bytes(
                CopyRange(wire, split, wire.Length - split));
            Http1ParseResult completed = parser.CompleteHttp1Input();
            List<Http1ParseEvent> events = CombinedEvents(first, second);
            events.AddRange(completed.Events);

            Assert.Multiple(() =>
            {
                Assert.That(first.Consumed, Is.EqualTo(split),
                    "first feed at " + split);
                Assert.That(second.Consumed, Is.EqualTo(wire.Length - split),
                    "second feed at " + split);
                Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.NeedInput),
                    "status at " + split);
                Assert.That(second.NeedsEndOfInput, Is.True,
                    "end-input flag at " + split);
                Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done),
                    "completion at " + split);
                Assert.That(events.Count(item =>
                    item.EventKind == Http1ParseEventKind.MessageHead), Is.EqualTo(1),
                    "head events at " + split);
                Assert.That(events.Count(item =>
                    item.EventKind == Http1ParseEventKind.Complete), Is.EqualTo(1),
                    "complete events at " + split);
                Assert.That(events.Last().Response!.Content,
                    Is.EqualTo("abcdef"u8.ToArray()), "content at " + split);
            });

            split = split + 1;
        }
    }

    [Test]
    public void ThreePipelinedRequestsDrainThroughResetWithoutByteLoss()
    {
        byte[][] messages =
        {
            "GET /one HTTP/1.1\r\nHost: example.test\r\n\r\n"u8.ToArray(),
            "POST /two HTTP/1.1\r\nHost: example.test\r\n"u8.ToArray()
                .Concat("Content-Length: 3\r\n\r\ntwo"u8.ToArray()).ToArray(),
            "GET /three HTTP/1.0\r\n\r\n"u8.ToArray()
        };
        byte[] remaining = messages.SelectMany(item => item).ToArray();
        List<string> targets = new List<string>();
        int consumed = 0;
        int messageIndex = 0;
        IHttp1Parser parser = new Http1Parser();

        while (messageIndex < messages.Length)
        {
            Http1ParseResult result = parser.ParseHttp1Bytes(remaining);
            consumed = consumed + result.Consumed;
            targets.Add(result.Events[0].Request!.Target);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
                Assert.That(result.Consumed, Is.EqualTo(messages[messageIndex].Length));
                Assert.That(result.Events.Count(item =>
                    item.EventKind == Http1ParseEventKind.MessageHead), Is.EqualTo(1));
                Assert.That(result.Events.Count(item =>
                    item.EventKind == Http1ParseEventKind.Complete), Is.EqualTo(1));
            });

            remaining = result.Unconsumed;
            messageIndex = messageIndex + 1;
            if (messageIndex < messages.Length)
            {
                parser.ResetHttp1Parser(new Http1ParserOptions());
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(consumed, Is.EqualTo(messages.Sum(item => item.Length)));
            Assert.That(remaining, Is.Empty);
            Assert.That(targets, Is.EqualTo(new[] { "/one", "/two", "/three" }));
        });
    }

    [Test]
    public void ResetAtEveryPartialBoundaryDiscardsBufferedState()
    {
        byte[] abandoned = Encoding.ASCII.GetBytes(
            "POST /abandoned HTTP/1.1\r\nContent-Length: 8\r\n\r\npartial");
        byte[] replacement = Encoding.ASCII.GetBytes(
            "GET /replacement HTTP/1.1\r\nHost: example.test\r\n\r\n");

        int split = 0;
        while (split < abandoned.Length)
        {
            IHttp1Parser parser = new Http1Parser();
            Http1ParseResult partial = parser.ParseHttp1Bytes(
                CopyRange(abandoned, 0, split));
            parser.ResetHttp1Parser(new Http1ParserOptions());
            Http1ParseResult result = parser.ParseHttp1Bytes(replacement);

            Assert.Multiple(() =>
            {
                Assert.That(partial.Consumed, Is.EqualTo(split),
                    "partial consumed at " + split);
                Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done),
                    "replacement status at " + split);
                Assert.That(result.Consumed, Is.EqualTo(replacement.Length),
                    "replacement consumed at " + split);
                Assert.That(result.Events[0].Request!.Target,
                    Is.EqualTo("/replacement"), "replacement target at " + split);
                Assert.That(result.Events[0].Request!.Content, Is.Empty,
                    "replacement content at " + split);
            });

            split = split + 1;
        }
    }

    [TestCase("fixed", "incomplete-http1-content")]
    [TestCase("chunked", "incomplete-http1-chunked-content")]
    public void EndInputRejectsEveryTruncatedBodyBoundary(
        string selected,
        string errorCode)
    {
        byte[] wire;
        int bodyStart;
        switch (selected)
        {
            case "chunked":
                string chunkedHead =
                    "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n";
                wire = Encoding.ASCII.GetBytes(chunkedHead +
                    "3\r\nabc\r\n0\r\nDigest: final\r\n\r\n");
                bodyStart = Encoding.ASCII.GetByteCount(chunkedHead);
                break;
            default:
                string fixedHead =
                    "POST / HTTP/1.1\r\nContent-Length: 6\r\n\r\n";
                wire = Encoding.ASCII.GetBytes(fixedHead + "abcdef");
                bodyStart = Encoding.ASCII.GetByteCount(fixedHead);
                break;
        }

        int split = bodyStart;
        while (split < wire.Length)
        {
            IHttp1Parser parser = new Http1Parser();
            Http1ParseResult partial = parser.ParseHttp1Bytes(
                CopyRange(wire, 0, split));
            Http1ParseResult ended = parser.CompleteHttp1Input();
            Http1ParseResult terminal = parser.ParseHttp1Bytes(
                CopyRange(wire, split, wire.Length - split));

            Assert.Multiple(() =>
            {
                Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput),
                    selected + " partial at " + split);
                Assert.That(ended.Status, Is.EqualTo(HttpOperationStatus.Rejected),
                    selected + " ended at " + split);
                Assert.That(ended.ErrorCode, Is.EqualTo(errorCode),
                    selected + " error at " + split);
                Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal),
                    selected + " terminal at " + split);
                Assert.That(terminal.Consumed, Is.Zero,
                    selected + " terminal consumed at " + split);
                Assert.That(terminal.Unconsumed,
                    Is.EqualTo(CopyRange(wire, split, wire.Length - split)),
                    selected + " terminal surplus at " + split);
            });

            split = split + 1;
        }
    }

    static IHttp1Parser ResponseParser(string requestMethod)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = requestMethod;
        IHttp1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser;
    }

    static List<Http1ParseEvent> CombinedEvents(
        Http1ParseResult first,
        Http1ParseResult second)
    {
        List<Http1ParseEvent> result = new List<Http1ParseEvent>();
        result.AddRange(first.Events);
        result.AddRange(second.Events);
        return result;
    }

    static byte[] CopyRange(byte[] source, int start, int count)
    {
        byte[] result = new byte[count];
        Array.Copy(source, start, result, 0, count);
        return result;
    }

    static string Value(HttpHeaders headers, string name)
    {
        HttpHeaderField field = headers.Fields.Single(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        return Encoding.ASCII.GetString(field.Value);
    }
}
