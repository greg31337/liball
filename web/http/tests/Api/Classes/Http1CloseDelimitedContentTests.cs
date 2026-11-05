using System.Text;

namespace http.Tests;

public class Http1CloseDelimitedContentTests
{
    [Test]
    public void CloseDelimitedResponseCompletesOnlyAtExplicitEndOfInput()
    {
        byte[] input = Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nServer: origin\r\n\r\nhello");
        Http1Parser parser = ResponseParser();

        Http1ParseResult parsed = parser.ParseHttp1Bytes(input);
        HttpResponse response = parsed.Events[0].Response!;
        bool wasCompleteBeforeEnd = response.IsComplete;
        Http1ParseResult completed = parser.CompleteHttp1Input();
        byte[] postClose = "ignored"u8.ToArray();
        Http1ParseResult afterCompletion = parser.ParseHttp1Bytes(postClose);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(parsed.Consumed, Is.EqualTo(input.Length));
            Assert.That(parsed.NeedsEndOfInput, Is.True);
            Assert.That(parsed.CanAcceptInput, Is.True);
            Assert.That(parsed.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content
            }));
            Assert.That(parsed.Events[1].Content, Is.EqualTo("hello"u8.ToArray()));
            Assert.That(response.Content, Is.EqualTo("hello"u8.ToArray()));
            Assert.That(wasCompleteBeforeEnd, Is.False);
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Consumed, Is.Zero);
            Assert.That(completed.NeedsEndOfInput, Is.False);
            Assert.That(completed.CanAcceptInput, Is.False);
            Assert.That(completed.IsTerminal, Is.False);
            Assert.That(completed.Events.Single().EventKind,
                Is.EqualTo(Http1ParseEventKind.Complete));
            Assert.That(completed.Events[0].Response, Is.SameAs(response));
            Assert.That(response.IsComplete, Is.True);
            Assert.That(afterCompletion.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(afterCompletion.Consumed, Is.Zero);
            Assert.That(afterCompletion.Unconsumed, Is.EqualTo(postClose));
            Assert.That(afterCompletion.ErrorCode,
                Is.EqualTo("http1-post-close-data"));
            Assert.That(afterCompletion.Events, Is.Empty);
        });
    }

    [Test]
    public void FragmentedBinaryContentIsDeliveredAndOwnedPerFeed()
    {
        Http1Parser parser = ResponseParser();
        Http1ParseResult head = parser.ParseHttp1Bytes(
            "HTTP/1.0 200 OK\r\n\r\n"u8.ToArray());
        byte[] firstInput = [0x00, 0x80];
        byte[] secondInput = [0xff, 0x0a];

        Http1ParseResult empty = parser.ParseHttp1Bytes([]);
        Http1ParseResult first = parser.ParseHttp1Bytes(firstInput);
        Http1ParseResult second = parser.ParseHttp1Bytes(secondInput);
        firstInput[0] = 0x7f;
        secondInput[0] = 0x7f;
        Http1ParseResult completed = parser.CompleteHttp1Input();

        Assert.Multiple(() =>
        {
            Assert.That(head.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(head.NeedsEndOfInput, Is.True);
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(empty.NeedsEndOfInput, Is.True);
            Assert.That(empty.Events, Is.Empty);
            Assert.That(first.Events.Single().Content,
                Is.EqualTo(new byte[] { 0x00, 0x80 }));
            Assert.That(second.Events.Single().Content,
                Is.EqualTo(new byte[] { 0xff, 0x0a }));
            Assert.That(head.Events[0].Response!.Content,
                Is.EqualTo(new byte[] { 0x00, 0x80, 0xff, 0x0a }));
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(head.Events[0].Response!.IsComplete, Is.True);
        });
    }

    [Test]
    public void EmptyCloseDelimitedBodyCompletesAtEndOfInput()
    {
        Http1Parser parser = ResponseParser();
        Http1ParseResult head = parser.ParseHttp1Bytes(
            "HTTP/1.1 200 OK\r\n\r\n"u8.ToArray());

        Http1ParseResult completed = parser.CompleteHttp1Input();

        Assert.Multiple(() =>
        {
            Assert.That(head.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(head.Events.Select(item => item.EventKind),
                Is.EqualTo(new[] { Http1ParseEventKind.MessageHead }));
            Assert.That(head.NeedsEndOfInput, Is.True);
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Events.Select(item => item.EventKind),
                Is.EqualTo(new[] { Http1ParseEventKind.Complete }));
            Assert.That(head.Events[0].Response!.Content, Is.Empty);
            Assert.That(head.Events[0].Response!.IsComplete, Is.True);
        });
    }

    [Test]
    public void NonFinalTransferCodingContentRemainsCloseDelimitedBytes()
    {
        Http1Parser parser = ResponseParser();
        byte[] payload = "4\r\nraw!\r\n0\r\n\r\n"u8.ToArray();
        byte[] head =
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: gzip\r\n\r\n"u8.ToArray();
        byte[] input = new byte[head.Length + payload.Length];
        Array.Copy(head, input, head.Length);
        Array.Copy(payload, 0, input, head.Length, payload.Length);

        Http1ParseResult result = parser.ParseHttp1Bytes(input);
        Http1ParseResult completed = parser.CompleteHttp1Input();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(result.NeedsEndOfInput, Is.True);
            Assert.That(result.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.CloseDelimited));
            Assert.That(result.Events[1].Content, Is.EqualTo(payload));
            Assert.That(result.Events[0].Response!.Content, Is.EqualTo(payload));
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
        });
    }

    [Test]
    public void CloseDelimitedContentLimitIsCumulativeAndTerminal()
    {
        Http1ParserOptions options = ResponseOptions();
        options.Limits.MaximumContentSize = 5;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        Http1ParseResult first = parser.ParseHttp1Bytes(
            "HTTP/1.1 200 OK\r\n\r\nabc"u8.ToArray());

        Http1ParseResult rejected = parser.ParseHttp1Bytes("def"u8.ToArray());
        Http1ParseResult terminal = parser.CompleteHttp1Input();

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Events[0].Response!.Content,
                Is.EqualTo("abc"u8.ToArray()));
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.Consumed, Is.Zero);
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http1-content-limit-exceeded"));
            Assert.That(rejected.NeedsEndOfInput, Is.False);
            Assert.That(rejected.CanAcceptInput, Is.False);
            Assert.That(rejected.IsTerminal, Is.True);
            Assert.That(first.Events[0].Response!.Content,
                Is.EqualTo("abc"u8.ToArray()));
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal));
        });
    }

    [TestCase("fixed", "incomplete-http1-content")]
    [TestCase("chunk-size", "incomplete-http1-chunked-content")]
    [TestCase("chunk-data", "incomplete-http1-chunked-content")]
    [TestCase("chunk-delimiter", "incomplete-http1-chunked-content")]
    [TestCase("chunk-trailer", "incomplete-http1-chunked-content")]
    public void TruncatedSelfDelimitedBodiesLeaveTheParserUnusable(
        string selected,
        string errorCode)
    {
        Http1Parser parser = new Http1Parser();
        string message;
        switch (selected)
        {
            case "fixed":
                message = "POST / HTTP/1.1\r\nContent-Length: 2\r\n\r\na";
                break;
            case "chunk-size":
                message =
                    "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\nA";
                break;
            case "chunk-data":
                message =
                    "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n2\r\na";
                break;
            case "chunk-delimiter":
                message =
                    "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n1\r\na\r";
                break;
            default:
                message =
                    "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" +
                    "0\r\nDigest: partial";
                break;
        }

        Http1ParseResult partial = parser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(message));
        Http1ParseResult ended = parser.CompleteHttp1Input();
        Http1ParseResult terminal = parser.ParseHttp1Bytes("more"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(ended.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(ended.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(ended.NeedsEndOfInput, Is.False);
            Assert.That(ended.CanAcceptInput, Is.False);
            Assert.That(ended.IsTerminal, Is.True);
            Assert.That(ended.HasErrorPosition, Is.False);
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(terminal.Consumed, Is.Zero);
            Assert.That(terminal.CanAcceptInput, Is.False);
            Assert.That(terminal.IsTerminal, Is.True);
        });

        parser.ResetHttp1Parser(new Http1ParserOptions());
        Http1ParseResult recovered = parser.ParseHttp1Bytes(
            "GET /recovered HTTP/1.1\r\n\r\n"u8.ToArray());
        Assert.That(recovered.Status, Is.EqualTo(HttpOperationStatus.Done));
    }

    static Http1Parser ResponseParser()
    {
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(ResponseOptions());
        return parser;
    }

    static Http1ParserOptions ResponseOptions()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Get;
        return options;
    }
}
