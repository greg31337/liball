using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class GfmSpecFixtureLoaderTests
{
    [Test]
    public void Loader_ExtractsNumberSectionColumnsLinesSpacesAndTabs()
    {
        string fixture =
            "<html>\n"
            + "<h2 id=\"tables\"><span class=\"number\">4.10</span>Tables (extension)</h2>\n"
            + "<div class=\"example\" id=\"example-198\">\n"
            + "<div class=\"column\"><pre><code class=\"language-markdown\">→a<span class=\"space\"> </span>&amp;\n</code></pre></div>\n"
            + "<div class=\"column\"><pre><code class=\"language-html\">&lt;p&gt;→a<span class=\"space\"> </span>&amp;amp;&lt;/p&gt;\n</code></pre></div>\n"
            + "</div>\n"
            + "</html>\n";

        GfmSpecExample[] examples = GfmSpecFixtureLoader.LoadFromString(fixture);

        Assert.That(examples.Length, Is.EqualTo(1));
        Assert.That(examples[0].ExampleNumber, Is.EqualTo(198));
        Assert.That(examples[0].Section, Is.EqualTo("Tables (extension)"));
        Assert.That(examples[0].Markdown, Is.EqualTo("\ta &\n"));
        Assert.That(examples[0].Html, Is.EqualTo("<p>\ta &amp;</p>\n"));
        Assert.That(examples[0].StartLine, Is.EqualTo(3));
        Assert.That(examples[0].EndLine, Is.EqualTo(7));
        Assert.That(examples[0].ToString(), Is.EqualTo("Example 198 (Tables (extension))"));
    }

    [Test]
    public void Loader_LoadsFromFileAndPreservesExampleOrder()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gfm-spec-loader-test.html");
        File.WriteAllText(
            path,
            CreateExampleHtml(7, "Tables (extension)", "one\n", "<p>one</p>\n")
            + CreateExampleHtml(9, "Autolinks (extension)", "www.example.com\n", "<p>link</p>\n"));

        GfmSpecExample[] examples = GfmSpecFixtureLoader.LoadFromFile(path);

        Assert.That(examples.Length, Is.EqualTo(2));
        Assert.That(examples[0].ExampleNumber, Is.EqualTo(7));
        Assert.That(examples[1].ExampleNumber, Is.EqualTo(9));
        Assert.That(examples[1].Section, Is.EqualTo("Autolinks (extension)"));
    }

    [Test]
    public void Loader_RejectsMalformedHtmlFixtureStructures()
    {
        string noExamples = "<html><h2>Section</h2></html>";
        string missingMarkdown =
            "<html><h2>Section</h2><div class=\"example\" id=\"example-1\"></div></html>";
        string invalidNumber =
            "<html><h2>Section</h2><div class=\"example\" id=\"example-x\"></div></html>";

        Assert.That(() => GfmSpecFixtureLoader.LoadFromString(noExamples), Throws.TypeOf<FormatException>());
        Assert.That(() => GfmSpecFixtureLoader.LoadFromString(missingMarkdown), Throws.TypeOf<FormatException>());
        Assert.That(() => GfmSpecFixtureLoader.LoadFromString(invalidNumber), Throws.TypeOf<FormatException>());
    }

    [Test]
    public void Loader_RejectsInvalidArgumentsAndRanges()
    {
        Assert.That(() => new GfmSpecExample(0, "", "", "", 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new GfmSpecExample(1, null!, "", "", 1, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new GfmSpecExample(1, "", null!, "", 1, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new GfmSpecExample(1, "", "", null!, 1, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new GfmSpecExample(1, "", "", "", 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new GfmSpecExample(1, "", "", "", 2, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => GfmSpecFixtureLoader.LoadFromFile(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmSpecFixtureLoader.LoadFromString(null!), Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ExtensionClassification_RecognizesExactlyTheFiveGfmSections()
    {
        Assert.That(GfmSpecFixtureLoader.IsExtensionSection("Tables (extension)"), Is.True);
        Assert.That(GfmSpecFixtureLoader.IsExtensionSection("Task list items (extension)"), Is.True);
        Assert.That(GfmSpecFixtureLoader.IsExtensionSection("Strikethrough (extension)"), Is.True);
        Assert.That(GfmSpecFixtureLoader.IsExtensionSection("Autolinks (extension)"), Is.True);
        Assert.That(GfmSpecFixtureLoader.IsExtensionSection("Disallowed Raw HTML (extension)"), Is.True);
        Assert.That(GfmSpecFixtureLoader.IsExtensionSection("Links"), Is.False);
    }

    public static string CreateExampleHtml(int number, string section, string markdown, string html)
    {
        return "<html><h2 id=\"section\"><span class=\"number\">1</span>"
            + section
            + "</h2><div class=\"example\" id=\"example-"
            + number.ToString()
            + "\"><pre><code class=\"language-markdown\">"
            + System.Net.WebUtility.HtmlEncode(markdown)
            + "</code></pre><pre><code class=\"language-html\">"
            + System.Net.WebUtility.HtmlEncode(html)
            + "</code></pre></div></html>";
    }
}
