using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownEventReaderTests
{
    [Test]
    public void Read_ReturnsEnterAndLeaveEventsInTreeOrder()
    {
        MarkdownDocument document = CreateSampleDocument();
        MarkdownEventReader reader = MarkdownEventReader.Create(document);
        List<string> events = new List<string>();

        while (reader.Read())
        {
            MarkdownEvent current = reader.GetCurrent();
            events.Add(current.EventKind.ToString() + ":" + current.NodeKind.ToString() + ":" + current.Depth.ToString() + ":" + current.ChildIndex.ToString());
        }

        Assert.That(reader.GetCount(), Is.EqualTo(10));
        Assert.That(events, Is.EqualTo(new string[]
        {
            "EnterNode:Document:0:-1",
            "EnterNode:Paragraph:1:0",
            "EnterNode:Text:2:0",
            "LeaveNode:Text:2:0",
            "EnterNode:Emphasis:2:1",
            "EnterNode:Text:3:0",
            "LeaveNode:Text:3:0",
            "LeaveNode:Emphasis:2:1",
            "LeaveNode:Paragraph:1:0",
            "LeaveNode:Document:0:-1"
        }));
        Assert.That(reader.GetCurrent().EventKind, Is.EqualTo(MarkdownEventKind.None));
    }

    [Test]
    public void Event_SnapshotsNodeDataAndAttributes()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(1, 1, 2, 7, 1, 8);
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading, span);
        heading.SetLiteral("Title");
        heading.SetAttribute("level", "2");

        MarkdownEvent item = MarkdownEvent.Create(MarkdownEventKind.EnterNode, heading, 0, -1);

        heading.Kind = MarkdownNodeKind.Paragraph;
        heading.SetLiteral("Changed");
        heading.Span.Start.Offset = 99;
        heading.SetAttribute("level", "3");

        MarkdownAttribute[] attributes = item.GetAttributes();
        attributes[0].Value = "4";

        Assert.That(item.Node, Is.SameAs(heading));
        Assert.That(item.NodeKind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(item.Literal, Is.EqualTo("Title"));
        Assert.That(item.Span.Start.Offset, Is.EqualTo(1));
        Assert.That(item.GetAttributeValue("level"), Is.EqualTo("2"));
    }

    [Test]
    public void Reader_ReturnsCopiesOfCurrentAndEvents()
    {
        MarkdownDocument document = CreateSampleDocument();
        MarkdownEventReader reader = MarkdownEventReader.Create(document);

        Assert.That(reader.Read(), Is.True);

        MarkdownEvent current = reader.GetCurrent();
        current.NodeKind = MarkdownNodeKind.Unknown;

        Assert.That(reader.GetCurrent().NodeKind, Is.EqualTo(MarkdownNodeKind.Document));

        MarkdownEvent[] events = reader.GetEvents();
        events[0].NodeKind = MarkdownNodeKind.Unknown;

        Assert.That(reader.GetEvents()[0].NodeKind, Is.EqualTo(MarkdownNodeKind.Document));
    }

    [Test]
    public void Reset_RewindsReader()
    {
        MarkdownDocument document = CreateSampleDocument();
        MarkdownEventReader reader = MarkdownEventReader.Create(document);

        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrent().NodeKind, Is.EqualTo(MarkdownNodeKind.Document));

        reader.Reset();

        Assert.That(reader.GetCurrent().EventKind, Is.EqualTo(MarkdownEventKind.None));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrent().NodeKind, Is.EqualTo(MarkdownNodeKind.Document));
    }

    [Test]
    public void ReadAll_CanStartFromDocumentOrNode()
    {
        MarkdownDocument document = CreateSampleDocument();
        MarkdownNode paragraph = document.Root.GetChild(0);

        MarkdownEvent[] documentEvents = MarkdownEventReader.ReadAll(document);
        MarkdownEvent[] paragraphEvents = MarkdownEventReader.ReadAll(paragraph);

        Assert.That(documentEvents[0].NodeKind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(paragraphEvents[0].NodeKind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(paragraphEvents[0].Depth, Is.EqualTo(0));
        Assert.That(paragraphEvents[0].ChildIndex, Is.EqualTo(-1));
    }

    [Test]
    public void SourceReader_ParsesSourceAndPullsEvents()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownEventReader reader = MarkdownEventReader.Create("# Title\r\n\r\nHello *world*", pipeline);
        List<string> events = new List<string>();
        string headingId = "";

        while (reader.Read())
        {
            MarkdownEvent current = reader.GetCurrent();
            events.Add(current.EventKind.ToString() + ":" + current.NodeKind.ToString());
            if (current.IsEnter() && current.NodeKind == MarkdownNodeKind.Heading)
            {
                headingId = current.GetAttributeValue("id");
            }
        }

        Assert.That(reader.Source, Is.EqualTo("# Title\r\n\r\nHello *world*"));
        Assert.That(reader.GetDocument(), Is.Not.Null);
        Assert.That(reader.HasDiagnostics(), Is.False);
        Assert.That(headingId, Is.EqualTo("title"));
        Assert.That(events, Does.Contain("EnterNode:Document"));
        Assert.That(events, Does.Contain("EnterNode:Heading"));
        Assert.That(events, Does.Contain("EnterNode:Emphasis"));
        Assert.That(events[events.Count - 1], Is.EqualTo("LeaveNode:Document"));
    }

    [Test]
    public void SourceReader_ExposesParseDiagnostics()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetMaximumInputSize(3)
            .Build();

        MarkdownEventReader reader = MarkdownEventReader.Create("abcd", pipeline);

        Assert.That(reader.HasDiagnostics(), Is.True);
        Assert.That(reader.HasErrors(), Is.True);
        Assert.That(reader.GetDiagnostics()[0].Code, Is.EqualTo(MarkdownParser.InputSizeDiagnosticCode));
        Assert.That(reader.GetDocument(), Is.Not.Null);
        Assert.That(reader.GetCount(), Is.GreaterThan(0));
    }

    [Test]
    public void ReadAll_CanParseSource()
    {
        MarkdownEvent[] events = MarkdownEventReader.ReadAll("text");
        MarkdownEvent[] advancedEvents = MarkdownEventReader.ReadAll("# Title", MarkdownPipeline.AdvancedPreset());

        Assert.That(events[0].NodeKind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(advancedEvents[1].NodeKind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(advancedEvents[1].GetAttributeValue("id"), Is.EqualTo("title"));
    }

    [Test]
    public void Event_AttributeHelpersMirrorNodeBehavior()
    {
        MarkdownEvent item = MarkdownEvent.Create(MarkdownEventKind.EnterNode, MarkdownNodeKind.Link);

        item.SetAttribute("destination", "/docs");
        item.SetAttribute("title", "Docs");
        item.SetAttribute("title", "Reference");

        Assert.That(item.IsEnter(), Is.True);
        Assert.That(item.IsLeave(), Is.False);
        Assert.That(item.HasAttribute("destination"), Is.True);
        Assert.That(item.GetAttributeCount(), Is.EqualTo(2));
        Assert.That(item.GetAttributeValue("title"), Is.EqualTo("Reference"));
        Assert.That(item.RemoveAttribute("destination"), Is.True);
        Assert.That(item.RemoveAttribute("destination"), Is.False);

        item.ClearAttributes();

        Assert.That(item.GetAttributeCount(), Is.EqualTo(0));
    }

    [Test]
    public void EventModelRejectsInvalidArguments()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownEvent item = MarkdownEvent.Create(MarkdownEventKind.LeaveNode, node, 0, -1);

        Assert.That(item.IsLeave(), Is.True);
        Assert.That(() => MarkdownEvent.Create(MarkdownEventKind.EnterNode, null!, 0, -1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEvent.Create(MarkdownEventKind.EnterNode, node, -1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownEvent.Create(MarkdownEventKind.EnterNode, node, 0, -2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => item.SetLiteral(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => item.SetSourceSpan(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => item.SetAttribute(null!, "value"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => item.SetAttribute("name", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => item.SetAttributeCopy(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => item.GetAttribute(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => item.GetAttributeValue(""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => new MarkdownEventReader((string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventReader.Create("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventReader.Create((MarkdownDocument)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventReader.Create((MarkdownNode)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventReader.ReadAll((MarkdownDocument)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventReader.ReadAll((MarkdownNode)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventReader.ReadAll((string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventReader.ReadAll("", null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownDocument CreateSampleDocument()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode emphasis = MarkdownNode.Create(MarkdownNodeKind.Emphasis);
        MarkdownNode emphasizedText = MarkdownNode.Create(MarkdownNodeKind.Text);

        text.SetLiteral("hello");
        emphasizedText.SetLiteral("world");
        emphasis.AppendChild(emphasizedText);
        paragraph.AppendChild(text);
        paragraph.AppendChild(emphasis);
        document.Root.AppendChild(paragraph);

        return document;
    }
}
