using System.Text;

namespace http.Tests;

public class Http1FieldSectionTests
{
    [Test]
    public void FieldsPreserveOrderDuplicatesCasingAndOpaqueValues()
    {
        byte[] prefix = Encoding.ASCII.GetBytes(
            "GET / HTTP/1.1\r\nX-Test: one\r\nx-test:\ttwo\t \r\nX-Binary: ");
        byte[] opaque = [0x80, 0xff];
        byte[] suffix = "\r\nEmpty:\t \r\n\r\n"u8.ToArray();
        byte[] message = Join(prefix, opaque, suffix);

        Http1ParseResult result = new Http1Parser().ParseHttp1Bytes(message);
        HttpHeaders headers = result.Events[0].Request!.Headers;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(message.Length));
            Assert.That(headers.Fields.Select(field => field.Name), Is.EqualTo(new[]
            {
                "X-Test",
                "x-test",
                "X-Binary",
                "Empty"
            }));
            Assert.That(headers.Fields[0].Value, Is.EqualTo("one"u8.ToArray()));
            Assert.That(headers.Fields[1].Value, Is.EqualTo("two"u8.ToArray()));
            Assert.That(headers.Fields[2].Value, Is.EqualTo(opaque));
            Assert.That(headers.Fields[2].Value, Is.Not.SameAs(opaque));
            Assert.That(headers.Fields[3].Value, Is.Empty);
        });
    }

    [Test]
    public void FragmentedFieldsEmitHeadOnlyAtTheEmptyLineBoundary()
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult first = parser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nHost: ex"u8.ToArray());
        Http1ParseResult second = parser.ParseHttp1Bytes(
            "ample.test\r\nX-Test: value\r\n"u8.ToArray());
        Http1ParseResult third = parser.ParseHttp1Bytes("\r"u8.ToArray());
        Http1ParseResult completed = parser.ParseHttp1Bytes("\nNEXT"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(first.Events, Is.Empty);
            Assert.That(second.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(second.Events, Is.Empty);
            Assert.That(third.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(third.Events, Is.Empty);
            Assert.That(completed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(completed.Consumed, Is.EqualTo(1));
            Assert.That(completed.Events.Select(item => item.EventKind), Is.EqualTo(new[]
            {
                Http1ParseEventKind.MessageHead,
                Http1ParseEventKind.Complete
            }));
            Assert.That(completed.Events[0].Request!.Headers.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { HttpHeaderNames.Host, "X-Test" }));
        });
    }

    [TestCase("Host : example.test")]
    [TestCase("Host\t: example.test")]
    public void WhitespaceBeforeColonIsRejectedByDefault(string field)
    {
        Http1ParseResult result = ParseRequest(
            "GET / HTTP/1.1\r\n" + field + "\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("http1-whitespace-before-colon"));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [Test]
    public void WhitespaceBeforeColonCompatibilityIsExplicitAndNormalized()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowWhitespaceBeforeColon = true;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nHost \t : example.test\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events[0].Request!.Headers.Fields, Has.Count.EqualTo(1));
            Assert.That(result.Events[0].Request!.Headers.Fields[0].Name,
                Is.EqualTo(HttpHeaderNames.Host));
            Assert.That(result.Events[0].Request!.Headers.Fields[0].Value,
                Is.EqualTo("example.test"u8.ToArray()));
        });
    }

    [Test]
    public void ObsoleteFoldingIsRejectedByDefault()
    {
        Http1ParseResult result = ParseRequest(
            "GET / HTTP/1.1\r\nX-Test: first\r\n second\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("obsolete-http1-line-folding"));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [Test]
    public void ExplicitObsoleteFoldingModeReplacesEachFoldWithSpace()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowObsoleteLineFolding = true;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(
            "GET / HTTP/1.1\r\n" +
            "X-Test: first \t\r\n" +
            " \tsecond \t\r\n" +
            "\tthird\r\n" +
            " \r\n" +
            "X-Test: separate\r\n" +
            "\r\n"));

        HttpHeaders headers = result.Events[0].Request!.Headers;
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(headers.Fields, Has.Count.EqualTo(2));
            Assert.That(Encoding.ASCII.GetString(headers.Fields[0].Value),
                Is.EqualTo("first second third"));
            Assert.That(Encoding.ASCII.GetString(headers.Fields[1].Value),
                Is.EqualTo("separate"));
        });
    }

    [Test]
    public void FoldWithoutPrecedingFieldIsRejectedEvenWhenEnabled()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowObsoleteLineFolding = true;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\n continuation\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("http1-folding-without-field"));
            Assert.That(result.Events, Is.Empty);
        });
    }

    [TestCase(": value")]
    [TestCase("Missing-Colon")]
    [TestCase("Bad(Name: value")]
    [TestCase("Bad Name: value")]
    public void MalformedFieldNamesAreRejected(string field)
    {
        Http1ParseResult result = ParseRequest(
            "GET / HTTP/1.1\r\n" + field + "\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-http1-field-name"));
            Assert.That(result.Events, Is.Empty);
        });
    }

    [TestCase(0x00)]
    [TestCase(0x0b)]
    [TestCase(0x0c)]
    [TestCase(0x0d)]
    [TestCase(0x1f)]
    [TestCase(0x7f)]
    public void FieldValuesRejectProhibitedControlOctets(int invalid)
    {
        byte[] prefix = "GET / HTTP/1.1\r\nX-Test: before"u8.ToArray();
        byte[] suffix = "after\r\n\r\n"u8.ToArray();
        Http1ParseResult result = new Http1Parser().ParseHttp1Bytes(
            Join(prefix, new[] { (byte)invalid }, suffix));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-http1-field-value"));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [Test]
    public void FieldLineCrLfEnforcementHasExplicitBareLfCompatibility()
    {
        Http1ParseResult strict = new Http1Parser().ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nX-Test: value\n\n"u8.ToArray());

        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowBareLineFeed = true;
        Http1Parser compatibleParser = new Http1Parser();
        compatibleParser.ResetHttp1Parser(options);
        Http1ParseResult compatible = compatibleParser.ParseHttp1Bytes(
            "GET / HTTP/1.1\r\nX-Test: value\n\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(strict.ErrorCode, Is.EqualTo("invalid-http1-line-ending"));
            Assert.That(compatible.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(compatible.Events[0].Request!.Headers.Fields[0].Value,
                Is.EqualTo("value"u8.ToArray()));
        });
    }

    [Test]
    public void ResponseHeadReceivesTheSameDuplicatePreservingFieldBehavior()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.MessageKind = HttpMessageKind.Response;
        options.ResponseRequestMethod = HttpMethods.Head;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(
            "HTTP/1.1 200 OK\r\nSet-Cookie: one\r\nSet-Cookie: two\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Events[0].MessageKind, Is.EqualTo(HttpMessageKind.Response));
            Assert.That(result.Events[0].Response!.Headers.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "Set-Cookie", "Set-Cookie" }));
            Assert.That(result.Events[0].Response!.Headers.Fields.Select(
                field => Encoding.ASCII.GetString(field.Value)),
                Is.EqualTo(new[] { "one", "two" }));
        });
    }

    static Http1ParseResult ParseRequest(string message)
    {
        return new Http1Parser().ParseHttp1Bytes(Encoding.ASCII.GetBytes(message));
    }

    static byte[] Join(byte[] first, byte[] second, byte[] third)
    {
        byte[] result = new byte[first.Length + second.Length + third.Length];
        Array.Copy(first, 0, result, 0, first.Length);
        Array.Copy(second, 0, result, first.Length, second.Length);
        Array.Copy(third, 0, result, first.Length + second.Length, third.Length);
        return result;
    }
}
