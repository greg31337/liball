using System;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownCompleteFacadeTests
{
    [Test]
    public void RootInterface_ReachesEveryCapabilityFamilyThroughInterfaces()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .UseGfm()
            .TrackTrivia()
            .SetLineEnding("\r\n")
            .Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "# Title\n\nbody ~~old~~",
            pipeline);
        IMarkdownProcessingOperations processing = services.CreateProcessingOperations();
        IMarkdownTreeOperations trees = services.CreateTreeOperations();
        IMarkdownDocumentOperations documentOperations = services.CreateDocumentOperations(document);
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilderWithPipeline(pipeline);
        int visited = 0;
        IMarkdownNodeVisitor visitor = new MarkdownNodeVisitorAdapter(
            context =>
            {
                visited = visited + 1;
                return MarkdownTreeWalkResult.Continue;
            },
            null);

        MarkdownDocument clone = trees.CloneDocument(document);
        MarkdownTreeWalkResult walkResult = trees.WalkDocument(clone, visitor);
        MarkdownDocumentAnalysis analysis = services.AnalyzeDocumentWithPipeline(document, pipeline);
        builder.Heading(2, "Generated");

        Assert.That(services, Is.SameAs(markdown));
        Assert.That(processing.NormalizeDocumentWithPipeline(document, pipeline), Is.EqualTo(
            markdown.NormalizeDocumentWithPipeline(document, pipeline)));
        Assert.That(markdown.RenderHtmlDocumentWithPipeline(document, pipeline), Does.Contain("<del>old</del>"));
        Assert.That(builder.ToDocumentWithPipeline(pipeline).Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(analysis.PlainText, Does.Contain("\r\n"));
        Assert.That(clone.Root, Is.Not.SameAs(document.Root));
        Assert.That(walkResult, Is.EqualTo(MarkdownTreeWalkResult.Continue));
        Assert.That(visited, Is.GreaterThan(1));
        Assert.That(documentOperations.GetSourceMapEntries(), Is.Not.Empty);
    }

    [Test]
    public void NormalizationFacade_CoversSourceAndDocumentWithDefaultAndPipelineSettings()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .SetLineEnding("\r\n")
            .Build();
        string source = "# Title\n\ntext";
        MarkdownDocument document = markdown.ParseSource(source);

        Assert.That(markdown.NormalizeSource(source), Is.EqualTo("# Title\n\ntext\n"));
        Assert.That(markdown.NormalizeSourceWithPipeline(source, pipeline), Is.EqualTo("# Title\r\n\r\ntext\r\n"));
        Assert.That(markdown.NormalizeDocument(document), Is.EqualTo("# Title\n\ntext\n"));
        Assert.That(markdown.NormalizeDocumentWithPipeline(document, pipeline), Is.EqualTo("# Title\r\n\r\ntext\r\n"));
    }

    [Test]
    public void MaterializedEventFacade_CoversSourcePipelineDocumentAndSubtreeInputs()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        MarkdownPipeline pipeline = markdown.CreateGfmPipeline();
        MarkdownEvent[] commonMarkEvents = services.ReadSourceEvents("text ~~old~~");
        MarkdownEvent[] gfmEvents = services.ReadSourceEventsWithPipeline("text ~~old~~", pipeline);
        MarkdownDocument document = markdown.ParseSource("first *second*");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownEvent[] documentEvents = services.ReadDocumentEvents(document);
        MarkdownEvent[] nodeEvents = services.ReadNodeEvents(paragraph);
        bool commonMarkHasStrikethrough = ContainsKind(commonMarkEvents, MarkdownNodeKind.Strikethrough);
        bool gfmHasStrikethrough = ContainsKind(gfmEvents, MarkdownNodeKind.Strikethrough);

        Assert.That(commonMarkHasStrikethrough, Is.False);
        Assert.That(gfmHasStrikethrough, Is.True);
        Assert.That(documentEvents[0].NodeKind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(documentEvents[documentEvents.Length - 1].EventKind, Is.EqualTo(MarkdownEventKind.LeaveNode));
        Assert.That(nodeEvents[0].NodeKind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(nodeEvents[0].Depth, Is.EqualTo(0));
        Assert.That(markdown.CreateDocumentFromEvents(gfmEvents).Root.GetChildCount(), Is.EqualTo(1));
    }

    [Test]
    public void AddedFacadePaths_ValidateExpectedReferenceArguments()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownDataServices services = markdown.GetDataServices();
        MarkdownPipeline pipeline = markdown.CreateCommonMarkPipeline();
        MarkdownDocument document = markdown.ParseSource("text");

        Assert.That(() => markdown.NormalizeSourceWithPipeline(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.NormalizeSourceWithPipeline("", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.NormalizeDocument(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => markdown.NormalizeDocumentWithPipeline(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.AnalyzeDocumentWithPipeline(null!, pipeline), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.AnalyzeDocumentWithPipeline(document, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.ReadNodeEvents(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.ReadSourceEvents(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => services.ReadSourceEventsWithPipeline("", null!), Throws.TypeOf<ArgumentNullException>());
    }

    static bool ContainsKind(MarkdownEvent[] events, MarkdownNodeKind kind)
    {
        int index = 0;
        while (index < events.Length)
        {
            if (events[index].NodeKind == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
