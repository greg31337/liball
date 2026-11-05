using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicDiagnosticRenderingTests
{
    [Test]
    public void PublicPlainTextRendering_UsesPipelineForStringsDocumentsAndStreams()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseGfm()
            .SetLineEnding("\r\n")
            .Build();
        string source = "~~gone~~\n\nnext";
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        StringWriter sourceWriter = new StringWriter();
        StringWriter documentWriter = new StringWriter();

        markdown.WritePlainTextSourceWithPipeline(source, sourceWriter, pipeline);
        markdown.WritePlainTextDocumentWithPipeline(document, documentWriter, pipeline);
        documentWriter.Write("tail");

        string expected = "gone\r\n\r\nnext\r\n";
        Assert.That(markdown.RenderPlainTextSourceWithPipeline(source, pipeline), Is.EqualTo(expected));
        Assert.That(markdown.RenderPlainTextDocumentWithPipeline(document, pipeline), Is.EqualTo(expected));
        Assert.That(sourceWriter.ToString(), Is.EqualTo(expected));
        Assert.That(documentWriter.ToString(), Is.EqualTo(expected + "tail"));
    }

    [Test]
    public void PublicTreeRendering_UsesPipelineForStringsDocumentsAndStreams()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseGfm()
            .SetLineEnding("\r\n")
            .Build();
        string source = "~~gone~~";
        MarkdownDocument document = markdown.ParseSourceWithPipeline(source, pipeline);
        StringWriter debugWriter = new StringWriter();
        StringWriter xmlWriter = new StringWriter();

        string debugSource = markdown.RenderDebugTreeSourceWithPipeline(source, pipeline);
        string debugDocument = markdown.RenderDebugTreeDocumentWithPipeline(document, pipeline);
        string xmlSource = markdown.RenderXmlAstSourceWithPipeline(source, pipeline);
        string xmlDocument = markdown.RenderXmlAstDocumentWithPipeline(document, pipeline);
        markdown.WriteDebugTreeDocumentWithPipeline(document, debugWriter, pipeline);
        markdown.WriteXmlAstSourceWithPipeline(source, xmlWriter, pipeline);
        debugWriter.Write("tail");
        xmlWriter.Write("tail");

        Assert.That(debugSource, Is.EqualTo(debugDocument));
        Assert.That(debugWriter.ToString(), Is.EqualTo(debugDocument + "tail"));
        Assert.That(debugSource, Does.Contain("Strikethrough"));
        Assert.That(debugSource, Does.Contain("\r\n"));
        Assert.That(xmlSource, Is.EqualTo(xmlDocument));
        Assert.That(xmlWriter.ToString(), Is.EqualTo(xmlSource + "tail"));
        Assert.That(xmlSource, Does.Contain("<Strikethrough"));
        Assert.That(xmlSource, Does.Contain("\r\n"));
    }

    [Test]
    public void TreeRendering_RepresentsSpansAndAttributesConsistentlyAcrossCultures()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices dataServices = new Markdown();
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode text = new MarkdownNode();
        MarkdownAttribute first = new MarkdownAttribute();
        MarkdownAttribute second = new MarkdownAttribute();
        text.Kind = MarkdownNodeKind.Text;
        text.Literal = "value";
        text.Span.Start.Offset = 12;
        text.Span.Start.Line = 2;
        text.Span.Start.Column = 4;
        text.Span.End.Offset = 17;
        text.Span.End.Line = 2;
        text.Span.End.Column = 9;
        first.Name = "alpha";
        first.Value = "one";
        second.Name = "beta";
        second.Value = "two & more";
        text.Attributes.Add(first);
        text.Attributes.Add(second);
        dataServices.CreateNodeOperations(document.Root).AppendChild(text);
        CultureInfo originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fa-IR");
            string debug = markdown.RenderDebugTreeDocument(document);
            string xml = markdown.RenderXmlAstDocument(document);

            Assert.That(debug, Does.Contain("span=2:4-2:9 length=5"));
            Assert.That(debug, Does.Contain("attrs=[alpha=\"one\", beta=\"two & more\"]"));
            Assert.That(xml, Does.Contain("span=\"2:4-2:9\" length=\"5\""));
            Assert.That(xml, Does.Contain("attr-alpha=\"one\" attr-beta=\"two &amp; more\""));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public void PublicDiagnosticRendering_EnforcesOutputLimitsForStringsAndStreams()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetMaximumOutputSize(3)
            .Build();
        MarkdownDocument document = markdown.ParseSource("text");

        Assert.That(
            () => markdown.RenderPlainTextDocumentWithPipeline(document, pipeline),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.RenderDebugTreeDocumentWithPipeline(document, pipeline),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.RenderXmlAstDocumentWithPipeline(document, pipeline),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.WritePlainTextSourceWithPipeline("text", new StringWriter(), pipeline),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.WriteDebugTreeSourceWithPipeline("text", new StringWriter(), pipeline),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(
            () => markdown.WriteXmlAstSourceWithPipeline("text", new StringWriter(), pipeline),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void PublicDiagnosticRendering_ValidatesArguments()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreateCommonMarkPipeline();
        MarkdownDocument document = markdown.ParseSource("text");
        StringWriter writer = new StringWriter();

        Assert.That(() => markdown.RenderPlainTextSourceWithPipeline(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderPlainTextDocumentWithPipeline(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WritePlainTextSource("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WritePlainTextDocumentWithPipeline(document, writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderDebugTreeSourceWithPipeline("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderDebugTreeDocument(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteDebugTreeSource(null!, writer), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteDebugTreeDocumentWithPipeline(document, null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderXmlAstSource(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.RenderXmlAstDocumentWithPipeline(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteXmlAstSourceWithPipeline("", writer, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.WriteXmlAstDocument(null!, writer), Throws.TypeOf<ArgumentNullException>());
    }
}
