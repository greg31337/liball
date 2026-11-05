using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class DebugTreeRendererTests
{
    [Test]
    public void DebugTreeRenderer_RendersEmptyDocumentRoot()
    {
        MarkdownDocument document = new MarkdownDocument();

        string debug = DebugTreeRenderer.Render(document);

        Assert.That(debug, Is.EqualTo("Document span=1:1-1:1 length=0 children=0\n"));
    }

    [Test]
    public void DebugTreeRenderer_WritesToProvidedTextWriter()
    {
        MarkdownDocument document = new MarkdownDocument();
        StringWriter writer = new StringWriter();

        DebugTreeRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());

        Assert.That(writer.ToString(), Is.EqualTo("Document span=1:1-1:1 length=0 children=0\n"));
    }

    [Test]
    public void DebugTreeRenderer_RendersNestedNodesAttributesAndEscapedLiterals()
    {
        MarkdownDocument document = MarkdownDocument.Create(MarkdownSourceSpan.Create(0, 1, 1, 12, 2, 1));
        MarkdownNode heading = MarkdownNode.Create(
            MarkdownNodeKind.Heading,
            MarkdownSourceSpan.Create(0, 1, 1, 10, 1, 11));
        MarkdownNode text = MarkdownNode.Create(
            MarkdownNodeKind.Text,
            MarkdownSourceSpan.Create(2, 1, 3, 10, 1, 11));

        heading.SetAttribute("level", "1");
        heading.SetAttribute("data", "a\t\"b\"");
        heading.SetLiteral("Title\nLine");
        text.SetLiteral("Title\\Line");
        heading.AppendChild(text);
        document.Root.AppendChild(heading);

        string debug = DebugTreeRenderer.Render(document);

        Assert.That(debug, Is.EqualTo(
            "Document span=1:1-2:1 length=12 children=1\n" +
            "  Heading span=1:1-1:11 length=10 attrs=[level=\"1\", data=\"a\\t\\\"b\\\"\"] literal=\"Title\\nLine\" children=1\n" +
            "    Text span=1:3-1:11 length=8 literal=\"Title\\\\Line\" children=0\n"));
    }

    [Test]
    public void DebugTreeRenderer_UsesConfiguredLineEndings()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        string debug = DebugTreeRenderer.Render(document, pipeline);

        Assert.That(debug, Is.EqualTo("Document span=1:1-1:1 length=0 children=0\r\n"));
    }

    [Test]
    public void DebugTreeRenderContext_CapturesPipelineOptions()
    {
        StringWriter writer = new StringWriter();
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        DebugTreeRenderContext context = new DebugTreeRenderContext(writer, pipeline);

        Assert.That(context.Writer, Is.SameAs(writer));
        Assert.That(context.Pipeline.GetOptions().LineEnding, Is.EqualTo("\r\n"));
        Assert.That(context.Options.LineEnding, Is.EqualTo("\r\n"));
    }

    [Test]
    public void DebugTreeRenderer_HelperMethodsFormatStableText()
    {
        MarkdownSourcePosition position = MarkdownSourcePosition.Create(3, 2, 4);
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(3, 2, 4, 8, 2, 9);

        Assert.That(DebugTreeRenderer.FormatPosition(position), Is.EqualTo("2:4"));
        Assert.That(DebugTreeRenderer.FormatSpan(span), Is.EqualTo("2:4-2:9"));
        Assert.That(DebugTreeRenderer.EscapeDebugText("a\r\n\t\\\"b"), Is.EqualTo("a\\r\\n\\t\\\\\\\"b"));
    }

    [Test]
    public void DebugTreeRenderer_RejectsInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();
        StringWriter writer = new StringWriter();
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        DebugTreeRenderContext context = new DebugTreeRenderContext(writer, pipeline);

        Assert.That(() => DebugTreeRenderer.Render(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.Render(document, (MarkdownPipeline)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.Render(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.Render(null!, writer, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.Render(document, null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.Render(document, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.RenderNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.RenderNodeAtDepth(null!, document.Root, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.RenderNodeAtDepth(context, null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.RenderNodeAtDepth(context, document.Root, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => DebugTreeRenderer.WriteIndent(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.WriteIndent(context, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => DebugTreeRenderer.WriteAttributes(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.WriteAttributes(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.WriteLiteral(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.WriteLiteral(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.FormatSpan(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.FormatPosition(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.EscapeDebugText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.WriteLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.ValidateContextAndNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => DebugTreeRenderer.ValidateContextAndNode(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new DebugTreeRenderContext(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new DebugTreeRenderContext(writer, null!), Throws.TypeOf<ArgumentNullException>());
    }
}
