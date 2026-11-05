using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownSourceMapTests
{
    [Test]
    public void SourceMap_BuildsEntriesForNonEmptySpans()
    {
        MarkdownDocument document = CreateMappedDocument();

        MarkdownSourceMap map = document.CreateSourceMap();

        Assert.That(map.GetCount(), Is.EqualTo(4));
        Assert.That(map.GetEntries()[0].Node.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(map.GetEntries()[1].Node.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(map.GetEntries()[2].Node.Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(map.GetEntries()[3].Node.Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
    }

    [Test]
    public void FindContaining_ReturnsAllEntriesContainingOffset()
    {
        MarkdownSourceMap map = MarkdownSourceMap.FromDocument(CreateMappedDocument());

        MarkdownSourceMapEntry[] matches = map.FindContaining(3);

        Assert.That(matches.Length, Is.EqualTo(3));
        Assert.That(matches[0].Node.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(matches[1].Node.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(matches[2].Node.Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(matches[2].Depth, Is.EqualTo(2));
        Assert.That(matches[2].ChildIndex, Is.EqualTo(0));
    }

    [Test]
    public void FindDeepestContaining_ReturnsMostSpecificEntry()
    {
        MarkdownSourceMap map = MarkdownSourceMap.FromDocument(CreateMappedDocument());

        MarkdownSourceMapEntry? text = map.FindDeepestContaining(3);
        MarkdownSourceMapEntry? emphasis = map.FindDeepestContainingPosition(MarkdownSourcePosition.Create(7, 1, 8));
        MarkdownSourceMapEntry? missing = map.FindDeepestContaining(20);

        Assert.That(text, Is.Not.Null);
        Assert.That(text!.Node.Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(emphasis, Is.Not.Null);
        Assert.That(emphasis!.Node.Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(map.FindDeepestNode(7), Is.SameAs(emphasis.Node));
        Assert.That(missing, Is.Null);
    }

    [Test]
    public void FindNodes_FiltersContainingEntriesByKind()
    {
        MarkdownSourceMap map = MarkdownSourceMap.FromDocument(CreateMappedDocument());

        MarkdownNode[] paragraphs = map.FindNodes(MarkdownNodeKind.Paragraph, 3);
        MarkdownNode[] headings = map.FindNodes(MarkdownNodeKind.Heading, 3);

        Assert.That(paragraphs.Length, Is.EqualTo(1));
        Assert.That(paragraphs[0].Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(headings.Length, Is.EqualTo(0));
    }

    [Test]
    public void SourceMap_EntrySnapshotsAreCopied()
    {
        MarkdownDocument document = CreateMappedDocument();
        MarkdownSourceMap map = new MarkdownSourceMap(document);

        document.Root.Span.Start.Offset = 99;
        MarkdownSourceMapEntry[] entries = map.GetEntries();
        entries[0].Span.Start.Offset = 42;

        Assert.That(map.GetEntries()[0].Span.Start.Offset, Is.EqualTo(0));
    }

    [Test]
    public void SourceMap_UsesParserSpans()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Title\n\nparagraph");
        MarkdownSourceMap map = document.CreateSourceMap();

        MarkdownNode? heading = map.FindDeepestNode(1);
        MarkdownNode? paragraph = map.FindDeepestNode(10);

        Assert.That(heading, Is.Not.Null);
        Assert.That(heading!.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(paragraph, Is.Not.Null);
        Assert.That(paragraph!.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void SourceMapRejectsInvalidArguments()
    {
        MarkdownDocument document = CreateMappedDocument();
        MarkdownNode node = document.Root;
        MarkdownSourceSpan span = new MarkdownSourceSpan();
        MarkdownSourceMap map = new MarkdownSourceMap(document);

        Assert.That(() => new MarkdownSourceMap(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownSourceMap.FromDocument(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownSourceMapEntry.Create(null!, 0, -1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownSourceMapEntry.Create(node, -1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownSourceMapEntry.Create(node, 0, -2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new MarkdownSourceMapEntry(null!, span, 0, -1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownSourceMapEntry(node, null!, 0, -1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownSourceMapEntry(node, span, -1, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new MarkdownSourceMapEntry(node, span, 0, -2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => map.FindContaining(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => map.FindContainingPosition((MarkdownSourcePosition)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.FindDeepestContaining(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => map.FindDeepestContainingPosition((MarkdownSourcePosition)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.FindDeepestNodePosition((MarkdownSourcePosition)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.FindNodes(MarkdownNodeKind.Text, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => map.GetEntries()[0].ContainsOffset(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => map.GetEntries()[0].ContainsPosition(null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownDocument CreateMappedDocument()
    {
        MarkdownDocument document = MarkdownDocument.Create(MarkdownSourceSpan.Create(0, 1, 1, 20, 1, 21));
        MarkdownNode paragraph = MarkdownNode.Create(
            MarkdownNodeKind.Paragraph,
            MarkdownSourceSpan.Create(0, 1, 1, 12, 1, 13));
        MarkdownNode text = MarkdownNode.Create(
            MarkdownNodeKind.Text,
            MarkdownSourceSpan.Create(2, 1, 3, 5, 1, 6));
        MarkdownNode emphasis = MarkdownNode.Create(
            MarkdownNodeKind.Emphasis,
            MarkdownSourceSpan.Create(6, 1, 7, 11, 1, 12));
        MarkdownNode unspanned = MarkdownNode.Create(MarkdownNodeKind.Text);

        text.SetLiteral("one");
        unspanned.SetLiteral("skip");
        paragraph.AppendChild(text);
        paragraph.AppendChild(emphasis);
        paragraph.AppendChild(unspanned);
        document.Root.AppendChild(paragraph);

        return document;
    }
}
