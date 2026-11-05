using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class CustomInlineDelimiterRulesTests
{
    [Test]
    public void CustomInlineRule_ParsesRegisteredDelimiterWithNestedInlineContent()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomInlineDelimiter("kbd", "%%", "%%")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("Press %%**Ctrl** + C%% now.", pipeline);

        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode custom = paragraph.GetChild(1);

        Assert.That(custom.Kind, Is.EqualTo(MarkdownNodeKind.CustomInline));
        Assert.That(custom.GetAttributeValue("name"), Is.EqualTo("kbd"));
        Assert.That(custom.GetAttributeValue("openingDelimiter"), Is.EqualTo("%%"));
        Assert.That(custom.GetAttributeValue("closingDelimiter"), Is.EqualTo("%%"));
        Assert.That(custom.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
    }

    [Test]
    public void CustomInlineRule_RendersHtmlMarkdownAndPlainText()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomInlineDelimiter("kbd", "%%", "%%")
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("Press %%Ctrl%%.", pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>Press <span class=\"custom-inline kbd\">Ctrl</span>.</p>\n"));
        Assert.That(markdown, Is.EqualTo("Press %%Ctrl%%.\n"));
        Assert.That(plainText, Is.EqualTo("Press Ctrl.\n"));
    }

    [Test]
    public void CustomInlineRule_DoesNotParseUnregisteredDelimiter()
    {
        MarkdownDocument document = MarkdownParser.Parse("Press %%Ctrl%%.", MarkdownPipeline.AdvancedPreset());

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.CustomInline), Is.EqualTo(null));
    }

    [Test]
    public void CustomInlineRule_DoesNotParseWithoutFeature()
    {
        MarkdownOptions options = MarkdownOptions.CommonMarkPreset();
        options.CustomInlineDelimiters.Set("kbd", "%%", "%%");
        MarkdownPipeline pipeline = MarkdownPipeline.Create(options);
        MarkdownDocument document = MarkdownParser.Parse("Press %%Ctrl%%.", pipeline);

        Assert.That(FindFirstKind(document.Root, MarkdownNodeKind.CustomInline), Is.EqualTo(null));
    }

    [Test]
    public void CustomInlineRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList rules = MarkdownPipeline.AdvancedPreset().GetInlineRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseCommonMark();

        Assert.That(rules.Contains(CustomInlineDelimiterRules.CustomInlineDelimiterRuleName), Is.True);
        Assert.That(rules.IndexOf(CustomInlineDelimiterRules.CustomInlineDelimiterRuleName), Is.LessThan(rules.IndexOf(CommonMarkInlineRules.TextRuleName)));
        Assert.That(builder.GetInlineRules().Contains(CustomInlineDelimiterRules.CustomInlineDelimiterRuleName), Is.False);
    }

    [Test]
    public void CustomInlineMap_CopiesAndCanBeClearedThroughBuilder()
    {
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset()
            .RegisterCustomInlineDelimiter("kbd", "%%", "%%");
        MarkdownPipeline pipeline = builder.Build();

        builder.ClearCustomInlineDelimiters();

        Assert.That(pipeline.GetOptions().CustomInlineDelimiters.GetCount(), Is.EqualTo(1));
        Assert.That(builder.GetOptions().CustomInlineDelimiters.GetCount(), Is.EqualTo(0));
    }

    [Test]
    public void CustomInlineRule_RejectsInvalidArguments()
    {
        MarkdownInlineParseContext context = CreateInlineContext("%%x%%");
        MarkdownCustomInlineDelimiterMap map = new MarkdownCustomInlineDelimiterMap();

        Assert.That(() => CustomInlineDelimiterRules.RegisterCustomInlineDelimiterRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomInlineDelimiterRules.TryParseCustomInlineDelimiter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomInlineDelimiterRules.FindRegistrationAt(null!, "x", 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomInlineDelimiterRules.FindRegistrationAt(MarkdownPipeline.AdvancedPreset(), null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CustomInlineDelimiterRules.FindRegistrationAt(MarkdownPipeline.AdvancedPreset(), "x", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownCustomInlineDelimiterRegistration.Create(null!, "%%", "%%"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownCustomInlineDelimiterRegistration.Create("bad name", "%%", "%%"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => MarkdownCustomInlineDelimiterRegistration.Create("kbd", null!, "%%"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownCustomInlineDelimiterRegistration.Create("kbd", "", "%%"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => map.Set(null!, "%%", "%%"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Set("kbd", null!, "%%"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.IndexOf(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(CustomInlineDelimiterRules.TryParseCustomInlineDelimiter(context), Is.False);
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
