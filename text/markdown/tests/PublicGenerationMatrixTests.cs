using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicGenerationMatrixTests
{
    [Test]
    public void EveryWriterAndBuilderOperation_ProducesEquivalentStableMarkdown()
    {
        IMarkdown markdown = new Markdown();
        StringWriter output = new StringWriter();
        IMarkdownWriter writer = markdown.CreateMarkdownWriter(output);
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilder();

        WriteEveryConstruct(writer);
        BuildEveryConstruct(builder);

        string expected =
            "\\# literal \\* \\& raw\r\n\r\n" +
            "## Heading \\*\r\n\r\n" +
            "paragraph\r\n\\> continuation\r\n\r\n" +
            "---\r\n\r\n" +
            "- bullet \\*\r\n\r\n" +
            "3. ordered\r\n\r\n" +
            "- [x] done\r\n- [ ] open\r\n\r\n" +
            "> quote\r\n> \\> nested\r\n\r\n" +
            "````text\r\na ``` b\r\n````\r\n\r\n" +
            "inline *em \\** **strong \\*** ~~old~~ ``a ` b`` " +
            "[link \\]](</a b>) ![alt \\]](</i(x)>) $x+y$ [^n1]\r\n\r\n" +
            "| H\\|1 | H2 |\r\n| --- | --- |\r\n| A | B \\* |\r\n\r\n" +
            "$$\r\nx = y\r\n$$\r\n\r\n" +
            "!!! note \"Title \\\"quoted\\\"\"\r\n    - raw\r\n      - nested\r\n\r\n" +
            "::: panel \"Panel\"\r\ninside **raw**\r\n:::\r\n\r\n" +
            "[^n1]: foot \\*text\\*\r\n    \\> second\r\n\r\n" +
            "raw *markdown*\r\n";

        Assert.That(output.ToString(), Is.EqualTo(expected));
        Assert.That(builder.ToMarkdown(), Is.EqualTo(expected));
        Assert.That(writer.EscapeText("# a * b\n1. item\n    code"), Is.EqualTo(
            "\\# a \\* b\n1\\. item\n&#32;   code"));
    }

    [Test]
    public void EscapedParagraphText_RemainsOneParagraphAtAmbiguousBlockBoundaries()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilder();
        string text =
            "# heading\n" +
            "> quote\n" +
            "- item\n" +
            "+ item\n" +
            "1. item\n" +
            "123456789) item\n" +
            "---\n" +
            "===\n" +
            "~~~\n" +
            "    indented\n" +
            "\tindented\n" +
            "[link] <tag> ![image] & *em* _em_ `code` \\slash";

        builder.Paragraph(text);
        string generated = builder.ToMarkdown();
        MarkdownDocument document = builder.ToDocument();

        Assert.That(generated, Is.EqualTo(
            "\\# heading\n" +
            "\\> quote\n" +
            "\\- item\n" +
            "\\+ item\n" +
            "1\\. item\n" +
            "123456789\\) item\n" +
            "\\---\n" +
            "\\===\n" +
            "\\~~~\n" +
            "&#32;   indented\n" +
            "&#9;indented\n" +
            "\\[link\\] \\<tag\\> \\!\\[image\\] \\& \\*em\\* \\_em\\_ \\`code\\` \\\\slash\n"));
        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(markdown.RenderPlainTextDocument(document), Is.EqualTo(text + "\n"));
    }

    [Test]
    public void BuilderClearAndDocumentConversions_PreserveConfigurationAndMatchDirectParsing()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseGfm()
            .SetLineEnding("\r\n")
            .SetMaximumOutputSize(1000)
            .Build();
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilderWithPipeline(pipeline);

        builder.Heading(1, "discarded");
        Assert.That(builder.Clear(), Is.SameAs(builder));
        Assert.That(builder.ToMarkdown(), Is.Empty);
        builder.Paragraph("kept").TaskList(
            new string[] { "done", "open" },
            new bool[] { true, false });

        string generated = builder.ToMarkdown();
        MarkdownDocument commonMarkDocument = builder.ToDocument();
        MarkdownDocument pipelineDocument = builder.ToDocumentWithPipeline(pipeline);
        MarkdownDocument directlyParsed = markdown.ParseSourceWithPipeline(generated, pipeline);

        Assert.That(generated, Is.EqualTo("kept\r\n- [x] done\r\n- [ ] open\r\n"));
        Assert.That(commonMarkDocument.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(pipelineDocument.Root.GetChild(1).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.TaskListItem));
        Assert.That(
            markdown.RenderXmlAstDocumentWithPipeline(pipelineDocument, pipeline),
            Is.EqualTo(markdown.RenderXmlAstDocumentWithPipeline(directlyParsed, pipeline)));
        Assert.That(
            markdown.RenderHtmlDocumentWithPipeline(pipelineDocument, pipeline),
            Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(directlyParsed, pipeline)));
    }

    [Test]
    public void NestedExtensionGeneration_ProducesEquivalentDocumentsAndStableRoundTrips()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseAdvancedExtensions()
            .RegisterCustomContainer("panel")
            .Build();
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilderWithPipeline(pipeline);
        IMarkdownTreeOperations trees = markdown.GetDataServices().CreateTreeOperations();

        builder.CustomContainer(
            "panel",
            "Outer",
            "!!! note \"Inner\"\n" +
            "    - parent\n" +
            "      - child\n\n" +
            "Paragraph [link](/inside) and ~~old~~.");

        string generated = builder.ToMarkdown();
        MarkdownDocument built = builder.ToDocumentWithPipeline(pipeline);
        MarkdownDocument parsed = markdown.ParseSourceWithPipeline(generated, pipeline);
        string normalized = markdown.RenderMarkdownDocumentWithPipeline(built, pipeline);
        MarkdownDocument reparsed = markdown.ParseSourceWithPipeline(normalized, pipeline);
        string normalizedAgain = markdown.RenderMarkdownDocumentWithPipeline(reparsed, pipeline);

        Assert.That(trees.FindFirstInDocument(built, MarkdownNodeKind.CustomContainerBlock), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(built, MarkdownNodeKind.AdmonitionBlock), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(built, MarkdownNodeKind.List), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(built, MarkdownNodeKind.Link), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(built, MarkdownNodeKind.Strikethrough), Is.Not.Null);
        Assert.That(
            markdown.RenderHtmlDocumentWithPipeline(built, pipeline),
            Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(parsed, pipeline)));
        Assert.That(normalizedAgain, Is.EqualTo(normalized));
        Assert.That(
            markdown.RenderHtmlDocumentWithPipeline(reparsed, pipeline),
            Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(built, pipeline)));
    }

    public static void WriteEveryConstruct(IMarkdownWriter writer)
    {
        writer.SetLineEnding("\r\n");
        writer.WriteText("# literal * &");
        writer.WriteRawMarkdown(" raw");
        writer.WriteLineEnding();
        writer.WriteBlankLine();
        writer.WriteHeading(2, "Heading *");
        writer.WriteBlankLine();
        writer.WriteParagraph("paragraph\n> continuation");
        writer.WriteBlankLine();
        writer.WriteThematicBreak();
        writer.WriteBlankLine();
        writer.WriteBulletList(new string[] { "bullet *" });
        writer.WriteBlankLine();
        writer.WriteOrderedList(new string[] { "ordered" }, 3);
        writer.WriteBlankLine();
        writer.WriteTaskList(new string[] { "done", "open" }, new bool[] { true, false });
        writer.WriteBlankLine();
        writer.WriteBlockQuote("quote\n> nested");
        writer.WriteBlankLine();
        writer.WriteCodeBlock("text", "a ``` b");
        writer.WriteBlankLine();
        writer.WriteText("inline ");
        writer.WriteEmphasis("em *");
        writer.WriteText(" ");
        writer.WriteStrongEmphasis("strong *");
        writer.WriteText(" ");
        writer.WriteStrikethrough("old");
        writer.WriteText(" ");
        writer.WriteCodeSpan("a ` b");
        writer.WriteText(" ");
        writer.WriteLink("link ]", "/a b");
        writer.WriteText(" ");
        writer.WriteImage("alt ]", "/i(x)");
        writer.WriteText(" ");
        writer.WriteMathSpan("x+y");
        writer.WriteText(" ");
        writer.WriteFootnoteReference("n1");
        writer.WriteLineEnding();
        writer.WriteBlankLine();
        writer.WriteTable(new string[][]
        {
            new string[] { "H|1", "H2" },
            new string[] { "A", "B *" }
        });
        writer.WriteBlankLine();
        writer.WriteMathBlock("x = y");
        writer.WriteBlankLine();
        writer.WriteAdmonition("note", "Title \"quoted\"", "- raw\n  - nested");
        writer.WriteBlankLine();
        writer.WriteCustomContainer("panel", "Panel", "inside **raw**");
        writer.WriteBlankLine();
        writer.WriteFootnoteDefinition("n1", "foot *text*\n> second");
        writer.WriteBlankLine();
        writer.WriteRawMarkdown("raw *markdown*");
        writer.WriteLineEnding();
    }

    public static void BuildEveryConstruct(IMarkdownBuilder builder)
    {
        builder.SetLineEnding("\r\n")
            .Text("# literal * &")
            .RawMarkdown(" raw")
            .LineEnding()
            .BlankLine()
            .Heading(2, "Heading *")
            .BlankLine()
            .Paragraph("paragraph\n> continuation")
            .BlankLine()
            .ThematicBreak()
            .BlankLine()
            .BulletList(new string[] { "bullet *" })
            .BlankLine()
            .OrderedList(new string[] { "ordered" }, 3)
            .BlankLine()
            .TaskList(new string[] { "done", "open" }, new bool[] { true, false })
            .BlankLine()
            .BlockQuote("quote\n> nested")
            .BlankLine()
            .CodeBlock("text", "a ``` b")
            .BlankLine()
            .Text("inline ")
            .Emphasis("em *")
            .Text(" ")
            .StrongEmphasis("strong *")
            .Text(" ")
            .Strikethrough("old")
            .Text(" ")
            .CodeSpan("a ` b")
            .Text(" ")
            .Link("link ]", "/a b")
            .Text(" ")
            .Image("alt ]", "/i(x)")
            .Text(" ")
            .MathSpan("x+y")
            .Text(" ")
            .FootnoteReference("n1")
            .LineEnding()
            .BlankLine()
            .Table(new string[][]
            {
                new string[] { "H|1", "H2" },
                new string[] { "A", "B *" }
            })
            .BlankLine()
            .MathBlock("x = y")
            .BlankLine()
            .Admonition("note", "Title \"quoted\"", "- raw\n  - nested")
            .BlankLine()
            .CustomContainer("panel", "Panel", "inside **raw**")
            .BlankLine()
            .FootnoteDefinition("n1", "foot *text*\n> second")
            .BlankLine()
            .RawMarkdown("raw *markdown*")
            .LineEnding();
    }
}
