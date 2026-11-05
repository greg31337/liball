using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicMarkdownRenderingTests
{
    [Test]
    public void PublicMarkdownRendering_UsesPipelineForGfmStringsAndStreams()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseGfm()
            .SetLineEnding("\r\n")
            .Build();
        string source =
            "| A | B |\n" +
            "| --- | --- |\n" +
            "| *x* | ~~y~~ |";
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        StringWriter sourceWriter = new StringWriter();
        StringWriter documentWriter = new StringWriter();

        markdown.WriteMarkdownSourceWithPipeline(source, sourceWriter, pipeline);
        markdown.WriteMarkdownDocumentWithPipeline(document, documentWriter, pipeline);
        documentWriter.Write("tail");

        string expected =
            "| A | B |\r\n" +
            "| --- | --- |\r\n" +
            "| *x* | ~~y~~ |\r\n";
        Assert.That(markdown.RenderMarkdownSourceWithPipeline(source, pipeline), Is.EqualTo(expected));
        Assert.That(markdown.RenderMarkdownDocumentWithPipeline(document, pipeline), Is.EqualTo(expected));
        Assert.That(sourceWriter.ToString(), Is.EqualTo(expected));
        Assert.That(documentWriter.ToString(), Is.EqualTo(expected + "tail"));
    }

    [Test]
    public void MarkdownWrapping_WrapsPlainTextAtConfiguredWidthAndLineEnding()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetMaximumLineWidth(12)
            .SetLineEnding("\r\n")
            .Build();

        string rendered = markdown.RenderMarkdownSourceWithPipeline(
            "alpha beta gamma delta",
            pipeline);
        string list = markdown.RenderMarkdownSourceWithPipeline(
            "- alpha beta gamma",
            pipeline);

        Assert.That(rendered, Is.EqualTo(
            "alpha beta\r\n" +
            "gamma delta\r\n"));
        Assert.That(list, Is.EqualTo(
            "- alpha beta\r\n" +
            "  gamma\r\n"));
        Assert.That(pipeline.GetOptions().MaximumLineWidth, Is.EqualTo(12));
    }

    [Test]
    public void MarkdownWrapping_DoesNotSplitStructuredInlineAtoms()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetMaximumLineWidth(20)
            .Build();

        string rendered = markdown.RenderMarkdownSourceWithPipeline(
            "alpha [beta gamma](/destination \"two words\") delta",
            pipeline);

        Assert.That(rendered, Is.EqualTo(
            "alpha\n" +
            "[beta gamma](/destination \"two words\")\n" +
            "delta\n"));
    }

    [Test]
    public void MarkdownTriviaMode_TakesPrecedenceOverNormalizedWrapping()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .TrackTrivia()
            .SetMaximumLineWidth(5)
            .Build();
        string source = "long   original paragraph without final newline";
        StringWriter writer = new StringWriter();

        markdown.WriteMarkdownSourceWithPipeline(source, writer, pipeline);

        Assert.That(writer.ToString(), Is.EqualTo(source));
    }

    [Test]
    public void MarkdownRendering_IsCultureIndependentAndEscapesTextStably()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownDocument textDocument = new MarkdownDocument();
        MarkdownNode paragraph = new MarkdownNode();
        MarkdownNode text = new MarkdownNode();
        paragraph.Kind = MarkdownNodeKind.Paragraph;
        text.Kind = MarkdownNodeKind.Text;
        text.Literal = "a * b & c";
        dataServices.CreateNodeOperations(paragraph).AppendChild(text);
        dataServices.CreateNodeOperations(textDocument.Root).AppendChild(paragraph);
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");
            string list = markdown.RenderMarkdownSource("3. three\n4. four");
            string escaped = markdown.RenderMarkdownDocument(textDocument);

            Assert.That(list, Is.EqualTo("3. three\n4. four\n"));
            Assert.That(escaped, Is.EqualTo("a \\* b \\& c\n"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void MarkdownRendering_EscapesParagraphTextThatLooksLikeBlockSyntax()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = new MarkdownNode();
        MarkdownNode text = new MarkdownNode();
        paragraph.Kind = MarkdownNodeKind.Paragraph;
        text.Kind = MarkdownNodeKind.Text;
        text.Literal = "# title\n---\n1. item\n~~~ info\n    code";
        dataServices.CreateNodeOperations(paragraph).AppendChild(text);
        dataServices.CreateNodeOperations(document.Root).AppendChild(paragraph);

        string rendered = markdown.RenderMarkdownDocument(document);
        MarkdownDocument reparsed = markdown.ParseSource(rendered);

        Assert.That(rendered, Is.EqualTo(
            "\\# title\n" +
            "\\---\n" +
            "1\\. item\n" +
            "\\~~~ info\n" +
            "&#32;   code\n"));
        Assert.That(reparsed.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(reparsed.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void PublicMarkdownRendering_ValidatesArgumentsWidthsAndOutputLimits()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        MarkdownDocument document = markdown.ParseSource("text");
        MarkdownPipeline limited = markdown.CreatePipelineBuilder()
            .SetMaximumOutputSize(4)
            .Build();
        StringWriter writer = new StringWriter();

        Assert.That(() => builder.SetMaximumLineWidth(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(builder.SetMaximumLineWidth(0).GetOptions().MaximumLineWidth, Is.EqualTo(0));
        Assert.That(() => markdown.RenderMarkdownSourceWithPipeline(null!, limited), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderMarkdownSourceWithPipeline("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderMarkdownDocumentWithPipeline(null!, limited), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderMarkdownDocumentWithPipeline(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteMarkdownSource(null!, writer), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteMarkdownSource("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteMarkdownSourceWithPipeline("", writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteMarkdownDocument(null!, writer), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteMarkdownDocument(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteMarkdownDocumentWithPipeline(document, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteMarkdownDocumentWithPipeline(document, writer, limited), Throws.TypeOf<InvalidOperationException>());
    }
}
