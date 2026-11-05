using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownEventWriterTests
{
    [Test]
    public void WriteAll_RebuildsDocumentFromReaderEvents()
    {
        MarkdownDocument original = MarkdownParser.Parse("# Title\n\nHello *world* and [link](/url \"t\")");
        MarkdownEvent[] events = MarkdownEventReader.ReadAll(original);

        MarkdownDocument rebuilt = MarkdownEventWriter.WriteAll(events);

        Assert.That(rebuilt.Root, Is.Not.SameAs(original.Root));
        Assert.That(MarkdownRenderer.Render(rebuilt), Is.EqualTo(MarkdownRenderer.Render(original)));
        Assert.That(rebuilt.Root.GetChild(1).GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(rebuilt.Root.GetChild(1).GetChild(3).GetAttributeValue("destination"), Is.EqualTo("/url"));
    }

    [Test]
    public void WriteAll_CanBuildDocumentFromTopLevelNodeStream()
    {
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        text.SetLiteral("hello");
        paragraph.AppendChild(text);
        MarkdownEvent[] events = MarkdownEventReader.ReadAll(paragraph);

        MarkdownDocument document = MarkdownEventWriter.WriteAll(events);

        Assert.That(document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(MarkdownRenderer.Render(document), Is.EqualTo("hello\n"));
    }

    [Test]
    public void Writer_PreservesEventSnapshotData()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(2, 1, 3, 8, 1, 9);
        MarkdownEvent enter = MarkdownEvent.Create(MarkdownEventKind.EnterNode, MarkdownNodeKind.Heading);
        enter.SetSourceSpan(span);
        enter.SetLiteral("Title");
        enter.SetAttribute("level", "2");
        MarkdownEvent leave = enter.Copy();
        leave.EventKind = MarkdownEventKind.LeaveNode;

        span.Start.Offset = 99;
        enter.SetAttribute("level", "3");

        MarkdownDocument document = MarkdownEventWriter.WriteAll(new MarkdownEvent[]
        {
            enter,
            leave
        });
        MarkdownNode heading = document.Root.GetChild(0);

        Assert.That(heading.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(heading.Literal, Is.EqualTo("Title"));
        Assert.That(heading.Span.Start.Offset, Is.EqualTo(2));
        Assert.That(heading.GetAttributeValue("level"), Is.EqualTo("3"));
    }

    [Test]
    public void WriteAll_CanStreamFromReader()
    {
        MarkdownEventReader reader = new MarkdownEventReader("alpha **beta**");

        MarkdownDocument document = MarkdownEventWriter.WriteAll(reader);

        Assert.That(MarkdownRenderer.Render(document), Is.EqualTo("alpha **beta**\n"));
    }

    [Test]
    public void Writer_CanWriteIncrementallyAndIgnoreNoneEvents()
    {
        MarkdownEventWriter writer = new MarkdownEventWriter();
        MarkdownEvent enter = MarkdownEvent.Create(MarkdownEventKind.EnterNode, MarkdownNodeKind.Paragraph);
        MarkdownEvent textEnter = MarkdownEvent.Create(MarkdownEventKind.EnterNode, MarkdownNodeKind.Text);
        MarkdownEvent textLeave = MarkdownEvent.Create(MarkdownEventKind.LeaveNode, MarkdownNodeKind.Text);
        MarkdownEvent leave = MarkdownEvent.Create(MarkdownEventKind.LeaveNode, MarkdownNodeKind.Paragraph);
        textEnter.SetLiteral("chunk");

        writer.Write(new MarkdownEvent());
        writer.Write(enter);
        writer.Write(textEnter);
        writer.Write(textLeave);
        writer.Write(leave);

        Assert.That(writer.GetOpenNodeCount(), Is.EqualTo(0));
        Assert.That(MarkdownRenderer.Render(writer.Finish()), Is.EqualTo("chunk\n"));
    }

    [Test]
    public void WriterRejectsMalformedStreams()
    {
        MarkdownEventWriter writer = new MarkdownEventWriter();
        MarkdownEvent enterDocument = MarkdownEvent.Create(MarkdownEventKind.EnterNode, MarkdownNodeKind.Document);
        MarkdownEvent leaveDocument = MarkdownEvent.Create(MarkdownEventKind.LeaveNode, MarkdownNodeKind.Document);
        MarkdownEvent enterParagraph = MarkdownEvent.Create(MarkdownEventKind.EnterNode, MarkdownNodeKind.Paragraph);
        MarkdownEvent leaveHeading = MarkdownEvent.Create(MarkdownEventKind.LeaveNode, MarkdownNodeKind.Heading);
        MarkdownEvent invalid = MarkdownEvent.Create((MarkdownEventKind)99, MarkdownNodeKind.Text);

        Assert.That(() => writer.Write(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventWriter.WriteAll((MarkdownEvent[])null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEventWriter.WriteAll((MarkdownEventReader)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.Write(leaveDocument), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => writer.Write(invalid), Throws.TypeOf<ArgumentException>());

        writer.Write(enterParagraph);
        Assert.That(() => writer.Write(leaveHeading), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => writer.Finish(), Throws.TypeOf<InvalidOperationException>());

        MarkdownEventWriter documentWriter = new MarkdownEventWriter();
        documentWriter.Write(enterDocument);
        documentWriter.Write(leaveDocument);
        Assert.That(() => documentWriter.Write(enterParagraph), Throws.TypeOf<InvalidOperationException>());

        MarkdownEventWriter nestedDocumentWriter = new MarkdownEventWriter();
        nestedDocumentWriter.Write(enterParagraph);
        Assert.That(() => nestedDocumentWriter.Write(enterDocument), Throws.TypeOf<InvalidOperationException>());

        MarkdownEventWriter lateDocumentWriter = new MarkdownEventWriter();
        lateDocumentWriter.Write(enterParagraph);
        lateDocumentWriter.Write(MarkdownEvent.Create(MarkdownEventKind.LeaveNode, MarkdownNodeKind.Paragraph));
        Assert.That(() => lateDocumentWriter.Write(enterDocument), Throws.TypeOf<InvalidOperationException>());
    }
}
