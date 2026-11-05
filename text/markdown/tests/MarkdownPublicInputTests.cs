using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownPublicInputTests
{
    [Test]
    public void ReaderAndMemory_DefaultEntryPointsMatchStringParsing()
    {
        IMarkdown markdown = new Markdown();
        string source = "# Heading\r\n\rParagraph\twith\0null\nfinal";

        MarkdownDocument fromString = markdown.ParseSource(source);
        MarkdownDocument fromReader = markdown.ParseReader(new StringReader(source));
        MarkdownDocument fromMemory = markdown.ParseMemory(source.AsMemory());

        AssertDocumentsEquivalent(fromString, fromReader);
        AssertDocumentsEquivalent(fromString, fromMemory);
        MarkdownNode memoryParagraph = fromMemory.Root.Children[1];
        Assert.That(memoryParagraph.Literal, Does.Contain("\ufffd"));
        Assert.That(fromMemory.Root.Span.End.Offset, Is.EqualTo(source.Length));
        Assert.That(fromMemory.Root.Span.End.Line, Is.EqualTo(4));
        Assert.That(fromMemory.Root.Span.End.Column, Is.EqualTo(6));
    }

    [Test]
    public void Reader_ConsumesCurrentPositionWithoutTakingOwnership()
    {
        IMarkdown markdown = new Markdown();
        StringReader reader = new StringReader("ignored\n# Remaining");
        Assert.That(reader.ReadLine(), Is.EqualTo("ignored"));

        MarkdownDocument document = markdown.ParseReader(reader);

        Assert.That(document.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(document.Root.Children[0].Literal, Is.EqualTo("Remaining"));
        Assert.That(document.Root.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(document.Root.Span.End.Offset, Is.EqualTo(11));
        Assert.That(() => reader.Read(), Throws.Nothing);
        Assert.That(reader.Read(), Is.EqualTo(-1));
    }

    [Test]
    public void Memory_ParsesOnlyTheRequestedSlice()
    {
        IMarkdown markdown = new Markdown();
        string backing = "\0ignore# Slice\0ignored";
        ReadOnlyMemory<char> source = backing.AsMemory(7, 7);

        MarkdownDocument document = markdown.ParseMemory(source);

        Assert.That(document.Root.Children.Count, Is.EqualTo(1));
        Assert.That(document.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(document.Root.Children[0].Literal, Is.EqualTo("Slice"));
        Assert.That(document.Root.Span.End.Offset, Is.EqualTo(7));
        Assert.That(document.Root.Children[0].Literal, Does.Not.Contain("\ufffd"));

        MarkdownDocument empty = markdown.ParseMemory(default(ReadOnlyMemory<char>));
        Assert.That(empty.Root.Children, Is.Empty);
        Assert.That(empty.Root.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(empty.Root.Span.End.Offset, Is.EqualTo(0));
    }

    [Test]
    public void ExplicitPipeline_IsAppliedEquallyToAllInputForms()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreateGfmPipeline();
        string source = "| Left | Right |\n| :--- | ---: |\n| one | two |";

        MarkdownDocument fromString = markdown.ParseSourceWithPipeline(source, pipeline);
        MarkdownDocument fromReader = markdown.ParseReaderWithPipeline(new StringReader(source), pipeline);
        MarkdownDocument fromMemory = markdown.ParseMemoryWithPipeline(source.AsMemory(), pipeline);

        AssertDocumentsEquivalent(fromString, fromReader);
        AssertDocumentsEquivalent(fromString, fromMemory);
        Assert.That(fromReader.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.Table));
        Assert.That(
            markdown.RenderHtmlDocumentWithPipeline(fromMemory, pipeline),
            Is.EqualTo(markdown.RenderHtmlDocumentWithPipeline(fromString, pipeline)));
    }

    [Test]
    public void LimitsAndDiagnostics_AreEquivalentForAllInputForms()
    {
        IMarkdown markdown = new Markdown();
        MarkdownOptions options = new MarkdownOptions();
        options.MaximumInputSize = 3;
        MarkdownPipeline pipeline = markdown.CreatePipelineFromOptions(options);
        string source = "abcd";

        MarkdownDocument fromString = markdown.ParseSourceWithPipeline(source, pipeline);
        MarkdownDocument fromReader = markdown.ParseReaderWithPipeline(new StringReader(source), pipeline);
        MarkdownDocument fromMemory = markdown.ParseMemoryWithPipeline(source.AsMemory(), pipeline);

        AssertDocumentsEquivalent(fromString, fromReader);
        AssertDocumentsEquivalent(fromString, fromMemory);
        Assert.That(fromReader.Diagnostics.Items.Count, Is.EqualTo(1));
        Assert.That(fromReader.Diagnostics.Items[0].Code, Is.EqualTo("MD_LIMIT_INPUT_SIZE"));
        Assert.That(fromMemory.Diagnostics.Items[0].Span.End.Offset, Is.EqualTo(4));
    }

    [Test]
    public void RegisteredTransformsAndTrivia_RunForReaderAndMemoryInputs()
    {
        IMarkdown markdown = new Markdown();
        int transformCalls = 0;
        IMarkdownTransform transform = new TestTransform(
            "test.inputs",
            (document, context) =>
            {
                transformCalls = transformCalls + 1;
                new MarkdownNodeOperations(document.Root).SetAttribute("transformed", "true");
            });
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        MarkdownPipeline transformPipeline = builder.AddTransform(transform).Build();

        MarkdownDocument readerDocument = markdown.ParseReaderWithPipeline(
            new StringReader("text"),
            transformPipeline);
        MarkdownDocument memoryDocument = markdown.ParseMemoryWithPipeline(
            "text".AsMemory(),
            transformPipeline);

        Assert.That(transformCalls, Is.EqualTo(2));
        Assert.That(new MarkdownNodeOperations(readerDocument.Root).GetAttributeValue("transformed"), Is.EqualTo("true"));
        Assert.That(new MarkdownNodeOperations(memoryDocument.Root).GetAttributeValue("transformed"), Is.EqualTo("true"));

        MarkdownOptions triviaOptions = new MarkdownOptions();
        triviaOptions.TrackTrivia = true;
        MarkdownPipeline triviaPipeline = markdown.CreatePipelineFromOptions(triviaOptions);
        string triviaSource = "# Heading\r\n";
        MarkdownDocument triviaReader = markdown.ParseReaderWithPipeline(
            new StringReader(triviaSource),
            triviaPipeline);
        MarkdownDocument triviaMemory = markdown.ParseMemoryWithPipeline(
            triviaSource.AsMemory(),
            triviaPipeline);

        Assert.That(triviaReader.Root.Trivia!.Text, Is.EqualTo(triviaSource));
        Assert.That(triviaMemory.Root.Trivia!.Text, Is.EqualTo(triviaSource));
        AssertDocumentsEquivalent(triviaReader, triviaMemory);
    }

    [Test]
    public void ReaderAndMemoryEntryPoints_RejectInvalidInputsAndPropagateReaderFailures()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreateCommonMarkPipeline();
        StringReader disposed = new StringReader("text");
        disposed.Dispose();

        Assert.That(() => markdown.ParseReader(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.ParseReaderWithPipeline(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => markdown.ParseReaderWithPipeline(new StringReader(""), null!),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(
            () => markdown.ParseMemoryWithPipeline(ReadOnlyMemory<char>.Empty, null!),
            Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.ParseReader(disposed), Throws.TypeOf<ObjectDisposedException>());
    }

    [Test]
    public void AllLineEndingsAndFinalUnterminatedLine_HaveStablePublicPositions()
    {
        IMarkdown markdown = new Markdown();
        string[] lineEndings = new string[] { "\n", "\r", "\r\n" };
        int index = 0;
        while (index < lineEndings.Length)
        {
            string lineEnding = lineEndings[index];
            string source = "# one" + lineEnding + lineEnding + "last";
            MarkdownDocument fromString = markdown.ParseSource(source);
            MarkdownDocument fromReader = markdown.ParseReader(new StringReader(source));
            MarkdownDocument fromMemory = markdown.ParseMemory(source.AsMemory());

            AssertDocumentsEquivalent(fromString, fromReader);
            AssertDocumentsEquivalent(fromString, fromMemory);
            Assert.That(markdown.RenderHtmlDocument(fromString), Is.EqualTo(
                "<h1>one</h1>\n<p>last</p>\n"));
            Assert.That(fromString.Root.Span.End.Offset, Is.EqualTo(source.Length));
            Assert.That(fromString.Root.Span.End.Line, Is.EqualTo(3));
            Assert.That(fromString.Root.Span.End.Column, Is.EqualTo(5));
            Assert.That(fromString.Root.Children[0].Span.End.Offset, Is.EqualTo(5));
            Assert.That(fromString.Root.Children[0].Span.End.Line, Is.EqualTo(1));
            Assert.That(fromString.Root.Children[0].Span.End.Column, Is.EqualTo(6));
            Assert.That(
                fromString.Root.Children[1].Span.Start.Offset,
                Is.EqualTo(5 + lineEnding.Length * 2));
            Assert.That(fromString.Root.Children[1].Span.Start.Line, Is.EqualTo(3));
            Assert.That(fromString.Root.Children[1].Span.Start.Column, Is.EqualTo(1));
            index = index + 1;
        }
    }

    [Test]
    public void TabsUseFourColumnIndentationAndRemainLiteralInsideText()
    {
        IMarkdown markdown = new Markdown();
        string source = "\talpha\n  \tbeta\n\ntext\tvalue";

        MarkdownDocument fromString = markdown.ParseSource(source);
        MarkdownDocument fromReader = markdown.ParseReader(new StringReader(source));
        MarkdownDocument fromMemory = markdown.ParseMemory(source.AsMemory());

        AssertDocumentsEquivalent(fromString, fromReader);
        AssertDocumentsEquivalent(fromString, fromMemory);
        Assert.That(markdown.RenderHtmlDocument(fromString), Is.EqualTo(
            "<pre><code>alpha\nbeta\n</code></pre>\n<p>text\tvalue</p>\n"));
        Assert.That(fromString.Root.Children[0].Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(fromString.Root.Children[1].Literal, Is.EqualTo("text\tvalue"));
        Assert.That(fromString.Root.Span.End.Line, Is.EqualTo(4));
        Assert.That(fromString.Root.Span.End.Column, Is.EqualTo(11));
    }

    [Test]
    public void NullCharactersAreReplacedWithoutChangingSourceOffsets()
    {
        IMarkdown markdown = new Markdown();
        string source = "a\0b";

        MarkdownDocument fromString = markdown.ParseSource(source);
        MarkdownDocument fromReader = markdown.ParseReader(new StringReader(source));
        MarkdownDocument fromMemory = markdown.ParseMemory(source.AsMemory());

        AssertDocumentsEquivalent(fromString, fromReader);
        AssertDocumentsEquivalent(fromString, fromMemory);
        Assert.That(fromString.Root.Children[0].Literal, Is.EqualTo("a\ufffdb"));
        Assert.That(markdown.RenderHtmlDocument(fromString), Is.EqualTo("<p>a\ufffdb</p>\n"));
        Assert.That(fromString.Root.Span.End.Offset, Is.EqualTo(3));
        Assert.That(fromString.Root.Span.End.Column, Is.EqualTo(4));
    }

    [Test]
    public void UnicodePunctuationAndCjkLinesProduceEmphasisAndSoftBreaks()
    {
        IMarkdown markdown = new Markdown();
        string source = "«*em*»\n漢字\n仮名";

        MarkdownDocument fromString = markdown.ParseSource(source);
        MarkdownDocument fromReader = markdown.ParseReader(new StringReader(source));
        MarkdownDocument fromMemory = markdown.ParseMemory(source.AsMemory());

        AssertDocumentsEquivalent(fromString, fromReader);
        AssertDocumentsEquivalent(fromString, fromMemory);
        Assert.That(CountNodeKind(fromString.Root, MarkdownNodeKind.Emphasis), Is.EqualTo(1));
        Assert.That(CountNodeKind(fromString.Root, MarkdownNodeKind.SoftLineBreak), Is.EqualTo(2));
        Assert.That(markdown.RenderHtmlDocument(fromString), Is.EqualTo(
            "<p>«<em>em</em>»\n漢字\n仮名</p>\n"));
        Assert.That(fromString.Root.Span.End.Line, Is.EqualTo(3));
        Assert.That(fromString.Root.Span.End.Column, Is.EqualTo(3));
    }

    [Test]
    public void NamedNumericAndInvalidEntitiesAreHandledAcrossInputForms()
    {
        IMarkdown markdown = new Markdown();
        string source = "&amp; &#35; &#x1F600; &#0; &bogus;";

        MarkdownDocument fromString = markdown.ParseSource(source);
        MarkdownDocument fromReader = markdown.ParseReader(new StringReader(source));
        MarkdownDocument fromMemory = markdown.ParseMemory(source.AsMemory());

        AssertDocumentsEquivalent(fromString, fromReader);
        AssertDocumentsEquivalent(fromString, fromMemory);
        Assert.That(markdown.RenderHtmlDocument(fromString), Is.EqualTo(
            "<p>&amp; # 😀 \ufffd &amp;bogus;</p>\n"));
        Assert.That(fromString.Root.Span.End.Offset, Is.EqualTo(source.Length));
        Assert.That(fromString.Root.Span.End.Column, Is.EqualTo(source.Length + 1));
    }

    [Test]
    public void MaximumInputSizeCountsUtf16SourceUnitsAtAndAboveTheLimit()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        MarkdownPipeline pipeline = builder.SetMaximumInputSize(3).Build();
        string atLimit = "😀x";
        string overLimit = "😀xy";

        MarkdownDocument exactString = markdown.ParseSourceWithPipeline(atLimit, pipeline);
        MarkdownDocument exactReader = markdown.ParseReaderWithPipeline(new StringReader(atLimit), pipeline);
        MarkdownDocument exactMemory = markdown.ParseMemoryWithPipeline(atLimit.AsMemory(), pipeline);
        MarkdownDocument overString = markdown.ParseSourceWithPipeline(overLimit, pipeline);
        MarkdownDocument overReader = markdown.ParseReaderWithPipeline(new StringReader(overLimit), pipeline);
        MarkdownDocument overMemory = markdown.ParseMemoryWithPipeline(overLimit.AsMemory(), pipeline);

        AssertDocumentsEquivalent(exactString, exactReader);
        AssertDocumentsEquivalent(exactString, exactMemory);
        Assert.That(exactString.Diagnostics.Items, Is.Empty);
        AssertDocumentsEquivalent(overString, overReader);
        AssertDocumentsEquivalent(overString, overMemory);
        Assert.That(overString.Diagnostics.Items.Count, Is.EqualTo(1));
        Assert.That(overString.Diagnostics.Items[0].Code, Is.EqualTo("MD_LIMIT_INPUT_SIZE"));
        Assert.That(overString.Diagnostics.Items[0].Span.End.Offset, Is.EqualTo(4));
    }

    [Test]
    public void ReaderFailuresPropagateWithoutDisposingCallerOwnedReader()
    {
        IMarkdown markdown = new Markdown();
        ThrowingMarkdownInputReader reader = new ThrowingMarkdownInputReader();

        Assert.That(() => markdown.ParseReader(reader), Throws.TypeOf<IOException>());
        Assert.That(reader.Disposed, Is.False);
    }

    private static void AssertDocumentsEquivalent(MarkdownDocument expected, MarkdownDocument actual)
    {
        AssertNodesEquivalent(expected.Root, actual.Root);
        Assert.That(actual.Diagnostics.Items.Count, Is.EqualTo(expected.Diagnostics.Items.Count));
        int index = 0;
        while (index < expected.Diagnostics.Items.Count)
        {
            MarkdownDiagnostic expectedDiagnostic = expected.Diagnostics.Items[index];
            MarkdownDiagnostic actualDiagnostic = actual.Diagnostics.Items[index];
            Assert.That(actualDiagnostic.Severity, Is.EqualTo(expectedDiagnostic.Severity));
            Assert.That(actualDiagnostic.Code, Is.EqualTo(expectedDiagnostic.Code));
            Assert.That(actualDiagnostic.Message, Is.EqualTo(expectedDiagnostic.Message));
            AssertSpanEquivalent(expectedDiagnostic.Span, actualDiagnostic.Span);
            index = index + 1;
        }
    }

    private static void AssertNodesEquivalent(MarkdownNode expected, MarkdownNode actual)
    {
        Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
        Assert.That(actual.Literal, Is.EqualTo(expected.Literal));
        AssertSpanEquivalent(expected.Span, actual.Span);
        Assert.That(actual.Attributes.Count, Is.EqualTo(expected.Attributes.Count));
        int index = 0;
        while (index < expected.Attributes.Count)
        {
            Assert.That(actual.Attributes[index].Name, Is.EqualTo(expected.Attributes[index].Name));
            Assert.That(actual.Attributes[index].Value, Is.EqualTo(expected.Attributes[index].Value));
            index = index + 1;
        }

        if (expected.Trivia == null)
        {
            Assert.That(actual.Trivia, Is.Null);
        }
        else
        {
            Assert.That(actual.Trivia, Is.Not.Null);
            Assert.That(actual.Trivia!.Text, Is.EqualTo(expected.Trivia.Text));
        }

        Assert.That(actual.Children.Count, Is.EqualTo(expected.Children.Count));
        index = 0;
        while (index < expected.Children.Count)
        {
            AssertNodesEquivalent(expected.Children[index], actual.Children[index]);
            index = index + 1;
        }
    }

    private static void AssertSpanEquivalent(MarkdownSourceSpan expected, MarkdownSourceSpan actual)
    {
        Assert.That(actual.Start.Offset, Is.EqualTo(expected.Start.Offset));
        Assert.That(actual.Start.Line, Is.EqualTo(expected.Start.Line));
        Assert.That(actual.Start.Column, Is.EqualTo(expected.Start.Column));
        Assert.That(actual.End.Offset, Is.EqualTo(expected.End.Offset));
        Assert.That(actual.End.Line, Is.EqualTo(expected.End.Line));
        Assert.That(actual.End.Column, Is.EqualTo(expected.End.Column));
    }

    private static int CountNodeKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        int count = node.Kind == kind ? 1 : 0;
        int index = 0;
        while (index < node.Children.Count)
        {
            count = count + CountNodeKind(node.Children[index], kind);
            index = index + 1;
        }

        return count;
    }
}

internal class ThrowingMarkdownInputReader : TextReader
{
    public bool Disposed;

    public ThrowingMarkdownInputReader()
    {
        Disposed = false;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        throw new IOException("Synthetic Markdown reader failure.");
    }

    protected override void Dispose(bool disposing)
    {
        Disposed = true;
        base.Dispose(disposing);
    }
}
