using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class FootnoteRenderingTests
{
    [Test]
    public void HtmlRenderer_RendersFootnoteReferencesAndDefinitions()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "Text[^note].\n" +
            "\n" +
            "[^note]: note *em*",
            MarkdownPipeline.AdvancedPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.AdvancedPreset());

        Assert.That(html, Is.EqualTo(
            "<p>Text<sup id=\"fnref-note\"><a href=\"#fn-note\" class=\"footnote-ref\">1</a></sup>.</p>\n" +
            "<section class=\"footnotes\">\n" +
            "<ol>\n" +
            "<li id=\"fn-note\"><p>note <em>em</em> <a href=\"#fnref-note\" class=\"footnote-backref\">&#8617;</a></p>\n" +
            "</li>\n" +
            "</ol>\n" +
            "</section>\n"));
    }

    [Test]
    public void HtmlRenderer_NumbersFootnotesInReferenceOrder()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "A[^b] B[^a] C[^b]\n" +
            "\n" +
            "[^a]: alpha\n" +
            "[^b]: beta",
            MarkdownPipeline.AdvancedPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.AdvancedPreset());

        Assert.That(html, Is.EqualTo(
            "<p>A<sup id=\"fnref-b\"><a href=\"#fn-b\" class=\"footnote-ref\">1</a></sup> " +
            "B<sup id=\"fnref-a\"><a href=\"#fn-a\" class=\"footnote-ref\">2</a></sup> " +
            "C<sup id=\"fnref-b\"><a href=\"#fn-b\" class=\"footnote-ref\">1</a></sup></p>\n" +
            "<section class=\"footnotes\">\n" +
            "<ol>\n" +
            "<li id=\"fn-b\"><p>beta <a href=\"#fnref-b\" class=\"footnote-backref\">&#8617;</a></p>\n" +
            "</li>\n" +
            "<li id=\"fn-a\"><p>alpha <a href=\"#fnref-a\" class=\"footnote-backref\">&#8617;</a></p>\n" +
            "</li>\n" +
            "</ol>\n" +
            "</section>\n"));
    }

    [Test]
    public void HtmlRenderer_DoesNotRenderFootnoteSectionForUnresolvedReferences()
    {
        MarkdownDocument document = MarkdownParser.Parse("Text[^missing].", MarkdownPipeline.AdvancedPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.AdvancedPreset());

        Assert.That(html, Is.EqualTo(
            "<p>Text<sup id=\"fnref-missing\"><a href=\"#fn-missing\" class=\"footnote-ref\">1</a></sup>.</p>\n"));
    }

    [Test]
    public void HtmlRenderer_SuppressesUnreferencedFootnoteDefinitions()
    {
        MarkdownDocument document = MarkdownParser.Parse("[^note]: hidden", MarkdownPipeline.AdvancedPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.AdvancedPreset());

        Assert.That(html, Is.EqualTo(""));
    }

    [Test]
    public void HtmlRenderer_FootnoteHelpersTrackOrderAndIds()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "A[^A B!] B[^c]\n\n[^A B!]: alpha",
            MarkdownPipeline.AdvancedPreset());
        HtmlRenderContext context = new HtmlRenderContext(new HtmlWriter(new StringWriter()), MarkdownPipeline.AdvancedPreset());

        HtmlRenderer.CollectFootnoteReferences(context, document.Root);

        Assert.That(context.FootnoteOrder.Count, Is.EqualTo(2));
        Assert.That(context.FootnoteOrder[0], Is.EqualTo("a b!"));
        Assert.That(HtmlRenderer.FindFootnoteOrderIndex(context, "a b!"), Is.EqualTo(0));
        Assert.That(HtmlRenderer.GetFootnoteIdLabel("A B!"), Is.EqualTo("a-b"));
        Assert.That(HtmlRenderer.IsFootnoteIdCharacter('_'), Is.True);
        Assert.That(HtmlRenderer.IsFootnoteIdCharacter('!'), Is.False);
        Assert.That(HtmlRenderer.FindFootnoteDefinition(document.Root, "a b!"), Is.Not.Null);
        Assert.That(HtmlRenderer.HasRenderableFootnotes(context, document.Root), Is.True);
    }

    [Test]
    public void HtmlRenderer_FootnoteRenderingRejectsInvalidArguments()
    {
        MarkdownDocument document = MarkdownParser.Parse("Text[^note].\n\n[^note]: note", MarkdownPipeline.AdvancedPreset());
        HtmlRenderContext context = new HtmlRenderContext(new HtmlWriter(new StringWriter()), MarkdownPipeline.AdvancedPreset());
        MarkdownNode definition = document.Root.GetChild(1);
        MarkdownNode reference = document.Root.GetChild(0).GetChild(1);

        Assert.That(() => HtmlRenderer.RenderDocument(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderDocument(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderFootnoteReference(null!, reference), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderFootnoteReference(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderFootnoteSection(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderFootnoteSection(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderFootnoteListItem(null!, definition, "note"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderFootnoteListItem(context, null!, "note"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderFootnoteListItem(context, definition, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteFootnoteBackreference(null!, "note"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteFootnoteBackreference(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.CollectFootnoteReferences(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.CollectFootnoteReferences(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.AddFootnoteReferenceLabel(null!, "note"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.AddFootnoteReferenceLabel(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.FindFootnoteOrderIndex(null!, "note"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.FindFootnoteOrderIndex(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.GetFootnoteReferenceNumber(null!, reference), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.GetFootnoteReferenceNumber(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.HasRenderableFootnotes(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.HasRenderableFootnotes(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.FindFootnoteDefinition(null!, "note"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.FindFootnoteDefinition(document.Root, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.GetFootnoteIdLabel(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
