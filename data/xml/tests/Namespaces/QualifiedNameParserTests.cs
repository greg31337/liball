using xml;

namespace xml.Tests;

[TestFixture]
public class QualifiedNameParserTests
{
    [TestCase(XmlNamespaceHandling.Namespaces10)]
    [TestCase(XmlNamespaceHandling.Namespaces11)]
    public void ResolvesElementNamesUsingDefaultPrefixedAndImplicitXmlBindings(XmlNamespaceHandling handling)
    {
        QualifiedNameParser parser = new QualifiedNameParser(handling, null);
        NamespaceContext context = Context(
            Binding("", "urn:default"),
            Binding("p", "urn:p"));

        XmlName unprefixed = parser.ResolveXmlElementName("root", context, null);
        XmlName prefixed = parser.ResolveXmlElementName("p:child", context, null);
        XmlName reserved = parser.ResolveXmlElementName("xml:lang", context, null);

        AssertName(unprefixed, null, "root", "urn:default", "root");
        AssertName(prefixed, "p", "child", "urn:p", "p:child");
        AssertName(reserved, "xml", "lang", XmlNamespaces.Xml, "xml:lang");
    }

    [Test]
    public void DoesNotApplyTheDefaultNamespaceToUnprefixedAttributes()
    {
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces10, null);
        NamespaceContext context = Context(
            Binding("", "urn:default"),
            Binding("p", "urn:p"));

        XmlName unprefixed = parser.ResolveXmlAttributeName("id", context, null);
        XmlName prefixed = parser.ResolveXmlAttributeName("p:id", context, null);
        XmlName reserved = parser.ResolveXmlAttributeName("xml:lang", context, null);

