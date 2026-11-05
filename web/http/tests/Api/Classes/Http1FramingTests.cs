using System.Text;

namespace http.Tests;

public class Http1FramingTests
{
    [Test]
    public void ContentLengthFramesRequestsAndResponsesIncludingZero()
    {
        Http1ParseResult request = ParseRequest(
            "POST /items HTTP/1.1\r\nContent-Length: 5\r\n\r\nhello");
        Http1ParseResult response = ParseResponse(
            "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n",
            HttpMethods.Get);

        Http1FramingDecision requestFraming = request.Events[0].FramingDecision!;
        Http1FramingDecision responseFraming = response.Events[0].FramingDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(requestFraming.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(requestFraming.IsValid, Is.True);
            Assert.That(requestFraming.IsSelfDelimited, Is.True);
            Assert.That(requestFraming.HasDeclaredLength, Is.True);
            Assert.That(requestFraming.DeclaredLength, Is.EqualTo(5));
            Assert.That(responseFraming.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(responseFraming.HasDeclaredLength, Is.True);
            Assert.That(responseFraming.DeclaredLength, Is.Zero);
        });
    }

    [Test]
    public void FinalChunkedCodingFramesEitherMessageKind()
    {
        Http1ParseResult request = ParseRequest(
            "POST / HTTP/1.1\r\nTransfer-Encoding: CHUNKED\r\n\r\n");
        Http1ParseResult response = ParseResponse(
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: gzip, chunked\r\n\r\n",
            HttpMethods.Get);

        Http1FramingDecision requestFraming = request.Events[0].FramingDecision!;
        Http1FramingDecision responseFraming = response.Events[0].FramingDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(requestFraming.BodyFraming, Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(requestFraming.TransferCodings,
                Is.EqualTo(new[] { "chunked" }));
            Assert.That(requestFraming.IsSelfDelimited, Is.True);
            Assert.That(responseFraming.BodyFraming,
                Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(responseFraming.TransferCodings,
                Is.EqualTo(new[] { "gzip", "chunked" }));
            Assert.That(responseFraming.IsSelfDelimited, Is.True);
        });
    }

