using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class TypographyProcessorTests
{
    [Test]
    public void TypographyProcessor_ReplacesQuotesDashesEllipsesAndSymbols()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "\"Hello\", it's 'fine'... wait -- no --- yes (c) (R) (tm)",
            MarkdownPipeline.AdvancedPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.AdvancedPreset());
        string markdown = MarkdownRenderer.Render(document, MarkdownPipeline.AdvancedPreset());
        string plainText = PlainTextRenderer.Render(document, MarkdownPipeline.AdvancedPreset());

        Assert.That(html, Is.EqualTo("<p>“Hello”, it’s ‘fine’… wait – no — yes © ® ™</p>\n"));
        Assert.That(markdown, Is.EqualTo("“Hello”, it’s ‘fine’… wait – no — yes © ® ™\n"));
        Assert.That(plainText, Is.EqualTo("“Hello”, it’s ‘fine’… wait – no — yes © ® ™\n"));
    }

    [Test]
    public void TypographyProcessor_DoesNotRunWithoutTypographyFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "\"Hello\" -- (c)",
            MarkdownPipeline.CommonMarkPreset());

        string html = HtmlRenderer.Render(document);

        Assert.That(html, Is.EqualTo("<p>&quot;Hello&quot; -- (c)</p>\n"));
    }

    [Test]
    public void TypographyProcessor_SkipsLiteralCaptureNodes()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "`\"code\" --`\n" +
            "\n" +
            "$\"math\" --$\n" +
            "\n" +
            "```mermaid\n" +
            "\"diagram\" --\n" +
            "```",
            pipeline);

        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode code = paragraph.GetChild(0);
        MarkdownNode math = document.Root.GetChild(1).GetChild(0);
        MarkdownNode diagram = document.Root.GetChild(2);

        Assert.That(code.Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(code.Literal, Is.EqualTo("\"code\" --"));
        Assert.That(math.Kind, Is.EqualTo(MarkdownNodeKind.MathSpan));
        Assert.That(math.Literal, Is.EqualTo("\"math\" --"));
        Assert.That(diagram.Kind, Is.EqualTo(MarkdownNodeKind.DiagramBlock));
        Assert.That(diagram.Literal, Is.EqualTo("\"diagram\" --"));
    }

    [Test]
    public void TypographyProcessor_TransformsTextInsideInlineContainers()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "**\"strong\"** and *'em'*",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode strongText = paragraph.GetChild(0).GetChild(0);
        MarkdownNode emphasisText = paragraph.GetChild(2).GetChild(0);

        Assert.That(strongText.Literal, Is.EqualTo("“strong”"));
        Assert.That(emphasisText.Literal, Is.EqualTo("‘em’"));
    }

    [Test]
    public void TypographyProcessor_HelperMethodsClassifyPatterns()
    {
        Assert.That(TypographyProcessor.ApplyText("a---b--c..."), Is.EqualTo("a—b–c…"));
        Assert.That(TypographyProcessor.StartsWithAt("abcdef", 2, "cd"), Is.True);
        Assert.That(TypographyProcessor.StartsWithIgnoreCaseAt("(TM)", 0, "(tm)"), Is.True);
        Assert.That(TypographyProcessor.IsApostrophe("it's", 2), Is.True);
        Assert.That(TypographyProcessor.IsApostrophe("'quote'", 0), Is.False);
        Assert.That(TypographyProcessor.ShouldProcessChildren(MarkdownNode.Create(MarkdownNodeKind.Paragraph)), Is.True);
        Assert.That(TypographyProcessor.ShouldProcessChildren(MarkdownNode.Create(MarkdownNodeKind.CodeSpan)), Is.False);
    }

    [Test]
    public void TypographyProcessor_RejectsInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();

        Assert.That(() => TypographyProcessor.Apply(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.ApplyNode(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.ApplyText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.ShouldProcessChildren(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.IsApostrophe(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.IsApostrophe("x", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => TypographyProcessor.StartsWithAt(null!, 0, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.StartsWithAt("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.StartsWithAt("x", -1, "x"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => TypographyProcessor.StartsWithIgnoreCaseAt(null!, 0, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.StartsWithIgnoreCaseAt("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => TypographyProcessor.StartsWithIgnoreCaseAt("x", -1, "x"), Throws.TypeOf<ArgumentOutOfRangeException>());

        TypographyProcessor.Apply(document);
        Assert.That(document.Root.GetChildCount(), Is.EqualTo(0));
    }
}
