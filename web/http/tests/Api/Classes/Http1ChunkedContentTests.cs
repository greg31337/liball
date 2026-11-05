using System.Text;

namespace http.Tests;

public class Http1ChunkedContentTests
{
    [Test]
    public void SameFeedDecodesChunksAndStopsAtTheTerminalBoundary()
    {
        byte[] input = Encoding.ASCII.GetBytes(
            "POST /items HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" +
            "4\r\nWiki\r\n5\r\npedia\r\n0\r\n\r\nNEXT");
        Http1Parser parser = new Http1Parser();

        Http1ParseResult result = parser.ParseHttp1Bytes(input);
        HttpRequest request = result.Events[0].Request!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length - 4));
            Assert.That(result.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Content,
                Http1ParseEventKind.Complete
            }));
            Assert.That(result.Events[1].Content, Is.EqualTo("Wiki"u8.ToArray()));
            Assert.That(result.Events[2].Content, Is.EqualTo("pedia"u8.ToArray()));
            Assert.That(request.Content, Is.EqualTo("Wikipedia"u8.ToArray()));
            Assert.That(request.IsComplete, Is.True);
            Assert.That(result.Events[3].FramingDecision,
                Is.SameAs(result.Events[0].FramingDecision));
        });
    }

    [Test]
    public void SizeDataAndDelimiterFragmentsAreAcceptedWithExtensions()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult first = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" +
            "0"));
        HttpRequest request = first.Events[0].Request!;
        Http1ParseResult sizeTail = parser.ParseHttp1Bytes(
            "A;foo=bar; quoted=\"a,b\\\"c\"\r"u8.ToArray());
        Http1ParseResult firstData = parser.ParseHttp1Bytes("\n01234"u8.ToArray());
        Http1ParseResult secondData = parser.ParseHttp1Bytes("56789\r"u8.ToArray());
        Http1ParseResult terminal = parser.ParseHttp1Bytes("\n0;done\r\n\r"u8.ToArray());
        Http1ParseResult completed = parser.ParseHttp1Bytes("\nNEXT"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Events.Select(item => item.EventKind),
                Is.EqualTo(new[] { Http1ParseEventKind.MessageHead }));
            Assert.That(sizeTail.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(sizeTail.Events, Is.Empty);
            Assert.That(firstData.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(firstData.Events.Single().Content,
                Is.EqualTo("01234"u8.ToArray()));
            Assert.That(secondData.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(secondData.Events.Single().Content,
                Is.EqualTo("56789"u8.ToArray()));
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(terminal.Consumed, Is.EqualTo(10));
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Consumed, Is.EqualTo(1));
            Assert.That(completed.Events.Single().EventKind,
                Is.EqualTo(Http1ParseEventKind.Complete));
            Assert.That(request.Content, Is.EqualTo("0123456789"u8.ToArray()));
        });
    }

    [Test]
    public void BadWhitespaceAroundValidExtensionsIsAccepted()
    {
        Http1ParseResult result = ParseRequest(
            "1 \t; \tfoo \t= \tbar\t ; flag; quoted = \"a,b\\\"c\"\r\n" +
            "x\r\n0 ; final=yes\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events[0].Request!.Content,
                Is.EqualTo("x"u8.ToArray()));
        });
    }

    [TestCase("")]
    [TestCase("+1")]
    [TestCase("-1")]
    [TestCase("0x1")]
    [TestCase(" 1")]
    public void MissingOrNonHexadecimalChunkSizesAreRejected(string line)
    {
        Http1ParseResult result = ParseRequest(line + "\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-http1-chunk-size"));
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [TestCase("1;")]
    [TestCase("1;=x")]
    [TestCase("1;foo=")]
    [TestCase("1;foo=two words")]
    [TestCase("1;foo=\"unterminated")]
    [TestCase("1;foo ")]
    [TestCase("1 ")]
    public void MalformedChunkExtensionsAreRejected(string line)
    {
        Http1ParseResult result = ParseRequest(line + "\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-http1-chunk-extension"));
        });
    }

    [Test]
    public void HexadecimalOverflowIsRejectedBeforeConfiguredLimits()
    {
        Http1ParseResult result = ParseRequest("FFFFFFFFFFFFFFFFF\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-chunk-size-overflow"));
        });
    }

    [Test]
    public void PerChunkLimitAcceptsTheBoundaryAndRejectsLargerChunks()
    {
        Http1ParserOptions exactOptions = new Http1ParserOptions();
        exactOptions.Limits.MaximumChunkSize = 3;
        Http1ParseResult exact = ParseRequest(
            "3\r\nabc\r\n0\r\n\r\n",
            exactOptions);

        Http1ParserOptions smallOptions = new Http1ParserOptions();
        smallOptions.Limits.MaximumChunkSize = 2;
        Http1ParseResult rejected = ParseRequest(
            "3\r\nabc\r\n0\r\n\r\n",
            smallOptions);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(exact.Events[0].Request!.Content,
                Is.EqualTo("abc"u8.ToArray()));
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http1-chunk-limit-exceeded"));
        });
    }

    [Test]
    public void CumulativeContentLimitIsCheckedBeforeTheNextChunkData()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits.MaximumChunkSize = 3;
        options.Limits.MaximumContentSize = 5;

        Http1ParseResult result = ParseRequest(
            "3\r\nabc\r\n3\r\ndef\r\n0\r\n\r\n",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-content-limit-exceeded"));
            Assert.That(result.Events[0].Request!.Content,
                Is.EqualTo("abc"u8.ToArray()));
            Assert.That(result.Events.Count(item =>
                item.EventKind == Http1ParseEventKind.Content), Is.EqualTo(1));
        });
    }

    [Test]
    public void ZeroChunkNeedsTheFollowingEmptyLineAndEmitsNoContent()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult partial = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n"));
        Http1ParseResult completed = parser.ParseHttp1Bytes("\r\nNEXT"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(partial.Events.Select(item => item.EventKind),
                Is.EqualTo(new[] { Http1ParseEventKind.MessageHead }));
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Consumed, Is.EqualTo(2));
            Assert.That(completed.Events.Select(item => item.EventKind),
                Is.EqualTo(new[] { Http1ParseEventKind.Complete }));
        });
    }

    [Test]
    public void InvalidChunkDataDelimiterIsRejected()
    {
        Http1ParseResult result = ParseRequest(
            "1\r\nxX0\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("invalid-http1-chunk-data-ending"));
        });
    }

    [Test]
    public void ExplicitBareLineFeedCompatibilityAppliesToChunkBoundaries()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowBareLineFeed = true;
        byte[] input = Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" +
            "1\nx\n0\n\nNEXT");
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length - 4));
            Assert.That(result.Events[0].Request!.Content,
                Is.EqualTo("x"u8.ToArray()));
        });
    }

    [Test]
    public void EndOfInputBeforeTerminalBoundaryIsRejected()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult partial = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" +
            "2\r\na"));

        Http1ParseResult ended = parser.CompleteHttp1Input();
        Http1ParseResult terminal = parser.ParseHttp1Bytes("b\r\n0\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(partial.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(ended.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(ended.ErrorCode,
                Is.EqualTo("incomplete-http1-chunked-content"));
            Assert.That(ended.IsTerminal, Is.True);
            Assert.That(terminal.Status, Is.EqualTo(HttpOperationStatus.Terminal));
            Assert.That(terminal.Consumed, Is.Zero);
        });
    }

    static Http1ParseResult ParseRequest(string chunkedBody)
    {
        return ParseRequest(chunkedBody, new Http1ParserOptions());
    }

    static Http1ParseResult ParseRequest(
        string chunkedBody,
        Http1ParserOptions options)
    {
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);
        return parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "POST / HTTP/1.1\r\nTransfer-Encoding: chunked\r\n\r\n" +
            chunkedBody));
    }
}
