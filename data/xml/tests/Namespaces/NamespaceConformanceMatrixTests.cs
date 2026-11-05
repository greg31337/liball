using xml;

namespace xml.Tests;

[TestFixture]
public class NamespaceConformanceMatrixTests
{
    [Test]
    public void ParserRestoresShadowedPrefixAndDefaultBindingsForSiblings()
    {
        string text = "<root xmlns='urn:outer' xmlns:p='urn:outer-p'><branch xmlns='urn:inner' xmlns:p='urn:inner-p'><p:item/><plain code='x'/></branch><p:item/></root>";

        XmlDocument document = ParseDocument(text, Options(XmlNamespaceHandling.Namespaces10), null);

        XmlNode root = document.DocumentElement!;
        XmlNode branch = root.Children![0];
        XmlNode innerItem = branch.Children![0];
        XmlNode plain = branch.Children[1];
        XmlNode outerItem = root.Children[1];
        AssertName(root.Name!, null, "root", "urn:outer");
        AssertName(branch.Name!, null, "branch", "urn:inner");
        AssertName(innerItem.Name!, "p", "item", "urn:inner-p");
        AssertName(plain.Name!, null, "plain", "urn:inner");
        AssertName(plain.Attributes![0].Name!, null, "code", null);
        AssertName(outerItem.Name!, "p", "item", "urn:outer-p");
    }

    [TestCase(XmlNamespaceHandling.Namespaces10)]
    [TestCase(XmlNamespaceHandling.Namespaces11)]
    public void DefaultUndeclarationAppliesOnlyWithinItsElementScope(XmlNamespaceHandling handling)
    {
        XmlDocument document = ParseDocument("<root xmlns='urn:outer'><branch xmlns=''><leaf/></branch><leaf/></root>", Options(handling), null);

        XmlNode root = document.DocumentElement!;
        XmlNode branch = root.Children![0];
        AssertName(branch.Name!, null, "branch", null);
        AssertName(branch.Children![0].Name!, null, "leaf", null);
        AssertName(root.Children[1].Name!, null, "leaf", "urn:outer");
        Assert.That(branch.NamespaceBindings![0].IsUndeclaration, Is.True);
    }

    [Test]
    public void NamespaceNamesCompareExactlyWithoutUriOrCaseNormalization()
    {
        XmlDocument document = ParseDocument("<root xmlns:p='urn:value' xmlns:q='URN:value' p:id='1' q:id='2'/>", Options(XmlNamespaceHandling.Namespaces11), null);

        XmlNode[] attributes = document.DocumentElement!.Attributes!;
        Assert.That(attributes, Has.Length.EqualTo(2));
        AssertName(attributes[0].Name!, "p", "id", "urn:value");
        AssertName(attributes[1].Name!, "q", "id", "URN:value");
    }

