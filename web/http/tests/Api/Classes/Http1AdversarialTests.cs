using System.Text;

namespace http.Tests;

public class Http1AdversarialTests
{
    [TestCase("Transfer-Encoding: chunked\r\nContent-Length: 0")]
    [TestCase("Content-Length: 0\r\nTransfer-Encoding: chunked")]
    [TestCase("Transfer-Encoding: chunked\r\nContent-Length: 0\r\nContent-Length: 0")]
    public void TransferEncodingAndContentLengthConflictsFailClosed(string fields)
    {
        Http1ParseResult request = ParseRequest(
            "POST / HTTP/1.1\r\n" + fields + "\r\n\r\n0\r\n\r\n");
        Http1ParseResult response = ParseResponse(
            "HTTP/1.1 200 OK\r\n" + fields + "\r\n\r\n0\r\n\r\n");

        AssertRejected(request,
            "http1-transfer-encoding-content-length-conflict");
        AssertRejected(response,
            "http1-transfer-encoding-content-length-conflict");
    }

    [TestCase("5, 6", "mismatched-http1-content-length")]
    [TestCase("5\r\nContent-Length: 6", "mismatched-http1-content-length")]
    [TestCase("5,,5", "invalid-http1-content-length")]
    [TestCase("+5", "invalid-http1-content-length")]
    [TestCase("5 5", "invalid-http1-content-length")]
    public void DuplicateAndObscuredContentLengthsAreRejected(
        string value,
        string errorCode)
    {
        Http1ParseResult result = ParseRequest(
            "POST / HTTP/1.1\r\nContent-Length: " + value + "\r\n\r\n");

        AssertRejected(result, errorCode);
    }

    [TestCase("chunked, gzip", "http1-chunked-transfer-coding-not-final")]
    [TestCase("chunked, chunked", "repeated-http1-chunked-transfer-coding")]
    [TestCase("chunked; mode=yes", "invalid-http1-chunked-transfer-coding")]
    [TestCase("gzip,,chunked", "invalid-http1-transfer-encoding")]
    [TestCase("gzip; level=", "invalid-http1-transfer-encoding")]
    public void MalformedTransferCodingOrderAndGrammarAreRejected(
        string value,
        string errorCode)
    {
        Http1ParseResult result = ParseResponse(
            "HTTP/1.1 200 OK\r\nTransfer-Encoding: " + value + "\r\n\r\n");

        AssertRejected(result, errorCode);
    }

    [Test]
    public void LineAndControlInjectionVectorsAreRejected()
    {
        Http1ParseResult bareLineFeed = ParseRequest(
            "GET / HTTP/1.1\nX-Injected: yes\r\n\r\n");
        Http1ParseResult bareResponseLineFeed = ParseResponse(
            "HTTP/1.1 200 OK\nX-Injected: yes\r\n\r\n");
        Http1ParseResult foldedField = ParseRequest(
            "GET / HTTP/1.1\r\nX-Safe: yes\r\n X-Injected: yes\r\n\r\n");
        Http1ParseResult whitespaceBeforeColon = ParseRequest(
            "GET / HTTP/1.1\r\nHost : safe.test\r\n\r\n");
        Http1ParseResult requestControl = ParseRequestBytes(
            Join("GET /"u8.ToArray(), [0x00, 0x20, 0x48, 0x54, 0x54,
                0x50, 0x2f, 0x31, 0x2e, 0x31, 0x0d, 0x0a]));
        Http1ParseResult fieldControl = ParseRequestBytes(
            Join("GET / HTTP/1.1\r\nX-Test: safe"u8.ToArray(),
                [0x00, 0x0d, 0x0a, 0x0d, 0x0a]));
        Http1ParseResult responseControl = ParseResponseBytes(
            Join("HTTP/1.1 200 OK"u8.ToArray(),
                [0x00, 0x0d, 0x0a, 0x0d, 0x0a]));

        Assert.Multiple(() =>
        {
            Assert.That(bareLineFeed.ErrorCode,
                Is.EqualTo("invalid-http1-line-ending"));
            Assert.That(bareResponseLineFeed.ErrorCode,
                Is.EqualTo("invalid-http1-line-ending"));
            Assert.That(foldedField.ErrorCode,
                Is.EqualTo("obsolete-http1-line-folding"));
            Assert.That(whitespaceBeforeColon.ErrorCode,
                Is.EqualTo("http1-whitespace-before-colon"));
            Assert.That(requestControl.ErrorCode,
                Is.EqualTo("invalid-http1-request-line-control"));
            Assert.That(fieldControl.ErrorCode,
                Is.EqualTo("invalid-http1-field-value"));
            Assert.That(responseControl.ErrorCode,
                Is.EqualTo("invalid-http1-reason-phrase"));
            Assert.That(new[]
            {
                bareLineFeed.Status,
                bareResponseLineFeed.Status,
                foldedField.Status,
                whitespaceBeforeColon.Status,
                requestControl.Status,
                fieldControl.Status,
                responseControl.Status
            }, Has.All.EqualTo(HttpOperationStatus.Rejected));
        });
    }

