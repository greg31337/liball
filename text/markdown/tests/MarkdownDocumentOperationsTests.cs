using System;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownDocumentOperationsTests
{
    [Test]
    public void FacadeCreatesBoundDocumentOperations()
    {
        IMarkdownDataServices services = new Markdown();
        MarkdownDocument document = new MarkdownDocument();

        IMarkdownDocumentOperations operations = services.CreateDocumentOperations(document);

        Assert.That(operations, Is.TypeOf<MarkdownDocumentOperations>());
        Assert.That(operations.GetDocument(), Is.SameAs(document));
        Assert.That(() => services.CreateDocumentOperations(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownDocumentOperations(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void DocumentDiagnosticsAreSafelyAddedCopiedAndQueried()
    {
        IMarkdownDocumentOperations operations = new MarkdownDocumentOperations(new MarkdownDocument());
        MarkdownSourceSpan firstSpan = CreateSpan(1, 5);
        MarkdownSourceSpan secondSpan = CreateSpan(7, 10);
        MarkdownDiagnostic supplied = new MarkdownDiagnostic();
        supplied.Severity = MarkdownDiagnosticSeverity.Warning;
        supplied.Code = "MD_FIRST";
        supplied.Message = "first";
        supplied.Span = firstSpan;

        operations.AddDiagnostic(supplied);
        operations.AddDiagnosticValuesWithSourceName(
            MarkdownDiagnosticSeverity.Error,
            "MD_SECOND",
            "second",
            secondSpan,
            "included.md");
        supplied.Code = "changed";
        supplied.Span.Start.Offset = 99;

        MarkdownDiagnostic first = operations.GetDiagnosticAt(0);
        first.Code = "detached";
        first.Span.Start.Offset = 88;

        Assert.That(operations.GetDiagnosticCount(), Is.EqualTo(2));
        Assert.That(operations.HasDiagnostics(), Is.True);
        Assert.That(operations.HasErrors(), Is.True);
        Assert.That(operations.GetDiagnosticAt(0).Code, Is.EqualTo("MD_FIRST"));
        Assert.That(operations.GetDiagnosticAt(0).Span.Start.Offset, Is.EqualTo(1));
        Assert.That(operations.GetDiagnosticAt(1).SourceName, Is.EqualTo("included.md"));
        Assert.That(operations.FindDiagnosticsContainingOffset(2).Length, Is.EqualTo(1));
        Assert.That(operations.FindDiagnosticsContainingOffset(5).Length, Is.EqualTo(0));
        Assert.That(operations.FindDiagnosticsIntersectingSpan(CreateSpan(4, 8)).Length, Is.EqualTo(2));

        operations.ClearDiagnostics();
        Assert.That(operations.HasDiagnostics(), Is.False);
    }

    [Test]
    public void NodeDiagnosticsSupportDetachedLookupByIndexOffsetAndSpan()
    {
        IMarkdownNodeOperations operations = new MarkdownNodeOperations(new MarkdownNode());
        MarkdownSourceSpan span = CreateSpan(3, 9);

        operations.AddDiagnosticValues(
            MarkdownDiagnosticSeverity.Info,
            "MD_NODE",
            "node",
            span);
        MarkdownDiagnostic detached = operations.GetDiagnosticAt(0);
        detached.Code = "changed";

        Assert.That(operations.GetDiagnosticAt(0).Code, Is.EqualTo("MD_NODE"));
        Assert.That(operations.FindDiagnosticsContainingOffset(3).Length, Is.EqualTo(1));
        Assert.That(operations.FindDiagnosticsContainingOffset(9).Length, Is.EqualTo(0));
        Assert.That(operations.FindDiagnosticsIntersectingSpan(CreateSpan(8, 12)).Length, Is.EqualTo(1));
        Assert.That(operations.FindDiagnosticsIntersectingSpan(CreateSpan(9, 12)).Length, Is.EqualTo(0));
    }

    [Test]
    public void SourceMapQueriesUsePreciseHalfOpenSpansAndDetachedEntries()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        MarkdownDocument document = markdown.ParseSource("# Title\n\nparagraph\n");
        IMarkdownDocumentOperations operations = services.CreateDocumentOperations(document);
        MarkdownSourcePosition position = new MarkdownSourcePosition();
        position.Offset = 10;
        position.Line = 3;
        position.Column = 2;
        MarkdownSourceSpan query = CreateSpan(9, 12);

        MarkdownSourceMapEntry[] containing = operations.FindSourceMapEntriesContainingPosition(position);
        MarkdownSourceMapEntry[] intersecting = operations.FindSourceMapEntriesIntersectingSpan(query);
        MarkdownSourceMapEntry? deepest = operations.FindDeepestSourceMapEntryAtOffset(10);
        MarkdownNode[] paragraphs = operations.FindNodesAtOffset(MarkdownNodeKind.Paragraph, 10);

        Assert.That(containing.Length, Is.EqualTo(2));
        Assert.That(intersecting.Length, Is.EqualTo(2));
        Assert.That(deepest, Is.Not.Null);
        Assert.That(deepest!.Node.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(operations.FindDeepestNodeAtOffset(10), Is.SameAs(deepest.Node));
        Assert.That(paragraphs.Length, Is.EqualTo(1));
        Assert.That(operations.DoesSourceSpanContainOffset(query, 9), Is.True);
        Assert.That(operations.DoesSourceSpanContainOffset(query, 12), Is.False);
        Assert.That(operations.DoSourceSpansIntersect(query, CreateSpan(11, 14)), Is.True);
        Assert.That(operations.DoSourceSpansIntersect(query, CreateSpan(12, 14)), Is.False);

        containing[0].Span.Start.Offset = 99;
        Assert.That(operations.GetSourceMapEntries()[0].Span.Start.Offset, Is.EqualTo(0));
    }

    [Test]
    public void TrackedTriviaExposesOriginalLineEndingsAndBlockDelimiters()
    {
        string source = "### Title ###\r\n\r\n```c\r\nvalue\r\n```\r\n";
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder().TrackTrivia().Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        MarkdownNode heading = services.CreateTreeOperations()
            .FindFirstInDocument(document, MarkdownNodeKind.Heading)!;
        MarkdownNode code = services.CreateTreeOperations()
            .FindFirstInDocument(document, MarkdownNodeKind.FencedCodeBlock)!;
        IMarkdownNodeOperations headingOperations = services.CreateNodeOperations(heading);
        IMarkdownNodeOperations codeOperations = services.CreateNodeOperations(code);
        IMarkdownNodeOperations rootOperations = services.CreateNodeOperations(document.Root);
        IMarkdownDocumentOperations documentOperations = services.CreateDocumentOperations(document);

        Assert.That(documentOperations.GetOriginalLineEnding(), Is.EqualTo("\r\n"));
        Assert.That(rootOperations.GetTriviaLineEnding(), Is.EqualTo("\r\n"));
        Assert.That(headingOperations.HasTrivia(), Is.True);
        Assert.That(headingOperations.GetOpeningDelimiter(), Is.EqualTo("###"));
        Assert.That(headingOperations.GetClosingDelimiter(), Is.EqualTo("###"));
        Assert.That(codeOperations.GetOpeningDelimiter(), Is.EqualTo("```"));
        Assert.That(codeOperations.GetClosingDelimiter(), Is.EqualTo("```"));

        MarkdownTrivia copy = headingOperations.GetTrivia()!;
        copy.OpeningDelimiter = "changed";
        Assert.That(headingOperations.GetOpeningDelimiter(), Is.EqualTo("###"));
    }

    [Test]
    public void SupportedLiteralEditPreservesUnchangedSourceAndOriginalLineEndings()
    {
        string source = "### Title ###\r\n\r\nold\r\n\r\n_tail_";
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = (IMarkdownDataServices)markdown;
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder().TrackTrivia().Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        MarkdownNode[] textNodes = services.CreateTreeOperations()
            .FindAllInDocument(document, MarkdownNodeKind.Text);

        services.CreateNodeOperations(textNodes[1]).SetLiteral("new");
        string rendered = services.CreateDocumentOperations(document)
            .RenderSourcePreservingWithPipeline(pipeline);

        Assert.That(rendered, Is.EqualTo("### Title ###\r\n\r\nnew\r\n\r\n_tail_"));
        Assert.That(document.Root.Trivia, Is.Not.Null);
        Assert.That(document.Root.Trivia!.IsModified, Is.True);
    }

    [Test]
    public void DocumentQueriesValidateInvalidOffsetsSpansAndCollaborators()
    {
        IMarkdownDocumentOperations operations = new MarkdownDocumentOperations(new MarkdownDocument());
        MarkdownSourceSpan invalid = CreateSpan(3, 4);
        invalid.End.Offset = 2;

        Assert.That(() => operations.GetDiagnosticAt(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => operations.FindDiagnosticsContainingOffset(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => operations.FindDiagnosticsIntersectingSpan(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.FindSourceMapEntriesContainingPosition(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => operations.DoesSourceSpanContainOffset(invalid, 3), Throws.TypeOf<ArgumentException>());
        Assert.That(() => operations.RenderSourcePreservingWithPipeline(null!), Throws.TypeOf<ArgumentNullException>());
    }

    private static MarkdownSourceSpan CreateSpan(int startOffset, int endOffset)
    {
        MarkdownSourceSpan span = new MarkdownSourceSpan();
        span.Start.Offset = startOffset;
        span.Start.Line = 1;
        span.Start.Column = startOffset + 1;
        span.End.Offset = endOffset;
        span.End.Line = 1;
        span.End.Column = endOffset + 1;
        return span;
    }
}
