using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class GfmDisallowedRawHtmlTests
{
    [Test]
    public void GfmDisallowedRawHtml_FiltersDisallowedBlockAndInlineTagsWhenRawHtmlIsAllowed()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.GfmPreset()
            .AllowRawHtml()
            .Build();

        string html = Markdown.ToHtml(
            "<div>safe</div>\n" +
            "\n" +
            "<script>alert(1)</script>\n" +
            "\n" +
            "x <span>safe</span> <SCRIPT type=\"text/javascript\">bad</SCRIPT> <scripted>safe</scripted>",
            pipeline);

        Assert.That(html, Is.EqualTo(
            "<div>safe</div>\n" +
            "&lt;script>alert(1)&lt;/script>\n" +
            "<p>x <span>safe</span> &lt;SCRIPT type=\"text/javascript\">bad&lt;/SCRIPT> <scripted>safe</scripted></p>\n"));
    }

    [Test]
    public void GfmDisallowedRawHtml_FiltersAllGfmDisallowedTags()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.GfmPreset()
            .AllowRawHtml()
            .Build();

        string html = Markdown.ToHtml(
            "<title>x</title> <textarea>x</textarea> <style>x</style> <xmp>x</xmp> " +
            "<iframe>x</iframe> <noembed>x</noembed> <noframes>x</noframes> <plaintext>x",
            pipeline);

        Assert.That(html, Is.EqualTo(
            "&lt;title>x&lt;/title> &lt;textarea>x&lt;/textarea> &lt;style>x&lt;/style> &lt;xmp>x&lt;/xmp> " +
            "&lt;iframe>x&lt;/iframe> &lt;noembed>x&lt;/noembed> &lt;noframes>x&lt;/noframes> &lt;plaintext>x\n"));
    }

    [Test]
    public void GfmDisallowedRawHtml_DoesNotFilterCommonMarkRawHtml()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AllowRawHtml()
            .Build();

        string html = Markdown.ToHtml("<script>alert(1)</script>", pipeline);

        Assert.That(html, Is.EqualTo("<script>alert(1)</script>\n"));
    }

    [Test]
    public void GfmDisallowedRawHtml_DoesNotFilterUnsafeHtml()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.GfmPreset()
            .AllowUnsafeHtml()
            .Build();

        string html = Markdown.ToHtml("<script>alert(1)</script>\n\nx <iframe>bad</iframe>", pipeline);

        Assert.That(html, Is.EqualTo("<script>alert(1)</script>\n<p>x <iframe>bad</iframe></p>\n"));
    }

    [Test]
    public void GfmDisallowedRawHtml_EscapesAllRawHtmlWhenRawHtmlIsNotAllowed()
    {
        string html = Markdown.ToHtml("<script>alert(1)</script>\n\nx <span>safe</span>", MarkdownPipeline.GfmPreset());

        Assert.That(html, Is.EqualTo("&lt;script&gt;alert(1)&lt;/script&gt;\n<p>x &lt;span&gt;safe&lt;/span&gt;</p>\n"));
    }

    [Test]
    public void GfmDisallowedRawHtml_HelperMethodsClassifyTags()
    {
        Assert.That(HtmlRenderer.IsGfmDisallowedRawHtmlTag("<script>", 0), Is.True);
        Assert.That(HtmlRenderer.IsGfmDisallowedRawHtmlTag("</SCRIPT>", 0), Is.True);
        Assert.That(HtmlRenderer.IsGfmDisallowedRawHtmlTag("<script/>", 0), Is.True);
        Assert.That(HtmlRenderer.IsGfmDisallowedRawHtmlTag("<script type=\"x\">", 0), Is.True);
        Assert.That(HtmlRenderer.IsGfmDisallowedRawHtmlTag("<scripted>", 0), Is.False);
        Assert.That(HtmlRenderer.IsGfmDisallowedRawHtmlTag("x <iframe>", 2), Is.True);
        Assert.That(HtmlRenderer.IsGfmDisallowedRawHtmlTagName("<textarea>", 1, "textarea"), Is.True);
        Assert.That(HtmlRenderer.IsGfmRawHtmlTagNameBoundary('>'), Is.True);
        Assert.That(HtmlRenderer.IsGfmRawHtmlTagNameBoundary('x'), Is.False);
        Assert.That(HtmlRenderer.StartsWithIgnoreCaseAt("abC", 1, "BC"), Is.True);
    }

    [Test]
    public void GfmDisallowedRawHtml_HelperMethodsRejectInvalidArguments()
    {
        HtmlRenderContext context = new HtmlRenderContext(
            new HtmlWriter(new StringWriter()),
            MarkdownPipeline.GfmPreset());

        Assert.That(() => HtmlRenderer.ShouldFilterGfmDisallowedRawHtml(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteGfmFilteredRawHtml(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteGfmFilteredRawHtml(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.IsGfmDisallowedRawHtmlTag(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.IsGfmDisallowedRawHtmlTag("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => HtmlRenderer.IsGfmDisallowedRawHtmlTagName(null!, 0, "script"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.IsGfmDisallowedRawHtmlTagName("", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.IsGfmDisallowedRawHtmlTagName("", -1, "script"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => HtmlRenderer.StartsWithIgnoreCaseAt(null!, 0, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.StartsWithIgnoreCaseAt("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.StartsWithIgnoreCaseAt("x", -1, "x"), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