    [Test]
    public void FramingNumbersCannotOverflowTheirSignedRepresentation()
    {
        Http1ParseResult contentLength = ParseRequest(
            "POST / HTTP/1.1\r\n" +
            "Content-Length: 9223372036854775808\r\n\r\n");
        Http1ParseResult chunkSize = ParseRequest(
            "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" +
            "FFFFFFFFFFFFFFFFF\r\n");

        AssertRejected(contentLength, "http1-content-length-overflow");
        AssertRejected(chunkSize, "http1-chunk-size-overflow");
    }

    [Test]
    public void CloseDelimitedCompletionRejectsLaterProtocolBytesByDefault()
    {
        Http1Parser strictParser = ResponseParser(new Http1ParserOptions());
        Http1ParseResult body = strictParser.ParseHttp1Bytes(
            "HTTP/1.1 200 OK\r\n\r\nbody"u8.ToArray());
        Http1ParseResult completed = strictParser.CompleteHttp1Input();
        byte[] injected = "HTTP/1.1 204 No Content\r\n\r\n"u8.ToArray();
        Http1ParseResult rejected = strictParser.ParseHttp1Bytes(injected);

        Http1ParserOptions compatibleOptions = new Http1ParserOptions();
        compatibleOptions.AllowPostCloseData = true;
        Http1Parser compatibleParser = ResponseParser(compatibleOptions);
        compatibleParser.ParseHttp1Bytes(
            "HTTP/1.1 200 OK\r\n\r\nbody"u8.ToArray());
        compatibleParser.CompleteHttp1Input();
        Http1ParseResult allowed = compatibleParser.ParseHttp1Bytes(injected);

        Assert.Multiple(() =>
        {
            Assert.That(body.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http1-post-close-data"));
            Assert.That(rejected.Consumed, Is.Zero);
            Assert.That(rejected.Unconsumed, Is.EqualTo(injected));
            Assert.That(allowed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(allowed.Consumed, Is.Zero);
            Assert.That(allowed.Unconsumed, Is.EqualTo(injected));
        });
    }

    [TestCase("start", "http1-start-line-limit-exceeded")]
    [TestCase("field-line", "http1-field-line-limit-exceeded")]
    [TestCase("field-section", "http1-field-section-limit-exceeded")]
    [TestCase("field-count", "http1-field-count-limit-exceeded")]
    [TestCase("field-value", "http1-field-value-limit-exceeded")]
    [TestCase("trailer-section", "http1-trailer-section-limit-exceeded")]
    [TestCase("trailer-count", "http1-trailer-count-limit-exceeded")]
    [TestCase("content", "http1-content-limit-exceeded")]
    [TestCase("chunk", "http1-chunk-limit-exceeded")]
    [TestCase("buffer", "http1-buffer-limit-exceeded")]
    public void EveryConfiguredHttp1LimitFailsWithItsStableDiagnostic(
        string selected,
        string errorCode)
    {
        Http1ParseResult result = ParseLimitAttack(selected);

        AssertRejected(result, errorCode);
    }

    static Http1ParseResult ParseLimitAttack(string selected)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        string message = "GET / HTTP/1.1\r\nA: x\r\n\r\n";
        switch (selected)
        {
            case "start":
                options.Limits.MaximumStartLineSize = 1;
                break;
            case "field-line":
                options.Limits.MaximumFieldLineSize = 1;
                break;
            case "field-section":
                options.Limits.MaximumFieldSectionSize = 0;
                break;
            case "field-count":
                options.Limits.MaximumFieldCount = 0;
                break;
            case "field-value":
                options.Limits.MaximumFieldValueSize = 0;
                break;
            case "trailer-section":
                options.Limits.MaximumTrailerSectionSize = 0;
                message = ChunkedRequest("0\r\nA: x\r\n\r\n");
                break;
            case "trailer-count":
                options.Limits.MaximumTrailerCount = 0;
                message = ChunkedRequest("0\r\nA: x\r\n\r\n");
                break;
            case "content":
                options.Limits.MaximumContentSize = 0;
                message = "POST / HTTP/1.1\r\nContent-Length: 1\r\n\r\nx";
                break;
            case "chunk":
                options.Limits.MaximumChunkSize = 0;
                message = ChunkedRequest("1\r\nx\r\n0\r\n\r\n");
                break;
            default:
                options.Limits.MaximumBufferedSize = 1;
                break;
        }

        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(message));
    }

    static string ChunkedRequest(string body)
    {
        return "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" + body;
    }

    static Http1ParseResult ParseRequest(string message)
    {
        return ParseRequestBytes(Encoding.ASCII.GetBytes(message));
    }

    static Http1ParseResult ParseRequestBytes(byte[] message)
    {
        return new Http1Parser().ParseHttp1Bytes(message);
    }

    static Http1ParseResult ParseResponse(string message)
    {
        return ParseResponseBytes(Encoding.ASCII.GetBytes(message));
    }

    static Http1ParseResult ParseResponseBytes(byte[] message)
    {
        return ResponseParser(new Http1ParserOptions()).ParseHttp1Bytes(message);
    }

    static Http1Parser ResponseParser(Http1ParserOptions options)
    {
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Get;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser;
    }

    static void AssertRejected(Http1ParseResult result, string errorCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    static byte[] Join(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        Array.Copy(first, result, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
