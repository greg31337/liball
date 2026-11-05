using System.Text;

namespace http.Tests;

public class Http1ParserConformanceTests
{
    [Test]
    public void RequestsCoverNoBodyFixedAndChunkedTrailerMessageShapes()
    {
        Http1ParseResult noBody = ParseRequest(
            "GET /items?q=1 HTTP/1.1\r\nHost: example.test\r\n\r\n");
        Http1ParseResult fixedLength = ParseRequest(
            "POST /items HTTP/1.1\r\nHost: example.test\r\n" +
            "Content-Length: 4\r\n\r\ndata");
        Http1ParseResult chunked = ParseRequest(
            "POST /items HTTP/1.1\r\nHost: example.test\r\n" +
            "Transfer-Encoding: chunked\r\nTrailer: Digest\r\n\r\n" +
            "2\r\nab\r\n2\r\ncd\r\n0\r\nDigest: final\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(EventKinds(noBody), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(noBody.Events[0].Request!.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(noBody.Events[0].Request!.Path, Is.EqualTo("/items"));
            Assert.That(noBody.Events[0].Request!.Query, Is.EqualTo("q=1"));
            Assert.That(noBody.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.None));

            Assert.That(EventKinds(fixedLength), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Complete
            }));
            Assert.That(fixedLength.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(fixedLength.Events[0].Request!.Content,
                Is.EqualTo("data"u8.ToArray()));

            Assert.That(EventKinds(chunked), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Trailers,
                Http1ParseEventKind.Complete
            }));
            Assert.That(chunked.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(chunked.Events[0].Request!.Content,
                Is.EqualTo("abcd"u8.ToArray()));
            Assert.That(Value(chunked.Events[0].Request!.Trailers, "Digest"),
                Is.EqualTo("final"));
        });
    }

    [Test]
    public void ResponsesCoverFixedChunkedAndCloseDelimitedMessageShapes()
    {
        Http1ParseResult fixedLength = ParseResponse(
            "HTTP/1.1 200 OK\r\nContent-Length: 4\r\n\r\ndata",
            HttpMethods.Get);
        Http1ParseResult chunked = ParseResponse(
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n" +
            "4\r\ndata\r\n0\r\n\r\n",
            HttpMethods.Get);
        IHttp1Parser closeParser = ResponseParser(HttpMethods.Get);
        Http1ParseResult closeHead = closeParser.ParseHttp1Bytes(
            "HTTP/1.0 200 OK\r\n\r\ndata"u8.ToArray());
        Http1ParseResult closeEnd = closeParser.CompleteHttp1Input();

        Assert.Multiple(() =>
        {
            Assert.That(fixedLength.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(fixedLength.Events[0].Response!.Content,
                Is.EqualTo("data"u8.ToArray()));
            Assert.That(fixedLength.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(chunked.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(chunked.Events[0].Response!.Content,
                Is.EqualTo("data"u8.ToArray()));
            Assert.That(chunked.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(closeHead.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(closeHead.NeedsEndOfInput, Is.True);
            Assert.That(closeHead.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.CloseDelimited));
            Assert.That(closeEnd.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(EventKinds(closeEnd),
                Is.EqualTo(new[] { Http1ParseEventKind.Complete }));
        });
    }

    [TestCase(HttpMethods.Head, 200, false)]
    [TestCase(HttpMethods.Get, 100, false)]
    [TestCase(HttpMethods.Get, 199, false)]
    [TestCase(HttpMethods.Get, 204, false)]
    [TestCase(HttpMethods.Get, 304, false)]
    [TestCase(HttpMethods.Connect, 200, true)]
    [TestCase(HttpMethods.Connect, 299, true)]
    public void ResponseContextSuppressesContentAndStopsBeforeFollowingBytes(
        string requestMethod,
        int statusCode,
        bool tunnel)
    {
        string wire = "HTTP/1.1 " + statusCode + " Result\r\n" +
            "Content-Length: 10\r\nTransfer-Encoding: chunked\r\n\r\nNEXT";
        Http1ParseResult result = ParseResponse(wire, requestMethod);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed,
                Is.EqualTo(Encoding.ASCII.GetByteCount(wire) - 4));
            Assert.That(result.Unconsumed, Is.EqualTo("NEXT"u8.ToArray()));
            Assert.That(result.Events[0].Response!.Content, Is.Empty);
            Assert.That(result.Events[0].Response!.IsComplete, Is.True);
            Assert.That(result.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(tunnel ? Http1BodyFraming.Tunnel : Http1BodyFraming.None));
            Assert.That(result.Events[0].ConnectionDecision!.IsTunnel,
                Is.EqualTo(tunnel));
            Assert.That(result.Events[0].MessageKind,
                Is.EqualTo(statusCode >= 100 && statusCode <= 199 ?
                    HttpMessageKind.InformationalResponse : HttpMessageKind.Response));
        });
    }

    [Test]
    public void InformationalResponseCanBeFollowedByFinalResponseAfterReset()
    {
        Http1ParserOptions options = ResponseOptions(HttpMethods.Get);
        IHttp1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult earlyHints = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "HTTP/1.1 103 Early Hints\r\nLink: </style.css>; rel=preload\r\n\r\n"));
        parser.ResetHttp1Parser(options);
        Http1ParseResult final = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\nok"));

        Assert.Multiple(() =>
        {
            Assert.That(earlyHints.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(earlyHints.Events[0].MessageKind,
                Is.EqualTo(HttpMessageKind.InformationalResponse));
            Assert.That(earlyHints.Events[0].Response!.IsInformational, Is.True);
            Assert.That(Value(earlyHints.Events[0].Response!.Headers, "Link"),
                Is.EqualTo("</style.css>; rel=preload"));
            Assert.That(final.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(final.Events[0].MessageKind, Is.EqualTo(HttpMessageKind.Response));
            Assert.That(final.Events[0].Response!.StatusCode,
                Is.EqualTo(HttpStatusCodes.Ok));
            Assert.That(final.Events[0].Response!.Content, Is.EqualTo("ok"u8.ToArray()));
        });
    }

    [Test]
    public void FieldsPreserveLinesWhileRemovingDelimiterWhitespace()
    {
        Http1ParseResult result = ParseRequest(
            "GET / HTTP/1.1\r\n" +
            "Host: example.test\r\n" +
            "X-Empty:\r\n" +
            "x-list: first \t\r\n" +
            "X-List:\tsecond\t \r\n\r\n");
        HttpHeaders headers = result.Events[0].Request!.Headers;

        Assert.Multiple(() =>
        {
            Assert.That(headers.Fields.Select(item => item.Name), Is.EqualTo(new[]
            {
                "Host",
                "X-Empty",
                "x-list",
                "X-List"
            }));
            Assert.That(headers.Fields.Select(item =>
                Encoding.ASCII.GetString(item.Value)), Is.EqualTo(new[]
            {
                "example.test",
                "",
                "first",
                "second"
            }));
        });
    }

    [TestCase("GET / HTTP/1.1\r\nHost: example.test", "incomplete-http1-message")]
    [TestCase("POST / HTTP/1.1\r\nContent-Length: 2\r\n\r\na", "incomplete-http1-content")]
    [TestCase("POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n1\r\na\r\n",
        "incomplete-http1-chunked-content")]
    public void EndOfInputRejectsEveryIncompleteMessageCategory(
        string wire,
        string errorCode)
    {
        IHttp1Parser parser = new Http1Parser();
        Http1ParseResult partial = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(wire));

        Http1ParseResult ended = parser.CompleteHttp1Input();
        Http1ParseResult terminal = parser.ParseHttp1Bytes(Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(ended.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(ended.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(ended.IsTerminal, Is.True);
            Assert.That(ended.CanAcceptInput, Is.False);
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(terminal.Events, Is.Empty);
        });
    }

    [TestCase("HTTP/1.1", "", Http1ConnectionMode.Persistent)]
    [TestCase("HTTP/1.1", "Connection: close\r\n", Http1ConnectionMode.Close)]
    [TestCase("HTTP/1.0", "", Http1ConnectionMode.Close)]
    [TestCase("HTTP/1.0", "Connection: keep-alive\r\n",
        Http1ConnectionMode.Persistent)]
    public void VersionAndConnectionFieldsDeterminePersistence(
        string version,
        string fields,
        Http1ConnectionMode expected)
    {
        Http1ParseResult result = ParseRequest(
            "GET / " + version + "\r\nHost: example.test\r\n" + fields + "\r\n");

        Http1ConnectionDecision decision = result.Events[0].ConnectionDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(decision.ConnectionMode, Is.EqualTo(expected));
            Assert.That(decision.IsReusable,
                Is.EqualTo(expected == Http1ConnectionMode.Persistent));
            Assert.That(result.Events.All(item =>
                ReferenceEquals(item.ConnectionDecision, decision)), Is.True);
        });
    }

    [Test]
    public void UpgradeAndTunnelTransitionsExposeExactProtocolBoundaries()
    {
        IHttp1Parser upgradeParser = ResponseParser(HttpMethods.Get);
        string upgradeHead =
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Connection: Upgrade\r\nUpgrade: websocket\r\n\r\n";
        Http1ParseResult upgrade = upgradeParser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(upgradeHead + "FRAME"));

        IHttp1Parser tunnelParser = ResponseParser(HttpMethods.Connect);
        string tunnelHead = "HTTP/1.1 200 Connection Established\r\n\r\n";
        Http1ParseResult tunnel = tunnelParser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(tunnelHead + "TLS"));

        Assert.Multiple(() =>
        {
            Assert.That(upgrade.Consumed,
                Is.EqualTo(Encoding.ASCII.GetByteCount(upgradeHead)));
            Assert.That(upgrade.Unconsumed, Is.EqualTo("FRAME"u8.ToArray()));
            Assert.That(upgrade.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete,
                Http1ParseEventKind.ProtocolSwitch
            }));
            Assert.That(upgrade.Events[2].ProtocolName, Is.EqualTo("websocket"));
            Assert.That(upgrade.IsTerminal, Is.True);

            Assert.That(tunnel.Consumed,
                Is.EqualTo(Encoding.ASCII.GetByteCount(tunnelHead)));
            Assert.That(tunnel.Unconsumed, Is.EqualTo("TLS"u8.ToArray()));
            Assert.That(tunnel.Events[0].ConnectionDecision!.IsTunnel, Is.True);
            Assert.That(tunnel.IsTerminal, Is.True);
        });
    }

    static Http1ParseResult ParseRequest(string wire)
    {
        IHttp1Parser parser = new Http1Parser();
        return parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(wire));
    }

    static Http1ParseResult ParseResponse(string wire, string requestMethod)
    {
        return ResponseParser(requestMethod).ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(wire));
    }

    static IHttp1Parser ResponseParser(string requestMethod)
    {
        IHttp1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(ResponseOptions(requestMethod));
        return parser;
    }

    static Http1ParserOptions ResponseOptions(string requestMethod)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = requestMethod;
        return options;
    }

    static Http1ParseEventKind[] EventKinds(Http1ParseResult result)
    {
        return result.Events.Select(item => item.EventKind).ToArray();
    }

    static string Value(HttpHeaders headers, string name)
    {
        HttpHeaderField field = headers.Fields.Single(item =>
            string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        return Encoding.ASCII.GetString(field.Value);
    }
}
