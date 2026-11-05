using xml;

namespace xml.Tests;

[TestFixture]
public class ParserEventQueueTests
{
    [Test]
    public void CreatesPairedEmptyElementEventsWithOrderedAttributePayloads()
    {
        ParserEventQueue queue = CreateQueue(true, true, true, true, true, true, XmlEntityHandling.Expand);
        XmlElementTag tag = ParseElementTag("<p:root p:b='one' a=\"two\"/>", true);

        queue.EnqueueXmlElementTag(tag);

        Assert.That(queue.CountQueuedXmlEvents(), Is.EqualTo(2));
        XmlEvent start = RequireEvent(queue);
        XmlEvent end = RequireEvent(queue);
        Assert.That(start.Kind, Is.EqualTo(XmlEventKind.StartElement));
        Assert.That(start.NodeKind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(start.Name!.Prefix, Is.EqualTo("p"));
        Assert.That(start.Name.LocalName, Is.EqualTo("root"));
        Assert.That(start.Name.QualifiedName, Is.EqualTo("p:root"));
        Assert.That(start.Depth, Is.Zero);
        Assert.That(start.IsEmptyElement, Is.True);
        Assert.That(start.Attributes, Has.Length.EqualTo(2));
        Assert.That(start.Attributes!.Select(attribute => attribute.Name!.QualifiedName), Is.EqualTo(new string[] { "p:b", "a" }));
        Assert.That(start.Attributes.Select(attribute => attribute.Value), Is.EqualTo(new string?[] { "one", "two" }));
        Assert.That(start.Attributes.Select(attribute => attribute.RawValue), Is.EqualTo(new string?[] { "one", "two" }));
        Assert.That(start.Attributes.Select(attribute => attribute.Quote), Is.EqualTo(new char[] { '\'', '"' }));
        Assert.That(start.Attributes.All(attribute => attribute.IsSpecified), Is.True);
        Assert.That(end.Kind, Is.EqualTo(XmlEventKind.EndElement));
        Assert.That(end.Name!.QualifiedName, Is.EqualTo("p:root"));
        Assert.That(end.Attributes, Is.Null);
        Assert.That(end.IsEmptyElement, Is.True);
        Assert.That(queue.DequeueXmlEvent(), Is.Null);
    }

    [Test]
    public void CreatesNestedStartAndEndEventsAtTheirParserDepths()
    {
        XmlScanner scanner = CreateScanner("<outer><inner></inner></outer>");
        XmlElementTagParser tagParser = new XmlElementTagParser(scanner, true, null);
        ParserEventQueue queue = CreateQueue(false, false, false, false, false, false, XmlEntityHandling.Expand);

        queue.EnqueueXmlElementTag(tagParser.ParseNextXmlElementTag());
        queue.EnqueueXmlElementTag(tagParser.ParseNextXmlElementTag());
        queue.EnqueueXmlElementTag(tagParser.ParseNextXmlElementTag());
        queue.EnqueueXmlElementTag(tagParser.ParseNextXmlElementTag());

        XmlEvent[] events = DrainEvents(queue);
        Assert.That(events.Select(item => item.Kind), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.StartElement,
            XmlEventKind.StartElement,
            XmlEventKind.EndElement,
            XmlEventKind.EndElement,
        }));
        Assert.That(events.Select(item => item.Depth), Is.EqualTo(new int[] { 0, 1, 1, 0 }));
    }

    [Test]
    public void PrefixSwitchRemovesLexicalPrefixesFromElementsAndAttributes()
    {
        ParserEventQueue queue = CreateQueue(false, false, false, false, false, false, XmlEntityHandling.Expand);
        queue.EnqueueXmlElementTag(ParseElementTag("<p:root q:name='value'>", true));

        XmlEvent item = RequireEvent(queue);

        Assert.That(item.Name!.Prefix, Is.Null);
        Assert.That(item.Name.LocalName, Is.EqualTo("root"));
        Assert.That(item.Name.QualifiedName, Is.EqualTo("root"));
        Assert.That(item.Attributes![0].Name!.Prefix, Is.Null);
        Assert.That(item.Attributes[0].Name!.LocalName, Is.EqualTo("name"));
        Assert.That(item.Attributes[0].Name!.QualifiedName, Is.EqualTo("name"));
    }

    [Test]
    public void PreservesCDataCommentsProcessingInstructionsAndIgnorableWhitespace()
    {
        ParserEventQueue queue = CreateQueue(true, false, true, true, true, true, XmlEntityHandling.Expand);

        queue.EnqueueXmlTokenEvent(ScanSingleToken("<![CDATA[raw < data]]>"), 2, false);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("<!--comment-->"), 2, false);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("<?p:target value?>"), 2, false);
        queue.EnqueueXmlTokenEvent(ScanSingleToken(" \n\t"), 2, true);

        XmlEvent[] events = DrainEvents(queue);
        Assert.That(events.Select(item => item.Kind), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.CData,
            XmlEventKind.Comment,
            XmlEventKind.ProcessingInstruction,
            XmlEventKind.IgnorableWhitespace,
        }));
        Assert.That(events.Select(item => item.NodeKind), Is.EqualTo(new XmlNodeKind[]
        {
            XmlNodeKind.CData,
            XmlNodeKind.Comment,
            XmlNodeKind.ProcessingInstruction,
            XmlNodeKind.Text,
        }));
        Assert.That(events.Select(item => item.Value), Is.EqualTo(new string?[] { "raw < data", "comment", "value", " \n\t" }));
        Assert.That(events[2].Name!.Prefix, Is.EqualTo("p"));
        Assert.That(events[2].Name!.LocalName, Is.EqualTo("target"));
        Assert.That(events[3].IsIgnorableWhitespace, Is.True);
        Assert.That(events.All(item => item.Depth == 2), Is.True);
    }

    [Test]
    public void ConvertsCDataToTextAndDropsDisabledPreservedConstructs()
    {
        ParserEventQueue queue = CreateQueue(false, false, false, false, false, false, XmlEntityHandling.Expand);

        queue.EnqueueXmlTokenEvent(ScanSingleToken("<![CDATA[data]]>"), 0, false);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("<!--comment-->"), 0, false);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("<?target value?>"), 0, false);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("  "), 0, true);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("ordinary text"), 0, false);

        XmlEvent[] events = DrainEvents(queue);
        Assert.That(events, Has.Length.EqualTo(2));
        Assert.That(events.Select(item => item.Kind), Is.EqualTo(new XmlEventKind[] { XmlEventKind.Text, XmlEventKind.Text }));
        Assert.That(events.Select(item => item.Value), Is.EqualTo(new string?[] { "data", "ordinary text" }));
        Assert.That(events.All(item => !item.IsIgnorableWhitespace), Is.True);
    }

    [Test]
    public void SynthesizesEntityBoundariesAroundExpandedGeneralEntities()
    {
        SingleEntitySource source = new SingleEntitySource(CreateDeclaration("entity", "replacement"));
        EntityExpander expander = new EntityExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, source);
        XmlEntityDispatchResult dispatch = expander.DispatchXmlReference(ScanSingleToken("&entity;"));
        ParserEventQueue queue = CreateQueue(false, true, false, false, true, false, XmlEntityHandling.Expand);

        queue.EnqueueXmlEntityDispatch(dispatch, 3);

        XmlEvent[] events = DrainEvents(queue);
        Assert.That(events.Select(item => item.Kind), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.StartEntity,
            XmlEventKind.Text,
            XmlEventKind.EndEntity,
        }));
        Assert.That(events[0].Name!.QualifiedName, Is.EqualTo("entity"));
        Assert.That(events[1].Name, Is.Null);
        Assert.That(events[1].Value, Is.EqualTo("replacement"));
        Assert.That(events[2].Name!.QualifiedName, Is.EqualTo("entity"));
        Assert.That(events.All(item => item.Depth == 3), Is.True);
    }

    [Test]
    public void SynthesizesBoundariesForEmptyAndPredefinedEntitiesButNotNumericReferences()
    {
        SingleEntitySource source = new SingleEntitySource(CreateDeclaration("empty", ""));
        EntityExpander expander = new EntityExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, source);
        ParserEventQueue queue = CreateQueue(false, true, false, false, true, false, XmlEntityHandling.Expand);

        queue.EnqueueXmlEntityDispatch(expander.DispatchXmlReference(ScanSingleToken("&empty;")), 0);
        queue.EnqueueXmlEntityDispatch(expander.DispatchXmlReference(ScanSingleToken("&amp;")), 0);
        queue.EnqueueXmlEntityDispatch(expander.DispatchXmlReference(ScanSingleToken("&#65;")), 0);

        XmlEvent[] events = DrainEvents(queue);
        Assert.That(events.Select(item => item.Kind), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.StartEntity,
            XmlEventKind.EndEntity,
            XmlEventKind.StartEntity,
            XmlEventKind.Text,
            XmlEventKind.EndEntity,
            XmlEventKind.Text,
        }));
        Assert.That(events[3].Value, Is.EqualTo("&"));
        Assert.That(events[5].Value, Is.EqualTo("A"));
    }

    [Test]
    public void FiltersSuppliedEntityBoundariesWhenReferencePreservationIsDisabled()
    {
        SingleEntitySource source = new SingleEntitySource(CreateDeclaration("entity", "value"));
        EntityExpander expander = new EntityExpander(XmlVersion.Xml10, XmlEntityHandling.PreserveReferences, false, source);
        ParserEventQueue queue = CreateQueue(false, false, false, false, false, false, XmlEntityHandling.Expand);

        queue.EnqueueXmlEntityDispatch(expander.DispatchXmlReference(ScanSingleToken("&entity;")), 1);

        XmlEvent item = RequireEvent(queue);
        Assert.That(item.Kind, Is.EqualTo(XmlEventKind.Text));
        Assert.That(item.Value, Is.EqualTo("value"));
        Assert.That(queue.CountQueuedXmlEvents(), Is.Zero);
    }

    [Test]
    public void EntityHandlingOptionAlsoEnablesReferenceBoundaries()
    {
        EntityExpander expander = new EntityExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, false, null);
        ParserEventQueue queue = CreateQueue(false, false, false, false, false, false, XmlEntityHandling.PreserveReferences);

        queue.EnqueueXmlEntityDispatch(expander.DispatchXmlReference(ScanSingleToken("&lt;")), 0);

        Assert.That(DrainEvents(queue).Select(item => item.Kind), Is.EqualTo(new XmlEventKind[]
        {
            XmlEventKind.StartEntity,
            XmlEventKind.Text,
            XmlEventKind.EndEntity,
        }));
    }

    [Test]
    public void AlwaysEmitsSkippedEntityEvents()
    {
        EntityExpander expander = new EntityExpander(XmlVersion.Xml10, XmlEntityHandling.Expand, true, null);
        ParserEventQueue queue = CreateQueue(false, false, false, false, false, false, XmlEntityHandling.Expand);

        queue.EnqueueXmlEntityDispatch(expander.DispatchXmlReference(ScanSingleToken("&missing;")), 4);

        XmlEvent item = RequireEvent(queue);
        Assert.That(item.Kind, Is.EqualTo(XmlEventKind.SkippedEntity));
        Assert.That(item.NodeKind, Is.EqualTo(XmlNodeKind.EntityReference));
        Assert.That(item.Name!.QualifiedName, Is.EqualTo("missing"));
        Assert.That(item.Depth, Is.EqualTo(4));
    }

    [Test]
    public void CreatesBoundedFifoBatchesAndMarksOnlyTheDrainedFinalBatchFinal()
    {
        ParserEventQueue queue = CreateQueue(false, false, true, false, false, false, XmlEntityHandling.Expand);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("<!--first-->"), 0, false);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("<!--second-->"), 0, false);
        queue.EnqueueXmlTokenEvent(ScanSingleToken("<!--third-->"), 0, false);

        XmlEventBatch first = queue.CreateXmlEventBatch(2, XmlParserStatus.Running, true);
        XmlEventBatch second = queue.CreateXmlEventBatch(2, XmlParserStatus.Completed, true);

        Assert.That(first.Count, Is.EqualTo(2));
        Assert.That(first.Events!.Select(item => item.Value), Is.EqualTo(new string?[] { "first", "second" }));
        Assert.That(first.Status, Is.EqualTo(XmlParserStatus.Running));
        Assert.That(first.IsFinal, Is.False);
        Assert.That(second.Count, Is.EqualTo(1));
        Assert.That(second.Events!.Select(item => item.Value), Is.EqualTo(new string?[] { "third" }));
        Assert.That(second.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(second.IsFinal, Is.True);
        Assert.That(queue.CountQueuedXmlEvents(), Is.Zero);
    }

    [Test]
    public void SnapshotsPreservationOptionsAndCanClearQueuedEvents()
    {
        XmlParserOptions options = new XmlParserOptions();
        options.PreserveComments = true;
        ParserEventQueue queue = new ParserEventQueue(options);
        options.PreserveComments = false;

        queue.EnqueueXmlTokenEvent(ScanSingleToken("<!--kept-->"), 0, false);
        Assert.That(queue.CountQueuedXmlEvents(), Is.EqualTo(1));

        queue.ClearParserEvents();
        Assert.That(queue.CountQueuedXmlEvents(), Is.Zero);
        Assert.That(queue.DequeueXmlEvent(), Is.Null);
    }

    [Test]
    public void RejectsUnsupportedTokensNegativeDepthAndInvalidBatchSize()
    {
        ParserEventQueue queue = CreateQueue(false, false, false, false, false, false, XmlEntityHandling.Expand);
        XmlToken opening = ScanSingleToken("<root/>");

        Assert.Throws<ArgumentException>(() => queue.EnqueueXmlTokenEvent(opening, 0, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => queue.EnqueueXmlTokenEvent(ScanSingleToken("text"), -1, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => queue.CreateXmlEventBatch(0, XmlParserStatus.Running, false));
    }

    private static ParserEventQueue CreateQueue(bool preserveCData, bool preserveEntities, bool preserveComments, bool preserveProcessingInstructions, bool preservePrefixes, bool preserveIgnorableWhitespace, XmlEntityHandling handling)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.PreserveCData = preserveCData;
        options.PreserveEntityReferences = preserveEntities;
        options.PreserveComments = preserveComments;
        options.PreserveProcessingInstructions = preserveProcessingInstructions;
        options.PreservePrefixes = preservePrefixes;
        options.PreserveIgnorableWhitespace = preserveIgnorableWhitespace;
        options.EntityHandling = handling;
        return new ParserEventQueue(options);
    }

    private static XmlElementTag ParseElementTag(string text, bool preserveQuotes)
    {
        XmlElementTagParser parser = new XmlElementTagParser(CreateScanner(text), preserveQuotes, null);
        return parser.ParseNextXmlElementTag();
    }

    private static XmlToken ScanSingleToken(string text)
    {
        return CreateScanner(text).ScanNextXmlToken();
    }

    private static XmlScanner CreateScanner(string text)
    {
        return new XmlScanner(text.AsMemory(), XmlVersion.Xml10, XmlEncodingEntityKind.Document);
    }

    private static XmlEvent RequireEvent(ParserEventQueue queue)
    {
        XmlEvent? item = queue.DequeueXmlEvent();
        Assert.That(item, Is.Not.Null);
        return item!;
    }

    private static XmlEvent[] DrainEvents(ParserEventQueue queue)
    {
        List<XmlEvent> events = [];
        XmlEvent? item = queue.DequeueXmlEvent();
        while (item != null)
        {
            events.Add(item);
            item = queue.DequeueXmlEvent();
        }

        return events.ToArray();
    }

    private static XmlEntityDeclaration CreateDeclaration(string name, string replacement)
    {
        XmlEntityDeclaration declaration = new XmlEntityDeclaration();
        declaration.Name = name;
        declaration.ReplacementText = replacement;
        declaration.IsParsed = true;
        return declaration;
    }

    private class SingleEntitySource : IXmlGeneralEntitySource
    {
        public XmlEntityDeclaration Declaration;

        public SingleEntitySource(XmlEntityDeclaration declaration)
        {
            Declaration = declaration;
        }

        public XmlEntityDeclaration? FindXmlGeneralEntity(string name)
        {
            return string.Equals(Declaration.Name, name, StringComparison.Ordinal) ? Declaration : null;
        }
    }
}
