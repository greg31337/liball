using System.Text;

namespace pop3.Tests;

public class Pop3ResponseParserTests
{
    [TestCase("+OK", Pop3ResponseKind.Positive, "")]
    [TestCase("+OK ready for mail", Pop3ResponseKind.Positive, "ready for mail")]
    [TestCase("-ERR", Pop3ResponseKind.Negative, "")]
    [TestCase("-ERR [SYS/TEMP] mailbox busy", Pop3ResponseKind.Negative, "[SYS/TEMP] mailbox busy")]
    [TestCase("+ ", Pop3ResponseKind.SaslContinuation, "")]
    [TestCase("+ Y2hhbGxlbmdl", Pop3ResponseKind.SaslContinuation, "Y2hhbGxlbmdl")]
    public void ParserRecognizesResponseKindsAndPreservesText(string line, Pop3ResponseKind kind, string text)
    {
        Pop3Response response = Parse(line, false, 1024);

        Assert.That(response.Kind, Is.EqualTo(kind));
        Assert.That(response.Text, Is.EqualTo(text));
        Assert.That(response.GreetingTimestamp, Is.Null);
    }

    [Test]
    public void ParserPreservesArbitraryUtf8HumanText()
    {
        string text = "ready: café 世界 [] !?";

        Pop3Response response = Parse("+OK " + text, false, 1024);

        Assert.That(response.Text, Is.EqualTo(text));
    }

    [TestCase("+OK  leading and  repeated spaces ", Pop3ResponseKind.Positive, " leading and  repeated spaces ")]
    [TestCase("-ERR  leading and  repeated spaces ", Pop3ResponseKind.Negative, " leading and  repeated spaces ")]
    [TestCase("+  leading and  repeated spaces ", Pop3ResponseKind.SaslContinuation, " leading and  repeated spaces ")]
    public void ParserPreservesAllHumanTextSpacing(
        string line,
        Pop3ResponseKind kind,
        string expectedText)
    {
        Pop3Response response = Parse(line, false, 1024);

        Assert.That(response.Kind, Is.EqualTo(kind));
        Assert.That(response.Text, Is.EqualTo(expectedText));
    }

    [TestCaseSource(nameof(ResponseCodeRoundTripCases))]
    public void ParserAndFormatterRoundTripEveryRegisteredAndUnknownHierarchicalCode(
        string[] suppliedLevels,
        string expectedCode)
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        Pop3Response source = new Pop3Response();
        source.Kind = Pop3ResponseKind.Negative;
        source.Text = "human text";
        source.ResponseCodeLevels = suppliedLevels;

        byte[] formatted = parser.FormatResponse(source, 1024);
        Pop3Response parsed = parser.ParseResponse(formatted, false, 1024);

