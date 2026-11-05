using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicDocumentAnalysisTests
{
    [Test]
    public void PublicAnalysis_BuildsOutlineTocInventoriesAndFrontMatter()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseAdvancedExtensions()
            .SetLineEnding("\r\n")
            .Build();
        MarkdownAnalysisOptions options = new MarkdownAnalysisOptions();
        options.TableOfContentsMaximumLevel = 2;
        options.WordsPerMinute = 60;
        options.MinimumReadingTimeSeconds = 120;
        string source =
            "---\n" +
            "title: Demo\n" +
            "---\n" +
            "# First {#same}\n" +
            "\n" +
            "## Child\n" +
            "\n" +
            "### Deep\n" +
            "\n" +
            "# Second {#same}\n" +
            "\n" +
            "[ref]: /reference \"Reference\"\n" +
            "\n" +
            "See [docs][ref] and ![Logo](/logo.png \"Image\").";

        MarkdownDocumentAnalysis analysis = dataServices.AnalyzeSourceWithPipelineAndOptions(
            source,
            pipeline,
            options);

        Assert.That(analysis.Headings.Count, Is.EqualTo(4));
        Assert.That(analysis.Headings[0].Id, Is.EqualTo("same"));
        Assert.That(analysis.Headings[1].Id, Is.EqualTo("child"));
        Assert.That(analysis.Headings[2].Id, Is.EqualTo("deep"));
        Assert.That(analysis.Headings[3].Id, Is.EqualTo("same-1"));
        Assert.That(analysis.HeadingOutline.Count, Is.EqualTo(4));
        Assert.That(analysis.HeadingOutline[0].ParentIndex, Is.EqualTo(-1));
        Assert.That(analysis.HeadingOutline[1].ParentIndex, Is.EqualTo(0));
        Assert.That(analysis.HeadingOutline[1].Depth, Is.EqualTo(1));
        Assert.That(analysis.HeadingOutline[2].ParentIndex, Is.EqualTo(1));
        Assert.That(analysis.HeadingOutline[2].Depth, Is.EqualTo(2));
        Assert.That(analysis.HeadingOutline[3].ParentIndex, Is.EqualTo(-1));
        Assert.That(analysis.TableOfContents, Is.EqualTo(
            "- [First](#same)\r\n" +
            "  - [Child](#child)\r\n" +
            "- [Second](#same-1)\r\n"));

        Assert.That(analysis.Links.Count, Is.EqualTo(1));
        Assert.That(analysis.Links[0].Destination, Is.EqualTo("/reference"));
        Assert.That(analysis.Images.Count, Is.EqualTo(1));
        Assert.That(analysis.Images[0].Destination, Is.EqualTo("/logo.png"));
        Assert.That(analysis.LinkReferenceDefinitions.Count, Is.EqualTo(1));
        Assert.That(analysis.LinkReferenceDefinitions[0].Label, Is.EqualTo("ref"));
        Assert.That(analysis.LinkReferenceDefinitions[0].NormalizedLabel, Is.EqualTo("ref"));
        Assert.That(analysis.LinkReferenceDefinitions[0].Destination, Is.EqualTo("/reference"));
        Assert.That(analysis.LinkReferenceDefinitions[0].Title, Is.EqualTo("Reference"));
        Assert.That(analysis.FrontMatter.Count, Is.EqualTo(1));
        Assert.That(analysis.FrontMatter[0].Format, Is.EqualTo("yaml"));
        Assert.That(analysis.FrontMatter[0].Delimiter, Is.EqualTo("---"));
        Assert.That(analysis.FrontMatter[0].Content, Is.EqualTo("title: Demo"));
        Assert.That(analysis.CharacterCount, Is.EqualTo(analysis.PlainText.Length));
        Assert.That(analysis.WordCount, Is.GreaterThan(0));
        Assert.That(analysis.ReadingTimeSeconds, Is.EqualTo(120));
        Assert.That(analysis.ReadingTimeMinutes, Is.EqualTo(2));
    }

    [Test]
    public void PublicAnalysis_GeneratesCollisionSafeSlugsWithoutMutatingAst()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownDocument document = markdown.ParseSource(
            "# Hello, World!\n\n" +
            "### Hello World\n\n" +
            "# !!!");

        MarkdownDocumentAnalysis analysis = dataServices.AnalyzeDocument(document);

        Assert.That(analysis.Headings[0].Id, Is.EqualTo("hello-world"));
        Assert.That(analysis.Headings[1].Id, Is.EqualTo("hello-world-1"));
        Assert.That(analysis.Headings[2].Id, Is.EqualTo("section"));
        Assert.That(analysis.HeadingOutline[1].ParentIndex, Is.EqualTo(0));
        Assert.That(analysis.HeadingOutline[1].Depth, Is.EqualTo(1));
        Assert.That(analysis.TableOfContents, Is.EqualTo(
            "- [Hello, World\\!](#hello-world)\n" +
            "  - [Hello World](#hello-world-1)\n" +
            "- [\\!\\!\\!](#section)\n"));
        Assert.That(document.Root.Children[0].Attributes.Count, Is.EqualTo(1));
        Assert.That(document.Root.Children[0].Attributes[0].Name, Is.EqualTo("level"));
    }

    [Test]
    public void PublicAnalysis_ConfiguresReadingTimeAndCanDisableToc()
    {
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownAnalysisOptions options = new MarkdownAnalysisOptions();
        options.WordsPerMinute = 120;
        options.MinimumReadingTimeSeconds = 20;
        options.GenerateTableOfContents = false;

        MarkdownDocumentAnalysis analysis = dataServices.AnalyzeSourceWithOptions(
            "# Title\n\none two three four five six seven eight nine ten",
            options);

        Assert.That(analysis.WordCount, Is.EqualTo(11));
        Assert.That(analysis.ReadingTimeSeconds, Is.EqualTo(20));
        Assert.That(analysis.ReadingTimeMinutes, Is.EqualTo(1));
        Assert.That(analysis.TableOfContents, Is.Empty);
        Assert.That(options.WordsPerMinute, Is.EqualTo(120));
    }

    [Test]
    public void PublicAnalysis_ValidatesArgumentsAndOptions()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownDocument document = markdown.ParseSource("text");
        MarkdownAnalysisOptions options = new MarkdownAnalysisOptions();

        Assert.That(() => dataServices.AnalyzeDocumentWithOptions(null!, options), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => dataServices.AnalyzeDocumentWithOptions(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => dataServices.AnalyzeDocumentWithPipelineAndOptions(document, null!, options), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => dataServices.AnalyzeSource(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => dataServices.AnalyzeSourceWithPipeline("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => dataServices.AnalyzeSourceWithPipelineAndOptions("", markdown.CreateCommonMarkPipeline(), null!), Throws.TypeOf<ArgumentNullException>());

        options.WordsPerMinute = 0;
        Assert.That(() => dataServices.AnalyzeDocumentWithOptions(document, options), Throws.TypeOf<ArgumentOutOfRangeException>());
        options.WordsPerMinute = 200;
        options.MinimumReadingTimeSeconds = -1;
        Assert.That(() => dataServices.AnalyzeDocumentWithOptions(document, options), Throws.TypeOf<ArgumentOutOfRangeException>());
        options.MinimumReadingTimeSeconds = 0;
        options.TableOfContentsMinimumLevel = 0;
        Assert.That(() => dataServices.AnalyzeDocumentWithOptions(document, options), Throws.TypeOf<ArgumentOutOfRangeException>());
        options.TableOfContentsMinimumLevel = 3;
        options.TableOfContentsMaximumLevel = 2;
        Assert.That(() => dataServices.AnalyzeDocumentWithOptions(document, options), Throws.TypeOf<ArgumentException>());
    }
}
