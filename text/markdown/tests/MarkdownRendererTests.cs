using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownRendererTests
{
    [Test]
    public void MarkdownRenderer_RendersEmptyDocumentAsEmptyString()
    {
        MarkdownDocument document = MarkdownParser.Parse("");

        string markdown = MarkdownRenderer.Render(document);

        Assert.That(markdown, Is.EqualTo(""));
    }

    [Test]
    public void MarkdownRenderer_WritesToProvidedTextWriter()
    {
        MarkdownDocument document = MarkdownParser.Parse("");
        StringWriter writer = new StringWriter();

        MarkdownRenderer.Render(document, writer, MarkdownPipeline.CommonMarkPreset());

        Assert.That(writer.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void MarkdownRenderer_RendersParagraphText()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);

        text.SetLiteral("hello");
        paragraph.AppendChild(text);
        document.Root.AppendChild(paragraph);

        string markdown = MarkdownRenderer.Render(document);

        Assert.That(markdown, Is.EqualTo("hello\n"));
    }

    [Test]
    public void MarkdownRenderer_RendersCommonMarkLeafBlocks()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode heading = MarkdownNode.Create(MarkdownNodeKind.Heading);
        MarkdownNode headingText = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode paragraphText = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode thematicBreak = MarkdownNode.Create(MarkdownNodeKind.ThematicBreak);
        MarkdownNode codeBlock = MarkdownNode.Create(MarkdownNodeKind.FencedCodeBlock);
        MarkdownNode htmlBlock = MarkdownNode.Create(MarkdownNodeKind.HtmlBlock);
        MarkdownNode reference = MarkdownNode.Create(MarkdownNodeKind.LinkReferenceDefinition);

        heading.SetAttribute("level", "2");
        headingText.SetLiteral("Title");
        heading.AppendChild(headingText);
        paragraphText.SetLiteral("a * b & c");
        paragraph.AppendChild(paragraphText);
        codeBlock.SetLiteral("Console.WriteLine(\"hi\");");
        codeBlock.SetAttribute("info", "csharp");
        htmlBlock.SetLiteral("<div>raw</div>");
        reference.SetAttribute("label", "ref");
        reference.SetAttribute("destination", "/x y");
        reference.SetAttribute("title", "t\"itle");

        document.Root.AppendChild(heading);
        document.Root.AppendChild(paragraph);
        document.Root.AppendChild(thematicBreak);
        document.Root.AppendChild(codeBlock);
        document.Root.AppendChild(htmlBlock);
        document.Root.AppendChild(reference);

        string markdown = MarkdownRenderer.Render(document);

        Assert.That(markdown, Is.EqualTo(
            "## Title\n" +
            "\n" +
            "a \\* b \\& c\n" +
            "\n" +
            "---\n" +
            "\n" +
            "```csharp\n" +
            "Console.WriteLine(\"hi\");\n" +
            "```\n" +
            "\n" +
            "<div>raw</div>\n" +
            "\n" +
            "[ref]: </x y> \"t\\\"itle\"\n"));
    }

    [Test]
    public void MarkdownRenderer_RendersCommonMarkInlineNodes()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode emphasis = MarkdownNode.Create(MarkdownNodeKind.Emphasis);
        MarkdownNode emphasisText = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode strong = MarkdownNode.Create(MarkdownNodeKind.StrongEmphasis);
        MarkdownNode strongText = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode code = MarkdownNode.Create(MarkdownNodeKind.CodeSpan);
        MarkdownNode link = MarkdownNode.Create(MarkdownNodeKind.Link);
        MarkdownNode linkText = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode image = MarkdownNode.Create(MarkdownNodeKind.Image);
        MarkdownNode imageText = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode html = MarkdownNode.Create(MarkdownNodeKind.HtmlInline);

        text.SetLiteral("Hello ");
        emphasisText.SetLiteral("em");
        emphasis.AppendChild(emphasisText);
        strongText.SetLiteral("strong");
        strong.AppendChild(strongText);
        code.SetLiteral("x ` y");
        link.SetAttribute("destination", "/x?a=1&b=2");
        link.SetAttribute("title", "title");
        linkText.SetLiteral("link");
        link.AppendChild(linkText);
        image.SetAttribute("destination", "/img test.png");
        image.SetAttribute("title", "pic");
        imageText.SetLiteral("alt");
        image.AppendChild(imageText);
        html.SetLiteral("<span>raw</span>");

        paragraph.AppendChild(text);
        paragraph.AppendChild(emphasis);
        paragraph.AppendChild(MarkdownNode.Create(MarkdownNodeKind.Text));
        paragraph.GetChild(2).SetLiteral(" ");
        paragraph.AppendChild(strong);
        paragraph.AppendChild(MarkdownNode.Create(MarkdownNodeKind.Text));
        paragraph.GetChild(4).SetLiteral(" ");
        paragraph.AppendChild(code);
        paragraph.AppendChild(MarkdownNode.Create(MarkdownNodeKind.Text));
        paragraph.GetChild(6).SetLiteral(" ");
        paragraph.AppendChild(link);
        paragraph.AppendChild(MarkdownNode.Create(MarkdownNodeKind.Text));
        paragraph.GetChild(8).SetLiteral(" ");
        paragraph.AppendChild(image);
        paragraph.AppendChild(MarkdownNode.Create(MarkdownNodeKind.Text));
        paragraph.GetChild(10).SetLiteral(" ");
        paragraph.AppendChild(html);
        document.Root.AppendChild(paragraph);

        string markdown = MarkdownRenderer.Render(document);

        Assert.That(markdown, Is.EqualTo(
            "Hello *em* **strong** ``x ` y`` [link](/x?a=1&b=2 \"title\") ![alt](</img test.png> \"pic\") <span>raw</span>\n"));
    }

    [Test]
    public void MarkdownRenderer_RendersBreaksWithConfiguredLineEndings()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode first = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode second = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode third = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        first.SetLiteral("a");
        second.SetLiteral("b");
        third.SetLiteral("c");
        paragraph.AppendChild(first);
        paragraph.AppendChild(MarkdownNode.Create(MarkdownNodeKind.SoftLineBreak));
        paragraph.AppendChild(second);
        paragraph.AppendChild(MarkdownNode.Create(MarkdownNodeKind.HardLineBreak));
        paragraph.AppendChild(third);
        document.Root.AppendChild(paragraph);

        string markdown = MarkdownRenderer.Render(document, pipeline);

        Assert.That(markdown, Is.EqualTo("a\r\nb\\\r\nc\r\n"));
    }

    [Test]
    public void MarkdownRenderer_RendersCommonMarkContainerBlocks()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode quote = MarkdownNode.Create(MarkdownNodeKind.BlockQuote);
        MarkdownNode quotedParagraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode quotedText = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownNode list = MarkdownNode.Create(MarkdownNodeKind.List);
        MarkdownNode item = MarkdownNode.Create(MarkdownNodeKind.ListItem);
        MarkdownNode itemParagraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode itemText = MarkdownNode.Create(MarkdownNodeKind.Text);

        quotedText.SetLiteral("quote");
        quotedParagraph.AppendChild(quotedText);
        quote.AppendChild(quotedParagraph);
        list.SetAttribute("type", "ordered");
        list.SetAttribute("start", "3");
        itemText.SetLiteral("item");
        itemParagraph.AppendChild(itemText);
        item.AppendChild(itemParagraph);
        list.AppendChild(item);
        document.Root.AppendChild(quote);
        document.Root.AppendChild(list);

        string markdown = MarkdownRenderer.Render(document);

        Assert.That(markdown, Is.EqualTo(
            "> quote\n" +
            "\n" +
            "3. item\n"));
    }

    [Test]
    public void MarkdownWriter_WritesRawTextAndLineEndings()
    {
        StringWriter writer = new StringWriter();
        MarkdownWriter markdownWriter = new MarkdownWriter(writer);

        markdownWriter.Write("alpha");
        markdownWriter.WriteSpecificLineEnding("\r\n");
        markdownWriter.Write("beta");

        Assert.That(writer.ToString(), Is.EqualTo("alpha\r\nbeta"));
    }

    [Test]
    public void MarkdownRenderContext_CapturesPipelineOptions()
    {
        StringWriter writer = new StringWriter();
        MarkdownWriter markdownWriter = new MarkdownWriter(writer);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();

        MarkdownRenderContext context = new MarkdownRenderContext(markdownWriter, pipeline);

        Assert.That(context.Writer, Is.SameAs(markdownWriter));
        Assert.That(context.Pipeline.GetOptions().LineEnding, Is.EqualTo("\r\n"));
        Assert.That(context.Options.LineEnding, Is.EqualTo("\r\n"));
    }

    [Test]
    public void MarkdownRenderer_WriteLineEndingUsesConfiguredOption()
    {
        StringWriter writer = new StringWriter();
        MarkdownWriter markdownWriter = new MarkdownWriter(writer);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .SetLineEnding("\r\n")
            .Build();
        MarkdownRenderContext context = new MarkdownRenderContext(markdownWriter, pipeline);

        MarkdownRenderer.WriteLineEnding(context);

        Assert.That(writer.ToString(), Is.EqualTo("\r\n"));
    }

    [Test]
    public void MarkdownRenderer_FormatsOrderedMarkersBeyondIntMaximum()
    {
        MarkdownNode list = MarkdownNode.Create(MarkdownNodeKind.List);
        list.SetAttribute("type", "ordered");

        Assert.That(
            MarkdownRenderer.GetListItemMarker(list, 1, int.MaxValue),
            Is.EqualTo("2147483648. "));
    }

    [Test]
    public void MarkdownRenderer_RejectsInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();
        StringWriter writer = new StringWriter();
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        MarkdownWriter markdownWriter = new MarkdownWriter(writer);
        MarkdownRenderContext context = new MarkdownRenderContext(markdownWriter, pipeline);

        Assert.That(() => MarkdownRenderer.Render(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.Render(document, (MarkdownPipeline)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.Render(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.Render(null!, writer, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.Render(document, null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.Render(document, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderNode(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderChildren(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderChildren(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteListItem(null!, document.Root, "- "), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteListItem(context, null!, "- "), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteListItem(context, document.Root, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WritePrefixedLines(null!, "", "", ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WritePrefixedLines(context, null!, "", ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WritePrefixedLines(context, "", null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WritePrefixedLines(context, "", "", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderChildrenToString(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderChildrenToString(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetListItemMarker(null!, 0, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetListItemMarker(document.Root, -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownRenderer.GetCodeFence(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetCodeSpanDelimiter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.NeedsCodeSpanPadding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetLongestRun(null!, '`'), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteLinkDestination(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteLinkDestination(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteOptionalTitle(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteOptionalTitle(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.NeedsAngleDestination(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EscapeText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EscapeBareDestination(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EscapeAngleDestination(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EscapeTitle(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EscapeCharacters(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EscapeCharacters("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.ContainsCharacter(null!, 'x'), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteLiteralWithNormalizedLineEndings(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.WriteLiteralWithNormalizedLineEndings(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.NormalizeLineEndings(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EndsWithLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.StripTrailingLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.TrimMarkerPadding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.ValidateContextAndNode(null!, document.Root), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.ValidateContextAndNode(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownWriter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownRenderContext(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownRenderContext(markdownWriter, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdownWriter.Write(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdownWriter.WriteSpecificLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
