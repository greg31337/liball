using System.Text;

namespace http.Tests;

public class Http1FixedLengthContentTests
{
    [Test]
    public void SameFeedEmitsHeadContentAndCompletionAtTheExactBoundary()
    {
        byte[] input = Encoding.ASCII.GetBytes(
            "POST /items HTTP/1.1\r\nContent-Length: 5\r\n\r\nhelloNEXT");
        int boundary = input.Length - 4;
        Http1Parser parser = new Http1Parser();

        Http1ParseResult result = parser.ParseHttp1Bytes(input);
        HttpRequest request = result.Events[0].Request!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(boundary));
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Complete
            }));
            Assert.That(result.Events[1].Content, Is.EqualTo("hello"u8.ToArray()));
            Assert.That(request.Content, Is.EqualTo("hello"u8.ToArray()));
            Assert.That(request.IsComplete, Is.True);
            Assert.That(result.Events[1].Request, Is.SameAs(request));
            Assert.That(result.Events[2].Request, Is.SameAs(request));
        });
    }

    [Test]
    public void FragmentedFeedsDeliverAvailableSegmentsAndLeaveSurplusUnconsumed()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult first = parser.ParseHttp1Bytes(
            "POST / HTTP/1.1\r\nContent-Length: 5\r\n\r\nab"u8.ToArray());
        HttpRequest request = first.Events[0].Request!;
        Http1ParseResult empty = parser.ParseHttp1Bytes([]);
        Http1ParseResult second = parser.ParseHttp1Bytes("cd"u8.ToArray());
        Http1ParseResult completed = parser.ParseHttp1Bytes("eNEXT"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content
            }));
            Assert.That(first.Events[1].Content, Is.EqualTo("ab"u8.ToArray()));
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(empty.Consumed, Is.Zero);
            Assert.That(empty.Events, Is.Empty);
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(second.Consumed, Is.EqualTo(2));
            Assert.That(second.Events.Single().Content, Is.EqualTo("cd"u8.ToArray()));
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Consumed, Is.EqualTo(1));
            Assert.That(completed.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Complete
            }));
            Assert.That(completed.Events[0].Content, Is.EqualTo("e"u8.ToArray()));
            Assert.That(request.Content, Is.EqualTo("abcde"u8.ToArray()));
            Assert.That(request.IsComplete, Is.True);
        });
    }

    [Test]
    public void ZeroLengthCompletesWithoutContentEventAndPreservesFollowingBytes()
    {
        byte[] input =
            "POST / HTTP/1.1\r\nContent-Length: 0\r\n\r\nNEXT"u8.ToArray();
        Http1Parser parser = new Http1Parser();

        Http1ParseResult result = parser.ParseHttp1Bytes(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length - 4));
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(result.Events[0].Request!.Content, Is.Empty);
            Assert.That(result.Events[0].Request!.IsComplete, Is.True);
        });
    }

    [Test]
    public void BinaryResponseContentIsDeliveredAndRetainedByteExactly()
    {
        byte[] head =
            "HTTP/1.1 200 OK\r\nContent-Length: 4\r\n\r\n"u8.ToArray();
        byte[] body = [0x00, 0x80, 0xff, 0x0a];
        byte[] input = new byte[head.Length + body.Length];
        Array.Copy(head, input, head.Length);
        Array.Copy(body, 0, input, head.Length, body.Length);
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Get;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(input);
        input[head.Length] = 0x7f;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events[1].Content, Is.EqualTo(body));
            Assert.That(result.Events[0].Response!.Content, Is.EqualTo(body));
            Assert.That(result.Events[0].Response!.IsComplete, Is.True);
        });
    }

    [Test]
    public void EndOfInputBeforeDeclaredLengthIsTerminal()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult partial = parser.ParseHttp1Bytes(
            "POST / HTTP/1.1\r\nContent-Length: 5\r\n\r\nab"u8.ToArray());
        HttpRequest request = partial.Events[0].Request!;

        Http1ParseResult ended = parser.CompleteHttp1Input();
        Http1ParseResult terminal = parser.ParseHttp1Bytes("cde"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(request.Content, Is.EqualTo("ab"u8.ToArray()));
            Assert.That(request.IsComplete, Is.False);
            Assert.That(ended.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(ended.ErrorCode, Is.EqualTo("incomplete-http1-content"));
            Assert.That(ended.IsTerminal, Is.True);
            Assert.That(ended.HasErrorPosition, Is.False);
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(terminal.Consumed, Is.Zero);
        });
    }

    [Test]
    public void DeclaredContentLengthHonorsTheConfiguredContentLimit()
    {
        Http1ParserOptions exactOptions = new Http1ParserOptions();
        exactOptions.Limits.MaximumContentSize = 5;
        Http1Parser exactParser = new Http1Parser();
        exactParser.ResetHttp1Parser(exactOptions);
        Http1ParseResult exact = exactParser.ParseHttp1Bytes(
            "POST / HTTP/1.1\r\nContent-Length: 5\r\n\r\nhello"u8.ToArray());

        Http1ParserOptions shortOptions = new Http1ParserOptions();
        shortOptions.Limits.MaximumContentSize = 4;
        Http1Parser shortParser = new Http1Parser();
        shortParser.ResetHttp1Parser(shortOptions);
        Http1ParseResult rejected = shortParser.ParseHttp1Bytes(
            "POST / HTTP/1.1\r\nContent-Length: 5\r\n\r\nhello"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exact.Events[1].Content, Is.EqualTo("hello"u8.ToArray()));
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode, Is.EqualTo("http1-content-limit-exceeded"));
            Assert.That(rejected.Events, Is.Empty);
        });
    }

    [Test]
    public void ResetDiscardsPartialFixedLengthState()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult partial = parser.ParseHttp1Bytes(
            "POST /first HTTP/1.1\r\nContent-Length: 4\r\n\r\nab"u8.ToArray());
        HttpRequest first = partial.Events[0].Request!;

        parser.ResetHttp1Parser(new Http1ParserOptions());
        Http1ParseResult reset = parser.ParseHttp1Bytes(
            "GET /second HTTP/1.1\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Content, Is.EqualTo("ab"u8.ToArray()));
            Assert.That(first.IsComplete, Is.False);
            Assert.That(reset.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(reset.Events[0].Request!.Target, Is.EqualTo("/second"));
            Assert.That(reset.Events[0].Request!.Content, Is.Empty);
        });
    }
}
