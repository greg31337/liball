using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class CustomFencedBlockRulesTests
{
    [Test]
    public void CustomFencedRule_ParsesRegisteredInfoString()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomFencedBlock("chart")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse(
            "```chart title=Sales\n" +
            "a < b\n" +
            "```",
            pipeline);

        MarkdownNode custom = document.Root.GetChild(0);

        Assert.That(custom.Kind, Is.EqualTo(MarkdownNodeKind.CustomFencedBlock));
        Assert.That(custom.Literal, Is.EqualTo("a < b"));
        Assert.That(custom.GetAttributeValue("info"), Is.EqualTo("chart title=Sales"));
        Assert.That(custom.GetAttributeValue("infoKey"), Is.EqualTo("chart"));
    }

    [Test]
    public void CustomFencedRule_LeavesUnregisteredFencesAsFencedCode()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "```chart\n" +
            "a\n" +
            "```",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
    }

    [Test]
    public void CustomFencedRule_LeavesInvalidInfoKeysAsFencedCode()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomFencedBlock("chart")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse(
            "```bad name\n" +
            "x\n" +
            "```",
            pipeline);

        Assert.That(document.HasErrors(), Is.False);
        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(document.Root.GetChild(0).GetAttributeValue("info"), Is.EqualTo("bad name"));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("x"));
    }

    [Test]
    public void CustomFencedRule_CanOverrideDiagramInfoString()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomFencedBlock("mermaid")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse(
            "```mermaid\n" +
            "graph TD\n" +
            "```",
            pipeline);

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CustomFencedBlock));
    }

    [Test]
    public void CustomFencedRule_DoesNotParseWithoutFeature()
    {
        MarkdownOptions options = MarkdownOptions.CommonMarkPreset();
        options.CustomFencedBlocks.Set("chart");
        MarkdownPipeline pipeline = MarkdownPipeline.Create(options);
        MarkdownDocument document = MarkdownParser.Parse(
            "```chart\n" +
            "a\n" +
            "```",
            pipeline);

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
    }

    [Test]
    public void CustomFencedRule_RenderersPreserveSourceAndExposeHtmlMetadata()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomFencedBlock("chart")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse(
            "```chart\n" +
            "a < b\n" +
            "```",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<pre><code class=\"language-chart\" data-custom-fenced=\"chart\">a &lt; b\n" +
            "</code></pre>\n"));
        Assert.That(markdown, Is.EqualTo(
            "```chart\n" +
            "a < b\n" +
            "```\n"));
        Assert.That(plainText, Is.EqualTo("a < b\n"));
    }

    [Test]
    public void CustomFencedRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList rules = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseCommonMark();

        Assert.That(rules.Contains(CustomFencedBlockRules.CustomFencedBlockRuleName), Is.True);
        Assert.That(rules.IndexOf(CustomFencedBlockRules.CustomFencedBlockRuleName), Is.LessThan(rules.IndexOf(DiagramBlockRules.DiagramBlockRuleName)));
        Assert.That(builder.GetBlockRules().Contains(CustomFencedBlockRules.CustomFencedBlockRuleName), Is.False);
    }

    [Test]
    public void CustomFencedMap_CopiesAndCanBeClearedThroughBuilder()
    {
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomFencedBlock("chart");
        MarkdownPipeline pipeline = builder.Build();

        builder.ClearCustomFencedBlocks();

        Assert.That(pipeline.GetOptions().CustomFencedBlocks.Contains("chart"), Is.True);
        Assert.That(builder.GetOptions().CustomFencedBlocks.GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void CustomFencedRule_RejectsInvalidArguments()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.AdvancedPreset());
        MarkdownCustomFencedBlockMap map = new MarkdownCustomFencedBlockMap();

        Assert.That(() => CustomFencedBlockRules.RegisterCustomFencedBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomFencedBlockRules.TryParseCustomFencedBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomFencedBlockRules.IsValidInfoKey(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownCustomFencedBlockRegistration.Create(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownCustomFencedBlockRegistration.Create("bad name"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => map.Set(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.IndexOf(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Contains("bad name"), Throws.TypeOf<ArgumentException>());
        Assert.That(CustomFencedBlockRules.TryParseCustomFencedBlock(context), Is.False);
        Assert.That(CustomFencedBlockRules.IsValidInfoKey("chart_1"), Is.True);
    }
}
