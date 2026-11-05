using xml;

namespace xml.Tests;

[TestFixture]
public class NamespaceIntegrationTests
{
    [Test]
    public void ParserResolvesNamesAndScopesDeclarationsAcrossTheTree()
    {
        XmlParserOptions options = NamespaceOptions(XmlNamespaceHandling.Namespaces10, true, false);

        XmlDocument document = ParseDocument("<p:root xmlns:p='urn:p' p:id='1' plain='2'><child xmlns='urn:default'><p:item/></child></p:root>", options, null);

        XmlNode root = document.DocumentElement!;
        AssertName(root.Name!, "p", "root", "urn:p", "p:root");
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(1));
        AssertBinding(root.NamespaceBindings![0], "p", "urn:p", false);
        Assert.That(root.Attributes, Has.Length.EqualTo(2));
        AssertName(root.Attributes![0].Name!, "p", "id", "urn:p", "p:id");
        AssertName(root.Attributes[1].Name!, null, "plain", null, "plain");

        XmlNode child = root.Children![0];
        AssertName(child.Name!, null, "child", "urn:default", "child");
        AssertBinding(child.NamespaceBindings![0], "", "urn:default", false);
        XmlNode item = child.Children![0];
        AssertName(item.Name!, "p", "item", "urn:p", "p:item");
        Assert.That(item.NamespaceBindings, Is.Empty);
    }

    [Test]
    public void ParserRetainsDeclarationAttributesAndCanDiscardLexicalPrefixes()
    {
        XmlParserOptions options = NamespaceOptions(XmlNamespaceHandling.Namespaces11, false, true);

        XmlDocument document = ParseDocument("<p:root xmlns:p='urn:p' xmlns='urn:default' p:id='1'/>", options, null);

        XmlNode root = document.DocumentElement!;
        AssertName(root.Name!, null, "root", "urn:p", "root");
        Assert.That(root.Attributes, Has.Length.EqualTo(3));
        AssertName(root.Attributes![0].Name!, null, "p", XmlNamespaces.Xmlns, "p");
        AssertName(root.Attributes[1].Name!, null, "xmlns", XmlNamespaces.Xmlns, "xmlns");
        AssertName(root.Attributes[2].Name!, null, "id", "urn:p", "id");
        Assert.That(root.NamespaceBindings, Has.Length.EqualTo(2));
    }

    [Test]
    public void ParserUsesNormalizedDeclarationValues()
    {
        XmlParserOptions options = NamespaceOptions(XmlNamespaceHandling.Namespaces10, true, false);

        XmlDocument document = ParseDocument("<p:root xmlns:p='urn:a&amp;b'/>", options, null);

        AssertName(document.DocumentElement!.Name!, "p", "root", "urn:a&b", "p:root");
        AssertBinding(document.DocumentElement.NamespaceBindings![0], "p", "urn:a&b", false);
    }

    [Test]
    public void FragmentElementsResolveAgainstCallerSuppliedNamespaceBindings()
    {
        XmlParserOptions options = NamespaceOptions(XmlNamespaceHandling.Namespaces11, true, false);
        XmlNamespaceBinding inherited = Binding("p", "urn:inherited", false);
        options.FragmentNamespaceBindings = [inherited];

        XmlDocument document = new XmlParser().ParseXmlFragment(Input("<p:first/><p:second/>"), options, null, null, null);

        XmlNode first = document.DocumentNode!.Children![0];
        XmlNode second = document.DocumentNode.Children[1];
        AssertName(first.Name!, "p", "first", "urn:inherited", "p:first");
        AssertName(second.Name!, "p", "second", "urn:inherited", "p:second");
        Assert.That(first.NamespaceBindings, Is.Empty);
        Assert.That(second.NamespaceBindings, Is.Empty);
        AssertBinding(inherited, "p", "urn:inherited", false);
    }

    [Test]
    public void ParserRejectsUnboundPrefixesAndExpandedAttributeDuplicates()
    {
        XmlParserOptions options = NamespaceOptions(XmlNamespaceHandling.Namespaces10, true, false);
        RecordingReporter reporter = new RecordingReporter();

        XmlNamespaceException unbound = Assert.Throws<XmlNamespaceException>(() => ParseDocument("<p:root/>", options, reporter))!;
        Assert.That(unbound.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(unbound.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.NamespaceUnboundPrefix));

        XmlNamespaceException duplicate = Assert.Throws<XmlNamespaceException>(() => ParseDocument("<root xmlns:p='urn:same' xmlns:q='urn:same' p:id='1' q:id='2'/>", options, null))!;
        Assert.That(duplicate.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.NamespaceDuplicateAttribute));
    }

    [Test]
    public void ParserAppliesVersionSpecificPrefixUndeclarationRules()
    {
        string text = "<root xmlns:p='urn:p'><child xmlns:p=''/></root>";

        Assert.Throws<XmlNamespaceException>(() => ParseDocument(text, NamespaceOptions(XmlNamespaceHandling.Namespaces10, true, false), null));
        XmlDocument document = ParseDocument(text, NamespaceOptions(XmlNamespaceHandling.Namespaces11, true, false), null);

        XmlNamespaceBinding undeclaration = document.DocumentElement!.Children![0].NamespaceBindings![0];
        AssertBinding(undeclaration, "p", "", true);
    }

    [Test]
    public void EventQueueResolvesNamesAndSurroundsEmptyElementsWithNamespaceEvents()
    {
        XmlParserOptions options = NamespaceOptions(XmlNamespaceHandling.Namespaces11, true, false);
        ParserEventQueue queue = new ParserEventQueue(options);
        XmlElementTag tag = ParseTag("<p:root xmlns:p='urn:p&amp;x' p:id='a&amp;b'/>");

        queue.EnqueueXmlElementTag(tag);
        XmlEvent[] events = Drain(queue);

        Assert.That(EventKinds(events), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.StartNamespace,
            XmlEventKind.StartElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndNamespace
        }));
        AssertName(events[1].Name!, "p", "root", "urn:p&x", "p:root");
        Assert.That(events[1].Attributes, Has.Length.EqualTo(1));
        AssertName(events[1].Attributes![0].Name!, "p", "id", "urn:p&x", "p:id");
        Assert.That(events[1].Attributes[0].Value, Is.EqualTo("a&b"));
        Assert.That(events[1].Attributes[0].RawValue, Is.EqualTo("a&amp;b"));
        AssertName(events[2].Name!, "p", "root", "urn:p&x", "p:root");
        AssertBinding(events[0].NamespaceBindings![0], "p", "urn:p&x", false);
        AssertBinding(events[3].NamespaceBindings![0], "p", "urn:p&x", false);
    }

    [Test]
    public void EventQueueMaintainsNestedDefaultScopesAndRetainsDeclarationsOnRequest()
    {
        XmlParserOptions options = NamespaceOptions(XmlNamespaceHandling.Namespaces11, false, true);
        ParserEventQueue queue = new ParserEventQueue(options);
        XmlScanner scanner = new XmlScanner("<outer xmlns='urn:outer'><inner xmlns='urn:inner'/></outer>".AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        XmlElementTagParser parser = new XmlElementTagParser(scanner, true, null);

        queue.EnqueueXmlElementTag(parser.ParseNextXmlElementTag());
        queue.EnqueueXmlElementTag(parser.ParseNextXmlElementTag());
        queue.EnqueueXmlElementTag(parser.ParseNextXmlElementTag());
        XmlEvent[] events = Drain(queue);

        Assert.That(EventKinds(events), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.StartNamespace,
            XmlEventKind.StartElement,
            XmlEventKind.StartNamespace,
            XmlEventKind.StartElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndNamespace,
            XmlEventKind.EndElement,
            XmlEventKind.EndNamespace
        }));
        AssertName(events[1].Name!, null, "outer", "urn:outer", "outer");
        AssertName(events[3].Name!, null, "inner", "urn:inner", "inner");
        AssertName(events[4].Name!, null, "inner", "urn:inner", "inner");
        AssertName(events[6].Name!, null, "outer", "urn:outer", "outer");
        Assert.That(events[1].Attributes, Has.Length.EqualTo(1));
        Assert.That(events[1].Attributes![0].IsNamespaceDeclaration, Is.True);
        AssertName(events[1].Attributes[0].Name!, null, "xmlns", XmlNamespaces.Xmlns, "xmlns");
    }

    [Test]
    public void EventQueueUsesInheritedFragmentBindingsAndResetsItsScopeState()
    {
        XmlParserOptions options = NamespaceOptions(XmlNamespaceHandling.Namespaces10, true, false);
        options.ParseFragment = true;
        options.FragmentNamespaceBindings = [Binding("p", "urn:fragment", false)];
        ParserEventQueue queue = new ParserEventQueue(options);

        queue.EnqueueXmlElementTag(ParseTag("<p:item/>"));
        XmlEvent[] first = Drain(queue);
        AssertName(first[0].Name!, "p", "item", "urn:fragment", "p:item");

        queue.ClearParserEvents();
        queue.EnqueueXmlElementTag(ParseTag("<p:again/>"));
        XmlEvent[] second = Drain(queue);
        AssertName(second[0].Name!, "p", "again", "urn:fragment", "p:again");
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
        return input;
    }

    private static XmlParserOptions NamespaceOptions(XmlNamespaceHandling handling, bool preservePrefixes, bool retainDeclarations)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = handling;
        options.DtdProcessing = XmlDtdProcessing.Prohibit;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        options.PreservePrefixes = preservePrefixes;
        options.RetainNamespaceDeclarations = retainDeclarations;
        return options;
    }

    private static XmlElementTag ParseTag(string text)
    {
        XmlScanner scanner = new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
        return new XmlElementTagParser(scanner, true, null).ParseNextXmlElementTag();
    }

    private static XmlNamespaceBinding Binding(string prefix, string namespaceUri, bool undeclaration)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        binding.IsUndeclaration = undeclaration;
        return binding;
    }

    private static void AssertName(XmlName name, string? prefix, string localName, string? namespaceUri, string qualifiedName)
    {
        Assert.That(name.Prefix, Is.EqualTo(prefix));
        Assert.That(name.LocalName, Is.EqualTo(localName));
        Assert.That(name.NamespaceUri, Is.EqualTo(namespaceUri));
        Assert.That(name.QualifiedName, Is.EqualTo(qualifiedName));
    }

    private static void AssertBinding(XmlNamespaceBinding binding, string prefix, string namespaceUri, bool undeclaration)
    {
        Assert.That(binding.Prefix, Is.EqualTo(prefix));
        Assert.That(binding.NamespaceUri, Is.EqualTo(namespaceUri));
        Assert.That(binding.IsUndeclaration, Is.EqualTo(undeclaration));
    }

    private static XmlEventKind[] EventKinds(XmlEvent[] events)
    {
        XmlEventKind[] kinds = new XmlEventKind[events.Length];
        int index = 0;
        while (index < events.Length)
        {
            kinds[index] = events[index].Kind;
            index = index + 1;
        }

        return kinds;
    }

    private static XmlEvent[] Drain(ParserEventQueue queue)
    {
        List<XmlEvent> events = new List<XmlEvent>();
        XmlEvent? item = queue.DequeueXmlEvent();
        while (item != null)
        {
            events.Add(item);
            item = queue.DequeueXmlEvent();
        }

        return events.ToArray();
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