        AssertName(unprefixed, null, "id", null, "id");
        AssertName(prefixed, "p", "id", "urn:p", "p:id");
        AssertName(reserved, "xml", "lang", XmlNamespaces.Xml, "xml:lang");
    }

    [Test]
    public void ResolvesNamespaceDeclarationAttributeNamesToTheReservedNamespace()
    {
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces11, null);
        NamespaceContext context = Context();

        XmlName defaultDeclaration = parser.ResolveXmlAttributeName("xmlns", context, null);
        XmlName prefixedDeclaration = parser.ResolveXmlAttributeName("xmlns:p", context, null);

        AssertName(defaultDeclaration, null, "xmlns", XmlNamespaces.Xmlns, "xmlns");
        AssertName(prefixedDeclaration, "xmlns", "p", XmlNamespaces.Xmlns, "xmlns:p");
    }

    [TestCase("name", null, "name")]
    [TestCase("p:name", "p", "name")]
    [TestCase("é", null, "é")]
    [TestCase("π:名", "π", "名")]
    public void ParsesValidQualifiedNames(string qualifiedName, string? prefix, string localName)
    {
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces11, null);

        QualifiedNameParts parts = parser.ParseXmlQualifiedName(qualifiedName, null);

        Assert.That(parts.Prefix, Is.EqualTo(prefix));
        Assert.That(parts.LocalName, Is.EqualTo(localName));
        Assert.That(parts.QualifiedName, Is.EqualTo(qualifiedName));
    }

    [TestCase("")]
    [TestCase(":name")]
    [TestCase("p:")]
    [TestCase("p:a:b")]
    [TestCase("1name")]
    [TestCase("p:1name")]
    [TestCase("p:bad name")]
    public void RejectsMalformedQualifiedNamesWithNamespaceDiagnostics(string qualifiedName)
    {
        RecordingReporter reporter = new RecordingReporter();
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces10, reporter);
        SourceRange range = RangeAt(5);

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => parser.ParseXmlQualifiedName(qualifiedName, range))!;

        AssertNamespaceFailure(failure, reporter, XmlErrorCodes.MalformedMarkup, range);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void RejectsUnboundPrefixesOnElementsAndAttributes(bool element)
    {
        RecordingReporter reporter = new RecordingReporter();
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces11, reporter);
        SourceRange range = RangeAt(11);

        XmlNamespaceException failure;
        if (element)
        {
            failure = Assert.Throws<XmlNamespaceException>(() => parser.ResolveXmlElementName("missing:name", Context(), range))!;
        }
        else
        {
            failure = Assert.Throws<XmlNamespaceException>(() => parser.ResolveXmlAttributeName("missing:name", Context(), range))!;
        }

        AssertNamespaceFailure(failure, reporter, XmlErrorCodes.NamespaceUnboundPrefix, range);
        Assert.That(failure.Message, Does.Contain("missing"));
    }

    [Test]
    public void RejectsTheXmlnsPrefixOnElementNames()
    {
        RecordingReporter reporter = new RecordingReporter();
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces11, reporter);
        SourceRange range = RangeAt(2);

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => parser.ResolveXmlElementName("xmlns:element", Context(), range))!;

        AssertNamespaceFailure(failure, reporter, XmlErrorCodes.NamespaceReservedBinding, range);
    }

    [Test]
    public void ResolvesAnAttributeArrayAtomicallyAndPreservesLexicalNames()
    {
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces11, null);
        NamespaceContext context = Context(Binding("p", "urn:p"));
        XmlAttributeData first = Attribute("plain");
        XmlAttributeData second = Attribute("p:named");
        XmlAttributeData declaration = Attribute("xmlns:p");
        declaration.IsNamespaceDeclaration = true;

        parser.ResolveXmlAttributeNames([first, second, declaration], context);

        AssertName(first.Name!, null, "plain", null, "plain");
        AssertName(second.Name!, "p", "named", "urn:p", "p:named");
        AssertName(declaration.Name!, "xmlns", "p", XmlNamespaces.Xmlns, "xmlns:p");
    }

    [Test]
    public void RejectsDuplicateAttributesByExpandedName()
    {
        RecordingReporter reporter = new RecordingReporter();
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces10, reporter);
        NamespaceContext context = Context(
            Binding("p", "urn:shared"),
            Binding("q", "urn:shared"));
        XmlAttributeData first = Attribute("p:id");
        XmlAttributeData second = Attribute("q:id");
        SourceRange range = RangeAt(20);
        second.Range = range;

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => parser.ResolveXmlAttributeNames([first, second], context))!;

        AssertNamespaceFailure(failure, reporter, XmlErrorCodes.NamespaceDuplicateAttribute, range);
        Assert.That(failure.Message, Does.Contain("{urn:shared}id"));
        Assert.That(first.Name!.NamespaceUri, Is.Null);
        Assert.That(second.Name!.NamespaceUri, Is.Null);
    }

    [Test]
    public void KeepsAttributesDistinctWhenLocalNamesOrNamespaceNamesDiffer()
    {
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces11, null);
        NamespaceContext context = Context(
            Binding("p", "urn:p"),
            Binding("q", "urn:q"));
        XmlAttributeData unprefixed = Attribute("id");
        XmlAttributeData first = Attribute("p:id");
        XmlAttributeData second = Attribute("q:id");
        XmlAttributeData otherLocal = Attribute("p:other");

        parser.ResolveXmlAttributeNames([unprefixed, first, second, otherLocal], context);

        Assert.That(unprefixed.Name!.NamespaceUri, Is.Null);
        Assert.That(first.Name!.NamespaceUri, Is.EqualTo("urn:p"));
        Assert.That(second.Name!.NamespaceUri, Is.EqualTo("urn:q"));
        Assert.That(otherLocal.Name!.LocalName, Is.EqualTo("other"));
    }

    [Test]
    public void DoesNotMutateEarlierAttributesWhenALaterPrefixIsUnbound()
    {
        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces11, null);
        NamespaceContext context = Context(Binding("p", "urn:p"));
        XmlAttributeData first = Attribute("p:first");
        XmlAttributeData second = Attribute("missing:second");

        Assert.Throws<XmlNamespaceException>(() => parser.ResolveXmlAttributeNames([first, second], context));

        Assert.That(first.Name!.Prefix, Is.Null);
        Assert.That(first.Name.LocalName, Is.Null);
        Assert.That(first.Name.NamespaceUri, Is.Null);
        Assert.That(first.Name.QualifiedName, Is.EqualTo("p:first"));
    }

    [Test]
    public void RejectsInvalidConstructionAndArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new QualifiedNameParser(XmlNamespaceHandling.Disabled, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new QualifiedNameParser((XmlNamespaceHandling)99, null));

        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces10, null);
        NamespaceContext context = Context();
        Assert.Throws<ArgumentNullException>(() => parser.ParseXmlQualifiedName(null!, null));
        Assert.Throws<ArgumentNullException>(() => parser.ResolveXmlElementName("name", null!, null));
        Assert.Throws<ArgumentNullException>(() => parser.ResolveXmlAttributeName("name", null!, null));
        Assert.Throws<ArgumentNullException>(() => parser.ResolveXmlAttributeNames(null!, context));
        Assert.Throws<ArgumentNullException>(() => parser.ResolveXmlAttributeNames([], null!));
        Assert.Throws<ArgumentException>(() => parser.ResolveXmlAttributeNames(new XmlAttributeData[] { null! }, context));
        Assert.Throws<ArgumentException>(() => parser.ResolveXmlAttributeNames([new XmlAttributeData()], context));
    }

    private static NamespaceContext Context(params XmlNamespaceBinding[] bindings)
    {
        return new NamespaceContext(null, bindings);
    }

    private static XmlNamespaceBinding Binding(string prefix, string namespaceUri)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        return binding;
    }

    private static XmlAttributeData Attribute(string qualifiedName)
    {
        XmlName name = new XmlName();
        name.QualifiedName = qualifiedName;
        XmlAttributeData attribute = new XmlAttributeData();
        attribute.Name = name;
        return attribute;
    }

    private static SourceRange RangeAt(long characterOffset)
    {
        SourceLocation location = new SourceLocation();
        location.CharacterOffset = characterOffset;
        SourceRange range = new SourceRange();
        range.Start = location;
        range.End = location;
        return range;
    }

    private static void AssertName(XmlName name, string? prefix, string localName, string? namespaceUri, string qualifiedName)
    {
        Assert.That(name.Prefix, Is.EqualTo(prefix));
        Assert.That(name.LocalName, Is.EqualTo(localName));
        Assert.That(name.NamespaceUri, Is.EqualTo(namespaceUri));
        Assert.That(name.QualifiedName, Is.EqualTo(qualifiedName));
    }

    private static void AssertNamespaceFailure(XmlNamespaceException failure, RecordingReporter reporter, string code, SourceRange range)
    {
        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(code));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Namespace));
        Assert.That(failure.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        Assert.That(failure.Diagnostic.Range, Is.SameAs(range));
        Assert.That(failure.Location, Is.SameAs(range.Start));
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
