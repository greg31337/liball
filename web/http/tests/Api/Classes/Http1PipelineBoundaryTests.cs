using System.Text;

namespace http.Tests;

public class Http1PipelineBoundaryTests
{
    [Test]
    public void OneFeedReturnsTheEntireNextRequestWithoutParsingIt()
    {
        byte[] first =
            "GET /first HTTP/1.1\r\nHost: example.test\r\n\r\n"u8.ToArray();
        byte[] second =
            "GET /second HTTP/1.1\r\nHost: example.test\r\n\r\n"u8.ToArray();
        byte[] pipeline = Join(first, second);
        Http1Parser parser = new Http1Parser();

        Http1ParseResult result = parser.ParseHttp1Bytes(pipeline);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(first.Length));
            Assert.That(result.Unconsumed, Is.EqualTo(second));
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(result.Events[0].Request!.Target, Is.EqualTo("/first"));
        });

        pipeline[first.Length] = (byte)'X';
        Assert.That(result.Unconsumed, Is.EqualTo(second));
    }

    [Test]
    public void ReturnedPipelineBytesCanBeParsedAfterAnExplicitReset()
    {
        byte[] first = "GET /one HTTP/1.1\r\n\r\n"u8.ToArray();
        byte[] second = "GET /two HTTP/1.1\r\n\r\n"u8.ToArray();
        Http1Parser parser = new Http1Parser();
        Http1ParseResult firstResult = parser.ParseHttp1Bytes(Join(first, second));

        parser.ResetHttp1Parser(new Http1ParserOptions());
        Http1ParseResult secondResult =
            parser.ParseHttp1Bytes(firstResult.Unconsumed);

        Assert.Multiple(() =>
        {
            Assert.That(secondResult.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(secondResult.Consumed, Is.EqualTo(second.Length));
            Assert.That(secondResult.Unconsumed, Is.Empty);
            Assert.That(secondResult.Events[0].Request!.Target, Is.EqualTo("/two"));
        });
    }

    [Test]
    public void FixedLengthResponseStopsBeforeTheNextResponse()
    {
        byte[] first = Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nContent-Length: 4\r\n\r\nbody");
        byte[] second = Encoding.ASCII.GetBytes(
            "HTTP/1.1 204 No Content\r\n\r\n");
        Http1Parser parser = ResponseParser();

        Http1ParseResult result = parser.ParseHttp1Bytes(Join(first, second));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(first.Length));
            Assert.That(result.Unconsumed, Is.EqualTo(second));
            Assert.That(result.Events[0].Response!.Content,
                Is.EqualTo("body"u8.ToArray()));
            Assert.That(result.Events.Count, Is.EqualTo(3));
        });
    }

    [Test]
    public void ChunkedMessageStopsAtItsTerminalTrailerBoundary()
    {
        byte[] first = Encoding.ASCII.GetBytes(
            "POST /one HTTP/1.1\r\n" +
            "Transfer-Encoding: chunked\r\n" +
            "Trailer: Digest\r\n\r\n" +
            "3\r\none\r\n0\r\nDigest: final\r\n\r\n");
        byte[] second = "GET /two HTTP/1.1\r\n\r\n"u8.ToArray();
        Http1Parser parser = new Http1Parser();

        Http1ParseResult result = parser.ParseHttp1Bytes(Join(first, second));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(first.Length));
            Assert.That(result.Unconsumed, Is.EqualTo(second));
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Trailers,
                Http1ParseEventKind.Complete
            }));
        });
    }

    [Test]
    public void CompletionFeedReportsOnlyItsOwnSurplusBytes()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult partial = parser.ParseHttp1Bytes(
            "POST / HTTP/1.1\r\nContent-Length: 3\r\n\r\nab"u8.ToArray());

        Http1ParseResult completed = parser.ParseHttp1Bytes(
            "cGET /next HTTP/1.1\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(partial.Unconsumed, Is.Empty);
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Consumed, Is.EqualTo(1));
            Assert.That(completed.Unconsumed,
                Is.EqualTo("GET /next HTTP/1.1\r\n\r\n"u8.ToArray()));
        });
    }

    [Test]
    public void PostCloseDataRequiresAnExplicitCompatibilityOption()
    {
        byte[] message =
            "GET /last HTTP/1.1\r\nConnection: close\r\n\r\n"u8.ToArray();
        byte[] postClose = "GET /must-not-run HTTP/1.1\r\n\r\n"u8.ToArray();
        Http1Parser strictParser = new Http1Parser();

        Http1ParseResult rejected =
            strictParser.ParseHttp1Bytes(Join(message, postClose));
        Http1ParseResult terminal = strictParser.ParseHttp1Bytes(postClose);

        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowPostCloseData = true;
        Http1Parser compatibleParser = new Http1Parser();
        compatibleParser.ResetHttp1Parser(options);
        Http1ParseResult allowed =
            compatibleParser.ParseHttp1Bytes(Join(message, postClose));
        Http1ParseResult repeated = compatibleParser.ParseHttp1Bytes(postClose);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.Consumed, Is.EqualTo(message.Length));
            Assert.That(rejected.Unconsumed, Is.EqualTo(postClose));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http1-post-close-data"));
            Assert.That(terminal.Status,
                Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(terminal.Consumed, Is.Zero);
            Assert.That(terminal.Unconsumed, Is.EqualTo(postClose));
            Assert.That(allowed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(allowed.Consumed, Is.EqualTo(message.Length));
            Assert.That(allowed.Unconsumed, Is.EqualTo(postClose));
            Assert.That(allowed.CanAcceptInput, Is.False);
            Assert.That(allowed.Events[0].ConnectionDecision!.MustClose, Is.True);
            Assert.That(repeated.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(repeated.Consumed, Is.Zero);
            Assert.That(repeated.Unconsumed, Is.EqualTo(postClose));
            Assert.That(repeated.Events, Is.Empty);
        });
    }

    [Test]
    public void TerminalParserReportsAllLaterInputAsUnconsumed()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult rejected = parser.ParseHttp1Bytes(
            "GET / HTTP/1.1\n"u8.ToArray());
        byte[] later = "GET /later HTTP/1.1\r\n\r\n"u8.ToArray();

        Http1ParseResult terminal = parser.ParseHttp1Bytes(later);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(terminal.Consumed, Is.Zero);
            Assert.That(terminal.Unconsumed, Is.EqualTo(later));
            Assert.That(terminal.Events, Is.Empty);
        });
    }

    static Http1Parser ResponseParser()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Get;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser;
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
