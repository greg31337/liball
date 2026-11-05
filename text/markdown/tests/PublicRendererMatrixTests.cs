using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicRendererMatrixTests
{
    [Test]
    public void EveryParsedNodeKind_RendersThroughAllPublicStringAndStreamingModes()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = CreateMatrixPipeline(markdown);
        string source = CreateMatrixSource();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        MarkdownNodeKind[] expectedKinds = ParsedKinds();
        IMarkdownTreeOperations trees = markdown.GetDataServices().CreateTreeOperations();

        Assert.That(document.Diagnostics.Items, Is.Empty);
        int index = 0;
        while (index < expectedKinds.Length)
        {
            Assert.That(
                trees.FindFirstInDocument(document, expectedKinds[index]),
                Is.Not.Null,
                "missing parsed kind " + expectedKinds[index].ToString());
            index = index + 1;
        }

        AssertPublicOutputEquivalence(markdown, pipeline, source, document);

        string debugTree = markdown.RenderDebugTreeDocumentWithPipeline(document, pipeline);
        string xml = markdown.RenderXmlAstDocumentWithPipeline(document, pipeline);
        index = 0;
        while (index < expectedKinds.Length)
        {
            string kind = expectedKinds[index].ToString();
            Assert.That(debugTree, Does.Contain(kind), "debug tree " + kind);
            Assert.That(xml, Does.Contain("<" + kind), "XML " + kind);
            index = index + 1;
        }
    }

    [Test]
    public void CallerBuiltGenericAttributeAndUnknownKinds_RenderDeterministicallyInEveryMode()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        MarkdownNode block = new MarkdownExtensionNode().Node;
        MarkdownNode paragraph = new MarkdownNode();
        MarkdownNode inline = new MarkdownExtensionNode().Node;
        MarkdownNode attribute = new MarkdownNode();
        MarkdownNode unknown = new MarkdownNode();
        MarkdownNode text = new MarkdownNode();
        paragraph.Kind = MarkdownNodeKind.Paragraph;
        paragraph.Data.Kind = MarkdownNodeKind.Paragraph;
        inline.Kind = MarkdownNodeKind.ExtensionInline;
        inline.Data.Kind = MarkdownNodeKind.ExtensionInline;
        inline.Data.Extension!.IsBlock = false;
        attribute.Kind = MarkdownNodeKind.Attribute;
        attribute.Data.Kind = MarkdownNodeKind.Attribute;
        attribute.Data.Attribute = new MarkdownAttributeNodeData();
        attribute.Data.Attribute.Name = "data-test";
        attribute.Data.Attribute.Value = "value";
        text.Kind = MarkdownNodeKind.Text;
        text.Data.Kind = MarkdownNodeKind.Text;
        text.Literal = "visible";
        block.Literal = "block";
        inline.Literal = "inline";
        services.CreateExtensionNode("block-widget", block);
        services.CreateExtensionNode("inline-widget", inline);
        services.CreateNodeOperations(paragraph).AppendChild(inline);
        services.CreateNodeOperations(paragraph).AppendChild(attribute);
        services.CreateNodeOperations(paragraph).AppendChild(unknown);
        services.CreateNodeOperations(paragraph).AppendChild(text);
        MarkdownDocument document = markdown.CreateDocumentFromNodes(new[] { block, paragraph });

        string html = markdown.RenderHtmlDocument(document);
        string normalized = markdown.RenderMarkdownDocument(document);
        string plainText = markdown.RenderPlainTextDocument(document);
        string debugTree = markdown.RenderDebugTreeDocument(document);
        string xml = markdown.RenderXmlAstDocument(document);

        Assert.That(html, Does.Contain("data-markdown-extension=\"block-widget\""));
        Assert.That(html, Does.Contain("data-markdown-extension=\"inline-widget\""));
        Assert.That(normalized, Does.Contain("visible"));
        Assert.That(plainText, Does.Contain("visible"));
        Assert.That(debugTree, Does.Contain("ExtensionBlock"));
        Assert.That(debugTree, Does.Contain("ExtensionInline"));
        Assert.That(debugTree, Does.Contain("Attribute"));
        Assert.That(debugTree, Does.Contain("Unknown"));
        Assert.That(xml, Does.Contain("<ExtensionBlock"));
        Assert.That(xml, Does.Contain("<ExtensionInline"));
        Assert.That(xml, Does.Contain("<Attribute"));
        Assert.That(xml, Does.Contain("<Unknown"));

        AssertDocumentStreamingMatches(markdown, markdown.CreateCommonMarkPipeline(), document);
    }

    [Test]
    public void HtmlEscapingUriSafetyAndRawHtmlFiltering_RespectPipelinePolicy()
    {
        IMarkdown markdown = new Markdown();
        string source =
            "Text <span>raw & text</span> [bad](javascript:alert) ![bad](javascript:alert)\n\n" +
            "<script>x & y</script>";
        MarkdownPipeline safe = markdown.CreateCommonMarkPipeline();
        MarkdownPipeline unsafePipeline = markdown.CreatePipelineBuilder()
            .AllowUnsafeHtml()
            .AllowUnsafeLinks()
            .Build();
        MarkdownPipeline gfmFiltered = markdown.CreatePipelineBuilder()
            .UseGfm()
            .AllowRawHtml()
            .Build();

        string safeHtml = markdown.RenderHtmlSourceWithPipeline(source, safe);
        string unsafeHtml = markdown.RenderHtmlSourceWithPipeline(source, unsafePipeline);
        string filteredHtml = markdown.RenderHtmlSourceWithPipeline(source, gfmFiltered);

        Assert.That(safeHtml, Does.Contain("&lt;span&gt;raw &amp; text&lt;/span&gt;"));
        Assert.That(safeHtml, Does.Contain("<a>bad</a> <img alt=\"bad\">"));
        Assert.That(safeHtml, Does.Contain("&lt;script&gt;x &amp; y&lt;/script&gt;"));
        Assert.That(unsafeHtml, Does.Contain("<span>raw &amp; text</span>"));
        Assert.That(unsafeHtml, Does.Contain("href=\"javascript:alert\""));
        Assert.That(unsafeHtml, Does.Contain("src=\"javascript:alert\""));
        Assert.That(unsafeHtml, Does.Contain("<script>x & y</script>"));
        Assert.That(filteredHtml, Does.Contain("&lt;script>x & y&lt;/script>"));

        MarkdownDocument callerDocument = new MarkdownDocument();
        MarkdownNode paragraph = new MarkdownNode();
        MarkdownNode text = new MarkdownNode();
        paragraph.Kind = MarkdownNodeKind.Paragraph;
        text.Kind = MarkdownNodeKind.Text;
        text.Literal = "# block * inline & entity";
        markdown.GetDataServices().CreateNodeOperations(paragraph).AppendChild(text);
        markdown.GetDataServices().CreateNodeOperations(callerDocument.Root).AppendChild(paragraph);
        Assert.That(
            markdown.RenderMarkdownDocument(callerDocument),
            Is.EqualTo("\\# block \\* inline \\& entity\n"));
    }

    [Test]
    public void BreakModesXhtmlWrappingAndLineEndings_ProduceExactOutput()
    {
        IMarkdown markdown = new Markdown();
        string source = "a\nb\\\nc ![alt](/image.png)";
        MarkdownPipeline preserve = markdown.CreateCommonMarkPipeline();
        MarkdownPipeline spaces = markdown.CreatePipelineBuilder()
            .SetSoftBreakMode(MarkdownSoftBreakMode.Space)
            .Build();
        MarkdownPipeline hardXhtml = markdown.CreatePipelineBuilder()
            .SetSoftBreakMode(MarkdownSoftBreakMode.HardBreak)
            .UseXhtml()
            .SetLineEnding("\r\n")
            .Build();
        MarkdownPipeline hardWrap = markdown.CreatePipelineBuilder().UseHardWrap().Build();
        MarkdownPipeline wrapping = markdown.CreatePipelineBuilder()
            .SetMaximumLineWidth(12)
            .SetLineEnding("\r\n")
            .Build();

        Assert.That(
            markdown.RenderHtmlSourceWithPipeline(source, preserve),
            Is.EqualTo("<p>a\nb<br>\nc <img src=\"/image.png\" alt=\"alt\"></p>\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline(source, spaces),
            Is.EqualTo("<p>a b<br>\nc <img src=\"/image.png\" alt=\"alt\"></p>\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline(source, hardXhtml),
            Is.EqualTo("<p>a<br />\r\nb<br />\r\nc <img src=\"/image.png\" alt=\"alt\" /></p>\r\n"));
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline("a\nb", hardWrap),
            Is.EqualTo("<p>a<br>\nb</p>\n"));
        Assert.That(
            markdown.RenderMarkdownSourceWithPipeline("alpha beta gamma delta", wrapping),
            Is.EqualTo("alpha beta\r\ngamma delta\r\n"));
        Assert.That(
            markdown.RenderPlainTextSourceWithPipeline(source, hardXhtml),
            Is.EqualTo("a\r\nb\r\nc alt\r\n"));
    }

    [Test]
    public void EveryOutputMode_IsDeterministicAcrossCultures()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = CreateMatrixPipeline(markdown);
        string source = "3. item\n4. next\n\n# I\n\nMath $1.5$ :ship:";
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            string english = RenderAllModes(markdown, pipeline, source);
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            string turkish = RenderAllModes(markdown, pipeline, source);
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");
            string persian = RenderAllModes(markdown, pipeline, source);

            Assert.That(turkish, Is.EqualTo(english));
            Assert.That(persian, Is.EqualTo(english));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void OutputLimits_AreEnforcedByEveryPublicStringAndStreamingRenderer()
    {
        IMarkdown markdown = new Markdown();
        MarkdownDocument document = markdown.ParseSource("long text");
        MarkdownPipeline limited = markdown.CreatePipelineBuilder()
            .SetMaximumOutputSize(4)
            .Build();

        Assert.That(() => markdown.RenderHtmlDocumentWithPipeline(document, limited), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => markdown.RenderMarkdownDocumentWithPipeline(document, limited), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => markdown.RenderPlainTextDocumentWithPipeline(document, limited), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => markdown.RenderDebugTreeDocumentWithPipeline(document, limited), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => markdown.RenderXmlAstDocumentWithPipeline(document, limited), Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.WriteHtmlDocumentWithPipeline(document, new StringWriter(), limited),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.WriteMarkdownDocumentWithPipeline(document, new StringWriter(), limited),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.WritePlainTextDocumentWithPipeline(document, new StringWriter(), limited),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.WriteDebugTreeDocumentWithPipeline(document, new StringWriter(), limited),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.WriteXmlAstDocumentWithPipeline(document, new StringWriter(), limited),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void CustomHtmlRules_CanHandleNodesAndUseEscapedPublicWriterOperations()
    {
        IMarkdown markdown = new Markdown();
        IHtmlRenderRule heading = new TestHtmlRenderRule(
            "renderer.heading",
            (context, node) =>
            {
                if (node.Kind != MarkdownNodeKind.Heading)
                {
                    return false;
                }

                context.Writer.Write("<custom data-literal=\"");
                context.Writer.WriteEscapedAttribute(node.Literal + " & more");
                context.Writer.Write("\">");
                markdown.RenderHtmlChildren(context, node);
                context.Writer.Write("</custom>");
                markdown.WriteHtmlLineEnding(context);
                return true;
            });
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .AddHtmlRenderRule(heading)
            .RenderLinksWith((context, node) =>
            {
                context.Writer.Write("<custom-link>");
                markdown.RenderHtmlChildren(context, node);
                context.Writer.Write("</custom-link>");
                return true;
            })
            .Build();

        string html = markdown.RenderHtmlSourceWithPipeline("# A & B\n\n[go](/url)", pipeline);

        Assert.That(html, Is.EqualTo(
            "<custom data-literal=\"A &amp; B &amp; more\">A &amp; B</custom>\n" +
            "<p><custom-link>go</custom-link></p>\n"));
    }

    public static MarkdownPipeline CreateMatrixPipeline(IMarkdown markdown)
    {
        return markdown.CreatePipelineBuilder()
            .UseAdvancedExtensions()
            .AllowUnsafeHtml()
            .AllowUnsafeLinks()
            .MapEmojiShortcode("ship", "SHIP")
            .RegisterCustomContainer("panel")
            .RegisterCustomFencedBlock("chart")
            .RegisterCustomInlineDelimiter("kbd", "%%", "%%")
            .Build();
    }

    public static string CreateMatrixSource()
    {
        return
            "---\ntitle: Matrix\n---\n" +
            "# Heading {#heading}\n\n" +
            "---\n\n" +
            "    indented code\n\n" +
            "> quote\n\n" +
            "- ordinary\n- [x] task\n\n" +
            "```text\nfenced code\n```\n\n" +
            "<div>html block</div>\n\n" +
            "[ref]: /reference \"Reference\"\n" +
            "[^note]: footnote\n" +
            "*[HTML]: Hyper Text Markup Language\n\n" +
            "| Left | Right |\n| :--- | ---: |\n| cell | value |\n\n" +
            "Term\n: Definition\n\n" +
            "Text *em* **strong** ~~strike~~ `code` [link](/url) ![alt](/image) " +
            "<https://example.com> <span>inline</span> $math$ :ship: H^2^ H~2~ " +
            "++inserted++ ==marked== %%Ctrl%% HTML [ref] [^note] soft\n" +
            "line\\\nhard\n\n" +
            "$$\nblock math\n$$\n\n" +
            "```mermaid\ngraph TD\n```\n\n" +
            "```chart\na < b\n```\n\n" +
            "> [!NOTE]\n> Alert.\n\n" +
            "::: panel \"Panel\"\nInside.\n:::\n\n" +
            "??? details \"More\"\nContent.\n???";
    }

    public static MarkdownNodeKind[] ParsedKinds()
    {
        return new[]
        {
            MarkdownNodeKind.Document,
            MarkdownNodeKind.BlockQuote,
            MarkdownNodeKind.List,
            MarkdownNodeKind.ListItem,
            MarkdownNodeKind.Paragraph,
            MarkdownNodeKind.Heading,
            MarkdownNodeKind.ThematicBreak,
            MarkdownNodeKind.IndentedCodeBlock,
            MarkdownNodeKind.FencedCodeBlock,
            MarkdownNodeKind.HtmlBlock,
            MarkdownNodeKind.LinkReferenceDefinition,
            MarkdownNodeKind.Text,
            MarkdownNodeKind.SoftLineBreak,
            MarkdownNodeKind.HardLineBreak,
            MarkdownNodeKind.CodeSpan,
            MarkdownNodeKind.Emphasis,
            MarkdownNodeKind.StrongEmphasis,
            MarkdownNodeKind.Link,
            MarkdownNodeKind.Image,
            MarkdownNodeKind.HtmlInline,
            MarkdownNodeKind.Table,
            MarkdownNodeKind.TableHead,
            MarkdownNodeKind.TableBody,
            MarkdownNodeKind.TableRow,
            MarkdownNodeKind.TableCell,
            MarkdownNodeKind.Strikethrough,
            MarkdownNodeKind.TaskListItem,
            MarkdownNodeKind.FootnoteDefinition,
            MarkdownNodeKind.FootnoteReference,
            MarkdownNodeKind.DefinitionList,
            MarkdownNodeKind.DefinitionTerm,
            MarkdownNodeKind.DefinitionDescription,
            MarkdownNodeKind.FrontMatter,
            MarkdownNodeKind.MathSpan,
            MarkdownNodeKind.MathBlock,
            MarkdownNodeKind.AbbreviationDefinition,
            MarkdownNodeKind.Abbreviation,
            MarkdownNodeKind.Superscript,
            MarkdownNodeKind.Subscript,
            MarkdownNodeKind.InsertedText,
            MarkdownNodeKind.MarkedText,
            MarkdownNodeKind.DiagramBlock,
            MarkdownNodeKind.Emoji,
            MarkdownNodeKind.AdmonitionBlock,
            MarkdownNodeKind.CustomContainerBlock,
            MarkdownNodeKind.CustomFencedBlock,
            MarkdownNodeKind.CustomInline,
            MarkdownNodeKind.DetailsBlock,
            MarkdownNodeKind.DetailsSummary
        };
    }

    public static void AssertPublicOutputEquivalence(
        IMarkdown markdown,
        MarkdownPipeline pipeline,
        string source,
        MarkdownDocument document)
    {
        StringWriter htmlSource = new StringWriter();
        StringWriter htmlDocument = new StringWriter();
        StringWriter markdownSource = new StringWriter();
        StringWriter markdownDocument = new StringWriter();
        StringWriter textSource = new StringWriter();
        StringWriter textDocument = new StringWriter();
        StringWriter debugSource = new StringWriter();
        StringWriter debugDocument = new StringWriter();
        StringWriter xmlSource = new StringWriter();
        StringWriter xmlDocument = new StringWriter();

        markdown.WriteHtmlSourceWithPipeline(source, htmlSource, pipeline);
        markdown.WriteHtmlDocumentWithPipeline(document, htmlDocument, pipeline);
        markdown.WriteMarkdownSourceWithPipeline(source, markdownSource, pipeline);
        markdown.WriteMarkdownDocumentWithPipeline(document, markdownDocument, pipeline);
        markdown.WritePlainTextSourceWithPipeline(source, textSource, pipeline);
        markdown.WritePlainTextDocumentWithPipeline(document, textDocument, pipeline);
        markdown.WriteDebugTreeSourceWithPipeline(source, debugSource, pipeline);
        markdown.WriteDebugTreeDocumentWithPipeline(document, debugDocument, pipeline);
        markdown.WriteXmlAstSourceWithPipeline(source, xmlSource, pipeline);
        markdown.WriteXmlAstDocumentWithPipeline(document, xmlDocument, pipeline);

        Assert.That(htmlSource.ToString(), Is.EqualTo(markdown.RenderHtmlSourceWithPipeline(source, pipeline)));
        Assert.That(htmlDocument.ToString(), Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(document, pipeline)));
        Assert.That(htmlSource.ToString(), Is.EqualTo(htmlDocument.ToString()));
        Assert.That(markdownSource.ToString(), Is.EqualTo(markdown.RenderMarkdownSourceWithPipeline(source, pipeline)));
        Assert.That(markdownDocument.ToString(), Is.EqualTo(markdown.RenderMarkdownDocumentWithPipeline(document, pipeline)));
        Assert.That(markdownSource.ToString(), Is.EqualTo(markdownDocument.ToString()));
        Assert.That(textSource.ToString(), Is.EqualTo(markdown.RenderPlainTextSourceWithPipeline(source, pipeline)));
        Assert.That(textDocument.ToString(), Is.EqualTo(markdown.RenderPlainTextDocumentWithPipeline(document, pipeline)));
        Assert.That(textSource.ToString(), Is.EqualTo(textDocument.ToString()));
        Assert.That(debugSource.ToString(), Is.EqualTo(markdown.RenderDebugTreeSourceWithPipeline(source, pipeline)));
        Assert.That(debugDocument.ToString(), Is.EqualTo(markdown.RenderDebugTreeDocumentWithPipeline(document, pipeline)));
        Assert.That(debugSource.ToString(), Is.EqualTo(debugDocument.ToString()));
        Assert.That(xmlSource.ToString(), Is.EqualTo(markdown.RenderXmlAstSourceWithPipeline(source, pipeline)));
        Assert.That(xmlDocument.ToString(), Is.EqualTo(markdown.RenderXmlAstDocumentWithPipeline(document, pipeline)));
        Assert.That(xmlSource.ToString(), Is.EqualTo(xmlDocument.ToString()));
    }

    public static void AssertDocumentStreamingMatches(
        IMarkdown markdown,
        MarkdownPipeline pipeline,
        MarkdownDocument document)
    {
        StringWriter html = new StringWriter();
        StringWriter normalized = new StringWriter();
        StringWriter plainText = new StringWriter();
        StringWriter debugTree = new StringWriter();
        StringWriter xml = new StringWriter();
        markdown.WriteHtmlDocumentWithPipeline(document, html, pipeline);
        markdown.WriteMarkdownDocumentWithPipeline(document, normalized, pipeline);
        markdown.WritePlainTextDocumentWithPipeline(document, plainText, pipeline);
        markdown.WriteDebugTreeDocumentWithPipeline(document, debugTree, pipeline);
        markdown.WriteXmlAstDocumentWithPipeline(document, xml, pipeline);
        Assert.That(html.ToString(), Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(document, pipeline)));
        Assert.That(normalized.ToString(), Is.EqualTo(markdown.RenderMarkdownDocumentWithPipeline(document, pipeline)));
        Assert.That(plainText.ToString(), Is.EqualTo(markdown.RenderPlainTextDocumentWithPipeline(document, pipeline)));
        Assert.That(debugTree.ToString(), Is.EqualTo(markdown.RenderDebugTreeDocumentWithPipeline(document, pipeline)));
        Assert.That(xml.ToString(), Is.EqualTo(markdown.RenderXmlAstDocumentWithPipeline(document, pipeline)));
    }

    public static string RenderAllModes(IMarkdown markdown, MarkdownPipeline pipeline, string source)
    {
        return markdown.RenderHtmlSourceWithPipeline(source, pipeline)
            + "\u001f"
            + markdown.RenderMarkdownSourceWithPipeline(source, pipeline)
            + "\u001f"
            + markdown.RenderPlainTextSourceWithPipeline(source, pipeline)
            + "\u001f"
            + markdown.RenderDebugTreeSourceWithPipeline(source, pipeline)
            + "\u001f"
            + markdown.RenderXmlAstSourceWithPipeline(source, pipeline);
    }
}
