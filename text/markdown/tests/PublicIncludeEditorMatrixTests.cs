using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicIncludeEditorMatrixTests
{
    [Test]
    public void NestedIncludes_ProduceContiguousExactSourceAttribution()
    {
        IMarkdown markdown = new Markdown();
        string rootSource = "root-a\n!INCLUDE \"a.md\"\nroot-b\n";
        string aSource = "a-1\n!INCLUDE \"b.md\"\na-2\n";
        string bSource = "b-only\n";
        List<string> requests = new List<string>();
        MatrixIncludeResolver resolver = new MatrixIncludeResolver(context =>
        {
            requests.Add(
                context.RequestedName + ":" + context.RelativeToName + ":" + context.Depth);
            if (context.RequestedName == "a.md")
            {
                return Found("docs/a.md", aSource);
            }

            return Found("docs/b.md", bSource);
        });
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseIncludes(resolver)
            .SetIncludeRootName("docs/root.md")
            .SetMaximumIncludeDepth(4)
            .Build();

        MarkdownDocument document = markdown.ParseSourceWithPipeline(rootSource, pipeline);

        Assert.That(document.Source, Is.EqualTo(
            "root-a\n" +
            "a-1\n" +
            "b-only\n" +
            "a-2\n" +
            "root-b\n"));
        Assert.That(requests, Is.EqualTo(new string[]
        {
            "a.md:docs/root.md:1",
            "b.md:docs/a.md:2"
        }));
        Assert.That(document.SourceAttributions.Length, Is.EqualTo(5));
        AssertAttribution(document, 0, "docs/root.md", 0, "root-a\n", rootSource);
        AssertAttribution(document, 1, "docs/a.md", 1, "a-1\n", aSource);
        AssertAttribution(document, 2, "docs/b.md", 2, "b-only\n", bSource);
        AssertAttribution(document, 3, "docs/a.md", 1, "a-2\n", aSource);
        AssertAttribution(document, 4, "docs/root.md", 0, "root-b\n", rootSource);

        int index = 0;
        int nextOffset = 0;
        while (index < document.SourceAttributions.Length)
        {
            MarkdownSourceAttribution attribution = document.SourceAttributions[index];
            Assert.That(attribution.ExpandedSpan.Start.Offset, Is.EqualTo(nextOffset));
            nextOffset = attribution.ExpandedSpan.End.Offset;
            index = index + 1;
        }

        Assert.That(nextOffset, Is.EqualTo(document.Source.Length));
        Assert.That(document.Diagnostics.Items, Is.Empty);
    }

    [Test]
    public void MissingAndCyclicIncludes_PreserveDirectivesWithAttributedDiagnostics()
    {
        IMarkdown markdown = new Markdown();
        string source =
            "!INCLUDE \"missing.md\"\n" +
            "!INCLUDE \"self.md\"\n";
        MatrixIncludeResolver resolver = new MatrixIncludeResolver(context =>
        {
            if (context.RequestedName == "missing.md")
            {
                return new MarkdownIncludeResult();
            }

            return Found("root.md", "unreachable");
        });
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseIncludes(resolver)
            .SetIncludeRootName("root.md")
            .Build();

        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);

        Assert.That(resolver.CallCount, Is.EqualTo(2));
        Assert.That(document.Source, Is.EqualTo(source));
        Assert.That(document.Diagnostics.Items.Count, Is.EqualTo(2));
        Assert.That(document.Diagnostics.Items[0].Code, Is.EqualTo("MD_INCLUDE_NOT_FOUND"));
        Assert.That(document.Diagnostics.Items[1].Code, Is.EqualTo("MD_INCLUDE_CYCLE"));
        Assert.That(document.Diagnostics.Items[0].SourceName, Is.EqualTo("root.md"));
        Assert.That(document.Diagnostics.Items[1].SourceName, Is.EqualTo("root.md"));
        Assert.That(document.Diagnostics.Items[0].Span.Start.Offset, Is.EqualTo(0));
        Assert.That(
            document.Diagnostics.Items[0].Span.End.Offset,
            Is.EqualTo(source.IndexOf("!INCLUDE \"self.md\"", StringComparison.Ordinal)));
        Assert.That(document.Diagnostics.Items[1].Span.End.Offset, Is.EqualTo(source.Length));
        Assert.That(document.SourceAttributions.Length, Is.EqualTo(2));
        Assert.That(document.SourceAttributions[0].SourceName, Is.EqualTo("root.md"));
        Assert.That(document.SourceAttributions[1].SourceName, Is.EqualTo("root.md"));
        Assert.That(document.SourceAttributions[0].IncludeDepth, Is.EqualTo(0));
        Assert.That(document.SourceAttributions[1].IncludeDepth, Is.EqualTo(0));
    }

    [Test]
    public void PartialParsing_UsesWholeDocumentContextForListsFencesAndReferences()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations editor = markdown.GetDataServices().CreateEditorOperations();
        string source =
            "intro\n\n" +
            "- first\n" +
            "  continuation\n" +
            "- second\n\n" +
            "```lang\n" +
            "code\n\n" +
            "still code\n" +
            "```\n\n" +
            "[ref]: /url \"Reference\"\n\n" +
            "paragraph [ref]\n\n" +
            "tail";

        int continuationOffset = source.IndexOf("continuation", StringComparison.Ordinal);
        MarkdownDocument listPartial = editor.ParsePartialSource(
            source,
            continuationOffset,
            "continuation".Length);
        Assert.That(listPartial.Root.Children.Count, Is.EqualTo(1));
        Assert.That(listPartial.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(
            listPartial.Root.Children[0].Span.Start.Offset,
            Is.EqualTo(source.IndexOf("- first", StringComparison.Ordinal)));
        Assert.That(markdown.RenderHtmlDocument(listPartial), Does.Contain("continuation"));
        Assert.That(markdown.RenderHtmlDocument(listPartial), Does.Contain("second"));

        int codeOffset = source.IndexOf("still code", StringComparison.Ordinal);
        MarkdownDocument codePartial = editor.ParsePartialSource(source, codeOffset, 5);
        Assert.That(codePartial.Root.Children.Count, Is.EqualTo(1));
        Assert.That(codePartial.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(codePartial.Root.Children[0].Literal, Is.EqualTo("code\n\nstill code"));
        Assert.That(
            codePartial.Root.Children[0].Span.Start.Offset,
            Is.EqualTo(source.IndexOf("```lang", StringComparison.Ordinal)));

        int referenceUse = source.LastIndexOf("[ref]", StringComparison.Ordinal);
        MarkdownDocument referencePartial = editor.ParsePartialSource(source, referenceUse, 5);
        Assert.That(referencePartial.Root.Children.Count, Is.EqualTo(1));
        Assert.That(referencePartial.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(markdown.RenderHtmlDocument(referencePartial), Is.EqualTo(
            "<p>paragraph <a href=\"/url\" title=\"Reference\">ref</a></p>\n"));
        Assert.That(referencePartial.Source, Is.EqualTo(source));
        Assert.That(referencePartial.Root.Span.Start.Offset, Is.EqualTo(referenceUse));
        Assert.That(referencePartial.Root.Span.End.Offset, Is.EqualTo(referenceUse + 5));
    }

    [Test]
    public void IncrementalReparse_MatchesFullParseAcrossBoundarySensitiveChanges()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations editor = markdown.GetDataServices().CreateEditorOperations();

        string headingSource = "Paragraph\n\nTail";
        MarkdownDocument headingDocument = markdown.ParseSource(headingSource);
        MarkdownIncrementalParseResult headingResult = editor.ReparseDocument(
            headingDocument,
            new MarkdownTextChange(0, 0, "# "));
        AssertIncrementalMatchesFresh(
            markdown,
            headingResult,
            "# Paragraph\n\nTail",
            markdown.CreateCommonMarkPipeline());
        Assert.That(headingDocument.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Heading));

        string mergeSource = "first\n\nsecond\n\nTail";
        int mergeOffset = mergeSource.IndexOf("\n\n", StringComparison.Ordinal);
        MarkdownDocument mergeDocument = markdown.ParseSource(mergeSource);
        MarkdownIncrementalParseResult mergeResult = editor.ReparseDocumentWithPipeline(
            mergeDocument,
            new MarkdownTextChange(mergeOffset, 1, ""),
            markdown.CreateCommonMarkPipeline());
        AssertIncrementalMatchesFresh(
            markdown,
            mergeResult,
            "first\nsecond\n\nTail",
            markdown.CreateCommonMarkPipeline());
        Assert.That(mergeDocument.Root.Children.Count, Is.EqualTo(2));

        string fenceSource = "```\ncode\n```\n\nTail";
        MarkdownDocument fenceDocument = markdown.ParseSource(fenceSource);
        MarkdownIncrementalParseResult fenceResult = editor.ReparseDocumentWithPipeline(
            fenceDocument,
            new MarkdownTextChange(0, 4, ""),
            markdown.CreateCommonMarkPipeline());
        AssertIncrementalMatchesFresh(
            markdown,
            fenceResult,
            "code\n```\n\nTail",
            markdown.CreateCommonMarkPipeline());
    }

    [Test]
    public void IncrementalReparse_ReportsExactRangesAndRetainsOnlyUnshiftedNodes()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations editor = markdown.GetDataServices().CreateEditorOperations();
        MarkdownPipeline gfm = markdown.CreateGfmPipeline();
        string tableSource =
            "# Before\n\n" +
            "| A | B |\n" +
            "| --- | --- |\n" +
            "| old | x |\n\n" +
            "# After";
        MarkdownDocument tableDocument = markdown.ParseSourceWithPipeline(tableSource, gfm);
        MarkdownNode before = tableDocument.Root.Children[0];
        MarkdownNode table = tableDocument.Root.Children[1];
        MarkdownNode after = tableDocument.Root.Children[2];
        int tableChangeOffset = tableSource.IndexOf("old", StringComparison.Ordinal);

        MarkdownIncrementalParseResult tableResult = editor.ReparseDocumentWithPipeline(
            tableDocument,
            new MarkdownTextChange(tableChangeOffset, 3, "new"),
            gfm);
        AssertIncrementalMatchesFresh(
            markdown,
            tableResult,
            tableSource.Replace("old", "new"),
            gfm);
        Assert.That(tableDocument.Root.Children[0], Is.SameAs(before));
        Assert.That(tableDocument.Root.Children[1], Is.Not.SameAs(table));
        Assert.That(tableDocument.Root.Children[2], Is.SameAs(after));
        Assert.That(tableResult.ReusedTopLevelNodeCount, Is.EqualTo(2));

        string insertionSource = "one\n\nthree";
        MarkdownDocument insertionDocument = markdown.ParseSource(insertionSource);
        MarkdownNode first = insertionDocument.Root.Children[0];
        MarkdownNode shifted = insertionDocument.Root.Children[1];
        int insertionOffset = insertionSource.IndexOf("three", StringComparison.Ordinal);
        MarkdownIncrementalParseResult insertionResult = editor.ReparseDocument(
            insertionDocument,
            new MarkdownTextChange(insertionOffset, 0, "two\r\n"));

        AssertIncrementalMatchesFresh(
            markdown,
            insertionResult,
            "one\n\ntwo\r\nthree",
            markdown.CreateCommonMarkPipeline());
        Assert.That(insertionResult.ChangedSpan.Start.Offset, Is.EqualTo(insertionOffset));
        Assert.That(insertionResult.ChangedSpan.Start.Line, Is.EqualTo(3));
        Assert.That(insertionResult.ChangedSpan.Start.Column, Is.EqualTo(1));
        Assert.That(insertionResult.ChangedSpan.End.Offset, Is.EqualTo(insertionOffset + 5));
        Assert.That(insertionResult.ChangedSpan.End.Line, Is.EqualTo(4));
        Assert.That(insertionResult.ChangedSpan.End.Column, Is.EqualTo(1));
        Assert.That(insertionDocument.Root.Children[0], Is.SameAs(first));
        Assert.That(insertionDocument.Root.Children[1], Is.Not.SameAs(shifted));
        Assert.That(insertionResult.ReusedTopLevelNodeCount, Is.EqualTo(1));
    }

    [Test]
    public void IncrementalIncludeReparse_ReplacesStaleAttributionWithFullParseAttribution()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownEditorOperations editor = markdown.GetDataServices().CreateEditorOperations();
        MatrixIncludeResolver resolver = new MatrixIncludeResolver(context =>
            Found("docs/part.md", "inside"));
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseIncludes(resolver)
            .SetIncludeRootName("docs/root.md")
            .Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "before\n\n!INCLUDE \"part.md\"\n\nafter",
            pipeline);
        Assert.That(HasAttribution(document, "docs/part.md"), Is.True);
        int changeOffset = document.Source.IndexOf("inside", StringComparison.Ordinal);

        MarkdownIncrementalParseResult result = editor.ReparseDocumentWithPipeline(
            document,
            new MarkdownTextChange(changeOffset, "inside".Length, "edited"),
            pipeline);
        MarkdownDocument fresh = markdown.ParseSourceWithPipeline(
            "before\n\nedited\n\nafter",
            pipeline);

        AssertIncrementalMatchesFresh(markdown, result, fresh.Source, pipeline);
        AssertAttributionsEqual(fresh.SourceAttributions, document.SourceAttributions);
        Assert.That(HasAttribution(document, "docs/part.md"), Is.False);
        Assert.That(HasAttribution(document, "docs/root.md"), Is.True);
        Assert.That(resolver.CallCount, Is.EqualTo(1));
    }

    public static void AssertAttribution(
        MarkdownDocument document,
        int index,
        string sourceName,
        int depth,
        string expectedText,
        string originalSource)
    {
        MarkdownSourceAttribution attribution = document.SourceAttributions[index];
        int expandedLength = attribution.ExpandedSpan.End.Offset
            - attribution.ExpandedSpan.Start.Offset;
        int originalLength = attribution.OriginalSpan.End.Offset
            - attribution.OriginalSpan.Start.Offset;
        Assert.That(attribution.SourceName, Is.EqualTo(sourceName));
        Assert.That(attribution.IncludeDepth, Is.EqualTo(depth));
        Assert.That(
            document.Source.Substring(attribution.ExpandedSpan.Start.Offset, expandedLength),
            Is.EqualTo(expectedText));
        Assert.That(
            originalSource.Substring(attribution.OriginalSpan.Start.Offset, originalLength),
            Is.EqualTo(expectedText));
    }

    public static void AssertIncrementalMatchesFresh(
        IMarkdown markdown,
        MarkdownIncrementalParseResult result,
        string expectedSource,
        MarkdownPipeline pipeline)
    {
        MarkdownDocument fresh = markdown.ParseSourceWithPipeline(expectedSource, pipeline);
        Assert.That(result.Document.Source, Is.EqualTo(fresh.Source));
        Assert.That(result.UsedFullParse, Is.True);
        Assert.That(
            markdown.RenderHtmlDocumentWithPipeline(result.Document, pipeline),
            Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(fresh, pipeline)));
        Assert.That(
            markdown.RenderMarkdownDocumentWithPipeline(result.Document, pipeline),
            Is.EqualTo(markdown.RenderMarkdownDocumentWithPipeline(fresh, pipeline)));
        Assert.That(
            markdown.RenderPlainTextDocumentWithPipeline(result.Document, pipeline),
            Is.EqualTo(markdown.RenderPlainTextDocumentWithPipeline(fresh, pipeline)));
        Assert.That(
            markdown.RenderDebugTreeDocumentWithPipeline(result.Document, pipeline),
            Is.EqualTo(markdown.RenderDebugTreeDocumentWithPipeline(fresh, pipeline)));
        Assert.That(
            markdown.RenderXmlAstDocumentWithPipeline(result.Document, pipeline),
            Is.EqualTo(markdown.RenderXmlAstDocumentWithPipeline(fresh, pipeline)));
        Assert.That(
            result.Document.Diagnostics.Items.Count,
            Is.EqualTo(fresh.Diagnostics.Items.Count));
    }

    public static void AssertAttributionsEqual(
        MarkdownSourceAttribution[] expected,
        MarkdownSourceAttribution[] actual)
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length));
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(actual[index].SourceName, Is.EqualTo(expected[index].SourceName));
            Assert.That(actual[index].IncludeDepth, Is.EqualTo(expected[index].IncludeDepth));
            Assert.That(
                actual[index].ExpandedSpan.Start.Offset,
                Is.EqualTo(expected[index].ExpandedSpan.Start.Offset));
            Assert.That(
                actual[index].ExpandedSpan.End.Offset,
                Is.EqualTo(expected[index].ExpandedSpan.End.Offset));
            Assert.That(
                actual[index].OriginalSpan.Start.Offset,
                Is.EqualTo(expected[index].OriginalSpan.Start.Offset));
            Assert.That(
                actual[index].OriginalSpan.End.Offset,
                Is.EqualTo(expected[index].OriginalSpan.End.Offset));
            index = index + 1;
        }
    }

    public static bool HasAttribution(MarkdownDocument document, string sourceName)
    {
        int index = 0;
        while (index < document.SourceAttributions.Length)
        {
            if (document.SourceAttributions[index].SourceName == sourceName)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    public static MarkdownIncludeResult Found(string sourceName, string content)
    {
        MarkdownIncludeResult result = new MarkdownIncludeResult();
        result.Found = true;
        result.SourceName = sourceName;
        result.Content = content;
        return result;
    }

    private sealed class MatrixIncludeResolver : IMarkdownIncludeResolver
    {
        private readonly Func<MarkdownIncludeContext, MarkdownIncludeResult> handler;
        public int CallCount;

        public MatrixIncludeResolver(Func<MarkdownIncludeContext, MarkdownIncludeResult> handler)
        {
            this.handler = handler;
            CallCount = 0;
        }

        public MarkdownIncludeResult Resolve(MarkdownIncludeContext context)
        {
            CallCount = CallCount + 1;
            return handler(context);
        }
    }
}
