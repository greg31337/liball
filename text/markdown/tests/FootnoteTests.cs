using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class FootnoteTests
{
    [Test]
    public void FootnoteRules_ParseDefinitionsAndReferencesInAdvancedPipeline()
    {
        string source =
            "Text[^note].\n" +
            "\n" +
            "[^note]: note *em*\n" +
            "    continued";
        MarkdownParseContext context = MarkdownParser.CreateContext(source, MarkdownPipeline.AdvancedPreset());

        MarkdownParser.Run(context);

        MarkdownNode paragraph = context.Document.Root.GetChild(0);
        MarkdownNode reference = paragraph.GetChild(1);
        MarkdownNode definition = context.Document.Root.GetChild(1);
        MarkdownNode definitionParagraph = definition.GetChild(0);

        Assert.That(reference.Kind, Is.EqualTo(MarkdownNodeKind.FootnoteReference));
        Assert.That(reference.GetAttributeValue("label"), Is.EqualTo("note"));
        Assert.That(reference.GetAttributeValue("normalizedLabel"), Is.EqualTo("note"));
        Assert.That(reference.GetAttributeValue("resolved"), Is.EqualTo("true"));
        Assert.That(definition.Kind, Is.EqualTo(MarkdownNodeKind.FootnoteDefinition));
        Assert.That(definition.GetAttributeValue("label"), Is.EqualTo("note"));
        Assert.That(definition.GetAttributeValue("normalizedLabel"), Is.EqualTo("note"));
        Assert.That(definition.Literal, Is.EqualTo("note *em*\ncontinued"));
        Assert.That(definitionParagraph.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(definitionParagraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(context.Footnotes.GetCount(), Is.EqualTo(1));
        Assert.That(context.Footnotes.Find("NOTE"), Is.SameAs(definition));
    }

    [Test]
    public void FootnoteRules_DoNotParseWithoutFootnoteFeature()
    {
        MarkdownDocument commonMark = MarkdownParser.Parse("Text[^note].\n\n[^note]: note", MarkdownPipeline.CommonMarkPreset());
        MarkdownDocument gfm = MarkdownParser.Parse("Text[^note].\n\n[^note]: note", MarkdownPipeline.GfmPreset());

        Assert.That(ContainsKind(commonMark.Root, MarkdownNodeKind.FootnoteReference), Is.False);
        Assert.That(ContainsKind(commonMark.Root, MarkdownNodeKind.FootnoteDefinition), Is.False);
        Assert.That(ContainsKind(gfm.Root, MarkdownNodeKind.FootnoteReference), Is.False);
        Assert.That(ContainsKind(gfm.Root, MarkdownNodeKind.FootnoteDefinition), Is.False);
    }

    [Test]
    public void FootnoteRules_ParseIndentedContinuationAfterBlankLine()
    {
        string source =
            "[^a]: first\n" +
            "\n" +
            "    second\n" +
            "\n" +
            "after";

        MarkdownDocument document = MarkdownParser.Parse(source, MarkdownPipeline.AdvancedPreset());
        MarkdownNode definition = document.Root.GetChild(0);

        Assert.That(definition.Kind, Is.EqualTo(MarkdownNodeKind.FootnoteDefinition));
        Assert.That(definition.Literal, Is.EqualTo("first\n\nsecond"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("after"));
    }

    [Test]
    public void FootnoteRules_RenderMarkdownSyntax()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "Text[^note].\n" +
            "\n" +
            "[^note]: note *em*\n" +
            "    continued",
            MarkdownPipeline.AdvancedPreset());

        string markdown = MarkdownRenderer.Render(document, MarkdownPipeline.AdvancedPreset());

        Assert.That(markdown, Is.EqualTo(
            "Text[^note].\n" +
            "\n" +
            "[^note]: note *em*\n" +
            "    continued\n"));
    }

    [Test]
    public void FootnoteRules_AreRegisteredOnlyForAdvancedPipelines()
    {
        MarkdownRuleList advancedBlocks = MarkdownPipeline.AdvancedPreset().GetBlockRules();
        MarkdownRuleList advancedInlines = MarkdownPipeline.AdvancedPreset().GetInlineRules();
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.AdvancedPreset();

        Assert.That(advancedBlocks.Contains(FootnoteBlockRules.DefinitionRuleName), Is.True);
        Assert.That(advancedBlocks.IndexOf(FootnoteBlockRules.DefinitionRuleName), Is.LessThan(advancedBlocks.IndexOf(CommonMarkBlockRules.LinkReferenceDefinitionRuleName)));
        Assert.That(advancedInlines.Contains(FootnoteInlineRules.ReferenceRuleName), Is.True);
        Assert.That(advancedInlines.IndexOf(FootnoteInlineRules.ReferenceRuleName), Is.LessThan(advancedInlines.IndexOf(CommonMarkInlineRules.LinkRuleName)));
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(FootnoteBlockRules.DefinitionRuleName), Is.False);
        Assert.That(MarkdownPipeline.GfmPreset().GetInlineRules().Contains(FootnoteInlineRules.ReferenceRuleName), Is.False);
        Assert.That(MarkdownPipelineBuilder.GfmPreset().GetBlockRules().Contains(FootnoteBlockRules.DefinitionRuleName), Is.False);

        builder.UseGfm();
        Assert.That(builder.GetBlockRules().Contains(FootnoteBlockRules.DefinitionRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(FootnoteInlineRules.ReferenceRuleName), Is.False);
    }

    [Test]
    public void FootnoteRules_HelperMethodsClassifyMarkers()
    {
        MarkdownLine line = MarkdownLineReader.ReadLines("  [^ A\tB ]: content")[0];
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("[^a]: first\n\n    second", MarkdownPipeline.AdvancedPreset());
        context.LineIndex = 1;

        Assert.That(FootnoteBlockRules.IsFootnoteDefinitionLine(line), Is.True);
        Assert.That(FootnoteBlockRules.GetFootnoteDefinitionLabel(line), Is.EqualTo(" A\tB "));
        Assert.That(FootnoteBlockRules.GetFootnoteDefinitionContent(line), Is.EqualTo("content"));
        Assert.That(FootnoteBlockRules.GetFootnoteDefinitionMarkerStart(line), Is.EqualTo(2));
        Assert.That(FootnoteBlockRules.GetFootnoteDefinitionMarkerEnd(line), Is.EqualTo(11));
        Assert.That(FootnoteBlockRules.HasIndentedContinuationAfterBlank(context), Is.True);
        Assert.That(FootnoteInlineRules.FindFootnoteReferenceEnd("x [^a b] y", 2), Is.EqualTo(7));
        Assert.That(FootnoteInlineRules.IsValidFootnoteReferenceLabel(" A\tB "), Is.True);
        Assert.That(FootnoteInlineRules.IsValidFootnoteReferenceLabel(""), Is.False);
        Assert.That(FootnoteInlineRules.IsValidFootnoteReferenceLabel("a]"), Is.False);
    }

    [Test]
    public void FootnoteRules_RejectInvalidArguments()
    {
        MarkdownBlockParseContext blockContext = MarkdownBlockParseContext.Create("[^a]: b", MarkdownPipeline.AdvancedPreset());
        MarkdownInlineParseContext inlineContext = MarkdownInlineParseContext.Create(
            MarkdownParser.CreateContext("[^a]", MarkdownPipeline.AdvancedPreset()),
            MarkdownNode.Create(MarkdownNodeKind.Paragraph),
            "[^a]");

        Assert.That(() => FootnoteBlockRules.RegisterFootnoteBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteBlockRules.TryParseFootnoteDefinition(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteBlockRules.IsFootnoteDefinitionLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteBlockRules.GetFootnoteDefinitionLabel(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteBlockRules.GetFootnoteDefinitionContent(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteBlockRules.GetFootnoteDefinitionMarkerStart(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteBlockRules.GetFootnoteDefinitionMarkerEnd(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteBlockRules.HasIndentedContinuationAfterBlank(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteInlineRules.RegisterFootnoteInlineRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteInlineRules.TryParseFootnoteReference(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteInlineRules.FindFootnoteReferenceEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => FootnoteInlineRules.FindFootnoteReferenceEnd("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => FootnoteInlineRules.IsValidFootnoteReferenceLabel(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderFootnoteDefinition(null!, MarkdownNode.Create(MarkdownNodeKind.FootnoteDefinition)), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderFootnoteDefinition(new MarkdownRenderContext(new MarkdownWriter(new StringWriter()), MarkdownPipeline.AdvancedPreset()), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderFootnoteReference(null!, MarkdownNode.Create(MarkdownNodeKind.FootnoteReference)), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderFootnoteReference(new MarkdownRenderContext(new MarkdownWriter(new StringWriter()), MarkdownPipeline.AdvancedPreset()), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(FootnoteBlockRules.TryParseFootnoteDefinition(blockContext), Is.True);
        Assert.That(FootnoteInlineRules.TryParseFootnoteReference(inlineContext), Is.True);
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
