using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class CommonMarkSpecConformanceTests
{
    [Test]
    public void CommonMarkJsonFixture_LoadsEveryOfficialExample()
    {
        string path = GetCommonMarkFixturePath();

        CommonMarkSpecExample[] examples = CommonMarkSpecFixtureLoader.LoadFromFile(path);

        Assert.That(File.Exists(path), Is.True);
        Assert.That(examples.Length, Is.EqualTo(652));
        Assert.That(examples[0].ExampleNumber, Is.EqualTo(1));
        Assert.That(examples[0].Section, Is.EqualTo("Tabs"));
        Assert.That(examples[651].ExampleNumber, Is.EqualTo(652));
        Assert.That(examples[651].Section, Is.EqualTo("Textual content"));
    }

    [TestCaseSource(nameof(CommonMarkExamples))]
    public void CommonMarkExample_ParsesAndRendersExpectedHtml(CommonMarkSpecExample example)
    {
        IMarkdown markdown = new Markdown();
        MarkdownPipeline pipeline = markdown.CreatePipelineBuilder()
            .AllowUnsafeHtml()
            .AllowUnsafeLinks()
            .UseXhtml()
            .Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(example.Markdown, pipeline);

        string html = markdown.RenderHtmlDocumentWithPipeline(document, pipeline);
        string identity = "CommonMark example "
            + example.ExampleNumber.ToString()
            + " in section '"
            + example.Section
            + "' (spec lines "
            + example.StartLine.ToString()
            + "-"
            + example.EndLine.ToString()
            + ")";

        Assert.That(document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document), identity + " did not parse to a document.");
        Assert.That(html, Is.EqualTo(example.Html), identity + " rendered different HTML.");
    }

    public static IEnumerable<TestCaseData> CommonMarkExamples()
    {
        CommonMarkSpecExample[] examples = CommonMarkSpecFixtureLoader.LoadFromFile(GetCommonMarkFixturePath());
        int index = 0;
        while (index < examples.Length)
        {
            CommonMarkSpecExample example = examples[index];
            string name = "Example_"
                + example.ExampleNumber.ToString("000")
                + "_"
                + SanitizeTestName(example.Section);
            yield return new TestCaseData(example).SetName(name);
            index = index + 1;
        }
    }

    public static string GetCommonMarkFixturePath()
    {
        return Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Fixtures",
            "CommonMark",
            "commonmark-0.31.2-examples.json");
    }

    public static string SanitizeTestName(string text)
    {
        char[] characters = text.ToCharArray();
        int index = 0;
        while (index < characters.Length)
        {
            char character = characters[index];
            if (!char.IsLetterOrDigit(character))
            {
                characters[index] = '_';
            }

            index = index + 1;
        }

        return new string(characters);
    }
}
