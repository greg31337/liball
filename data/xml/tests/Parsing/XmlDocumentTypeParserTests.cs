using xml;

namespace xml.Tests;

[TestFixture]
public class XmlDocumentTypeParserTests
{
    [TestCase("<!DOCTYPE root>", "root")]
    [TestCase("<!DOCTYPE p:root   >", "p:root")]
    [TestCase("<!DOCTYPE 名>", "名")]
    public void ParsesNamesAndClosingDelimiters(string text, string expectedName)
    {
        XmlDocumentTypeInfo information = ParseDeclaration(text);

        Assert.That(information.Name, Is.EqualTo(expectedName));
        Assert.That(information.PublicIdentifier, Is.Null);
        Assert.That(information.SystemIdentifier, Is.Null);
        Assert.That(information.InternalSubset, Is.Null);
        Assert.That(information.StartUtf16Offset, Is.Zero);
        Assert.That(information.Utf16Length, Is.EqualTo(text.Length));
    }

    [TestCase("<!DOCTYPE root SYSTEM 'document.dtd'>", "document.dtd")]
    [TestCase("<!DOCTYPE root SYSTEM \"https://example.test/root.dtd\">", "https://example.test/root.dtd")]
    public void ParsesSystemExternalIdentifiers(string text, string expectedSystemIdentifier)
    {
        XmlDocumentTypeInfo information = ParseDeclaration(text);

        Assert.That(information.PublicIdentifier, Is.Null);
        Assert.That(information.SystemIdentifier, Is.EqualTo(expectedSystemIdentifier));
        Assert.That(information.InternalSubset, Is.Null);
    }

    [Test]
    public void ParsesPublicExternalIdentifiersWithRequiredSystemLiterals()
    {
        string publicIdentifier = "-//Example Org//DTD Sample 1.0//EN";

        XmlDocumentTypeInfo information = ParseDeclaration("<!DOCTYPE root PUBLIC '" + publicIdentifier + "' \"sample.dtd\">");

        Assert.That(information.PublicIdentifier, Is.EqualTo(publicIdentifier));
        Assert.That(information.SystemIdentifier, Is.EqualTo("sample.dtd"));
        Assert.That(information.InternalSubset, Is.Null);
    }

    [Test]
    public void RetainsTheExactLexicalInternalSubsetWithoutItsBrackets()
    {
        string subset = "\n  <!ELEMENT root (child)>\n  <!ELEMENT child EMPTY>\n";
        string text = "<!DOCTYPE root SYSTEM 'root.dtd' [" + subset + "]>";

        XmlDocumentTypeInfo information = ParseDeclaration(text);

        Assert.That(information.Name, Is.EqualTo("root"));
        Assert.That(information.SystemIdentifier, Is.EqualTo("root.dtd"));
        Assert.That(information.InternalSubset, Is.EqualTo(subset));
        Assert.That(information.Utf16Length, Is.EqualTo(text.Length));
    }

    [Test]
    public void ClosingBracketsInsideLiteralsCommentsAndProcessingInstructionsDoNotEndTheSubset()
    {
        string subset = "<!ENTITY bracket ']'><!ELEMENT root EMPTY><!-- ] --><?inside ]?>";

        XmlDocumentTypeInfo information = ParseDeclaration("<!DOCTYPE root [" + subset + "]>");

        Assert.That(information.InternalSubset, Is.EqualTo(subset));
    }

    [TestCase("<!DOCTYPE root SYSTEM>")]
    [TestCase("<!DOCTYPE root SYSTEM 'root.dtd' extra>")]
    [TestCase("<!DOCTYPE root PUBLIC 'public-only'>")]
    [TestCase("<!DOCTYPE root PUBLIC 'public' 'system' extra>")]
    [TestCase("<!DOCTYPE root UNKNOWN 'value'>")]
    [TestCase("<!DOCTYPE root [] extra>")]
    [TestCase("<!DOCTYPE root [][]>")]
    [TestCase("<!DOCTYPE root [")]
    [TestCase("<!DOCTYPE root [<!ELEMENT root EMPTY>")]
    public void RejectsMalformedExternalIdentifiersSubsetsAndClosingDelimiters(string text)
    {
        Assert.Throws<FormatException>(() => ParseDeclaration(text));
    }

