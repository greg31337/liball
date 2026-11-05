using System.Text;

namespace pop3.Tests;

public class Pop3ResponseCodeParserTests
{
    [Test]
    public void ParserPreservesEveryHierarchicalLevelAndHumanText()
    {
        Pop3Response response = Parse("-ERR [sYs/TeMp/vendor-detail] mailbox busy");

        Assert.That(response.ResponseCodeLevels, Is.EqualTo(new[] { "sYs", "TeMp", "vendor-detail" }));
        Assert.That(response.Text, Is.EqualTo("[sYs/TeMp/vendor-detail] mailbox busy"));
    }

    [Test]
    public void ParserPreservesUnknownResponseCodes()
    {
        Pop3ResponseCodeParser parser = new Pop3ResponseCodeParser();
        string[] levels = parser.ParseResponseCode(Encoding.ASCII.GetBytes("[X-Vendor/detail_1] explanatory text"));

        Assert.That(levels, Is.EqualTo(new[] { "X-Vendor", "detail_1" }));
        Assert.That(parser.GetRegisteredResponseCode(levels), Is.Empty);
        Assert.That(Encoding.ASCII.GetString(parser.FormatResponseCode(levels)), Is.EqualTo("[X-Vendor/detail_1]"));
    }

    [TestCase("login-delay/account", Pop3ResponseCodes.LoginDelay, Pop3AuthenticationStatus.LoginDelay)]
    [TestCase("IN-use/vendor", Pop3ResponseCodes.InUse, Pop3AuthenticationStatus.InUse)]
    [TestCase("sys/temp/backend", Pop3ResponseCodes.SystemTemporary, Pop3AuthenticationStatus.TemporaryFailure)]
    [TestCase("SYS/perm/storage", Pop3ResponseCodes.SystemPermanent, Pop3AuthenticationStatus.PermanentFailure)]
    [TestCase("auth/password", Pop3ResponseCodes.Authentication, Pop3AuthenticationStatus.Rejected)]
    [TestCase("utfohner", "", Pop3AuthenticationStatus.Rejected)]
    [TestCase("utf8/required", Pop3ResponseCodes.Utf8, Pop3AuthenticationStatus.Rejected)]
    public void RegisteredCodesAreInterpretedCaseInsensitivelyAndByKnownPrefix(
        string code,
        string expectedRegisteredCode,
        Pop3AuthenticationStatus expectedStatus)
    {
        Pop3ResponseCodeParser parser = new Pop3ResponseCodeParser();
        string[] levels = code.Split('/');

        Assert.That(parser.GetRegisteredResponseCode(levels), Is.EqualTo(expectedRegisteredCode));
        Assert.That(parser.ClassifyAuthenticationStatus(levels), Is.EqualTo(expectedStatus));
    }

    [TestCaseSource(nameof(CanonicalFormattingCases))]
    public void FormatterCanonicalizesRegisteredLevelsAndPreservesUnknownDetails(string[] levels, string expected)
    {
        byte[] formatted = new Pop3ResponseCodeParser().FormatResponseCode(levels);

        Assert.That(Encoding.ASCII.GetString(formatted), Is.EqualTo(expected));
    }

    [Test]
    public void FormatterReturnsNoCodeForNoLevels()
    {
        Assert.That(new Pop3ResponseCodeParser().FormatResponseCode(Array.Empty<string>()), Is.Empty);
    }

    [TestCase("")]
    [TestCase("[]")]
    [TestCase("[/AUTH]")]
    [TestCase("[AUTH/]")]
    [TestCase("[SYS//TEMP]")]
    [TestCase("[AUTH")]
    [TestCase("[AU TH]")]
    public void ParserRejectsMalformedBracketedCodes(string text)
    {
        Pop3ResponseCodeParser parser = new Pop3ResponseCodeParser();
        byte[] bytes = Encoding.ASCII.GetBytes(text);

        if (text.Length == 0)
        {
            Assert.That(parser.ParseResponseCode(bytes), Is.Empty);
        }
        else
        {
            Assert.Throws<InvalidDataException>(() => parser.ParseResponseCode(bytes));
        }
    }

    [Test]
    public void ParserRejectsNonAsciiCodeOctets()
    {
        Pop3ResponseCodeParser parser = new Pop3ResponseCodeParser();

        Assert.Throws<InvalidDataException>(() => parser.ParseResponseCode(new byte[] { 91, 65, 127, 93 }));
        Assert.Throws<InvalidDataException>(() => parser.ParseResponseCode(new byte[] { 91, 65, 195, 169, 93 }));
    }

    [TestCaseSource(nameof(InvalidFormattingCases))]
    public void FormatterRejectsInvalidLevels(object value)
    {
        string[] levels = (string[])value;
        Assert.Throws<InvalidDataException>(() => new Pop3ResponseCodeParser().FormatResponseCode(levels));
    }

    [Test]
    public void ResponseParserIgnoresBracketedSaslPayloadsAndNonLeadingCodes()
    {
        Pop3Response sasl = Parse("+ [AUTH]");
        Pop3Response humanText = Parse("+OK ready [AUTH]");

        Assert.That(sasl.ResponseCodeLevels, Is.Empty);
        Assert.That(humanText.ResponseCodeLevels, Is.Empty);
    }

    [Test]
    public void ResponseFormatterAddsAResponseCodeAndRoundTripsIt()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = "mailbox busy";
        response.ResponseCodeLevels = new[] { "sys", "temp", "Backend" };

