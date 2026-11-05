using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

public class CommonMarkFeatureCase
{
    public string Name;
    public string Source;
    public string Html;
    public string Markdown;
    public string PlainText;
    public MarkdownNodeKind[] ExpectedKinds;
    public MarkdownNodeKind[] ForbiddenKinds;

    public CommonMarkFeatureCase(
        string name,
        string source,
        string html,
        string markdown,
        string plainText,
        MarkdownNodeKind[] expectedKinds,
        MarkdownNodeKind[] forbiddenKinds)
    {
        Name = name;
        Source = source;
        Html = html;
        Markdown = markdown;
        PlainText = plainText;
        ExpectedKinds = expectedKinds;
        ForbiddenKinds = forbiddenKinds;
    }

    public override string ToString()
    {
        return Name;
    }
}

[TestFixture]
public class CommonMarkFeatureMatrixTests
{
    [TestCaseSource(nameof(CommonMarkCases))]
    public void CoreConstruct_ProducesStablePublicAstMapsAndOutputs(CommonMarkFeatureCase feature)
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.AllowRawHtml();
        builder.AllowUnsafeHtml();
        builder.AllowUnsafeLinks();
        MarkdownPipeline pipeline = builder.Build();

        MarkdownDocument document = markdown.ParseSourceWithPipeline(feature.Source, pipeline);
        IMarkdownDocumentOperations operations = services.CreateDocumentOperations(document);
        string html = markdown.RenderHtmlDocumentWithPipeline(document, pipeline);
        string normalized = markdown.RenderMarkdownDocumentWithPipeline(document, pipeline);
        string plainText = markdown.RenderPlainTextDocumentWithPipeline(document, pipeline);
        string debugTree = markdown.RenderDebugTreeDocumentWithPipeline(document, pipeline);
        string xml = markdown.RenderXmlAstDocumentWithPipeline(document, pipeline);

