using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class TextFormattingInlineRulesTests
{
    [Test]
    public void TextFormattingRule_ParsesAdvancedInlineFormatting()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "H^2^O ~sub~ ++ins++ ==mark==",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("H"));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Superscript));
        Assert.That(paragraph.GetChild(1).GetChild(0).Literal, Is.EqualTo("2"));
        Assert.That(paragraph.GetChild(3).Kind, Is.EqualTo(MarkdownNodeKind.Subscript));
        Assert.That(paragraph.GetChild(5).Kind, Is.EqualTo(MarkdownNodeKind.InsertedText));
        Assert.That(paragraph.GetChild(7).Kind, Is.EqualTo(MarkdownNodeKind.MarkedText));
    }

    [Test]
    public void TextFormattingRule_RendersHtmlMarkdownAndPlainText()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "H^2^O ~sub~ ++ins++ ==mark==",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>H<sup>2</sup>O <sub>sub</sub> <ins>ins</ins> <mark>mark</mark></p>\n"));
        Assert.That(markdown, Is.EqualTo("H^2^O ~sub~ ++ins++ ==mark==\n"));
        Assert.That(plainText, Is.EqualTo("H2O sub ins mark\n"));
    }

    [Test]
    public void TextFormattingRule_ParsesNestedInlineContent()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "^**bold**^ and ==*em*==",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode superscript = paragraph.GetChild(0);
        MarkdownNode marked = paragraph.GetChild(2);

        Assert.That(superscript.Kind, Is.EqualTo(MarkdownNodeKind.Superscript));
        Assert.That(superscript.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
        Assert.That(marked.Kind, Is.EqualTo(MarkdownNodeKind.MarkedText));
        Assert.That(marked.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
    }

    [Test]
    public void TextFormattingRule_DoesNotParseWithoutAdvancedFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "H^2^O ~sub~ ++ins++ ==mark==",
            MarkdownPipeline.CommonMarkPreset());

        string html = HtmlRenderer.Render(document);

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Superscript), Is.EqualTo(null));
        Assert.That(html, Is.EqualTo("<p>H^2^O ~sub~ ++ins++ ==mark==</p>\n"));
    }

    [Test]
    public void TextFormattingRule_LeavesUnmatchedOrEmptyDelimitersAsText()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "^ ^ ^^ ~ ~ ++ ++ == ==",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Superscript), Is.EqualTo(null));
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Subscript), Is.EqualTo(null));
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.InsertedText), Is.EqualTo(null));
        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.MarkedText), Is.EqualTo(null));
    }

    [Test]
    public void TextFormattingRule_DoesNotStealGfmStrikethrough()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "~~del~~ ~sub~",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Strikethrough));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(MarkdownNodeKind.Subscript));
    }

    [Test]
    public void TextFormattingRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList advancedRules = MarkdownPipeline.AdvancedPreset().GetInlineRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseCommonMark();

        Assert.That(advancedRules.Contains(TextFormattingInlineRules.TextFormattingRuleName), Is.True);
        Assert.That(advancedRules.IndexOf(TextFormattingInlineRules.TextFormattingRuleName), Is.LessThan(advancedRules.IndexOf(CommonMarkInlineRules.TextRuleName)));
        Assert.That(builder.GetInlineRules().Contains(TextFormattingInlineRules.TextFormattingRuleName), Is.False);
    }

    [Test]
    public void TextFormattingRule_RejectsInvalidArguments()
    {
        MarkdownInlineParseContext context = CreateInlineContext("^x^");
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Superscript);
        HtmlRenderContext htmlContext = new HtmlRenderContext(new HtmlWriter(new StringWriter()), MarkdownPipeline.AdvancedPreset());
        MarkdownRenderContext markdownContext = new MarkdownRenderContext(new MarkdownWriter(new StringWriter()), MarkdownPipeline.AdvancedPreset());

        Assert.That(() => TextFormattingInlineRules.RegisterTextFormattingInlineRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.TryParseTextFormatting(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.TryParseDelimitedInline(null!, "^", MarkdownNodeKind.Superscript), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.TryParseDelimitedInline(context, null!, MarkdownNodeKind.Superscript), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.TryParseDelimitedInline(context, "^", MarkdownNodeKind.Paragraph), Throws.TypeOf<ArgumentException>());
        Assert.That(() => TextFormattingInlineRules.AppendDelimitedInline(null!, MarkdownNodeKind.Superscript, "^", 0, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.AppendDelimitedInline(context, MarkdownNodeKind.Superscript, null!, 0, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.AppendDelimitedInline(context, MarkdownNodeKind.Paragraph, "^", 0, 1), Throws.TypeOf<ArgumentException>());
        Assert.That(() => TextFormattingInlineRules.AppendDelimitedInline(context, MarkdownNodeKind.Superscript, "^", -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => TextFormattingInlineRules.AppendDelimitedInline(context, MarkdownNodeKind.Superscript, "^", 0, 99), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => TextFormattingInlineRules.FindClosingDelimiter(null!, 0, "^"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.FindClosingDelimiter("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.FindClosingDelimiter("x", -1, "^"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => TextFormattingInlineRules.FindClosingDelimiter("x", 0, ""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => TextFormattingInlineRules.StartsWithAt(null!, 0, "^"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.StartsWithAt("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.StartsWithAt("x", -1, "^"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => TextFormattingInlineRules.HasNonWhitespaceContent(null!, 0, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TextFormattingInlineRules.HasNonWhitespaceContent("x", -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => TextFormattingInlineRules.HasNonWhitespaceContent("x", 0, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownNodeKindFacts.ValidateInlineExtensionKind(MarkdownNodeKind.Paragraph), Throws.TypeOf<ArgumentException>());
        Assert.That(() => HtmlRenderer.RenderWrappedInline(null!, node, "sup"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderWrappedInline(htmlContext, null!, "sup"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderWrappedInline(htmlContext, node, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderWrappedInline(null!, node, "^", "^"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderWrappedInline(markdownContext, null!, "^", "^"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderWrappedInline(markdownContext, node, null!, "^"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderWrappedInline(markdownContext, node, "^", null!), Throws.TypeOf<ArgumentNullException>());
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
