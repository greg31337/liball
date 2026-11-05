using System;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class PublicBoundaryFailureMatrixTests
{
    [Test]
    public void EmptyInputsDocumentsEventsAndCollectionsRemainValidAcrossFacadeOperations()
    {
        IMarkdown markdown = new Markdown();
        MarkdownDocument parsed = markdown.ParseSource("");
        MarkdownDocument fromNodes = markdown.CreateDocumentFromNodes(Array.Empty<MarkdownNode>());
        MarkdownEvent[] events = markdown.GetDataServices().ReadDocumentEvents(parsed);
        MarkdownDocument fromEvents = markdown.CreateDocumentFromEvents(events);
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilder();
        StringWriter generated = new StringWriter();
        IMarkdownWriter writer = markdown.CreateMarkdownWriter(generated);

        writer.WriteBulletList(Array.Empty<string>());
        writer.WriteOrderedList(Array.Empty<string>(), int.MaxValue);

        Assert.That(parsed.Root.Children, Is.Empty);
        Assert.That(parsed.Diagnostics.Items, Is.Empty);
        Assert.That(fromNodes.Root.Children, Is.Empty);
        Assert.That(fromEvents.Root.Children, Is.Empty);
        Assert.That(markdown.RenderHtmlSource(""), Is.EqualTo(""));
        Assert.That(markdown.RenderMarkdownSource(""), Is.EqualTo(""));
        Assert.That(markdown.RenderPlainTextSource(""), Is.EqualTo(""));
        Assert.That(markdown.RenderDebugTreeDocument(parsed), Is.EqualTo(markdown.RenderDebugTreeSource("")));
        Assert.That(markdown.RenderXmlAstDocument(parsed), Is.EqualTo(markdown.RenderXmlAstSource("")));
        Assert.That(builder.ToMarkdown(), Is.EqualTo(""));
        Assert.That(builder.ToDocument().Root.Children, Is.Empty);
        Assert.That(generated.ToString(), Is.EqualTo(""));
    }

    [Test]
    public void ExtremeValidOptionsAndOrderedListValuesAreAcceptedWithoutOverflow()
    {
        IMarkdown markdown = new Markdown();
        MarkdownOptions options = new MarkdownOptions();
        options.MaximumNestingDepth = int.MaxValue;
        options.MaximumInputSize = int.MaxValue;
        options.MaximumReferenceDefinitions = int.MaxValue;
        options.MaximumOutputSize = int.MaxValue;
        options.MaximumLineWidth = int.MaxValue;
        options.MaximumIncludeDepth = int.MaxValue;
        options.SoftBreakMode = MarkdownSoftBreakMode.Cjk;
        MarkdownPipeline pipeline = markdown.CreatePipelineFromOptions(options);
        StringWriter output = new StringWriter();
        IMarkdownWriter writer = markdown.CreateMarkdownWriterWithPipeline(output, pipeline);

        writer.WriteOrderedList(new string[] { "maximum", "next" }, int.MaxValue);

        Assert.That(pipeline.GetOptions().MaximumNestingDepth, Is.EqualTo(int.MaxValue));
        Assert.That(pipeline.GetOptions().MaximumInputSize, Is.EqualTo(int.MaxValue));
        Assert.That(pipeline.GetOptions().MaximumReferenceDefinitions, Is.EqualTo(int.MaxValue));
        Assert.That(pipeline.GetOptions().MaximumOutputSize, Is.EqualTo(int.MaxValue));
        Assert.That(pipeline.GetOptions().MaximumLineWidth, Is.EqualTo(int.MaxValue));
        Assert.That(pipeline.GetOptions().MaximumIncludeDepth, Is.EqualTo(int.MaxValue));
        Assert.That(output.ToString(), Is.EqualTo("2147483647. maximum\n2147483648. next\n"));
    }

    [Test]
    public void InvalidEnumsAndDirectlyEditedOptionsAreRejectedAtThePublicBoundary()
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        MarkdownOptions options = new MarkdownOptions();

        Assert.That(
            () => builder.SetSoftBreakMode((MarkdownSoftBreakMode)99),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(builder.SetSoftBreakMode(MarkdownSoftBreakMode.Space).Build(), Is.Not.Null);

        options.Features = null!;
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentNullException>());

        options = new MarkdownOptions();
        options.MaximumNestingDepth = 0;
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentOutOfRangeException>());

        options = new MarkdownOptions();
        options.MaximumInputSize = 0;
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentOutOfRangeException>());

        options = new MarkdownOptions();
        options.MaximumReferenceDefinitions = 0;
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentOutOfRangeException>());

        options = new MarkdownOptions();
        options.MaximumOutputSize = 0;
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentOutOfRangeException>());

        options = new MarkdownOptions();
        options.MaximumLineWidth = -1;
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentOutOfRangeException>());

        options = new MarkdownOptions();
        options.MaximumIncludeDepth = 0;
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentOutOfRangeException>());

        options = new MarkdownOptions();
        options.SoftBreakMode = (MarkdownSoftBreakMode)(-1);
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentOutOfRangeException>());

        options = new MarkdownOptions();
        options.LineEnding = "\n\n";
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentException>());

        options = new MarkdownOptions();
        options.IncludeRootName = null!;
        Assert.That(() => markdown.CreatePipelineFromOptions(options), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void DisposedReadersAndWritersFailPredictablyOnRepeatedUse()
    {
        IMarkdown markdown = new Markdown();
        StringReader reader = new StringReader("text");
        StringWriter renderOutput = new StringWriter();
        StringWriter generationOutput = new StringWriter();
        reader.Dispose();
        renderOutput.Dispose();
        generationOutput.Dispose();
        IMarkdownWriter generator = markdown.CreateMarkdownWriter(generationOutput);

        Assert.That(() => markdown.ParseReader(reader), Throws.TypeOf<ObjectDisposedException>());
        Assert.That(() => markdown.ParseReader(reader), Throws.TypeOf<ObjectDisposedException>());
        Assert.That(() => markdown.WriteHtmlSource("text", renderOutput), Throws.TypeOf<ObjectDisposedException>());
        Assert.That(() => markdown.WriteHtmlSource("text", renderOutput), Throws.TypeOf<ObjectDisposedException>());
        Assert.That(() => generator.WriteRawMarkdown("text"), Throws.TypeOf<ObjectDisposedException>());
        Assert.That(() => generator.WriteRawMarkdown("text"), Throws.TypeOf<ObjectDisposedException>());
    }

    [Test]
    public void ThrowingCallerRulesAndResolversPropagateAndPipelinesRemainReusable()
    {
        IMarkdown markdown = new Markdown();
        int ruleCalls = 0;
        IMarkdownBlockRule rule = new TestBlockRule(
            "throw-once.block",
            context =>
            {
                ruleCalls = ruleCalls + 1;
                if (ruleCalls == 1)
                {
                    throw new InvalidOperationException("rule failure");
                }

                return false;
            });
        MarkdownPipeline rulePipeline = markdown.CreatePipelineBuilder()
            .ClearBlockRules()
            .AddBlockRule(rule)
            .Build();
        ThrowOnceIncludeResolver resolver = new ThrowOnceIncludeResolver();
        MarkdownPipeline includePipeline = markdown.CreatePipelineBuilder()
            .UseIncludes(resolver)
            .Build();

        Assert.That(
            () => markdown.ParseSourceWithPipeline("text", rulePipeline),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("rule failure"));
        Assert.That(markdown.ParseSourceWithPipeline("text", rulePipeline).Root.Children, Is.Empty);
        Assert.That(ruleCalls, Is.EqualTo(2));

        Assert.That(
            () => markdown.ParseSourceWithPipeline("!INCLUDE \"part.md\"", includePipeline),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("resolver failure"));
        MarkdownDocument recovered = markdown.ParseSourceWithPipeline(
            "!INCLUDE \"part.md\"",
            includePipeline);
        Assert.That(recovered.Diagnostics.Items.Count, Is.EqualTo(1));
        Assert.That(recovered.Diagnostics.Items[0].Code, Is.EqualTo("MD_INCLUDE_NOT_FOUND"));
        Assert.That(resolver.CallCount, Is.EqualTo(2));
    }

    [Test]
    public void OutputAndDestinationFailuresDoNotConsumeRejectedGenerationBudget()
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline limited = markdown.CreatePipelineBuilder()
            .SetMaximumOutputSize(3)
            .Build();
        ThrowOnceStringWriter destination = new ThrowOnceStringWriter();
        IMarkdownWriter writer = markdown.CreateMarkdownWriterWithPipeline(destination, limited);
        IMarkdownBuilder builder = markdown.CreateMarkdownBuilderWithPipeline(limited);

        Assert.That(
            () => writer.WriteRawMarkdown("abc"),
            Throws.TypeOf<IOException>().With.Message.EqualTo("destination failure"));
        Assert.That(() => writer.WriteRawMarkdown("abc"), Throws.Nothing);
        Assert.That(destination.ToString(), Is.EqualTo("abc"));

        Assert.That(() => builder.RawMarkdown("four"), Throws.TypeOf<InvalidOperationException>());
        Assert.That(builder.ToMarkdown(), Is.EqualTo(""));
        Assert.That(() => builder.RawMarkdown("abc"), Throws.Nothing);
        Assert.That(builder.ToMarkdown(), Is.EqualTo("abc"));

        Assert.That(
            () => markdown.RenderHtmlSourceWithPipeline("text", limited),
            Throws.TypeOf<InvalidOperationException>());
        Assert.That(markdown.RenderPlainTextSourceWithPipeline("ok", limited), Is.EqualTo("ok\n"));
    }

    private sealed class ThrowOnceIncludeResolver : IMarkdownIncludeResolver
    {
        public int CallCount;

        public MarkdownIncludeResult Resolve(MarkdownIncludeContext context)
        {
            CallCount = CallCount + 1;
            if (CallCount == 1)
            {
                throw new InvalidOperationException("resolver failure");
            }

            return new MarkdownIncludeResult();
        }
    }

    private sealed class ThrowOnceStringWriter : StringWriter
    {
        private bool failed;

        public ThrowOnceStringWriter()
        {
            failed = false;
        }

        public override void Write(string? value)
        {
            if (!failed)
            {
                failed = true;
                throw new IOException("destination failure");
            }

            base.Write(value);
        }
    }
}
