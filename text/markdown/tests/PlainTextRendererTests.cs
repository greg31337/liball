using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PlainTextRendererTests
{
    [Test]
    public void PlainTextRenderer_RendersEmptyDocumentAsEmptyString()
    {
        MarkdownDocument document = MarkdownParser.Parse("");

        string text = PlainTextRenderer.Render(document);

        Assert.That(text, Is.EqualTo(""));
    }

    [Test]
    public void PlainTextRenderer_WritesToProvidedTextWriter()
    {
        MarkdownDocument document = MarkdownParser.Parse("");
        StringWriter writer = new StringWriter();

        PlainTextRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());

        Assert.That(writer.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void PlainTextRenderer_RendersInlineTextWithoutMarkdownMarkup()
    {
        MarkdownDocument document = MarkdownParser.Parse("Hello *em* **strong** [link](/x) ![alt](/img.png) `code`");

        string text = PlainTextRenderer.Render(document);

        Assert.That(text, Is.EqualTo("Hello em strong link alt code\n"));
    }

    [Test]
    public void PlainTextRenderer_RendersBlocksWithReadableSpacing()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "# Title\n" +
            "\n" +
            "Paragraph\n" +
            "\n" +
            "```csharp\n" +
            "code\n" +
            "```\n" +
            "\n" +
            "<div>raw</div>\n" +
            "\n" +
            "[ref]: /ignored\n" +
            "\n" +
            "---");

        string text = PlainTextRenderer.Render(document);

        Assert.That(text, Is.EqualTo(
            "Title\n" +
            "\n" +
            "Paragraph\n" +
            "\n" +
            "code\n" +
            "\n" +
            "<div>raw</div>\n"));
    }

    [Test]
    public void PlainTextRenderer_RendersContainersWithoutMarkdownMarkers()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "> quote\n" +
            "\n" +
            "3. item\n" +
            "4. next");

        string text = PlainTextRenderer.Render(document);

        Assert.That(text, Is.EqualTo(
            "quote\n" +
            "\n" +
            "item\n" +
            "next\n"));
    }

    [Test]
    public void PlainTextRenderer_RendersBreaksWithConfiguredLineEndings()
    {
        MarkdownDocument document = MarkdownParser.Parse("a\nb  \nc\\\nd");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        string text = PlainTextRenderer.Render(document, pipeline);

        Assert.That(text, Is.EqualTo("a\r\nb\r\nc\r\nd\r\n"));
    }

    [Test]
    public void PlainTextWriter_WritesRawTextAndLineEndings()
    {
        StringWriter writer = new StringWriter();
        PlainTextWriter plainTextWriter = new PlainTextWriter(writer);

        plainTextWriter.Write("alpha");
        plainTextWriter.WriteLineEnding("\r\n");
        plainTextWriter.Write("beta");

        Assert.That(writer.ToString(), Is.EqualTo("alpha\r\nbeta"));
    }

    [Test]
    public void PlainTextRenderContext_CapturesPipelineOptions()
    {
        StringWriter writer = new StringWriter();
        PlainTextWriter plainTextWriter = new PlainTextWriter(writer);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        PlainTextRenderContext context = new PlainTextRenderContext(plainTextWriter, pipeline);

        Assert.That(context.Writer, Is.SameAs(plainTextWriter));
        Assert.That(context.Pipeline.GetOptions().LineEnding, Is.EqualTo("\r\n"));
        Assert.That(context.Options.LineEnding, Is.EqualTo("\r\n"));
    }

    [Test]
    public void PlainTextRenderer_RejectsInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();
        StringWriter writer = new StringWriter();
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        PlainTextWriter plainTextWriter = new PlainTextWriter(writer);
        PlainTextRenderContext context = new PlainTextRenderContext(plainTextWriter, pipeline);

        Assert.That(() => PlainTextRenderer.Render(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.Render(document, (MarkdownPipeline)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.Render(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.Render(null!, writer, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.Render(document, null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.Render(document, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.RenderNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.RenderNode(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.RenderChildren(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.RenderChildren(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.ShouldRenderBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.WriteLiteralWithNormalizedLineEndings(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.WriteLiteralWithNormalizedLineEndings(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.WriteLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.ValidateContextAndNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => PlainTextRenderer.ValidateContextAndNode(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new PlainTextWriter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new PlainTextRenderContext(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new PlainTextRenderContext(plainTextWriter, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => plainTextWriter.Write(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => plainTextWriter.WriteLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
