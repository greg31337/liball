using System.Text;

namespace http.Tests;

public class Http1ProtocolTransitionTests
{
    [Test]
    public void Http11ContentRequestReportsContinueExpectationAtTheHead()
    {
        Http1Parser parser = new Http1Parser();

        Http1ParseResult result = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "POST /upload HTTP/1.1\r\n" +
            "Content-Length: 3\r\n" +
            "Expect: 100-CoNtInUe\r\n\r\n"));
        HttpRequest request = result.Events[0].Request!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(result.Events.Select(item => item.EventKind),
                Is.EqualTo(new[] { Http1ParseEventKind.MessageHead }));
            Assert.That(request.HasContinueExpectation, Is.True);
            Assert.That(request.ShouldSendContinue, Is.True);
            Assert.That(request.HasUnsupportedExpectations, Is.False);
        });
    }

    [Test]
    public void ContinueMetadataAccountsForVersionContentAndExtensions()
    {
        Http1ParseResult http10 = ParseRequest(
            "POST / HTTP/1.0\r\nContent-Length: 1\r\n" +
            "Expect: 100-continue\r\n\r\nx");
        Http1ParseResult empty = ParseRequest(
            "POST / HTTP/1.1\r\nContent-Length: 0\r\n" +
            "Expect: 100-continue\r\n\r\n");
        Http1ParseResult extension = ParseRequest(
            "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n" +
            "Expect: custom=value, 100-continue\r\n\r\n0\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(http10.Events[0].Request!.HasContinueExpectation, Is.True);
            Assert.That(http10.Events[0].Request!.ShouldSendContinue, Is.False);
            Assert.That(empty.Events[0].Request!.HasContinueExpectation, Is.True);
            Assert.That(empty.Events[0].Request!.ShouldSendContinue, Is.False);
            Assert.That(extension.Events[0].Request!.HasContinueExpectation, Is.True);
            Assert.That(extension.Events[0].Request!.ShouldSendContinue, Is.True);
            Assert.That(extension.Events[0].Request!.HasUnsupportedExpectations,
                Is.True);
        });
    }

    [Test]
    public void MalformedAndExcessiveExpectationsAreRejected()
    {
        Http1ParseResult malformed = ParseRequest(
            "POST / HTTP/1.1\r\nContent-Length: 1\r\n" +
            "Expect: 100-continue=\r\n\r\nx");
        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits.MaximumListMemberCount = 1;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        Http1ParseResult excessive = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\nContent-Length: 1\r\n" +
            "Expect: one\r\nExpect: two\r\n\r\nx"));

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.ErrorCode,
                Is.EqualTo("invalid-http1-expectation"));
            Assert.That(excessive.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(excessive.ErrorCode,
                Is.EqualTo("http1-expectation-limit-exceeded"));
        });
    }

    [Test]
    public void ContinueResponseRemainsAnInformationalHttpMessage()
    {
        Http1ParseResult result = ParseResponse(
            "HTTP/1.1 100 Continue\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(result.Events[0].MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(result.Events[0].Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.Continue));
            Assert.That(result.Events[0].Response!.IsInformational, Is.True);
        });
    }

    [Test]
    public void Http11RequestRecognizesOrderedUpgradeCandidates()
    {
        Http1ParseResult result = ParseRequest(
            "GET /chat HTTP/1.1\r\n" +
            "Connection: keep-alive, UpGrAdE\r\n" +
            "Upgrade: WebSocket, IRC/6.9\r\n\r\n");
        HttpRequest request = result.Events[0].Request!;
        Http1ParseResult ignored = ParseRequest(
            "GET /chat HTTP/1.0\r\n" +
            "Connection: Upgrade\r\nUpgrade: websocket\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(request.IsUpgradeRequested, Is.True);
            Assert.That(request.UpgradeProtocols,
                Is.EqualTo(new[] { "WebSocket", "IRC/6.9" }));
            Assert.That(request.Headers.Fields.Count, Is.EqualTo(2));
            Assert.That(ignored.Events[0].Request!.IsUpgradeRequested, Is.False);
            Assert.That(ignored.Events[0].Request!.UpgradeProtocols, Is.Empty);
        });
    }

    [Test]
    public void SwitchingProtocolsHandsOffAtTheExactResponseBoundary()
    {
        byte[] head = Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: keep-alive, Upgrade\r\n" +
            "Upgrade: WebSocket\r\n\r\n");
        byte[] upgraded = [0x81, 0x02, 0x68, 0x69];
        Http1Parser parser = ResponseParser(HttpMethods.Get);

        Http1ParseResult result = parser.ParseHttp1Bytes(Join(head, upgraded));
        Http1ConnectionDecision decision =
            result.Events[0].ConnectionDecision!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(head.Length));
            Assert.That(result.Unconsumed, Is.EqualTo(upgraded));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.CanAcceptInput, Is.False);
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete,
                Http1ParseEventKind.ProtocolSwitch
            }));
            Assert.That(decision.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Upgraded));
            Assert.That(decision.IsUpgrade, Is.True);
            Assert.That(decision.IsTunnel, Is.False);
            Assert.That(decision.IsReusable, Is.False);
            Assert.That(decision.MustClose, Is.False);
            Assert.That(decision.ProtocolName, Is.EqualTo("WebSocket"));
            Assert.That(result.Events[2].ProtocolName, Is.EqualTo("WebSocket"));
            Assert.That(result.Events.All(item =>
                ReferenceEquals(item.ConnectionDecision, decision)), Is.True);
        });
    }

    [Test]
    public void UpgradedParserLeavesLaterProtocolBytesForTheCaller()
    {
        Http1Parser parser = ResponseParser(HttpMethods.Get);
        Http1ParseResult switched = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\nUpgrade: websocket\r\n\r\n"));
        byte[] frame = [0x82, 0x01, 0xff];

        Http1ParseResult terminal = parser.ParseHttp1Bytes(frame);

        Assert.Multiple(() =>
        {
            Assert.That(switched.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(terminal.IsTerminal, Is.True);
            Assert.That(terminal.Consumed, Is.Zero);
            Assert.That(terminal.Unconsumed, Is.EqualTo(frame));
            Assert.That(terminal.Events, Is.Empty);
        });
    }

    [Test]
    public void IncompleteUpgradeHandshakeDoesNotSwitchProtocols()
    {
        Http1ParseResult missingConnection = ParseResponse(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n\r\nNEXT");
        Http1ParseResult missingUpgrade = ParseResponse(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\n\r\nNEXT");
        Http1ParseResult http10 = ParseResponse(
            "HTTP/1.0 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\nUpgrade: websocket\r\n\r\nNEXT");

        Assert.Multiple(() =>
        {
            Assert.That(missingConnection.IsTerminal, Is.False);
            Assert.That(missingUpgrade.IsTerminal, Is.False);
            Assert.That(http10.IsTerminal, Is.True);
            Assert.That(http10.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(http10.ErrorCode,
                Is.EqualTo("http1-post-close-data"));
            Assert.That(missingConnection.Events.Any(item =>
                item.EventKind == Http1ParseEventKind.ProtocolSwitch), Is.False);
            Assert.That(missingUpgrade.Events.Any(item =>
                item.EventKind == Http1ParseEventKind.ProtocolSwitch), Is.False);
            Assert.That(http10.Events.Any(item =>
                item.EventKind == Http1ParseEventKind.ProtocolSwitch), Is.False);
            Assert.That(missingConnection.Unconsumed,
                Is.EqualTo("NEXT"u8.ToArray()));
            Assert.That(missingUpgrade.Unconsumed,
                Is.EqualTo("NEXT"u8.ToArray()));
            Assert.That(http10.Unconsumed, Is.EqualTo("NEXT"u8.ToArray()));
        });
    }

    [Test]
    public void MalformedAndExcessiveUpgradeSelectionsAreRejected()
    {
        Http1ParseResult malformed = ParseResponse(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\nUpgrade: bad protocol\r\n\r\n");
        Http1ParserOptions options = ResponseOptions(HttpMethods.Get);
        options.Limits.MaximumListMemberCount = 1;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        Http1ParseResult excessive = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\n" +
            "Upgrade: first\r\nUpgrade: second\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.ErrorCode, Is.EqualTo("invalid-http1-upgrade"));
            Assert.That(excessive.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(excessive.ErrorCode,
                Is.EqualTo("http1-upgrade-protocol-limit-exceeded"));
        });
    }

    [Test]
    public void SuccessfulConnectHandsOffAndCanDeliverOpaqueTunnelBytes()
    {
        byte[] head =
            "HTTP/1.1 200 Connection Established\r\n\r\n"u8.ToArray();
        byte[] initialTunnelBytes = [0x16, 0x03, 0x01, 0x00];
        Http1Parser parser = ResponseParser(HttpMethods.Connect);

        Http1ParseResult connected =
            parser.ParseHttp1Bytes(Join(head, initialTunnelBytes));
        Http1ConnectionDecision decision =
            connected.Events[0].ConnectionDecision!;
        byte[] nextTunnelBytes = [0xff, 0x00, 0x80, 0x7f];
        Http1ParseResult tunnel = parser.ParseHttp1Bytes(nextTunnelBytes);
        nextTunnelBytes[0] = 0x01;

        Assert.Multiple(() =>
        {
            Assert.That(connected.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(connected.Consumed, Is.EqualTo(head.Length));
            Assert.That(connected.Unconsumed, Is.EqualTo(initialTunnelBytes));
            Assert.That(connected.IsTerminal, Is.True);
            Assert.That(connected.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(decision.ConnectionMode,
                Is.EqualTo(Http1ConnectionMode.Tunneled));
            Assert.That(decision.IsTunnel, Is.True);
            Assert.That(decision.IsUpgrade, Is.False);
            Assert.That(decision.MustClose, Is.False);
            Assert.That(tunnel.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(tunnel.Consumed, Is.EqualTo(4));
            Assert.That(tunnel.Unconsumed, Is.Empty);
            Assert.That(tunnel.Events.Single().EventKind,
                Is.EqualTo(Http1ParseEventKind.TunnelBytes));
            Assert.That(tunnel.Events.Single().Content,
                Is.EqualTo(new byte[] { 0xff, 0x00, 0x80, 0x7f }));
            Assert.That(tunnel.Events.Single().ConnectionDecision,
                Is.SameAs(decision));
        });
    }

    [Test]
    public void UnsuccessfulConnectResponseRemainsHttp()
    {
        byte[] response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 403 Forbidden\r\nContent-Length: 3\r\n\r\nno!");
        Http1Parser parser = ResponseParser(HttpMethods.Connect);

        Http1ParseResult result = parser.ParseHttp1Bytes(
            Join(response, "NEXT"u8.ToArray()));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsTerminal, Is.False);
            Assert.That(result.Consumed, Is.EqualTo(response.Length));
            Assert.That(result.Unconsumed, Is.EqualTo("NEXT"u8.ToArray()));
            Assert.That(result.Events[0].ConnectionDecision!.IsTunnel, Is.False);
            Assert.That(result.Events[0].Response!.Content,
                Is.EqualTo("no!"u8.ToArray()));
        });
    }

    static Http1ParseResult ParseRequest(string message)
    {
        return new Http1Parser().ParseHttp1Bytes(Encoding.ASCII.GetBytes(message));
    }

    static Http1ParseResult ParseResponse(string message)
    {
        return ResponseParser(HttpMethods.Get).ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(message));
    }

    static Http1Parser ResponseParser(string requestMethod)
    {
        Http1ParserOptions options = ResponseOptions(requestMethod);
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser;
    }

    static Http1ParserOptions ResponseOptions(string requestMethod)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = requestMethod;
        return options;
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
