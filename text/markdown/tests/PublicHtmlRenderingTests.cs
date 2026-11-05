using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicHtmlRenderingTests
{
    [Test]
    public void PublicHtmlRendering_StreamsSourceAndDocumentWithPipelineOptions()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseXhtml()
            .SetSoftBreakMode(MarkdownSoftBreakMode.HardBreak)
            .SetLineEnding("\r\n")
            .Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "a\nb ![alt](/image.png)",
            pipeline);
        StringWriter sourceWriter = new StringWriter();
        StringWriter documentWriter = new StringWriter();

        markdown.WriteHtmlSourceWithPipeline(
            "a\nb ![alt](/image.png)",
            sourceWriter,
            pipeline);
        markdown.WriteHtmlDocumentWithPipeline(document, documentWriter, pipeline);
        documentWriter.Write("tail");

        string expected =
            "<p>a<br />\r\n" +
            "b <img src=\"/image.png\" alt=\"alt\" /></p>\r\n";
        Assert.That(sourceWriter.ToString(), Is.EqualTo(expected));
        Assert.That(documentWriter.ToString(), Is.EqualTo(expected + "tail"));
        Assert.That(markdown.RenderHtmlSourceWithPipeline(
            "a\nb ![alt](/image.png)",
            pipeline), Is.EqualTo(expected));
        Assert.That(markdown.RenderHtmlDocumentWithPipeline(document, pipeline), Is.EqualTo(expected));
    }

    [Test]
    public void PublicHtmlRendering_StreamsEventsAndPreservesTypedAttributeData()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownPipeline pipeline = markdown.CreateAdvancedPipeline();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "*important*{#key .accent}",
            pipeline);
        MarkdownEvent[] events = dataServices.ReadDocumentEvents(document);
        StringWriter writer = new StringWriter();

        markdown.WriteHtmlEventsWithPipeline(events, writer, pipeline);
        writer.Write("tail");

        Assert.That(writer.ToString(), Is.EqualTo(
            "<p><em id=\"key\" class=\"accent\">important</em></p>\n" +
            "tail"));
    }

    [Test]
    public void PublicHtmlRendering_ExposesFocusedAndArbitraryCallerRules()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        IHtmlRenderRule headingRule = new TestHtmlRenderRule(
            "test.public-heading",
            (context, node) =>
            {
                if (node.Kind != MarkdownNodeKind.Heading)
                {
                    return false;
                }

                context.Writer.Write("<heading>");
                markdown.RenderHtmlChildren(context, node);
                context.Writer.Write("</heading>");
                markdown.WriteHtmlLineEnding(context);
                return true;
            });

        builder.RenderCodeBlocksWith((context, node) =>
        {
            context.Writer.Write("<code-hook>");
            context.Writer.WriteEscapedText(node.Literal);
            context.Writer.Write("</code-hook>");
            markdown.WriteHtmlLineEnding(context);
            return true;
        });
        builder.RenderLinksWith((context, node) =>
        {
            context.Writer.Write("<link-hook>");
            markdown.RenderHtmlChildren(context, node);
            context.Writer.Write("</link-hook>");
            return true;
        });
        builder.RenderImagesWith((context, node) =>
        {
            context.Writer.Write("<image-hook></image-hook>");
            return true;
        });
        builder.RenderRawHtmlWith((context, node) =>
        {
            context.Writer.Write("<raw-hook>");
            context.Writer.WriteEscapedText(node.Literal);
            context.Writer.Write("</raw-hook>");
            return true;
        });
        builder.AddHtmlRenderRule(headingRule);
        MarkdownPipeline pipeline = builder.Build();

        string html = markdown.RenderHtmlSourceWithPipeline(
            "# Title\n\n```text\nx\n```\n\n[go](/go) ![pic](/pic) <b>x</b>",
            pipeline);

        Assert.That(html, Is.EqualTo(
            "<heading>Title</heading>\n" +
            "<code-hook>x</code-hook>\n" +
            "<p><link-hook>go</link-hook> <image-hook></image-hook> " +
            "<raw-hook>&lt;b&gt;</raw-hook>x<raw-hook>&lt;/b&gt;</raw-hook></p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersGenericExtensionNodesAndTheirAttributes()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode block = new MarkdownExtensionNode().Node;
        MarkdownNode inline = new MarkdownExtensionNode().Node;
        MarkdownNode paragraph = new MarkdownNode();
        paragraph.Kind = MarkdownNodeKind.Paragraph;
        inline.Kind = MarkdownNodeKind.ExtensionInline;
        inline.Data.Extension!.IsBlock = false;
        block.Literal = "<block>";
        inline.Literal = "<inline>";
        dataServices.CreateExtensionNode("block-widget", block);
        dataServices.CreateExtensionNode("inline-widget", inline);
        AddAttachedAttribute(block, "class", "wide");
        AddAttachedAttribute(inline, "data-mode", "compact");
        dataServices.CreateNodeOperations(paragraph).AppendChild(inline);
        dataServices.CreateNodeOperations(document.Root).AppendChild(block);
        dataServices.CreateNodeOperations(document.Root).AppendChild(paragraph);

        string html = markdown.RenderHtmlDocument(document);

        Assert.That(html, Is.EqualTo(
            "<div data-markdown-extension=\"block-widget\" class=\"wide\">\n" +
            "&lt;block&gt;\n" +
            "</div>\n" +
            "<p><span data-markdown-extension=\"inline-widget\" " +
            "data-mode=\"compact\">&lt;inline&gt;</span></p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersAttachedAttributesOnExtensionContainers()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreateAdvancedPipeline();
        string source =
            "| A |\n" +
            "| --- |\n" +
            "| B |\n" +
            "{#values .striped}";

        string html = markdown.RenderHtmlSourceWithPipeline(source, pipeline);

        Assert.That(html, Does.StartWith(
            "<table id=\"values\" class=\"striped\">\n"));
        Assert.That(html, Does.Contain("<th>A</th>\n"));
        Assert.That(html, Does.Contain("<td>B</td>\n"));
    }

    [Test]
    public void PublicHtmlRendering_ValidatesStreamsEventsAndOutputLimits()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetMaximumOutputSize(4)
            .Build();
        MarkdownDocument document = markdown.ParseSource("text");
        MarkdownEvent[] events = ((IMarkdownDataServices)markdown).ReadDocumentEvents(document);
        StringWriter writer = new StringWriter();

        Assert.That(() => markdown.WriteHtmlSource(null!, writer), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlSource("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlSourceWithPipeline("", writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlDocument(null!, writer), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlDocument(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlDocumentWithPipeline(document, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlEvents(null!, writer), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlEvents(events, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlEventsWithPipeline(events, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteHtmlDocumentWithPipeline(document, writer, pipeline), Throws.TypeOf<InvalidOperationException>());
    }

    private static void AddAttachedAttribute(MarkdownNode node, string name, string value)
    {
        MarkdownAttribute attribute = new MarkdownAttribute();
        attribute.Name = name;
        attribute.Value = value;
        node.Attributes.Add(attribute);
        node.Data.AttachedAttributes.Names.Add(name);
    }
}
