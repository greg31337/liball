using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownEditorOperationsTests
{
    [Test]
    public void FactoryAndParsedDocuments_ExposeEditorOperationsAndNormalizedSource()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = (IMarkdownDataServices)markdown;
        IMarkdownEditorOperations operations = dataServices.CreateEditorOperations();
        MarkdownDocument document = markdown.ParseSource("text\0value");
        MarkdownDocument clone = dataServices.CreateTreeOperations().CloneDocument(document);

        Assert.That(operations, Is.TypeOf<MarkdownEditorOperations>());
        Assert.That(document.Source, Is.EqualTo("text\ufffdvalue"));
        Assert.That(clone.Source, Is.EqualTo(document.Source));
    }

    [Test]
    public void PartialParse_ReturnsContextualBlocksWithAbsoluteSpansAndSourceMaps()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        IMarkdownEditorOperations operations = services.CreateEditorOperations();
        string source = "# One\n\nParagraph *text*.\n\n## Two\n\nLast.";
        int start = source.IndexOf("text", StringComparison.Ordinal);

        MarkdownDocument partial = operations.ParsePartialSource(source, start, 4);
        MarkdownSourceMapEntry[] entries = services.GetSourceMapEntries(partial);

        Assert.That(partial.Source, Is.EqualTo(source));
        Assert.That(partial.Root.Span.Start.Offset, Is.EqualTo(start));
        Assert.That(partial.Root.Span.Start.Line, Is.EqualTo(3));
        Assert.That(partial.Root.Span.Start.Column, Is.EqualTo(12));
        Assert.That(partial.Root.Span.End.Offset, Is.EqualTo(start + 4));
        Assert.That(partial.Root.Children.Count, Is.EqualTo(1));
        Assert.That(partial.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(partial.Root.Children[0].Span.Start.Offset, Is.EqualTo(source.IndexOf("Paragraph", StringComparison.Ordinal)));
        Assert.That(entries[0].Node, Is.SameAs(partial.Root));
        Assert.That(entries[1].Node, Is.SameAs(partial.Root.Children[0]));
        Assert.That(entries[1].Span.Start.Offset, Is.EqualTo(source.IndexOf("Paragraph", StringComparison.Ordinal)));
    }

    [Test]
    public void PartialParse_UsesFullContextAtBoundarySensitiveConstructsAndPipelineFeatures()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations operations = ((IMarkdownDataServices)markdown).CreateEditorOperations();
        string setext = "Context heading\n===============\n\nafter";
        int headingText = setext.IndexOf("heading", StringComparison.Ordinal);

        MarkdownDocument contextual = operations.ParsePartialSource(setext, headingText, 3);

        Assert.That(contextual.Root.Children.Count, Is.EqualTo(1));
        Assert.That(contextual.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(contextual.Root.Children[0].Data.Heading!.Level, Is.EqualTo(1));

        string tableSource = "before\n\n| A | B |\n| --- | --- |\n| x | y |\n\nafter";
        int tableCell = tableSource.IndexOf("| x", StringComparison.Ordinal);
        MarkdownDocument table = operations.ParsePartialSourceWithPipeline(
            tableSource,
            tableCell,
            3,
            markdown.CreateGfmPipeline());
        Assert.That(table.Root.Children.Count, Is.EqualTo(1));
        Assert.That(table.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Table));
    }

    [Test]
    public void PartialParse_PreservesRequestedTriviaAndSupportsEmptyCaretRanges()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations operations = ((IMarkdownDataServices)markdown).CreateEditorOperations();
        MarkdownOptions options = new MarkdownOptions();
        options.TrackTrivia = true;
        MarkdownPipeline pipeline = markdown.CreatePipelineFromOptions(options);
        string source = "# One\r\n\r\nParagraph\r\n";
        int start = source.IndexOf("Paragraph", StringComparison.Ordinal);

        MarkdownDocument partial = operations.ParsePartialSourceWithPipeline(
            source,
            start,
            "Paragraph".Length,
            pipeline);
        MarkdownDocument caret = operations.ParsePartialSource(source, source.Length, 0);

        Assert.That(partial.Root.Trivia, Is.Not.Null);
        Assert.That(partial.Root.Trivia!.Text, Is.EqualTo("Paragraph"));
        Assert.That(partial.Root.Trivia.Span.Start.Line, Is.EqualTo(3));
        Assert.That(partial.Root.Children[0].Trivia, Is.Not.Null);
        Assert.That(caret.Root.Span.Start.Offset, Is.EqualTo(source.Length));
        Assert.That(caret.Root.Span.Start.Line, Is.EqualTo(4));
        Assert.That(caret.Root.Children, Is.Empty);
    }

    [Test]
    public void IncrementalReparse_UpdatesInPlaceAndReusesUnaffectedPrefixNodes()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        IMarkdownEditorOperations operations = services.CreateEditorOperations();
        string source = "# One\n\nFirst.\n\nSecond.";
        MarkdownDocument document = markdown.ParseSource(source);
        MarkdownNode root = document.Root;
        MarkdownNode heading = document.Root.Children[0];
        MarkdownNode firstParagraph = document.Root.Children[1];
        MarkdownNode oldSecond = document.Root.Children[2];
        int start = source.IndexOf("Second", StringComparison.Ordinal);

        MarkdownIncrementalParseResult result = operations.ReparseDocument(
            document,
            new MarkdownTextChange(start, "Second".Length, "Changed"));
        MarkdownDocument fresh = markdown.ParseSource("# One\n\nFirst.\n\nChanged.");

        Assert.That(result.Document, Is.SameAs(document));
        Assert.That(document.Root, Is.SameAs(root));
        Assert.That(document.Root.Children[0], Is.SameAs(heading));
        Assert.That(document.Root.Children[1], Is.SameAs(firstParagraph));
        Assert.That(document.Root.Children[2], Is.Not.SameAs(oldSecond));
        Assert.That(result.ReusedTopLevelNodeCount, Is.EqualTo(2));
        Assert.That(result.UsedFullParse, Is.True);
        Assert.That(document.Source, Is.EqualTo(fresh.Source));
        Assert.That(markdown.RenderHtmlDocument(document), Is.EqualTo(markdown.RenderHtmlDocument(fresh)));
        Assert.That(result.ChangedSpan.Start.Offset, Is.EqualTo(start));
        Assert.That(result.ChangedSpan.End.Offset, Is.EqualTo(start + "Changed".Length));

        MarkdownSourceMapEntry[] entries = services.GetSourceMapEntries(document);
        MarkdownSourceMapEntry changedEntry = Array.Find(
            entries,
            entry => entry.Node == document.Root.Children[2])!;
        Assert.That(changedEntry.Span.End.Offset, Is.EqualTo(document.Source.Length));
    }

    [Test]
    public void IncrementalReparse_ReusesMatchingSuffixForSameLengthChanges()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations operations = ((IMarkdownDataServices)markdown).CreateEditorOperations();
        string source = "# Before\n\nAlpha.\n\n# After";
        MarkdownDocument document = markdown.ParseSource(source);
        MarkdownNode before = document.Root.Children[0];
        MarkdownNode after = document.Root.Children[2];
        int start = source.IndexOf("Alpha", StringComparison.Ordinal);

        MarkdownIncrementalParseResult result = operations.ReparseDocument(
            document,
            new MarkdownTextChange(start, 5, "Bravo"));

        Assert.That(document.Root.Children[0], Is.SameAs(before));
        Assert.That(document.Root.Children[2], Is.SameAs(after));
        Assert.That(result.ReusedTopLevelNodeCount, Is.EqualTo(2));
        Assert.That(document.Root.Children[1].Literal, Is.EqualTo("Bravo."));
    }

    [Test]
    public void IncrementalReparse_FallsBackDeterministicallyAtBlockBoundaries()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations operations = ((IMarkdownDataServices)markdown).CreateEditorOperations();
        string source = "Paragraph\n\nNext";
        MarkdownDocument document = markdown.ParseSource(source);
        MarkdownNode oldParagraph = document.Root.Children[0];

        MarkdownIncrementalParseResult result = operations.ReparseDocument(
            document,
            new MarkdownTextChange(0, 0, "# "));
        MarkdownDocument fresh = markdown.ParseSource("# Paragraph\n\nNext");

        Assert.That(result.UsedFullParse, Is.True);
        Assert.That(result.ReusedTopLevelNodeCount, Is.EqualTo(0));
        Assert.That(document.Root.Children[0], Is.Not.SameAs(oldParagraph));
        Assert.That(document.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(markdown.RenderHtmlDocument(document), Is.EqualTo(markdown.RenderHtmlDocument(fresh)));
    }

    [Test]
    public void EditorOperations_ValidateRangesChangesAndPipelines()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations operations = ((IMarkdownDataServices)markdown).CreateEditorOperations();
        MarkdownDocument document = markdown.ParseSource("text");
        MarkdownTextChange change = new MarkdownTextChange(0, 0, "");

        Assert.That(() => new MarkdownTextChange(-1, 0, ""), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new MarkdownTextChange(0, -1, ""), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new MarkdownTextChange(0, 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.ParsePartialSource(null!, 0, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.ParsePartialSource("text", -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => operations.ParsePartialSource("text", 5, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => operations.ParsePartialSource("text", 2, 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => operations.ParsePartialSourceWithPipeline("", 0, 0, null!),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.ReparseDocument(null!, change), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.ReparseDocument(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => operations.ReparseDocumentWithPipeline(document, change, null!),
            Throws.TypeOf<ArgumentNullException>());

        change.StartOffset = 6;
        Assert.That(() => operations.ReparseDocument(document, change), Throws.TypeOf<ArgumentOutOfRangeException>());
        change.StartOffset = 0;
        change.RemovedLength = 6;
        Assert.That(() => operations.ReparseDocument(document, change), Throws.TypeOf<ArgumentOutOfRangeException>());
        change.RemovedLength = 0;
        change.InsertedText = null!;
        Assert.That(() => operations.ReparseDocument(document, change), Throws.TypeOf<ArgumentNullException>());
    }
}