    [Test]
    public void ImplicitXmlBindingResolvesWithoutADeclarationAndDefaultNamespacesDoNotApplyToAttributes()
    {
        XmlDocument document = ParseDocument("<root xmlns='urn:elements' xml:lang='en' plain='value'/>", Options(XmlNamespaceHandling.Namespaces10), null);

        XmlNode root = document.DocumentElement!;
        AssertName(root.Name!, null, "root", "urn:elements");
        AssertName(root.Attributes![0].Name!, "xml", "lang", XmlNamespaces.Xml);
        AssertName(root.Attributes[1].Name!, null, "plain", null);
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(1));
        Assert.That(root.NamespaceBindings![0].Prefix, Is.EqualTo(""));
    }

    [TestCase("<root xmlns:xml='urn:wrong'/>")]
    [TestCase("<root xmlns:p='http://www.w3.org/XML/1998/namespace'/>")]
    [TestCase("<root xmlns='http://www.w3.org/XML/1998/namespace'/>")]
    [TestCase("<root xmlns:xmlns='urn:value'/>")]
    [TestCase("<root xmlns:p='http://www.w3.org/2000/xmlns/'/>")]
    [TestCase("<root xmlns='http://www.w3.org/2000/xmlns/'/>")]
    [TestCase("<xmlns:root/>")]
    public void PublicParserRejectsEveryReservedBindingConstraint(string text)
    {
        RecordingReporter reporter = new RecordingReporter();

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => ParseDocument(text, Options(XmlNamespaceHandling.Namespaces11), reporter))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.NamespaceReservedBinding));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Namespace));
        Assert.That(failure.Location, Is.Not.Null);
    }

    [TestCase(XmlNamespaceHandling.Namespaces10)]
    [TestCase(XmlNamespaceHandling.Namespaces11)]
    public void PublicParserAcceptsTheRequiredXmlBindingAndNonFatalXmlLikePrefixes(XmlNamespaceHandling handling)
    {
        string text = "<XMLtools:root xmlns:xml='http://www.w3.org/XML/1998/namespace' xmlns:XMLtools='relative/path' xml:lang='en'/>";

        XmlDocument document = ParseDocument(text, Options(handling), null);

        AssertName(document.DocumentElement!.Name!, "XMLtools", "root", "relative/path");
        AssertName(document.DocumentElement.Attributes![0].Name!, "xml", "lang", XmlNamespaces.Xml);
    }

    [Test]
    public void DisabledNamespaceModeLeavesNamespaceLikeSyntaxLexical()
    {
        XmlParserOptions options = Options(XmlNamespaceHandling.Disabled);

        XmlDocument document = ParseDocument("<p:root xmlns:xml='urn:wrong' p:id='1'/>", options, null);

        XmlNode root = document.DocumentElement!;
        Assert.That(root.Name!.QualifiedName, Is.EqualTo("p:root"));
        Assert.That(root.Name.NamespaceUri, Is.Null);
        Assert.That(root.Attributes, Has.Length.EqualTo(2));
        Assert.That(root.Attributes![0].Name!.QualifiedName, Is.EqualTo("xmlns:xml"));
        Assert.That(root.NamespaceBindings, Is.Null);
    }

    [Test]
    public void FragmentOverridesInheritedBindingsWithoutLeakingAcrossTopLevelItems()
    {
        XmlParserOptions options = Options(XmlNamespaceHandling.Namespaces11);
        options.FragmentNamespaceBindings =
        [
            Binding("", "urn:inherited-default", false),
            Binding("p", "urn:inherited-p", false)
        ];

        XmlDocument document = new XmlParser().ParseXmlFragment(Input("<p:first/><middle xmlns:p='urn:local'><p:item/></middle><p:last/>"), options, null, null, null);

        XmlNode[] children = document.DocumentNode!.Children!;
        AssertName(children[0].Name!, "p", "first", "urn:inherited-p");
        AssertName(children[1].Name!, null, "middle", "urn:inherited-default");
        AssertName(children[1].Children![0].Name!, "p", "item", "urn:local");
        AssertName(children[2].Name!, "p", "last", "urn:inherited-p");
    }

    [Test]
    public void FragmentRejectsDuplicateAndReservedInheritedBindings()
    {
        XmlParserOptions duplicate = Options(XmlNamespaceHandling.Namespaces11);
        duplicate.FragmentNamespaceBindings = [Binding("p", "urn:first", false), Binding("p", "urn:second", false)];
        Assert.Throws<ArgumentException>(() => new XmlParser().ParseXmlFragment(Input("<root/>"), duplicate, null, null, null));

        XmlParserOptions reserved = Options(XmlNamespaceHandling.Namespaces11);
        reserved.FragmentNamespaceBindings = [Binding("xml", "urn:wrong", false)];
        Assert.Throws<XmlNamespaceException>(() => new XmlParser().ParseXmlFragment(Input("<root/>"), reserved, null, null, null));

        XmlParserOptions undeclaration = Options(XmlNamespaceHandling.Namespaces10);
        undeclaration.FragmentNamespaceBindings = [Binding("p", "", true)];
        Assert.Throws<XmlNamespaceException>(() => new XmlParser().ParseXmlFragment(Input("<root/>"), undeclaration, null, null, null));
    }

    [Test]
    public void NamespaceEventFailureIsAtomicAndLeavesTheRootScopeUsable()
    {
        XmlParserOptions options = Options(XmlNamespaceHandling.Namespaces11);
        ParserEventQueue queue = new ParserEventQueue(options);

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => queue.EnqueueXmlElementTag(ParseTag("<root xmlns:p='urn:p' q:id='1'/>")))!;
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.NamespaceUnboundPrefix));
        Assert.That(queue.CountQueuedXmlEvents(), Is.Zero);

        queue.EnqueueXmlElementTag(ParseTag("<root/>"));
        XmlEvent start = queue.DequeueXmlEvent()!;
        XmlEvent end = queue.DequeueXmlEvent()!;
        Assert.That(start.Kind, Is.EqualTo(XmlEventKind.StartElement));
        Assert.That(end.Kind, Is.EqualTo(XmlEventKind.EndElement));
        AssertName(start.Name!, null, "root", null);
    }

    [TestCase("xml")]
    [TestCase("xmlns")]
    [TestCase("1invalid")]
    public void FixupReplacesReservedOrInvalidCallerPrefixes(string prefix)
    {
        XmlNode element = Element(Name(prefix, "root", "urn:element"));

        Fixup(element, Context());

        AssertName(element.Name!, "ns1", "root", "urn:element");
        AssertBinding(element.NamespaceBindings![0], "ns1", "urn:element", false);
    }

    [Test]
    public void FixupReusesCompatibleBindingsBeforeGeneratingNewPrefixes()
    {
        XmlNode element = Element(Name(null, "root", null));
        element.NamespaceBindings =
        [
            Binding("p", "urn:target", false),
            Binding("q", "urn:other", false)
        ];
        element.Attributes = [Attribute(Name(null, "id", "urn:target"))];

        Fixup(element, Context());

        AssertName(element.Attributes![0].Name!, "p", "id", "urn:target");
        Assert.That(element.NamespaceBindings, Has.Length.EqualTo(2));
    }

    [Test]
    public void FixupRepairsTwoAttributesRequestingOnePrefixForDifferentNamespaces()
    {
        XmlNode element = Element(Name(null, "root", null));
        element.Attributes =
        [
            Attribute(Name("p", "first", "urn:first")),
            Attribute(Name("p", "second", "urn:second"))
        ];

        Fixup(element, Context());

        AssertName(element.Attributes![0].Name!, "p", "first", "urn:first");
        AssertName(element.Attributes[1].Name!, "ns1", "second", "urn:second");
        AssertBinding(element.NamespaceBindings![0], "p", "urn:first", false);
        AssertBinding(element.NamespaceBindings[1], "ns1", "urn:second", false);
    }

    [Test]
    public void FixupClearsPrefixesFromNoNamespaceNamesAndUndeclaresTheDefault()
    {
        XmlNode element = Element(Name("p", "root", null));
        element.Attributes = [Attribute(Name("q", "id", null))];

        Fixup(element, Context(Binding("", "urn:default", false), Binding("p", "urn:p", false), Binding("q", "urn:q", false)));

        AssertName(element.Name!, null, "root", null);
        AssertName(element.Attributes![0].Name!, null, "id", null);
        AssertBinding(element.NamespaceBindings![0], "", "", true);
    }

    [Test]
    public void FixupUsesACompatiblePrefixWhenTheLocalDefaultBindingConflicts()
    {
        XmlNode element = Element(Name(null, "root", "urn:target"));
        element.NamespaceBindings =
        [
            Binding("", "urn:other", false),
            Binding("p", "urn:target", false)
        ];

        Fixup(element, Context());

        AssertName(element.Name!, "p", "root", "urn:target");
        Assert.That(element.NamespaceBindings, Has.Length.EqualTo(2));
    }

    private static XmlDocument ParseDocument(string text, XmlParserOptions options, IXmlErrorReporter? reporter)
    {
        return new XmlParser().ParseXmlDocument(Input(text), options, null, reporter, null);
    }

    private static XmlInput Input(string text)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "memory.xml";
        return input;
    }

    private static XmlParserOptions Options(XmlNamespaceHandling handling)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = handling;
        options.DtdProcessing = XmlDtdProcessing.Prohibit;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        options.PreservePrefixes = true;
        return options;
    }

    private static XmlElementTag ParseTag(string text)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        return new XmlElementTagParser(scanner, true, null).ParseNextXmlElementTag();
    }

    private static void Fixup(XmlNode node, NamespaceContext context)
    {
        new NamespaceFixup(XmlNamespaceHandling.Namespaces11, null).FixupXmlNamespaceSubtree(node, context);
    }

    private static NamespaceContext Context(params XmlNamespaceBinding[] bindings)
    {
        return new NamespaceContext(null, bindings);
    }

    private static XmlNode Element(XmlName name)
    {
        XmlNode node = new XmlNode();
        node.Kind = XmlNodeKind.Element;
        node.Name = name;
        node.Attributes = [];
        node.Children = [];
        node.NamespaceBindings = [];
        return node;
    }

    private static XmlNode Attribute(XmlName name)
    {
        XmlNode node = new XmlNode();
        node.Kind = XmlNodeKind.Attribute;
        node.Name = name;
        return node;
    }

    private static XmlName Name(string? prefix, string localName, string? namespaceUri)
    {
        XmlName name = new XmlName();
        name.Prefix = prefix;
        name.LocalName = localName;
        name.NamespaceUri = namespaceUri;
        return name;
    }

    private static XmlNamespaceBinding Binding(string prefix, string namespaceUri, bool undeclaration)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        binding.IsUndeclaration = undeclaration;
        return binding;
    }

    private static void AssertName(XmlName name, string? prefix, string localName, string? namespaceUri)
    {
        Assert.That(name.Prefix, Is.EqualTo(prefix));
        Assert.That(name.LocalName, Is.EqualTo(localName));
        Assert.That(name.NamespaceUri, Is.EqualTo(namespaceUri));
    }

    private static void AssertBinding(XmlNamespaceBinding binding, string prefix, string namespaceUri, bool undeclaration)
    {
        Assert.That(binding.Prefix, Is.EqualTo(prefix));
        Assert.That(binding.NamespaceUri, Is.EqualTo(namespaceUri));
        Assert.That(binding.IsUndeclaration, Is.EqualTo(undeclaration));
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
