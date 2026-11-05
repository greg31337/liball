using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownWriterGenerationTests
{
    [Test]
    public void MarkdownWriter_GeneratesBlockMarkdown()
    {
        StringWriter output = new StringWriter();
        MarkdownWriter writer = new MarkdownWriter(output);

        writer.Heading(2, "A *Title*");
        writer.BlankLine();
        writer.Paragraph("Use [brackets] & stars *carefully*.");
        writer.BlankLine();
        writer.CodeBlock("csharp", "Console.WriteLine(\"hi\");");

        Assert.That(output.ToString(), Is.EqualTo(
            "## A \\*Title\\*\n" +
            "\n" +
            "Use \\[brackets\\] \\& stars \\*carefully\\*.\n" +
            "\n" +
            "```csharp\n" +
            "Console.WriteLine(\"hi\");\n" +
            "```\n"));
    }

    [Test]
    public void MarkdownWriter_GeneratesInlineLinksAndImages()
    {
        StringWriter output = new StringWriter();
        MarkdownWriter writer = new MarkdownWriter(output);

        writer.Write("See ");
        writer.Link("a [doc]", "/docs file");
        writer.Write(" ");
        writer.Image("! logo", "/logo.png");
        writer.WriteLineEnding();

        Assert.That(output.ToString(), Is.EqualTo("See [a \\[doc\\]](</docs file>) ![\\! logo](/logo.png)\n"));
    }

    [Test]
    public void MarkdownWriter_GeneratesTables()
    {
        StringWriter output = new StringWriter();
        MarkdownWriter writer = new MarkdownWriter(output);

        writer.Table(new string[][]
        {
            new string[] { "Name", "Value" },
            new string[] { "A|B", "*strong*" }
        });

        Assert.That(output.ToString(), Is.EqualTo(
            "| Name | Value |\n" +
            "| --- | --- |\n" +
            "| A\\|B | \\*strong\\* |\n"));
    }

    [Test]
    public void MarkdownWriter_UsesConfiguredLineEnding()
    {
        StringWriter output = new StringWriter();
        MarkdownWriter writer = MarkdownWriter.Create(output, "\r\n");

        writer.Heading(1, "Title");
        writer.SetLineEnding("\n");
        writer.Paragraph("body");

        Assert.That(output.ToString(), Is.EqualTo("# Title\r\nbody\n"));
    }

    [Test]
    public void MarkdownWriter_ChoosesCodeFenceLongerThanContentRuns()
    {
        Assert.That(MarkdownWriter.GetCodeFence("code"), Is.EqualTo("```"));
        Assert.That(MarkdownWriter.GetCodeFence("contains ``` fence"), Is.EqualTo("````"));
    }

    [Test]
    public void MarkdownWriterGenerationRejectsInvalidArguments()
    {
        StringWriter output = new StringWriter();
        MarkdownWriter writer = new MarkdownWriter(output);

        Assert.That(() => MarkdownWriter.Create(null!, "\n"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownWriter.Create(output, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownWriter.Create(output, "\n\n"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.SetLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.SetLineEnding("\n\n"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.Heading(0, "x"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => writer.Heading(7, "x"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => writer.Heading(1, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.Paragraph(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.CodeBlock(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.CodeBlock("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.CodeBlock("c`sharp", ""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.Link(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.Link("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.Image(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.Image("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.Table(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.Table(new string[0][]), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.Table(new string[][] { null! }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.Table(new string[][] { new string[0] }), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.Table(new string[][] { new string[] { "a" }, new string[] { "a", "b" } }), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.Table(new string[][] { new string[] { null! } }), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownWriter.GetCodeFence(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.WriteDestination(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.ValidateTableRows(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.WriteTableRow(null!, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.WriteTableRow(new string[] { "a" }, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => writer.WriteTableRow(new string[] { "a" }, 2), Throws.TypeOf<ArgumentException>());
        Assert.That(() => writer.WriteTableRow(new string[] { null! }, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => writer.WriteTableDelimiterRow(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownWriter.EscapeTableCell(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
