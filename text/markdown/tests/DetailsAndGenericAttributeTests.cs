using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class DetailsAndGenericAttributeTests
{
    [Test]
    public void DetailsBlock_ParsesTypedDataAndNestedMarkdown()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "???+ details \"More info\"\n" +
            "Content with **bold**.\n" +
            "???",
            pipeline);

        MarkdownNode details = document.Root.GetChild(0);
        MarkdownNode summary = details.GetChild(0);
        MarkdownDetailsData data = details.Data.Details!;

        Assert.That(details.Kind, Is.EqualTo(MarkdownNodeKind.DetailsBlock));
        Assert.That(data.Summary, Is.EqualTo("More info"));
        Assert.That(data.IsOpen, Is.True);
        Assert.That(data.IsSummary, Is.False);
        Assert.That(summary.Kind, Is.EqualTo(MarkdownNodeKind.DetailsSummary));
        Assert.That(summary.Data.Details!.IsSummary, Is.True);
        Assert.That(summary.Literal, Is.EqualTo("More info"));
        Assert.That(details.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(details.GetChild(1).GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
    }

    [Test]
    public void DetailsBlock_RenderersPreserveBehavior()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "??? details \"More\"\n" +
            "Content.\n" +
            "???",
            pipeline);

        Assert.That(HtmlRenderer.Render(document, pipeline), Is.EqualTo(
            "<details>\n" +
            "<summary>More</summary>\n" +
            "<p>Content.</p>\n" +
            "</details>\n"));
        Assert.That(MarkdownRenderer.Render(document, pipeline), Is.EqualTo(
            "??? details \"More\"\n" +
            "Content.\n" +
            "???\n"));
        Assert.That(PlainTextRenderer.Render(document, pipeline), Is.EqualTo(
            "More\n" +
            "Content.\n"));
    }

    [Test]
    public void DetailsBlock_RequiresFeatureAndDoesNotClaimCustomContainers()
    {
        MarkdownPipeline commonMark = MarkdownPipeline.CommonMarkPreset();
        MarkdownPipeline custom = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomContainer("details")
            .Build();

        MarkdownDocument disabled = MarkdownParser.Parse(
            "??? details \"More\"\nContent.\n???",
            commonMark);
        MarkdownDocument container = MarkdownParser.Parse(
            "::: details \"More\"\nContent.\n:::",
            custom);

        Assert.That(FindFirstKind(disabled.Root, MarkdownNodeKind.DetailsBlock), Is.EqualTo(null));
        Assert.That(container.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CustomContainerBlock));
        Assert.That(commonMark.GetFeatures().Details, Is.False);
        Assert.That(MarkdownPipeline.AdvancedPreset().GetFeatures().Details, Is.True);
        Assert.That(commonMark.GetBlockRules().Contains(DetailsBlockRules.DetailsBlockRuleName), Is.False);
        Assert.That(custom.GetBlockRules().Contains(DetailsBlockRules.DetailsBlockRuleName), Is.True);
    }

    [Test]
    public void GenericBlockAttributes_AttachToPreviousBlockAndRender()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "Paragraph.\n" +
            "{#intro .lead data-mode=\"wide\"}",
            pipeline);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetAttributeValue("id"), Is.EqualTo("intro"));
        Assert.That(paragraph.GetAttributeValue("class"), Is.EqualTo("lead"));
        Assert.That(paragraph.GetAttributeValue("data-mode"), Is.EqualTo("wide"));
        Assert.That(paragraph.Data.AttachedAttributes.IsBlock, Is.True);
        Assert.That(pipeline.GetBlockRules().Contains(GenericAttributeRules.BlockRuleName), Is.True);
        Assert.That(pipeline.GetInlineRules().Contains(GenericAttributeRules.InlineRuleName), Is.True);
        Assert.That(HtmlRenderer.Render(document, pipeline), Is.EqualTo(
            "<p id=\"intro\" class=\"lead\" data-mode=\"wide\">Paragraph.</p>\n"));
        Assert.That(MarkdownRenderer.Render(document, pipeline), Is.EqualTo(
            "Paragraph.\n" +
            "{#intro .lead data-mode=\"wide\"}\n"));
    }

    [Test]
    public void GenericInlineAttributes_AttachToPreviousInlineAndRender()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "This is *important*{#key .accent data-level=\"1\"}.",
            pipeline);
        MarkdownNode emphasis = document.Root.GetChild(0).GetChild(1);

        Assert.That(emphasis.Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(emphasis.GetAttributeValue("id"), Is.EqualTo("key"));
        Assert.That(emphasis.GetAttributeValue("class"), Is.EqualTo("accent"));
        Assert.That(emphasis.GetAttributeValue("data-level"), Is.EqualTo("1"));
        Assert.That(emphasis.Data.AttachedAttributes.IsBlock, Is.False);
        Assert.That(HtmlRenderer.Render(document, pipeline), Is.EqualTo(
            "<p>This is <em id=\"key\" class=\"accent\" data-level=\"1\">important</em>.</p>\n"));
        Assert.That(MarkdownRenderer.Render(document, pipeline), Is.EqualTo(
            "This is *important*{#key .accent data-level=\"1\"}.\n"));
    }

    [Test]
    public void GenericAttributes_RequireAttributeFeature()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        MarkdownDocument document = MarkdownParser.Parse("*text*{.accent}", pipeline);
        MarkdownNode emphasis = document.Root.GetChild(0).GetChild(0);

        Assert.That(emphasis.Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(emphasis.GetAttributeValue("class"), Is.EqualTo(""));
        Assert.That(HtmlRenderer.Render(document, pipeline), Is.EqualTo(
            "<p><em>text</em>{.accent}</p>\n"));
    }

    private static MarkdownNode? FindFirstKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode? found = FindFirstKind(node.GetChild(index), kind);
            if (found != null)
            {
                return found;
            }

            index = index + 1;
        }

        return null;
    }
}
