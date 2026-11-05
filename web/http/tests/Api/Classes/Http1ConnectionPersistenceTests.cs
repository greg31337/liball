using System.Text;

namespace http.Tests;

public class Http1ConnectionPersistenceTests
{
    [Test]
    public void Http11SelfDelimitedMessagesDefaultToPersistent()
    {
        Http1ParseResult request = ParseRequest(
            "GET / HTTP/1.1\r\n\r\n",
            false);
        Http1ParseResult response = ParseResponse(
            "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n",
            false,
            HttpMethods.Get);

        Http1ConnectionDecision requestDecision =
            request.Events[0].ConnectionDecision!;
        Http1ConnectionDecision responseDecision =
            response.Events[0].ConnectionDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(HttpOperationStatus.Done));
            AssertPersistent(requestDecision);
            AssertPersistent(responseDecision);
            Assert.That(request.Events[1].ConnectionDecision,
                Is.SameAs(requestDecision));
            Assert.That(response.Events[1].ConnectionDecision,
                Is.SameAs(responseDecision));
        });
    }

    [Test]
    public void CloseOptionOverridesVersionAndKeepAlive()
    {
        Http1ParseResult result = ParseRequest(
            "GET / HTTP/1.1\r\n" +
            "Connection: keep-alive, custom\r\n" +
            "Connection: CLOSE\r\n\r\n",
            false);

        Http1ConnectionDecision decision = result.Events[0].ConnectionDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            AssertClose(decision);
            Assert.That(result.Events[1].ConnectionDecision, Is.SameAs(decision));
        });
    }

    [Test]
    public void Http10DefaultsToCloseForRequestsAndResponses()
    {
        Http1ParseResult request = ParseRequest(
            "GET / HTTP/1.0\r\n\r\n",
            false);
        Http1ParseResult response = ParseResponse(
            "HTTP/1.0 200 OK\r\nContent-Length: 0\r\n\r\n",
            false,
            HttpMethods.Get);

        Assert.Multiple(() =>
        {
            AssertClose(request.Events[0].ConnectionDecision!);
            AssertClose(response.Events[0].ConnectionDecision!);
        });
    }

    [Test]
    public void Http10KeepAliveHonorsProxyRecipientRules()
    {
        string requestMessage =
            "GET / HTTP/1.0\r\nConnection: KEEP-ALIVE\r\n\r\n";
        string responseMessage =
            "HTTP/1.0 200 OK\r\n" +
            "Connection: keep-alive\r\nContent-Length: 0\r\n\r\n";

        Http1ParseResult directRequest = ParseRequest(requestMessage, false);
        Http1ParseResult proxyRequest = ParseRequest(requestMessage, true);
        Http1ParseResult proxyResponse = ParseResponse(
            responseMessage,
            true,
            HttpMethods.Get);

        Assert.Multiple(() =>
        {
            AssertPersistent(directRequest.Events[0].ConnectionDecision!);
            AssertClose(proxyRequest.Events[0].ConnectionDecision!);
            AssertPersistent(proxyResponse.Events[0].ConnectionDecision!);
        });
    }

    [TestCase("HTTP/1.1")]
    [TestCase("HTTP/1.0")]
    public void CloseDelimitedResponsesCannotBeReused(string version)
    {
        Http1Parser parser = ResponseParser(true, HttpMethods.Get);
        Http1ParseResult parsed = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            version + " 200 OK\r\nConnection: keep-alive\r\n\r\nbody"));
        Http1ConnectionDecision decision = parsed.Events[0].ConnectionDecision!;

        Http1ParseResult completed = parser.CompleteHttp1Input();

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(parsed.NeedsEndOfInput, Is.True);
            AssertClose(decision);
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Events[0].ConnectionDecision, Is.SameAs(decision));
        });
    }

    [Test]
    public void OneDecisionIsSharedByAllMessageEvents()
    {
        Http1ParseResult result = ParseRequest(
            "POST / HTTP/1.1\r\n" +
            "Transfer-Encoding: chunked\r\n" +
            "Trailer: Digest\r\n\r\n" +
            "1\r\na\r\n0\r\nDigest: final\r\n\r\n",
            false);
        Http1ConnectionDecision decision = result.Events[0].ConnectionDecision!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Trailers,
                Http1ParseEventKind.Complete
            }));
            AssertPersistent(decision);
            Assert.That(result.Events.All(item =>
                ReferenceEquals(item.ConnectionDecision, decision)), Is.True);
        });
    }

    [Test]
    public void MalformedConnectionOptionsAndAggregateLimitsAreRejected()
    {
        Http1ParseResult malformed = ParseRequest(
            "GET / HTTP/1.1\r\nConnection: close, bad option\r\n\r\n",
            false);

        Http1ParserOptions limitedOptions = new Http1ParserOptions();
        limitedOptions.Limits.MaximumListMemberCount = 2;
        Http1Parser limitedParser = new Http1Parser();
        limitedParser.ResetHttp1Parser(limitedOptions);
        Http1ParseResult limited = limitedParser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\n"u8.ToArray());
        Assert.That(limited.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
        limited = limitedParser.ParseHttp1Bytes(
            "Connection: one, two\r\nConnection: three\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.ErrorCode, Is.EqualTo("invalid-http1-connection"));
            Assert.That(malformed.Events, Is.Empty);
            Assert.That(limited.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(limited.ErrorCode,
                Is.EqualTo("http1-connection-option-limit-exceeded"));
            Assert.That(limited.Events, Is.Empty);
        });
    }

    [Test]
    public void TruncationChangesTheSharedConnectionDecisionToUnusable()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult partial = parser.ParseHttp1Bytes(
            "POST / HTTP/1.1\r\nContent-Length: 2\r\n\r\na"u8.ToArray());
        Http1ConnectionDecision decision = partial.Events[0].ConnectionDecision!;
        bool initiallyReusable = decision.IsReusable;

        Http1ParseResult ended = parser.CompleteHttp1Input();

        Assert.Multiple(() =>
        {
            Assert.That(initiallyReusable, Is.True);
            Assert.That(ended.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(decision.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Unusable));
            Assert.That(decision.IsPersistent, Is.False);
            Assert.That(decision.MustClose, Is.True);
            Assert.That(decision.IsReusable, Is.False);
            Assert.That(decision.IsUpgrade, Is.False);
            Assert.That(decision.IsTunnel, Is.False);
        });
    }

    [Test]
    public void PostCloseBytesMakeTheConnectionUnusable()
    {
        byte[] input = Encoding.ASCII.GetBytes(
            "GET / HTTP/1.1\r\nConnection: close\r\n\r\nNEXT");

        Http1ParseResult result = ParseRequest(input, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Consumed, Is.EqualTo(input.Length - 4));
            Assert.That(result.Unconsumed, Is.EqualTo("NEXT"u8.ToArray()));
            Assert.That(result.CanAcceptInput, Is.False);
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-post-close-data"));
            Assert.That(result.Events[0].ConnectionDecision!.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Unusable));
            Assert.That(result.Events[0].ConnectionDecision!.MustClose, Is.True);
        });
    }

    [Test]
    public void SuccessfulConnectResponseUsesTunnelDisposition()
    {
        Http1ParseResult result = ParseResponse(
            "HTTP/1.1 200 Connection Established\r\n\r\n",
            false,
            HttpMethods.Connect);
        Http1ConnectionDecision decision = result.Events[0].ConnectionDecision!;

        Assert.Multiple(() =>
        {
            Assert.That(decision.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Tunneled));
            Assert.That(decision.IsTunnel, Is.True);
            Assert.That(decision.IsPersistent, Is.False);
            Assert.That(decision.MustClose, Is.False);
            Assert.That(decision.IsReusable, Is.False);
        });
    }

    static Http1ParseResult ParseRequest(string input, bool isProxyRecipient)
    {
        return ParseRequest(Encoding.ASCII.GetBytes(input), isProxyRecipient);
    }

    static Http1ParseResult ParseRequest(byte[] input, bool isProxyRecipient)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.IsProxyRecipient = isProxyRecipient;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser.ParseHttp1Bytes(input);
    }

    static Http1ParseResult ParseResponse(
        string input,
        bool isProxyRecipient,
        string requestMethod)
    {
        Http1Parser parser = ResponseParser(isProxyRecipient, requestMethod);
        return parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(input));
    }

    static Http1Parser ResponseParser(
        bool isProxyRecipient,
        string requestMethod)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = requestMethod;
        options.IsProxyRecipient = isProxyRecipient;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser;
    }

    static void AssertPersistent(Http1ConnectionDecision decision)
    {
        Assert.That(decision.ConnectionMode,
            Is.EqualTo(Http1ConnectionMode.Persistent));
        Assert.That(decision.IsPersistent, Is.True);
        Assert.That(decision.MustClose, Is.False);
        Assert.That(decision.IsReusable, Is.True);
        Assert.That(decision.IsUpgrade, Is.False);
        Assert.That(decision.IsTunnel, Is.False);
    }

    static void AssertClose(Http1ConnectionDecision decision)
    {
        Assert.That(decision.ConnectionMode, Is.EqualTo(Http1ConnectionMode.Close));
        Assert.That(decision.IsPersistent, Is.False);
        Assert.That(decision.MustClose, Is.True);
        Assert.That(decision.IsReusable, Is.False);
        Assert.That(decision.IsUpgrade, Is.False);
        Assert.That(decision.IsTunnel, Is.False);
    }
}
