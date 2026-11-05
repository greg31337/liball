using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownBuilderTests
{
    [Test]
    public void Builder_ChainsMarkdownGeneration()
    {
        MarkdownBuilder builder = new MarkdownBuilder();

        MarkdownBuilder returned = builder
            .Heading(1, "Title")
            .BlankLine()
            .Paragraph("Hello *world*")
            .BlankLine()
            .Write("See ")
            .Link("docs", "/docs")
            .Write(" and ")
            .Image("logo", "/logo.png")
            .LineEnding();

        Assert.That(returned, Is.SameAs(builder));
        Assert.That(builder.ToMarkdown(), Is.EqualTo(
            "# Title\n" +
            "\n" +
            "Hello \\*world\\*\n" +
            "\n" +
            "See [docs](/docs) and ![logo](/logo.png)\n"));
    }

    [Test]
    public void Builder_CanGenerateDocument()
    {
        MarkdownBuilder builder = new MarkdownBuilder()
            .Heading(2, "Title")
            .BlankLine()
            .Paragraph("body");

        MarkdownDocument document = builder.ToDocument();

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(Markdown.ToPlainText(document), Is.EqualTo("Title\n\nbody\n"));
    }

    [Test]
    public void Builder_ToDocumentHonorsPipeline()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.AdvancedPreset();
        MarkdownBuilder builder = new MarkdownBuilder()
            .Heading(1, "Title");

        MarkdownDocument document = builder.ToDocumentWithPipeline(pipeline);

        Assert.That(document.Root.GetChild(0).GetAttributeValue("id"), Is.EqualTo("title"));
    }

    [Test]
    public void Builder_CanWriteRawTablesAndCodeBlocks()
    {
        MarkdownBuilder builder = new MarkdownBuilder()
            .CodeBlock("text", "a\nb")
            .BlankLine()
            .Table(new string[][]
            {
                new string[] { "Name", "Value" },
                new string[] { "A", "B" }
            })
            .WriteRaw("raw *markdown*");

        Assert.That(builder.ToMarkdown(), Is.EqualTo(
            "```text\n" +
            "a\n" +
            "b\n" +
            "```\n" +
            "\n" +
            "| Name | Value |\n" +
            "| --- | --- |\n" +
            "| A | B |\n" +
            "raw *markdown*"));
    }

    [Test]
    public void Builder_CanClearAndReuse()
    {
        MarkdownBuilder builder = new MarkdownBuilder()
            .Paragraph("first")
            .Clear()
            .Paragraph("second");

        Assert.That(builder.ToMarkdown(), Is.EqualTo("second\n"));
    }

    [Test]
    public void Builder_UsesConfiguredLineEnding()
    {
        MarkdownBuilder builder = Markdown.CreateBuilder("\r\n")
            .Heading(1, "Title")
            .SetLineEnding("\n")
            .Paragraph("body");

        Assert.That(builder.ToMarkdown(), Is.EqualTo("# Title\r\nbody\n"));
    }

    [Test]
    public void MarkdownFacade_CreatesPipelineAndBuilder()
    {
        MarkdownPipelineBuilder pipelineBuilder = Markdown.CreatePipeline();
        MarkdownBuilder builder = Markdown.CreateBuilder();

        Assert.That(pipelineBuilder.GetOptions().Features.CommonMark, Is.True);
        Assert.That(builder.Paragraph("text").ToMarkdown(), Is.EqualTo("text\n"));
    }

    [Test]
    public void BuilderRejectsInvalidArguments()
    {
        MarkdownBuilder builder = new MarkdownBuilder();

        Assert.That(() => MarkdownBuilder.Create(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownBuilder.Create("\n\n"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => Markdown.CreateBuilder(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetLineEnding(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.Write(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.WriteRaw(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.Heading(0, "x"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => builder.Heading(1, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.Paragraph(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.CodeBlock(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.Link(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.Image(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.Table(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.ToDocumentWithPipeline(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
