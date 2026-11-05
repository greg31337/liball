namespace sip.Tests;

public class SipHeaderPrimitiveTests
{
    [TestCase("INVITE", true)]
    [TestCase("x-custom_1", true)]
    [TestCase("", false)]
    [TestCase("has space", false)]
    [TestCase("bad:semicolon", false)]
    public void TokenValidationRecognizesSipTokens(string text, bool expected)
    {
        Assert.That(SipHeaderPrimitives.IsTokenText(text), Is.EqualTo(expected));
    }

    [Test]
    public void QuotedStringsDecodeAndEncodeQuotedPairs()
    {
        SipHeaderPrimitiveResult parsed = SipHeaderPrimitives.ParseQuotedString(" \t\"A \\\"quote\\\" and \\\\\" \t");

        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Text, Is.EqualTo("A \"quote\" and \\"));
        Assert.That(SipHeaderPrimitives.FormatQuotedString(parsed.Text),
            Is.EqualTo("\"A \\\"quote\\\" and \\\\\"") );
    }

    [TestCase("plain", 0)]
    [TestCase("\"unterminated", 13)]
    [TestCase("\"bad\ntext\"", 4)]
    [TestCase("\"done\"tail", 6)]
    public void InvalidQuotedStringsReportFailure(string text, int expectedOffset)
    {
        SipHeaderPrimitiveResult result = SipHeaderPrimitives.ParseQuotedString(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorOffset, Is.EqualTo(expectedOffset));
    }

    [Test]
    public void CommentsSupportNestingAndEscapedParentheses()
    {
        SipHeaderPrimitiveResult parsed = SipHeaderPrimitives.ParseCommentText(" \t(outer (inner) \\(literal\\)) \t");

        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Text, Is.EqualTo("outer (inner) (literal)"));
        Assert.That(SipHeaderPrimitives.ParseCommentText(
            SipHeaderPrimitives.FormatCommentText(parsed.Text)).Text, Is.EqualTo(parsed.Text));
        Assert.That(SipHeaderPrimitives.ParseCommentText("(missing").Success, Is.False);
        Assert.That(SipHeaderPrimitives.ParseCommentText("(done)tail").Success, Is.False);
    }

    [TestCase("example.com", "example.com", -1)]
    [TestCase("192.0.2.10:5060", "192.0.2.10", 5060)]
    [TestCase("[2001:db8::1]:5070", "2001:db8::1", 5070)]
    public void HostPortParsingHandlesNameIpv4AndBracketedIpv6(string text, string host, int port)
    {
        SipHeaderPrimitiveResult result = SipHeaderPrimitives.ParseHostPortText(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Host, Is.EqualTo(host));
        Assert.That(result.Port, Is.EqualTo(port));
        Assert.That(SipHeaderPrimitives.FormatHostPortText(result.Host, result.Port), Is.EqualTo(text));
    }

    [TestCase("[2001:db8::1", 0)]
    [TestCase("2001:db8::1", 4)]
    [TestCase("example.com:", 12)]
    [TestCase("example.com:65536", 16)]
    [TestCase("bad_host", 0)]
    public void InvalidHostPortsAreRejected(string text, int expectedOffset)
    {
        SipHeaderPrimitiveResult result = SipHeaderPrimitives.ParseHostPortText(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorOffset, Is.EqualTo(expectedOffset));
    }

    [Test]
    public void ParametersSupportValuelessTokenAndQuotedForms()
    {
        SipHeaderPrimitiveResult valueless = SipHeaderPrimitives.ParseParameterText(";lr");
        SipHeaderPrimitiveResult token = SipHeaderPrimitives.ParseParameterText("transport = TCP");
        SipHeaderPrimitiveResult quoted = SipHeaderPrimitives.ParseParameterText("note=\"a; b\\\"c\"");
        SipHeaderPrimitiveResult ipv6 = SipHeaderPrimitives.ParseParameterText("received=[2001:db8::1]");

        Assert.That(valueless.Success, Is.True);
        Assert.That(valueless.Parameter!.HasValue, Is.False);
        Assert.That(token.Parameter!.Name, Is.EqualTo("transport"));
        Assert.That(token.Parameter.Value, Is.EqualTo("TCP"));
        Assert.That(quoted.Parameter!.Value, Is.EqualTo("a; b\"c"));
        Assert.That(ipv6.Parameter!.Value, Is.EqualTo("[2001:db8::1]"));
        Assert.That(SipHeaderPrimitives.FormatParameterValue(quoted.Parameter), Is.EqualTo(";note=\"a; b\\\"c\""));
        Assert.That(SipHeaderPrimitives.FormatParameterValue(ipv6.Parameter), Is.EqualTo(";received=[2001:db8::1]"));
    }

    [TestCase("=value")]
    [TestCase("bad name=value")]
    [TestCase("name=")]
    [TestCase("name=bad/value")]
    [TestCase("name=\"unterminated")]
    public void InvalidParametersAreRejected(string text)
    {
        Assert.That(SipHeaderPrimitives.ParseParameterText(text).Success, Is.False);
    }

    [Test]
    public void CommaListsIgnoreProtectedCommasAndEnforceBounds()
    {
        string text = "\"Doe, Jane\" <sip:jane@example.com>, <sip:ops@example.com;note=a,b>, token(comment,still)";

        SipHeaderPrimitiveResult result = SipHeaderPrimitives.SplitCommaListText(text, 3);

        Assert.That(result.Success, Is.True);
        Assert.That(result.TextValues, Is.EqualTo(new string[]
        {
            "\"Doe, Jane\" <sip:jane@example.com>",
            "<sip:ops@example.com;note=a,b>",
            "token(comment,still)"
        }));
        Assert.That(SipHeaderPrimitives.JoinCommaListValues(result.TextValues), Is.EqualTo(
            "\"Doe, Jane\" <sip:jane@example.com>, <sip:ops@example.com;note=a,b>, token(comment,still)"));
        Assert.That(SipHeaderPrimitives.SplitCommaListText(text, 2).Success, Is.False);
        Assert.That(SipHeaderPrimitives.SplitCommaListText("one,,two", 3).Success, Is.False);
        Assert.That(SipHeaderPrimitives.SplitCommaListText("one,(two", 3).Success, Is.False);
    }

    [TestCase("0", 0)]
    [TestCase("0.", 0)]
    [TestCase("0.5", 500)]
    [TestCase("0.125", 125)]
    [TestCase("1", 1000)]
    [TestCase("1.000", 1000)]
    public void QualityValuesParseToThousandths(string text, int expected)
    {
        SipHeaderPrimitiveResult result = SipHeaderPrimitives.ParseQualityText(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Number, Is.EqualTo(expected));
        Assert.That(SipHeaderPrimitives.ParseQualityText(
            SipHeaderPrimitives.FormatQualityValue(result.Number)).Number, Is.EqualTo(expected));
    }

    [TestCase(".5")]
    [TestCase("00")]
    [TestCase("0.1234")]
    [TestCase("1.001")]
    [TestCase("2")]
    public void InvalidQualityValuesAreRejected(string text)
    {
        Assert.That(SipHeaderPrimitives.ParseQualityText(text).Success, Is.False);
    }

    [Test]
    public void SipDatesParseValidateWeekdayAndFormatAsGmt()
    {
        SipHeaderPrimitiveResult parsed = SipHeaderPrimitives.ParseDateText("Sat, 13 Nov 2010 23:29:00 GMT");

        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Date, Is.EqualTo(new System.DateTime(2010, 11, 13, 23, 29, 0, System.DateTimeKind.Utc)));
        Assert.That(SipHeaderPrimitives.FormatDateValue(parsed.Date), Is.EqualTo("Sat, 13 Nov 2010 23:29:00 GMT"));
        Assert.That(SipHeaderPrimitives.ParseDateText("Sun, 13 Nov 2010 23:29:00 GMT").Success, Is.False);
        Assert.That(SipHeaderPrimitives.ParseDateText("Mon, 29 Feb 2021 00:00:00 GMT").Success, Is.False);
        Assert.That(SipHeaderPrimitives.ParseDateText("Sat, 13 Nov 2010 24:00:00 GMT").Success, Is.False);
    }

    [TestCase("0", 0)]
    [TestCase("3600", 3600)]
    [TestCase("2147483647", int.MaxValue)]
    public void DeltaSecondsParseWithinInt32Bounds(string text, int expected)
    {
        SipHeaderPrimitiveResult result = SipHeaderPrimitives.ParseDeltaSecondsText(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Number, Is.EqualTo(expected));
        Assert.That(SipHeaderPrimitives.FormatDeltaSecondsValue(result.Number), Is.EqualTo(text));
    }

    [TestCase("")]
    [TestCase("-1")]
    [TestCase("2147483648")]
    [TestCase("1s")]
    public void InvalidDeltaSecondsAreRejected(string text)
    {
        Assert.That(SipHeaderPrimitives.ParseDeltaSecondsText(text).Success, Is.False);
    }

    [Test]
    public void NameAddressesParseDisplayUriAndHeaderParameters()
    {
        SipParseOptions options = new SipParseOptions();
        SipHeaderPrimitiveResult result = SipHeaderPrimitives.ParseNameAddressText(
            "\"Alice \\\"A\\\"\" <SIP:alice@Example.COM>;tag=abc;note=\"a;b\"", options);

        Assert.That(result.Success, Is.True);
        SipNameAddress address = result.NameAddress!;
        Assert.That(address.DisplayName, Is.EqualTo("Alice \"A\""));
        Assert.That(address.Uri!.User, Is.EqualTo("alice"));
        Assert.That(address.Uri.Host, Is.EqualTo("Example.COM"));
        Assert.That(address.Parameters.Select(parameter => parameter.Name), Is.EqualTo(new string[] { "tag", "note" }));
        Assert.That(address.Parameters.Select(parameter => parameter.Value), Is.EqualTo(new string[] { "abc", "a;b" }));
        Assert.That(address.OriginalText, Is.Not.Null);

        SipWriteOptions writeOptions = new SipWriteOptions();
        Assert.That(SipHeaderPrimitives.FormatNameAddressValue(address, writeOptions),
            Is.EqualTo("\"Alice \\\"A\\\"\" <sip:alice@example.com>;tag=abc;note=\"a;b\""));
    }

    [Test]
    public void NameAddressSupportsBareUriAndRejectsMalformedForms()
    {
        SipParseOptions options = new SipParseOptions();

        SipHeaderPrimitiveResult bare = SipHeaderPrimitives.ParseNameAddressText("sip:bob@example.com", options);

        Assert.That(bare.Success, Is.True);
        Assert.That(bare.NameAddress!.DisplayName, Is.Null);
        Assert.That(bare.NameAddress.Uri!.User, Is.EqualTo("bob"));
        Assert.That(SipHeaderPrimitives.ParseNameAddressText("Bob <sip:bob@example.com", options).Success, Is.False);
        Assert.That(SipHeaderPrimitives.ParseNameAddressText("Bob <sip:bob@example.com> junk", options).Success, Is.False);
        Assert.That(SipHeaderPrimitives.ParseNameAddressText("Bob<sip:bob@example.com>", options).Success, Is.False);
        Assert.That(SipHeaderPrimitives.ParseNameAddressText("Bad Display? <sip:bob@example.com>", options).Success,
            Is.False);
    }
}
