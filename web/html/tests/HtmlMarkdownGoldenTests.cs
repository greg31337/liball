using System.Text.Json;
using System.Text.Json.Serialization;

namespace UniversalHtml.Tests;

public class HtmlMarkdownGenerationCase
{
    [JsonPropertyName("example")]
    public int Example { get; set; }

    [JsonPropertyName("section")]
    public string Section { get; set; }

    [JsonPropertyName("html")]
    public string Html { get; set; }

    [JsonPropertyName("upstream_markdown")]
    public string UpstreamMarkdown { get; set; }

    [JsonPropertyName("expected_markdown")]
    public string ExpectedMarkdown { get; set; }

    public string Profile { get; set; }

    public override string ToString()
    {
        return this.Profile + " example " + this.Example + " ("
            + this.Section + ")";
    }
}

[TestFixture]
public class HtmlMarkdownGoldenTests
{
    [TestCaseSource(nameof(GetPinnedGenerationCases))]
    public void PinnedCommonMarkAndGfmHtmlGeneratesCanonicalMarkdown(
        HtmlMarkdownGenerationCase testCase)
    {
        HtmlNode fragment = Html.ParseFragment(testCase.Html, "body")
            .GetFragment();
        HtmlMarkdownOptions options = testCase.Profile == "GFM"
            ? HtmlMarkdownOptions.CreateGitHubFlavored()
            : HtmlMarkdownOptions.CreateCommonMark();

        HtmlMarkdownResult result = HtmlMarkdownConverter.Convert(fragment,
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True, testCase.ToString());
            Assert.That(result.IsComplete(), Is.True, testCase.ToString());
            Assert.That(result.GetMarkdown(),
                Is.EqualTo(testCase.ExpectedMarkdown),
                testCase.ToString() + " from "
                    + testCase.UpstreamMarkdown);
        });
    }

    [TestCase(
        "<p>one<div>two<p>three</div>four",
        "one\n\ntwo\n\nthree\n\nfour")]
    [TestCase(
        "<p><strong>alpha <em>beta</strong> gamma</em> delta",
        "**alpha *beta*** *gamma* delta")]
    [TestCase(
        "<table><tr><td>A<td>B<tr><td>C</table><p>After",
        "| A   | B   |\n| --- | --- |\n| C   |     |\n\nAfter")]
    public void CorrectedMalformedTreesHaveStableGoldenOutput(string html,
        string expected)
    {
        HtmlMarkdownResult result = HtmlMarkdownConverter.Convert(html,
            HtmlMarkdownOptions.CreateGitHubFlavored(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.GetMarkdown(), Is.EqualTo(expected));
        });
    }

    static IEnumerable<TestCaseData> GetPinnedGenerationCases()
    {
        foreach (HtmlMarkdownGenerationCase testCase in
            ReadCases("commonmark", "CommonMark"))
        {
            yield return new TestCaseData(testCase)
                .SetName("CommonMark_" + testCase.Example + "_"
                    + NormalizeName(testCase.Section));
        }

        foreach (HtmlMarkdownGenerationCase testCase in
            ReadCases("gfm", "GFM"))
        {
            yield return new TestCaseData(testCase)
                .SetName("GFM_" + testCase.Example + "_"
                    + NormalizeName(testCase.Section));
        }
    }

    static HtmlMarkdownGenerationCase[] ReadCases(string directoryName,
        string profile)
    {
        string path = FindFixturePath(directoryName);
        HtmlMarkdownGenerationCase[] cases =
            JsonSerializer.Deserialize<HtmlMarkdownGenerationCase[]>(
                File.ReadAllText(path));
        int index = 0;
        while (index < cases.Length)
        {
            cases[index].Profile = profile;
            index = index + 1;
        }

        return cases;
    }

    static string FindFixturePath(string directoryName)
    {
        DirectoryInfo directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string path = Path.Combine(directory.FullName, "tests", "data",
                directoryName, "generation.json");
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the pinned Markdown generation fixtures.");
    }

    static string NormalizeName(string value)
    {
        return value.Replace(" ", "_").Replace("(", "")
            .Replace(")", "");
    }
}
