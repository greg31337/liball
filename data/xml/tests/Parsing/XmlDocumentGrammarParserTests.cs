using xml;

namespace xml.Tests;

[TestFixture]
public class XmlDocumentGrammarParserTests
{
    [Test]
    public void ParsesDeclarationDoctypeRootAndMiscContent()
    {
        string text = "<?xml version='1.1' encoding='UTF-8' standalone='yes'?> \n<!--before--><?prolog ok?><!DOCTYPE root [<!ELEMENT root (#PCDATA)>]>\n<root><child/>text</root> <?after ok?><!--end-->";
        XmlDocumentGrammarParser parser = CreateParser(text);

        XmlDocumentStructure document = parser.ParseXmlDocumentStructure();

        Assert.That(document.HasXmlDeclaration, Is.True);
        Assert.That(document.Version, Is.EqualTo(XmlVersion.Xml11));
        Assert.That(document.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(document.Standalone, Is.EqualTo(XmlStandalone.Yes));
        Assert.That(document.HasDocumentType, Is.True);
        Assert.That(document.DocumentTypeName, Is.EqualTo("root"));
        Assert.That(document.RootName, Is.EqualTo("root"));
        Assert.That(document.PrologMiscCount, Is.EqualTo(4));
        Assert.That(document.EpilogMiscCount, Is.EqualTo(3));
        Assert.That(parser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.Complete));
        Assert.That(parser.ElementParser.CurrentXmlElementDepth(), Is.Zero);
    }

