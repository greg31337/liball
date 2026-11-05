namespace sip.Tests;

public class SipUriCodecTests
{
    [Test]
    public void ParsesSipUriWithEscapedUserInfoIpv6ParametersAndHeaders()
    {
        SipUriCodec codec = new SipUriCodec();
        string text = "SIP:alice%20smith:p%40ss@[2001:db8::10]:5070;transport=tcp;lr?Subject=Project%20X&Priority=urgent";

        SipParseResult result = codec.ParseUri(System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Consumed, Is.EqualTo(text.Length));
        Assert.That(result.RequiredBytes, Is.EqualTo(text.Length));
        Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        Assert.That(result.ValidationIssues, Is.Empty);
        SipUri uri = result.Uri!;
        Assert.Multiple(() =>
        {
            Assert.That(uri.Scheme, Is.EqualTo(SipUriScheme.Sip));
            Assert.That(uri.SchemeToken, Is.EqualTo("SIP"));
            Assert.That(uri.User, Is.EqualTo("alice smith"));
            Assert.That(uri.Password, Is.EqualTo("p@ss"));
            Assert.That(uri.Host, Is.EqualTo("2001:db8::10"));
            Assert.That(uri.Port, Is.EqualTo(5070));
            Assert.That(uri.Parameters, Has.Length.EqualTo(2));
            Assert.That(uri.Parameters[0].Name, Is.EqualTo("transport"));
            Assert.That(uri.Parameters[0].Value, Is.EqualTo("tcp"));
            Assert.That(uri.Parameters[0].HasValue, Is.True);
            Assert.That(uri.Parameters[0].OriginalText, Is.EqualTo("transport=tcp"));
            Assert.That(uri.Parameters[1].Name, Is.EqualTo("lr"));
            Assert.That(uri.Parameters[1].HasValue, Is.False);
            Assert.That(uri.Headers, Has.Length.EqualTo(2));
            Assert.That(uri.Headers[0].Name, Is.EqualTo("Subject"));
            Assert.That(uri.Headers[0].Value, Is.EqualTo("Project X"));
            Assert.That(uri.Headers[1].Name, Is.EqualTo("Priority"));
            Assert.That(uri.Headers[1].Value, Is.EqualTo("urgent"));
            Assert.That(uri.OriginalText, Is.EqualTo(text));
        });
    }

    [TestCase("sip:example.com", SipUriScheme.Sip, "example.com")]
    [TestCase("sips:alice@192.0.2.4:5061", SipUriScheme.Sips, "192.0.2.4")]
    [TestCase("sip:bob@[::ffff:192.0.2.1]", SipUriScheme.Sip, "::ffff:192.0.2.1")]
    [TestCase("sip:alice;day=tuesday@example.com", SipUriScheme.Sip, "example.com")]
    public void ParsesSipAndSipsHostForms(string text, SipUriScheme expectedScheme, string expectedHost)
    {
        SipUriCodec codec = new SipUriCodec();

        SipParseResult result = codec.ParseUri(System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Uri!.Scheme, Is.EqualTo(expectedScheme));
        Assert.That(result.Uri.Host, Is.EqualTo(expectedHost));
    }

