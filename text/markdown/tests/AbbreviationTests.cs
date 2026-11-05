using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class AbbreviationTests
{
    [Test]
    public void AbbreviationRule_ParsesDefinitionAndAppliesInlineAbbreviations()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "*[HTML]: Hyper Text Markup Language\n" +
            "\n" +
            "HTML and **HTML**.",
            pipeline);

        MarkdownNode definition = document.Root.GetChild(0);
        MarkdownNode paragraph = document.Root.GetChild(1);
        MarkdownNode firstAbbreviation = paragraph.GetChild(0);
        MarkdownNode strong = paragraph.GetChild(2);

        Assert.That(definition.Kind, Is.EqualTo(MarkdownNodeKind.AbbreviationDefinition));
        Assert.That(definition.GetAttributeValue("label"), Is.EqualTo("HTML"));
        Assert.That(definition.GetAttributeValue("title"), Is.EqualTo("Hyper Text Markup Language"));
        Assert.That(firstAbbreviation.Kind, Is.EqualTo(MarkdownNodeKind.Abbreviation));
        Assert.That(firstAbbreviation.GetAttributeValue("title"), Is.EqualTo("Hyper Text Markup Language"));
        Assert.That(firstAbbreviation.GetChild(0).Literal, Is.EqualTo("HTML"));
        Assert.That(strong.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Abbreviation));
    }

    [Test]
    public void AbbreviationRule_RendersHtmlMarkdownAndPlainText()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "*[HTML]: Hyper Text Markup Language\n" +
            "\n" +
            "HTML.",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p><abbr title=\"Hyper Text Markup Language\">HTML</abbr>.</p>\n"));
        Assert.That(markdown, Is.EqualTo(
            "*[HTML]: Hyper Text Markup Language\n" +
            "\n" +
            "HTML.\n"));
        Assert.That(plainText, Is.EqualTo("HTML.\n"));
    }

    [Test]
    public void AbbreviationRule_DoesNotParseWithoutAdvancedFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "*[HTML]: Hyper Text Markup Language\n" +
            "\n" +
            "HTML.",
            MarkdownPipeline.CommonMarkPreset());

        string html = HtmlRenderer.Render(document);

        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(html, Is.EqualTo("<p>*[HTML]: Hyper Text Markup Language</p>\n<p>HTML.</p>\n"));
    }

    [Test]
    public void AbbreviationRule_RequiresWordBoundariesAndUsesLongestMatch()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "*[HTML]: Hyper Text Markup Language\n" +
            "*[HTML5]: Hyper Text Markup Language 5\n" +
            "\n" +
            "HTML HTML5 XHTML HTML5a",
            pipeline);

        MarkdownNode paragraph = document.Root.GetChild(2);

        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Abbreviation));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("HTML"));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(MarkdownNodeKind.Abbreviation));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo("HTML5"));
        Assert.That(paragraph.GetChild(3).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(3).Literal, Is.EqualTo(" XHTML HTML5a"));
    }

    [Test]
    public void AbbreviationRule_DoesNotApplyInsideCodeSpans()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "*[HTML]: Hyper Text Markup Language\n" +
            "\n" +
            "`HTML` HTML",
            pipeline);

        MarkdownNode paragraph = document.Root.GetChild(1);

        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(MarkdownNodeKind.Abbreviation));
    }

    [Test]
    public void AbbreviationRule_IsRegisteredForAdvancedPipelines()
    {
        MarkdownRuleList advancedRules = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        builder.UseGfm();

        Assert.That(advancedRules.Contains(AbbreviationBlockRules.DefinitionRuleName), Is.True);
        Assert.That(advancedRules.IndexOf(AbbreviationBlockRules.DefinitionRuleName), Is.LessThan(advancedRules.IndexOf(CommonMarkBlockRules.ParagraphRuleName)));
        Assert.That(builder.GetBlockRules().Contains(AbbreviationBlockRules.DefinitionRuleName), Is.False);
    }

    [Test]
    public void AbbreviationRule_HelperMethodsClassifyDefinitions()
    {
        MarkdownLine definition = new MarkdownLine();
        MarkdownLine emptyTitle = new MarkdownLine();

        definition.Text = "*[HTML]: Hyper Text Markup Language";
        emptyTitle.Text = "*[HTML]:   ";

        Assert.That(AbbreviationBlockRules.IsAbbreviationDefinitionLine(definition), Is.True);
        Assert.That(AbbreviationBlockRules.GetAbbreviationLabel(definition), Is.EqualTo("HTML"));
        Assert.That(AbbreviationBlockRules.GetAbbreviationTitle(definition), Is.EqualTo("Hyper Text Markup Language"));
        Assert.That(AbbreviationBlockRules.GetAbbreviationMarkerStart(definition), Is.EqualTo(0));
        Assert.That(AbbreviationBlockRules.GetAbbreviationMarkerEnd(definition), Is.EqualTo(8));
        Assert.That(AbbreviationBlockRules.IsAbbreviationDefinitionLine(emptyTitle), Is.False);
    }

    [Test]
    public void AbbreviationRule_RejectsInvalidArguments()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.AdvancedPreset());
        MarkdownLine line = new MarkdownLine();
        MarkdownAbbreviationMap map = new MarkdownAbbreviationMap();
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);

        text.SetLiteral("HTML");
        paragraph.AppendChild(text);
        document.Root.AppendChild(paragraph);

        Assert.That(() => AbbreviationBlockRules.RegisterAbbreviationBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationBlockRules.TryParseAbbreviationDefinition(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationBlockRules.IsAbbreviationDefinitionLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationBlockRules.GetAbbreviationLabel(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationBlockRules.GetAbbreviationTitle(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationBlockRules.GetAbbreviationMarkerStart(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationBlockRules.GetAbbreviationMarkerEnd(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Add(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.Add(MarkdownNode.Create(MarkdownNodeKind.Text)), Throws.TypeOf<ArgumentException>());
        Assert.That(() => map.Find(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => map.InsertByDescendingLabelLength(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.Apply(null!, map), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.Apply(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.ApplyNode(null!, map), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.ApplyNode(document.Root, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.ReplaceTextChild(null!, 0, text, map), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.ReplaceTextChild(paragraph, 0, null!, map), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.ReplaceTextChild(paragraph, 0, text, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.CreateReplacementNodes(null!, map), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.CreateReplacementNodes("HTML", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.FindDefinitionAt(null!, 0, map), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.FindDefinitionAt("HTML", -1, map), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => AbbreviationInlineProcessor.FindDefinitionAt("HTML", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.MatchesAt(null!, 0, "HTML"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.MatchesAt("HTML", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.MatchesAt("HTML", -1, "HTML"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => AbbreviationInlineProcessor.HasWordBoundaries(null!, 0, "HTML"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.HasWordBoundaries("HTML", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.HasWordBoundaries("HTML", -1, "HTML"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => AbbreviationInlineProcessor.CreateTextNode(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.CreateAbbreviationNode(null!, "title"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.CreateAbbreviationNode("HTML", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => AbbreviationInlineProcessor.ShouldProcessChildren(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(AbbreviationBlockRules.TryParseAbbreviationDefinition(context), Is.False);
        Assert.That(AbbreviationBlockRules.GetAbbreviationLabel(line), Is.EqualTo(""));
    }
}
