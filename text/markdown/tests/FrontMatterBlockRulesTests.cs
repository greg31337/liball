using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class FrontMatterBlockRulesTests
{
    [Test]
    public void FrontMatterRule_CapturesYamlAtDocumentStart()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "---\n" +
            "title: Test\n" +
            "tags:\n" +
            "  - docs\n" +
            "---\n" +
            "# Title",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode frontMatter = document.Root.GetChild(0);
        MarkdownNode heading = document.Root.GetChild(1);

        Assert.That(frontMatter.Kind, Is.EqualTo(MarkdownNodeKind.FrontMatter));
        Assert.That(frontMatter.Literal, Is.EqualTo("title: Test\ntags:\n  - docs"));
        Assert.That(frontMatter.GetAttributeValue("delimiter"), Is.EqualTo("---"));
        Assert.That(frontMatter.GetAttributeValue("format"), Is.EqualTo("yaml"));
        Assert.That(frontMatter.Span.Start.Line, Is.EqualTo(1));
        Assert.That(frontMatter.Span.End.Line, Is.EqualTo(5));
        Assert.That(heading.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(heading.Literal, Is.EqualTo("Title"));
    }

    [Test]
    public void FrontMatterRule_CapturesTomlAtDocumentStart()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "+++\n" +
            "title = \"Test\"\n" +
            "+++\n" +
            "Body",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode frontMatter = document.Root.GetChild(0);

        Assert.That(frontMatter.Kind, Is.EqualTo(MarkdownNodeKind.FrontMatter));
        Assert.That(frontMatter.Literal, Is.EqualTo("title = \"Test\""));
        Assert.That(frontMatter.GetAttributeValue("delimiter"), Is.EqualTo("+++"));
        Assert.That(frontMatter.GetAttributeValue("format"), Is.EqualTo("toml"));
    }

    [Test]
    public void FrontMatterRule_DoesNotCaptureWithoutFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "---\n" +
            "title: Test\n" +
            "---\n" +
            "# Title",
            MarkdownPipeline.CommonMarkPreset());

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.ThematicBreak));
        Assert.That(FindFirstFrontMatter(document), Is.EqualTo(null));
    }

    [Test]
    public void FrontMatterRule_DoesNotCaptureAwayFromDocumentStart()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "Intro\n" +
            "\n" +
            "---\n" +
            "title: Test\n" +
            "---",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(FindFirstFrontMatter(document), Is.EqualTo(null));
    }

    [Test]
    public void FrontMatterRule_RequiresClosingDelimiter()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "---\n" +
            "title: Test\n" +
            "# Title",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.ThematicBreak));
        Assert.That(FindFirstFrontMatter(document), Is.EqualTo(null));
    }

    [Test]
    public void FrontMatterRule_RenderersOmitHtmlAndRoundTripMarkdown()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "---\n" +
            "title: Test\n" +
            "---\n" +
            "# Title",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<h1 id=\"title\">Title</h1>\n"));
        Assert.That(markdown, Is.EqualTo(
            "---\n" +
            "title: Test\n" +
            "---\n" +
            "\n" +
            "# Title\n"));
    }

    [Test]
    public void FrontMatterRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList advancedRules = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownRuleList builderRules = MarkdownPipelineBuilder.AdvancedPreset().GetBlockRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseCommonMark();

        Assert.That(advancedRules.Contains(FrontMatterBlockRules.FrontMatterRuleName), Is.True);
        Assert.That(advancedRules.IndexOf(FrontMatterBlockRules.FrontMatterRuleName), Is.EqualTo(0));
        Assert.That(builderRules.Contains(FrontMatterBlockRules.FrontMatterRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(FrontMatterBlockRules.FrontMatterRuleName), Is.False);
    }

    [Test]
    public void FrontMatterRule_HelperMethodsClassifyDelimiters()
    {
        MarkdownLine yaml = new MarkdownLine();
        MarkdownLine toml = new MarkdownLine();
        MarkdownLine thematic = new MarkdownLine();

        yaml.Text = "---";
        toml.Text = "+++";
        thematic.Text = "----";

        Assert.That(FrontMatterBlockRules.IsFrontMatterOpeningLine(yaml), Is.True);
        Assert.That(FrontMatterBlockRules.GetFrontMatterDelimiter(toml), Is.EqualTo("+++"));
        Assert.That(FrontMatterBlockRules.GetFrontMatterFormat("---"), Is.EqualTo("yaml"));
        Assert.That(FrontMatterBlockRules.GetFrontMatterFormat("+++"), Is.EqualTo("toml"));
        Assert.That(FrontMatterBlockRules.GetFrontMatterFormat("***"), Is.EqualTo(""));
        Assert.That(FrontMatterBlockRules.IsFrontMatterOpeningLine(thematic), Is.False);
        Assert.That(FrontMatterBlockRules.StartsWith("abcdef", "abc"), Is.True);
    }

    [Test]
    public void FrontMatterRule_RejectsInvalidArguments()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.AdvancedPreset());
        MarkdownLine line = new MarkdownLine();

        Assert.That(() => FrontMatterBlockRules.RegisterFrontMatterBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.TryParseFrontMatter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.FindClosingDelimiterIndex(null!, "---"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.FindClosingDelimiterIndex(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.IsFrontMatterOpeningLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.IsFrontMatterDelimiterLine(null!, "---"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.IsFrontMatterDelimiterLine(line, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.GetFrontMatterDelimiter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.GetFrontMatterFormat(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.StartsWith(null!, "a"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FrontMatterBlockRules.StartsWith("a", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(FrontMatterBlockRules.TryParseFrontMatter(context), Is.False);
    }

    static MarkdownNode? FindFirstFrontMatter(MarkdownDocument document)
    {
        int index = 0;
        while (index < document.Root.GetChildCount())
        {
            MarkdownNode child = document.Root.GetChild(index);
            if (child.Kind == MarkdownNodeKind.FrontMatter)
            {
                return child;
            }

            index = index + 1;
        }

        return null;
    }
}
