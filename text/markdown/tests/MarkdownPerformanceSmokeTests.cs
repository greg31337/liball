using System.Diagnostics;
using System.Text;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownPerformanceSmokeTests
{
    public const int MaximumSmokeMilliseconds = 10000;

    [Test]
    public void LargeParagraph_ParseAndRenderCompletesPromptly()
    {
        RunSmokeCase("large paragraph", BuildLargeParagraph(24000), MarkdownPipeline.CommonMarkPreset());
    }

    [Test]
    public void LargeList_ParseAndRenderCompletesPromptly()
    {
        RunSmokeCase("large list", BuildLargeList(2500), MarkdownPipeline.CommonMarkPreset());
    }

    [Test]
    public void ManyLinks_ParseAndRenderCompletesPromptly()
    {
        RunSmokeCase("many links", BuildManyLinks(1500), MarkdownPipeline.CommonMarkPreset());
    }

    [Test]
    public void LongEmphasisRuns_ParseAndRenderCompletesPromptly()
    {
        RunSmokeCase("long emphasis runs", BuildLongEmphasisRuns(1800), MarkdownPipeline.CommonMarkPreset());
    }

    public static void RunSmokeCase(string name, string source, MarkdownPipeline pipeline)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        MarkdownDocument document = MarkdownParser.Parse(source, pipeline);
        string html = HtmlRenderer.Render(document, pipeline);
        string markdown = MarkdownRenderer.Render(document, pipeline);
        string plainText = PlainTextRenderer.Render(document, pipeline);

        stopwatch.Stop();

        Assert.That(document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document));
        Assert.That(html.Length, Is.GreaterThan(0), name);
        Assert.That(markdown.Length, Is.GreaterThan(0), name);
        Assert.That(plainText.Length, Is.GreaterThan(0), name);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(MaximumSmokeMilliseconds), name);
    }

    public static string BuildLargeParagraph(int words)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < words)
        {
            builder.Append("word");
            builder.Append(index.ToString());
            builder.Append(' ');
            index = index + 1;
        }

        return builder.ToString();
    }

    public static string BuildLargeList(int items)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < items)
        {
            builder.Append("- item ");
            builder.Append(index.ToString());
            builder.Append('\n');
            index = index + 1;
        }

        return builder.ToString();
    }

    public static string BuildManyLinks(int links)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < links)
        {
            builder.Append("[link");
            builder.Append(index.ToString());
            builder.Append("](https://example.com/");
            builder.Append(index.ToString());
            builder.Append(") ");
            index = index + 1;
        }

        return builder.ToString();
    }

    public static string BuildLongEmphasisRuns(int runs)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < runs)
        {
            builder.Append("*emphasis");
            builder.Append(index.ToString());
            builder.Append("* ");
            builder.Append("**strong");
            builder.Append(index.ToString());
            builder.Append("** ");
            index = index + 1;
        }

        return builder.ToString();
    }
}
