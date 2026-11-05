namespace sip.Tests;

public class SipUriComparisonTests
{
    [TestCase("sip:%61lice@atlanta.com;transport=TCP", "sip:alice@AtLanTa.CoM;Transport=tcp")]
    [TestCase("sip:carol@chicago.com", "sip:carol@chicago.com;newparam=5")]
    [TestCase("sip:carol@chicago.com", "sip:carol@chicago.com;security=on")]
    [TestCase("sip:carol@chicago.com", "sip:carol@chicago.com;lr")]
    [TestCase("sip:biloxi.com;transport=tcp;method=REGISTER?to=sip:bob%40biloxi.com", "sip:biloxi.com;method=register;transport=TCP?To=sip:bob%40biloxi.com")]
    [TestCase("sip:alice@atlanta.com?subject=project%20x&priority=urgent", "sip:alice@atlanta.com?Priority=URGENT&Subject=Project%20X")]
    public void Rfc3261EquivalentSipUrisCompareEqual(string leftText, string rightText)
    {
        SipUriCodec codec = new SipUriCodec();

        Assert.That(codec.CompareUris(Parse(codec, leftText), Parse(codec, rightText)), Is.True);
        Assert.That(codec.CompareUris(Parse(codec, rightText), Parse(codec, leftText)), Is.True);
    }

    [TestCase("SIP:ALICE@AtLanTa.CoM;Transport=udp", "sip:alice@AtLanTa.CoM;Transport=UDP")]
    [TestCase("sip:alice@atlanta.com", "sips:alice@atlanta.com")]
    [TestCase("sip:bob@biloxi.com", "sip:bob@biloxi.com:5060")]
    [TestCase("sips:bob@biloxi.com", "sips:bob@biloxi.com:5061")]
    [TestCase("sip:bob@biloxi.com", "sip:bob@biloxi.com;transport=udp")]
    [TestCase("sip:bob@biloxi.com", "sip:bob@biloxi.com;user=ip")]
    [TestCase("sip:bob@biloxi.com", "sip:bob@biloxi.com;ttl=1")]
    [TestCase("sip:bob@biloxi.com", "sip:bob@biloxi.com;method=INVITE")]
    [TestCase("sip:bob@biloxi.com", "sip:bob@biloxi.com;maddr=224.2.0.1")]
    [TestCase("sip:carol@chicago.com", "sip:carol@chicago.com?Subject=next%20meeting")]
    [TestCase("sip:bob@phone21.boxesbybob.com", "sip:bob@192.0.2.4")]
    [TestCase("sip:bob@example.com", "sip:bob:@example.com")]
    [TestCase("sip:example.com", "sip:bob@example.com")]
    [TestCase("sip:bob@example.com;security=on", "sip:bob@example.com;security=off")]
    [TestCase("sip:bob@example.com;lr=on", "sip:bob@example.com;lr=off")]
    [TestCase("sip:a%2Fb@example.com", "sip:a/b@example.com")]
    [TestCase("sip:a@example.com;x=a%2Fb", "sip:a@example.com;x=a/b")]
    [TestCase("sip:a@example.com?subject=a%2Fb", "sip:a@example.com?subject=a/b")]
    [TestCase("sip:+358-555-1234567;postd=PP22@example.com;user=phone", "sip:+3585551234567;postd=pp22@example.com;user=phone")]
    public void Rfc3261NonEquivalentSipUrisCompareUnequal(string leftText, string rightText)
    {
        SipUriCodec codec = new SipUriCodec();

        Assert.That(codec.CompareUris(Parse(codec, leftText), Parse(codec, rightText)), Is.False);
        Assert.That(codec.CompareUris(Parse(codec, rightText), Parse(codec, leftText)), Is.False);
    }