        Assert.That(document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document), feature.Name);
        Assert.That(document.Root.Data.Kind, Is.EqualTo(MarkdownNodeKind.Document), feature.Name);
        Assert.That(operations.GetDiagnosticCount(), Is.EqualTo(0), feature.Name);
        Assert.That(operations.HasDiagnostics(), Is.False, feature.Name);
        Assert.That(html, Is.EqualTo(feature.Html), feature.Name);
        Assert.That(normalized, Is.EqualTo(feature.Markdown), feature.Name);
        Assert.That(plainText, Is.EqualTo(feature.PlainText), feature.Name);
        Assert.That(
            markdown.RenderHtmlSourceWithPipeline(feature.Source, pipeline),
            Is.EqualTo(html),
            feature.Name + " source HTML");
        Assert.That(
            markdown.RenderMarkdownSourceWithPipeline(feature.Source, pipeline),
            Is.EqualTo(normalized),
            feature.Name + " source Markdown");
        Assert.That(
            markdown.RenderPlainTextSourceWithPipeline(feature.Source, pipeline),
            Is.EqualTo(plainText),
            feature.Name + " source plain text");
        Assert.That(
            markdown.RenderDebugTreeSourceWithPipeline(feature.Source, pipeline),
            Is.EqualTo(debugTree),
            feature.Name + " source debug tree");
        Assert.That(
            markdown.RenderXmlAstSourceWithPipeline(feature.Source, pipeline),
            Is.EqualTo(xml),
            feature.Name + " source XML");

        AssertExpectedKinds(document.Root, feature.ExpectedKinds, feature.Name);
        AssertForbiddenKinds(document.Root, feature.ForbiddenKinds, feature.Name);
        AssertTypedDataAndWrappers(document.Root, services, feature.Name);
        AssertMappedBlockNodes(document.Root, operations.GetSourceMapEntries(), feature.Name);
        AssertStructuredOutputKinds(feature.ExpectedKinds, debugTree, xml, feature.Name);

        MarkdownDocument roundTrip = markdown.ParseSourceWithPipeline(normalized, pipeline);
        Assert.That(roundTrip.Diagnostics.Items, Is.Empty, feature.Name);
        Assert.That(
            markdown.RenderHtmlDocumentWithPipeline(roundTrip, pipeline),
            Is.EqualTo(feature.Html),
            feature.Name + " HTML round trip");
        Assert.That(
            markdown.RenderMarkdownDocumentWithPipeline(roundTrip, pipeline),
            Is.EqualTo(normalized),
            feature.Name + " Markdown round trip");
        Assert.That(
            markdown.RenderPlainTextDocumentWithPipeline(roundTrip, pipeline),
            Is.EqualTo(feature.PlainText),
            feature.Name + " plain-text round trip");
        AssertExpectedKinds(roundTrip.Root, feature.ExpectedKinds, feature.Name + " round trip");
        AssertForbiddenKinds(roundTrip.Root, feature.ForbiddenKinds, feature.Name + " round trip");
    }

    public static IEnumerable<CommonMarkFeatureCase> CommonMarkCases()
    {
        yield return Case(
            "Paragraph", "plain text",
            "<p>plain text</p>\n", "plain text\n", "plain text\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text),
            Kinds());
        yield return Case(
            "ATX heading", "# heading",
            "<h1>heading</h1>\n", "# heading\n", "heading\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Heading, MarkdownNodeKind.Text),
            Kinds());
        yield return Case(
            "Setext heading", "heading\n---",
            "<h2>heading</h2>\n", "## heading\n", "heading\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Heading, MarkdownNodeKind.Text),
            Kinds(MarkdownNodeKind.ThematicBreak));
        yield return Case(
            "Thematic break", "---",
            "<hr>\n", "---\n", "",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.ThematicBreak),
            Kinds(MarkdownNodeKind.Heading, MarkdownNodeKind.Paragraph));
        yield return Case(
            "Block quote", "> quote",
            "<blockquote>\n<p>quote</p>\n</blockquote>\n", "> quote\n", "quote\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.BlockQuote, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text),
            Kinds());
        yield return Case(
            "Bullet list", "- one\n- two",
            "<ul>\n<li>one</li>\n<li>two</li>\n</ul>\n", "- one\n- two\n", "one\ntwo\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.List, MarkdownNodeKind.ListItem, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text),
            Kinds());
        yield return Case(
            "Ordered list", "3. three\n4. four",
            "<ol start=\"3\">\n<li>three</li>\n<li>four</li>\n</ol>\n", "3. three\n4. four\n", "three\nfour\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.List, MarkdownNodeKind.ListItem, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text),
            Kinds());
        yield return Case(
            "Indented code block", "    code\n\n    more",
            "<pre><code>code\n\nmore\n</code></pre>\n",
            "    code\n    \n    more\n",
            "code\n\nmore\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.IndentedCodeBlock),
            Kinds(MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text));
        yield return Case(
            "Fenced code block", "```lang\ncode\n```",
            "<pre><code class=\"language-lang\">code\n</code></pre>\n", "```lang\ncode\n```\n", "code\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.FencedCodeBlock),
            Kinds(MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text));
        yield return Case(
            "HTML block", "<div>raw</div>",
            "<div>raw</div>\n", "<div>raw</div>\n", "<div>raw</div>\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.HtmlBlock),
            Kinds(MarkdownNodeKind.Paragraph, MarkdownNodeKind.HtmlInline));
        yield return Case(
            "Link reference definition", "[ref]: /url \"title\"\n\n[ref]",
            "<p><a href=\"/url\" title=\"title\">ref</a></p>\n",
            "[ref]: /url \"title\"\n\n[ref](/url \"title\")\n", "ref\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.LinkReferenceDefinition, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Link, MarkdownNodeKind.Text),
            Kinds());
        yield return Case(
            "Escapes and entities", "\\*literal\\* &amp;",
            "<p>*literal* &amp;</p>\n", "\\*literal\\* \\&\n", "*literal* &\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text),
            Kinds(MarkdownNodeKind.Emphasis));
        yield return Case(
            "Soft and hard line breaks", "soft\nline\\\nhard",
            "<p>soft\nline<br>\nhard</p>\n", "soft\nline\\\nhard\n", "soft\nline\nhard\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text, MarkdownNodeKind.SoftLineBreak, MarkdownNodeKind.HardLineBreak),
            Kinds());
        yield return Case(
            "Code span precedence", "`*code*`",
            "<p><code>*code*</code></p>\n", "`*code*`\n", "*code*\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Paragraph, MarkdownNodeKind.CodeSpan),
            Kinds(MarkdownNodeKind.Emphasis));
        yield return Case(
            "Emphasis and strong emphasis", "*em* **strong**",
            "<p><em>em</em> <strong>strong</strong></p>\n", "*em* **strong**\n", "em strong\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text, MarkdownNodeKind.Emphasis, MarkdownNodeKind.StrongEmphasis),
            Kinds());
        yield return Case(
            "Links and images", "[link](/url \"t\") ![alt](/img)",
            "<p><a href=\"/url\" title=\"t\">link</a> <img src=\"/img\" alt=\"alt\"></p>\n",
            "[link](/url \"t\") ![alt](/img)\n", "link alt\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text, MarkdownNodeKind.Link, MarkdownNodeKind.Image),
            Kinds());
        yield return Case(
            "Autolinks", "<https://example.com> <a@example.com>",
            "<p><a href=\"https://example.com\">https://example.com</a> <a href=\"mailto:a@example.com\">a@example.com</a></p>\n",
            "[https://example.com](https://example.com) [a@example.com](mailto:a@example.com)\n",
            "https://example.com a@example.com\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text, MarkdownNodeKind.Link),
            Kinds(MarkdownNodeKind.HtmlInline));
        yield return Case(
            "Inline HTML", "before <span>raw</span> after",
            "<p>before <span>raw</span> after</p>\n", "before <span>raw</span> after\n", "before <span>raw</span> after\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Paragraph, MarkdownNodeKind.Text, MarkdownNodeKind.HtmlInline),
            Kinds());
        yield return Case(
            "Block and inline precedence", "paragraph\n---\n\n    *code*",
            "<h2>paragraph</h2>\n<pre><code>*code*\n</code></pre>\n",
            "## paragraph\n\n    *code*\n", "paragraph\n\n*code*\n",
            Kinds(MarkdownNodeKind.Document, MarkdownNodeKind.Heading, MarkdownNodeKind.Text, MarkdownNodeKind.IndentedCodeBlock),
            Kinds(MarkdownNodeKind.ThematicBreak, MarkdownNodeKind.Emphasis));
    }

    public static CommonMarkFeatureCase Case(
        string name,
        string source,
        string html,
        string markdown,
        string plainText,
        MarkdownNodeKind[] expectedKinds,
        MarkdownNodeKind[] forbiddenKinds)
    {
        return new CommonMarkFeatureCase(name, source, html, markdown, plainText, expectedKinds, forbiddenKinds);
    }

    public static MarkdownNodeKind[] Kinds(params MarkdownNodeKind[] kinds)
    {
        return kinds;
    }

    public static void AssertExpectedKinds(MarkdownNode root, MarkdownNodeKind[] kinds, string name)
    {
        int index = 0;
        while (index < kinds.Length)
        {
            Assert.That(ContainsKind(root, kinds[index]), Is.True, name + " missing " + kinds[index].ToString());
            index = index + 1;
        }
    }

    public static void AssertForbiddenKinds(MarkdownNode root, MarkdownNodeKind[] kinds, string name)
    {
        int index = 0;
        while (index < kinds.Length)
        {
            Assert.That(ContainsKind(root, kinds[index]), Is.False, name + " unexpectedly contained " + kinds[index].ToString());
            index = index + 1;
        }
    }

    public static bool ContainsKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node.Kind == kind)
        {
            return true;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            if (ContainsKind(node.Children[index], kind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    public static void AssertTypedDataAndWrappers(MarkdownNode node, IMarkdownDataServices services, string name)
    {
        Assert.That(node.Data.Kind, Is.EqualTo(node.Kind), name + " typed data");
        if (IsCoreBlockKind(node.Kind))
        {
            Assert.That(services.CreateBlockNode(node).Node, Is.SameAs(node), name + " block wrapper");
        }
        else if (IsCoreInlineKind(node.Kind))
        {
            Assert.That(services.CreateInlineNode(node).Node, Is.SameAs(node), name + " inline wrapper");
        }

        if (node.Kind == MarkdownNodeKind.Heading)
        {
            Assert.That(node.Data.Heading, Is.Not.Null, name + " heading data");
            Assert.That(node.Data.Heading!.Level, Is.GreaterThan(0), name + " heading level");
        }
        else if (node.Kind == MarkdownNodeKind.List)
        {
            Assert.That(node.Data.List, Is.Not.Null, name + " list data");
        }
        else if (node.Kind == MarkdownNodeKind.ListItem)
        {
            Assert.That(node.Data.ListItem, Is.Not.Null, name + " list-item data");
        }
        else if (node.Kind == MarkdownNodeKind.IndentedCodeBlock || node.Kind == MarkdownNodeKind.FencedCodeBlock)
        {
            Assert.That(node.Data.CodeBlock, Is.Not.Null, name + " code-block data");
        }
        else if (node.Kind == MarkdownNodeKind.Link)
        {
            Assert.That(node.Data.Link, Is.Not.Null, name + " link data");
        }
        else if (node.Kind == MarkdownNodeKind.Image)
        {
            Assert.That(node.Data.Image, Is.Not.Null, name + " image data");
        }
        else if (node.Kind == MarkdownNodeKind.LinkReferenceDefinition)
        {
            Assert.That(node.Data.Reference, Is.Not.Null, name + " reference data");
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            AssertTypedDataAndWrappers(node.Children[index], services, name);
            index = index + 1;
        }
    }

    public static void AssertMappedBlockNodes(MarkdownNode node, MarkdownSourceMapEntry[] entries, string name)
    {
        if ((node.Kind == MarkdownNodeKind.Document || IsCoreBlockKind(node.Kind))
            && node.Span.End.Offset > node.Span.Start.Offset)
        {
            Assert.That(ContainsMappedNode(entries, node), Is.True, name + " source map " + node.Kind.ToString());
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            AssertMappedBlockNodes(node.Children[index], entries, name);
            index = index + 1;
        }
    }

    public static bool ContainsMappedNode(MarkdownSourceMapEntry[] entries, MarkdownNode node)
    {
        int index = 0;
        while (index < entries.Length)
        {
            if (object.ReferenceEquals(entries[index].Node, node))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    public static void AssertStructuredOutputKinds(
        MarkdownNodeKind[] kinds,
        string debugTree,
        string xml,
        string name)
    {
        int index = 0;
        while (index < kinds.Length)
        {
            string kind = kinds[index].ToString();
            Assert.That(debugTree, Does.Contain(kind), name + " debug tree " + kind);
            Assert.That(xml, Does.Contain("<" + kind), name + " XML " + kind);
            index = index + 1;
        }
    }

    public static bool IsCoreBlockKind(MarkdownNodeKind kind)
    {
        return kind == MarkdownNodeKind.BlockQuote
            || kind == MarkdownNodeKind.List
            || kind == MarkdownNodeKind.ListItem
            || kind == MarkdownNodeKind.Paragraph
            || kind == MarkdownNodeKind.Heading
            || kind == MarkdownNodeKind.ThematicBreak
            || kind == MarkdownNodeKind.IndentedCodeBlock
            || kind == MarkdownNodeKind.FencedCodeBlock
            || kind == MarkdownNodeKind.HtmlBlock
            || kind == MarkdownNodeKind.LinkReferenceDefinition;
    }

    public static bool IsCoreInlineKind(MarkdownNodeKind kind)
    {
        return kind == MarkdownNodeKind.Text
            || kind == MarkdownNodeKind.SoftLineBreak
            || kind == MarkdownNodeKind.HardLineBreak
            || kind == MarkdownNodeKind.CodeSpan
            || kind == MarkdownNodeKind.Emphasis
            || kind == MarkdownNodeKind.StrongEmphasis
            || kind == MarkdownNodeKind.Link
            || kind == MarkdownNodeKind.Image
            || kind == MarkdownNodeKind.HtmlInline;
    }
}
