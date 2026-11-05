using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicEventMatrixTests
{
    [Test]
    public void SourceAndAstPullReaders_PreserveTraversalAttributesAndTrivia()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = markdown.GetDataServices();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseAdvancedExtensions()
            .TrackTrivia()
            .Build();
        string source =
            "# Title *em* {#custom .lead data-kind=hero}\r\n\r\n" +
            "- first\r\n" +
            "- second";
        IMarkdownEventReader sourceReader = markdown.CreateEventReaderFromSourceWithPipeline(
            source,
            pipeline);
        MarkdownEvent[] sourceEvents = ReadAll(sourceReader);
        MarkdownDocument document = sourceReader.GetDocument()!;
        IMarkdownEventReader documentReader = markdown.CreateEventReaderFromDocument(document);
        MarkdownEvent[] documentEvents = ReadAll(documentReader);
        MarkdownEvent[] arrayEvents = dataServices.ReadSourceEventsWithPipeline(source, pipeline);
        MarkdownEvent[] documentArrayEvents = dataServices.ReadDocumentEvents(document);

        AssertTraversal(sourceEvents);
        AssertEventsEqual(sourceEvents, documentEvents);
        AssertEventsEqual(sourceEvents, arrayEvents);
        AssertEventsEqual(sourceEvents, documentArrayEvents);
        Assert.That(sourceReader.GetSource(), Is.EqualTo(source));
        Assert.That(documentReader.GetSource(), Is.Empty);
        Assert.That(sourceReader.HasDiagnostics(), Is.False);
        Assert.That(sourceReader.HasErrors(), Is.False);

        MarkdownEvent heading = FindEnter(sourceEvents, MarkdownNodeKind.Heading);
        Assert.That(GetAttributeValue(heading, "id"), Is.EqualTo("custom"));
        Assert.That(GetAttributeValue(heading, "class"), Is.EqualTo("lead"));
        Assert.That(GetAttributeValue(heading, "data-kind"), Is.EqualTo("hero"));
        Assert.That(heading.Trivia, Is.Not.Null);
        Assert.That(heading.Trivia!.Text, Is.EqualTo(
            "# Title *em* {#custom .lead data-kind=hero}"));

        heading.Attributes[0].Value = "changed";
        heading.Trivia.Text = "changed trivia";
        MarkdownNode originalHeading = document.Root.Children[0];
        Assert.That(GetAttributeValue(originalHeading, "id"), Is.EqualTo("custom"));
        Assert.That(originalHeading.Trivia!.Text, Is.EqualTo(
            "# Title *em* {#custom .lead data-kind=hero}"));

        IMarkdownEventReader nodeReader = markdown.CreateEventReaderFromNode(originalHeading);
        MarkdownEvent[] nodeEvents = ReadAll(nodeReader);
        MarkdownEvent[] nodeArrayEvents = dataServices.ReadNodeEvents(originalHeading);
        AssertTraversal(nodeEvents);
        AssertEventsEqual(nodeEvents, nodeArrayEvents);
        Assert.That(nodeEvents[0].Depth, Is.EqualTo(0));
        Assert.That(nodeEvents[0].ChildIndex, Is.EqualTo(-1));
        Assert.That(nodeEvents[0].NodeKind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(nodeReader.GetDocument(), Is.Null);
    }

    [Test]
    public void EventArraysAndIncrementalWriter_ReconstructAndFeedEveryRenderer()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = markdown.GetDataServices();
        string source = "# Title\n\nA [link](/url) and *emphasis*.";
        MarkdownDocument original = markdown.ParseSource(source);
        MarkdownEvent[] events = dataServices.ReadDocumentEvents(original);
        MarkdownDocument fromArray = markdown.CreateDocumentFromEvents(events);
        IMarkdownEventWriter incremental = markdown.CreateEventWriter();
        incremental.Write(new MarkdownEvent());
        incremental.WriteFromReader(markdown.CreateEventReaderFromDocument(original));
        MarkdownDocument fromPull = incremental.FinishDocument();
        StringWriter html = new StringWriter();
        StringWriter normalized = new StringWriter();
        StringWriter plainText = new StringWriter();
        StringWriter debugTree = new StringWriter();
        StringWriter xml = new StringWriter();
        StringWriter htmlEvents = new StringWriter();
        StringWriter htmlEventsWithPipeline = new StringWriter();
        MarkdownPipeline crlf = markdown.CreatePipelineBuilder().SetLineEnding("\r\n").Build();

        incremental.WriteHtml(html);
        incremental.WriteMarkdown(normalized);
        incremental.WritePlainText(plainText);
        incremental.WriteDebugTree(debugTree);
        incremental.WriteXmlAst(xml);
        markdown.WriteHtmlEvents(events, htmlEvents);
        markdown.WriteHtmlEventsWithPipeline(events, htmlEventsWithPipeline, crlf);

        Assert.That(incremental.GetOpenNodeCount(), Is.EqualTo(0));
        Assert.That(incremental.FinishDocument(), Is.SameAs(fromPull));
        Assert.That(markdown.RenderMarkdownDocument(fromArray), Is.EqualTo(
            markdown.RenderMarkdownDocument(original)));
        Assert.That(markdown.RenderMarkdownDocument(fromPull), Is.EqualTo(
            markdown.RenderMarkdownDocument(original)));
        Assert.That(html.ToString(), Is.EqualTo(markdown.RenderHtmlDocument(original)));
        Assert.That(normalized.ToString(), Is.EqualTo(markdown.RenderMarkdownDocument(original)));
        Assert.That(plainText.ToString(), Is.EqualTo(markdown.RenderPlainTextDocument(original)));
        Assert.That(debugTree.ToString(), Is.EqualTo(markdown.RenderDebugTreeDocument(original)));
        Assert.That(xml.ToString(), Is.EqualTo(markdown.RenderXmlAstDocument(original)));
        Assert.That(htmlEvents.ToString(), Is.EqualTo(markdown.RenderHtmlDocument(original)));
        Assert.That(htmlEventsWithPipeline.ToString(), Is.EqualTo(
            markdown.RenderHtmlDocumentWithPipeline(original, crlf)));

        events[0].Literal = "mutated after reconstruction";
        Assert.That(markdown.RenderMarkdownDocument(fromArray), Is.EqualTo(
            markdown.RenderMarkdownDocument(original)));
    }

    [Test]
    public void PublicEventFields_CanTransformTextAndLinkAttributesWithoutMutatingInput()
    {
        IMarkdown markdown = new Markdown();
        string source = "[docs](/old \"Old\") and *quiet*";
        IMarkdownEventReader reader = markdown.CreateEventReaderFromSource(source);
        IMarkdownEventWriter writer = markdown.CreateEventWriter();

        while (reader.Read())
        {
            MarkdownEvent item = reader.GetCurrent();
            if (item.EventKind == MarkdownEventKind.EnterNode
                && item.NodeKind == MarkdownNodeKind.Text)
            {
                item.Literal = item.Literal.ToUpperInvariant();
            }

            if (item.EventKind == MarkdownEventKind.EnterNode
                && item.NodeKind == MarkdownNodeKind.Link)
            {
                SetAttributeValue(item, "destination", "/new");
                SetAttributeValue(item, "title", "New");
            }

            writer.Write(item);
        }

        MarkdownDocument transformed = writer.FinishDocument();

        Assert.That(markdown.RenderMarkdownDocument(transformed), Is.EqualTo(
            "[DOCS](/new \"New\") AND *QUIET*\n"));
        Assert.That(markdown.RenderHtmlDocument(transformed), Is.EqualTo(
            "<p><a href=\"/new\" title=\"New\">DOCS</a> AND <em>QUIET</em></p>\n"));
        Assert.That(markdown.RenderMarkdownDocument(reader.GetDocument()!), Is.EqualTo(
            "[docs](/old \"Old\") and *quiet*\n"));
    }

    [Test]
    public void IncrementalWriter_RejectsMalformedPublicEventSequences()
    {
        IMarkdown markdown = new Markdown();
        MarkdownEvent enterDocument = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Document,
            0,
            -1);
        MarkdownEvent leaveDocument = CreateEvent(
            MarkdownEventKind.LeaveNode,
            MarkdownNodeKind.Document,
            0,
            -1);

        MarkdownEvent unsupported = new MarkdownEvent();
        unsupported.EventKind = (MarkdownEventKind)99;
        Assert.That(
            () => markdown.CreateEventWriter().Write(unsupported),
            Throws.TypeOf<ArgumentException>());

        MarkdownEvent orphanLeave = CreateEvent(
            MarkdownEventKind.LeaveNode,
            MarkdownNodeKind.Paragraph,
            0,
            -1);
        Assert.That(
            () => markdown.CreateEventWriter().Write(orphanLeave),
            Throws.TypeOf<InvalidOperationException>());

        IMarkdownEventWriter nestedDocument = markdown.CreateEventWriter();
        nestedDocument.Write(enterDocument);
        MarkdownEvent badNestedRoot = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Document,
            1,
            0);
        Assert.That(
            () => nestedDocument.Write(badNestedRoot),
            Throws.TypeOf<InvalidOperationException>());

        IMarkdownEventWriter incomplete = markdown.CreateEventWriter();
        incomplete.Write(enterDocument);
        MarkdownEvent paragraph = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Paragraph,
            1,
            0);
        incomplete.Write(paragraph);
        Assert.That(
            () => incomplete.FinishDocument(),
            Throws.TypeOf<InvalidOperationException>());

        IMarkdownEventWriter afterRoot = markdown.CreateEventWriter();
        afterRoot.Write(enterDocument);
        afterRoot.Write(leaveDocument);
        MarkdownEvent secondRoot = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Paragraph,
            0,
            -1);
        Assert.That(
            () => afterRoot.Write(secondRoot),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void PullConsumption_CanStopEarlyAndResumeFromTheCurrentPosition()
    {
        IMarkdown markdown = new Markdown();
        string source = "# One\n\nfirst *value*\n\nsecond [link](/url)";
        IMarkdownEventReader reader = markdown.CreateEventReaderFromSource(source);
        IMarkdownEventWriter writer = markdown.CreateEventWriter();
        int consumed = 0;

        while (consumed < 5 && reader.Read())
        {
            writer.Write(reader.GetCurrent());
            consumed = consumed + 1;
        }

        Assert.That(consumed, Is.EqualTo(5));
        Assert.That(writer.GetOpenNodeCount(), Is.GreaterThan(0));

        writer.WriteFromReader(reader);
        MarkdownDocument resumed = writer.FinishDocument();

        Assert.That(writer.GetOpenNodeCount(), Is.EqualTo(0));
        Assert.That(reader.Read(), Is.False);
        Assert.That(reader.GetCurrent().EventKind, Is.EqualTo(MarkdownEventKind.None));
        Assert.That(markdown.RenderHtmlDocument(resumed), Is.EqualTo(
            markdown.RenderHtmlSource(source)));
    }

    [Test]
    public void LargeSource_IsPulledAndReconstructedIncrementally()
    {
        IMarkdown markdown = new Markdown();
        int itemCount = 1500;
        StringBuilder sourceBuilder = new StringBuilder();
        int index = 0;
        while (index < itemCount)
        {
            sourceBuilder.Append("- item ");
            sourceBuilder.Append(index);
            sourceBuilder.Append('\n');
            index = index + 1;
        }

        string source = sourceBuilder.ToString();
        IMarkdownEventReader reader = markdown.CreateEventReaderFromSource(source);
        IMarkdownEventWriter writer = markdown.CreateEventWriter();
        int eventCount = 0;
        int textCount = 0;
        int maximumDepth = 0;
        while (reader.Read())
        {
            MarkdownEvent item = reader.GetCurrent();
            eventCount = eventCount + 1;
            if (item.EventKind == MarkdownEventKind.EnterNode
                && item.NodeKind == MarkdownNodeKind.Text)
            {
                textCount = textCount + 1;
            }

            if (item.Depth > maximumDepth)
            {
                maximumDepth = item.Depth;
            }

            writer.Write(item);
        }

        MarkdownDocument reconstructed = writer.FinishDocument();
        MarkdownNode list = reconstructed.Root.Children[0];

        Assert.That(eventCount, Is.EqualTo(4 + (itemCount * 6)));
        Assert.That(textCount, Is.EqualTo(itemCount));
        Assert.That(maximumDepth, Is.EqualTo(4));
        Assert.That(list.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(list.Children.Count, Is.EqualTo(itemCount));
        Assert.That(markdown.RenderMarkdownDocument(reconstructed), Is.EqualTo(source));
    }

    public static MarkdownEvent[] ReadAll(IMarkdownEventReader reader)
    {
        List<MarkdownEvent> events = new List<MarkdownEvent>();
        while (reader.Read())
        {
            events.Add(reader.GetCurrent());
        }

        return events.ToArray();
    }

    public static void AssertTraversal(MarkdownEvent[] events)
    {
        List<MarkdownEvent> open = new List<MarkdownEvent>();
        List<int> childCounts = new List<int>();
        int rootCount = 0;
        int index = 0;
        while (index < events.Length)
        {
            MarkdownEvent item = events[index];
            if (item.EventKind == MarkdownEventKind.EnterNode)
            {
                Assert.That(item.Depth, Is.EqualTo(open.Count), "depth at event " + index);
                if (open.Count == 0)
                {
                    Assert.That(item.ChildIndex, Is.EqualTo(-1), "root child index");
                    rootCount = rootCount + 1;
                }
                else
                {
                    int parent = childCounts.Count - 1;
                    Assert.That(
                        item.ChildIndex,
                        Is.EqualTo(childCounts[parent]),
                        "child index at event " + index);
                    childCounts[parent] = childCounts[parent] + 1;
                }

                open.Add(item);
                childCounts.Add(0);
            }
            else
            {
                Assert.That(item.EventKind, Is.EqualTo(MarkdownEventKind.LeaveNode));
                Assert.That(open, Is.Not.Empty);
                MarkdownEvent enter = open[open.Count - 1];
                Assert.That(item.NodeKind, Is.EqualTo(enter.NodeKind));
                Assert.That(item.Depth, Is.EqualTo(enter.Depth));
                Assert.That(item.ChildIndex, Is.EqualTo(enter.ChildIndex));
                open.RemoveAt(open.Count - 1);
                childCounts.RemoveAt(childCounts.Count - 1);
            }

            index = index + 1;
        }

        Assert.That(open, Is.Empty);
        Assert.That(rootCount, Is.EqualTo(1));
    }

    public static void AssertEventsEqual(MarkdownEvent[] expected, MarkdownEvent[] actual)
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(actual[index].EventKind, Is.EqualTo(expected[index].EventKind));
            Assert.That(actual[index].NodeKind, Is.EqualTo(expected[index].NodeKind));
            Assert.That(actual[index].Depth, Is.EqualTo(expected[index].Depth));
            Assert.That(actual[index].ChildIndex, Is.EqualTo(expected[index].ChildIndex));
            Assert.That(actual[index].Literal, Is.EqualTo(expected[index].Literal));
            Assert.That(actual[index].Attributes.Count, Is.EqualTo(expected[index].Attributes.Count));
            Assert.That(actual[index].Trivia?.Text, Is.EqualTo(expected[index].Trivia?.Text));
            index = index + 1;
        }
    }

    public static MarkdownEvent FindEnter(MarkdownEvent[] events, MarkdownNodeKind kind)
    {
        int index = 0;
        while (index < events.Length)
        {
            if (events[index].EventKind == MarkdownEventKind.EnterNode
                && events[index].NodeKind == kind)
            {
                return events[index];
            }

            index = index + 1;
        }

        throw new AssertionException("Missing enter event for " + kind.ToString());
    }

    public static string GetAttributeValue(MarkdownEvent item, string name)
    {
        int index = 0;
        while (index < item.Attributes.Count)
        {
            if (item.Attributes[index].Name == name)
            {
                return item.Attributes[index].Value;
            }

            index = index + 1;
        }

        return "";
    }

    public static string GetAttributeValue(MarkdownNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index].Value;
            }

            index = index + 1;
        }

        return "";
    }

    public static void SetAttributeValue(MarkdownEvent item, string name, string value)
    {
        int index = 0;
        while (index < item.Attributes.Count)
        {
            if (item.Attributes[index].Name == name)
            {
                item.Attributes[index].Value = value;
                return;
            }

            index = index + 1;
        }

        MarkdownAttribute attribute = new MarkdownAttribute();
        attribute.Name = name;
        attribute.Value = value;
        item.Attributes.Add(attribute);
    }

    public static MarkdownEvent CreateEvent(
        MarkdownEventKind eventKind,
        MarkdownNodeKind nodeKind,
        int depth,
        int childIndex)
    {
        MarkdownEvent item = new MarkdownEvent();
        item.EventKind = eventKind;
        item.NodeKind = nodeKind;
        item.Depth = depth;
        item.ChildIndex = childIndex;
        return item;
    }
}
