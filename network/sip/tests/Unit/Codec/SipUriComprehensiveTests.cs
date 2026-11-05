namespace sip.Tests;

public class SipUriComprehensiveTests
{
    [TestCase("sip:example.com.", "example.com.", -1)]
    [TestCase("sip:192.0.2.1:0", "192.0.2.1", 0)]
    [TestCase("sips:alice@255.255.255.255:65535", "255.255.255.255", 65535)]
    [TestCase("sip:[::]", "::", -1)]
    [TestCase("sip:[::1]", "::1", -1)]
    [TestCase("sip:[2001:db8:0:1::1]", "2001:db8:0:1::1", -1)]
    [TestCase("sip:[2001:db8:0:1:2:3:4:5]", "2001:db8:0:1:2:3:4:5", -1)]
    [TestCase("sip:[::192.0.2.1]", "::192.0.2.1", -1)]
    public void ParsesEveryHostAndPortForm(string text, string expectedHost, int expectedPort)
    {
        SipParseResult result = Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.Uri!.Host, Is.EqualTo(expectedHost));
            Assert.That(result.Uri.Port, Is.EqualTo(expectedPort));
        });
    }

    [Test]
    public void ParsesAllSipUserPasswordParameterAndHeaderCharacterBranches()
    {
        string text = "sip:&=+$,;?/:&=+$,@example.com"
            + ";x[]/:&+$=v[]/:&+$;flag"
            + "?H[]/?:+$=&Next=%E2%82%AC";

        SipParseResult result = Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.Uri!.User, Is.EqualTo("&=+$,;?/"));
            Assert.That(result.Uri.Password, Is.EqualTo("&=+$,"));
            Assert.That(result.Uri.Parameters.Select(value => value.Name),
                Is.EqualTo(new[] { "x[]/:&+$", "flag" }));
            Assert.That(result.Uri.Parameters[0].Value, Is.EqualTo("v[]/:&+$"));
            Assert.That(result.Uri.Parameters[1].HasValue, Is.False);
            Assert.That(result.Uri.Headers[0].Name, Is.EqualTo("H[]/?:+$"));
            Assert.That(result.Uri.Headers[0].Value, Is.Empty);
            Assert.That(result.Uri.Headers[1].Value, Is.EqualTo("€"));
        });
    }

    [Test]
    public void ParsesEmptyPasswordAndRegisteredSipParameters()
    {
        string text = "sip:alice:@example.com;transport=ws;user=phone;method=FOO"
            + ";ttl=255;maddr=[ff02::1];lr";

        SipParseResult result = Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.Uri!.Password, Is.Empty);
            Assert.That(result.Uri.Parameters.Select(value => value.Name),
                Is.EqualTo(new[] { "transport", "user", "method", "ttl", "maddr", "lr" }));
            Assert.That(result.Uri.Parameters.Last().HasValue, Is.False);
        });
    }

    [TestCase("tel:+1(201)-555.0123;ext=(45);foo;bar=a%2Fb", "+1(201)-555.0123")]
    [TestCase("tel:AB#*;isub=a%40b;phone-context=Example.COM.", "AB#*")]
    [TestCase("tel:12;phone-context=+1(212)-555", "12")]
    public void ParsesGlobalAndLocalTelGrammarBranches(string text, string subscriber)
    {
        SipParseResult result = Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.Uri!.Scheme, Is.EqualTo(SipUriScheme.Tel));
            Assert.That(result.Uri.User, Is.EqualTo(subscriber));
        });
    }

    [TestCase("x+1.foo-bar:/path;value?query=yes", "x+1.foo-bar", "/path;value?query=yes")]
    [TestCase("urn:a-z_A-Z.0~!*'();/?:@&=+$,[]%2F", "urn", "a-z_A-Z.0~!*'();/?:@&=+$,[]%2F")]
    public void ParsesUnknownAbsoluteSchemeAndCharacterBranches(string text, string scheme,
        string opaquePart)
    {
        SipParseResult result = Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), result.ErrorMessage);
            Assert.That(result.Uri!.Scheme, Is.EqualTo(SipUriScheme.Absolute));
            Assert.That(result.Uri.SchemeToken, Is.EqualTo(scheme));
            Assert.That(result.Uri.OpaquePart, Is.EqualTo(opaquePart));
        });
    }

    [Test]
    public void CanonicalFormattingEscapesEachComponentByItsOwnGrammar()
    {
        SipUri uri = new SipUri();
        uri.Scheme = SipUriScheme.Sip;
        uri.SchemeToken = "sip";
        uri.User = ":@[]";
        uri.Password = ":;?/@[]";
        uri.Host = "Example.COM";
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = "n;=?@,";
        parameter.Value = "v;=?@,";
        parameter.HasValue = true;
        uri.Parameters = new[] { parameter };
        SipUriHeader header = new SipUriHeader();
        header.Name = "H&=,@;";
        header.Value = "V&=,@;";
        uri.Headers = new[] { header };

        SipWriteResult result = new SipUriCodec().FormatUri(uri, new SipWriteOptions());

        Assert.That(Text(result.Bytes), Is.EqualTo(
            "sip:%3A%40%5B%5D:%3A%3B%3F%2F%40%5B%5D@example.com"
            + ";n%3B%3D%3F%40%2C=v%3B%3D%3F%40%2C"
            + "?H%26%3D%2C%40%3B=V%26%3D%2C%40%3B"));
    }

    [Test]
    public void NonCanonicalFormattingUsesStoredCaseWithoutOriginalText()
    {
        SipUri uri = Parse("SIP:Alice@Example.COM;X=VaL").Uri!;
        uri.OriginalText = null;
        SipWriteOptions options = new SipWriteOptions();
        options.Canonical = false;
        options.PreserveOriginal = false;

        SipWriteResult result = new SipUriCodec().FormatUri(uri, options);

        Assert.That(Text(result.Bytes), Is.EqualTo("SIP:Alice@Example.COM;X=VaL"));
    }

    [TestCase("sip:alice:Secret@example.com", "sip:alice:secret@example.com", false)]
    [TestCase("sip:alice@example.com;x=ONE", "sip:alice@example.com;X=one", true)]
    [TestCase("sip:alice@example.com?a=1&B=2", "sip:alice@example.com?b=2&A=1", true)]
    [TestCase("sip:alice@[2001:DB8::1]", "sip:alice@[2001:db8::1]", true)]
    [TestCase("urn:example:VALUE", "URN:example:value", false)]
    public void CoversRemainingComparisonRules(string left, string right, bool equivalent)
    {
        SipUriCodec codec = new SipUriCodec();

        Assert.That(codec.CompareUris(Parse(left).Uri!, Parse(right).Uri!), Is.EqualTo(equivalent));
    }

    [Test]
    public void NormalizationSortsDuplicateNamedHeadersAndPreservesExplicitDefaults()
    {
        SipUri source = Parse("SIPS:Alice@[2001:DB8::1]:5061"
            + ";Z=two;A=one?Subject=z&Subject=A").Uri!;

        SipUri normalized = new SipUriCodec().NormalizeUri(source);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.SchemeToken, Is.EqualTo("sips"));
            Assert.That(normalized.Host, Is.EqualTo("2001:db8::1"));
            Assert.That(normalized.Port, Is.EqualTo(5061));
            Assert.That(normalized.Parameters.Select(value => value.Name),
                Is.EqualTo(new[] { "a", "z" }));
            Assert.That(normalized.Headers.Select(value => value.Value),
                Is.EqualTo(new[] { "a", "z" }));
            Assert.That(normalized.OriginalText, Is.Null);
        });
    }

    [TestCase("1sip:example.com")]
    [TestCase("sip:@example.com")]
    [TestCase("sip:alice:bad;password@example.com")]
    [TestCase("sip:-bad.example.com")]
    [TestCase("sip:bad-.example.com")]
    [TestCase("sip:example.123")]
    [TestCase("sip:256.0.0.1")]
    [TestCase("sip:example.com:65536")]
    [TestCase("sip:[::1]extra")]
    [TestCase("sip:[1:2:3:4:5:6:7:8:9]")]
    [TestCase("sip:[2001::db8::1]")]
    [TestCase("sip:example.com;=value")]
    [TestCase("sip:example.com;x=")]
    [TestCase("sip:example.com?=value")]
    [TestCase("sip:example.com?a=1&")]
    [TestCase("sip:example.com?a=%FF")]
    [TestCase("tel:+ABC")]
    [TestCase("tel:+1201;phone-context=example.com")]
    [TestCase("tel:+1201;ext=1;isub=2")]
    [TestCase("tel:1234;phone-context=example.com;foo=1;FOO=2")]
    [TestCase("tel:1234;phone-context=example.com?")]
    [TestCase("urn:")]
    public void RejectsRemainingMalformedGrammarAndTelSemanticCases(string text)
    {
        SipParseResult result = Parse(text);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error), text);
    }

    [TestCase("sip:example.com;transport")]
    [TestCase("sip:example.com;transport=a%2Fb")]
    [TestCase("sip:example.com;user")]
    [TestCase("sip:example.com;method=%20")]
    [TestCase("sip:example.com;ttl=256")]
    [TestCase("sip:example.com;ttl=-1")]
    [TestCase("sip:example.com;maddr=bad_host")]
    [TestCase("sip:example.com;x=1;X=2")]
    [TestCase("sips:example.com;transport=udp")]
    public void RejectsInvalidRegisteredAndDuplicateSipParameters(string text)
    {
        SipParseResult result = Parse(text);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error), text);
    }

    [Test]
    public void CoversEveryConfiguredUriLimitBoundary()
    {
        SipUriCodec codec = new SipUriCodec();
        SipParseOptions negativeSize = new SipParseOptions();
        negativeSize.MaxUriBytes = -1;
        SipParseOptions negativeParameters = new SipParseOptions();
        negativeParameters.MaxParameterCount = -1;
        SipParseOptions negativeHeaders = new SipParseOptions();
        negativeHeaders.MaxListValueCount = -1;
        SipParseOptions zeroParameters = new SipParseOptions();
        zeroParameters.MaxParameterCount = 0;
        SipParseOptions zeroHeaders = new SipParseOptions();
        zeroHeaders.MaxListValueCount = 0;

        SipParseResult size = codec.ParseUri(Bytes("sip:example.com"), negativeSize);
        SipParseResult parameters = codec.ParseUri(Bytes("sip:example.com"), negativeParameters);
        SipParseResult headers = codec.ParseUri(Bytes("sip:example.com"), negativeHeaders);
        SipParseResult parameterValue = codec.ParseUri(Bytes("sip:example.com;x=1"), zeroParameters);
        SipParseResult headerValue = codec.ParseUri(Bytes("sip:example.com?x=1"), zeroHeaders);

        Assert.Multiple(() =>
        {
            Assert.That(size.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(parameters.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(headers.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(parameterValue.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(headerValue.Status, Is.EqualTo(SipParseStatus.Error));
        });
    }

    [Test]
    public void ValidationCoversInvalidModelsForEverySchemeFamily()
    {
        SipUri sip = new SipUri();
        sip.Scheme = SipUriScheme.Sip;
        sip.SchemeToken = "sips";
        sip.Host = "::bad::";
        sip.Port = 65536;
        sip.Password = "secret";
        sip.OpaquePart = "opaque";
        sip.Parameters = null!;
        sip.Headers = null!;
        SipUri tel = new SipUri();
        tel.Scheme = SipUriScheme.Tel;
        tel.SchemeToken = "sip";
        tel.User = "1234";
        tel.Host = "example.com";
        SipUri absolute = new SipUri();
        absolute.Scheme = SipUriScheme.Absolute;
        absolute.SchemeToken = "1bad";
        absolute.OpaquePart = "bad value";
        absolute.User = "unexpected";
        SipUri unknown = new SipUri();

        SipValidationIssue[] sipIssues = new SipUriCodec().ValidateUri(sip);
        SipValidationIssue[] telIssues = new SipUriCodec().ValidateUri(tel);
        SipValidationIssue[] absoluteIssues = new SipUriCodec().ValidateUri(absolute);
        SipValidationIssue[] unknownIssues = new SipUriCodec().ValidateUri(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(sipIssues.Select(value => value.Code), Does.Contain("uri-scheme"));
            Assert.That(sipIssues.Select(value => value.Code), Does.Contain("uri-host"));
            Assert.That(sipIssues.Select(value => value.Code), Does.Contain("uri-port"));
            Assert.That(sipIssues.Select(value => value.Code), Does.Contain("uri-userinfo"));
            Assert.That(sipIssues.Select(value => value.Code), Does.Contain("uri-components"));
            Assert.That(sipIssues.Select(value => value.Code), Does.Contain("uri-parameters"));
            Assert.That(sipIssues.Select(value => value.Code), Does.Contain("uri-headers"));
            Assert.That(telIssues.Select(value => value.Code), Does.Contain("uri-phone-context"));
            Assert.That(telIssues.Select(value => value.Code), Does.Contain("uri-tel-components"));
            Assert.That(absoluteIssues.Select(value => value.Code), Does.Contain("uri-scheme"));
            Assert.That(absoluteIssues.Select(value => value.Code), Does.Contain("uri-opaque"));
            Assert.That(absoluteIssues.Select(value => value.Code), Does.Contain("uri-components"));
            Assert.That(unknownIssues.Select(value => value.Code), Does.Contain("uri-scheme"));
        });
    }

    static SipParseResult Parse(string text)
    {
        return new SipUriCodec().ParseUri(Bytes(text), new SipParseOptions());
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.ASCII.GetBytes(text);
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.ASCII.GetString(bytes);
    }
}
