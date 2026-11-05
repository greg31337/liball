using xml;

namespace xml.Tests;

[TestFixture]
public class XmlDeclarationParserTests
{
    [Test]
    public void ParsesCompleteXmlDeclarationInRequiredOrder()
    {
        string text = "<?xml \nversion = \"1.1\"\tencoding='UTF-8' standalone = 'yes' ?>rest";

        XmlDeclarationInfo declaration = ParseDeclaration(text, XmlEncodingEntityKind.Document);

        Assert.That(declaration.HasDeclaration, Is.True);
        Assert.That(declaration.IsTextDeclaration, Is.False);
        Assert.That(declaration.HasVersion, Is.True);
        Assert.That(declaration.Version, Is.EqualTo(XmlVersion.Xml11));
        Assert.That(declaration.VersionNumber, Is.EqualTo("1.1"));
        Assert.That(declaration.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(declaration.Standalone, Is.EqualTo(XmlStandalone.Yes));
        Assert.That(declaration.StartUtf16Offset, Is.Zero);
        Assert.That(declaration.CharacterCount, Is.EqualTo(text.IndexOf("?>", StringComparison.Ordinal) + 2));
    }

    [TestCase("<?xml version='1.0'?>", XmlVersion.Xml10, "1.0")]
    [TestCase("<?xml version=\"1.1\" ?>", XmlVersion.Xml11, "1.1")]
    [TestCase("<?xml version='1.20'?>", XmlVersion.Xml10, "1.20")]
    public void ParsesSupportedDocumentVersions(string text, XmlVersion expected, string versionNumber)
    {
        XmlDeclarationInfo declaration = ParseDeclaration(text, XmlEncodingEntityKind.Document);

        Assert.That(declaration.HasVersion, Is.True);
        Assert.That(declaration.Version, Is.EqualTo(expected));
        Assert.That(declaration.VersionNumber, Is.EqualTo(versionNumber));
        Assert.That(declaration.EncodingName, Is.Null);
        Assert.That(declaration.Standalone, Is.EqualTo(XmlStandalone.Unspecified));
    }

    [TestCase("yes", XmlStandalone.Yes)]
    [TestCase("no", XmlStandalone.No)]
    public void ParsesBothStandaloneValues(string value, XmlStandalone expected)
    {
        XmlDeclarationInfo declaration = ParseDeclaration("<?xml version='1.0' standalone='" + value + "'?>", XmlEncodingEntityKind.Document);

        Assert.That(declaration.Standalone, Is.EqualTo(expected));
    }

    [Test]
    public void ParsesTextDeclarationsWithOptionalVersionAndRequiredEncoding()
    {
        XmlDeclarationInfo withoutVersion = ParseDeclaration("<?xml encoding='ISO-8859-1'?>entity", XmlEncodingEntityKind.ExternalParsedEntity);
        XmlDeclarationInfo withVersion = ParseDeclaration("<?xml version='1.1' encoding=\"UTF-8\" ?>entity", XmlEncodingEntityKind.ExternalParsedEntity);

        Assert.That(withoutVersion.HasDeclaration, Is.True);
        Assert.That(withoutVersion.IsTextDeclaration, Is.True);
        Assert.That(withoutVersion.HasVersion, Is.False);
        Assert.That(withoutVersion.EncodingName, Is.EqualTo("ISO-8859-1"));
        Assert.That(withVersion.HasVersion, Is.True);
        Assert.That(withVersion.Version, Is.EqualTo(XmlVersion.Xml11));
        Assert.That(withVersion.EncodingName, Is.EqualTo("UTF-8"));
    }

    [Test]
    public void LeavesOrdinaryContentAndLongerProcessingInstructionTargetsUnconsumed()
    {
        XmlDeclarationInfo content = ParseDeclaration("<root/>", XmlEncodingEntityKind.Document);
        XmlDeclarationInfo processingInstruction = ParseDeclaration("<?xml-stylesheet href='style.css'?>", XmlEncodingEntityKind.Document);

        Assert.That(content.HasDeclaration, Is.False);
        Assert.That(content.CharacterCount, Is.Zero);
        Assert.That(processingInstruction.HasDeclaration, Is.False);
        Assert.That(processingInstruction.CharacterCount, Is.Zero);
    }

    [TestCase("<?xml?>")]
    [TestCase("<?xml encoding='UTF-8'?>")]
    [TestCase("<?xml version='1.0' version='1.0'?>")]
    [TestCase("<?xml version='1.0' standalone='yes' encoding='UTF-8'?>")]
    [TestCase("<?xml version='1.0' flavor='strict'?>")]
    [TestCase("<?xml version='1.0'encoding='UTF-8'?>")]
    public void RejectsMissingDuplicatedUnknownAndOutOfOrderDocumentAttributes(string text)
    {
        Assert.Throws<FormatException>(() => ParseDeclaration(text, XmlEncodingEntityKind.Document));
    }

    [TestCase("<?xml version='2.0'?>")]
    [TestCase("<?xml version='1.'?>")]
    [TestCase("<?xml version='1.a'?>")]
    [TestCase("<?xml Version='1.0'?>")]
    [TestCase("<?xml version=1.0?>")]
    [TestCase("<?xml version='1.0\"?>")]
    [TestCase("<?xml version='1.0' encoding=''?>")]
    [TestCase("<?xml version='1.0' encoding='8BIT'?>")]
    [TestCase("<?xml version='1.0' encoding='UTF:8'?>")]
    [TestCase("<?xml version='1.0' standalone='true'?>")]
    public void RejectsInvalidQuotesAndPseudoAttributeValues(string text)
    {
        Assert.Throws<FormatException>(() => ParseDeclaration(text, XmlEncodingEntityKind.Document));
    }

    [TestCase("<?xml version='1.0'?>")]
    [TestCase("<?xml encoding='UTF-8' standalone='no'?>")]
    [TestCase("<?xml encoding='UTF-8' version='1.0'?>")]
    [TestCase("<?xml encoding='UTF-8' encoding='UTF-8'?>")]
    public void RejectsInvalidTextDeclarationAttributeSets(string text)
    {
        Assert.Throws<FormatException>(() => ParseDeclaration(text, XmlEncodingEntityKind.ExternalParsedEntity));
    }

    [TestCase("<?XML version='1.0'?>")]
    [TestCase("<?Xml version='1.0'?>")]
    public void RejectsCaseVariantsOfReservedXmlTarget(string text)
    {
        Assert.Throws<FormatException>(() => ParseDeclaration(text, XmlEncodingEntityKind.Document));
    }

    [Test]
    public void RejectsUnterminatedAndMisplacedDeclarations()
    {
        Assert.Throws<FormatException>(() => ParseDeclaration("<?xml version='1.0'", XmlEncodingEntityKind.Document));

        string text = "x<?xml version='1.0'?>";
        XmlDeclarationParser misplaced = new XmlDeclarationParser(text.AsMemory(), XmlEncodingEntityKind.Document, 1);
        Assert.Throws<FormatException>(() => misplaced.ParseXmlOrTextDeclaration());

        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlDeclarationParser(text.AsMemory(), (XmlEncodingEntityKind)2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new XmlDeclarationParser(text.AsMemory(), XmlEncodingEntityKind.Document, text.Length + 1));
    }

    [Test]
    public void ParserCanReadTheSameDeclarationAgainWithoutRetainingState()
    {
        XmlDeclarationParser parser = new XmlDeclarationParser("<?xml version='1.0' encoding='UTF-8'?>".AsMemory(), XmlEncodingEntityKind.Document, 0);

        XmlDeclarationInfo first = parser.ParseXmlOrTextDeclaration();
        XmlDeclarationInfo second = parser.ParseXmlOrTextDeclaration();

        Assert.That(second.CharacterCount, Is.EqualTo(first.CharacterCount));
        Assert.That(second.VersionNumber, Is.EqualTo(first.VersionNumber));
        Assert.That(second.EncodingName, Is.EqualTo(first.EncodingName));
    }

    private static XmlDeclarationInfo ParseDeclaration(string text, XmlEncodingEntityKind entityKind)
    {
        XmlDeclarationParser parser = new XmlDeclarationParser(text.AsMemory(), entityKind, 0);
        return parser.ParseXmlOrTextDeclaration();
    }
}
