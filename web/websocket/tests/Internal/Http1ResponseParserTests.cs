using System.Text;
using websocket;

namespace websocket.Tests;

public class Http1ResponseParserTests
{
    [Test]
    public void ParsesFinalResponseMetadataDuplicatesAndOpaqueOctets()
    {
        const string headerText =
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection:\t Upgrade \t\r\n" +
            "Set-Cookie: one\r\n" +
            "Set-Cookie: two\u0080\r\n" +
            "\r\n";
        var header = Encoding.Latin1.GetBytes(headerText);
        var source = header.Concat(new byte[] { 0x81, 0x00 }).ToArray();
        var parser = CreateParser();

        var result = parser.ReadResponse(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(header.Length));
            Assert.That(result.HasRequest, Is.False);
            Assert.That(result.HasResponse, Is.True);
            Assert.That(result.HasError, Is.False);
            Assert.That(result.Response.Transport, Is.EqualTo(WebSocketHandshakeTransport.Http1Upgrade));
            Assert.That(result.Response.StatusCode, Is.EqualTo(101));
            Assert.That(result.Response.HasReasonPhrase, Is.True);
            Assert.That(result.Response.ReasonPhrase, Is.EqualTo("Switching Protocols"));
            Assert.That(result.Response.HasBody, Is.False);
            Assert.That(result.Response.Body, Is.Empty);
            Assert.That(result.Response.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "Upgrade", "Connection", "Set-Cookie", "Set-Cookie" }));
            Assert.That(result.Response.Fields.Select(field => field.Value),
                Is.EqualTo(new[] { "websocket", "Upgrade", "one", "two\u0080" }));
        });
    }

    [TestCase("HTTP/1.1 204 ", false, "")]
    [TestCase("HTTP/1.0 403 Forbidden access", true, "Forbidden access")]
    [TestCase("HTTP/2.0 200  ", true, " ")]
    [TestCase("HTTP/9.9 599 Last\tKnown\u0080", true, "Last\tKnown\u0080")]
    public void ParsesEmptyAndPresentReasonPhrases(
        string statusLine,
        bool expectedPresence,
        string expectedReason)
    {
        var parser = CreateParser();

        var result = parser.ReadResponse(Bytes(statusLine + "\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Response.HasReasonPhrase, Is.EqualTo(expectedPresence));
            Assert.That(result.Response.ReasonPhrase, Is.EqualTo(expectedReason));
        });
    }

    [Test]
    public void FinalResponseCanArriveOneByteAtATime()
    {
        var source = Bytes(
            "HTTP/1.1 403 Forbidden\r\n" +
            "Content-Type: text/plain\r\n\r\n");
        var parser = CreateParser();

        for (var index = 0; index < source.Length; index++)
        {
            var result = parser.ReadResponse(source.AsSpan(index, 1));

            Assert.That(result.ConsumedCount, Is.EqualTo(1), $"Byte {index}");
            if (index + 1 == source.Length)
            {
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
                Assert.That(result.Response.StatusCode, Is.EqualTo(403));
            }
            else
            {
                Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput), $"Byte {index}");
                Assert.That(result.HasResponse, Is.False, $"Byte {index}");
            }
        }
    }

    [Test]
    public void FinalResponseCompletesAcrossEveryInputSplit()
    {
        var source = Bytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\n\r\n");

        for (var split = 0; split < source.Length; split++)
        {
            var parser = CreateParser();
            var first = parser.ReadResponse(source.AsSpan(0, split));
            var second = parser.ReadResponse(source.AsSpan(split));

            Assert.Multiple(() =>
            {
                Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput), $"Split {split}");
                Assert.That(first.ConsumedCount, Is.EqualTo(split), $"Split {split}");
                Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete), $"Split {split}");
                Assert.That(second.ConsumedCount, Is.EqualTo(source.Length - split), $"Split {split}");
                Assert.That(second.Response.StatusCode, Is.EqualTo(101), $"Split {split}");
            });
        }
    }

    [Test]
    public void SurfacesInformationalResponsesThenOneFinalResponse()
    {
        var firstBytes = Bytes("HTTP/1.1 100 Continue\r\nX-Step: one\r\n\r\n");
        var secondBytes = Bytes("HTTP/1.1 103 Early Hints\r\nLink: </style.css>\r\n\r\n");
        var finalBytes = Bytes("HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\n\r\n");
        var frameBytes = new byte[] { 0x81, 0x00 };
        var source = firstBytes.Concat(secondBytes).Concat(finalBytes).Concat(frameBytes).ToArray();
        var parser = CreateParser();

        var first = parser.ReadResponse(source);
        var secondOffset = first.ConsumedCount;
        var second = parser.ReadResponse(source.AsSpan(secondOffset));
        var finalOffset = secondOffset + second.ConsumedCount;
        var final = parser.ReadResponse(source.AsSpan(finalOffset));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first.ConsumedCount, Is.EqualTo(firstBytes.Length));
            Assert.That(first.Response.StatusCode, Is.EqualTo(100));
            Assert.That(first.Response.Fields.Single().Value, Is.EqualTo("one"));
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.ConsumedCount, Is.EqualTo(secondBytes.Length));
            Assert.That(second.Response.StatusCode, Is.EqualTo(103));
            Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.ConsumedCount, Is.EqualTo(finalBytes.Length));
            Assert.That(final.Response.StatusCode, Is.EqualTo(101));
            Assert.That(source.Length - finalOffset - final.ConsumedCount, Is.EqualTo(frameBytes.Length));
        });
    }

    [Test]
    public void InformationalResultIsDeliveredOnceBeforeAwaitingTheNextResponse()
    {
        var parser = CreateParser();
        var interim = parser.ReadResponse(Bytes("HTTP/1.1 102 Processing\r\n\r\n"));

        var empty = parser.ReadResponse(ReadOnlySpan<byte>.Empty);
        var final = parser.ReadResponse(Bytes("HTTP/1.1 204 \r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(interim.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(interim.Response.StatusCode, Is.EqualTo(102));
            Assert.That(empty.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(empty.HasResponse, Is.False);
            Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.Response.StatusCode, Is.EqualTo(204));
        });
    }

    [Test]
    public void InformationalResponsesReceiveIndependentPerResponseLimits()
    {
        var parser = new Http1ResponseParser(64, 1, 32);
        var interimBytes = Bytes("HTTP/1.1 103 Hints\r\nLink: one\r\n\r\n");
        var finalBytes = Bytes("HTTP/1.1 101 Upgrade\r\nUpgrade: websocket\r\n\r\n");

        var interim = parser.ReadResponse(interimBytes);
        var final = parser.ReadResponse(finalBytes);

        Assert.Multiple(() =>
        {
            Assert.That(interim.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(interim.Response.Fields, Has.Length.EqualTo(1));
            Assert.That(final.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(final.Response.Fields, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void SwitchingProtocolsIsTerminalAndLeavesFrameBytesUntouched()
    {
        var header = Bytes("HTTP/1.1 101 Switching Protocols\r\n\r\n");
        var source = header.Concat(new byte[] { 0x81, 0x00 }).ToArray();
        var parser = CreateParser();

        var first = parser.ReadResponse(source);
        var repeated = parser.ReadResponse(source.AsSpan(first.ConsumedCount));

        Assert.Multiple(() =>
        {
            Assert.That(first.ConsumedCount, Is.EqualTo(header.Length));
            Assert.That(first.Response.StatusCode, Is.EqualTo(101));
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Response.StatusCode, Is.EqualTo(101));
        });
    }

    [Test]
    public void NonInformationalResponseLeavesRejectionBodyUntouched()
    {
        var header = Bytes("HTTP/1.1 403 Forbidden\r\nContent-Length: 4\r\n\r\n");
        var body = Bytes("nope");
        var source = header.Concat(body).ToArray();
        var parser = CreateParser();

        var result = parser.ReadResponse(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(header.Length));
            Assert.That(result.Response.StatusCode, Is.EqualTo(403));
            Assert.That(result.Response.HasBody, Is.False);
            Assert.That(source.AsSpan(result.ConsumedCount).ToArray(), Is.EqualTo(body));
        });
    }

    [Test]
    public void FinalResultIsPersistentAndIndependentlyOwnedUntilReset()
    {
        var parser = CreateParser();
        var first = parser.ReadResponse(Bytes(
            "HTTP/1.1 403 Forbidden\r\nX-Value: original\r\n\r\n"));
        first.Response.ReasonPhrase = "mutated";
        first.Response.Fields[0].Value = "mutated";

        var repeated = parser.ReadResponse(Bytes("HTTP/1.1 200 Ignored\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Response, Is.Not.SameAs(first.Response));
            Assert.That(repeated.Response.StatusCode, Is.EqualTo(403));
            Assert.That(repeated.Response.ReasonPhrase, Is.EqualTo("Forbidden"));
            Assert.That(repeated.Response.Fields[0], Is.Not.SameAs(first.Response.Fields[0]));
            Assert.That(repeated.Response.Fields[0].Value, Is.EqualTo("original"));
        });
    }

    [Test]
    public void ResetAllowsAnotherFinalResponse()
    {
        var parser = CreateParser();
        parser.ReadResponse(Bytes("HTTP/1.1 403 Forbidden\r\n\r\n"));

        parser.ResetResponse();
        var second = parser.ReadResponse(Bytes("HTTP/1.0 200 OK\r\nServer: two\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.Response.StatusCode, Is.EqualTo(200));
            Assert.That(second.Response.ReasonPhrase, Is.EqualTo("OK"));
            Assert.That(second.Response.Fields.Single().Value, Is.EqualTo("two"));
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
            new Http1ResponseParser(maximumBytes, maximumCount, maximumLine));
    }

    [TestCase("")]
    [TestCase("HTTP/1.1")]
    [TestCase("HTTP/1.1 200")]
    [TestCase("http/1.1 200 ")]
    [TestCase("HTTP/1.10 200 ")]
    [TestCase("HTTP/1.1  200 ")]
    [TestCase("HTTP/1.1\t200 ")]
    [TestCase("HTTP/1.1 200\tOK")]
    [TestCase("HTTP/1.1 20 ")]
    [TestCase("HTTP/1.1 2000 ")]
    [TestCase("HTTP/1.1 2A0 ")]
    [TestCase("HTTP/1.1 099 Invalid")]
    [TestCase("HTTP/1.1 600 Invalid")]
    [TestCase("HTTP/1.1 999 Invalid")]
    public void RejectsMalformedStatusLines(string statusLine)
    {
        AssertProtocolFailure(
            statusLine + "\r\n\r\n",
            "http-response-status-line-invalid");
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(8)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(31)]
    [TestCase(127)]
    public void RejectsUnsafeReasonPhraseControls(int control)
    {
        var prefix = Bytes("HTTP/1.1 200 Safe");
        var suffix = Bytes("text\r\n\r\n");
        var source = new byte[prefix.Length + 1 + suffix.Length];
        prefix.CopyTo(source, 0);
        source[prefix.Length] = (byte)control;
        suffix.CopyTo(source, prefix.Length + 1);
        var parser = CreateParser();

        var result = parser.ReadResponse(source);

        AssertFailure(result, WebSocketParseStatus.ProtocolFailure, "http-response-reason-invalid", 502);
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
            "HTTP/1.1 200 OK\r\n" + fieldLine + "\r\n\r\n",
            "http-header-name-invalid");
    }

    [TestCase(" continuation")]
    [TestCase("\tcontinuation")]
    public void RejectsObsoleteLineFolding(string foldedLine)
    {
        AssertProtocolFailure(
            "HTTP/1.1 200 OK\r\nX-Test: one\r\n" + foldedLine + "\r\n\r\n",
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
        var prefix = Bytes("HTTP/1.1 200 OK\r\nX-Test: safe");
        var suffix = Bytes("value\r\n\r\n");
        var source = new byte[prefix.Length + 1 + suffix.Length];
        prefix.CopyTo(source, 0);
        source[prefix.Length] = (byte)control;
        suffix.CopyTo(source, prefix.Length + 1);
        var parser = CreateParser();

        var result = parser.ReadResponse(source);

        AssertFailure(result, WebSocketParseStatus.ProtocolFailure, "http-header-value-invalid", 502);
    }

    [TestCase("HTTP/1.1 200 OK\n\n")]
    [TestCase("HTTP/1.1 200 OK\rX")]
    [TestCase("HTTP/1.1 200 OK\r\nServer: example.test\n\n")]
    [TestCase("HTTP/1.1 200 OK\r\nServer: example.test\r\n\n")]
    public void RejectsBareOrBrokenLineEndings(string responseText)
    {
        AssertProtocolFailure(responseText, "http-line-ending-invalid");
    }

    [Test]
    public void ExactLineLimitIsAcceptedAndNextByteIsRejected()
    {
        const string statusLine = "HTTP/1.1 200 ";
        var exactParser = new Http1ResponseParser(128, 4, statusLine.Length);
        var tooLongParser = new Http1ResponseParser(128, 4, statusLine.Length - 1);

        var exact = exactParser.ReadResponse(Bytes(statusLine + "\r\n\r\n"));
        var tooLong = tooLongParser.ReadResponse(Bytes(statusLine + "\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(tooLong.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(tooLong.ConsumedCount, Is.EqualTo(statusLine.Length));
            Assert.That(tooLong.Error.Identifier, Is.EqualTo("http-header-line-too-long"));
        });
    }

    [Test]
    public void HeaderFieldLineLimitIsEnforced()
    {
        const string statusLine = "HTTP/1.1 200 ";
        const string longField = "Long: 12345678";
        var parser = new Http1ResponseParser(128, 4, statusLine.Length);

        var result = parser.ReadResponse(Bytes(statusLine + "\r\n" + longField + "\r\n\r\n"));

        AssertFailure(result, WebSocketParseStatus.LimitRejected, "http-header-line-too-long", 431);
    }

    [Test]
    public void ExactTotalByteLimitIsAcceptedAndIncompleteLimitIsRejected()
    {
        var source = Bytes("HTTP/1.1 200 \r\n\r\n");
        var exactParser = new Http1ResponseParser(source.Length, 1, 13);
        var shortParser = new Http1ResponseParser(source.Length - 1, 1, 13);

        var exact = exactParser.ReadResponse(source);
        var tooLarge = shortParser.ReadResponse(source);

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
        var exactParser = new Http1ResponseParser(256, 1, 64);
        var parser = new Http1ResponseParser(256, 1, 64);
        var exact = exactParser.ReadResponse(Bytes(
            "HTTP/1.1 200 OK\r\n" +
            "Server: example.test\r\n\r\n"));
        var source = Bytes(
            "HTTP/1.1 200 OK\r\n" +
            "Server: example.test\r\n" +
            "Upgrade: websocket\r\n\r\n");

        var result = parser.ReadResponse(source);

        Assert.That(exact.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        Assert.That(exact.Response.Fields, Has.Length.EqualTo(1));
        AssertFailure(result, WebSocketParseStatus.LimitRejected, "http-header-count-too-large", 431);
    }

    [Test]
    public void ProtocolFailureIsPersistentAndErrorResultsAreIndependent()
    {
        var parser = CreateParser();
        var first = parser.ReadResponse(Bytes("HTTP/1.1 999 Invalid\r\n\r\n"));
        first.Error.Identifier = "mutated";
        first.Error.Diagnostic = "mutated";

        var repeated = parser.ReadResponse(Bytes("HTTP/1.1 200 OK\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.HasResponse, Is.False);
            Assert.That(repeated.HasError, Is.True);
            Assert.That(repeated.Error, Is.Not.SameAs(first.Error));
            Assert.That(repeated.Error.Identifier, Is.EqualTo("http-response-status-line-invalid"));
            Assert.That(repeated.Error.Diagnostic, Does.Not.Contain("mutated"));
        });
    }

    [Test]
    public void ResetClearsAProtocolFailure()
    {
        var parser = CreateParser();
        parser.ReadResponse(Bytes("HTTP/1.1 999 Invalid\r\n\r\n"));

        parser.ResetResponse();
        var result = parser.ReadResponse(Bytes("HTTP/1.1 200 OK\r\n\r\n"));

        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
    }

    static Http1ResponseParser CreateParser()
    {
        return new Http1ResponseParser(1024, 16, 256);
    }

    static byte[] Bytes(string value)
    {
        return Encoding.Latin1.GetBytes(value);
    }

    static void AssertProtocolFailure(string responseText, string identifier)
    {
        var parser = CreateParser();

        var result = parser.ReadResponse(Bytes(responseText));

        AssertFailure(result, WebSocketParseStatus.ProtocolFailure, identifier, 502);
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
