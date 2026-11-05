using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class AdmonitionBlockRulesTests
{
    [Test]
    public void AdmonitionBlockRule_ParsesGitHubAlertBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "> [!WARNING]\n" +
            "> Be **careful**.\n" +
            "> Second line.",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode admonition = document.Root.GetChild(0);

        Assert.That(admonition.Kind, Is.EqualTo(MarkdownNodeKind.AdmonitionBlock));
        Assert.That(admonition.GetAttributeValue("style"), Is.EqualTo("alert"));
        Assert.That(admonition.GetAttributeValue("type"), Is.EqualTo("warning"));
        Assert.That(admonition.GetAttributeValue("title"), Is.EqualTo("Warning"));
        Assert.That(admonition.Literal, Is.EqualTo("Be **careful**.\nSecond line."));
        Assert.That(admonition.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(admonition.GetChild(0).GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
    }

    [Test]
    public void AdmonitionBlockRule_ParsesBangAdmonitionBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "!!! tip \"Remember\"\n" +
            "    Use **this**.\n" +
            "\n" +
            "after",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode admonition = document.Root.GetChild(0);

        Assert.That(admonition.Kind, Is.EqualTo(MarkdownNodeKind.AdmonitionBlock));
        Assert.That(admonition.GetAttributeValue("style"), Is.EqualTo("admonition"));
        Assert.That(admonition.GetAttributeValue("type"), Is.EqualTo("tip"));
        Assert.That(admonition.GetAttributeValue("title"), Is.EqualTo("Remember"));
        Assert.That(admonition.Literal, Is.EqualTo("Use **this**."));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void AdmonitionBlockRule_DoesNotParseWithoutFeature()
    {
        MarkdownDocument alert = MarkdownParser.Parse(
            "> [!NOTE]\n" +
            "> text",
            MarkdownPipeline.CommonMarkPreset());
        MarkdownDocument admonition = MarkdownParser.Parse(
            "!!! note\n" +
            "    text",
            MarkdownPipeline.CommonMarkPreset());

        Assert.That(alert.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(admonition.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void AdmonitionBlockRule_RenderersOutputContainersAndRoundTripMarkdown()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "> [!NOTE]\n" +
            "> Alert text.\n" +
            "\n" +
            "!!! warning \"Careful\"\n" +
            "    Warn text.",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<div class=\"admonition note\">\n" +
            "<p class=\"admonition-title\">Note</p>\n" +
            "<p>Alert text.</p>\n" +
            "</div>\n" +
            "<div class=\"admonition warning\">\n" +
            "<p class=\"admonition-title\">Careful</p>\n" +
            "<p>Warn text.</p>\n" +
            "</div>\n"));
        Assert.That(markdown, Is.EqualTo(
            "> [!NOTE]\n" +
            "> Alert text.\n" +
            "\n" +
            "!!! warning \"Careful\"\n" +
            "    Warn text.\n"));
        Assert.That(plainText, Is.EqualTo(
            "Alert text.\n" +
            "\n" +
            "Warn text.\n"));
    }

    [Test]
    public void AdmonitionBlockRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList rules = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseCommonMark();

        Assert.That(rules.Contains(AdmonitionBlockRules.AdmonitionBlockRuleName), Is.True);
        Assert.That(rules.IndexOf(AdmonitionBlockRules.AdmonitionBlockRuleName), Is.LessThan(rules.IndexOf(CommonMarkBlockRules.BlockQuoteRuleName)));
        Assert.That(builder.GetBlockRules().Contains(AdmonitionBlockRules.AdmonitionBlockRuleName), Is.False);
    }

    [Test]
    public void AdmonitionBlockRule_HelperMethodsClassifyStartsAndTitles()
    {
        MarkdownLine alert = new MarkdownLine();
        MarkdownLine admonition = new MarkdownLine();

        alert.Text = "> [!TIP] Custom title";
        admonition.Text = "!!! warning \"Careful\"";

        Assert.That(AdmonitionBlockRules.IsAlertStartLine(alert), Is.True);
        Assert.That(AdmonitionBlockRules.GetAlertType(alert), Is.EqualTo("tip"));
        Assert.That(AdmonitionBlockRules.GetAlertTitle(alert), Is.EqualTo("Custom title"));
        Assert.That(AdmonitionBlockRules.IsAlertType("IMPORTANT"), Is.True);
        Assert.That(AdmonitionBlockRules.IsBangAdmonitionStartLine(admonition), Is.True);
        Assert.That(AdmonitionBlockRules.GetBangAdmonitionType(admonition), Is.EqualTo("warning"));
        Assert.That(AdmonitionBlockRules.GetBangAdmonitionTitle(admonition), Is.EqualTo("Careful"));
        Assert.That(AdmonitionBlockRules.ToTitleCase("caution"), Is.EqualTo("Caution"));
        Assert.That(AdmonitionBlockRules.TrimLeadingSpaces("  text"), Is.EqualTo("text"));
        Assert.That(AdmonitionBlockRules.TrimSpaces("  text  "), Is.EqualTo("text"));
        Assert.That(AdmonitionBlockRules.StartsWithAt("abcdef", 2, "cd"), Is.True);
    }

    [Test]
    public void AdmonitionBlockRule_RejectsInvalidArguments()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.AdvancedPreset());
        MarkdownLine line = new MarkdownLine();
        List<string> lines = new List<string>();

        Assert.That(() => AdmonitionBlockRules.RegisterAdmonitionBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.TryParseAdmonitionBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.TryParseAlertBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.TryParseBangAdmonitionBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.CreateAdmonitionNode(null!, "note", "Note", new MarkdownSourceSpan(), lines, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.CreateAdmonitionNode("alert", null!, "Note", new MarkdownSourceSpan(), lines, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.CreateAdmonitionNode("alert", "note", null!, new MarkdownSourceSpan(), lines, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.CreateAdmonitionNode("alert", "note", "Note", null!, lines, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.CreateAdmonitionNode("alert", "note", "Note", new MarkdownSourceSpan(), null!, MarkdownPipeline.AdvancedPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.CreateAdmonitionNode("alert", "note", "Note", new MarkdownSourceSpan(), lines, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.IsAlertStartLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.GetAlertType(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.GetAlertTitle(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.IsAlertType(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.IsBangAdmonitionStartLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.GetBangAdmonitionType(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.GetBangAdmonitionTitle(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.TrimTrailingBlankLines(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.TrimLeadingSpaces(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.TrimSpaces(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.ToTitleCase(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.StartsWithAt(null!, 0, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.StartsWithAt("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AdmonitionBlockRules.StartsWithAt("x", -1, "x"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(AdmonitionBlockRules.TryParseAdmonitionBlock(context), Is.False);
        Assert.That(AdmonitionBlockRules.GetAlertType(line), Is.EqualTo(""));
    }
}