        Assert.That(
            Encoding.UTF8.GetString(formatted),
            Is.EqualTo("-ERR " + expectedCode + " human text"));
        Assert.That(
            parsed.ResponseCodeLevels,
            Is.EqualTo(expectedCode.Substring(1, expectedCode.Length - 2).Split('/')));
        Assert.That(parsed.Text, Is.EqualTo(expectedCode + " human text"));
    }

    [Test]
    public void ParserExtractsAValidGreetingTimestampWithoutRemovingText()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        byte[] line = Encoding.ASCII.GetBytes("+OK server <123.456@example.test> ready");

        Pop3Response response = parser.ParseResponse(line, true, 1024);

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(response.Text, Is.EqualTo("server <123.456@example.test> ready"));
        Assert.That(response.GreetingTimestamp, Is.EqualTo("<123.456@example.test>"));
    }

    [Test]
    public void TimestampExtractionOnlyRunsForPositiveGreetingLines()
    {
        Pop3Response ordinary = Parse("+OK value <a@b>", false, 1024);
        Pop3Response negativeGreeting = Parse("-ERR rejected <a@b>", true, 1024);

        Assert.That(ordinary.GreetingTimestamp, Is.Null);
        Assert.That(negativeGreeting.GreetingTimestamp, Is.Null);
    }

    [TestCase("+OK ready <missing-at>")]
    [TestCase("+OK ready <@domain>")]
    [TestCase("+OK ready <local@>")]
    [TestCase("+OK ready <a b@domain>")]
    [TestCase("+OK ready <a@b> and <c@d>")]
    [TestCase("+OK ready <a@b")]
    public void ParserRejectsMalformedGreetingTimestamps(string line)
    {
        Assert.Throws<InvalidDataException>(() => Parse(line, true, 1024));
    }

    [TestCase("+ok ready")]
    [TestCase("-err failed")]
    [TestCase("+OKready")]
    [TestCase("-ERROR")]
    [TestCase("+")]
    [TestCase("OK ready")]
    [TestCase("")]
    public void ParserRejectsInvalidStatusIndicators(string line)
    {
        Assert.Throws<InvalidDataException>(() => Parse(line, false, 1024));
    }

    [Test]
    public void ParserEnforcesThe512OctetBoundaryForNormalResponses()
    {
        string exactPositive = "+OK " + new string('a', 506);
        string longPositive = "+OK " + new string('a', 507);
        string exactNegative = "-ERR " + new string('b', 505);
        string longNegative = "-ERR " + new string('b', 506);

        Assert.That(Encoding.ASCII.GetByteCount(exactPositive) + 2, Is.EqualTo(512));
        Assert.That(Parse(exactPositive, false, 1024).Text, Has.Length.EqualTo(506));
        Assert.That(Parse(exactNegative, false, 1024).Text, Has.Length.EqualTo(505));
        Assert.Throws<InvalidDataException>(() => Parse(longPositive, false, 1024));
        Assert.Throws<InvalidDataException>(() => Parse(longNegative, false, 1024));
    }

    [Test]
    public void SaslContinuationUsesItsSeparateConfiguredLimit()
    {
        string challenge = new string('A', 600);
        string line = "+ " + challenge;

        Assert.That(Parse(line, false, 604).Text, Is.EqualTo(challenge));
        Assert.Throws<InvalidDataException>(() => Parse(line, false, 603));
    }

    [Test]
    public void ParserRejectsControlBytesAndMalformedUtf8()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();

        Assert.Throws<InvalidDataException>(() => parser.ParseResponse(new byte[] { 43, 79, 75, 32, 0 }, false, 1024));
        Assert.Throws<InvalidDataException>(() => parser.ParseResponse(new byte[] { 43, 79, 75, 32, 10 }, false, 1024));
        Assert.Throws<DecoderFallbackException>(() => parser.ParseResponse(new byte[] { 43, 79, 75, 32, 195 }, false, 1024));
    }

    [TestCase(Pop3ResponseKind.Positive, "ready", "+OK ready")]
    [TestCase(Pop3ResponseKind.Positive, "", "+OK")]
    [TestCase(Pop3ResponseKind.Negative, "failed", "-ERR failed")]
    [TestCase(Pop3ResponseKind.Negative, "", "-ERR")]
    [TestCase(Pop3ResponseKind.SaslContinuation, "YQ==", "+ YQ==")]
    [TestCase(Pop3ResponseKind.SaslContinuation, "", "+ ")]
    public void FormatterEmitsCanonicalStatusLines(Pop3ResponseKind kind, string text, string expected)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = kind;
        response.Text = text;

        byte[] line = new Pop3ResponseParser().FormatResponse(response, 1024);

        Assert.That(Encoding.UTF8.GetString(line), Is.EqualTo(expected));
    }

    [Test]
    public void FormatterAppendsTimestampOnceAndRoundTripsGreeting()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = "server ready";
        response.GreetingTimestamp = "<123@example.test>";

        byte[] line = parser.FormatResponse(response, 1024);
        Pop3Response parsed = parser.ParseResponse(line, true, 1024);

        Assert.That(Encoding.ASCII.GetString(line), Is.EqualTo("+OK server ready <123@example.test>"));
        Assert.That(parsed.GreetingTimestamp, Is.EqualTo("<123@example.test>"));

        response.Text = parsed.Text;
        Assert.That(Encoding.ASCII.GetString(parser.FormatResponse(response, 1024)), Is.EqualTo("+OK server ready <123@example.test>"));
    }

    [Test]
    public void FormatterRejectsAConflictingTimestampAlreadyPresentInGreetingText()
    {
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = "server <old@example.test> ready";
        response.GreetingTimestamp = "<new@example.test>";

        Assert.Throws<InvalidDataException>(() =>
            new Pop3ResponseParser().FormatResponse(response, 1024));
    }

    [Test]
    public void FormatterCountsUtf8OctetsAndEnforcesResponseLimits()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        Pop3Response exact = new Pop3Response();
        Pop3Response tooLong = new Pop3Response();
        exact.Kind = Pop3ResponseKind.Positive;
        exact.Text = new string('é', 253);
        tooLong.Kind = Pop3ResponseKind.Positive;
        tooLong.Text = new string('é', 254);

        Assert.That(parser.FormatResponse(exact, 1024), Has.Length.EqualTo(510));
        Assert.Throws<InvalidDataException>(() => parser.FormatResponse(tooLong, 1024));
    }

    [TestCase(Pop3ResponseKind.Positive, 505, 511, true)]
    [TestCase(Pop3ResponseKind.Positive, 506, 512, true)]
    [TestCase(Pop3ResponseKind.Positive, 507, 513, false)]
    [TestCase(Pop3ResponseKind.Negative, 504, 511, true)]
    [TestCase(Pop3ResponseKind.Negative, 505, 512, true)]
    [TestCase(Pop3ResponseKind.Negative, 506, 513, false)]
    public void ParserEnforcesEveryNormalResponseWireBoundary(
        Pop3ResponseKind kind,
        int textLength,
        int wireLength,
        bool accepted)
    {
        string marker = kind == Pop3ResponseKind.Positive ? "+OK " : "-ERR ";
        string line = marker + new string('a', textLength);

        Assert.That(Encoding.ASCII.GetByteCount(line) + 2, Is.EqualTo(wireLength));
        if (accepted)
        {
            Assert.That(Parse(line, false, 1024).Text,
                Has.Length.EqualTo(textLength));
        }
        else
        {
            Assert.Throws<InvalidDataException>(() => Parse(line, false, 1024));
        }
    }

    [TestCase(Pop3ResponseKind.Positive, 505, 511, true)]
    [TestCase(Pop3ResponseKind.Positive, 506, 512, true)]
    [TestCase(Pop3ResponseKind.Positive, 507, 513, false)]
    [TestCase(Pop3ResponseKind.Negative, 504, 511, true)]
    [TestCase(Pop3ResponseKind.Negative, 505, 512, true)]
    [TestCase(Pop3ResponseKind.Negative, 506, 513, false)]
    public void FormatterEnforcesEveryNormalResponseWireBoundary(
        Pop3ResponseKind kind,
        int textLength,
        int wireLength,
        bool accepted)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = kind;
        response.Text = new string('a', textLength);
        Pop3ResponseParser parser = new Pop3ResponseParser();

        if (accepted)
        {
            Assert.That(parser.FormatResponse(response, 1024),
                Has.Length.EqualTo(wireLength - 2));
        }
        else
        {
            Assert.Throws<InvalidDataException>(() =>
                parser.FormatResponse(response, 1024));
        }
    }

    [Test]
    public void ParserAndFormatterRejectEveryResponseTextControlCharacter()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        int value = 0;
        while (value <= 31)
        {
            AssertInvalidTextValue(parser, value);
            value++;
        }
        AssertInvalidTextValue(parser, 127);
    }

    [Test]
    public void FormatterRejectsInvalidDataAndConfiguration()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        Pop3Response invalidKind = new Pop3Response();
        Pop3Response controlText = new Pop3Response();
        Pop3Response negativeTimestamp = new Pop3Response();
        Pop3Response invalidTimestamp = new Pop3Response();
        Pop3Response nullText = new Pop3Response();
        Pop3Response nullResponseCode = new Pop3Response();
        invalidKind.Kind = (Pop3ResponseKind)99;
        controlText.Text = "bad\ntext";
        negativeTimestamp.Kind = Pop3ResponseKind.Negative;
        negativeTimestamp.GreetingTimestamp = "<a@b>";
        invalidTimestamp.GreetingTimestamp = "<missing-at>";
        nullText.Text = null!;
        nullResponseCode.ResponseCodeLevels = null!;

        Assert.Throws<ArgumentNullException>(() => parser.FormatResponse(null!, 1024));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.FormatResponse(new Pop3Response(), 1));
        Assert.Throws<InvalidDataException>(() => parser.FormatResponse(invalidKind, 1024));
        Assert.Throws<InvalidDataException>(() => parser.FormatResponse(controlText, 1024));
        Assert.Throws<InvalidDataException>(() => parser.FormatResponse(negativeTimestamp, 1024));
        Assert.Throws<InvalidDataException>(() => parser.FormatResponse(invalidTimestamp, 1024));
        Assert.Throws<InvalidDataException>(() => parser.FormatResponse(nullText, 1024));
        Assert.Throws<InvalidDataException>(() => parser.FormatResponse(nullResponseCode, 1024));
        Assert.Throws<ArgumentOutOfRangeException>(() => parser.ParseResponse(new byte[] { 43, 79, 75 }, false, 1));
    }

    static IEnumerable<TestCaseData> ResponseCodeRoundTripCases()
    {
        yield return new TestCaseData(
            new[] { "login-delay", "Account" },
            "[LOGIN-DELAY/Account]");
        yield return new TestCaseData(
            new[] { "in-use", "Lock" },
            "[IN-USE/Lock]");
        yield return new TestCaseData(
            new[] { "sys", "temp", "Backend" },
            "[SYS/TEMP/Backend]");
        yield return new TestCaseData(
            new[] { "sys", "perm", "Storage" },
            "[SYS/PERM/Storage]");
        yield return new TestCaseData(
            new[] { "auth", "Password" },
            "[AUTH/Password]");
        yield return new TestCaseData(
            new[] { "utf8", "Required" },
            "[UTF8/Required]");
        yield return new TestCaseData(
            new[] { "X-Vendor", "Detail_1", "Leaf" },
            "[X-Vendor/Detail_1/Leaf]");
    }

    static void AssertInvalidTextValue(Pop3ResponseParser parser, int value)
    {
        byte[] line = new byte[] { 43, 79, 75, 32, 97, (byte)value, 98 };
        Pop3Response response = new Pop3Response();
        response.Text = "a" + (char)value + "b";

        Assert.Throws<InvalidDataException>(() =>
            parser.ParseResponse(line, false, 1024),
            "parse control 0x" + value.ToString("X2"));
        Assert.Throws<InvalidDataException>(() =>
            parser.FormatResponse(response, 1024),
            "format control U+" + value.ToString("X4"));
    }

    private static Pop3Response Parse(string line, bool isGreeting, int maximumSaslLineOctets)
    {
        return new Pop3ResponseParser().ParseResponse(Encoding.UTF8.GetBytes(line), isGreeting, maximumSaslLineOctets);
    }
}
