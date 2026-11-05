using System.Text;

namespace http.Tests;

public class Http1FramingNormalizationTests
{
    [Test]
    public void IdenticalContentLengthsNormalizeAcrossListsAndFieldLines()
    {
        Http1ParseResult result = ParseRequest(
            "POST / HTTP/1.1\r\n" +
            "Content-Length: 005, 5\r\n" +
            "Content-Length: 0005\r\n" +
            "\r\nhello");

        Http1FramingDecision framing = result.Events[0].FramingDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(framing.BodyFraming,
                Is.EqualTo(Http1BodyFraming.ContentLength));
            Assert.That(framing.HasDeclaredLength, Is.True);
            Assert.That(framing.DeclaredLength, Is.EqualTo(5));
        });
    }

    [TestCase("5, 6")]
    [TestCase("5\r\nContent-Length: 6")]
    public void DivergentContentLengthsAreRejected(string fields)
    {
        Http1ParseResult result = ParseRequest(
            "POST / HTTP/1.1\r\nContent-Length: " + fields + "\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("mismatched-http1-content-length"));
            Assert.That(result.Events, Is.Empty);
        });
    }

    [Test]
    public void DivergentContentLengthCompatibilityUsesTheFirstValue()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowMismatchedContentLengths = true;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(
            "POST / HTTP/1.1\r\nContent-Length: 5, 6\r\n\r\nhello"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events[0].FramingDecision!.DeclaredLength,
                Is.EqualTo(5));
        });
    }

    [TestCase("")]
    [TestCase("+5")]
    [TestCase("-5")]
    [TestCase("5x")]
    [TestCase("5 6")]
    [TestCase("5,")]
    [TestCase(",5")]
    [TestCase("5,,5")]
    [TestCase("0x5")]
    public void SignsJunkAndEmptyContentLengthMembersAreRejected(string value)
    {
        Http1ParseResult result = ParseRequest(
            "POST / HTTP/1.1\r\nContent-Length: " + value + "\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-http1-content-length"));
        });
    }

    [Test]
    public void ContentLengthOverflowIsRejectedWithoutWrapping()
    {
        Http1ParseResult result = ParseRequest(
            "POST / HTTP/1.1\r\nContent-Length: 9223372036854775808\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("http1-content-length-overflow"));
        });
    }

    [Test]
    public void TransferCodingsNormalizeAcrossParametersListsAndFieldLines()
    {
        Http1ParseResult result = ParseResponse(
            "HTTP/1.1 200 OK\r\n" +
            "Transfer-Encoding: GZIP; level=9; note=\"a,b\\\"c\"\r\n" +
            "Transfer-Encoding: CHUNKED\r\n" +
            "\r\n" +
            "0\r\n\r\n");

        Http1FramingDecision framing = result.Events[0].FramingDecision!;
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(framing.BodyFraming, Is.EqualTo(Http1BodyFraming.Chunked));
            Assert.That(framing.TransferCodings,
                Is.EqualTo(new[] { "gzip", "chunked" }));
        });
    }

    [TestCase("")]
    [TestCase(",chunked")]
    [TestCase("chunked,")]
    [TestCase("gzip,,chunked")]
    [TestCase("gzip value")]
    [TestCase("gzip;")]
    [TestCase("gzip; level")]
    [TestCase("gzip; level=")]
    [TestCase("gzip; level=\"unterminated")]
    [TestCase("gzip; level=two words")]
    public void MalformedTransferCodingGrammarIsRejected(string value)
    {
        Http1ParseResult result = ParseResponse(
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: " + value + "\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-http1-transfer-encoding"));
        });
    }

    [Test]
    public void ChunkedCannotRepeatOrCarryParameters()
    {
        Http1ParseResult repeated = ParseResponse(
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked, chunked\r\n\r\n");
        Http1ParseResult parameterized = ParseResponse(
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked; mode=yes\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(repeated.ErrorCode,
                Is.EqualTo("repeated-http1-chunked-transfer-coding"));
            Assert.That(parameterized.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parameterized.ErrorCode,
                Is.EqualTo("invalid-http1-chunked-transfer-coding"));
        });
    }

    [Test]
    public void ChunkedMustBeFinalUnlessResponseCompatibilityIsEnabled()
    {
        string message =
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked, gzip\r\n\r\n";
        Http1ParseResult strict = ParseResponse(message);

        Http1ParserOptions options = ResponseOptions();
        options.AllowChunkedNotFinal = true;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        Http1ParseResult compatible = parser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(message));

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(strict.ErrorCode,
                Is.EqualTo("http1-chunked-transfer-coding-not-final"));
            Assert.That(compatible.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(compatible.NeedsEndOfInput, Is.True);
            Assert.That(compatible.Events[0].FramingDecision!.BodyFraming,
                Is.EqualTo(Http1BodyFraming.CloseDelimited));
        });
    }

    [TestCase("gzip, chunked")]
    [TestCase("custom, chunked")]
    public void RequestsRejectTransferCodingsTheParserCannotDecode(string value)
    {
        Http1ParseResult result = ParseRequest(
            "POST / HTTP/1.1\r\nTransfer-Encoding: " + value + "\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("unsupported-http1-request-transfer-coding"));
            Assert.That(result.Events, Is.Empty);
        });
    }

    static Http1ParseResult ParseRequest(string message)
    {
        return new Http1Parser().ParseHttp1Bytes(Encoding.ASCII.GetBytes(message));
    }

    static Http1ParseResult ParseResponse(string message)
    {
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(ResponseOptions());
        return parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(message));
    }

    static Http1ParserOptions ResponseOptions()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Get;
        return options;
    }
}
