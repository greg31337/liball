using System.Text;

namespace http.Tests;

public class Http1LimitTests
{
    [Test]
    public void ParserEnforcesTheSharedDefaultStartLineLimit()
    {
        Http1Parser parser = new Http1Parser();
        string request = "GET /" +
            new string('a', new HttpLimits().MaximumStartLineSize) +
            " HTTP/1.1\r\n";

        Http1ParseResult result = parser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(request));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http1-start-line-limit-exceeded"));
            Assert.That(result.HasErrorPosition, Is.True);
        });
    }

    [Test]
    public void FieldLimitsAcceptTheirExactBoundaries()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowObsoleteLineFolding = true;
        options.Limits.MaximumFieldLineSize = 5;
        options.Limits.MaximumFieldSectionSize = 12;
        options.Limits.MaximumFieldCount = 2;
        options.Limits.MaximumFieldValueSize = 4;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "GET / HTTP/1.1\r\n" +
            "A: ab\r\n" +
            " c\r\n" +
            "B: xy\r\n" +
            "\r\n"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events[0].Request!.Headers.Fields, Has.Count.EqualTo(2));
            Assert.That(result.Events[0].Request!.Headers.Fields[0].Value,
                Is.EqualTo("ab c"u8.ToArray()));
            Assert.That(result.Events[0].Request!.Headers.Fields[1].Value,
                Is.EqualTo("xy"u8.ToArray()));
        });
    }

    [TestCase(4, 100, 100, 8192, "http1-field-line-limit-exceeded")]
    [TestCase(8192, 4, 100, 8192, "http1-field-section-limit-exceeded")]
    [TestCase(8192, 65536, 0, 8192, "http1-field-count-limit-exceeded")]
    [TestCase(8192, 65536, 100, 1, "http1-field-value-limit-exceeded")]
    public void EachFieldResourceHasADistinctStableFailure(
        int lineLimit,
        int sectionLimit,
        int countLimit,
        int valueLimit,
        string errorCode)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits.MaximumFieldLineSize = lineLimit;
        options.Limits.MaximumFieldSectionSize = sectionLimit;
        options.Limits.MaximumFieldCount = countLimit;
        options.Limits.MaximumFieldValueSize = valueLimit;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nA: ab\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [Test]
    public void TotalFieldSectionAndCountAccumulateAcrossDuplicateLines()
    {
        Http1ParserOptions sectionOptions = new Http1ParserOptions();
        sectionOptions.Limits.MaximumFieldSectionSize = 7;
        Http1Parser sectionParser = new Http1Parser();
        sectionParser.ResetHttp1Parser(sectionOptions);
        Http1ParseResult sectionResult = sectionParser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nA: 1\r\nA: 2\r\n\r\n"u8.ToArray());

        Http1ParserOptions countOptions = new Http1ParserOptions();
        countOptions.Limits.MaximumFieldCount = 1;
        Http1Parser countParser = new Http1Parser();
        countParser.ResetHttp1Parser(countOptions);
        Http1ParseResult countResult = countParser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nA: 1\r\nA: 2\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(sectionResult.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(sectionResult.ErrorCode,
                Is.EqualTo("http1-field-section-limit-exceeded"));
            Assert.That(countResult.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(countResult.ErrorCode,
                Is.EqualTo("http1-field-count-limit-exceeded"));
        });
    }

    [Test]
    public void FoldedLinesCountTowardSectionAndValueButNotFieldCount()
    {
        Http1ParserOptions exact = new Http1ParserOptions();
        exact.AllowObsoleteLineFolding = true;
        exact.Limits.MaximumFieldSectionSize = 7;
        exact.Limits.MaximumFieldCount = 1;
        exact.Limits.MaximumFieldValueSize = 4;
        Http1Parser exactParser = new Http1Parser();
        exactParser.ResetHttp1Parser(exact);
        Http1ParseResult accepted = exactParser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nA: ab\r\n c\r\n\r\n"u8.ToArray());

        Http1ParserOptions shortValue = new Http1ParserOptions();
        shortValue.AllowObsoleteLineFolding = true;
        shortValue.Limits.MaximumFieldValueSize = 3;
        Http1Parser valueParser = new Http1Parser();
        valueParser.ResetHttp1Parser(shortValue);
        Http1ParseResult valueRejected = valueParser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nA: ab\r\n c\r\n\r\n"u8.ToArray());

        Http1ParserOptions shortSection = new Http1ParserOptions();
        shortSection.AllowObsoleteLineFolding = true;
        shortSection.Limits.MaximumFieldSectionSize = 6;
        Http1Parser sectionParser = new Http1Parser();
        sectionParser.ResetHttp1Parser(shortSection);
        Http1ParseResult sectionRejected = sectionParser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nA: ab\r\n c\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valueRejected.ErrorCode,
                Is.EqualTo("http1-field-value-limit-exceeded"));
            Assert.That(sectionRejected.ErrorCode,
                Is.EqualTo("http1-field-section-limit-exceeded"));
        });
    }

    [Test]
    public void FragmentedFieldLineFailsAsSoonAsItExceedsTheLimit()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.Limits.MaximumFieldLineSize = 5;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult first = parser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nA: 12"u8.ToArray());
        Http1ParseResult rejected = parser.ParseHttp1Bytes("3more"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(rejected.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.Consumed, Is.EqualTo(1));
            Assert.That(rejected.ErrorPosition, Is.Zero);
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http1-field-line-limit-exceeded"));
        });
    }

    [TestCase("start")]
    [TestCase("field-line")]
    [TestCase("field-section")]
    [TestCase("field-count")]
    [TestCase("field-value")]
    [TestCase("trailer-section")]
    [TestCase("trailer-count")]
    [TestCase("content")]
    [TestCase("chunk")]
    [TestCase("buffer")]
    public void NegativeHttp1LimitsAreRejectedDuringReset(string selected)
    {
        Http1ParserOptions options = new Http1ParserOptions();
        SetLimit(options.Limits, selected, -1);
        Http1Parser parser = new Http1Parser();

        HttpProtocolException error = Assert.Throws<HttpProtocolException>(
            () => parser.ResetHttp1Parser(options))!;

        Assert.That(error.DiagnosticCode, Is.EqualTo("invalid-http1-parser-options"));
    }

    static void SetLimit(HttpLimits limits, string selected, int value)
    {
        switch (selected)
        {
            case "start":
                limits.MaximumStartLineSize = value;
                break;
            case "field-line":
                limits.MaximumFieldLineSize = value;
                break;
            case "field-section":
                limits.MaximumFieldSectionSize = value;
                break;
            case "field-count":
                limits.MaximumFieldCount = value;
                break;
            case "field-value":
                limits.MaximumFieldValueSize = value;
                break;
            case "trailer-section":
                limits.MaximumTrailerSectionSize = value;
                break;
            case "trailer-count":
                limits.MaximumTrailerCount = value;
                break;
            case "content":
                limits.MaximumContentSize = value;
                break;
            case "chunk":
                limits.MaximumChunkSize = value;
                break;
            default:
                limits.MaximumBufferedSize = value;
                break;
        }
    }
}
