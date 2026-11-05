using System.Text;

namespace http.Tests;

public class Http1ParserTests
{
    [Test]
    public void RequestModeEmitsOrderedHeadAndCompletionAtExactBoundary()
    {
        Http1Parser parser = new Http1Parser();
        byte[] input = Encoding.ASCII.GetBytes(
            "GET /items?q=1 HTTP/1.1\r\nHost: example.test\r\nX-Test: value\r\n\r\nNEXT");
        int boundary = input.Length - 4;

        Http1ParseResult result = parser.ParseHttp1Bytes(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(boundary));
            Assert.That(result.CanAcceptInput, Is.False);
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(result.Events[0].Request, Is.SameAs(result.Events[1].Request));
            Assert.That(result.Events[0].Response, Is.Null);
            Assert.That(result.Events[0].Request!.Method, Is.EqualTo(HttpMethods.Get));
            Assert.That(result.Events[0].Request!.Target, Is.EqualTo("/items?q=1"));
            Assert.That(result.Events[0].Request!.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http11));
            Assert.That(result.Events[0].Request!.Headers.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { HttpHeaderNames.Host, "X-Test" }));
            Assert.That(Encoding.ASCII.GetString(
                result.Events[0].Request!.Headers.Fields[1].Value), Is.EqualTo("value"));
            Assert.That(result.Events[0].Request!.IsComplete, Is.True);
        });

        Http1ParseResult repeated = parser.ParseHttp1Bytes("ignored"u8.ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(repeated.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(repeated.Consumed, Is.Zero);
            Assert.That(repeated.Events, Is.Empty);
            Assert.That(repeated.CanAcceptInput, Is.False);
        });
    }

    [Test]
    public void ResponseModePreservesResponseHeadAcrossFragmentedCalls()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Get;
        parser.ResetHttp1Parser(options);

        Http1ParseResult first = parser.ParseHttp1Bytes(
            "HTTP/1.0 204 No "u8.ToArray());
        byte[] secondInput = "Content\r\nServer: origin\r\n"u8.ToArray();
        Http1ParseResult second = parser.ParseHttp1Bytes(secondInput);
        Http1ParseResult third = parser.ParseHttp1Bytes("\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Consumed, Is.EqualTo(16));
            Assert.That(first.Events, Is.Empty);
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(second.Consumed, Is.EqualTo(secondInput.Length));
            Assert.That(second.Events, Is.Empty);
            Assert.That(third.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(third.Consumed, Is.EqualTo(2));
            Assert.That(third.Options, Is.SameAs(options));
            Assert.That(third.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(third.Events[0].MessageKind, Is.EqualTo(HttpMessageKind.Response));
            Assert.That(third.Events[0].Request, Is.Null);
            Assert.That(third.Events[0].Response!.ProtocolVersion,
                Is.EqualTo(HttpProtocolVersion.Http10));
            Assert.That(third.Events[0].Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.NoContent));
            Assert.That(third.Events[0].Response!.HasReasonPhrase, Is.True);
            Assert.That(Encoding.ASCII.GetString(third.Events[0].Response!.ReasonPhrase),
                Is.EqualTo("No Content"));
            Assert.That(third.Events[0].Response!.Headers.Fields[0].Name,
                Is.EqualTo(HttpHeaderNames.Server));
        });
    }

    [Test]
    public void EveryByteBoundaryCanResumeWithoutLosingOrReplayingInput()
    {
        byte[] message = "OPTIONS * HTTP/1.1\r\nHost: example.test\r\n\r\n"u8.ToArray();
        Http1Parser parser = new Http1Parser();
        Http1ParseResult result = new Http1ParseResult();

        int index = 0;
        while (index < message.Length)
        {
            result = parser.ParseHttp1Bytes(new[] { message[index] });
            Assert.That(result.Consumed, Is.EqualTo(1));
            if (index < message.Length - 1)
            {
                Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
                Assert.That(result.Events, Is.Empty);
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events, Has.Count.EqualTo(2));
            Assert.That(result.Events[0].Request!.Method, Is.EqualTo(HttpMethods.Options));
            Assert.That(result.Events[0].Request!.Target, Is.EqualTo("*"));
        });
    }

    [Test]
    public void EmptyFeedsRetainStateAndConsumeNothing()
    {
        Http1Parser parser = new Http1Parser();

        Http1ParseResult empty = parser.ParseHttp1Bytes([]);
        Http1ParseResult partial = parser.ParseHttp1Bytes("GET / HTTP/1.1\r\n"u8.ToArray());
        Http1ParseResult anotherEmpty = parser.ParseHttp1Bytes([]);
        Http1ParseResult completed = parser.ParseHttp1Bytes("\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(empty.Consumed, Is.Zero);
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(anotherEmpty.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(anotherEmpty.Consumed, Is.Zero);
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Events, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void EndOfInputFailureIsTerminalUntilReset()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult partial = parser.ParseHttp1Bytes("GET / HTTP/1.1\r\nHost:"u8.ToArray());

        Http1ParseResult ended = parser.CompleteHttp1Input();
        Http1ParseResult terminal = parser.ParseHttp1Bytes(" remainder"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(ended.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(ended.Consumed, Is.Zero);
            Assert.That(ended.IsTerminal, Is.True);
            Assert.That(ended.CanAcceptInput, Is.False);
            Assert.That(ended.ErrorCode, Is.EqualTo("incomplete-http1-message"));
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(terminal.Consumed, Is.Zero);
            Assert.That(terminal.Events, Is.Empty);
            Assert.That(terminal.IsTerminal, Is.True);
        });

        Http1ParserOptions options = new Http1ParserOptions();
        parser.ResetHttp1Parser(options);
        Http1ParseResult recovered = parser.ParseHttp1Bytes(
            "HEAD / HTTP/1.1\r\n\r\n"u8.ToArray());
        Assert.Multiple(() =>
        {
            Assert.That(recovered.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(recovered.Events[0].Request!.Method, Is.EqualTo(HttpMethods.Head));
            Assert.That(recovered.Options, Is.SameAs(options));
        });
    }

    [Test]
    public void ResetChangesModeAndClearsCompletedMessageState()
    {
        IHttp1Parser parser = new Http1Parser();
        Http1ParseResult request = parser.ParseHttp1Bytes(
            "GET /first HTTP/1.1\r\n\r\n"u8.ToArray());
        Http1ParserOptions responseOptions = new Http1ParserOptions();
        responseOptions.MessageKind = HttpMessageKind.Response;

        parser.ResetHttp1Parser(responseOptions);
        Http1ParseResult response = parser.ParseHttp1Bytes(
            "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(request.Events[0].Request!.Target, Is.EqualTo("/first"));
            Assert.That(response.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(response.Events[0].Request, Is.Null);
            Assert.That(response.Events[0].Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(response.Options, Is.SameAs(responseOptions));
        });
    }

    [Test]
    public void BufferedInputLimitFailureReportsCurrentCallPosition()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits.MaximumBufferedSize = 8;
        parser.ResetHttp1Parser(options);

        Http1ParseResult first = parser.ParseHttp1Bytes("GET /lon"u8.ToArray());
        Http1ParseResult rejected = parser.ParseHttp1Bytes("g"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Consumed, Is.EqualTo(8));
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.Consumed, Is.EqualTo(1));
            Assert.That(rejected.HasErrorPosition, Is.True);
            Assert.That(rejected.ErrorPosition, Is.Zero);
            Assert.That(rejected.ErrorCode, Is.EqualTo("http1-buffer-limit-exceeded"));
            Assert.That(rejected.IsTerminal, Is.True);
        });
    }
}
