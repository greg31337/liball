using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class GfmInlineRulesTests
{
    [Test]
    public void StrikethroughRule_ParsesInGfmPipeline()
    {
        MarkdownDocument document = MarkdownParser.Parse("a ~~gone *em*~~ b", MarkdownPipeline.GfmPreset());
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode strike = paragraph.GetChild(1);

        Assert.That(strike.Kind, Is.EqualTo(MarkdownNodeKind.Strikethrough));
        Assert.That(strike.GetChild(0).Literal, Is.EqualTo("gone "));
        Assert.That(strike.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo(" b"));
    }

    [Test]
    public void StrikethroughRule_DoesNotParseWithoutGfmFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse("a ~~gone~~ b", MarkdownPipeline.CommonMarkPreset());

        Assert.That(ContainsInlineKind(document.Root, MarkdownNodeKind.Strikethrough), Is.False);
    }

    [Test]
    public void StrikethroughRule_LeavesUnmatchedDelimiterAsText()
    {
        MarkdownDocument document = MarkdownParser.Parse("a ~~gone b", MarkdownPipeline.GfmPreset());

        Assert.That(ContainsInlineKind(document.Root, MarkdownNodeKind.Strikethrough), Is.False);
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("a ~~gone b"));
    }

    [Test]
    public void StrikethroughRule_AcceptsOneOrTwoTildesAndRejectsLongerRuns()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.GfmPreset();

        Assert.That(Markdown.ToHtml("~one~ and ~~two~~", pipeline), Is.EqualTo(
            "<p><del>one</del> and <del>two</del></p>\n"));
        Assert.That(Markdown.ToHtml("a ~~~three~~~ b", pipeline), Is.EqualTo(
            "<p>a ~~~three~~~ b</p>\n"));
    }

    [Test]
    public void StrikethroughRule_IsRegisteredBeforeTextInGfmPipelines()
    {
        MarkdownRuleList rules = MarkdownPipeline.GfmPreset().GetInlineRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.GfmPreset();

        Assert.That(rules.Contains(GfmInlineRules.StrikethroughRuleName), Is.True);
        Assert.That(rules.IndexOf(GfmInlineRules.StrikethroughRuleName), Is.LessThan(rules.IndexOf(CommonMarkInlineRules.TextRuleName)));
        Assert.That(rules.Contains(GfmInlineRules.BareAutolinkRuleName), Is.True);
        Assert.That(rules.IndexOf(GfmInlineRules.BareAutolinkRuleName), Is.LessThan(rules.IndexOf(CommonMarkInlineRules.TextRuleName)));
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(GfmInlineRules.StrikethroughRuleName), Is.False);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(GfmInlineRules.BareAutolinkRuleName), Is.False);

        builder.UseCommonMark();
        Assert.That(builder.GetInlineRules().Contains(GfmInlineRules.StrikethroughRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(GfmInlineRules.BareAutolinkRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(GfmBlockRules.TableRuleName), Is.False);
    }

    [Test]
    public void StrikethroughRule_RendersHtmlAndMarkdown()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.GfmPreset();

        Assert.That(Markdown.ToHtml("a ~~gone *em*~~", pipeline), Is.EqualTo("<p>a <del>gone <em>em</em></del></p>\n"));
        Assert.That(Markdown.ToMarkdown("a ~~gone *em*~~", pipeline), Is.EqualTo("a ~~gone *em*~~\n"));
        Assert.That(Markdown.ToPlainText("a ~~gone *em*~~", pipeline), Is.EqualTo("a gone em\n"));
    }

    [Test]
    public void StrikethroughRule_HelperMethodsFindDelimiters()
    {
        Assert.That(GfmInlineRules.FindClosingStrikethroughDelimiter("a ~~b~~ c", 4), Is.EqualTo(5));
        Assert.That(GfmInlineRules.FindClosingStrikethroughDelimiter("a ~~b", 4), Is.EqualTo(-1));
    }

    [Test]
    public void StrikethroughRule_RejectsInvalidArguments()
    {
        MarkdownInlineParseContext context = MarkdownInlineParseContext.Create(
            new MarkdownParseContext(),
            MarkdownNode.Create(MarkdownNodeKind.Paragraph),
            "~~x~~");

        Assert.That(() => GfmInlineRules.RegisterGfmInlineRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.TryParseStrikethrough(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.AppendStrikethroughInline(null!, 0, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.AppendStrikethroughInline(context, -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => GfmInlineRules.AppendStrikethroughInline(context, 1, 99), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => GfmInlineRules.FindClosingStrikethroughDelimiter(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.FindClosingStrikethroughDelimiter("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => HtmlRenderer.RenderStrikethrough(null!, MarkdownNode.Create(MarkdownNodeKind.Strikethrough)), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderStrikethrough(new HtmlRenderContext(new HtmlWriter(new StringWriter()), MarkdownPipeline.GfmPreset()), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderStrikethrough(null!, MarkdownNode.Create(MarkdownNodeKind.Strikethrough)), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderStrikethrough(new MarkdownRenderContext(new MarkdownWriter(new StringWriter()), MarkdownPipeline.GfmPreset()), null!), Throws.TypeOf<ArgumentNullException>());
    }

    public static bool ContainsInlineKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.Kind == kind)
        {
            return true;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            if (ContainsInlineKind(node.GetChild(index), kind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