    [Test]
    public void ParsesGlobalAndLocalTelephoneUris()
    {
        SipUriCodec codec = new SipUriCodec();

        SipParseResult global = ParseText(codec, "tel:+1-201-555-0123;ext=456");
        SipParseResult local = ParseText(codec, "TEL:7042;phone-context=example.com");

        Assert.Multiple(() =>
        {
            Assert.That(global.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(global.Uri!.Scheme, Is.EqualTo(SipUriScheme.Tel));
            Assert.That(global.Uri.User, Is.EqualTo("+1-201-555-0123"));
            Assert.That(global.Uri.Parameters[0].Name, Is.EqualTo("ext"));
            Assert.That(global.Uri.Parameters[0].Value, Is.EqualTo("456"));
            Assert.That(local.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(local.Uri!.SchemeToken, Is.EqualTo("TEL"));
            Assert.That(local.Uri.User, Is.EqualTo("7042"));
            Assert.That(local.Uri.Parameters[0].Name, Is.EqualTo("phone-context"));
        });
    }

    [Test]
    public void ParsesUnknownAbsoluteUriWithoutDiscardingOpaqueData()
    {
        SipUriCodec codec = new SipUriCodec();

        SipParseResult result = ParseText(codec, "urn:example:animal:ferret:nose");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(result.Uri!.Scheme, Is.EqualTo(SipUriScheme.Absolute));
            Assert.That(result.Uri.SchemeToken, Is.EqualTo("urn"));
            Assert.That(result.Uri.OpaquePart, Is.EqualTo("example:animal:ferret:nose"));
            Assert.That(result.Uri.OriginalText, Is.EqualTo("urn:example:animal:ferret:nose"));
        });

        SipParseResult hierarchical = ParseText(codec, "http://[2001:db8::1]/resource?x=y");
        Assert.That(hierarchical.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(hierarchical.Uri!.OpaquePart, Is.EqualTo("//[2001:db8::1]/resource?x=y"));
    }

    [Test]
    public void FormatsCanonicalUriWithEscapedUtf8AndDelimiters()
    {
        SipUri uri = new SipUri();
        uri.Scheme = SipUriScheme.Sip;
        uri.SchemeToken = "SIP";
        uri.User = "Jö rg";
        uri.Password = "p@ss";
        uri.Host = "Example.COM";
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = "X";
        parameter.Value = "a;b";
        parameter.HasValue = true;
        uri.Parameters = new SipUriParameter[] { parameter };
        SipUriHeader header = new SipUriHeader();
        header.Name = "Subject";
        header.Value = "project x&y";
        uri.Headers = new SipUriHeader[] { header };
        SipUriCodec codec = new SipUriCodec();

        SipWriteResult result = codec.FormatUri(uri, new SipWriteOptions());
        SipParseResult reparsed = codec.ParseUri(result.Bytes, new SipParseOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(result.Bytes),
            Is.EqualTo("sip:J%C3%B6%20rg:p%40ss@example.com;x=a%3Bb?Subject=project%20x%26y"));
        Assert.That(result.Written, Is.EqualTo(result.Bytes.Length));
        Assert.That(result.RequiredBytes, Is.EqualTo(result.Bytes.Length));
        Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(reparsed.Uri!.User, Is.EqualTo("Jö rg"));
        Assert.That(reparsed.Uri.Password, Is.EqualTo("p@ss"));
        Assert.That(reparsed.Uri.Parameters[0].Value, Is.EqualTo("a;b"));
        Assert.That(reparsed.Uri.Headers[0].Value, Is.EqualTo("project x&y"));
    }

    [Test]
    public void PreservationModeReusesOriginalUriText()
    {
        SipUriCodec codec = new SipUriCodec();
        SipParseResult parsed = ParseText(codec, "SIP:Alice@Example.COM");
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = true;
        options.Canonical = false;

        SipWriteResult result = codec.FormatUri(parsed.Uri!, options);

        Assert.That(result.Success, Is.True);
        Assert.That(System.Text.Encoding.ASCII.GetString(result.Bytes), Is.EqualTo("SIP:Alice@Example.COM"));
    }

    [TestCase("sip", 0)]
    [TestCase("sip:", 4)]
    [TestCase("sip:alice@", 10)]
    [TestCase("sip:example.com:abc", 16)]
    [TestCase("sip:[2001:db8::1", 4)]
    [TestCase("sip:2001:db8::1", 8)]
    [TestCase("sip:999.1.1.1", 4)]
    [TestCase("sip:bad_host.example", 4)]
    [TestCase("sip:%GG@example.com", 4)]
    [TestCase("sip:%C0%AF@example.com", 4)]
    [TestCase("sip:example.com;", 16)]
    [TestCase("sip:example.com?Subject", 16)]
    [TestCase("tel:7042", 4)]
    [TestCase("tel:+", 4)]
    [TestCase("tel:%2B1201", 4)]
    [TestCase("tel:+1201;bad_name=x", 10)]
    [TestCase("tel:+1201;%65xt=1", 10)]
    [TestCase("tel:+1201;ext=abc", 10)]
    [TestCase("tel:7042;phone-context=bad_context", 9)]
    [TestCase("urn:bad%escape", 7)]
    [TestCase("urn:bad|value", 7)]
    public void MalformedUrisReturnBoundedErrors(string text, int expectedOffset)
    {
        SipUriCodec codec = new SipUriCodec();

        SipParseResult result = ParseText(codec, text);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.Uri, Is.Null);
        Assert.That(result.ErrorOffset, Is.EqualTo(expectedOffset));
        Assert.That(result.ErrorMessage, Is.Not.Empty);
        Assert.That(result.ValidationIssues, Has.Length.EqualTo(1));
        Assert.That(result.ValidationIssues[0].Severity, Is.EqualTo(SipValidationSeverity.Error));
        Assert.That(result.ValidationIssues[0].ByteOffset, Is.EqualTo(expectedOffset));
    }

