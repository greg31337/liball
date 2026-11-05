using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownFacadeTests
{
    [Test]
    public void Markdown_ParseReturnsParsedDocument()
    {
        MarkdownDocument document = Markdown.Parse("# Title");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
    }

    [Test]
    public void Markdown_ToHtmlRendersSourceAndDocument()
    {
        MarkdownDocument document = Markdown.Parse("# Title\n\nHello *world*");

        Assert.That(Markdown.ToHtml("# Title\n\nHello *world*"), Is.EqualTo(
            "<h1>Title</h1>\n" +
            "<p>Hello <em>world</em></p>\n"));
        Assert.That(Markdown.ToHtml(document), Is.EqualTo(
            "<h1>Title</h1>\n" +
            "<p>Hello <em>world</em></p>\n"));
    }

    [Test]
    public void Markdown_ToMarkdownAndNormalizeRenderSourceAndDocument()
    {
        MarkdownDocument document = Markdown.Parse("# Title\n\nHello *world*");

        Assert.That(Markdown.ToMarkdown("# Title\n\nHello *world*"), Is.EqualTo(
            "# Title\n" +
            "\n" +
            "Hello *world*\n"));
        Assert.That(Markdown.ToMarkdown(document), Is.EqualTo(
            "# Title\n" +
            "\n" +
            "Hello *world*\n"));
        Assert.That(Markdown.Normalize("# Title\n\nHello *world*"), Is.EqualTo(Markdown.ToMarkdown(document)));
        Assert.That(Markdown.Normalize(document), Is.EqualTo(Markdown.ToMarkdown(document)));
    }

    [Test]
    public void Markdown_ToPlainTextRendersSourceAndDocument()
    {
        MarkdownDocument document = Markdown.Parse("# Title\n\nHello *world*");

        Assert.That(Markdown.ToPlainText("# Title\n\nHello *world*"), Is.EqualTo(
            "Title\n" +
            "\n" +
            "Hello world\n"));
        Assert.That(Markdown.ToPlainText(document), Is.EqualTo(
            "Title\n" +
            "\n" +
            "Hello world\n"));
    }

    [Test]
    public void Markdown_FacadeMethodsHonorPipelineOptions()
    {
        MarkdownPipeline rawHtml = MarkdownPipelineBuilder.CommonMarkPreset()
            .AllowRawHtml()
            .Build();
        MarkdownPipeline crlf = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();
        MarkdownDocument document = Markdown.Parse("a\nb", crlf);

        Assert.That(Markdown.ToHtml("x <span>raw</span>", rawHtml), Is.EqualTo("<p>x <span>raw</span></p>\n"));
        Assert.That(Markdown.ToMarkdown("a\nb", crlf), Is.EqualTo("a\r\nb\r\n"));
        Assert.That(Markdown.Normalize("a\nb", crlf), Is.EqualTo("a\r\nb\r\n"));
        Assert.That(Markdown.ToPlainText(document, crlf), Is.EqualTo("a\r\nb\r\n"));
    }

    [Test]
    public void Markdown_RejectsInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();

        Assert.That(() => Markdown.Parse(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.Parse(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.Parse("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToHtml((string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToHtml((MarkdownDocument)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToHtml((string)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToHtml("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToHtml((MarkdownDocument)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToHtml(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToMarkdown((string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToMarkdown((MarkdownDocument)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToMarkdown((string)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToMarkdown("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToMarkdown((MarkdownDocument)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToMarkdown(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToPlainText((string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToPlainText((MarkdownDocument)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToPlainText((string)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToPlainText("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToPlainText((MarkdownDocument)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.ToPlainText(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.Normalize((string)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.Normalize((MarkdownDocument)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.Normalize((string)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.Normalize("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.Normalize((MarkdownDocument)null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => Markdown.Normalize(document, null!), Throws.TypeOf<ArgumentNullException>());
    }
}
