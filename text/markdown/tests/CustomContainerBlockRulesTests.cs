using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class CustomContainerBlockRulesTests
{
    [Test]
    public void CustomContainerRule_ParsesRegisteredContainer()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomContainer("details")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse(
            "::: details \"More info\"\n" +
            "Content with **bold**.\n" +
            ":::\n" +
            "\n" +
            "after",
            pipeline);

        MarkdownNode container = document.Root.GetChild(0);

        Assert.That(container.Kind, Is.EqualTo(MarkdownNodeKind.CustomContainerBlock));
        Assert.That(container.GetAttributeValue("name"), Is.EqualTo("details"));
        Assert.That(container.GetAttributeValue("title"), Is.EqualTo("More info"));
        Assert.That(container.Literal, Is.EqualTo("Content with **bold**."));
        Assert.That(container.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(container.GetChild(0).GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void CustomContainerRule_DoesNotParseUnregisteredContainer()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "::: details\n" +
            "Content\n" +
            ":::",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void CustomContainerRule_DoesNotParseWithoutFeature()
    {
        MarkdownOptions options = MarkdownOptions.CommonMarkPreset();
        options.CustomContainers.Set("details");
        MarkdownPipeline pipeline = MarkdownPipeline.Create(options);
        MarkdownDocument document = MarkdownParser.Parse(
            "::: details\n" +
            "Content\n" +
            ":::",
            pipeline);

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void CustomContainerRule_AllowsUnclosedContainerToEndAtDocumentEnd()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomContainer("panel")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse(
            "::: panel\n" +
            "Content",
            pipeline);

        MarkdownNode container = document.Root.GetChild(0);

        Assert.That(container.Kind, Is.EqualTo(MarkdownNodeKind.CustomContainerBlock));
        Assert.That(container.Literal, Is.EqualTo("Content"));
    }

    [Test]
    public void CustomContainerRule_RenderersOutputContainersAndRoundTripMarkdown()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomContainer("details")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse(
            "::: details \"More\"\n" +
            "Content.\n" +
            ":::",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<div class=\"custom-container details\">\n" +
            "<p class=\"custom-container-title\">More</p>\n" +
            "<p>Content.</p>\n" +
            "</div>\n"));
        Assert.That(markdown, Is.EqualTo(
            "::: details \"More\"\n" +
            "Content.\n" +
            ":::\n"));
        Assert.That(plainText, Is.EqualTo("Content.\n"));
    }

    [Test]
    public void CustomContainerRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList rules = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseGfm();

        Assert.That(rules.Contains(CustomContainerBlockRules.CustomContainerBlockRuleName), Is.True);
        Assert.That(rules.IndexOf(CustomContainerBlockRules.CustomContainerBlockRuleName), Is.LessThan(rules.IndexOf(CommonMarkBlockRules.ParagraphRuleName)));
        Assert.That(builder.GetBlockRules().Contains(CustomContainerBlockRules.CustomContainerBlockRuleName), Is.False);
    }

    [Test]
    public void CustomContainerMap_CopiesAndCanBeClearedThroughBuilder()
    {
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomContainer("details");
        MarkdownPipeline pipeline = builder.Build();

        builder.ClearCustomContainers();

        Assert.That(pipeline.GetOptions().CustomContainers.Contains("details"), Is.True);
        Assert.That(builder.GetOptions().CustomContainers.GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void CustomContainerRule_HelperMethodsClassifyContainers()
    {
        MarkdownLine opening = new MarkdownLine();
        MarkdownLine closing = new MarkdownLine();

        opening.Text = "::: details \"More\"";
        closing.Text = ":::";

        Assert.That(CustomContainerBlockRules.IsContainerOpeningLine(opening), Is.True);
        Assert.That(CustomContainerBlockRules.GetContainerFenceLength(opening), Is.EqualTo(3));
        Assert.That(CustomContainerBlockRules.GetContainerName(opening), Is.EqualTo("details"));
        Assert.That(CustomContainerBlockRules.GetContainerTitle(opening), Is.EqualTo("More"));
        Assert.That(CustomContainerBlockRules.IsContainerClosingLine(closing, 3), Is.True);
        Assert.That(CustomContainerBlockRules.IsValidContainerName("panel_1"), Is.True);
        Assert.That(CustomContainerBlockRules.IsValidContainerName("bad name"), Is.False);
    }

    [Test]
    public void CustomContainerRule_RejectsInvalidArguments()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.AdvancedPreset());
        MarkdownLine line = new MarkdownLine();
        List<string> lines = new List<string>();
        MarkdownCustomContainerMap map = new MarkdownCustomContainerMap();

        Assert.That(() => CustomContainerBlockRules.RegisterCustomContainerBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.TryParseCustomContainerBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.CreateContainerNode(null!, "", new MarkdownSourceSpan(), lines, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.CreateContainerNode("details", null!, new MarkdownSourceSpan(), lines, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.CreateContainerNode("details", "", null!, lines, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.CreateContainerNode("details", "", new MarkdownSourceSpan(), null!, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.CreateContainerNode("details", "", new MarkdownSourceSpan(), lines, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.IsContainerOpeningLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.GetContainerFenceLength(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.GetContainerName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.GetContainerTitle(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.IsContainerClosingLine(null!, 3), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomContainerBlockRules.IsContainerClosingLine(line, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CustomContainerBlockRules.IsValidContainerName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownCustomContainerRegistration.Create(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownCustomContainerRegistration.Create("bad name"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => map.Set(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.IndexOf(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Contains("bad name"), Throws.TypeOf<ArgumentException>());
        Assert.That(CustomContainerBlockRules.TryParseCustomContainerBlock(context), Is.False);
        Assert.That(CustomContainerBlockRules.GetContainerName(line), Is.EqualTo(""));
    }
}