    [TestCase("<root/>", XmlStandalone.Unspecified)]
    [TestCase("<?xml version='1.0' standalone='no'?><root/>", XmlStandalone.No)]
    public void AcceptsAnEmptyRootAndRetainsStandaloneMetadata(string text, XmlStandalone expectedStandalone)
    {
        XmlDocumentGrammarParser parser = CreateParser(text);

        XmlDocumentStructure document = parser.ParseXmlDocumentStructure();

        Assert.That(document.RootName, Is.EqualTo("root"));
        Assert.That(document.Standalone, Is.EqualTo(expectedStandalone));
        Assert.That(parser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.Complete));
        Assert.That(parser.ElementParser.CurrentXmlElementDepth(), Is.Zero);
    }

    [Test]
    public void AcceptsNestedElementContentAndAllMiscFormsAroundTheRoot()
    {
        string text = "\t<?before data?><!--before--><outer><middle><leaf/></middle></outer>\r\n<!--after--><?after?>";
        XmlDocumentGrammarParser parser = CreateParser(text);

        XmlDocumentStructure document = parser.ParseXmlDocumentStructure();

        Assert.That(document.RootName, Is.EqualTo("outer"));
        Assert.That(document.PrologMiscCount, Is.EqualTo(3));
        Assert.That(document.EpilogMiscCount, Is.EqualTo(3));
        Assert.That(document.HasXmlDeclaration, Is.False);
        Assert.That(document.HasDocumentType, Is.False);
    }

    [TestCase("")]
    [TestCase(" \n\t")]
    [TestCase("<!--only misc-->")]
    [TestCase("<!DOCTYPE root>")]
    [TestCase("<?xml version='1.0'?>")]
    public void RejectsDocumentsWithoutARootElement(string text)
    {
        XmlDocumentGrammarParser parser = CreateParser(text);

        Assert.Throws<FormatException>(() => parser.ParseXmlDocumentStructure());
        Assert.That(parser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.Prolog));
    }

    [TestCase("<first/><second/>")]
    [TestCase("<first></first><second></second>")]
    public void RejectsAdditionalRootElements(string text)
    {
        XmlDocumentGrammarParser parser = CreateParser(text);

        Assert.Throws<FormatException>(() => parser.ParseXmlDocumentStructure());
        Assert.That(parser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.Epilog));
        Assert.That(parser.ElementParser.CurrentXmlElementDepth(), Is.Zero);
    }

    [TestCase("text<root/>")]
    [TestCase("&entity;<root/>")]
    [TestCase("<![CDATA[data]]><root/>")]
    [TestCase("<root/>text")]
    [TestCase("<root/>&entity;")]
    [TestCase("<root/><![CDATA[data]]>")]
    public void RejectsNonMiscContentOutsideTheRoot(string text)
    {
        XmlDocumentGrammarParser parser = CreateParser(text);

        Assert.Throws<FormatException>(() => parser.ParseXmlDocumentStructure());
    }

    [TestCase("<!DOCTYPE root><!DOCTYPE root><root/>")]
    [TestCase("<root/><!DOCTYPE root>")]
    [TestCase("<root><!DOCTYPE root></root>")]
    public void RejectsDuplicateOrMisplacedDoctypes(string text)
    {
        XmlDocumentGrammarParser parser = CreateParser(text);

        Assert.Throws<FormatException>(() => parser.ParseXmlDocumentStructure());
    }

    [TestCase("<!DOCTYPE root><other/>")]
    [TestCase("<!DOCTYPE p:root><p:Root/>")]
    [TestCase("<!DOCTYPE Root><root></root>")]
    public void LeavesDoctypeRootMismatchToDtdValidation(string text)
    {
        XmlDocumentGrammarParser parser = CreateParser(text);

        XmlDocumentStructure document = parser.ParseXmlDocumentStructure();

        Assert.That(document.RootName, Is.Not.EqualTo(document.DocumentTypeName));
        Assert.That(parser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.Complete));
    }

    [TestCase("<!DOCTYPE root><root/>", "root")]
    [TestCase("<!DOCTYPE p:root><p:root></p:root>", "p:root")]
    public void AcceptsExactDoctypeAndDocumentElementNameMatches(string text, string expectedName)
    {
        XmlDocumentStructure document = CreateParser(text).ParseXmlDocumentStructure();

        Assert.That(document.DocumentTypeName, Is.EqualTo(expectedName));
        Assert.That(document.RootName, Is.EqualTo(expectedName));
    }

    [TestCase("<!ELEMENT root><root/>")]
    [TestCase("<!doctype root><root/>")]
    [TestCase("<! DOCTYPE root><root/>")]
    [TestCase("<!DOCTYPE><root/>")]
    [TestCase("<!DOCTYPE ><root/>")]
    public void RejectsMalformedDocumentTypeOpenings(string text)
    {
        XmlDocumentGrammarParser parser = CreateParser(text);

        Assert.Throws<FormatException>(() => parser.ParseXmlDocumentStructure());
        Assert.That(parser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.Prolog));
    }

    [TestCase("<root>")]
    [TestCase("<root><child/></root")]
    [TestCase("<root><child></child>")]
    public void RejectsDocumentsWhoseRootDoesNotClose(string text)
    {
        XmlDocumentGrammarParser parser = CreateParser(text);

        Assert.Throws<FormatException>(() => parser.ParseXmlDocumentStructure());
        Assert.That(parser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.ElementContent));
        Assert.That(parser.ElementParser.CurrentXmlElementDepth(), Is.GreaterThan(0));
    }

    [Test]
    public void RejectsAnXmlDeclarationAfterLeadingMisc()
    {
        XmlDocumentGrammarParser parser = CreateParser(" <!--before--><?xml version='1.0'?><root/>");

        Assert.Throws<FormatException>(() => parser.ParseXmlDocumentStructure());
        Assert.That(parser.CurrentXmlDocumentPhase(), Is.EqualTo(XmlDocumentPhase.Prolog));
    }

    [Test]
    public void RejectsASecondParseAttempt()
    {
        XmlDocumentGrammarParser parser = CreateParser("<root/>");
        parser.ParseXmlDocumentStructure();

        Assert.Throws<InvalidOperationException>(() => parser.ParseXmlDocumentStructure());
    }

    [Test]
    public void ConstructorRejectsAnExternalParsedEntityScanner()
    {
        XmlScanner scanner = new XmlScanner("<root/>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.ExternalParsedEntity);

        Assert.Throws<ArgumentException>(() => new XmlDocumentGrammarParser(scanner, true, null));
    }

    private static XmlDocumentGrammarParser CreateParser(string text)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        return new XmlDocumentGrammarParser(scanner, true, null);
    }
}
