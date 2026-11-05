using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicMarkdownGenerationTests
{
    [Test]
    public void PublicMarkdownWriter_GeneratesCoreBlocksAndInlinesStably()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetLineEnding("\r\n")
            .Build();
        StringWriter output = new StringWriter();
        IMarkdownWriter writer = markdown.CreateMarkdownWriterWithPipeline(output, pipeline);

        writer.WriteHeading(2, "A *title*");
        writer.WriteBlankLine();
        writer.WriteParagraph("# literal\nsecond");
        writer.WriteBlankLine();
        writer.WriteThematicBreak();
        writer.WriteBlankLine();
        writer.WriteBulletList(new string[] { "one", "two *" });
        writer.WriteBlankLine();
        writer.WriteOrderedList(new string[] { "three", "four" }, 3);
        writer.WriteBlankLine();
        writer.WriteBlockQuote("quoted\nline");
        writer.WriteBlankLine();
        writer.WriteCodeBlock("text", "a ``` b");
        writer.WriteText("See ");
        writer.WriteEmphasis("some");
        writer.WriteText(" and ");
        writer.WriteStrongEmphasis("more");
        writer.WriteText(" ");
        writer.WriteCodeSpan("a ` b");
        writer.WriteText(" ");
        writer.WriteLink("docs", "/docs file");
        writer.WriteText(" ");
        writer.WriteImage("logo *", "/logo.png");
        writer.WriteLineEnding();
        writer.WriteTable(new string[][]
        {
            new string[] { "Name", "Value" },
            new string[] { "A|B", "C" }
        });
        writer.WriteRawMarkdown("raw *markdown*");
        writer.WriteLineEnding();
        output.Write("tail");

        Assert.That(output.ToString(), Is.EqualTo(
            "## A \\*title\\*\r\n" +
            "\r\n" +
            "\\# literal\r\n" +
            "second\r\n" +
            "\r\n" +
            "---\r\n" +
            "\r\n" +
            "- one\r\n" +
            "- two \\*\r\n" +
            "\r\n" +
            "3. three\r\n" +
            "4. four\r\n" +
            "\r\n" +
            "> quoted\r\n" +
            "> line\r\n" +
            "\r\n" +
            "````text\r\n" +
            "a ``` b\r\n" +
            "````\r\n" +
            "See *some* and **more** ``a ` b`` [docs](</docs file>) ![logo \\*](/logo.png)\r\n" +
            "| Name | Value |\r\n" +
            "| --- | --- |\r\n" +
            "| A\\|B | C |\r\n" +
            "raw *markdown*\r\n" +
            "tail"));
        Assert.That(writer.EscapeText("# a * b"), Is.EqualTo("\\# a \\* b"));
    }

    [Test]
    public void PublicMarkdownBuilder_GeneratesExtensionsAndBuildsDocument()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseAdvancedExtensions()
            .RegisterCustomContainer("panel")
            .Build();
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilderWithPipeline(pipeline);

        builder.TaskList(
                new string[] { "done", "open" },
                new bool[] { true, false })
            .BlankLine()
            .Text("Use ")
            .Strikethrough("old")
            .Text(" and ")
            .MathSpan("x+y")
            .LineEnding()
            .BlankLine()
            .MathBlock("x = y")
            .BlankLine()
            .Admonition("note", "Heads \"up\"", "body *raw*")
            .BlankLine()
            .CustomContainer("panel", "Panel", "inside **raw**")
            .BlankLine()
            .FootnoteDefinition("n1", "note *text*")
            .Text("ref ")
            .FootnoteReference("n1")
            .LineEnding();

        string generated = builder.ToMarkdown();
        MarkdownDocument document = builder.ToDocumentWithPipeline(pipeline);
        IMarkdownTreeOperations trees = ((IMarkdownDataServices)markdown).CreateTreeOperations();

        Assert.That(generated, Does.Contain("- [x] done\n- [ ] open\n"));
        Assert.That(generated, Does.Contain("Use ~~old~~ and $x+y$\n"));
        Assert.That(generated, Does.Contain("!!! note \"Heads \\\"up\\\"\"\n    body *raw*\n"));
        Assert.That(generated, Does.Contain("::: panel \"Panel\"\ninside **raw**\n:::\n"));
        Assert.That(generated, Does.Contain("[^n1]: note \\*text\\*\nref [^n1]\n"));
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.TaskListItem), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.Strikethrough), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.MathBlock), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.AdmonitionBlock), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.CustomContainerBlock), Is.Not.Null);
        Assert.That(trees.FindFirstInDocument(document, MarkdownNodeKind.FootnoteDefinition), Is.Not.Null);
    }

    [Test]
    public void GenerationDocumentConversion_ClonesEventsAndNodes()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownDocument original = markdown.ParseSource("# Title\n\nbody");
        MarkdownEvent[] events = dataServices.ReadDocumentEvents(original);
        MarkdownDocument fromEvents = markdown.CreateDocumentFromEvents(events);
        MarkdownNode paragraph = new MarkdownNode();
        paragraph.Kind = MarkdownNodeKind.Paragraph;
        paragraph.Literal = "generated";

        MarkdownDocument fromNodes = markdown.CreateDocumentFromNodes(
            new MarkdownNode[] { paragraph });
        paragraph.Literal = "changed";

        Assert.That(markdown.RenderMarkdownDocument(fromEvents), Is.EqualTo(
            markdown.RenderMarkdownDocument(original)));
        Assert.That(markdown.RenderMarkdownDocument(fromNodes), Is.EqualTo("generated\n"));
        Assert.That(fromNodes.Root.GetChild(0), Is.Not.SameAs(paragraph));
    }

    [Test]
    public void GenerationFormatting_IsCultureIndependentAndHonorsLimitsAfterClear()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetMaximumOutputSize(12)
            .Build();
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilderWithPipeline(pipeline);
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");
            builder.OrderedList(new string[] { "a", "b" }, 3);
            Assert.That(builder.ToMarkdown(), Is.EqualTo("3. a\n4. b\n"));
            builder.Clear().Paragraph("abc");
            Assert.That(builder.ToMarkdown(), Is.EqualTo("abc\n"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void PublicGeneration_ValidatesArgumentsAndOutputLimits()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline limited = markdown.CreatePipelineBuilder()
            .SetMaximumOutputSize(3)
            .Build();
        IMarkdownWriter writer = markdown.CreateMarkdownWriter(new StringWriter());
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilder();

        Assert.That(() => new MarkdownGenerationWriter(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateMarkdownWriterWithPipeline(new StringWriter(), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateMarkdownBuilderWithPipeline(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.WriteBulletList(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.WriteOrderedList(new string[] { "a" }, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => writer.WriteTaskList(new string[] { "a" }, new bool[0]), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.WriteMathSpan("a$b"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.WriteAdmonition("bad name", "", ""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.WriteCustomContainer("", "", ""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.WriteFootnoteReference(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.ToDocumentWithPipeline(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateDocumentFromEvents(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateDocumentFromNodes(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.CreateDocumentFromNodes(new MarkdownNode[] { null! }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => markdown.CreateMarkdownWriterWithPipeline(new StringWriter(), limited).WriteParagraph("text"),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.CreateMarkdownBuilderWithPipeline(limited).Paragraph("text"),
            Throws.TypeOf<InvalidOperationException>());
    }
}
