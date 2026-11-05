namespace sip.Tests;

public class SipCoreHeaderComprehensiveTests
{
    [TestCase("Content-Length: 0007", "Content-Length: 7\r\n")]
    [TestCase("Content-Type: Text/Plain;Charset=US-ASCII", "Content-Type: text/plain;charset=US-ASCII\r\n")]
    [TestCase("Content-Disposition: SIGNAL;Handling=REQUIRED", "Content-Disposition: signal;handling=REQUIRED\r\n")]
    [TestCase("Accept: TEXT/PLAIN;Q=0.500, APPLICATION/*", "Accept: text/plain;q=0.5, application/*\r\n")]
    [TestCase("Allow: INVITE,ACK", "Allow: INVITE, ACK\r\n")]
    [TestCase("Supported: timer,100rel", "Supported: timer, 100rel\r\n")]
    [TestCase("Require: timer,100rel", "Require: timer, 100rel\r\n")]
    [TestCase("Proxy-Require: foo,bar", "Proxy-Require: foo, bar\r\n")]
    [TestCase("Unsupported: path,outbound", "Unsupported: path, outbound\r\n")]
    [TestCase("Date: Sat, 13 Nov 2010 23:29:00 GMT", "Date: Sat, 13 Nov 2010 23:29:00 GMT\r\n")]
    [TestCase("Expires: 003600", "Expires: 3600\r\n")]
    [TestCase("Min-Expires: 000090", "Min-Expires: 90\r\n")]
    [TestCase("Retry-After: 00120;Duration=00060", "Retry-After: 120;duration=60\r\n")]
    [TestCase("Warning: 399 example.com \"miscellaneous\"", "Warning: 399 example.com \"miscellaneous\"\r\n")]
    [TestCase("Server: Product/1.0 Module", "Server: Product/1.0 Module\r\n")]
    [TestCase("User-Agent: Product/1.0 Module", "User-Agent: Product/1.0 Module\r\n")]
    public void CanonicallyFormatsEveryCoreHeader(string source, string expected)
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(source);

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete), parsed.ErrorMessage);
            Assert.That(formatted.Success, Is.True, formatted.ErrorMessage);
            Assert.That(Text(formatted.Bytes), Is.EqualTo(expected));
        });
    }

    [TestCase("L: 0007", false, "Content-Length: 7\r\n")]
    [TestCase("CONTENT-LENGTH: 0007", true, "l: 7\r\n")]
    [TestCase("C: Text/Plain", false, "Content-Type: text/plain\r\n")]
    [TestCase("CONTENT-TYPE: Text/Plain", true, "c: text/plain\r\n")]
    [TestCase("K: Timer", false, "Supported: Timer\r\n")]
    [TestCase("SUPPORTED: Timer", true, "k: Timer\r\n")]
    public void AppliesEveryCoreCompactNameRule(string source, bool compact, string expected)
    {
        SipHeaderParseResult parsed = Parse(source);
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = compact;

        SipWriteResult formatted = new SipHeaderCodec().FormatHeader(parsed.Header!, options);

        Assert.That(Text(formatted.Bytes), Is.EqualTo(expected));
    }

    [TestCase("session", SipBodyDisposition.Session)]
    [TestCase("render", SipBodyDisposition.Render)]
    [TestCase("icon", SipBodyDisposition.Icon)]
    [TestCase("alert", SipBodyDisposition.Alert)]
    [TestCase("signal", SipBodyDisposition.Signal)]
    [TestCase("by-reference", SipBodyDisposition.ByReference)]
    [TestCase("recording-session", SipBodyDisposition.RecordingSession)]
    [TestCase("x-extension", SipBodyDisposition.Unknown)]
    public void MapsEveryContentDispositionType(string token, SipBodyDisposition expected)
    {
        SipHeaderParseResult result = Parse("Content-Disposition: " + token);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.ContentDisposition!.Disposition, Is.EqualTo(expected));
            Assert.That(result.ContentDisposition.DispositionToken, Is.EqualTo(token));
        });
    }

    [Test]
    public void PreservesDuplicateListEntriesInEveryListModel()
    {
        SipHeaderParseResult accept = Parse("Accept: text/plain, text/plain");
        SipHeaderParseResult allow = Parse("Allow: INVITE, INVITE");
        SipHeaderParseResult supported = Parse("Supported: timer, timer");
        SipHeaderParseResult require = Parse("Require: timer, timer");
        SipHeaderParseResult proxyRequire = Parse("Proxy-Require: foo, foo");
        SipHeaderParseResult unsupported = Parse("Unsupported: path, path");
        SipHeaderParseResult warning = Parse("Warning: 399 host \"one\", 399 host \"one\"");

        Assert.Multiple(() =>
        {
            Assert.That(accept.MediaTypes, Has.Length.EqualTo(2));
            Assert.That(allow.Methods, Is.EqualTo(new[] { "INVITE", "INVITE" }));
            Assert.That(supported.OptionTags, Is.EqualTo(new[] { "timer", "timer" }));
            Assert.That(require.OptionTags, Has.Length.EqualTo(2));
            Assert.That(proxyRequire.OptionTags, Has.Length.EqualTo(2));
            Assert.That(unsupported.OptionTags, Has.Length.EqualTo(2));
            Assert.That(warning.WarningValues, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void PreservesUnknownParametersAndTheirQuotedValues()
    {
        SipHeaderParseResult contentType = Parse("Content-Type: text/plain;X-Token=one;X-Quoted=\"two words\"");
        SipHeaderParseResult disposition = Parse(
            "Content-Disposition: session;X-Token=one;X-Quoted=\"two words\"");
        SipHeaderParseResult retry = Parse("Retry-After: 5;X-Token=one;X-Quoted=\"two words\"");

        Assert.Multiple(() =>
        {
            AssertParameters(contentType.MediaType!.Parameters);
            AssertParameters(disposition.ContentDisposition!.Parameters);
            AssertParameters(retry.RetryAfter!.Parameters);
            Assert.That(Format(contentType), Is.EqualTo(
                "Content-Type: text/plain;x-token=one;x-quoted=\"two words\"\r\n"));
            Assert.That(Format(disposition), Is.EqualTo(
                "Content-Disposition: session;x-token=one;x-quoted=\"two words\"\r\n"));
            Assert.That(Format(retry), Is.EqualTo(
                "Retry-After: 5;x-token=one;x-quoted=\"two words\"\r\n"));
        });
    }

    [Test]
    public void DecodesAndCanonicallyEscapesQuotedStringsAndComments()
    {
        SipHeaderParseResult contentType = Parse("Content-Type: text/plain;x=\"a\\\"b\\\\c\"");
        SipHeaderParseResult disposition = Parse("Content-Disposition: session;x=\"a\\\"b\\\\c\"");
        SipHeaderParseResult retry = Parse("Retry-After: 9 (a\\(b\\)\\\\c);x=\"q\\\"r\\\\s\"");
        SipHeaderParseResult warning = Parse("Warning: 399 agent \"q\\\"r\\\\s\"");
        SipHeaderParseResult server = Parse("Server: Product/1 (a\\(b\\)\\\\c)");

        Assert.Multiple(() =>
        {
            Assert.That(contentType.MediaType!.Parameters[0].Value, Is.EqualTo("a\"b\\c"));
            Assert.That(disposition.ContentDisposition!.Parameters[0].Value, Is.EqualTo("a\"b\\c"));
            Assert.That(retry.RetryAfter!.Comment, Is.EqualTo("a(b)\\c"));
            Assert.That(retry.RetryAfter.Parameters[0].Value, Is.EqualTo("q\"r\\s"));
            Assert.That(warning.WarningValues[0].Text, Is.EqualTo("q\"r\\s"));
            Assert.That(server.ProductValues[1].Comment, Is.EqualTo("a(b)\\c"));
            Assert.That(Format(contentType), Is.EqualTo("Content-Type: text/plain;x=\"a\\\"b\\\\c\"\r\n"));
            Assert.That(Format(disposition), Is.EqualTo(
                "Content-Disposition: session;x=\"a\\\"b\\\\c\"\r\n"));
            Assert.That(Format(retry), Is.EqualTo(
                "Retry-After: 9 (a\\(b\\)\\\\c);x=\"q\\\"r\\\\s\"\r\n"));
            Assert.That(Format(warning), Is.EqualTo("Warning: 399 agent \"q\\\"r\\\\s\"\r\n"));
            Assert.That(Format(server), Is.EqualTo("Server: Product/1 (a\\(b\\)\\\\c)\r\n"));
        });
    }

    [TestCase("Content-Type: text/plain;foo=one;FOO=two")]
    [TestCase("Accept: text/plain;foo=one;FOO=two")]
    [TestCase("Content-Disposition: session;foo=one;FOO=two")]
    [TestCase("Retry-After: 5;foo=one;FOO=two")]
    [TestCase("Content-Disposition: session;handling=\"optional\"")]
    public void RejectsDuplicateParametersAndQuotedHandling(string source)
    {
        SipHeaderParseResult result = Parse(source);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error), source);
    }

    [TestCase("Content-Length: 1 2")]
    [TestCase("Content-Type: text/plain;bad@name=value")]
    [TestCase("Accept: text/plain, ,application/sdp")]
    [TestCase("Allow: INVITE ACK")]
    [TestCase("Supported: timer/")]
    [TestCase("Require: ")]
    [TestCase("Proxy-Require: ")]
    [TestCase("Unsupported: ")]
    [TestCase("Date: Sat, 13 Nov 2010 24:00:00 GMT")]
    [TestCase("Expires: 0x10")]
    [TestCase("Min-Expires: +1")]
    [TestCase("Retry-After: 5 (bad\\)")]
    [TestCase("Warning: 399 host \"bad\\\"")]
    [TestCase("Server: Product (bad\\)")]
    [TestCase("User-Agent: Product(comment)")]
    public void RejectsMalformedValueForEveryCoreHeaderFamily(string source)
    {
        SipHeaderParseResult result = Parse(source);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error), source);
    }

    static void AssertParameters(SipUriParameter[] parameters)
    {
        Assert.That(parameters.Select(value => value.Name), Is.EqualTo(new[] { "X-Token", "X-Quoted" }));
        Assert.That(parameters.Select(value => value.Value), Is.EqualTo(new[] { "one", "two words" }));
        Assert.That(parameters.Select(value => value.Quoted), Is.EqualTo(new[] { false, true }));
    }

    static string Format(SipHeaderParseResult parsed)
    {
        return Text(new SipHeaderCodec().FormatHeader(parsed.Header!, new SipWriteOptions()).Bytes);
    }

    static SipHeaderParseResult Parse(string text)
    {
        return new SipHeaderCodec().ParseHeader(System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
