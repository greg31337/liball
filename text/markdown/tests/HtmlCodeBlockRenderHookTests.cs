using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class HtmlCodeBlockRenderHookTests
{
    [Test]
    public void HtmlRenderer_UsesCodeBlockRenderHookWhenHandled()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode code = MarkdownNode.Create(MarkdownNodeKind.FencedCodeBlock);
        code.SetAttribute("info", "csharp");
        code.SetLiteral("x < y");
        document.Root.AppendChild(code);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderCodeBlocksWith((context, node) =>
            {
                context.Writer.Write("<pre data-info=\"");
                context.Writer.WriteEscapedAttribute(node.GetAttributeValue("info"));
                context.Writer.Write("\">");
                context.Writer.WriteEscapedText(node.Literal);
                context.Writer.Write("</pre>");
                HtmlRenderer.WriteLineEnding(context);
                return true;
            })
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<pre data-info=\"csharp\">x &lt; y</pre>\n"));
    }

    [Test]
    public void HtmlRenderer_FallsBackWhenCodeBlockRenderHookDeclines()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode code = MarkdownNode.Create(MarkdownNodeKind.FencedCodeBlock);
        code.SetAttribute("info", "csharp");
        code.SetLiteral("x");
        document.Root.AppendChild(code);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderCodeBlocksWith((context, node) =>
            {
                return false;
            })
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<pre><code class=\"language-csharp\">x\n</code></pre>\n"));
    }

    [Test]
    public void HtmlRenderer_InvokesCodeBlockHookForIndentedBlocks()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode code = MarkdownNode.Create(MarkdownNodeKind.IndentedCodeBlock);
        code.SetLiteral("indented");
        document.Root.AppendChild(code);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderCodeBlocksWith((context, node) =>
            {
                context.Writer.Write("<code-block kind=\"");
                context.Writer.Write(node.Kind.ToString());
                context.Writer.Write("\">");
                context.Writer.WriteEscapedText(node.Literal);
                context.Writer.Write("</code-block>");
                HtmlRenderer.WriteLineEnding(context);
                return true;
            })
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<code-block kind=\"IndentedCodeBlock\">indented</code-block>\n"));
    }

    [Test]
    public void Builder_SnapshotsAndClearsCodeBlockHook()
    {
        MarkdownPipelineBuilder builder = MarkdownPipelineBuilder.CommonMarkPreset()
            .RenderCodeBlocksWith((context, node) =>
            {
                context.Writer.Write("<handled></handled>");
                HtmlRenderer.WriteLineEnding(context);
                return true;
            });
        MarkdownPipeline withHook = builder.Build();
        MarkdownPipeline withoutHook = builder.ClearCodeBlockRenderHook().Build();
        MarkdownDocument document = MarkdownParser.Parse("```text\nx\n```");

        Assert.That(HtmlRenderer.Render(document, withHook), Is.EqualTo("<handled></handled>\n"));
        Assert.That(HtmlRenderer.Render(document, withoutHook), Is.EqualTo("<pre><code class=\"language-text\">x\n</code></pre>\n"));
    }

    [Test]
    public void CodeBlockHookHelpersRejectInvalidArguments()
    {
        StringWriter writer = new StringWriter();
        HtmlRenderContext context = new HtmlRenderContext(new HtmlWriter(writer), MarkdownPipeline.CommonMarkPreset());
        MarkdownNode code = MarkdownNode.Create(MarkdownNodeKind.FencedCodeBlock);
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();

        Assert.That(() => builder.RenderCodeBlocksWith(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.TryRenderWithRules(null!, code), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.TryRenderWithRules(context, null!), Throws.TypeOf<ArgumentNullException>());
    }
}
