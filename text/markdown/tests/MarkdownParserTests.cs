using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownParserTests
{
    [Test]
    public void Parse_EmptySourceReturnsEmptyDocumentRoot()
    {
        MarkdownDocument document = MarkdownParser.Parse("");

        Assert.That(document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(document.Root.Span.IsEmpty(), Is.True);
        Assert.That(document.Root.GetChildCount(), Is.EqualTo(0));
        Assert.That(document.HasDiagnostics(), Is.False);
    }

    [Test]
    public void Parse_ComputesWholeDocumentSpan()
    {
        MarkdownDocument document = MarkdownParser.Parse("alpha\nbeta");

        Assert.That(document.Root.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(document.Root.Span.Start.Line, Is.EqualTo(1));
        Assert.That(document.Root.Span.Start.Column, Is.EqualTo(1));
        Assert.That(document.Root.Span.End.Offset, Is.EqualTo(10));
        Assert.That(document.Root.Span.End.Line, Is.EqualTo(2));
        Assert.That(document.Root.Span.End.Column, Is.EqualTo(5));
    }

    [Test]
    public void Parse_UsesNormalizedInputForSpanAndLimitChecks()
    {
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .SetMaximumInputSize(5)
            .Build();

        MarkdownParseContext context = MarkdownParser.CreateContext("a\0\nbc", pipeline);

        Assert.That(context.Source[1], Is.EqualTo(MarkdownInputNormalizer.NullReplacementCharacter));
        Assert.That(context.Document.Root.Span.End.Offset, Is.EqualTo(5));
        Assert.That(context.Document.Root.Span.End.Line, Is.EqualTo(2));
        Assert.That(context.Document.Root.Span.End.Column, Is.EqualTo(3));
    }

    [Test]
    public void Parse_AddsDiagnosticWhenInputExceedsPipelineLimit()
    {
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .SetMaximumInputSize(3)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("abcd", pipeline);

        Assert.That(document.HasErrors(), Is.True);
        Assert.That(document.GetDiagnostics()[0].Code, Is.EqualTo(MarkdownParser.InputSizeDiagnosticCode));
        Assert.That(document.GetDiagnostics()[0].Span.GetLength(), Is.EqualTo(4));
    }

    [Test]
    public void ParseReader_ReadsTextReaderInput()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.CommonMarkPreset();
        MarkdownDocument document = MarkdownParser.ParseReader(new StringReader("reader"), pipeline);

        Assert.That(document.Root.Span.End.Offset, Is.EqualTo(6));
        Assert.That(document.Root.Span.End.Line, Is.EqualTo(1));
        Assert.That(document.Root.Span.End.Column, Is.EqualTo(7));
    }

    [Test]
    public void ParseContext_CopiesPipeline()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder()
            .AllowUnsafeLinks();

        MarkdownParseContext context = MarkdownParser.CreateContext("text", builder.Build());
        builder.DisableHtml();

        Assert.That(context.Pipeline.AllowsUnsafeLinks(), Is.True);
    }

    [Test]
    public void Parser_RejectsNullArguments()
    {
        Assert.That(() => MarkdownParser.Parse(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParser.Parse("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParser.ParseReader(null!, MarkdownPipeline.CommonMarkPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParser.ParseReader(new StringReader(""), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParser.CreateContext(null!, MarkdownPipeline.CommonMarkPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParser.CreateContext("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParser.Run(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParser.AddLimitDiagnostics(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownParser.GetDocumentSpan(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
