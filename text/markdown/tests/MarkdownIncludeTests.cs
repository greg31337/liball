using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownIncludeTests
{
    [Test]
    public void Includes_AreDisabledByDefault()
    {
        IMarkdown markdown = new Markdown();
        TestIncludeResolver resolver = new TestIncludeResolver(context => Found("part.md", "included"));
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();

        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "!INCLUDE \"part.md\"",
            builder.Build());

        Assert.That(resolver.CallCount, Is.EqualTo(0));
        Assert.That(document.Source, Is.EqualTo("!INCLUDE \"part.md\""));
        Assert.That(document.SourceAttributions, Is.Empty);
        Assert.That(document.Diagnostics.Items, Is.Empty);
    }

    [Test]
    public void Includes_ResolveNestedContentWithRelativeContextAndAttribution()
    {
        IMarkdown markdown = new Markdown();
        TestIncludeResolver resolver = new TestIncludeResolver(context =>
        {
            if (context.RequestedName == "part.md")
            {
                Assert.That(context.RelativeToName, Is.EqualTo("docs/main.md"));
                Assert.That(context.Depth, Is.EqualTo(1));
                return Found("docs/part.md", "## Part\n\n!INCLUDE \"child.md\"");
            }

            Assert.That(context.RequestedName, Is.EqualTo("child.md"));
            Assert.That(context.RelativeToName, Is.EqualTo("docs/part.md"));
            Assert.That(context.Depth, Is.EqualTo(2));
            return Found("docs/child.md", "child text");
        });
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.UseIncludes(resolver)
            .SetIncludeRootName("docs/main.md")
            .SetMaximumIncludeDepth(3);

        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "# Root\n\n!INCLUDE \"part.md\"\n\nEnd",
            builder.Build());

        Assert.That(resolver.CallCount, Is.EqualTo(2));
        Assert.That(document.Source, Is.EqualTo("# Root\n\n## Part\n\nchild text\n\nEnd"));
        Assert.That(markdown.RenderHtmlDocumentWithPipeline(document, builder.Build()), Does.Contain("<h2>Part</h2>"));
        Assert.That(markdown.RenderHtmlDocumentWithPipeline(document, builder.Build()), Does.Contain("<p>child text</p>"));
        MarkdownSourceAttribution child = FindAttribution(document, "docs/child.md");
        Assert.That(child.IncludeDepth, Is.EqualTo(2));
        Assert.That(child.OriginalSpan.Start.Offset, Is.EqualTo(0));
        Assert.That(child.ExpandedSpan.Start.Offset, Is.GreaterThan(0));
        Assert.That(HasAttribution(document, "docs/main.md"), Is.True);
        Assert.That(HasAttribution(document, "docs/part.md"), Is.True);
    }

    [Test]
    public void Includes_ReportMissingContentAndPreserveDirectiveText()
    {
        IMarkdown markdown = new Markdown();
        TestIncludeResolver resolver = new TestIncludeResolver(context => new MarkdownIncludeResult());
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.UseIncludes(resolver).SetIncludeRootName("main.md");

        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "before\n\n!INCLUDE \"missing.md\"\n",
            builder.Build());

        Assert.That(document.Source, Is.EqualTo("before\n\n!INCLUDE \"missing.md\"\n"));
        Assert.That(document.Diagnostics.Items.Count, Is.EqualTo(1));
        Assert.That(document.Diagnostics.Items[0].Code, Is.EqualTo(MarkdownIncludeProcessor.NotFoundDiagnosticCode));
        Assert.That(document.Diagnostics.Items[0].SourceName, Is.EqualTo("main.md"));
        Assert.That(document.Diagnostics.Items[0].Span.Start.Offset, Is.EqualTo(8));
    }

    [Test]
    public void Includes_DetectCanonicalSourceCycles()
    {
        IMarkdown markdown = new Markdown();
        TestIncludeResolver resolver = new TestIncludeResolver(context =>
        {
            if (context.RequestedName == "a.md")
            {
                return Found("a.md", "!INCLUDE \"main.md\"\n");
            }

            return Found("main.md", "unreachable");
        });
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.UseIncludes(resolver).SetIncludeRootName("main.md");

        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "!INCLUDE \"a.md\"\n",
            builder.Build());

        Assert.That(resolver.CallCount, Is.EqualTo(2));
        Assert.That(document.Source, Is.EqualTo("!INCLUDE \"main.md\"\n"));
        Assert.That(document.Diagnostics.Items.Count, Is.EqualTo(1));
        Assert.That(document.Diagnostics.Items[0].Code, Is.EqualTo(MarkdownIncludeProcessor.CycleDiagnosticCode));
        Assert.That(document.Diagnostics.Items[0].SourceName, Is.EqualTo("a.md"));
    }

    [Test]
    public void Includes_EnforceDepthBeforeResolvingDeeperRequest()
    {
        IMarkdown markdown = new Markdown();
        TestIncludeResolver resolver = new TestIncludeResolver(context =>
            Found("a.md", "!INCLUDE \"b.md\"\n"));
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.UseIncludes(resolver)
            .SetIncludeRootName("main.md")
            .SetMaximumIncludeDepth(1);

        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "!INCLUDE \"a.md\"\n",
            builder.Build());

        Assert.That(resolver.CallCount, Is.EqualTo(1));
        Assert.That(document.Source, Is.EqualTo("!INCLUDE \"b.md\"\n"));
        Assert.That(document.Diagnostics.Items[0].Code, Is.EqualTo(MarkdownIncludeProcessor.DepthDiagnosticCode));
        Assert.That(document.Diagnostics.Items[0].SourceName, Is.EqualTo("a.md"));
    }

    [Test]
    public void Includes_WorkForStringReaderAndMemoryInputs()
    {
        IMarkdown markdown = new Markdown();
        TestIncludeResolver resolver = new TestIncludeResolver(context => Found("part.md", "included"));
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.UseIncludes(resolver);
        MarkdownPipeline pipeline = builder.Build();
        string source = "!INCLUDE 'part.md'";

        MarkdownDocument fromString = markdown.ParseSourceWithPipeline(source, pipeline);
        MarkdownDocument fromReader = markdown.ParseReaderWithPipeline(new StringReader(source), pipeline);
        MarkdownDocument fromMemory = markdown.ParseMemoryWithPipeline(source.AsMemory(), pipeline);

        Assert.That(fromString.Source, Is.EqualTo("included"));
        Assert.That(fromReader.Source, Is.EqualTo(fromString.Source));
        Assert.That(fromMemory.Source, Is.EqualTo(fromString.Source));
    }

    [Test]
    public void Includes_CanBeDisabledAndValidateConfiguration()
    {
        IMarkdown markdown = new Markdown();
        TestIncludeResolver resolver = new TestIncludeResolver(context => Found("part.md", "included"));
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();

        builder.UseIncludes(resolver).DisableIncludes();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "!INCLUDE \"part.md\"",
            builder.Build());

        Assert.That(resolver.CallCount, Is.EqualTo(0));
        Assert.That(document.Source, Is.EqualTo("!INCLUDE \"part.md\""));
        Assert.That(() => builder.UseIncludes(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetIncludeRootName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => builder.SetMaximumIncludeDepth(0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Includes_ReportEnabledOptionsWithoutResolver()
    {
        IMarkdown markdown = new Markdown();
        MarkdownOptions options = new MarkdownOptions();
        options.ProcessIncludes = true;
        options.IncludeRootName = "main.md";
        MarkdownPipeline pipeline = markdown.CreatePipelineFromOptions(options);

        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "!INCLUDE \"part.md\"",
            pipeline);

        Assert.That(document.Diagnostics.Items.Count, Is.EqualTo(1));
        Assert.That(document.Diagnostics.Items[0].Code, Is.EqualTo(MarkdownIncludeProcessor.ResolverMissingDiagnosticCode));
        Assert.That(document.Source, Is.EqualTo("!INCLUDE \"part.md\""));
    }

    [Test]
    public void IncludeAttributionAndDiagnosticSourceNamesAreCloned()
    {
        Markdown markdown = new Markdown();
        TestIncludeResolver resolver = new TestIncludeResolver(context => new MarkdownIncludeResult());
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        builder.UseIncludes(resolver).SetIncludeRootName("main.md");
        MarkdownDocument document = markdown.ParseSourceWithPipeline(
            "root\n!INCLUDE \"missing.md\"",
            builder.Build());

        MarkdownDocument clone = markdown.CreateTreeOperations().CloneDocument(document);
        document.SourceAttributions[0].SourceName = "changed";
        document.Diagnostics.Items[0].SourceName = "changed";

        Assert.That(clone.SourceAttributions[0].SourceName, Is.EqualTo("main.md"));
        Assert.That(clone.Diagnostics.Items[0].SourceName, Is.EqualTo("main.md"));
    }

    static MarkdownIncludeResult Found(string sourceName, string content)
    {
        MarkdownIncludeResult result = new MarkdownIncludeResult();
        result.Found = true;
        result.SourceName = sourceName;
        result.Content = content;
        return result;
    }

    static bool HasAttribution(MarkdownDocument document, string sourceName)
    {
        int index = 0;
        while (index < document.SourceAttributions.Length)
        {
            if (document.SourceAttributions[index].SourceName == sourceName)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static MarkdownSourceAttribution FindAttribution(MarkdownDocument document, string sourceName)
    {
        int index = 0;
        while (index < document.SourceAttributions.Length)
        {
            if (document.SourceAttributions[index].SourceName == sourceName)
            {
                return document.SourceAttributions[index];
            }

            index++;
        }

        throw new AssertionException("Expected source attribution was not found.");
    }
}

internal class TestIncludeResolver : IMarkdownIncludeResolver
{
    internal Func<MarkdownIncludeContext, MarkdownIncludeResult> Handler;
    internal int CallCount;

    internal TestIncludeResolver(Func<MarkdownIncludeContext, MarkdownIncludeResult> handler)
    {
        Handler = handler;
        CallCount = 0;
    }

    public MarkdownIncludeResult Resolve(MarkdownIncludeContext context)
    {
        CallCount++;
        return Handler(context);
    }
}