        byte[] line = parser.FormatResponse(response, 1024);
        Pop3Response parsed = parser.ParseResponse(line, false, 1024);

        Assert.That(Encoding.ASCII.GetString(line), Is.EqualTo("-ERR [SYS/TEMP/Backend] mailbox busy"));
        Assert.That(parsed.ResponseCodeLevels, Is.EqualTo(new[] { "SYS", "TEMP", "Backend" }));
        Assert.That(parsed.Text, Is.EqualTo("[SYS/TEMP/Backend] mailbox busy"));
    }

    [Test]
    public void ResponseFormatterDoesNotDuplicateAnExistingEquivalentCode()
    {
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = "[sys/temp/vendor] retry later";
        response.ResponseCodeLevels = new[] { "SYS", "TEMP", "VENDOR" };

        byte[] line = new Pop3ResponseParser().FormatResponse(response, 1024);

        Assert.That(Encoding.ASCII.GetString(line), Is.EqualTo("-ERR [sys/temp/vendor] retry later"));
    }

    [Test]
    public void ResponseFormatterRejectsConflictingOrSaslResponseCodes()
    {
        Pop3Response conflicting = new Pop3Response();
        conflicting.Kind = Pop3ResponseKind.Negative;
        conflicting.Text = "[AUTH] rejected";
        conflicting.ResponseCodeLevels = new[] { "SYS", "TEMP" };
        Pop3Response sasl = new Pop3Response();
        sasl.Kind = Pop3ResponseKind.SaslContinuation;
        sasl.ResponseCodeLevels = new[] { "AUTH" };

        Assert.Throws<InvalidDataException>(() => new Pop3ResponseParser().FormatResponse(conflicting, 1024));
        Assert.Throws<InvalidDataException>(() => new Pop3ResponseParser().FormatResponse(sasl, 1024));
    }

    [Test]
    public void ResponseCodesParticipateInTheNormalResponseOctetLimit()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        Pop3Response exact = new Pop3Response();
        exact.Kind = Pop3ResponseKind.Negative;
        exact.Text = new string('a', 498);
        exact.ResponseCodeLevels = new[] { "AUTH" };
        Pop3Response tooLong = new Pop3Response();
        tooLong.Kind = Pop3ResponseKind.Negative;
        tooLong.Text = new string('a', 499);
        tooLong.ResponseCodeLevels = new[] { "AUTH" };

        Assert.That(parser.FormatResponse(exact, 1024), Has.Length.EqualTo(510));
        Assert.Throws<InvalidDataException>(() => parser.FormatResponse(tooLong, 1024));
    }

    [Test]
    public void GreetingFormattingPlacesTheCodeBeforeTextAndTimestamp()
    {
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Positive;
        response.Text = "server ready";
        response.ResponseCodeLevels = new[] { "X-GREETING" };
        response.GreetingTimestamp = "<123@example.test>";

        byte[] line = new Pop3ResponseParser().FormatResponse(response, 1024);

        Assert.That(Encoding.ASCII.GetString(line), Is.EqualTo("+OK [X-GREETING] server ready <123@example.test>"));
    }

    [Test]
    public void GreetingTimestampDetectionIgnoresTimestampShapedCodeDetails()
    {
        Pop3ResponseParser parser = new Pop3ResponseParser();
        Pop3Response parsedWithoutTimestamp = parser.ParseResponse(
            Encoding.ASCII.GetBytes("+OK [X-DETAIL/<not@timestamp>] ready"),
            true,
            1024);
        Pop3Response parsedWithTimestamp = parser.ParseResponse(
            Encoding.ASCII.GetBytes("+OK [X-DETAIL/<not@timestamp>] ready <123@example.test>"),
            true,
            1024);

        Assert.That(parsedWithoutTimestamp.GreetingTimestamp, Is.Null);
        Assert.That(parsedWithTimestamp.GreetingTimestamp, Is.EqualTo("<123@example.test>"));
    }

    static IEnumerable<TestCaseData> CanonicalFormattingCases()
    {
        yield return new TestCaseData(new[] { "login-delay", "Vendor" }, "[LOGIN-DELAY/Vendor]");
        yield return new TestCaseData(new[] { "in-use" }, "[IN-USE]");
        yield return new TestCaseData(new[] { "sys", "temp", "Vendor" }, "[SYS/TEMP/Vendor]");
        yield return new TestCaseData(new[] { "sys", "perm" }, "[SYS/PERM]");
        yield return new TestCaseData(new[] { "auth" }, "[AUTH]");
        yield return new TestCaseData(new[] { "utf8" }, "[UTF8]");
        yield return new TestCaseData(new[] { "X-Vendor", "Detail" }, "[X-Vendor/Detail]");
    }

    static IEnumerable<TestCaseData> InvalidFormattingCases()
    {
        yield return new TestCaseData((object)new string[] { null! });
        yield return new TestCaseData((object)new[] { "" });
        yield return new TestCaseData((object)new[] { "SYS/TEMP" });
        yield return new TestCaseData((object)new[] { "AUTH]" });
        yield return new TestCaseData((object)new[] { "AU TH" });
        yield return new TestCaseData((object)new[] { "café" });
    }

    static Pop3Response Parse(string line)
    {
        return new Pop3ResponseParser().ParseResponse(Encoding.UTF8.GetBytes(line), false, 1024);
    }
}
