using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class HtmlInlineRenderHookTests
{
    [Test]
    public void HtmlRenderer_UsesLinkRenderHookWhenHandled()
    {
        MarkdownDocument document = MarkdownParser.Parse("[docs](/docs \"Docs\")");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderLinksWith((context, node) =>
            {
                context.Writer.Write("<a data-url=\"");
                context.Writer.WriteEscapedAttribute(node.GetAttributeValue("destination"));
                context.Writer.Write("\">");
                HtmlRenderer.RenderChildren(context, node);
                context.Writer.Write("</a>");
                return true;
            })
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p><a data-url=\"/docs\">docs</a></p>\n"));
    }

    [Test]
    public void HtmlRenderer_UsesImageRenderHookWhenHandled()
    {
        MarkdownDocument document = MarkdownParser.Parse("![alt](/img.png \"Image\")");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderImagesWith((context, node) =>
            {
                context.Writer.Write("<picture data-src=\"");
                context.Writer.WriteEscapedAttribute(node.GetAttributeValue("destination"));
                context.Writer.Write("\" data-alt=\"");
                context.Writer.WriteEscapedAttribute(HtmlRenderer.GetPlainText(node));
                context.Writer.Write("\"></picture>");
                return true;
            })
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p><picture data-src=\"/img.png\" data-alt=\"alt\"></picture></p>\n"));
    }

    [Test]
    public void HtmlRenderer_UsesRawHtmlHookForBlockAndInlineNodes()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode htmlBlock = MarkdownNode.Create(MarkdownNodeKind.HtmlBlock);
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode htmlInline = MarkdownNode.Create(MarkdownNodeKind.HtmlInline);
        htmlBlock.SetLiteral("<div>block</div>");
        text.SetLiteral("inline ");
        htmlInline.SetLiteral("<span>raw</span>");
        paragraph.AppendChild(text);
        paragraph.AppendChild(htmlInline);
        document.Root.AppendChild(htmlBlock);
        document.Root.AppendChild(paragraph);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderRawHtmlWith((context, node) =>
            {
                context.Writer.Write("<raw kind=\"");
                context.Writer.Write(node.Kind.ToString());
                context.Writer.Write("\">");
                context.Writer.WriteEscapedText(node.Literal);
                context.Writer.Write("</raw>");
                if (node.Kind == MarkdownNodeKind.HtmlBlock)
                {
                    HtmlRenderer.WriteLineEnding(context);
                }

                return true;
            })
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<raw kind=\"HtmlBlock\">&lt;div&gt;block&lt;/div&gt;</raw>\n" +
            "<p>inline <raw kind=\"HtmlInline\">&lt;span&gt;raw&lt;/span&gt;</raw></p>\n"));
    }

    [Test]
    public void HtmlRenderer_FallsBackWhenHooksDecline()
    {
        MarkdownDocument document = MarkdownParser.Parse("[docs](/docs) ![alt](/img.png) <span>raw</span>");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderLinksWith((context, node) =>
            {
                return false;
            })
            .RenderImagesWith((context, node) =>
            {
                return false;
            })
            .RenderRawHtmlWith((context, node) =>
            {
                return false;
            })
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p><a href=\"/docs\">docs</a> <img src=\"/img.png\" alt=\"alt\"> &lt;span&gt;raw&lt;/span&gt;</p>\n"));
    }

    [Test]
    public void Builder_SnapshotsAndClearsInlineRenderHooks()
    {
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderLinksWith((context, node) =>
            {
                context.Writer.Write("<link></link>");
                return true;
            })
            .RenderImagesWith((context, node) =>
            {
                context.Writer.Write("<image></image>");
                return true;
            })
            .RenderRawHtmlWith((context, node) =>
            {
                context.Writer.Write("<raw></raw>");
                return true;
            });
        MarkdownPipeline withHooks = builder.Build();
        MarkdownPipeline withoutHooks = builder
            .ClearLinkRenderHook()
            .ClearImageRenderHook()
            .ClearRawHtmlRenderHook()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("[x](/x) ![y](/y) <z>");

        Assert.That(HtmlRenderer.Render(document, withHooks), Is.EqualTo("<p><link></link> <image></image> <raw></raw></p>\n"));
        Assert.That(HtmlRenderer.Render(document, withoutHooks), Is.EqualTo("<p><a href=\"/x\">x</a> <img src=\"/y\" alt=\"y\"> &lt;z&gt;</p>\n"));
    }

    [Test]
    public void InlineRenderHookHelpersRejectInvalidArguments()
    {
        StringWriter writer = new StringWriter();
        HtmlRenderContext context = new HtmlRenderContext(new HtmlWriter(writer), MarkdownPipeline.CommonMarkPreset());
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Link);
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();

        Assert.That(() => builder.RenderLinksWith(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.RenderImagesWith(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.RenderRawHtmlWith(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.TryRenderWithRules(null!, node), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.TryRenderWithRules(context, null!), Throws.TypeOf<ArgumentNullException>());
    }
}
