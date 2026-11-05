using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicPullEventTests
{
    [Test]
    public void SourcePullReader_StreamsPipelineEventsAndCanReset()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreateGfmPipeline();
        string source = "# Title\n\nHello ~~gone~~";
        IMarkdownEventReader reader = markdown.CreateEventReaderFromSourceWithPipeline(
            source,
            pipeline);
        List<string> events = new List<string>();

        while (reader.Read())
        {
            MarkdownEvent item = reader.GetCurrent();
            events.Add(
                item.EventKind.ToString()
                + ":"
                + item.NodeKind.ToString()
                + ":"
                + item.Depth.ToString()
                + ":"
                + item.ChildIndex.ToString());
        }

        Assert.That(events[0], Is.EqualTo("EnterNode:Document:0:-1"));
        Assert.That(events[events.Count - 1], Is.EqualTo("LeaveNode:Document:0:-1"));
        Assert.That(events, Does.Contain("EnterNode:Strikethrough:2:1"));
        Assert.That(reader.GetCurrent().EventKind, Is.EqualTo(MarkdownEventKind.None));
        Assert.That(reader.GetSource(), Is.EqualTo(source));
        Assert.That(reader.GetDocument(), Is.Not.Null);
        Assert.That(reader.HasDiagnostics(), Is.False);

        reader.Reset();
        Assert.That(reader.Read(), Is.True);
        MarkdownEvent first = reader.GetCurrent();
        first.NodeKind = MarkdownNodeKind.Unknown;
        Assert.That(reader.GetDocument()!.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(reader.Read(), Is.True);
        Assert.That(first.NodeKind, Is.EqualTo(MarkdownNodeKind.Unknown));
        Assert.That(reader.GetCurrent().NodeKind, Is.EqualTo(MarkdownNodeKind.Heading));
    }

    [Test]
    public void AstPullReaders_StartAtDocumentOrStandaloneNodeRoots()
    {
        IMarkdown markdown = new Markdown();
        MarkdownDocument document = markdown.ParseSource("alpha *beta*");
        MarkdownNode paragraph = document.Root.Children[0];
        IMarkdownEventReader documentReader = markdown.CreateEventReaderFromDocument(document);
        IMarkdownEventReader nodeReader = markdown.CreateEventReaderFromNode(paragraph);

        Assert.That(documentReader.Read(), Is.True);
        Assert.That(documentReader.GetCurrent().NodeKind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(documentReader.GetDocument(), Is.SameAs(document));
        Assert.That(documentReader.GetSource(), Is.Empty);

        Assert.That(nodeReader.Read(), Is.True);
        MarkdownEvent root = nodeReader.GetCurrent();
        Assert.That(root.NodeKind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(root.Depth, Is.EqualTo(0));
        Assert.That(root.ChildIndex, Is.EqualTo(-1));
        Assert.That(nodeReader.GetDocument(), Is.Null);
        Assert.That(nodeReader.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void PullEventWriter_BuildsDocumentAndFeedsEveryRenderer()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseGfm()
            .SetLineEnding("\r\n")
            .Build();
        IMarkdownEventReader reader = markdown.CreateEventReaderFromSourceWithPipeline(
            "Hello *world* and ~~old~~",
            pipeline);
        IMarkdownEventWriter eventWriter = markdown.CreateEventWriter();
        StringWriter html = new StringWriter();
        StringWriter markdownOutput = new StringWriter();
        StringWriter plainText = new StringWriter();
        StringWriter debugTree = new StringWriter();
        StringWriter xmlAst = new StringWriter();

        eventWriter.WriteFromReader(reader);
        MarkdownDocument document = eventWriter.FinishDocument();
        eventWriter.WriteHtmlWithPipeline(html, pipeline);
        eventWriter.WriteMarkdownWithPipeline(markdownOutput, pipeline);
        eventWriter.WritePlainTextWithPipeline(plainText, pipeline);
        eventWriter.WriteDebugTreeWithPipeline(debugTree, pipeline);
        eventWriter.WriteXmlAstWithPipeline(xmlAst, pipeline);
        xmlAst.Write("tail");

        Assert.That(eventWriter.GetOpenNodeCount(), Is.EqualTo(0));
        Assert.That(html.ToString(), Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(document, pipeline)));
        Assert.That(markdownOutput.ToString(), Is.EqualTo(markdown.RenderMarkdownDocumentWithPipeline(document, pipeline)));
        Assert.That(plainText.ToString(), Is.EqualTo(markdown.RenderPlainTextDocumentWithPipeline(document, pipeline)));
        Assert.That(debugTree.ToString(), Is.EqualTo(markdown.RenderDebugTreeDocumentWithPipeline(document, pipeline)));
        Assert.That(xmlAst.ToString(), Is.EqualTo(markdown.RenderXmlAstDocumentWithPipeline(document, pipeline) + "tail"));
    }

    [Test]
    public void PullEventWriter_ValidatesDepthChildIndexesAndMatchingLeaves()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEventWriter writer = markdown.CreateEventWriter();
        MarkdownEvent enterDocument = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Document,
            0,
            -1,
            "");
        MarkdownEvent enterParagraph = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Paragraph,
            1,
            0,
            "");
        MarkdownEvent enterText = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Text,
            2,
            0,
            "chunk");
        MarkdownEvent leaveText = CreateEvent(
            MarkdownEventKind.LeaveNode,
            MarkdownNodeKind.Text,
            2,
            0,
            "chunk");
        MarkdownEvent leaveParagraph = CreateEvent(
            MarkdownEventKind.LeaveNode,
            MarkdownNodeKind.Paragraph,
            1,
            0,
            "");
        MarkdownEvent leaveDocument = CreateEvent(
            MarkdownEventKind.LeaveNode,
            MarkdownNodeKind.Document,
            0,
            -1,
            "");

        writer.Write(enterDocument);
        writer.Write(enterParagraph);
        writer.Write(enterText);
        Assert.That(writer.GetOpenNodeCount(), Is.EqualTo(3));
        writer.Write(leaveText);
        writer.Write(leaveParagraph);
        writer.Write(leaveDocument);

        MarkdownDocument document = writer.FinishDocument();
        Assert.That(markdown.RenderMarkdownDocument(document), Is.EqualTo("chunk\n"));
        Assert.That(() => writer.Write(new MarkdownEvent()), Throws.TypeOf<InvalidOperationException>());

        IMarkdownEventWriter badDepth = markdown.CreateEventWriter();
        MarkdownEvent wrongDepth = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Paragraph,
            1,
            -1,
            "");
        Assert.That(() => badDepth.Write(wrongDepth), Throws.TypeOf<InvalidOperationException>());

        IMarkdownEventWriter badChild = markdown.CreateEventWriter();
        badChild.Write(enterDocument);
        MarkdownEvent wrongChild = CreateEvent(
            MarkdownEventKind.EnterNode,
            MarkdownNodeKind.Paragraph,
            1,
            1,
            "");
        Assert.That(() => badChild.Write(wrongChild), Throws.TypeOf<InvalidOperationException>());

        IMarkdownEventWriter badLeave = markdown.CreateEventWriter();
        badLeave.Write(enterDocument);
        Assert.That(() => badLeave.Write(leaveParagraph), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => badLeave.FinishDocument(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void PublicPullEvents_ExposeDiagnosticsLimitsAndArgumentValidation()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline inputLimited = markdown.CreatePipelineBuilder()
            .SetMaximumInputSize(3)
            .Build();
        MarkdownPipeline outputLimited = markdown.CreatePipelineBuilder()
            .SetMaximumOutputSize(3)
            .Build();
        IMarkdownEventReader diagnosticReader = markdown.CreateEventReaderFromSourceWithPipeline(
            "abcd",
            inputLimited);
        IMarkdownEventWriter eventWriter = markdown.CreateEventWriter();
        eventWriter.WriteFromReader(markdown.CreateEventReaderFromSource("text"));

        Assert.That(diagnosticReader.HasDiagnostics(), Is.True);
        Assert.That(diagnosticReader.HasErrors(), Is.True);
        Assert.That(diagnosticReader.GetDiagnostics(), Is.Not.Empty);
        Assert.That(
            () => eventWriter.WriteHtmlWithPipeline(new StringWriter(), outputLimited),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => markdown.CreateEventReaderFromSource(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateEventReaderFromSourceWithPipeline("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateEventReaderFromDocument(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateEventReaderFromNode(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateEventWriter().Write(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateEventWriter().WriteFromReader(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateEventWriter().WriteHtml(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateEventWriter().WriteMarkdownWithPipeline(new StringWriter(), null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownEvent CreateEvent(
        MarkdownEventKind eventKind,
        MarkdownNodeKind nodeKind,
        int depth,
        int childIndex,
        string literal)
    {
        MarkdownEvent item = new MarkdownEvent();
        item.EventKind = eventKind;
        item.NodeKind = nodeKind;
        item.Depth = depth;
        item.ChildIndex = childIndex;
        item.Literal = literal;
        return item;
    }
}
