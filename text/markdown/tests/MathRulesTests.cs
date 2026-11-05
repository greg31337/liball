using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MathRulesTests
{
    [Test]
    public void MathRules_ParseMathSpanAndBlock()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "Inline $x < y$ math.\n" +
            "\n" +
            "$$\n" +
            "a^2 + b^2 = c^2\n" +
            "$$\n" +
            "\n" +
            "after",
            pipeline);

        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode mathSpan = paragraph.GetChild(1);
        MarkdownNode mathBlock = document.Root.GetChild(1);

        Assert.That(mathSpan.Kind, Is.EqualTo(MarkdownNodeKind.MathSpan));
        Assert.That(mathSpan.Literal, Is.EqualTo("x < y"));
        Assert.That(mathSpan.GetAttributeValue("delimiter"), Is.EqualTo("$"));
        Assert.That(mathBlock.Kind, Is.EqualTo(MarkdownNodeKind.MathBlock));
        Assert.That(mathBlock.Literal, Is.EqualTo("a^2 + b^2 = c^2"));
        Assert.That(mathBlock.GetAttributeValue("delimiter"), Is.EqualTo("$$"));
        Assert.That(document.Root.GetChild(2).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void MathRules_RenderHtmlMarkdownAndPlainText()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "Inline $x < y$ math.\n" +
            "\n" +
            "$$\n" +
            "a < b\n" +
            "$$",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<p>Inline <span class=\"math\">x &lt; y</span> math.</p>\n" +
            "<div class=\"math\">\n" +
            "a &lt; b\n" +
            "</div>\n"));
        Assert.That(markdown, Is.EqualTo(
            "Inline $x < y$ math.\n" +
            "\n" +
            "$$\n" +
            "a < b\n" +
            "$$\n"));
        Assert.That(plainText, Is.EqualTo(
            "Inline x < y math.\n" +
            "\n" +
            "a < b\n"));
    }

    [Test]
    public void MathRules_DoNotParseWithoutMathFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "$x$\n" +
            "\n" +
            "$$\n" +
            "x\n" +
            "$$",
            MarkdownPipeline.CommonMarkPreset());

        string html = HtmlRenderer.Render(document);

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.MathSpan), Is.EqualTo(null));
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.MathBlock), Is.EqualTo(null));
        Assert.That(html, Is.EqualTo("<p>$x$</p>\n<p>$$\n" + "x\n" + "$$</p>\n"));
    }

    [Test]
    public void MathRules_RequireClosedAndTightMathSpanDelimiters()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "$ x$ $x $ $x\n" +
            "y$ $$z$$",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.MathSpan), Is.EqualTo(null));
    }

    [Test]
    public void MathRules_RequireClosedMathBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "$$\n" +
            "x",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.MathBlock), Is.EqualTo(null));
    }

    [Test]
    public void MathRules_AreRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList blockRules = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownRuleList inlineRules = MarkdownPipeline.AdvancedPreset().GetInlineRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseCommonMark();

        Assert.That(blockRules.Contains(MathBlockRules.MathBlockRuleName), Is.True);
        Assert.That(blockRules.IndexOf(MathBlockRules.MathBlockRuleName), Is.LessThan(blockRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName)));
        Assert.That(inlineRules.Contains(MathInlineRules.MathSpanRuleName), Is.True);
        Assert.That(inlineRules.IndexOf(MathInlineRules.MathSpanRuleName), Is.LessThan(inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName)));
        Assert.That(builder.GetBlockRules().Contains(MathBlockRules.MathBlockRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(MathInlineRules.MathSpanRuleName), Is.False);
    }

    [Test]
    public void MathRules_RejectInvalidArguments()
    {
        MarkdownBlockParseContext blockContext = MarkdownBlockParseContext.Create("", MarkdownPipeline.AdvancedPreset());
        MarkdownInlineParseContext inlineContext = CreateInlineContext("$x$");
        MarkdownLine line = new MarkdownLine();

        Assert.That(() => MathBlockRules.RegisterMathBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathBlockRules.TryParseMathBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathBlockRules.FindClosingDelimiterIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathBlockRules.IsMathBlockDelimiterLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathInlineRules.RegisterMathInlineRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathInlineRules.TryParseMathSpan(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathInlineRules.FindClosingDollar(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathInlineRules.FindClosingDollar("x", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MathInlineRules.HasValidMathSpanContent(null!, 0, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathInlineRules.HasValidMathSpanContent("x", -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MathInlineRules.HasValidMathSpanContent("x", 0, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MathInlineRules.IsAdjacentToDollar(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MathInlineRules.IsAdjacentToDollar("$", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(MathBlockRules.TryParseMathBlock(blockContext), Is.False);
        Assert.That(MathBlockRules.IsMathBlockDelimiterLine(line), Is.False);
        Assert.That(MathInlineRules.TryParseMathSpan(inlineContext), Is.True);
    }

    static MarkdownInlineParseContext CreateInlineContext(string source)
    {
        MarkdownParseContext parseContext = MarkdownParseContext.Create("", MarkdownPipeline.AdvancedPreset());
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        paragraph.SetLiteral(source);
        parseContext.Document.Root.AppendChild(paragraph);
        return MarkdownInlineParseContext.Create(parseContext, paragraph);
    }

    static MarkdownNode? FindFirstKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return node;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode? result = FindFirstKind(node.GetChild(index), kind);
            if (result != null)
            {
                return result;
            }

            index = index + 1;
        }

        return null;
    }
}
