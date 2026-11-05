using xml;

namespace xml.Tests;

[TestFixture]
public class NamespaceDeclarationProcessorTests
{
    [Test]
    public void ProcessesDefaultAndPrefixedDeclarationsIntoAChildContext()
    {
        NamespaceContext parent = new NamespaceContext(null, [Binding("parent", "urn:parent")]);
        XmlAttributeData defaultDeclaration = Attribute("xmlns", "urn:default");
        XmlAttributeData ordinary = Attribute("id", "42");
        XmlAttributeData prefixedDeclaration = Attribute("xmlns:p", "urn:p");
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces10, null);

        NamespaceDeclarationResult result = processor.ProcessXmlNamespaceDeclarations(parent, [defaultDeclaration, ordinary, prefixedDeclaration]);

        Assert.That(result.Context.CurrentXmlNamespaceDepth(), Is.EqualTo(1));
        Assert.That(result.Context.DefaultXmlNamespaceUri(), Is.EqualTo("urn:default"));
        Assert.That(result.Context.LookupXmlNamespaceUri("p"), Is.EqualTo("urn:p"));
        Assert.That(result.Context.LookupXmlNamespaceUri("parent"), Is.EqualTo("urn:parent"));
        Assert.That(parent.DefaultXmlNamespaceUri(), Is.Null);
        Assert.That(parent.LookupXmlNamespaceUri("p"), Is.Null);
        Assert.That(result.Declarations, Has.Length.EqualTo(2));
        Assert.That(result.Declarations[0].Prefix, Is.EqualTo(""));
        Assert.That(result.Declarations[1].Prefix, Is.EqualTo("p"));
        Assert.That(result.OrdinaryAttributes, Is.EqualTo(new XmlAttributeData[] { ordinary }));
        Assert.That(defaultDeclaration.IsNamespaceDeclaration, Is.True);
        Assert.That(defaultDeclaration.Name!.Prefix, Is.Null);
        Assert.That(defaultDeclaration.Name.LocalName, Is.EqualTo("xmlns"));
        Assert.That(defaultDeclaration.Name.NamespaceUri, Is.EqualTo(XmlNamespaces.Xmlns));
        Assert.That(prefixedDeclaration.IsNamespaceDeclaration, Is.True);
        Assert.That(prefixedDeclaration.Name!.Prefix, Is.EqualTo("xmlns"));
        Assert.That(prefixedDeclaration.Name.LocalName, Is.EqualTo("p"));
        Assert.That(prefixedDeclaration.Name.NamespaceUri, Is.EqualTo(XmlNamespaces.Xmlns));
        Assert.That(ordinary.IsNamespaceDeclaration, Is.False);
    }

    [Test]
    public void ClassifiesEmptyValuesAsUndeclarations()
    {
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces11, null);
        NamespaceContext parent = new NamespaceContext(null,
        [
            Binding("", "urn:default"),
            Binding("p", "urn:p")
        ]);

        NamespaceDeclarationResult result = processor.ProcessXmlNamespaceDeclarations(parent,
        [
            Attribute("xmlns", ""),
            Attribute("xmlns:p", "")
        ]);

        Assert.That(result.Declarations, Has.Length.EqualTo(2));
        Assert.That(result.Declarations[0].IsUndeclaration, Is.True);
        Assert.That(result.Declarations[1].IsUndeclaration, Is.True);
        Assert.That(result.Context.DefaultXmlNamespaceUri(), Is.Null);
        Assert.That(result.Context.LookupXmlNamespaceUri("p"), Is.Null);
    }

    [TestCase(XmlNamespaceHandling.Namespaces10)]
    [TestCase(XmlNamespaceHandling.Namespaces11)]
    public void AllowsTheRequiredXmlBinding(XmlNamespaceHandling handling)
    {
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(handling, null);

        NamespaceDeclarationResult result = processor.ProcessXmlNamespaceDeclarations(new NamespaceContext(null, []), [Attribute("xmlns:xml", XmlNamespaces.Xml)]);

        Assert.That(result.Declarations, Has.Length.EqualTo(1));
        Assert.That(result.Declarations[0].Prefix, Is.EqualTo("xml"));
        Assert.That(result.Context.LookupXmlNamespaceUri("xml"), Is.EqualTo(XmlNamespaces.Xml));
    }

    [TestCase(XmlNamespaceHandling.Namespaces10, "xmlns:xml", "urn:wrong")]
    [TestCase(XmlNamespaceHandling.Namespaces10, "xmlns:xml", "")]
    [TestCase(XmlNamespaceHandling.Namespaces10, "xmlns:p", XmlNamespaces.Xml)]
    [TestCase(XmlNamespaceHandling.Namespaces10, "xmlns", XmlNamespaces.Xml)]
    [TestCase(XmlNamespaceHandling.Namespaces10, "xmlns:xmlns", "urn:value")]
    [TestCase(XmlNamespaceHandling.Namespaces10, "xmlns:p", XmlNamespaces.Xmlns)]
    [TestCase(XmlNamespaceHandling.Namespaces10, "xmlns", XmlNamespaces.Xmlns)]
    [TestCase(XmlNamespaceHandling.Namespaces11, "xmlns:xml", "urn:wrong")]
    [TestCase(XmlNamespaceHandling.Namespaces11, "xmlns:xml", "")]
    [TestCase(XmlNamespaceHandling.Namespaces11, "xmlns:p", XmlNamespaces.Xml)]
    [TestCase(XmlNamespaceHandling.Namespaces11, "xmlns", XmlNamespaces.Xml)]
    [TestCase(XmlNamespaceHandling.Namespaces11, "xmlns:xmlns", "urn:value")]
    [TestCase(XmlNamespaceHandling.Namespaces11, "xmlns:p", XmlNamespaces.Xmlns)]
    [TestCase(XmlNamespaceHandling.Namespaces11, "xmlns", XmlNamespaces.Xmlns)]
    public void RejectsEveryReservedPrefixAndNamespaceNameViolation(XmlNamespaceHandling handling, string qualifiedName, string namespaceUri)
    {
        RecordingReporter reporter = new RecordingReporter();
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(handling, reporter);
        SourceRange range = RangeAt(7);
        XmlAttributeData attribute = Attribute(qualifiedName, namespaceUri);
        attribute.Range = range;

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => processor.ProcessXmlNamespaceDeclarations(new NamespaceContext(null, []), [attribute]))!;

        AssertNamespaceFailure(failure, reporter, XmlErrorCodes.NamespaceReservedBinding, range);
        Assert.That(attribute.IsNamespaceDeclaration, Is.False);
    }

    [TestCase("xmlns:")]
    [TestCase("xmlns:1bad")]
    [TestCase("xmlns:p:q")]
    public void RejectsMalformedPrefixedDeclarationNames(string qualifiedName)
    {
        RecordingReporter reporter = new RecordingReporter();
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces11, reporter);
        SourceRange range = RangeAt(3);
        XmlAttributeData attribute = Attribute(qualifiedName, "urn:value");
        attribute.Range = range;

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => processor.ProcessXmlNamespaceDeclarations(new NamespaceContext(null, []), [attribute]))!;

        AssertNamespaceFailure(failure, reporter, XmlErrorCodes.NamespaceReservedBinding, range);
    }

    [Test]
    public void LeavesSimilarCaseSensitiveAttributeNamesOrdinary()
    {
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces11, null);
        XmlAttributeData first = Attribute("xmlnsish", "one");
        XmlAttributeData second = Attribute("p:xmlns", "two");
        XmlAttributeData third = Attribute("XMLNS:p", "three");

        NamespaceDeclarationResult result = processor.ProcessXmlNamespaceDeclarations(new NamespaceContext(null, []), [first, second, third]);

        Assert.That(result.Declarations, Is.Empty);
        Assert.That(result.OrdinaryAttributes, Is.EqualTo(new XmlAttributeData[] { first, second, third }));
        Assert.That(result.Context.CurrentXmlNamespaceDepth(), Is.EqualTo(1));
    }

    [TestCase(XmlNamespaceHandling.Namespaces10)]
    [TestCase(XmlNamespaceHandling.Namespaces11)]
    public void AllowsNonFatalXmlLikePrefixesAndRelativeNamespaceNames(XmlNamespaceHandling handling)
    {
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(handling, null);

        NamespaceDeclarationResult result = processor.ProcessXmlNamespaceDeclarations(new NamespaceContext(null, []), [Attribute("xmlns:XMLtools", "relative/path")]);

        Assert.That(result.Context.LookupXmlNamespaceUri("XMLtools"), Is.EqualTo("relative/path"));
    }

    [Test]
    public void RejectsDuplicateDeclarationsWithoutPartiallyMarkingAttributes()
    {
        RecordingReporter reporter = new RecordingReporter();
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces11, reporter);
        XmlAttributeData first = Attribute("xmlns:p", "urn:first");
        XmlAttributeData second = Attribute("xmlns:p", "urn:second");

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => processor.ProcessXmlNamespaceDeclarations(new NamespaceContext(null, []), [first, second]))!;

        AssertNamespaceFailure(failure, reporter, XmlErrorCodes.NamespaceDuplicateAttribute, null);
        Assert.That(first.IsNamespaceDeclaration, Is.False);
        Assert.That(second.IsNamespaceDeclaration, Is.False);
    }

    [Test]
    public void AcceptsExpandedNameInputWithoutAQualifiedName()
    {
        XmlName name = new XmlName();
        name.Prefix = "xmlns";
        name.LocalName = "p";
        XmlAttributeData declaration = new XmlAttributeData();
        declaration.Name = name;
        declaration.Value = "urn:p";
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces11, null);

        NamespaceDeclarationResult result = processor.ProcessXmlNamespaceDeclarations(new NamespaceContext(null, []), [declaration]);

        Assert.That(result.Context.LookupXmlNamespaceUri("p"), Is.EqualTo("urn:p"));
        Assert.That(declaration.Name.NamespaceUri, Is.EqualTo(XmlNamespaces.Xmlns));
    }

    [Test]
    public void RejectsInvalidConstructionAndAttributeBags()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NamespaceDeclarationProcessor(XmlNamespaceHandling.Disabled, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => new NamespaceDeclarationProcessor((XmlNamespaceHandling)99, null));

        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces10, null);
        NamespaceContext context = new NamespaceContext(null, []);
        Assert.Throws<ArgumentNullException>(() => processor.ProcessXmlNamespaceDeclarations(null!, []));
        Assert.Throws<ArgumentNullException>(() => processor.ProcessXmlNamespaceDeclarations(context, null!));
        Assert.Throws<ArgumentException>(() => processor.ProcessXmlNamespaceDeclarations(context, new XmlAttributeData[] { null! }));
        Assert.Throws<ArgumentException>(() => processor.ProcessXmlNamespaceDeclarations(context, [new XmlAttributeData()]));

        XmlAttributeData missingValue = Attribute("xmlns:p", "urn:p");
        missingValue.Value = null;
        Assert.Throws<ArgumentException>(() => processor.ProcessXmlNamespaceDeclarations(context, [missingValue]));
    }

    private static XmlAttributeData Attribute(string qualifiedName, string value)
    {
        XmlName name = new XmlName();
        name.QualifiedName = qualifiedName;
        XmlAttributeData attribute = new XmlAttributeData();
        attribute.Name = name;
        attribute.Value = value;
        return attribute;
    }

    private static XmlNamespaceBinding Binding(string prefix, string namespaceUri)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        return binding;
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

    private static void AssertNamespaceFailure(XmlNamespaceException failure, RecordingReporter reporter, string code, SourceRange? range)
    {
        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(code));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Namespace));
        Assert.That(failure.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        Assert.That(failure.Diagnostic.Range, Is.SameAs(range));
        Assert.That(failure.Location, Is.SameAs(range == null ? null : range.Start));
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
