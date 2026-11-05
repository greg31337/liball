using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class HtmlRendererTests
{
    [Test]
    public void HtmlRenderer_RendersEmptyDocumentAsEmptyString()
    {
        MarkdownDocument document = MarkdownParser.Parse("");

        string html = HtmlRenderer.Render(document);

        Assert.That(html, Is.EqualTo(""));
    }

    [Test]
    public void HtmlRenderer_WritesToProvidedTextWriter()
    {
        MarkdownDocument document = MarkdownParser.Parse("");
        StringWriter writer = new StringWriter();

        HtmlRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());

        Assert.That(writer.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void HtmlRenderer_TreatsOverflowingNumericAttributesAsInvalid()
    {
        Assert.That(HtmlRenderer.ParsePositiveInteger("2147483647"), Is.EqualTo(int.MaxValue));
        Assert.That(HtmlRenderer.ParsePositiveInteger("2147483648"), Is.EqualTo(0));
        Assert.That(HtmlRenderer.ParsePositiveInteger("999999999999999999999999"), Is.EqualTo(0));
    }

    [Test]
    public void HtmlRenderer_RendersCommonMarkLeafBlocks()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode thematicBreak = MarkdownNode.Create(MarkdownNodeKind.ThematicBreak);
        MarkdownNode indentedCode = MarkdownNode.Create(MarkdownNodeKind.IndentedCodeBlock);
        MarkdownNode fencedCode = MarkdownNode.Create(MarkdownNodeKind.FencedCodeBlock);
        MarkdownNode htmlBlock = MarkdownNode.Create(MarkdownNodeKind.HtmlBlock);
        MarkdownNode reference = MarkdownNode.Create(MarkdownNodeKind.LinkReferenceDefinition);

        heading.SetAttribute("level", "2");
        indentedCode.SetLiteral("a < b");
        fencedCode.SetLiteral("Console.WriteLine(\"hi\");");
        fencedCode.SetAttribute("info", "c\"sharp title");
        htmlBlock.SetLiteral("<div>raw</div>");
        reference.SetAttribute("label", "ref");
        reference.SetAttribute("destination", "/ignored");

        document.Root.AppendChild(heading);
        document.Root.AppendChild(paragraph);
        document.Root.AppendChild(thematicBreak);
        document.Root.AppendChild(indentedCode);
        document.Root.AppendChild(fencedCode);
        document.Root.AppendChild(htmlBlock);
        document.Root.AppendChild(reference);

        string html = HtmlRenderer.Render(document);

        Assert.That(html, Is.EqualTo(
            "<h2></h2>\n" +
            "<p></p>\n" +
            "<hr>\n" +
            "<pre><code>a &lt; b\n" +
            "</code></pre>\n" +
            "<pre><code class=\"language-c&quot;sharp\">Console.WriteLine(&quot;hi&quot;);\n" +
            "</code></pre>\n" +
            "&lt;div&gt;raw&lt;/div&gt;\n"));
    }

    [Test]
    public void HtmlRenderer_RendersCommonMarkContainerBlocks()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode quote = MarkdownNode.Create(MarkdownNodeKind.BlockQuote);
        MarkdownNode quotedParagraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode orderedList = MarkdownNode.Create(MarkdownNodeKind.List);
        MarkdownNode orderedItem = MarkdownNode.Create(MarkdownNodeKind.ListItem);
        MarkdownNode orderedParagraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode bulletList = MarkdownNode.Create(MarkdownNodeKind.List);
        MarkdownNode bulletItem = MarkdownNode.Create(MarkdownNodeKind.ListItem);

        orderedList.SetAttribute("type", "ordered");
        orderedList.SetAttribute("start", "3");
        bulletList.SetAttribute("type", "bullet");

        quote.AppendChild(quotedParagraph);
        orderedItem.AppendChild(orderedParagraph);
        orderedList.AppendChild(orderedItem);
        bulletList.AppendChild(bulletItem);
        document.Root.AppendChild(quote);
        document.Root.AppendChild(orderedList);
        document.Root.AppendChild(bulletList);

        string html = HtmlRenderer.Render(document);

        Assert.That(html, Is.EqualTo(
            "<blockquote>\n" +
            "<p></p>\n" +
            "</blockquote>\n" +
            "<ol start=\"3\">\n" +
            "<li>\n<p></p>\n" +
            "</li>\n" +
            "</ol>\n" +
            "<ul>\n" +
            "<li></li>\n" +
            "</ul>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersRawHtmlWhenAllowed()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode htmlBlock = MarkdownNode.Create(MarkdownNodeKind.HtmlBlock);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AllowRawHtml()
            .Build();

        htmlBlock.SetLiteral("<section>raw</section>");
        document.Root.AppendChild(htmlBlock);

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<section>raw</section>\n"));
    }

    [Test]
    public void HtmlRenderer_UsesConfiguredLineEndingsForBlockOutput()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        heading.SetAttribute("level", "1");
        document.Root.AppendChild(heading);

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<h1></h1>\r\n"));
    }

    [Test]
    public void HtmlRenderer_RendersCommonMarkInlineTextAndFormatting()
    {
        MarkdownDocument document = MarkdownParser.Parse("Hello &amp; <tag> *em* **strong** `x < y`");

        string html = HtmlRenderer.Render(document);

        Assert.That(html, Is.EqualTo(
            "<p>Hello &amp; &lt;tag&gt; <em>em</em> <strong>strong</strong> <code>x &lt; y</code></p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersCommonMarkInlineBreaks()
    {
        MarkdownDocument document = MarkdownParser.Parse("a\nb  \nc\\\nd");

        string html = HtmlRenderer.Render(document);

        Assert.That(html, Is.EqualTo(
            "<p>a\n" +
            "b<br>\n" +
            "c<br>\n" +
            "d</p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersCommonMarkLinksAndImages()
    {
        MarkdownDocument document = MarkdownParser.Parse("[link](/x?a=1&b=2 \"title &amp;\") ![**alt** &amp; `code`](/img.png \"pic\")");

        string html = HtmlRenderer.Render(document);

        Assert.That(html, Is.EqualTo(
            "<p><a href=\"/x?a=1&amp;b=2\" title=\"title &amp;\">link</a> " +
            "<img src=\"/img.png\" alt=\"alt &amp; code\" title=\"pic\"></p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersRawInlineHtmlWhenAllowed()
    {
        MarkdownDocument document = MarkdownParser.Parse("<span>raw</span>");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AllowRawHtml()
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p><span>raw</span></p>\n"));
    }

    [Test]
    public void HtmlRenderer_SuppressesUnsafeInlineDestinationsByDefault()
    {
        MarkdownDocument document = MarkdownParser.Parse("[bad](javascript:alert) ![bad](javascript:alert)");

        string html = HtmlRenderer.Render(document);

        Assert.That(html, Is.EqualTo("<p><a>bad</a> <img alt=\"bad\"></p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersUnsafeInlineDestinationsWhenAllowed()
    {
        MarkdownDocument document = MarkdownParser.Parse("[bad](javascript:alert) ![bad](javascript:alert)");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AllowUnsafeLinks()
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo(
            "<p><a href=\"javascript:alert\">bad</a> <img src=\"javascript:alert\" alt=\"bad\"></p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersXhtmlInlineVoidTags()
    {
        MarkdownDocument document = MarkdownParser.Parse("a\\\n![alt](/img.png)");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .UseXhtml()
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>a<br />\n<img src=\"/img.png\" alt=\"alt\" /></p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersXhtmlThematicBreak()
    {
        MarkdownDocument document = MarkdownParser.Parse("---");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .UseXhtml()
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<hr />\n"));
    }

    [Test]
    public void HtmlRenderer_RendersSoftBreaksAsSpacesWhenConfigured()
    {
        MarkdownDocument document = MarkdownParser.Parse("a\nb");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetSoftBreakMode(MarkdownSoftBreakMode.Space)
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>a b</p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersSoftBreaksAsHardBreaksWhenConfigured()
    {
        MarkdownDocument document = MarkdownParser.Parse("a\nb");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetSoftBreakMode(MarkdownSoftBreakMode.HardBreak)
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>a<br>\nb</p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersSoftBreaksAsHardBreaksWhenHardWrapIsConfigured()
    {
        MarkdownDocument document = MarkdownParser.Parse("a\nb");
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .UseHardWrap()
            .Build();

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<p>a<br>\nb</p>\n"));
    }

    [Test]
    public void HtmlRenderer_RendersRawHtmlWhenUnsafeHtmlIsConfigured()
    {
        MarkdownDocument document = MarkdownParser.Parse("<div>block</div>\n\nx <span>inline</span>");
        MarkdownOptions options = MarkdownOptions.CommonMarkPreset();
        options.AllowUnsafeHtml = true;
        MarkdownPipeline pipeline = MarkdownPipeline.Create(options);

        string html = HtmlRenderer.Render(document, pipeline);

        Assert.That(html, Is.EqualTo("<div>block</div>\n<p>x <span>inline</span></p>\n"));
    }

    [Test]
    public void HtmlWriter_WritesRawAndEscapedText()
    {
        StringWriter writer = new StringWriter();
        HtmlWriter htmlWriter = new HtmlWriter(writer);

        htmlWriter.Write("<raw>");
        htmlWriter.WriteEscapedText(" & < > ");
        htmlWriter.WriteEscapedAttribute("\"quoted\"");

        Assert.That(writer.ToString(), Is.EqualTo("<raw> &amp; &lt; &gt; &quot;quoted&quot;"));
    }

    [Test]
    public void HtmlRenderContext_CapturesPipelineOptions()
    {
        StringWriter writer = new StringWriter();
        HtmlWriter htmlWriter = new HtmlWriter(writer);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .AllowRawHtml()
            .Build();

        HtmlRenderContext context = new HtmlRenderContext(htmlWriter, pipeline);

        Assert.That(context.Writer, Is.SameAs(htmlWriter));
        Assert.That(context.Pipeline.AllowsRawHtml(), Is.True);
        Assert.That(context.Options.AllowRawHtml, Is.True);
    }

    [Test]
    public void HtmlRenderer_RejectsInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();
        StringWriter writer = new StringWriter();
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        HtmlWriter htmlWriter = new HtmlWriter(writer);
        HtmlRenderContext context = new HtmlRenderContext(htmlWriter, pipeline);

        Assert.That(() => HtmlRenderer.Render(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.Render(document, (MarkdownPipeline)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.Render(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.Render(null!, writer, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.Render(document, null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.Render(document, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderNode(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderChildren(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderChildren(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderParagraph(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderParagraph(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.GetHeadingLevel(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.GetListTagName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.GetCodeInfoLanguage(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ParsePositiveInteger(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ValidateContextAndNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ValidateContextAndNode(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ShouldRenderLinkDestination(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ShouldRenderLinkDestination(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ShouldRenderImageDestination(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ShouldRenderImageDestination(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.GetPlainText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WritePlainText(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WritePlainText(writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteHtmlLiteral(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteHtmlLiteral(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.ShouldRenderRawHtml(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.WriteBreakTag(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new HtmlWriter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new HtmlRenderContext(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new HtmlRenderContext(htmlWriter, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => htmlWriter.Write(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => htmlWriter.WriteEscapedText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => htmlWriter.WriteEscapedAttribute(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
