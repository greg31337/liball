using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class EmojiInlineRulesTests
{
    [Test]
    public void EmojiRule_CapturesShortcodeAndPreservesByDefault()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "Hello :smile: :thumbs-up:",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode firstEmoji = paragraph.GetChild(1);
        MarkdownNode secondEmoji = paragraph.GetChild(3);

        Assert.That(firstEmoji.Kind, Is.EqualTo(MarkdownNodeKind.Emoji));
        Assert.That(firstEmoji.Literal, Is.EqualTo(":smile:"));
        Assert.That(firstEmoji.GetAttributeValue("name"), Is.EqualTo("smile"));
        Assert.That(firstEmoji.GetAttributeValue("replacement"), Is.EqualTo(""));
        Assert.That(secondEmoji.Kind, Is.EqualTo(MarkdownNodeKind.Emoji));
        Assert.That(secondEmoji.GetAttributeValue("name"), Is.EqualTo("thumbs-up"));
    }

    [Test]
    public void EmojiRule_UsesOptionalMappingForHtmlAndPlainText()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .MapEmojiShortcode("smile", "SMILE")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("Hello :smile:", pipeline);

        MarkdownNode emoji = document.Root.GetChild(0).GetChild(1);
        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(emoji.GetAttributeValue("replacement"), Is.EqualTo("SMILE"));
        Assert.That(html, Is.EqualTo("<p>Hello SMILE</p>\n"));
        Assert.That(markdown, Is.EqualTo("Hello :smile:\n"));
        Assert.That(plainText, Is.EqualTo("Hello SMILE\n"));
    }

    [Test]
    public void EmojiRule_DoesNotParseWithoutEmojiFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "Hello :smile:",
            MarkdownPipeline.CommonMarkPreset());

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Emoji), Is.EqualTo(null));
        Assert.That(HtmlRenderer.Render(document), Is.EqualTo("<p>Hello :smile:</p>\n"));
    }

    [Test]
    public void EmojiRule_LeavesInvalidShortcodesAsText()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            ": :bad name: :bad.name: :missing",
            MarkdownPipeline.AdvancedPreset());

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.Emoji), Is.EqualTo(null));
    }

    [Test]
    public void EmojiRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList inlineRules = MarkdownPipeline.AdvancedPreset().GetInlineRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseGfm();

        Assert.That(inlineRules.Contains(EmojiInlineRules.EmojiRuleName), Is.True);
        Assert.That(inlineRules.IndexOf(EmojiInlineRules.EmojiRuleName), Is.LessThan(inlineRules.IndexOf(CommonMarkInlineRules.TextRuleName)));
        Assert.That(builder.GetInlineRules().Contains(EmojiInlineRules.EmojiRuleName), Is.False);
    }

    [Test]
    public void EmojiMap_CopiesAndCanBeClearedThroughBuilder()
    {
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset()
            .MapEmojiShortcode("smile", "SMILE");
        MarkdownPipeline pipeline = builder.Build();

        builder.ClearEmojiShortcodes();

        Assert.That(pipeline.GetOptions().EmojiShortcodes.GetReplacement("smile"), Is.EqualTo("SMILE"));
        Assert.That(builder.GetOptions().EmojiShortcodes.GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void EmojiRule_HelperMethodsClassifyShortcodes()
    {
        Assert.That(EmojiInlineRules.FindEmojiClosingColon(":smile:", 0), Is.EqualTo(6));
        Assert.That(EmojiInlineRules.FindEmojiClosingColon(":bad name:", 0), Is.EqualTo(-1));
        Assert.That(EmojiInlineRules.IsValidEmojiName("thumbs-up"), Is.True);
        Assert.That(EmojiInlineRules.IsValidEmojiName("person+1"), Is.True);
        Assert.That(EmojiInlineRules.IsValidEmojiName("bad.name"), Is.False);
    }

    [Test]
    public void EmojiRule_RejectsInvalidArguments()
    {
        MarkdownInlineParseContext context = CreateInlineContext(":smile:");
        MarkdownEmojiMap map = new MarkdownEmojiMap();

        Assert.That(() => EmojiInlineRules.RegisterEmojiInlineRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => EmojiInlineRules.TryParseEmoji(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => EmojiInlineRules.FindEmojiClosingColon(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => EmojiInlineRules.FindEmojiClosingColon(":x:", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => EmojiInlineRules.IsValidEmojiName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEmojiMapping.Create(null!, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownEmojiMapping.Create("bad name", "x"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownEmojiMapping.Create("ok", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Set(null!, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Set("ok", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.IndexOf(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.GetReplacement("bad name"), Throws.TypeOf<ArgumentException>());
        Assert.That(EmojiInlineRules.TryParseEmoji(context), Is.True);
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
