using System.Text;

namespace http.Tests;

public class Http1RequestLineTests
{
    [Test]
    public void RequestTargetFormsPopulateTheirDistinctMetadata()
    {
        HttpRequest origin = RequestFrom(
            "GET /items/one?q=now HTTP/1.1\r\n\r\n");
        HttpRequest absolute = RequestFrom(
            "GET https://example.test:8443/a/b?x=1 HTTP/1.1\r\n\r\n");
        HttpRequest authority = RequestFrom(
            "CONNECT [2001:db8::1]:443 HTTP/1.1\r\n\r\n");
        HttpRequest asterisk = RequestFrom(
            "OPTIONS * HTTP/1.1\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(origin.TargetForm, Is.EqualTo(HttpRequestTargetForm.Origin));
            Assert.That(origin.Path, Is.EqualTo("/items/one"));
            Assert.That(origin.HasQuery, Is.True);
            Assert.That(origin.Query, Is.EqualTo("q=now"));

            Assert.That(absolute.TargetForm, Is.EqualTo(HttpRequestTargetForm.Absolute));
            Assert.That(absolute.Scheme, Is.EqualTo("https"));
            Assert.That(absolute.Authority, Is.EqualTo("example.test:8443"));
            Assert.That(absolute.Path, Is.EqualTo("/a/b"));
            Assert.That(absolute.HasQuery, Is.True);
            Assert.That(absolute.Query, Is.EqualTo("x=1"));

            Assert.That(authority.TargetForm, Is.EqualTo(HttpRequestTargetForm.Authority));
            Assert.That(authority.Authority, Is.EqualTo("[2001:db8::1]:443"));
            Assert.That(authority.Path, Is.Null);

            Assert.That(asterisk.TargetForm, Is.EqualTo(HttpRequestTargetForm.Asterisk));
            Assert.That(asterisk.Path, Is.Null);
        });
    }

    [Test]
    public void ValidExtensionMethodAndBothHttp1VersionsArePreserved()
    {
        HttpRequest extension = RequestFrom(
            "CUSTOM-METHOD /resource HTTP/1.0\r\n\r\n");
        HttpRequest standard = RequestFrom(
            "GET /resource HTTP/1.1\r\n\r\n");

        Assert.Multiple(() =>
        {
            Assert.That(extension.Method, Is.EqualTo("CUSTOM-METHOD"));
            Assert.That(extension.MethodMetadata, Is.Not.Null);
            Assert.That(extension.MethodMetadata!.IsRegistered, Is.False);
            Assert.That(extension.ProtocolVersion, Is.EqualTo(HttpProtocolVersion.Http10));
            Assert.That(standard.MethodMetadata!.IsRegistered, Is.True);
            Assert.That(standard.ProtocolVersion, Is.EqualTo(HttpProtocolVersion.Http11));
        });
    }

    [TestCase(" GET / HTTP/1.1")]
    [TestCase("GET  / HTTP/1.1")]
    [TestCase("GET /  HTTP/1.1")]
    [TestCase("GET / HTTP/1.1 ")]
    [TestCase("GET\t/ HTTP/1.1")]
    [TestCase("GET /\tHTTP/1.1")]
    [TestCase("GET / HTTP/1.1\t")]
    [TestCase("GET / HTTP/1.1 extra")]
    public void RequestLineRequiresExactlyTwoSingleSpaceSeparators(string line)
    {
        Http1ParseResult result = ParseLine(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.Consumed, Is.EqualTo(Encoding.ASCII.GetByteCount(line) + 2));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.HasErrorPosition, Is.True);
        });
    }

    [Test]
    public void MethodMustBeANonEmptyCaseSensitiveToken()
    {
        Http1ParseResult invalidToken = ParseLine("G(ET / HTTP/1.1");
        Http1ParseResult lowercase = ParseLine("get / HTTP/1.1");
        byte[] nonAsciiLine =
        [
            0x47, 0x80, 0x54, 0x20, 0x2f, 0x20,
            0x48, 0x54, 0x54, 0x50, 0x2f, 0x31, 0x2e, 0x31,
            0x0d, 0x0a, 0x0d, 0x0a
        ];
        Http1Parser parser = new Http1Parser();
        Http1ParseResult nonAscii = parser.ParseHttp1Bytes(nonAsciiLine);

        Assert.Multiple(() =>
        {
            Assert.That(invalidToken.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(invalidToken.ErrorCode, Is.EqualTo("invalid-http1-method"));
            Assert.That(lowercase.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(lowercase.Events[0].Request!.Method, Is.EqualTo("get"));
            Assert.That(lowercase.Events[0].Request!.MethodMetadata!.IsRegistered, Is.False);
            Assert.That(nonAscii.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(nonAscii.ErrorCode, Is.EqualTo("invalid-http1-request-line-octet"));
        });
    }

    [TestCase("GET * HTTP/1.1")]
    [TestCase("CONNECT /path HTTP/1.1")]
    [TestCase("CONNECT host.example: HTTP/1.1")]
    [TestCase("CONNECT https://example.test/ HTTP/1.1")]
    [TestCase("GET relative-path HTTP/1.1")]
    [TestCase("GET /path#fragment HTTP/1.1")]
    [TestCase("GET /bad%XX HTTP/1.1")]
    [TestCase("GET http:/missing-authority HTTP/1.1")]
    [TestCase("GET http://user@example.test/ HTTP/1.1")]
    [TestCase("GET http://example.test:70000/ HTTP/1.1")]
    public void InvalidOrMethodIncompatibleTargetsAreRejected(string line)
    {
        Http1ParseResult result = ParseLine(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Does.Contain("target"));
            Assert.That(result.IsTerminal, Is.True);
            Assert.That(result.Events, Is.Empty);
        });
    }

    [TestCase("GET / HTTP/1.2")]
    [TestCase("GET / HTTP/2.0")]
    [TestCase("GET / http/1.1")]
    [TestCase("GET / HTTP/1")]
    [TestCase("GET / HTTP/1.10")]
    public void RequestVersionMustBeExactHttp10OrHttp11(string line)
    {
        Http1ParseResult result = ParseLine(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-http1-version"));
            Assert.That(result.IsTerminal, Is.True);
        });
    }

    [Test]
    public void OneLeadingEmptyRequestLineIsControlledByExplicitPolicy()
    {
        Http1Parser allowedParser = new Http1Parser();
        byte[] allowedInput = "\r\nGET / HTTP/1.1\r\n\r\n"u8.ToArray();
        Http1ParseResult allowed = allowedParser.ParseHttp1Bytes(allowedInput);

        Http1ParserOptions disabledOptions = new Http1ParserOptions();
        disabledOptions.AllowLeadingEmptyRequestLine = false;
        Http1Parser disabledParser = new Http1Parser();
        disabledParser.ResetHttp1Parser(disabledOptions);
        Http1ParseResult disabled = disabledParser.ParseHttp1Bytes(allowedInput);

        Http1Parser repeatedParser = new Http1Parser();
        Http1ParseResult repeated = repeatedParser.ParseHttp1Bytes(
            "\r\n\r\nGET / HTTP/1.1\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(allowed.Consumed, Is.EqualTo(allowedInput.Length));
            Assert.That(disabled.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(disabled.Consumed, Is.EqualTo(2));
            Assert.That(repeated.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(repeated.Consumed, Is.EqualTo(4));
        });
    }

    [Test]
    public void StartLineLimitIsAppliedAfterFragmentReassembly()
    {
        string line = "GET / HTTP/1.1";
        Http1ParserOptions exactOptions = new Http1ParserOptions();
        exactOptions.Limits.MaximumStartLineSize = Encoding.ASCII.GetByteCount(line);
        Http1Parser exactParser = new Http1Parser();
        exactParser.ResetHttp1Parser(exactOptions);
        Http1ParseResult exact = exactParser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(line + "\r\n\r\n"));

        Http1ParserOptions shortOptions = new Http1ParserOptions();
        shortOptions.Limits.MaximumStartLineSize = Encoding.ASCII.GetByteCount(line) - 1;
        Http1Parser shortParser = new Http1Parser();
        shortParser.ResetHttp1Parser(shortOptions);
        Http1ParseResult first = shortParser.ParseHttp1Bytes("GET / HT"u8.ToArray());
        Http1ParseResult exceeded = shortParser.ParseHttp1Bytes("TP/1.1\r\nrest"u8.ToArray());

        Http1Parser unterminatedParser = new Http1Parser();
        unterminatedParser.ResetHttp1Parser(shortOptions);
        Http1ParseResult unterminated = unterminatedParser.ParseHttp1Bytes(
            "GET / HTTP/1.1Xmore"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(first.Status, Is.EqualTo(HttpOperationStatus.NeedInput));
            Assert.That(exceeded.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(exceeded.Consumed, Is.EqualTo(
                shortOptions.Limits.MaximumStartLineSize - 8 + 1));
            Assert.That(exceeded.ErrorCode, Is.EqualTo("http1-start-line-limit-exceeded"));
            Assert.That(exceeded.IsTerminal, Is.True);
            Assert.That(unterminated.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(unterminated.Consumed,
                Is.EqualTo(shortOptions.Limits.MaximumStartLineSize + 1));
            Assert.That(unterminated.ErrorCode,
                Is.EqualTo("http1-start-line-limit-exceeded"));
            Assert.Throws<HttpProtocolException>(() =>
            {
                Http1ParserOptions invalid = new Http1ParserOptions();
                invalid.Limits.MaximumStartLineSize = -1;
                new Http1Parser().ResetHttp1Parser(invalid);
            });
        });
    }

    [Test]
    public void RawControlOctetsCannotInjectASecondRequestLine()
    {
        Http1ParseResult carriageReturn = ParseLine("GET / HTTP/1.1\rX-Injected: yes");
        byte[] nullLine =
        [
            0x47, 0x45, 0x54, 0x20, 0x2f, 0x00, 0x20,
            0x48, 0x54, 0x54, 0x50, 0x2f, 0x31, 0x2e, 0x31,
            0x0d, 0x0a
        ];
        Http1ParseResult nullOctet = new Http1Parser().ParseHttp1Bytes(nullLine);
        Http1ParseResult bareLineFeed = new Http1Parser().ParseHttp1Bytes(
            "GET / HTTP/1.1\nX-Injected: yes\r\n\r\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(carriageReturn.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(carriageReturn.ErrorCode,
                Is.EqualTo("invalid-http1-request-line-control"));
            Assert.That(nullOctet.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(nullOctet.ErrorCode,
                Is.EqualTo("invalid-http1-request-line-control"));
            Assert.That(bareLineFeed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(bareLineFeed.ErrorCode,
                Is.EqualTo("invalid-http1-line-ending"));
        });
    }

    [Test]
    public void BareLineFeedCompatibilityIsOptIn()
    {
        Http1ParserOptions options = new Http1ParserOptions();
        options.AllowBareLineFeed = true;
        Http1Parser parser = new Http1Parser();
        parser.ResetHttp1Parser(options);

        Http1ParseResult result = parser.ParseHttp1Bytes(
            "GET / HTTP/1.1\n\n"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(16));
            Assert.That(result.Events[0].Request!.TargetForm,
                Is.EqualTo(HttpRequestTargetForm.Origin));
        });
    }

    static HttpRequest RequestFrom(string message)
    {
        Http1Parser parser = new Http1Parser();
        Http1ParseResult result = parser.ParseHttp1Bytes(
            Encoding.ASCII.GetBytes(message));
        Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
        return result.Events[0].Request!;
    }

    static Http1ParseResult ParseLine(string line)
    {
        Http1Parser parser = new Http1Parser();
        return parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(line + "\r\n\r\n"));
    }
}
