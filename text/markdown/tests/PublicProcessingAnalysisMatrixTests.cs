using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicProcessingAnalysisMatrixTests
{
    [Test]
    public void PublicNormalization_IsEquivalentAcrossSourceDocumentAndProcessingOperations()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownProcessingOperations processing = markdown.GetDataServices()
            .CreateProcessingOperations();
        string source = "## Heading ##\r\n\r\n-   one\r\n- two";
        MarkdownDocument document = markdown.ParseSource(source);
        MarkdownPipeline crlf = markdown.CreatePipelineBuilder()
            .SetLineEnding("\r\n")
            .Build();

        string normalizedSource = markdown.NormalizeSource(source);
        string normalizedDocument = markdown.NormalizeDocument(document);
        string processingNormalized = processing.NormalizeDocument(document);
        string configuredSource = markdown.NormalizeSourceWithPipeline(source, crlf);
        string configuredDocument = markdown.NormalizeDocumentWithPipeline(document, crlf);
        string processingConfigured = processing.NormalizeDocumentWithPipeline(document, crlf);

        Assert.That(normalizedSource, Is.EqualTo(
            "## Heading\n\n" +
            "- one\n" +
            "- two\n"));
        Assert.That(normalizedDocument, Is.EqualTo(normalizedSource));
        Assert.That(processingNormalized, Is.EqualTo(normalizedSource));
        Assert.That(configuredSource, Is.EqualTo(normalizedSource.Replace("\n", "\r\n")));
        Assert.That(configuredDocument, Is.EqualTo(configuredSource));
        Assert.That(processingConfigured, Is.EqualTo(configuredSource));
        Assert.That(
            markdown.NormalizeSourceWithPipeline(configuredSource, crlf),
            Is.EqualTo(configuredSource));
    }

    [Test]
    public void EveryPublicTransformPath_RunsInOrderAndPreservesDiagnosticsAndSourceMaps()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        IMarkdownProcessingOperations processing = services.CreateProcessingOperations();
        MarkdownDocument document = markdown.ParseSource("alpha");
        MarkdownPipeline commonMark = markdown.CreateCommonMarkPipeline();
        IMarkdownDocumentOperations documentOperations = services.CreateDocumentOperations(document);
        documentOperations.AddDiagnosticValuesWithSourceName(
            MarkdownDiagnosticSeverity.Warning,
            "KEEP001",
            "keep this diagnostic",
            document.Root.Span,
            "main.md");
        MarkdownDiagnosticCollection originalDiagnostics = document.Diagnostics;
        MarkdownSourceMapEntry[] before = documentOperations.GetSourceMapEntries();

        Assert.That(
            processing.ApplyDocumentTransform(document, new AppendDocumentTextTransform("a", "A")),
            Is.SameAs(document));
        processing.ApplyDocumentTransformWithPipeline(
            document,
            new AppendDocumentTextTransform("b", "B"),
            commonMark);
        processing.ApplyDocumentTransformsInOrder(
            document,
            new IMarkdownTransform[]
            {
                new AppendDocumentTextTransform("c", "C"),
                new AppendDocumentTextTransform("d", "D")
            },
            commonMark);
        processing.ApplyNodeTransform(document, new AppendNodeTextTransform("e", "E"));
        processing.ApplyNodeTransformWithPipeline(
            document,
            new AppendNodeTextTransform("f", "F"),
            commonMark);
        processing.ApplyNodeTransformsInOrder(
            document,
            new IMarkdownNodeTransform[]
            {
                new AppendNodeTextTransform("g", "G"),
                new AppendNodeTextTransform("h", "H")
            },
            commonMark);
        MarkdownPipeline transformPipeline = markdown.CreatePipelineBuilder()
            .AddTransform(new AppendDocumentTextTransform("i", "I"))
            .AddTransform(new AppendDocumentTextTransform("j", "J"))
            .Build();
        processing.ApplyPipelineTransforms(document, transformPipeline);

        MarkdownSourceMapEntry[] after = documentOperations.GetSourceMapEntries();
        MarkdownNode text = services.CreateTreeOperations()
            .FindFirstInDocument(document, MarkdownNodeKind.Text)!;

        Assert.That(text.Literal, Is.EqualTo("alphaABCDEFGHIJ"));
        Assert.That(markdown.RenderPlainTextDocument(document), Is.EqualTo("alphaABCDEFGHIJ\n"));
        Assert.That(document.Source, Is.EqualTo("alpha"));
        Assert.That(document.Diagnostics, Is.SameAs(originalDiagnostics));
        Assert.That(documentOperations.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(documentOperations.GetDiagnosticAt(0).Code, Is.EqualTo("KEEP001"));
        Assert.That(documentOperations.GetDiagnosticAt(0).SourceName, Is.EqualTo("main.md"));
        Assert.That(after.Length, Is.EqualTo(before.Length));
        int index = 0;
        while (index < before.Length)
        {
            Assert.That(after[index].Node, Is.SameAs(before[index].Node));
            Assert.That(after[index].Span.Start.Offset, Is.EqualTo(before[index].Span.Start.Offset));
            Assert.That(after[index].Span.End.Offset, Is.EqualTo(before[index].Span.End.Offset));
            Assert.That(after[index].Depth, Is.EqualTo(before[index].Depth));
            Assert.That(after[index].ChildIndex, Is.EqualTo(before[index].ChildIndex));
            index = index + 1;
        }
    }

    [Test]
    public void EveryAnalysisEntryPoint_AgreesOnOutlineSlugsTocInventoriesAndMetrics()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseAdvancedExtensions()
            .SetLineEnding("\r\n")
            .Build();
        MarkdownAnalysisOptions options = new MarkdownAnalysisOptions();
        options.WordsPerMinute = 60;
        options.TableOfContentsMinimumLevel = 1;
        options.TableOfContentsMaximumLevel = 2;
        string source =
            "---\n" +
            "title: Report\n" +
            "---\n" +
            "# Repeat\n\n" +
            "## Child\n\n" +
            "# Repeat\n\n" +
            "[ref]: /reference \"Reference\"\n\n" +
            "Words [linked][ref] and ![logo][ref].";
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);

        MarkdownDocumentAnalysis sourceConfigured =
            services.AnalyzeSourceWithPipelineAndOptions(source, pipeline, options);
        MarkdownDocumentAnalysis documentConfigured =
            services.AnalyzeDocumentWithPipelineAndOptions(document, pipeline, options);
        AssertAnalysesEqual(sourceConfigured, documentConfigured);

        MarkdownDocumentAnalysis sourcePipeline = services.AnalyzeSourceWithPipeline(source, pipeline);
        MarkdownDocumentAnalysis documentPipeline = services.AnalyzeDocumentWithPipeline(document, pipeline);
        AssertAnalysesEqual(sourcePipeline, documentPipeline);

        string simpleSource = "# Simple\n\none two three";
        MarkdownDocument simpleDocument = markdown.ParseSource(simpleSource);
        MarkdownDocumentAnalysis sourceDefault = services.AnalyzeSource(simpleSource);
        MarkdownDocumentAnalysis documentDefault = services.AnalyzeDocument(simpleDocument);
        AssertAnalysesEqual(sourceDefault, documentDefault);
        MarkdownDocumentAnalysis sourceOptions = services.AnalyzeSourceWithOptions(simpleSource, options);
        MarkdownDocumentAnalysis documentOptions = services.AnalyzeDocumentWithOptions(simpleDocument, options);
        AssertAnalysesEqual(sourceOptions, documentOptions);

        Assert.That(sourceConfigured.Headings.Count, Is.EqualTo(3));
        Assert.That(sourceConfigured.Headings[0].Id, Is.EqualTo("repeat"));
        Assert.That(sourceConfigured.Headings[1].Id, Is.EqualTo("child"));
        Assert.That(sourceConfigured.Headings[2].Id, Is.EqualTo("repeat-1"));
        Assert.That(sourceConfigured.HeadingOutline[0].ParentIndex, Is.EqualTo(-1));
        Assert.That(sourceConfigured.HeadingOutline[1].ParentIndex, Is.EqualTo(0));
        Assert.That(sourceConfigured.HeadingOutline[1].Depth, Is.EqualTo(1));
        Assert.That(sourceConfigured.HeadingOutline[2].ParentIndex, Is.EqualTo(-1));
        Assert.That(sourceConfigured.TableOfContents, Is.EqualTo(
            "- [Repeat](#repeat)\r\n" +
            "  - [Child](#child)\r\n" +
            "- [Repeat](#repeat-1)\r\n"));
        Assert.That(sourceConfigured.LinkReferenceDefinitions.Count, Is.EqualTo(1));
        Assert.That(sourceConfigured.LinkReferenceDefinitions[0].Destination, Is.EqualTo("/reference"));
        Assert.That(sourceConfigured.FrontMatter.Count, Is.EqualTo(1));
        Assert.That(sourceConfigured.FrontMatter[0].Format, Is.EqualTo("yaml"));
        Assert.That(sourceConfigured.FrontMatter[0].Content, Is.EqualTo("title: Report"));
        Assert.That(sourceConfigured.Links.Count, Is.EqualTo(1));
        Assert.That(sourceConfigured.Images.Count, Is.EqualTo(1));
        Assert.That(sourceConfigured.PlainText, Does.Contain("Words linked and logo."));
        Assert.That(sourceConfigured.CharacterCount, Is.EqualTo(sourceConfigured.PlainText.Length));
        Assert.That(sourceConfigured.WordCount, Is.EqualTo(CountWords(sourceConfigured.PlainText)));
        Assert.That(sourceConfigured.ReadingTimeSeconds, Is.EqualTo(sourceConfigured.WordCount));
        Assert.That(sourceConfigured.ReadingTimeMinutes, Is.EqualTo(1));
        Assert.That(sourceConfigured.DocumentCount, Is.EqualTo(1));
        Assert.That(sourceConfigured.NodeCount, Is.EqualTo(CountEnterEvents(
            services.ReadDocumentEvents(document))));
        Assert.That(sourceConfigured.TextNodeCount, Is.EqualTo(CountEnterEventsOfKind(
            services.ReadDocumentEvents(document),
            MarkdownNodeKind.Text)));
        Assert.That(sourceConfigured.BlockCount, Is.GreaterThan(0));
        Assert.That(sourceConfigured.InlineCount, Is.GreaterThan(0));
        Assert.That(sourceConfigured.ExtensionCount, Is.GreaterThan(0));
    }

    [Test]
    public void ReferenceAndUriRewriters_UpdateResolvedLinksImagesAndSubsequentAnalysis()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        IMarkdownProcessingOperations processing = services.CreateProcessingOperations();
        MarkdownDocument document = markdown.ParseSource(
            "[old]: /one \"One\"\n" +
            "[next]: /two \"Two\"\n\n" +
            "[link][old] ![image][old] [direct](/direct) ![direct image](/pic)");

        Assert.That(processing.ResolveReferences(document), Is.EqualTo(2));
        Assert.That(
            processing.RewriteLinkDefinitions(document, new PrefixDestinationRewriter("/base")),
            Is.EqualTo(2));
        Assert.That(
            processing.RewriteLinkDestinations(document, new SuffixDestinationRewriter("?tracked")),
            Is.EqualTo(2));
        Assert.That(
            processing.RewriteImageReferences(document, new FixedReferenceRewriter("next")),
            Is.EqualTo(1));

        MarkdownDocumentAnalysis analysis = services.AnalyzeDocument(document);
        Assert.That(analysis.LinkReferenceDefinitions[0].Destination, Is.EqualTo("/base/one"));
        Assert.That(analysis.LinkReferenceDefinitions[1].Destination, Is.EqualTo("/base/two"));
        Assert.That(analysis.Links.Count, Is.EqualTo(2));
        Assert.That(analysis.Links[0].Destination, Is.EqualTo("/base/one?tracked"));
        Assert.That(analysis.Links[1].Destination, Is.EqualTo("/direct?tracked"));
        Assert.That(analysis.Images.Count, Is.EqualTo(2));
        Assert.That(analysis.Images[0].Destination, Is.EqualTo("/base/two"));
        Assert.That(analysis.Images[0].Title, Is.EqualTo("Two"));
        Assert.That(analysis.Images[1].Destination, Is.EqualTo("/pic"));
        Assert.That(markdown.RenderHtmlDocument(document), Is.EqualTo(
            "<p><a href=\"/base/one?tracked\" title=\"One\">link</a> " +
            "<img src=\"/base/two\" alt=\"image\" title=\"Two\"> " +
            "<a href=\"/direct?tracked\">direct</a> " +
            "<img src=\"/pic\" alt=\"direct image\"></p>\n"));

        MarkdownPipeline renderingRewrites = markdown.CreatePipelineBuilder()
            .RewriteLinkUrisWith(new PrefixDestinationRewriter("/render"))
            .RewriteImageUrisWith(new PrefixDestinationRewriter("/media"))
            .Build();
        string rewrittenHtml = markdown.RenderHtmlDocumentWithPipeline(document, renderingRewrites);
        Assert.That(rewrittenHtml, Does.Contain("href=\"/render/base/one?tracked\""));
        Assert.That(rewrittenHtml, Does.Contain("href=\"/render/direct?tracked\""));
        Assert.That(rewrittenHtml, Does.Contain("src=\"/media/base/two\""));
        Assert.That(rewrittenHtml, Does.Contain("src=\"/media/pic\""));
    }

    public static void AssertAnalysesEqual(
        MarkdownDocumentAnalysis expected,
        MarkdownDocumentAnalysis actual)
    {
        Assert.That(actual.PlainText, Is.EqualTo(expected.PlainText));
        Assert.That(actual.TableOfContents, Is.EqualTo(expected.TableOfContents));
        Assert.That(actual.NodeCount, Is.EqualTo(expected.NodeCount));
        Assert.That(actual.DocumentCount, Is.EqualTo(expected.DocumentCount));
        Assert.That(actual.BlockCount, Is.EqualTo(expected.BlockCount));
        Assert.That(actual.InlineCount, Is.EqualTo(expected.InlineCount));
        Assert.That(actual.ExtensionCount, Is.EqualTo(expected.ExtensionCount));
        Assert.That(actual.TextNodeCount, Is.EqualTo(expected.TextNodeCount));
        Assert.That(actual.WordCount, Is.EqualTo(expected.WordCount));
        Assert.That(actual.CharacterCount, Is.EqualTo(expected.CharacterCount));
        Assert.That(actual.ReadingTimeSeconds, Is.EqualTo(expected.ReadingTimeSeconds));
        Assert.That(actual.ReadingTimeMinutes, Is.EqualTo(expected.ReadingTimeMinutes));
        Assert.That(actual.Headings.Count, Is.EqualTo(expected.Headings.Count));
        Assert.That(actual.HeadingOutline.Count, Is.EqualTo(expected.HeadingOutline.Count));
        Assert.That(actual.Links.Count, Is.EqualTo(expected.Links.Count));
        Assert.That(actual.Images.Count, Is.EqualTo(expected.Images.Count));
        Assert.That(
            actual.LinkReferenceDefinitions.Count,
            Is.EqualTo(expected.LinkReferenceDefinitions.Count));
        Assert.That(actual.FrontMatter.Count, Is.EqualTo(expected.FrontMatter.Count));
        int index = 0;
        while (index < expected.Headings.Count)
        {
            Assert.That(actual.Headings[index].Level, Is.EqualTo(expected.Headings[index].Level));
            Assert.That(actual.Headings[index].Text, Is.EqualTo(expected.Headings[index].Text));
            Assert.That(actual.Headings[index].Id, Is.EqualTo(expected.Headings[index].Id));
            Assert.That(
                actual.HeadingOutline[index].ParentIndex,
                Is.EqualTo(expected.HeadingOutline[index].ParentIndex));
            Assert.That(
                actual.HeadingOutline[index].Depth,
                Is.EqualTo(expected.HeadingOutline[index].Depth));
            index = index + 1;
        }
    }

    public static int CountWords(string text)
    {
        int count = 0;
        bool inWord = false;
        int index = 0;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                count = count + 1;
                inWord = true;
            }

            index = index + 1;
        }

        return count;
    }

    public static int CountEnterEvents(MarkdownEvent[] events)
    {
        int count = 0;
        int index = 0;
        while (index < events.Length)
        {
            if (events[index].EventKind == MarkdownEventKind.EnterNode)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    public static int CountEnterEventsOfKind(MarkdownEvent[] events, MarkdownNodeKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < events.Length)
        {
            if (events[index].EventKind == MarkdownEventKind.EnterNode
                && events[index].NodeKind == kind)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    public static MarkdownNode? FindFirst(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            MarkdownNode? found = FindFirst(node.Children[index], kind);
            if (found != null)
            {
                return found;
            }

            index = index + 1;
        }

        return null;
    }

    private sealed class AppendDocumentTextTransform : IMarkdownTransform
    {
        private readonly string name;
        private readonly string suffix;

        public AppendDocumentTextTransform(string name, string suffix)
        {
            this.name = name;
            this.suffix = suffix;
        }

        public string GetName()
        {
            return name;
        }

        public void Apply(MarkdownDocument document, MarkdownTransformContext context)
        {
            Assert.That(context.Document, Is.SameAs(document));
            MarkdownNode? text = FindFirst(document.Root, MarkdownNodeKind.Text);
            if (text != null)
            {
                text.Literal = text.Literal + suffix;
            }
        }
    }

    private sealed class AppendNodeTextTransform : IMarkdownNodeTransform
    {
        private readonly string name;
        private readonly string suffix;

        public AppendNodeTextTransform(string name, string suffix)
        {
            this.name = name;
            this.suffix = suffix;
        }

        public string GetNodeTransformName()
        {
            return name;
        }

        public void ApplyToNode(MarkdownNode node, MarkdownTransformContext context)
        {
            Assert.That(context.Document, Is.Not.Null);
            if (node.Kind == MarkdownNodeKind.Text)
            {
                node.Literal = node.Literal + suffix;
            }
        }
    }

    private sealed class PrefixDestinationRewriter : IMarkdownDestinationRewriter
    {
        private readonly string prefix;

        public PrefixDestinationRewriter(string prefix)
        {
            this.prefix = prefix;
        }

        public string RewriteDestination(MarkdownNode node, string destination)
        {
            return prefix + destination;
        }
    }

    private sealed class SuffixDestinationRewriter : IMarkdownDestinationRewriter
    {
        private readonly string suffix;

        public SuffixDestinationRewriter(string suffix)
        {
            this.suffix = suffix;
        }

        public string RewriteDestination(MarkdownNode node, string destination)
        {
            return destination + suffix;
        }
    }

    private sealed class FixedReferenceRewriter : IMarkdownReferenceRewriter
    {
        private readonly string replacement;

        public FixedReferenceRewriter(string replacement)
        {
            this.replacement = replacement;
        }

        public string RewriteReference(MarkdownNode node, string referenceLabel)
        {
            return replacement;
        }
    }
}
