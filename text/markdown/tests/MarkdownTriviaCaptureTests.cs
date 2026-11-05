using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownTriviaCaptureTests
{
    [Test]
    public void Parser_CapturesTriviaWhenTrackTriviaIsEnabled()
    {
        string source = "# Title\n\nparagraph";
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .TrackTrivia()
            .Build();

        MarkdownDocument document = MarkdownParser.Parse(source, pipeline);
        MarkdownNode heading = document.Root.GetChild(0);
        MarkdownNode paragraph = document.Root.GetChild(1);

        Assert.That(document.Root.HasTrivia(), Is.True);
        Assert.That(document.Root.GetTriviaText(), Is.EqualTo(source));
        Assert.That(heading.HasTrivia(), Is.True);
        Assert.That(heading.GetTriviaText(), Is.EqualTo("# Title"));
        Assert.That(paragraph.HasTrivia(), Is.True);
        Assert.That(paragraph.GetTriviaText(), Is.EqualTo("paragraph"));
        Assert.That(paragraph.GetChild(0).HasTrivia(), Is.False);
    }

    [Test]
    public void Parser_DoesNotCaptureTriviaByDefault()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Title\n\nparagraph");

        Assert.That(document.Root.HasTrivia(), Is.False);
        Assert.That(document.Root.GetChild(0).HasTrivia(), Is.False);
    }

    [Test]
    public void TriviaCapture_CapturesManualSpansAndSkipsEmptySpans()
    {
        MarkdownDocument document = MarkdownDocument.Create(MarkdownSourceSpan.Create(0, 1, 1, 12, 1, 13));
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph, MarkdownSourceSpan.Create(2, 1, 3, 7, 1, 8));
        MarkdownNode empty = MarkdownNode.Create(MarkdownNodeKind.Text);
        paragraph.AppendChild(empty);
        document.Root.AppendChild(paragraph);

        MarkdownTriviaCapture.Apply(document, "0123456789ab");

        Assert.That(document.Root.GetTriviaText(), Is.EqualTo("0123456789ab"));
        Assert.That(paragraph.GetTriviaText(), Is.EqualTo("23456"));
        Assert.That(empty.HasTrivia(), Is.False);
    }

    [Test]
    public void TriviaValuesAreCopied()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(1, 1, 2, 4, 1, 5);
        MarkdownTrivia trivia = new MarkdownTrivia(span, "abc");
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Text);

        node.SetTrivia(trivia);
        span.Start.Offset = 99;
        trivia.Text = "changed";

        MarkdownTrivia? copy = node.GetTrivia();
        Assert.That(copy, Is.Not.Null);
        copy!.Text = "mutated copy";
        copy.Span.Start.Offset = 42;

        Assert.That(node.GetTriviaText(), Is.EqualTo("abc"));
        Assert.That(node.GetTrivia()!.Span.Start.Offset, Is.EqualTo(1));

        node.ClearTrivia();

        Assert.That(node.HasTrivia(), Is.False);
    }

    [Test]
    public void CloneAndEventsPreserveTrivia()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .TrackTrivia()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("hello", pipeline);

        MarkdownDocument clone = document.CloneDocument();
        MarkdownEvent[] events = MarkdownEventReader.ReadAll(document);
        MarkdownDocument rebuilt = MarkdownEventWriter.WriteAll(events);

        Assert.That(clone.Root.GetTriviaText(), Is.EqualTo("hello"));
        Assert.That(clone.Root.GetChild(0).GetTriviaText(), Is.EqualTo("hello"));
        Assert.That(events[0].GetTriviaText(), Is.EqualTo("hello"));
        Assert.That(rebuilt.Root.GetTriviaText(), Is.EqualTo("hello"));
        Assert.That(rebuilt.Root.GetChild(0).GetTriviaText(), Is.EqualTo("hello"));
    }

    [Test]
    public void GetSourceText_ReturnsSpanSlice()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(2, 1, 3, 5, 1, 6);

        Assert.That(MarkdownTriviaCapture.GetSourceText("abcdef", span), Is.EqualTo("cde"));
        Assert.That(MarkdownTriviaCapture.GetSourceText("abcdef", MarkdownSourceSpan.Create(3, 1, 4, 3, 1, 4)), Is.EqualTo(""));
    }

    [Test]
    public void TriviaCaptureRejectsInvalidArguments()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode node = new MarkdownNode();
        MarkdownSourceSpan span = new MarkdownSourceSpan();
        MarkdownTrivia trivia = new MarkdownTrivia(span, "");

        Assert.That(() => new MarkdownTrivia(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownTrivia(span, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTriviaCapture.Apply(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTriviaCapture.Apply(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTriviaCapture.CaptureNode(null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTriviaCapture.CaptureNode(node, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTriviaCapture.GetSourceText(null!, span), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTriviaCapture.GetSourceText("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownTriviaCapture.GetSourceText("abc", MarkdownSourceSpan.Create(0, 1, 1, 5, 1, 6)), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => node.SetTrivia(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownEvent().SetTrivia(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(trivia.Copy().Text, Is.EqualTo(""));
    }
}
