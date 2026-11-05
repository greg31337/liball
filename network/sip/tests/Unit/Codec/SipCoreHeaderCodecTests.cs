namespace sip.Tests;

public class SipCoreHeaderCodecTests
{
    [TestCase("Content-Length: 0", 0)]
    [TestCase("l: 00042", 42)]
    [TestCase("Content-Length: 2147483647", 2147483647)]
    public void ParsesAndCanonicallyFormatsContentLength(string text, int expected)
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec, text, new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, options);

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.ContentLength, Is.EqualTo(expected));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes), Is.EqualTo("l: " + expected + "\r\n"));
    }

    [TestCase("Content-Length:")]
    [TestCase("Content-Length: -1")]
    [TestCase("Content-Length: +1")]
    [TestCase("Content-Length: 12x")]
    [TestCase("Content-Length: 2147483648")]
    public void RejectsInvalidContentLength(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ContentLength, Is.EqualTo(-1));
    }

    [Test]
    public void ParsesContentTypeParametersAndCompactName()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "c: Application/SDP;Charset=\"utf-8\";boundary=part-1", new SipParseOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.MediaType, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(parsed.MediaType!.Type, Is.EqualTo("Application"));
            Assert.That(parsed.MediaType.Subtype, Is.EqualTo("SDP"));
            Assert.That(parsed.MediaType.QualityThousandths, Is.EqualTo(-1));
            Assert.That(parsed.MediaType.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[] { "Charset", "boundary" }));
            Assert.That(parsed.MediaType.Parameters[0].Value, Is.EqualTo("utf-8"));
        });
    }

    [Test]
    public void CanonicallyFormatsContentType()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Content-Type: Application/SDP;Charset=\"utf-8\";boundary=\"part one\"", new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, options);

        Assert.That(formatted.Success, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(formatted.Bytes),
            Is.EqualTo("c: application/sdp;charset=utf-8;boundary=\"part one\"\r\n"));
    }

    [TestCase("Content-Type:")]
    [TestCase("Content-Type: text")]
    [TestCase("Content-Type: text/plain/extra")]
    [TestCase("Content-Type: */*")]
    [TestCase("Content-Type: text/*")]
    [TestCase("Content-Type: text/plain;charset")]
    [TestCase("Content-Type: text/plain;charset=")]
    public void RejectsInvalidContentType(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.MediaType, Is.Null);
    }

    [Test]
    public void ParsesEmptyAndWeightedAcceptRanges()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult empty = Parse(codec, "Accept:", new SipParseOptions());
        SipHeaderParseResult ranges = Parse(codec,
            "Accept: text/plain;level=1;q=0.750, application/*;q=1, */*;q=0", new SipParseOptions());

        Assert.That(empty.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(empty.MediaTypes, Is.Empty);
        Assert.That(ranges.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(ranges.MediaTypes, Has.Length.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(ranges.MediaTypes[0].Type, Is.EqualTo("text"));
            Assert.That(ranges.MediaTypes[0].QualityThousandths, Is.EqualTo(750));
            Assert.That(ranges.MediaTypes[1].Subtype, Is.EqualTo("*"));
            Assert.That(ranges.MediaTypes[1].QualityThousandths, Is.EqualTo(1000));
            Assert.That(ranges.MediaTypes[2].Type, Is.EqualTo("*"));
            Assert.That(ranges.MediaTypes[2].QualityThousandths, Is.EqualTo(0));
        });
    }

    [Test]
    public void CanonicallyFormatsAcceptRanges()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Accept: TEXT/PLAIN;Level=1;Q=0.750, APPLICATION/*;q=1.000", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(formatted.Success, Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("Accept: text/plain;level=1;q=0.75, application/*;q=1\r\n"));
    }

    [TestCase("Accept: */plain")]
    [TestCase("Accept: text/plain;q")]
    [TestCase("Accept: text/plain;q=\"0.5\"")]
    [TestCase("Accept: text/plain;q=1.1")]
    [TestCase("Accept: text/plain;q=0.5;Q=0.6")]
    [TestCase("Accept: text/plain,")]
    public void RejectsInvalidAcceptRanges(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.MediaTypes, Is.Empty);
    }

    [Test]
    public void AcceptHonorsListAndParameterLimits()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions listOptions = new SipParseOptions();
        listOptions.MaxListValueCount = 1;
        SipParseOptions parameterOptions = new SipParseOptions();
        parameterOptions.MaxParameterCount = 1;

        SipHeaderParseResult tooManyRanges = Parse(codec,
            "Accept: text/plain, application/sdp", listOptions);
        SipHeaderParseResult tooManyParameters = Parse(codec,
            "Accept: text/plain;level=1;q=0.5", parameterOptions);

        Assert.That(tooManyRanges.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooManyRanges.ErrorMessage, Does.Contain("item count"));
        Assert.That(tooManyParameters.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(tooManyParameters.ErrorMessage, Does.Contain("parameter count"));
    }

    [Test]
    public void ParsesKnownAndExtensionContentDispositions()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult known = Parse(codec,
            "Content-Disposition: Session;Handling=required;foo=\"bar baz\"", new SipParseOptions());
        SipHeaderParseResult extension = Parse(codec,
            "Content-Disposition: x-custom;handling=optional", new SipParseOptions());

        Assert.That(known.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(known.ContentDisposition!.Disposition, Is.EqualTo(SipBodyDisposition.Session));
        Assert.That(known.ContentDisposition.DispositionToken, Is.EqualTo("Session"));
        Assert.That(known.ContentDisposition.Parameters[1].Value, Is.EqualTo("bar baz"));
        Assert.That(extension.ContentDisposition!.Disposition, Is.EqualTo(SipBodyDisposition.Unknown));
        Assert.That(extension.ContentDisposition.DispositionToken, Is.EqualTo("x-custom"));
    }

    [Test]
    public void CanonicallyFormatsContentDisposition()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Content-Disposition: Session;Handling=REQUIRED;Foo=\"bar baz\"", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(System.Text.Encoding.UTF8.GetString(formatted.Bytes), Is.EqualTo(
            "Content-Disposition: session;handling=REQUIRED;foo=\"bar baz\"\r\n"));
    }

    [TestCase("Content-Disposition:")]
    [TestCase("Content-Disposition: session;handling")]
    [TestCase("Content-Disposition: session;handling=\"required\"")]
    [TestCase("Content-Disposition: session;handling=required;Handling=optional")]
    public void RejectsInvalidContentDisposition(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ContentDisposition, Is.Null);
    }

    [Test]
    public void ParsesCapabilityListsAndTheirEmptyForms()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult allow = Parse(codec, "Allow: INVITE, X-CUSTOM", new SipParseOptions());
        SipHeaderParseResult emptyAllow = Parse(codec, "Allow:", new SipParseOptions());
        SipHeaderParseResult supported = Parse(codec, "k: timer, 100rel", new SipParseOptions());
        SipHeaderParseResult emptySupported = Parse(codec, "Supported:", new SipParseOptions());
        SipHeaderParseResult require = Parse(codec, "Require: timer", new SipParseOptions());
        SipHeaderParseResult proxyRequire = Parse(codec, "Proxy-Require: foo, bar", new SipParseOptions());
        SipHeaderParseResult unsupported = Parse(codec, "Unsupported: path", new SipParseOptions());

        Assert.That(allow.Methods, Is.EqualTo(new string[] { "INVITE", "X-CUSTOM" }));
        Assert.That(emptyAllow.Methods, Is.Empty);
        Assert.That(supported.OptionTags, Is.EqualTo(new string[] { "timer", "100rel" }));
        Assert.That(emptySupported.OptionTags, Is.Empty);
        Assert.That(require.OptionTags, Is.EqualTo(new string[] { "timer" }));
        Assert.That(proxyRequire.OptionTags, Is.EqualTo(new string[] { "foo", "bar" }));
        Assert.That(unsupported.OptionTags, Is.EqualTo(new string[] { "path" }));
    }

    [Test]
    public void CanonicallyFormatsCapabilityListsAndCompactSupported()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec, "Supported: timer,100rel", new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.CompactHeaderNames = true;

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, options);

        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("k: timer, 100rel\r\n"));
    }

    [TestCase("Require:")]
    [TestCase("Proxy-Require:")]
    [TestCase("Unsupported:")]
    [TestCase("Allow: INVITE,")]
    [TestCase("Supported: timer;foo")]
    public void RejectsInvalidCapabilityLists(string text)
    {
        Assert.That(Parse(new SipHeaderCodec(), text, new SipParseOptions()).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void CapabilityListsHonorConfiguredLimit()
    {
        SipParseOptions options = new SipParseOptions();
        options.MaxListValueCount = 1;

        SipHeaderParseResult result = Parse(new SipHeaderCodec(), "Require: one, two", options);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorMessage, Does.Contain("item count"));
    }

    [Test]
    public void ParsesAndFormatsSipDate()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Date: Sat, 13 Nov 2010 23:29:00 GMT", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.HasDate, Is.True);
        Assert.That(parsed.Date, Is.EqualTo(new System.DateTime(
            2010, 11, 13, 23, 29, 0, System.DateTimeKind.Utc)));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo("Date: Sat, 13 Nov 2010 23:29:00 GMT\r\n"));
    }

    [TestCase("Date: Sun, 13 Nov 2010 23:29:00 GMT")]
    [TestCase("Date: Sat, 31 Nov 2010 23:29:00 GMT")]
    [TestCase("Date: Sat, 13 Nov 2010 23:29:00 UTC")]
    public void RejectsInvalidSipDates(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.HasDate, Is.False);
    }

    [TestCase("Expires: 000", 0, false)]
    [TestCase("Expires: 3600", 3600, false)]
    [TestCase("Min-Expires: 060", 60, true)]
    public void ParsesAndFormatsExpiryHeaders(string text, int expected, bool minimum)
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec, text, new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(minimum ? parsed.MinExpiresSeconds : parsed.ExpiresSeconds, Is.EqualTo(expected));
        string name = minimum ? "Min-Expires" : "Expires";
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
            Is.EqualTo(name + ": " + expected + "\r\n"));
    }

    [TestCase("Expires: -1")]
    [TestCase("Expires: 1.5")]
    [TestCase("Min-Expires:")]
    [TestCase("Min-Expires: 2147483648")]
    public void RejectsInvalidExpiryHeaders(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void ParsesAndCanonicallyFormatsRetryAfter()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Retry-After: 00120 (server (still busy));Duration=00060;Foo=\"bar baz\"",
            new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.RetryAfter, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(parsed.RetryAfter!.Seconds, Is.EqualTo(120));
            Assert.That(parsed.RetryAfter.Comment, Is.EqualTo("server (still busy)"));
            Assert.That(parsed.RetryAfter.DurationSeconds, Is.EqualTo(60));
            Assert.That(parsed.RetryAfter.Parameters[1].Value, Is.EqualTo("bar baz"));
            Assert.That(System.Text.Encoding.UTF8.GetString(formatted.Bytes), Is.EqualTo(
                "Retry-After: 120 (server \\(still busy\\));duration=60;foo=\"bar baz\"\r\n"));
        });
    }

    [TestCase("Retry-After:")]
    [TestCase("Retry-After: -1")]
    [TestCase("Retry-After: 120 later")]
    [TestCase("Retry-After: 120 (busy")]
    [TestCase("Retry-After: 120;duration")]
    [TestCase("Retry-After: 120;duration=\"60\"")]
    [TestCase("Retry-After: 120;duration=1;Duration=2")]
    public void RejectsInvalidRetryAfter(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.RetryAfter, Is.Null);
    }

    [Test]
    public void ParsesAndCanonicallyFormatsWarningList()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec,
            "Warning: 307 isi.edu \"Session parameter \\\"foo\\\" not understood\", "
                + "399 [2001:db8::1]:5060 \"Miscellaneous warning\"", new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.WarningValues, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(parsed.WarningValues[0].Code, Is.EqualTo(307));
            Assert.That(parsed.WarningValues[0].Agent, Is.EqualTo("isi.edu"));
            Assert.That(parsed.WarningValues[0].Text, Is.EqualTo("Session parameter \"foo\" not understood"));
            Assert.That(parsed.WarningValues[1].Agent, Is.EqualTo("[2001:db8::1]:5060"));
            Assert.That(System.Text.Encoding.UTF8.GetString(formatted.Bytes), Is.EqualTo(
                "Warning: 307 isi.edu \"Session parameter \\\"foo\\\" not understood\", "
                    + "399 [2001:db8::1]:5060 \"Miscellaneous warning\"\r\n"));
        });
    }

    [TestCase("Warning:")]
    [TestCase("Warning: 30 host \"text\"")]
    [TestCase("Warning: 300 bad@host \"text\"")]
    [TestCase("Warning: 300 host text")]
    [TestCase("Warning: 300 host \"text\" trailing")]
    [TestCase("Warning: 300 host \"text\",")]
    public void RejectsInvalidWarnings(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.WarningValues, Is.Empty);
    }

    [Test]
    public void WarningHonorsConfiguredListLimit()
    {
        SipParseOptions options = new SipParseOptions();
        options.MaxListValueCount = 1;

        SipHeaderParseResult result = Parse(new SipHeaderCodec(),
            "Warning: 300 one \"first\", 301 two \"second\"", options);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorMessage, Does.Contain("item count"));
    }

    [TestCase("Server: Softphone/1.0 (compatible (test)) Module")]
    [TestCase("User-Agent: Client/2.3 (desktop)")]
    public void ParsesAndCanonicallyFormatsProductValues(string text)
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeaderParseResult parsed = Parse(codec, text, new SipParseOptions());

        SipWriteResult formatted = codec.FormatHeader(parsed.Header!, new SipWriteOptions());

        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(parsed.ProductValues, Is.Not.Empty);
        Assert.That(parsed.ProductValues[0].Name, Is.Not.Empty);
        Assert.That(formatted.Success, Is.True);
        string expected = text.Replace("(compatible (test))", "(compatible \\(test\\))");
        Assert.That(System.Text.Encoding.UTF8.GetString(formatted.Bytes), Is.EqualTo(expected + "\r\n"));
    }

    [Test]
    public void ProductCommentsAreDecoded()
    {
        SipHeaderParseResult parsed = Parse(new SipHeaderCodec(),
            "Server: Product/1 (compatible (nested)) Other", new SipParseOptions());

        Assert.That(parsed.ProductValues, Has.Length.EqualTo(3));
        Assert.That(parsed.ProductValues[0].Name, Is.EqualTo("Product"));
        Assert.That(parsed.ProductValues[0].Version, Is.EqualTo("1"));
        Assert.That(parsed.ProductValues[1].IsComment, Is.True);
        Assert.That(parsed.ProductValues[1].Comment, Is.EqualTo("compatible (nested)"));
        Assert.That(parsed.ProductValues[2].Name, Is.EqualTo("Other"));
    }

    [TestCase("Server:")]
    [TestCase("Server: Product/")]
    [TestCase("Server: Product/1/2")]
    [TestCase("Server: Product(comment)")]
    [TestCase("User-Agent: (unterminated")]
    public void RejectsInvalidProductValues(string text)
    {
        SipHeaderParseResult result = Parse(new SipHeaderCodec(), text, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ProductValues, Is.Empty);
    }

    [Test]
    public void CoreHeadersHonorPreservationAndOutputLimits()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions parseOptions = new SipParseOptions();
        parseOptions.PreserveOriginal = false;
        SipHeaderParseResult parsed = Parse(codec,
            "Content-Type: text/plain;Charset=utf-8", parseOptions);
        SipWriteOptions writeOptions = new SipWriteOptions();
        writeOptions.MaxHeaderLineBytes = 10;

        SipWriteResult limited = codec.FormatHeader(parsed.Header!, writeOptions);

        Assert.That(parsed.Header!.OriginalBytes, Is.Empty);
        Assert.That(parsed.MediaType!.OriginalText, Is.Null);
        Assert.That(parsed.MediaType.Parameters.Select(parameter => parameter.OriginalText), Is.All.Null);
        Assert.That(limited.Success, Is.False);
        Assert.That(limited.RequiredBytes, Is.GreaterThan(10));
    }

    static SipHeaderParseResult Parse(SipHeaderCodec codec, string text, SipParseOptions options)
    {
        return codec.ParseHeader(System.Text.Encoding.UTF8.GetBytes(text), options);
    }
}
