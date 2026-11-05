using System.Text;
using websocket;

namespace websocket.Tests;

public class Http1RequestParserTests
{
    [Test]
    public void ParsesRequestMetadataFieldsDuplicatesAndOpaqueOctets()
    {
        const string headerText =
            "GET /chat?room=blue%20sky&next=? HTTP/1.1\r\n" +
            "Host: example.test\r\n" +
            "X-Value:\t alpha\tbeta \t\r\n" +
            "Set-Cookie: one\r\n" +
            "Set-Cookie: two\u0080\r\n" +
            "\r\n";
        var header = Encoding.Latin1.GetBytes(headerText);
        var source = header.Concat(new byte[] { 0x81, 0x00 }).ToArray();
        var parser = CreateParser();

        var result = parser.ReadRequest(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(header.Length));
            Assert.That(result.HasRequest, Is.True);
            Assert.That(result.HasResponse, Is.False);
            Assert.That(result.HasError, Is.False);
            Assert.That(result.Request.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(result.Request.Method, Is.EqualTo("GET"));
            Assert.That(result.Request.HasRequestTarget, Is.True);
            Assert.That(result.Request.RequestTarget, Is.EqualTo("/chat?room=blue%20sky&next=?"));
            Assert.That(result.Request.HasHttpVersion, Is.True);
            Assert.That(result.Request.HttpVersion, Is.EqualTo("HTTP/1.1"));
            Assert.That(result.Request.HasScheme, Is.False);
            Assert.That(result.Request.HasAuthority, Is.False);
            Assert.That(result.Request.HasPath, Is.False);
            Assert.That(result.Request.HasProtocol, Is.False);
            Assert.That(result.Request.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "Host", "X-Value", "Set-Cookie", "Set-Cookie" }));
            Assert.That(result.Request.Fields.Select(field => field.Value),
                Is.EqualTo(new[] { "example.test", "alpha\tbeta", "one", "two\u0080" }));
        });
    }

    [Test]
    public void RequestCanArriveOneByteAtATime()
    {
        var source = Bytes(
            "GET /socket HTTP/1.1\r\n" +
            "Host: example.test\r\n" +
            "Upgrade: websocket\r\n\r\n");
        var parser = CreateParser();

        for (var index = 0; index < source.Length; index++)
        {
            var result = parser.ReadRequest(source.AsSpan(index, 1));

            Assert.That(result.ConsumedCount, Is.EqualTo(1), $"Byte {index}");
            if (index + 1 == source.Length)
            {
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(result.Request.Fields, Has.Length.EqualTo(2));
            }
            else
            {
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput), $"Byte {index}");
                Assert.That(result.HasRequest, Is.False, $"Byte {index}");
            }
        }
    }

    [Test]
    public void RequestCompletesAcrossEveryInputSplit()
    {
        var source = Bytes("GET /chat HTTP/1.1\r\nHost: example.test\r\n\r\n");

        for (var split = 0; split < source.Length; split++)
        {
            var parser = CreateParser();
            var first = parser.ReadRequest(source.AsSpan(0, split));
            var second = parser.ReadRequest(source.AsSpan(split));

            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput), $"Split {split}");
                Assert.That(first.ConsumedCount, Is.EqualTo(split), $"Split {split}");
                Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete), $"Split {split}");
                Assert.That(second.ConsumedCount, Is.EqualTo(source.Length - split), $"Split {split}");
                Assert.That(second.Request.RequestTarget, Is.EqualTo("/chat"), $"Split {split}");
            });
        }
    }

    [Test]
    public void EmptyInputLeavesAnIncompleteRequestPending()
    {
        var parser = CreateParser();
        var first = parser.ReadRequest(Bytes("GET / HTTP/1.1\r"));

        var empty = parser.ReadRequest(ReadOnlySpan<byte>.Empty);
        var final = parser.ReadRequest(Bytes("\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(empty.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.ConsumedCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void CompletedResultIsPersistentAndIndependentlyOwnedUntilReset()
    {
        var parser = CreateParser();
        var first = parser.ReadRequest(Bytes("GET /first HTTP/1.1\r\nHost: original\r\n\r\n"));
        first.Request.Method = "MUTATED";
        first.Request.Fields[0].Value = "mutated";

        var repeated = parser.ReadRequest(Bytes("POST /ignored HTTP/1.0\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Request, Is.Not.SameAs(first.Request));
            Assert.That(repeated.Request.Method, Is.EqualTo("GET"));
            Assert.That(repeated.Request.RequestTarget, Is.EqualTo("/first"));
            Assert.That(repeated.Request.Fields[0], Is.Not.SameAs(first.Request.Fields[0]));
            Assert.That(repeated.Request.Fields[0].Value, Is.EqualTo("original"));
        });
    }

    [Test]
    public void ResetAllowsAnotherRequest()
    {
        var parser = CreateParser();
        parser.ReadRequest(Bytes("GET /first HTTP/1.1\r\nHost: one\r\n\r\n"));

        parser.ResetRequest();
        var second = parser.ReadRequest(Bytes("POST /second?x=1 HTTP/1.0\r\nHost: two\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.Request.Method, Is.EqualTo("POST"));
            Assert.That(second.Request.RequestTarget, Is.EqualTo("/second?x=1"));
            Assert.That(second.Request.HttpVersion, Is.EqualTo("HTTP/1.0"));
            Assert.That(second.Request.Fields.Single().Value, Is.EqualTo("two"));
        });
    }

    [TestCase(0, 1, 1)]
    [TestCase(-1, 1, 1)]
    [TestCase(1, 0, 1)]
    [TestCase(1, -1, 1)]
    [TestCase(1, 1, 0)]
    [TestCase(1, 1, -1)]
    [TestCase(8, 1, 9)]
    public void ConstructorRejectsInvalidLimits(int maximumBytes, int maximumCount, int maximumLine)
    {
        Assert.Throws<WebSocketLimitException>(() =>
            new Http1RequestParser(maximumBytes, maximumCount, maximumLine));
    }

    [TestCase("GET / HTTP/1.1", "GET", "/", "HTTP/1.1")]
    [TestCase("OPTIONS /a//b?x=1&y=two?three HTTP/9.9", "OPTIONS", "/a//b?x=1&y=two?three", "HTTP/9.9")]
    [TestCase("M-SEARCH /%7euser;v=@ HTTP/1.0", "M-SEARCH", "/%7euser;v=@", "HTTP/1.0")]
    [TestCase("custom!method /a(b),c+d HTTP/2.0", "custom!method", "/a(b),c+d", "HTTP/2.0")]
    public void AcceptsSyntacticallyValidRequestLines(
        string requestLine,
        string expectedMethod,
        string expectedTarget,
        string expectedVersion)
    {
        var parser = CreateParser();

        var result = parser.ReadRequest(Bytes(requestLine + "\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Request.Method, Is.EqualTo(expectedMethod));
            Assert.That(result.Request.RequestTarget, Is.EqualTo(expectedTarget));
            Assert.That(result.Request.HttpVersion, Is.EqualTo(expectedVersion));
        });
    }

    [TestCase("")]
    [TestCase("GET")]
    [TestCase("GET /")]
    [TestCase(" GET / HTTP/1.1")]
    [TestCase("GET  / HTTP/1.1")]
    [TestCase("GET /  HTTP/1.1")]
    [TestCase("GET / HTTP/1.1 ")]
    [TestCase("GET\t/ HTTP/1.1")]
    [TestCase("G(ET / HTTP/1.1")]
    [TestCase("GE\u0000T / HTTP/1.1")]
    [TestCase("GET / http/1.1")]
    [TestCase("GET / HTTP/11")]
    [TestCase("GET / HTTP/1.10")]
    [TestCase("GET / HTTP/A.1")]
    [TestCase("GET / HTTP/1.\u0001")]
    public void RejectsMalformedRequestLines(string requestLine)
    {
        AssertProtocolFailure(requestLine + "\r\n\r\n", "http-request-line-invalid");
    }

    [TestCase("*")]
    [TestCase("example.test:80")]
    [TestCase("http://example.test/chat")]
    [TestCase("/#fragment")]
    [TestCase("/bad%")]
    [TestCase("/bad%2")]
    [TestCase("/bad%GG")]
    [TestCase("/bad[value]")]
    [TestCase("/bad\\value")]
    [TestCase("/bad\u0001value")]
    [TestCase("/caf\u00e9")]
    public void RejectsTargetsOutsideStrictOriginForm(string target)
    {
        AssertProtocolFailure(
            "GET " + target + " HTTP/1.1\r\n\r\n",
            "http-request-target-invalid");
    }

    [TestCase(": value")]
    [TestCase("Bad Name: value")]
    [TestCase("Bad\tName: value")]
    [TestCase("Bad(Name): value")]
    [TestCase("Host : value")]
    [TestCase("Caf\u00e9: value")]
    public void RejectsInvalidFieldNamesAndWhitespaceBeforeColon(string fieldLine)
    {
        AssertProtocolFailure(
            "GET / HTTP/1.1\r\n" + fieldLine + "\r\n\r\n",
            "http-header-name-invalid");
    }

    [TestCase(" continuation")]
    [TestCase("\tcontinuation")]
    public void RejectsObsoleteLineFolding(string foldedLine)
    {
        AssertProtocolFailure(
            "GET / HTTP/1.1\r\nX-Test: one\r\n" + foldedLine + "\r\n\r\n",
            "http-header-obs-fold");
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(8)]
    [TestCase(11)]
    [TestCase(31)]
    [TestCase(127)]
    public void RejectsUnsafeFieldValueControls(int control)
    {
        var prefix = Bytes("GET / HTTP/1.1\r\nX-Test: safe");
        var suffix = Bytes("value\r\n\r\n");
        var source = new byte[prefix.Length + 1 + suffix.Length];
        prefix.CopyTo(source, 0);
        source[prefix.Length] = (byte)control;
        suffix.CopyTo(source, prefix.Length + 1);
        var parser = CreateParser();

        var result = parser.ReadRequest(source);

        AssertFailure(result, WebSocketParseStatus.ProtocolFailure, "http-header-value-invalid", 400);
    }

    [TestCase("GET / HTTP/1.1\n\n")]
    [TestCase("GET / HTTP/1.1\rX")]
    [TestCase("GET / HTTP/1.1\r\nHost: example.test\n\n")]
    [TestCase("GET / HTTP/1.1\r\nHost: example.test\r\n\n")]
    public void RejectsBareOrBrokenLineEndings(string requestText)
    {
        AssertProtocolFailure(requestText, "http-line-ending-invalid");
    }

    [Test]
    public void ExactLineLimitIsAcceptedAndNextByteIsRejected()
    {
        const string requestLine = "GET / HTTP/1.1";
        var exactParser = new Http1RequestParser(128, 4, requestLine.Length);
        var tooLongParser = new Http1RequestParser(128, 4, requestLine.Length - 1);

        var exact = exactParser.ReadRequest(Bytes(requestLine + "\r\n\r\n"));
        var tooLong = tooLongParser.ReadRequest(Bytes(requestLine + "\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(tooLong.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(tooLong.ConsumedCount, Is.EqualTo(requestLine.Length));
            Assert.That(tooLong.Error.Identifier, Is.EqualTo("http-header-line-too-long"));
            Assert.That(tooLong.Error.HttpStatusCode, Is.EqualTo(431));
        });
    }

    [Test]
    public void HeaderFieldLineLimitIsEnforced()
    {
        const string requestLine = "GET / HTTP/1.1";
        const string longField = "Long: 123456789";
        var parser = new Http1RequestParser(128, 4, requestLine.Length);

        var result = parser.ReadRequest(Bytes(requestLine + "\r\n" + longField + "\r\n\r\n"));

        AssertFailure(result, WebSocketParseStatus.LimitRejected, "http-header-line-too-long", 431);
    }

    [Test]
    public void ExactTotalByteLimitIsAcceptedAndIncompleteLimitIsRejected()
    {
        var source = Bytes("GET / HTTP/1.1\r\n\r\n");
        var exactParser = new Http1RequestParser(source.Length, 1, 14);
        var shortParser = new Http1RequestParser(source.Length - 1, 1, 14);

        var exact = exactParser.ReadRequest(source);
        var tooLarge = shortParser.ReadRequest(source);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(exact.ConsumedCount, Is.EqualTo(source.Length));
            Assert.That(tooLarge.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(tooLarge.ConsumedCount, Is.EqualTo(source.Length - 1));
            Assert.That(tooLarge.Error.Identifier, Is.EqualTo("http-header-bytes-too-large"));
        });
    }

    [Test]
    public void HeaderCountLimitRejectsTheFirstExcessField()
    {
        var exactParser = new Http1RequestParser(256, 1, 64);
        var parser = new Http1RequestParser(256, 1, 64);
        var exact = exactParser.ReadRequest(Bytes(
            "GET / HTTP/1.1\r\n" +
            "Host: example.test\r\n\r\n"));
        var source = Bytes(
            "GET / HTTP/1.1\r\n" +
            "Host: example.test\r\n" +
            "Upgrade: websocket\r\n\r\n");

        var result = parser.ReadRequest(source);

        Assert.That(exact.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        Assert.That(exact.Request.Fields, Has.Length.EqualTo(1));
        AssertFailure(result, WebSocketParseStatus.LimitRejected, "http-header-count-too-large", 431);
    }

    [Test]
    public void ProtocolFailureIsPersistentAndErrorResultsAreIndependent()
    {
        var parser = CreateParser();
        var first = parser.ReadRequest(Bytes("GET * HTTP/1.1\r\n\r\n"));
        first.Error.Identifier = "mutated";
        first.Error.Diagnostic = "mutated";

        var repeated = parser.ReadRequest(Bytes("GET / HTTP/1.1\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.HasRequest, Is.False);
            Assert.That(repeated.HasError, Is.True);
            Assert.That(repeated.Error, Is.Not.SameAs(first.Error));
            Assert.That(repeated.Error.Identifier, Is.EqualTo("http-request-target-invalid"));
            Assert.That(repeated.Error.Diagnostic, Does.Not.Contain("mutated"));
        });
    }

    [Test]
    public void ResetClearsAProtocolFailure()
    {
        var parser = CreateParser();
        parser.ReadRequest(Bytes("GET * HTTP/1.1\r\n\r\n"));

        parser.ResetRequest();
        var result = parser.ReadRequest(Bytes("GET / HTTP/1.1\r\n\r\n"));

        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
    }

    static Http1RequestParser CreateParser()
    {
        return new Http1RequestParser(1024, 16, 256);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.Latin1.GetBytes(value);
    }

    static void AssertProtocolFailure(string requestText, string identifier)
    {
        var parser = CreateParser();

        var result = parser.ReadRequest(Bytes(requestText));

        AssertFailure(result, WebSocketParseStatus.ProtocolFailure, identifier, 400);
    }

    static void AssertFailure(
        WebSocketHttp1ReadResult result,
        WebSocketParseStatus status,
        string identifier,
        int httpStatus)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.HasRequest, Is.False);
            Assert.That(result.HasResponse, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(
                status == WebSocketParseStatus.LimitRejected
                    ? WebSocketErrorKind.Limit
                    : WebSocketErrorKind.Handshake));
            Assert.That(result.Error.HasHttpStatusCode, Is.True);
            Assert.That(result.Error.HttpStatusCode, Is.EqualTo(httpStatus));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
            Assert.That(result.Error.Diagnostic, Does.Not.Contain("\r").And.Not.Contain("\n"));
        });
    }
}
