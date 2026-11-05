using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class XmlAstRendererTests
{
    [Test]
    public void XmlAstRenderer_RendersEmptyDocumentRoot()
    {
        MarkdownDocument document = new MarkdownDocument();

        string xml = XmlAstRenderer.Render(document);

        Assert.That(xml, Is.EqualTo("<Document span=\"1:1-1:1\" length=\"0\" children=\"0\" />\n"));
    }

    [Test]
    public void XmlAstRenderer_WritesToProvidedTextWriter()
    {
        MarkdownDocument document = new MarkdownDocument();
        StringWriter writer = new StringWriter();

        XmlAstRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());

        Assert.That(writer.ToString(), Is.EqualTo("<Document span=\"1:1-1:1\" length=\"0\" children=\"0\" />\n"));
    }

    [Test]
    public void XmlAstRenderer_RendersNestedNodesAttributesAndEscapedLiterals()
    {
        MarkdownDocument document = MarkdownDocument.Create(MarkdownSourceSpan.Create(0, 1, 1, 12, 2, 1));
        MarkdownNode heading = MarkdownNode.Create(
            MarkdownNodeKind.Heading,
            MarkdownSourceSpan.Create(0, 1, 1, 10, 1, 11));
        MarkdownNode text = MarkdownNode.Create(
            MarkdownNodeKind.Text,
            MarkdownSourceSpan.Create(2, 1, 3, 10, 1, 11));

        heading.SetAttribute("level", "1");
        heading.SetAttribute("data", "a\t\"b\" & <c>");
        heading.SetLiteral("Title\nLine");
        text.SetLiteral("Tom's <Title>");
        heading.AppendChild(text);
        document.Root.AppendChild(heading);

        string xml = XmlAstRenderer.Render(document);

        Assert.That(xml, Is.EqualTo(
            "<Document span=\"1:1-2:1\" length=\"12\" children=\"1\">\n" +
            "  <Heading span=\"1:1-1:11\" length=\"10\" literal=\"Title&#10;Line\" attr-level=\"1\" attr-data=\"a&#9;&quot;b&quot; &amp; &lt;c&gt;\" children=\"1\">\n" +
            "    <Text span=\"1:3-1:11\" length=\"8\" literal=\"Tom&#39;s &lt;Title&gt;\" children=\"0\" />\n" +
            "  </Heading>\n" +
            "</Document>\n"));
    }

    [Test]
    public void XmlAstRenderer_UsesConfiguredLineEndings()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        string xml = XmlAstRenderer.Render(document, pipeline);

        Assert.That(xml, Is.EqualTo("<Document span=\"1:1-1:1\" length=\"0\" children=\"0\" />\r\n"));
    }

    [Test]
    public void XmlAstRenderContext_CapturesPipelineOptions()
    {
        StringWriter writer = new StringWriter();
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        XmlAstRenderContext context = new XmlAstRenderContext(writer, pipeline);

        Assert.That(context.Writer, Is.SameAs(writer));
        Assert.That(context.Pipeline.GetOptions().LineEnding, Is.EqualTo("\r\n"));
        Assert.That(context.Options.LineEnding, Is.EqualTo("\r\n"));
    }

    [Test]
    public void XmlAstRenderer_HelperMethodsFormatStableText()
    {
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.StrongEmphasis);

        Assert.That(XmlAstRenderer.GetElementName(node), Is.EqualTo("StrongEmphasis"));
        Assert.That(XmlAstRenderer.EscapeXmlAttribute("a\r\n\t&<>\"'b"), Is.EqualTo("a&#13;&#10;&#9;&amp;&lt;&gt;&quot;&#39;b"));
    }

    [Test]
    public void XmlAstRenderer_RejectsInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();
        StringWriter writer = new StringWriter();
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        XmlAstRenderContext context = new XmlAstRenderContext(writer, pipeline);

        Assert.That(() => XmlAstRenderer.Render(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.Render(document, (MarkdownPipeline)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.Render(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.Render(null!, writer, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.Render(document, null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.Render(document, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.RenderNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.RenderNodeAtDepth(null!, document.Root, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.RenderNodeAtDepth(context, null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.RenderNodeAtDepth(context, document.Root, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => XmlAstRenderer.GetElementName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.WriteNodeAttributes(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.WriteNodeAttributes(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.WriteAttribute(null!, "name", "value"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.WriteAttribute(context, null!, "value"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.WriteAttribute(context, "name", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.WriteIndent(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.WriteIndent(context, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => XmlAstRenderer.EscapeXmlAttribute(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.WriteLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.ValidateContextAndNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => XmlAstRenderer.ValidateContextAndNode(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new XmlAstRenderContext(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new XmlAstRenderContext(writer, null!), Throws.TypeOf<ArgumentNullException>());
    }
}
