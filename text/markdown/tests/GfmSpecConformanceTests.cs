using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class GfmSpecConformanceTests
{
    [Test]
    public void GfmHtmlFixture_LoadsEveryOfficialExampleAndExtensionSection()
    {
        string path = GetGfmFixturePath();

        GfmSpecExample[] examples = GfmSpecFixtureLoader.LoadFromFile(path);

        Assert.That(File.Exists(path), Is.True);
        Assert.That(examples.Length, Is.EqualTo(677));
        Assert.That(examples[0].ExampleNumber, Is.EqualTo(1));
        Assert.That(examples[0].Section, Is.EqualTo("Tabs"));
        Assert.That(examples[676].ExampleNumber, Is.EqualTo(677));
        Assert.That(examples[676].Section, Is.EqualTo("Textual content"));
        Assert.That(CountExtensionSections(examples), Is.EqualTo(5));
    }

    [TestCaseSource(nameof(GfmExamples))]
    public void GfmExample_ParsesAndRendersExpectedHtml(GfmSpecExample example)
    {
        IMarkdown markdown = new Markdown();
        IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
        if (GfmSpecFixtureLoader.IsExtensionSection(example.Section))
        {
            builder.UseGfm();
        }

        builder.AllowRawHtml();
        builder.AllowUnsafeLinks();
        builder.UseXhtml();
        if (example.Section != "Disallowed Raw HTML (extension)")
        {
            builder.AllowUnsafeHtml();
        }

        MarkdownPipeline pipeline = builder.Build();
        MarkdownDocument document = markdown.ParseSourceWithPipeline(example.Markdown, pipeline);

        string html = markdown.RenderHtmlDocumentWithPipeline(document, pipeline);
        string identity = "GFM example "
            + example.ExampleNumber.ToString()
            + " in section '"
            + example.Section
            + "' (fixture lines "
            + example.StartLine.ToString()
            + "-"
            + example.EndLine.ToString()
            + ")";

        Assert.That(document.Root.Kind, Is.EqualTo(MarkdownNodeKind.Document), identity + " did not parse to a document.");
        Assert.That(html, Is.EqualTo(GetExpectedHtml(example)), identity + " rendered different HTML.");
    }

    public static IEnumerable<TestCaseData> GfmExamples()
    {
        GfmSpecExample[] examples = GfmSpecFixtureLoader.LoadFromFile(GetGfmFixturePath());
        int index = 0;
        while (index < examples.Length)
        {
            GfmSpecExample example = examples[index];
            string name = "Example_"
                + example.ExampleNumber.ToString("000")
                + "_"
                + CommonMarkSpecConformanceTests.SanitizeTestName(example.Section);
            yield return new TestCaseData(example).SetName(name);
            index = index + 1;
        }
    }

    public static string GetGfmFixturePath()
    {
        return Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Fixtures",
            "Gfm",
            "gfm-0.29-spec.html");
    }

    public static int CountExtensionSections(GfmSpecExample[] examples)
    {
        List<string> sections = new List<string>();
        int index = 0;
        while (index < examples.Length)
        {
            string section = examples[index].Section;
            if (GfmSpecFixtureLoader.IsExtensionSection(section) && !sections.Contains(section))
            {
                sections.Add(section);
            }

            index = index + 1;
        }

        return sections.Count;
    }

    public static string GetExpectedHtml(GfmSpecExample example)
    {
        // CommonMark 0.31.2 accepts these legacy comment forms that GFM 0.29 escaped.
        if (example.ExampleNumber == 649)
        {
            return "<p>foo <!-- not a comment -- two hyphens --></p>\n";
        }

        if (example.ExampleNumber == 650)
        {
            return "<p>foo <!--> foo --&gt;</p>\n<p>foo <!-- foo---></p>\n";
        }

        return example.Html;
    }
}