    [Test]
    public void ReservedEscapesRemainDistinctWithoutPublicOriginalText()
    {
        SipUriCodec codec = new SipUriCodec();
        SipParseOptions options = new SipParseOptions();
        options.PreserveOriginal = false;
        SipUri escapedLower = codec.ParseUri(
            System.Text.Encoding.ASCII.GetBytes("sip:a%2fb@example.com"), options).Uri!;
        SipUri escapedUpper = codec.ParseUri(
            System.Text.Encoding.ASCII.GetBytes("sip:a%2Fb@example.com"), options).Uri!;
        SipUri literal = codec.ParseUri(
            System.Text.Encoding.ASCII.GetBytes("sip:a/b@example.com"), options).Uri!;

        SipUri normalized = codec.NormalizeUri(escapedLower);
        SipWriteResult formatted = codec.FormatUri(normalized, new SipWriteOptions());

        Assert.That(codec.CompareUris(escapedLower, escapedUpper), Is.True);
        Assert.That(codec.CompareUris(escapedLower, literal), Is.False);
        Assert.That(normalized.User, Is.EqualTo("a/b"));
        Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes), Is.EqualTo("sip:a%2Fb@example.com"));

        escapedLower.User = "x/y";
        literal.User = "x/y";
        Assert.That(codec.CompareUris(escapedLower, literal), Is.True);
    }

    [Test]
    public void ManuallyConstructedDecodedUserMatchesItsEscapedWireForm()
    {
        SipUriCodec codec = new SipUriCodec();
        SipUri unicode = new SipUri();
        unicode.Scheme = SipUriScheme.Sip;
        unicode.SchemeToken = "sip";
        unicode.User = "Jö";
        unicode.Host = "example.com";
        SipUri percent = new SipUri();
        percent.Scheme = SipUriScheme.Sip;
        percent.SchemeToken = "sip";
        percent.User = "%2F";
        percent.Host = "example.com";

        Assert.That(codec.CompareUris(unicode, Parse(codec, "sip:J%C3%B6@example.com")), Is.True);
        Assert.That(codec.CompareUris(percent, Parse(codec, "sip:%252F@example.com")), Is.True);
    }

    [Test]
    public void UnknownOneSidedParametersPreserveRfcNonTransitiveComparison()
    {
        SipUriCodec codec = new SipUriCodec();
        SipUri withoutSecurity = Parse(codec, "sip:carol@chicago.com");
        SipUri securityOn = Parse(codec, "sip:carol@chicago.com;security=on");
        SipUri securityOff = Parse(codec, "sip:carol@chicago.com;security=off");

        Assert.That(codec.CompareUris(withoutSecurity, securityOn), Is.True);
        Assert.That(codec.CompareUris(withoutSecurity, securityOff), Is.True);
        Assert.That(codec.CompareUris(securityOn, securityOff), Is.False);
    }

    [Test]
    public void SipNormalizationCanonicalizesComparableComponentsWithoutAddingDefaults()
    {
        SipUriCodec codec = new SipUriCodec();
        SipUri source = Parse(codec,
            "SIP:%61lice@EXAMPLE.com;X=VaL;Transport=TCP;Method=REGISTER?Subject=Project%20X&Priority=URGENT");

        SipUri normalized = codec.NormalizeUri(source);
        SipWriteResult formatted = codec.FormatUri(normalized, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalized.SchemeToken, Is.EqualTo("sip"));
            Assert.That(normalized.User, Is.EqualTo("alice"));
            Assert.That(normalized.Host, Is.EqualTo("example.com"));
            Assert.That(normalized.Port, Is.EqualTo(-1));
            Assert.That(normalized.Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[] { "method", "transport", "x" }));
            Assert.That(normalized.Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new string[] { "register", "tcp", "val" }));
            Assert.That(normalized.Headers.Select(header => header.Name),
                Is.EqualTo(new string[] { "priority", "subject" }));
            Assert.That(normalized.Headers.Select(header => header.Value),
                Is.EqualTo(new string[] { "urgent", "project x" }));
            Assert.That(normalized.OriginalText, Is.Null);
            Assert.That(System.Text.Encoding.ASCII.GetString(formatted.Bytes),
                Is.EqualTo("sip:alice@example.com;method=register;transport=tcp;x=val?priority=urgent&subject=project%20x"));
        });
    }

    [TestCase("tel:+1-201-555-0123;ext=4-5-6", "TEL:+12015550123;EXT=456")]
    [TestCase("tel:7042;phone-context=example.com", "tel:7(0)4.2;PHONE-CONTEXT=EXAMPLE.COM")]
    [TestCase("tel:863-1234;phone-context=+1-914-555", "tel:8631234;phone-context=+1914555")]
    [TestCase("tel:1234;phone-context=example.com;foo=BAR", "tel:1-2-3-4;FOO=bar;PHONE-CONTEXT=Example.Com")]
    public void EquivalentTelephoneUrisIgnoreCaseOrderAndVisualSeparators(string leftText, string rightText)
    {
        SipUriCodec codec = new SipUriCodec();

        Assert.That(codec.CompareUris(Parse(codec, leftText), Parse(codec, rightText)), Is.True);
    }

    [TestCase("tel:+12015550123", "tel:12015550123;phone-context=+1")]
    [TestCase("tel:+12015550123", "tel:+12015550124")]
    [TestCase("tel:1234;phone-context=example.com", "tel:1234;phone-context=other.example.com")]
    [TestCase("tel:+12015550123;ext=1", "tel:+12015550123")]
    [TestCase("tel:+12015550123;foo=one", "tel:+12015550123;foo=two")]
    public void NonEquivalentTelephoneUrisRemainDistinct(string leftText, string rightText)
    {
        SipUriCodec codec = new SipUriCodec();

        Assert.That(codec.CompareUris(Parse(codec, leftText), Parse(codec, rightText)), Is.False);
    }

    [Test]
    public void TelephoneNormalizationRemovesVisualSeparatorsAndSortsParameters()
    {
        SipUriCodec codec = new SipUriCodec();
        SipUri source = Parse(codec, "TEL:1-(2)3.4;A=VALUE;PHONE-CONTEXT=EXAMPLE.COM;EXT=4-5");

        SipUri normalized = codec.NormalizeUri(source);

        Assert.That(normalized.User, Is.EqualTo("1234"));
        Assert.That(normalized.Parameters.Select(parameter => parameter.Name),
            Is.EqualTo(new string[] { "ext", "phone-context", "a" }));
        Assert.That(normalized.Parameters.Select(parameter => parameter.Value),
            Is.EqualTo(new string[] { "45", "example.com", "value" }));
    }

    [Test]
    public void AbsoluteUriNormalizationHandlesOnlyUnreservedPercentEscapes()
    {
        SipUriCodec codec = new SipUriCodec();
        SipUri escapedUnreserved = Parse(codec, "URN:example:%61%7e");
        SipUri literalUnreserved = Parse(codec, "urn:example:a~");
        SipUri escapedReserved = Parse(codec, "urn:example:%2f");
        SipUri literalReserved = Parse(codec, "urn:example:/");

        SipUri normalized = codec.NormalizeUri(escapedReserved);

        Assert.That(codec.CompareUris(escapedUnreserved, literalUnreserved), Is.True);
        Assert.That(codec.CompareUris(escapedReserved, literalReserved), Is.False);
        Assert.That(normalized.SchemeToken, Is.EqualTo("urn"));
        Assert.That(normalized.OpaquePart, Is.EqualTo("example:%2F"));
    }

    [Test]
    public void InvalidModelsDoNotCompareEquivalent()
    {
        SipUriCodec codec = new SipUriCodec();
        SipUri invalid = new SipUri();

        Assert.That(codec.CompareUris(invalid, invalid), Is.False);
    }

    static SipUri Parse(SipUriCodec codec, string text)
    {
        SipParseResult result = codec.ParseUri(System.Text.Encoding.ASCII.GetBytes(text), new SipParseOptions());
        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete), text);
        return result.Uri!;
    }
}
