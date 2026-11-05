using xml;

namespace xml.Tests;

[TestFixture]
public class NamespaceVersionAndEventTests
{
    [Test]
    public void Namespaces10RejectsPrefixedUndeclarations()
    {
        RecordingReporter reporter = new RecordingReporter();
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces10, reporter);
        XmlAttributeData declaration = Attribute("xmlns:p", "");
        SourceRange range = RangeAt(6);
        declaration.Range = range;

        XmlNamespaceException failure = Assert.Throws<XmlNamespaceException>(() => processor.ProcessXmlNamespaceDeclarations(Context(Binding("p", "urn:p", false)), [declaration]))!;

        Assert.That(failure.Diagnostic, Is.SameAs(reporter.Diagnostic));
        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.NamespaceReservedBinding));
        Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Namespace));
        Assert.That(failure.Diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        Assert.That(failure.Diagnostic.Range, Is.SameAs(range));
        Assert.That(failure.Location, Is.SameAs(range.Start));
        Assert.That(declaration.IsNamespaceDeclaration, Is.False);
    }

    [Test]
    public void Namespaces11AllowsPrefixedUndeclarations()
    {
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(XmlNamespaceHandling.Namespaces11, null);
        NamespaceContext parent = Context(Binding("p", "urn:p", false));

        NamespaceDeclarationResult result = processor.ProcessXmlNamespaceDeclarations(parent, [Attribute("xmlns:p", "")]);

        Assert.That(result.Declarations, Has.Length.EqualTo(1));
        Assert.That(result.Declarations[0].Prefix, Is.EqualTo("p"));
        Assert.That(result.Declarations[0].NamespaceUri, Is.EqualTo(""));
        Assert.That(result.Declarations[0].IsUndeclaration, Is.True);
        Assert.That(result.Context.LookupXmlNamespaceUri("p"), Is.Null);
        Assert.That(parent.LookupXmlNamespaceUri("p"), Is.EqualTo("urn:p"));
    }

    [TestCase(XmlNamespaceHandling.Namespaces10)]
    [TestCase(XmlNamespaceHandling.Namespaces11)]
    public void BothVersionsAllowDefaultNamespaceUndeclarations(XmlNamespaceHandling handling)
    {
        NamespaceDeclarationProcessor processor = new NamespaceDeclarationProcessor(handling, null);

        NamespaceDeclarationResult result = processor.ProcessXmlNamespaceDeclarations(Context(Binding("", "urn:default", false)), [Attribute("xmlns", "")]);

        Assert.That(result.Declarations[0].Prefix, Is.EqualTo(""));
        Assert.That(result.Declarations[0].IsUndeclaration, Is.True);
        Assert.That(result.Context.DefaultXmlNamespaceUri(), Is.Null);
    }

    [TestCase(XmlNamespaceHandling.Namespaces10)]
    [TestCase(XmlNamespaceHandling.Namespaces11)]
    public void EmitsNamespaceStartsBeforeTheElementAndEndsAfterItInReverseOrder(XmlNamespaceHandling handling)
    {
        ParserEventQueue queue = Queue(handling);
        XmlNamespaceBinding first = Binding("p", "urn:p", false);
        XmlNamespaceBinding second = Binding("q", "urn:q", false);
        XmlEvent start = ElementEvent(XmlEventKind.StartElement, 2);
        XmlEvent end = ElementEvent(XmlEventKind.EndElement, 2);

        queue.EnqueueXmlNamespaceStartEvents(start, [first, second]);
        queue.EnqueueXmlNamespaceEndEvents(end, [first, second]);
        XmlEvent[] events = Drain(queue);

        Assert.That(EventKinds(events), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.StartNamespace,
            XmlEventKind.StartNamespace,
            XmlEventKind.StartElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndNamespace,
            XmlEventKind.EndNamespace
        }));
        Assert.That(EventPrefix(events[0]), Is.EqualTo("p"));
        Assert.That(EventPrefix(events[1]), Is.EqualTo("q"));
        Assert.That(EventPrefix(events[4]), Is.EqualTo("q"));
        Assert.That(EventPrefix(events[5]), Is.EqualTo("p"));
        Assert.That(events[0].Value, Is.EqualTo("urn:p"));
        Assert.That(events[1].Value, Is.EqualTo("urn:q"));
        Assert.That(events[2], Is.SameAs(start));
        Assert.That(events[3], Is.SameAs(end));
        Assert.That(events[2].NamespaceBindings, Has.Length.EqualTo(2));
        Assert.That(events, Has.All.Matches<XmlEvent>(item => item.Depth == 2));
    }

    [Test]
    public void EmitsNamespaces11PrefixedUndeclarationBoundaries()
    {
        ParserEventQueue queue = Queue(XmlNamespaceHandling.Namespaces11);
        XmlNamespaceBinding undeclaration = Binding("p", "", true);

        queue.EnqueueXmlNamespaceStartEvents(ElementEvent(XmlEventKind.StartElement, 1), [undeclaration]);
        queue.EnqueueXmlNamespaceEndEvents(ElementEvent(XmlEventKind.EndElement, 1), [undeclaration]);
        XmlEvent[] events = Drain(queue);

        Assert.That(EventKinds(events), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.StartNamespace,
            XmlEventKind.StartElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndNamespace
        }));
        Assert.That(EventPrefix(events[0]), Is.EqualTo("p"));
        Assert.That(events[0].Value, Is.EqualTo(""));
        Assert.That(events[0].NamespaceBindings![0].IsUndeclaration, Is.True);
        Assert.That(events[3].NamespaceBindings![0].IsUndeclaration, Is.True);
    }

    [Test]
    public void MaintainsProperNamespaceBoundariesAcrossNestedElements()
    {
        ParserEventQueue queue = Queue(XmlNamespaceHandling.Namespaces11);
        XmlNamespaceBinding outer = Binding("outer", "urn:outer", false);
        XmlNamespaceBinding inner = Binding("inner", "urn:inner", false);

        queue.EnqueueXmlNamespaceStartEvents(ElementEvent(XmlEventKind.StartElement, 0), [outer]);
        queue.EnqueueXmlNamespaceStartEvents(ElementEvent(XmlEventKind.StartElement, 1), [inner]);
        queue.EnqueueXmlNamespaceEndEvents(ElementEvent(XmlEventKind.EndElement, 1), [inner]);
        queue.EnqueueXmlNamespaceEndEvents(ElementEvent(XmlEventKind.EndElement, 0), [outer]);
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
        Assert.That(EventPrefix(events[0]), Is.EqualTo("outer"));
        Assert.That(EventPrefix(events[2]), Is.EqualTo("inner"));
        Assert.That(EventPrefix(events[5]), Is.EqualTo("inner"));
        Assert.That(EventPrefix(events[7]), Is.EqualTo("outer"));
    }

    [Test]
    public void SnapshotsBindingsBeforeTheyEnterTheEventQueue()
    {
        ParserEventQueue queue = Queue(XmlNamespaceHandling.Namespaces11);
        XmlNamespaceBinding binding = Binding("p", "urn:original", false);
        XmlEvent start = ElementEvent(XmlEventKind.StartElement, 0);

        queue.EnqueueXmlNamespaceStartEvents(start, [binding]);
        binding.Prefix = "changed";
        binding.NamespaceUri = "urn:changed";
        start.NamespaceBindings![0].Prefix = "also-changed";
        XmlEvent first = RequireEvent(queue);

        Assert.That(EventPrefix(first), Is.EqualTo("p"));
        Assert.That(first.Value, Is.EqualTo("urn:original"));
    }

    [Test]
    public void RejectsInvalidNamespaceEventRequestsWithoutEnqueuingAnything()
    {
        ParserEventQueue disabled = Queue(XmlNamespaceHandling.Disabled);
        Assert.Throws<InvalidOperationException>(() => disabled.EnqueueXmlNamespaceStartEvents(ElementEvent(XmlEventKind.StartElement, 0), []));
        Assert.That(disabled.CountQueuedXmlEvents(), Is.Zero);

        ParserEventQueue enabled = Queue(XmlNamespaceHandling.Namespaces10);
        Assert.Throws<ArgumentException>(() => enabled.EnqueueXmlNamespaceStartEvents(ElementEvent(XmlEventKind.EndElement, 0), []));
        Assert.Throws<ArgumentException>(() => enabled.EnqueueXmlNamespaceEndEvents(ElementEvent(XmlEventKind.StartElement, 0), []));
        Assert.Throws<ArgumentException>(() => enabled.EnqueueXmlNamespaceStartEvents(ElementEvent(XmlEventKind.StartElement, 0), new XmlNamespaceBinding[] { null! }));
        Assert.Throws<ArgumentException>(() => enabled.EnqueueXmlNamespaceStartEvents(ElementEvent(XmlEventKind.StartElement, 0), [new XmlNamespaceBinding()]));
        Assert.Throws<ArgumentException>(() => enabled.EnqueueXmlNamespaceStartEvents(ElementEvent(XmlEventKind.StartElement, 0), [Binding("p", "", true)]));
        Assert.That(enabled.CountQueuedXmlEvents(), Is.Zero);
    }

    private static ParserEventQueue Queue(XmlNamespaceHandling handling)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.NamespaceHandling = handling;
        return new ParserEventQueue(options);
    }

    private static NamespaceContext Context(params XmlNamespaceBinding[] bindings)
    {
        return new NamespaceContext(null, bindings);
    }

    private static XmlNamespaceBinding Binding(string prefix, string namespaceUri, bool undeclaration)
    {
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = prefix;
        binding.NamespaceUri = namespaceUri;
        binding.IsUndeclaration = undeclaration;
        return binding;
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

    private static XmlEvent ElementEvent(XmlEventKind kind, int depth)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = kind;
        item.NodeKind = XmlNodeKind.Element;
        item.Depth = depth;
        return item;
    }

    private static string EventPrefix(XmlEvent item)
    {
        string? prefix = item.NamespaceBindings![0].Prefix;
        return prefix == null ? "" : prefix;
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

    private static XmlEvent RequireEvent(ParserEventQueue queue)
    {
        XmlEvent? item = queue.DequeueXmlEvent();
        Assert.That(item, Is.Not.Null);
        return item!;
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

    private sealed class RecordingReporter : IXmlErrorReporter
    {
        internal XmlDiagnostic? Diagnostic;

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }
    }
}