    [Test]
    public void RawWhitespaceControlAndNonAsciiBytesAreRejectedAtTheirByteOffsets()
    {
        SipUriCodec codec = new SipUriCodec();

        SipParseResult whitespace = ParseText(codec, "sip:alice @example.com");
        SipParseResult control = codec.ParseUri(new byte[] { 115, 105, 112, 58, 0 }, new SipParseOptions());
        SipParseResult nonAscii = codec.ParseUri(new byte[] { 115, 105, 112, 58, 195, 182 }, new SipParseOptions());

        Assert.That(whitespace.ErrorOffset, Is.EqualTo(9));
        Assert.That(control.ErrorOffset, Is.EqualTo(4));
        Assert.That(nonAscii.ErrorOffset, Is.EqualTo(4));
    }

    [Test]
    public void ParseLimitsRejectOversizeAndExcessCollections()
    {
        SipUriCodec codec = new SipUriCodec();
        string basicText = "sip:example.com";
        SipParseOptions exact = new SipParseOptions();
        exact.MaxUriBytes = basicText.Length;
        SipParseOptions shortLimit = new SipParseOptions();
        shortLimit.MaxUriBytes = basicText.Length - 1;
        SipParseOptions parameterLimit = new SipParseOptions();
        parameterLimit.MaxParameterCount = 1;
        SipParseOptions headerLimit = new SipParseOptions();
        headerLimit.MaxListValueCount = 1;

        SipParseResult exactResult = codec.ParseUri(System.Text.Encoding.ASCII.GetBytes(basicText), exact);
        SipParseResult shortResult = codec.ParseUri(System.Text.Encoding.ASCII.GetBytes(basicText), shortLimit);
        SipParseResult parameterResult = codec.ParseUri(System.Text.Encoding.ASCII.GetBytes("sip:example.com;one=1;two=2"), parameterLimit);
        SipParseResult headerResult = codec.ParseUri(System.Text.Encoding.ASCII.GetBytes("sip:example.com?a=1&b=2"), headerLimit);

        Assert.That(exactResult.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(shortResult.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(shortResult.ErrorOffset, Is.EqualTo(shortLimit.MaxUriBytes));
        Assert.That(parameterResult.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(parameterResult.ErrorMessage, Does.Contain("parameter count"));
        Assert.That(headerResult.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(headerResult.ErrorMessage, Does.Contain("header count"));
    }

    [Test]
    public void ValidationAndOutputLimitFailuresReturnResults()
    {
        SipUriCodec codec = new SipUriCodec();
        SipUri invalid = new SipUri();
        invalid.Scheme = SipUriScheme.Sip;
        invalid.User = "";
        SipValidationIssue[] issues = codec.ValidateUri(invalid);
        SipWriteResult invalidWrite = codec.FormatUri(invalid, new SipWriteOptions());

        SipParseResult parsed = ParseText(codec, "sip:example.com");
        SipWriteOptions limited = new SipWriteOptions();
        limited.MaxHeaderLineBytes = 5;
        SipWriteResult limitedWrite = codec.FormatUri(parsed.Uri!, limited);

        Assert.That(issues, Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "uri-host"));
        Assert.That(invalidWrite.Success, Is.False);
        Assert.That(invalidWrite.Bytes, Is.Empty);
        Assert.That(limitedWrite.Success, Is.False);
        Assert.That(limitedWrite.RequiredBytes, Is.EqualTo("sip:example.com".Length));
    }

    [Test]
    public void BaselineNormalizationCopiesModelsAndComparisonUsesCanonicalCase()
    {
        SipUriCodec codec = new SipUriCodec();
        SipUri left = ParseText(codec, "SIP:Alice@Example.COM;Transport=TCP").Uri!;
        SipUri right = ParseText(codec, "sip:Alice@example.com;transport=TCP").Uri!;
        SipUri differentUser = ParseText(codec, "sip:alice@example.com;transport=TCP").Uri!;

        SipUri normalized = codec.NormalizeUri(left);
        left.Host = "changed.example";
        left.Parameters[0].Name = "changed";

        Assert.That(normalized, Is.Not.SameAs(left));
        Assert.That(normalized.Host, Is.EqualTo("example.com"));
        Assert.That(normalized.Parameters, Is.Not.SameAs(left.Parameters));
        Assert.That(normalized.Parameters[0].Name, Is.EqualTo("transport"));
        Assert.That(codec.CompareUris(normalized, right), Is.True);
        Assert.That(codec.CompareUris(normalized, differentUser), Is.False);
    }

    static SipParseResult ParseText(SipUriCodec codec, string text)
    {
        return codec.ParseUri(System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
    }
}