    [Test]
    public void NonFinalChunkedTransferCodingIsCloseDelimitedOnlyForResponses()
    {
        Http1ParseResult response = ParseResponse(
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: gzip\r\n\r\n",
            HttpMethods.Get);
        Http1ParseResult request = ParseRequest(
            "POST / HTTP/1.1\r\nTransfer-Encoding: gzip\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(response.NeedsEndOfInput, Is.True);
            Assert.That(response.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.CloseDelimited));
            Assert.That(response.Events[0].FramingDecision!.IsSelfDelimited, Is.False);
            Assert.That(request.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(request.ErrorCode,
                Is.EqualTo("http1-request-transfer-coding-not-final"));
            Assert.That(request.Events, Is.Empty);
        });
    }

    [TestCase(HttpMethods.Head, 200)]
    [TestCase(HttpMethods.Connect, 101)]
    [TestCase(HttpMethods.Get, 101)]
    [TestCase(HttpMethods.Get, 199)]
    [TestCase(HttpMethods.Get, 204)]
    [TestCase(HttpMethods.Get, 304)]
    public void BodylessResponseCasesOverrideFramingFields(string method, int statusCode)
    {
        Http1ParseResult result = ParseResponse(
            "HTTP/1.1 " + statusCode + " Test\r\n" +
            "Transfer-Encoding: not valid syntax\r\n" +
            "Content-Length: invalid\r\n" +
            "\r\n",
            method);

        Http1FramingDecision framing = result.Events[0].FramingDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(framing.BodyFraming, Is.EqualTo(Http1BodyFraming.None));
            Assert.That(framing.HasNoBody, Is.True);
            Assert.That(framing.IsSelfDelimited, Is.True);
            Assert.That(framing.HasDeclaredLength, Is.False);
        });
    }

    [Test]
    public void SuccessfulConnectResponseBecomesTunnelAndIgnoresFields()
    {
        Http1ParseResult result = ParseResponse(
            "HTTP/1.1 200 Connection Established\r\n" +
            "Transfer-Encoding: malformed value\r\n" +
            "Content-Length: invalid\r\n" +
            "\r\nopaque",
            HttpMethods.Connect);

        Http1FramingDecision framing = result.Events[0].FramingDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(framing.BodyFraming, Is.EqualTo(Http1BodyFraming.Tunnel));
            Assert.That(framing.IsTunnel, Is.True);
            Assert.That(framing.HasNoBody, Is.False);
            Assert.That(framing.HasDeclaredLength, Is.False);
        });
    }

    [Test]
    public void NonSuccessfulConnectResponseUsesOrdinaryFraming()
    {
        Http1ParseResult result = ParseResponse(
            "HTTP/1.1 407 Proxy Authentication Required\r\n" +
            "Content-Length: 3\r\n\r\nabc",
            HttpMethods.Connect);

        Http1FramingDecision framing = result.Events[0].FramingDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(framing.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(framing.DeclaredLength, Is.EqualTo(3));
            Assert.That(framing.IsTunnel, Is.False);
        });
    }

    [Test]
    public void TransferEncodingOverridesContentLengthOnlyWhenExplicitlyAllowed()
    {
        string message =
            "POST / HTTP/1.1\r\n" +
            "Transfer-Encoding: chunked\r\n" +
            "Content-Length: malformed\r\n" +
            "\r\n" +
            "0\r\n\r\n";
        Http1ParseResult strict = ParseRequest(message);

        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowTransferEncodingWithContentLength = true;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        Http1ParseResult compatible = parser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(message));

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(strict.ErrorCode,
                Is.EqualTo("http1-transfer-encoding-content-length-conflict"));
            Assert.That(compatible.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(compatible.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(compatible.Events[0].FramingDecision!.HasDeclaredLength,
                Is.False);
        });
    }

    [Test]
    public void UnframedRequestHasNoBodyWhileUnframedResponseUsesClosure()
    {
        Http1ParseResult request = ParseRequest("POST / HTTP/1.1\r\n\r\n");
        Http1ParseResult response = ParseResponse(
            "HTTP/1.1 200 OK\r\n\r\n",
            HttpMethods.Get);

        Assert.Multiple(() =>
        {
            Assert.That(request.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.None));
            Assert.That(request.Events[0].FramingDecision!.HasNoBody, Is.True);
            Assert.That(response.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.CloseDelimited));
            Assert.That(response.Events[0].FramingDecision!.IsSelfDelimited, Is.False);
        });
    }

    [Test]
    public void TransferEncodingCannotFrameHttp10Messages()
    {
        Http1ParseResult request = ParseRequest(
            "POST / HTTP/1.0\r\nTransfer-Encoding: chunked\r\n\r\n");
        Http1ParseResult response = ParseResponse(
            "HTTP/1.0 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n",
            HttpMethods.Get);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(request.ErrorCode,
                Is.EqualTo("http1-transfer-encoding-on-http10"));
            Assert.That(response.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(response.ErrorCode,
                Is.EqualTo("http1-transfer-encoding-on-http10"));
        });
    }

    [Test]
    public void HeadAndCompletionEventsShareTheFramingDecision()
    {
        Http1ParseResult result = ParseRequest(
            "POST / HTTP/1.1\r\nContent-Length: 2\r\n\r\nok");

        Assert.Multiple(() =>
        {
            Assert.That(result.Events, Has.Count.EqualTo(3));
            Assert.That(result.Events[0].FramingDecision, Is.Not.Null);
            Assert.That(result.Events[1].FramingDecision,
                Is.SameAs(result.Events[0].FramingDecision));
            Assert.That(result.Events[2].FramingDecision,
                Is.SameAs(result.Events[0].FramingDecision));
        });
    }

    static Http1ParseResult ParseRequest(string message)
    {
        return new Http1Parser().ParseHttp1Bytes(Encoding.ASCII.GetBytes(message));
    }

    static Http1ParseResult ParseResponse(string message, string method)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = method;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(message));
    }
}