    [TestCase("<!DOCTYPE root SYSTEM'root.dtd'>")]
    [TestCase("<!DOCTYPE root PUBLIC'public' 'root.dtd'>")]
    [TestCase("<!DOCTYPE root PUBLIC 'public''root.dtd'>")]
    public void RequiresWhitespaceAroundExternalIdentifierParts(string text)
    {
        Assert.Throws<FormatException>(() => ParseDeclaration(text));
    }

    [TestCase("<!DOCTYPE root PUBLIC 'bad[id' 'root.dtd'>")]
    [TestCase("<!DOCTYPE root PUBLIC 'bad]id' 'root.dtd'>")]
    [TestCase("<!DOCTYPE root PUBLIC 'bad^id' 'root.dtd'>")]
    public void RejectsCharactersOutsidePubidChar(string text)
    {
        Assert.Throws<FormatException>(() => ParseDeclaration(text));
    }

    [TestCase("<!DOCTYPE root SYSTEM 'root.dtd#part'>")]
    [TestCase("<!DOCTYPE root PUBLIC 'public' 'root.dtd#part'>")]
    public void RejectsFragmentIdentifiersInSystemLiterals(string text)
    {
        Assert.Throws<FormatException>(() => ParseDeclaration(text));
    }

    [Test]
    public void GrammarStructureRetainsEveryDocumentTypeComponent()
    {
        string subset = "<!ELEMENT root EMPTY>";
        string declaration = "<!DOCTYPE root PUBLIC '-//Example//DTD Root//EN' 'root.dtd' [" + subset + "]>";
        XmlScanner scanner = new XmlScanner((declaration + "<root/>").AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);

        XmlDocumentStructure structure = new XmlDocumentGrammarParser(scanner, true, null).ParseXmlDocumentStructure();

        Assert.That(structure.HasDocumentType, Is.True);
        Assert.That(structure.DocumentTypeName, Is.EqualTo("root"));
        Assert.That(structure.DocumentTypePublicIdentifier, Is.EqualTo("-//Example//DTD Root//EN"));
        Assert.That(structure.DocumentTypeSystemIdentifier, Is.EqualTo("root.dtd"));
        Assert.That(structure.DocumentTypeInternalSubset, Is.EqualTo(subset));
        Assert.That(structure.DocumentTypeStartUtf16Offset, Is.Zero);
        Assert.That(structure.DocumentTypeUtf16Length, Is.EqualTo(declaration.Length));
        Assert.That(structure.RootName, Is.EqualTo("root"));
    }

    [Test]
    public void PublicParserRetainsDocumentTypeMetadataAndSourceRanges()
    {
        string subset = "\n<!ELEMENT p:root EMPTY>\n";
        string declaration = "<!DOCTYPE p:root PUBLIC '-//Example//DTD Root//EN' 'root.dtd' [" + subset + "]>";
        XmlParserOptions options = ParserOptions(XmlDtdProcessing.Parse);
        options.PreserveSourceLocations = true;
        XmlInput input = Input(declaration + "<p:root/>");
        input.BaseUri = "https://example.test/document.xml";

        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);

        XmlDocumentType documentType = document.DocumentType!;
        Assert.That(documentType.Name!.QualifiedName, Is.EqualTo("p:root"));
        Assert.That(documentType.PublicIdentifier, Is.EqualTo("-//Example//DTD Root//EN"));
        Assert.That(documentType.SystemIdentifier, Is.EqualTo("root.dtd"));
        Assert.That(documentType.InternalSubset, Is.EqualTo(subset));
        Assert.That(documentType.BaseUri, Is.EqualTo(input.BaseUri));
        Assert.That(documentType.Range!.Start!.CharacterOffset, Is.Zero);
        Assert.That(documentType.Range.End!.CharacterOffset, Is.EqualTo(declaration.Length));

