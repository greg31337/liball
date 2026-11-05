using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownTriviaRoundtripTests
{
    [Test]
    public void MarkdownRenderer_RendersRootTriviaWhenTrackingIsEnabled()
    {
        string source =
            "# Title\r\n" +
            "\r\n" +
            "paragraph without trailing newline";
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .TrackTrivia()
            .Build();

        string markdown = Markdown.ToMarkdown(source, pipeline);

        Assert.That(markdown, Is.EqualTo(source));
    }

    [Test]
    public void MarkdownRenderer_IgnoresTriviaWhenTrackingIsDisabledForRendering()
    {
        MarkdownPipeline trackingPipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .TrackTrivia()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse("hello", trackingPipeline);

        string markdown = MarkdownRenderer.Render(document, MarkdownPipeline.CommonMarkPreset());

        Assert.That(document.Root.HasTrivia(), Is.True);
        Assert.That(markdown, Is.EqualTo("hello\n"));
    }

    [Test]
    public void MarkdownRenderer_FallsBackWhenTrackedNodeHasNoTrivia()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .TrackTrivia()
            .Build();

        text.SetLiteral("generated");
        paragraph.AppendChild(text);
        document.Root.AppendChild(paragraph);

        Assert.That(MarkdownRenderer.Render(document, pipeline), Is.EqualTo("generated\n"));
    }

    [Test]
    public void MarkdownRenderer_CanRenderChildTriviaWhenRootTriviaIsAbsent()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .TrackTrivia()
            .Build();

        paragraph.SetTrivia(new MarkdownTrivia(MarkdownSourceSpan.Create(0, 1, 1, 18, 1, 19), "original paragraph"));
        document.Root.AppendChild(paragraph);

        Assert.That(MarkdownRenderer.Render(document, pipeline), Is.EqualTo("original paragraph"));
    }

    [Test]
    public void MarkdownRenderer_PreservesTriviaThroughEventRoundtrip()
    {
        string source = "hello";
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .TrackTrivia()
            .Build();
        MarkdownDocument document = MarkdownParser.Parse(source, pipeline);
        MarkdownDocument rebuilt = MarkdownEventWriter.WriteAll(MarkdownEventReader.ReadAll(document));

        Assert.That(MarkdownRenderer.Render(rebuilt, pipeline), Is.EqualTo(source));
    }

    [Test]
    public void TriviaRenderingHelpersRejectInvalidArguments()
    {
        StringWriter writer = new StringWriter();
        MarkdownWriter markdownWriter = new MarkdownWriter(writer);
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.CommonMarkPreset()
            .TrackTrivia()
            .Build();
        MarkdownRenderContext context = new MarkdownRenderContext(markdownWriter, pipeline);
        MarkdownNode node = MarkdownNode.Create(MarkdownNodeKind.Paragraph);

        Assert.That(() => MarkdownRenderer.ShouldRenderTrivia(null!, node), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.ShouldRenderTrivia(context, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderTrivia(null!, node), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderTrivia(context, null!), Throws.TypeOf<ArgumentNullException>());
    }
}
