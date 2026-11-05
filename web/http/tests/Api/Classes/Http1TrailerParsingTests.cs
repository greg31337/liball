using System.Text;

namespace http.Tests;

public class Http1TrailerParsingTests
{
    [Test]
    public void DeclaredTrailersPreserveOrderAndPrecedeCompletion()
    {
        byte[] input = Encoding.ASCII.GetBytes(
            "POST /items HTTP/1.1\r\n" +
            "Transfer-Encoding: chunked\r\n" +
            "Trailer: Digest, X-Trace\r\n" +
            "\r\n" +
            "3\r\nabc\r\n" +
            "0\r\n" +
            "Digest: sha-256=:YWJj:\r\n" +
            "X-Trace: first\r\n" +
            "X-Trace: second\r\n" +
            "\r\nNEXT");
        Http1Parser parser = new Http1Parser();

        Http1ParseResult result = parser.ParseHttp1Bytes(input);
        HttpRequest request = result.Events[0].Request!;
        Http1ParseEvent trailerEvent = result.Events[2];

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length - 4));
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Trailers,
                Http1ParseEventKind.Complete
            }));
            Assert.That(trailerEvent.MessageKind, Is.EqualTo(HttpMessageKind.Trailers));
            Assert.That(trailerEvent.Trailers, Is.SameAs(request.Trailers));
            Assert.That(trailerEvent.Request, Is.SameAs(request));
            Assert.That(trailerEvent.FramingDecision,
                Is.SameAs(result.Events[0].FramingDecision));
            Assert.That(request.Trailers.Fields.Select(item => item.Name),
                Is.EqualTo(new[] { "Digest", "X-Trace", "X-Trace" }));
            Assert.That(Value(request.Trailers.Fields[0]),
                Is.EqualTo("sha-256=:YWJj:"));
            Assert.That(Value(request.Trailers.Fields[1]), Is.EqualTo("first"));
            Assert.That(Value(request.Trailers.Fields[2]), Is.EqualTo("second"));
            Assert.That(request.IsComplete, Is.True);
        });
    }

    [Test]
    public void UndeclaredAndDifferentlyDeclaredTrailersAreAccepted()
    {
        Http1ParseResult undeclared = ParseRequest(
            "",
            "0\r\nX-Checksum: final\r\n\r\n",
            new Http1ParserOptions());
        Http1ParseResult advisory = ParseRequest(
            "Trailer: Expected-Only\r\n",
            "0\r\nActual-Only: present\r\n\r\n",
            new Http1ParserOptions());

        Assert.Multiple(() =>
        {
            Assert.That(undeclared.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(undeclared.Events[1].EventKind,
                Is.EqualTo(Http1ParseEventKind.Trailers));
            Assert.That(undeclared.Events[1].Request!.Trailers.Fields[0].Name,
                Is.EqualTo("X-Checksum"));
            Assert.That(advisory.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(advisory.Events[1].Request!.Trailers.Fields[0].Name,
                Is.EqualTo("Actual-Only"));
        });
    }

    [Test]
    public void FragmentedTrailerLinesEmitOnlyAfterTheTerminalEmptyLine()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult first = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" +
            "0\r\nDigest: sh"));
        HttpRequest request = first.Events[0].Request!;
        Http1ParseResult second = parser.ParseHttp1Bytes(
            "a-256\r\nX-Trace: one\r\n\r"u8.ToArray());
        Http1ParseResult completed = parser.ParseHttp1Bytes("\nNEXT"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Events.Select(item => item.EventKind),
                Is.EqualTo(new[] { Http1ParseEventKind.MessageHead }));
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(second.Events, Is.Empty);
            Assert.That(request.Trailers.Fields, Has.Count.EqualTo(2));
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Consumed, Is.EqualTo(1));
            Assert.That(completed.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.Trailers,
                Http1ParseEventKind.Complete
            }));
            Assert.That(Value(request.Trailers.Fields[0]), Is.EqualTo("sha-256"));
        });
    }

    [Test]
    public void ResponseTrailersAreAttachedToTheResponse()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Get;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\n\r\n" +
            "0\r\nDigest: response-value\r\n\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Trailers,
                Http1ParseEventKind.Complete
            }));
            Assert.That(result.Events[1].Trailers,
                Is.SameAs(result.Events[0].Response!.Trailers));
            Assert.That(Value(result.Events[0].Response!.Trailers.Fields[0]),
                Is.EqualTo("response-value"));
        });
    }

    [TestCase("Content-Length")]
    [TestCase("Transfer-Encoding")]
    [TestCase("Host")]
    [TestCase("Authorization")]
    [TestCase("Trailer")]
    public void ForbiddenTrailerFieldsAreRejected(string name)
    {
        Http1ParseResult result = ParseRequest(
            "",
            "0\r\n" + name + ": value\r\n\r\n",
            new Http1ParserOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("forbidden-http1-trailer-field"));
            Assert.That(result.Events.All(item =>
                item.EventKind != Http1ParseEventKind.Trailers), Is.True);
            Assert.That(result.Events[0].Request!.IsComplete, Is.False);
        });
    }

    [TestCase("Bad Name: value", "invalid-http1-field-name")]
    [TestCase("Missing-Colon", "invalid-http1-field-name")]
    [TestCase("Digest: bad\u0001value", "invalid-http1-field-value")]
    public void MalformedTrailerFieldLinesAreRejected(string line, string errorCode)
    {
        Http1ParseResult result = ParseRequest(
            "",
            "0\r\n" + line + "\r\n\r\n",
            new Http1ParserOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        });
    }

    [Test]
    public void TrailerDeclarationsRejectMalformedAndForbiddenNames()
    {
        Http1ParseResult malformed = ParseRequest(
            "Trailer: Digest, bad:name\r\n",
            "0\r\n\r\n",
            new Http1ParserOptions());
        Http1ParseResult forbidden = ParseRequest(
            "Trailer: Content-Length\r\n",
            "0\r\n\r\n",
            new Http1ParserOptions());

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(malformed.ErrorCode,
                Is.EqualTo("invalid-http1-trailer-declaration"));
            Assert.That(malformed.Events, Is.Empty);
            Assert.That(forbidden.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(forbidden.ErrorCode,
                Is.EqualTo("forbidden-http1-trailer-declaration"));
            Assert.That(forbidden.Events, Is.Empty);
        });
    }

    [Test]
    public void TrailerDeclarationMemberLimitSpansMultipleFieldLines()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits.MaximumListMemberCount = 2;

        Http1ParseResult result = ParseRequest(
            "Trailer: One, Two\r\nTrailer: Three\r\n",
            "0\r\n\r\n",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-trailer-declaration-limit-exceeded"));
            Assert.That(result.Events, Is.Empty);
        });
    }

    [TestCase("field-line", "http1-field-line-limit-exceeded")]
    [TestCase("field-value", "http1-field-value-limit-exceeded")]
    [TestCase("count", "http1-trailer-count-limit-exceeded")]
    [TestCase("section", "http1-trailer-section-limit-exceeded")]
    public void ConfiguredTrailerLimitsAreEnforced(string selected, string errorCode)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        string trailers;
        switch (selected)
        {
            case "field-line":
                options.Limits.MaximumFieldLineSize = 26;
                trailers = "X-Very-Long-Trailer: 123456\r\n";
                break;
            case "field-value":
                options.Limits.MaximumFieldValueSize = 7;
                trailers = "Digest: 12345678\r\n";
                break;
            case "count":
                options.Limits.MaximumTrailerCount = 1;
                trailers = "A: 1\r\nB: 2\r\n";
                break;
            default:
                options.Limits.MaximumTrailerSectionSize = 7;
                trailers = "A: 1\r\nB: 2\r\n";
                break;
        }

        Http1ParseResult result = ParseRequest(
            "",
            "0\r\n" + trailers + "\r\n",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
        });
    }

    [Test]
    public void TrailerLimitsAcceptExactBoundaries()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits.MaximumFieldLineSize = 26;
        options.Limits.MaximumFieldValueSize = 7;
        options.Limits.MaximumTrailerCount = 1;
        options.Limits.MaximumTrailerSectionSize = 26;

        Http1ParseResult result = ParseRequest(
            "",
            "0\r\nX-Very-Long-Trailer: 12345\r\n\r\n",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events[1].EventKind,
                Is.EqualTo(Http1ParseEventKind.Trailers));
        });
    }

    [Test]
    public void ObsoleteTrailerFoldingRequiresExplicitCompatibility()
    {
        string body = "0\r\nDigest: one\r\n two\r\n\r\n";
        Http1ParseResult strict = ParseRequest(
            "",
            body,
            new Http1ParserOptions());

        Http1ParserOptions compatibleOptions = new Http1ParserOptions();
        compatibleOptions.AllowObsoleteLineFolding = true;
        Http1ParseResult compatible = ParseRequest(
            "",
            body,
            compatibleOptions);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(strict.ErrorCode,
                Is.EqualTo("obsolete-http1-line-folding"));
            Assert.That(compatible.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Value(compatible.Events[0].Request!.Trailers.Fields[0]),
                Is.EqualTo("one two"));
        });
    }

    static Http1ParseResult ParseRequest(
        string headerFields,
        string chunkedBody,
        Http1ParserOptions options)
    {
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\n" +
            "Transfer-Encoding: chunked\r\n" +
            headerFields +
            "\r\n" +
            chunkedBody));
    }

    static string Value(HttpHeaderField field)
    {
        return Encoding.ASCII.GetString(field.Value);
    }
}