        XmlNode node = document.DocumentNode!.Children![0];
        Assert.That(node.Kind, Is.EqualTo(XmlNodeKind.DocumentType));
        Assert.That(node.Name!.QualifiedName, Is.EqualTo("p:root"));
        Assert.That(node.Range!.Start!.CharacterOffset, Is.Zero);
        Assert.That(node.Range.End!.CharacterOffset, Is.EqualTo(declaration.Length));
    }

    [TestCase(XmlDtdProcessing.Ignore)]
    [TestCase(XmlDtdProcessing.Prohibit)]
    public void EveryDtdPolicyStillRejectsMalformedDocumentTypeSyntax(XmlDtdProcessing processing)
    {
        XmlParserOptions options = ParserOptions(processing);

        Assert.Throws<XmlWellFormednessException>(() => new XmlParser().ParseXmlDocument(Input("<!DOCTYPE root SYSTEM><root/>"), options, null, null, null));
    }

    [Test]
    public void IgnorePolicyParsesButDoesNotRetainAValidDocumentType()
    {
        XmlParserOptions options = ParserOptions(XmlDtdProcessing.Ignore);

        XmlDocument document = new XmlParser().ParseXmlDocument(Input("<!DOCTYPE root SYSTEM 'root.dtd' [<!ELEMENT root EMPTY>]><root/>"), options, null, null, null);

        Assert.That(document.DocumentType, Is.Null);
        Assert.That(document.DocumentNode!.Children, Has.Length.EqualTo(1));
        Assert.That(document.DocumentElement!.Name!.LocalName, Is.EqualTo("root"));
    }

    [Test]
    public void ParserReportsThePreciseInvalidPublicIdentifierLocation()
    {
        string text = "<!DOCTYPE root PUBLIC 'bad[id' 'root.dtd'><root/>";
        XmlParserOptions options = ParserOptions(XmlDtdProcessing.Parse);
        RecordingReporter reporter = new RecordingReporter();

        XmlWellFormednessException failure = Assert.Throws<XmlWellFormednessException>(() => new XmlParser().ParseXmlDocument(Input(text), options, null, reporter, null))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Location!.CharacterOffset, Is.EqualTo(text.IndexOf('[')));
    }

    [Test]
    public void DocumentTypeParserRejectsInvalidUseAndASecondParse()
    {
        XmlScanner scanner = new XmlScanner("<!DOCTYPE root>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        XmlToken opening = scanner.ScanNextXmlToken();
        XmlDocumentTypeParser parser = new XmlDocumentTypeParser(scanner);

        parser.ParseXmlDocumentTypeDeclaration(opening);

        Assert.Throws<InvalidOperationException>(() => parser.ParseXmlDocumentTypeDeclaration(opening));
        Assert.Throws<ArgumentNullException>(() => new XmlDocumentTypeParser(null!));
        Assert.Throws<ArgumentNullException>(() => new XmlDocumentTypeParser(new XmlScanner("".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document)).ParseXmlDocumentTypeDeclaration(null!));

        XmlScanner invalidScanner = new XmlScanner("<root/>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        XmlToken invalidOpening = invalidScanner.ScanNextXmlToken();
        Assert.Throws<ArgumentException>(() => new XmlDocumentTypeParser(invalidScanner).ParseXmlDocumentTypeDeclaration(invalidOpening));
    }

    private static XmlDocumentTypeInfo ParseDeclaration(string text)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        XmlToken opening = scanner.ScanNextXmlToken();
        return new XmlDocumentTypeParser(scanner).ParseXmlDocumentTypeDeclaration(opening);
    }

    private static XmlParserOptions ParserOptions(XmlDtdProcessing processing)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = processing;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        options.PreservePrefixes = true;
        return options;
    }

    private static XmlInput Input(string text)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "memory.xml";
        return input;
    }

    private sealed class RecordingReporter : IXmlErrorReporter
    {
        internal XmlDiagnostic? Diagnostic;

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }
}
