using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class DefinitionListTests
{
    [Test]
    public void DefinitionListRule_ParsesTermsAndDescriptionsInAdvancedPipeline()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "Term *one*\n" +
            ": Definition **one**\n" +
            ": second\n" +
            "\n" +
            "After",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode list = document.Root.GetChild(0);
        MarkdownNode term = list.GetChild(0);
        MarkdownNode firstDescription = list.GetChild(1);
        MarkdownNode secondDescription = list.GetChild(2);

        Assert.That(list.Kind, Is.EqualTo(MarkdownNodeKind.DefinitionList));
        Assert.That(term.Kind, Is.EqualTo(MarkdownNodeKind.DefinitionTerm));
        Assert.That(term.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(firstDescription.Kind, Is.EqualTo(MarkdownNodeKind.DefinitionDescription));
        Assert.That(firstDescription.Literal, Is.EqualTo("Definition **one**"));
        Assert.That(firstDescription.GetChild(0).GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
        Assert.That(secondDescription.Literal, Is.EqualTo("second"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void DefinitionListRule_DoesNotParseWithoutDefinitionListFeature()
    {
        MarkdownDocument commonMark = MarkdownParser.Parse("Term\n: Definition", MarkdownPipeline.CommonMarkPreset());
        MarkdownDocument gfm = MarkdownParser.Parse("Term\n: Definition", MarkdownPipeline.GfmPreset());

        Assert.That(ContainsKind(commonMark.Root, MarkdownNodeKind.DefinitionList), Is.False);
        Assert.That(ContainsKind(gfm.Root, MarkdownNodeKind.DefinitionList), Is.False);
    }

    [Test]
    public void DefinitionListRule_ParsesIndentedContinuationAfterBlankLine()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "Term\n" +
            ": first\n" +
            "\n" +
            "    second\n" +
            "\n" +
            "After",
            MarkdownPipeline.AdvancedPreset());

        MarkdownNode description = document.Root.GetChild(0).GetChild(1);

        Assert.That(description.Kind, Is.EqualTo(MarkdownNodeKind.DefinitionDescription));
        Assert.That(description.Literal, Is.EqualTo("first\n\nsecond"));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("After"));
    }

    [Test]
    public void DefinitionListRenderers_RenderHtmlMarkdownAndPlainText()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownDocument document = MarkdownParser.Parse(
            "Term *one*\n" +
            ": Definition **one**\n" +
            ": second",
            pipeline);

        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<dl>\n" +
            "<dt>Term <em>one</em></dt>\n" +
            "<dd><p>Definition <strong>one</strong></p>\n" +
            "</dd>\n" +
            "<dd><p>second</p>\n" +
            "</dd>\n" +
            "</dl>\n"));
        Assert.That(markdown, Is.EqualTo(
            "Term *one*\n" +
            ": Definition **one**\n" +
            ": second\n"));
        Assert.That(plainText, Is.EqualTo(
            "Term one\n" +
            "Definition one\n" +
            "second\n"));
    }

    [Test]
    public void DefinitionListRule_AcceptsMultipleTerms()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "A\n" +
            ": first\n" +
            "B\n" +
            ": second",
            MarkdownPipeline.AdvancedPreset());
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetChildCount(), Is.EqualTo(4));
        Assert.That(list.GetChild(0).Literal, Is.EqualTo("A"));
        Assert.That(list.GetChild(2).Literal, Is.EqualTo("B"));
        Assert.That(list.GetChild(3).Literal, Is.EqualTo("second"));
    }

    [Test]
    public void DefinitionListRule_IsRegisteredOnlyForAdvancedPipelines()
    {
        MarkdownRuleList advancedBlocks = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        Assert.That(advancedBlocks.Contains(DefinitionListBlockRules.DefinitionListRuleName), Is.True);
        Assert.That(advancedBlocks.IndexOf(DefinitionListBlockRules.DefinitionListRuleName), Is.LessThan(advancedBlocks.IndexOf(CommonMarkBlockRules.ParagraphRuleName)));
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(DefinitionListBlockRules.DefinitionListRuleName), Is.False);
        Assert.That(MarkdownPipeline.GfmPreset().GetBlockRules().Contains(DefinitionListBlockRules.DefinitionListRuleName), Is.False);
        Assert.That(MarkdownPipelineBuilder.GfmPreset().GetBlockRules().Contains(DefinitionListBlockRules.DefinitionListRuleName), Is.False);

        builder.UseCommonMark();
        Assert.That(builder.GetBlockRules().Contains(DefinitionListBlockRules.DefinitionListRuleName), Is.False);
    }

    [Test]
    public void DefinitionListRule_HelperMethodsClassifyMarkers()
    {
        MarkdownBlockParseContext startContext = MarkdownBlockParseContext.Create("Term\n: description", MarkdownPipeline.AdvancedPreset());
        MarkdownBlockParseContext blankContext = MarkdownBlockParseContext.Create("Term\n: first\n\n    second", MarkdownPipeline.AdvancedPreset());
        MarkdownLine line = MarkdownLineReader.ReadLines("  : description")[0];
        blankContext.LineIndex = 2;

        Assert.That(DefinitionListBlockRules.IsDefinitionListStart(startContext), Is.True);
        Assert.That(DefinitionListBlockRules.IsDefinitionDescriptionLine(line), Is.True);
        Assert.That(DefinitionListBlockRules.GetDefinitionDescriptionMarkerStart(line), Is.EqualTo(2));
        Assert.That(DefinitionListBlockRules.GetDefinitionDescriptionContent(line), Is.EqualTo("description"));
        Assert.That(DefinitionListBlockRules.HasIndentedContinuationAfterBlank(blankContext), Is.True);
    }

    [Test]
    public void DefinitionListRule_RejectsInvalidArguments()
    {
        MarkdownBlockParseContext descriptionContext = MarkdownBlockParseContext.Create(": description", MarkdownPipeline.AdvancedPreset());
        MarkdownBlockParseContext paragraphContext = MarkdownBlockParseContext.Create("paragraph", MarkdownPipeline.AdvancedPreset());
        HtmlRenderContext htmlContext = new HtmlRenderContext(new HtmlWriter(new StringWriter()), MarkdownPipeline.AdvancedPreset());
        MarkdownRenderContext markdownContext = new MarkdownRenderContext(new MarkdownWriter(new StringWriter()), MarkdownPipeline.AdvancedPreset());
        MarkdownNode list = MarkdownNode.Create(MarkdownNodeKind.DefinitionList);

        Assert.That(() => DefinitionListBlockRules.RegisterDefinitionListBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DefinitionListBlockRules.TryParseDefinitionList(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DefinitionListBlockRules.ParseDefinitionDescription(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DefinitionListBlockRules.ParseDefinitionDescription(paragraphContext), Throws.TypeOf<ArgumentException>());
        Assert.That(() => DefinitionListBlockRules.IsDefinitionListStart(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DefinitionListBlockRules.IsDefinitionDescriptionLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DefinitionListBlockRules.GetDefinitionDescriptionMarkerStart(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DefinitionListBlockRules.GetDefinitionDescriptionContent(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DefinitionListBlockRules.HasIndentedContinuationAfterBlank(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderDefinitionList(null!, list), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderDefinitionList(htmlContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderDefinitionTerm(null!, list), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderDefinitionDescription(null!, list), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderDefinitionList(null!, list), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderDefinitionList(markdownContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderDefinitionTerm(null!, list), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderDefinitionDescription(null!, list), Throws.TypeOf<ArgumentNullException>());
        Assert.That(DefinitionListBlockRules.ParseDefinitionDescription(descriptionContext).Kind, Is.EqualTo(MarkdownNodeKind.DefinitionDescription));
    }

    public static bool ContainsKind(MarkdownNode node, MarkdownNodeKind kind)
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
            if (ContainsKind(node.GetChild(index), kind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
