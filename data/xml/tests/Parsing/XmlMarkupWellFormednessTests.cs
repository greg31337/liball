using xml;

namespace xml.Tests;

[TestFixture]
public class XmlMarkupWellFormednessTests
{
    [TestCase("<!---->", "")]
    [TestCase("<!--a-b-->", "a-b")]
    [TestCase("<!-- line one\nline two -->", " line one\nline two ")]
    public void ScannerAcceptsWellFormedComments(string text, string expectedValue)
    {
        XmlScanner scanner = CreateScanner(text);

        XmlToken token = scanner.ScanNextXmlToken();

        Assert.That(token.Kind, Is.EqualTo(XmlTokenKind.Comment));
        Assert.That(token.Value, Is.EqualTo(expectedValue));
        Assert.That(scanner.CurrentXmlScannerOffset(), Is.EqualTo(text.Length));
    }

    [TestCase("<!--a--b-->")]
    [TestCase("<!--a--->")]
    [TestCase("<!----->")]
    public void ScannerRejectsForbiddenCommentHyphensWithoutAdvancing(string text)
    {
        XmlScanner scanner = CreateScanner(text);

        Assert.Throws<FormatException>(() => scanner.ScanNextXmlToken());
        Assert.That(scanner.CurrentXmlScannerOffset(), Is.Zero);
        Assert.That(scanner.CurrentXmlScannerState(), Is.EqualTo(XmlScannerState.Content));
    }

    [TestCase("<?target?>", "target", "", false)]
    [TestCase("<?target   ?>", "target", "", true)]
    [TestCase("<?target data?>", "target", "data", true)]
    [TestCase("<?xml-stylesheet href='style.css'?>", "xml-stylesheet", "href='style.css'", true)]
    [TestCase("<?a:b data?>", "a:b", "data", true)]
    [TestCase("<?\u03C0 data?>", "\u03C0", "data", true)]
    public void ScannerAcceptsLegalProcessingInstructionTargetsAndSeparation(string text, string target, string value, bool separated)
    {
        XmlScanner scanner = CreateScanner(text);

        XmlToken token = scanner.ScanNextXmlToken();

        Assert.That(token.Kind, Is.EqualTo(XmlTokenKind.ProcessingInstruction));
        Assert.That(token.Name, Is.EqualTo(target));
        Assert.That(token.Value, Is.EqualTo(value));
        Assert.That(token.HasNameValueSeparator, Is.EqualTo(separated));
    }

    [TestCase("<?target/data?>")]
    [TestCase("<?target?data?>")]
    [TestCase("<?XML?>")]
    [TestCase("<?xMl data?>")]
    [TestCase("<?xml?>")]
    [TestCase("<?9target?>")]
    public void ScannerRejectsInvalidProcessingInstructionTargetsAndMissingSeparation(string text)
    {
        XmlScanner scanner = CreateScanner(text);

        Assert.Throws<FormatException>(() => scanner.ScanNextXmlToken());
        Assert.That(scanner.CurrentXmlScannerOffset(), Is.Zero);
    }

    [Test]
    public void CharacterDataRejectsCDataClosingSequenceTransactionally()
    {
        XmlScanner valid = CreateScanner("left]] >right");
        Assert.That(valid.ScanNextXmlToken().Value, Is.EqualTo("left]] >right"));

        XmlScanner invalid = CreateScanner("left]]>right");
        Assert.Throws<FormatException>(() => invalid.ScanNextXmlToken());
        Assert.That(invalid.CurrentXmlScannerOffset(), Is.Zero);
    }

    [TestCase("<![CDATA[]]>", "")]
    [TestCase("<![CDATA[a < b & c]]>", "a < b & c")]
    [TestCase("<![CDATA[]]]]>", "]]")]
    public void ScannerAcceptsCDataContentUpToTheFirstClosingDelimiter(string text, string value)
    {
        XmlScanner scanner = CreateScanner(text);

        XmlToken token = scanner.ScanNextXmlToken();

        Assert.That(token.Kind, Is.EqualTo(XmlTokenKind.CData));
        Assert.That(token.Value, Is.EqualTo(value));
        Assert.That(token.Lexeme, Is.EqualTo(text));
    }

    [Test]
    public void ValidatorsDefensivelyRejectForbiddenSequencesInSyntheticTokens()
    {
        XmlToken comment = CreateValueToken(XmlTokenKind.Comment, null, "a--b", false);
        XmlToken processingInstruction = CreateValueToken(XmlTokenKind.ProcessingInstruction, "target", "a?>b", true);
        XmlToken cdata = CreateValueToken(XmlTokenKind.CData, null, "a]]>b", false);
        XmlToken lessThan = CreateValueToken(XmlTokenKind.Text, null, "a<b", false);
        XmlToken ampersand = CreateValueToken(XmlTokenKind.Text, null, "a&b", false);

        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlCommentToken(comment));
        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlProcessingInstructionToken(processingInstruction));
        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlCDataToken(cdata));
        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlCharacterDataToken(lessThan));
        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlCharacterDataToken(ampersand));
    }

    [Test]
    public void ProcessingInstructionValidatorRequiresANameAndSeparatorForData()
    {
        XmlToken missingTarget = CreateValueToken(XmlTokenKind.ProcessingInstruction, null, "", false);
        XmlToken invalidTarget = CreateValueToken(XmlTokenKind.ProcessingInstruction, "9target", "", false);
        XmlToken reservedTarget = CreateValueToken(XmlTokenKind.ProcessingInstruction, "XmL", "", false);
        XmlToken missingSeparator = CreateValueToken(XmlTokenKind.ProcessingInstruction, "target", "data", false);

        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlProcessingInstructionToken(missingTarget));
        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlProcessingInstructionToken(invalidTarget));
        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlProcessingInstructionToken(reservedTarget));
        Assert.Throws<FormatException>(() => XmlMarkupWellFormedness.ValidateXmlProcessingInstructionToken(missingSeparator));
    }

    [TestCase("<!--bad\0character-->", XmlVersion.Xml10)]
    [TestCase("<![CDATA[bad\0character]]>", XmlVersion.Xml10)]
    [TestCase("<?target bad\0character?>", XmlVersion.Xml10)]
    [TestCase("<!--bad\u007Fcharacter-->", XmlVersion.Xml11)]
    [TestCase("<![CDATA[bad\u007Fcharacter]]>", XmlVersion.Xml11)]
    [TestCase("<?target bad\u007Fcharacter?>", XmlVersion.Xml11)]
    public void ScannerRejectsIllegalDirectCharactersInEveryMarkupTextForm(string text, XmlVersion version)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), version, XmlEncodingEntityKind.Document);

        Assert.Throws<FormatException>(() => scanner.ScanNextXmlToken());
        Assert.That(scanner.CurrentXmlScannerOffset(), Is.Zero);
    }

    [Test]
    public void ValidatorsRejectTokensOfTheWrongKind()
    {
        XmlToken text = CreateValueToken(XmlTokenKind.Text, null, "text", false);

        Assert.Throws<ArgumentException>(() => XmlMarkupWellFormedness.ValidateXmlCommentToken(text));
        Assert.Throws<ArgumentException>(() => XmlMarkupWellFormedness.ValidateXmlProcessingInstructionToken(text));
        Assert.Throws<ArgumentException>(() => XmlMarkupWellFormedness.ValidateXmlCDataToken(text));
    }

    private static XmlScanner CreateScanner(string text)
    {
        return new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
    }

    private static XmlToken CreateValueToken(XmlTokenKind kind, string? name, string value, bool separated)
    {
        return new XmlToken(kind, name, value, value, 0, value.Length, 0, false, '\0', separated, null);
    }
}
