
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPipelineFixtureTests
{
    [Test]
    public void MalformedEmailCompletesAsSafeReadableRichText()
    {
        HtmlProcessingOptions options =
            HtmlProcessingOptions.CreateUntrustedRichText();
        options.SetConvertText(true);

        HtmlProcessingResult result = Html.Process(
            ReadFixture("malformed-email.html"), options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetHtml(), Does.Contain("<table"));
            Assert.That(result.GetHtml(), Does.Contain("Account update"));
            Assert.That(result.GetHtml(), Does.Not.Contain("<style"));
            Assert.That(result.GetHtml(), Does.Not.Contain("style="));
            Assert.That(result.GetText(), Does.Contain(
                "Your April statement is ready"));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Parse),
                Is.EqualTo(HtmlProcessingStageStatus.Succeeded));
            Assert.That(result.GetSanitizerReport().GetTotalCount(),
                Is.GreaterThan(0));
        });
    }

    [Test]
    public void CmsRichTextPreservesSemanticStructuresAcrossOutputs()
    {
        HtmlProcessingOptions options = CreateRichConversionOptions();
        HtmlParseOptions parse = options.GetParseOptions();
        parse.SetBaseUrl("https://cms.example.test/articles/guide");
        options.SetParseOptions(parse);

        HtmlProcessingResult result = HtmlProcessor.Process(
            ReadFixture("cms-rich-text.html"), options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetHtml(), Does.Contain("<figure>"));
            Assert.That(result.GetHtml(), Does.Contain("<table>"));
            Assert.That(result.GetHtml(), Does.Not.Contain("cms-callout"));
            Assert.That(result.GetText(), Does.Contain("Migration guide"));
            Assert.That(result.GetMarkdown(),
                Does.Contain("## Migration guide"));
            Assert.That(result.GetMarkdown(), Does.Contain("| Step"));
        });
    }

    [Test]
    public void InternationalNewsRetainsMetadataAndUnicodeArticleText()
    {
        HtmlProcessingOptions options =
            HtmlProcessingOptions.CreateArticle();
        HtmlParseOptions parse = options.GetParseOptions();
        parse.SetBaseUrl("https://news.example.test/world/index.html");
        options.SetParseOptions(parse);

        HtmlProcessingResult result = HtmlProcessor.Process(
            ReadFixture("international-news.html"), options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetMetadata().GetTitle(),
                Is.EqualTo("世界の科学ニュース"));
            Assert.That(result.GetMetadata().GetLanguage(),
                Is.EqualTo("ja"));
            Assert.That(result.GetMetadata().GetCanonicalUrl(),
                Is.EqualTo(
                    "https://news.example.test/news/science-world"));
            Assert.That(result.GetArticle().GetByline(),
                Is.EqualTo("山田 花子"));
            Assert.That(result.GetText(), Does.Contain("東京の研究チーム"));
            Assert.That(result.GetMarkdown(),
                Does.Contain("# 世界の科学ニュース"));
            Assert.That(result.GetHtml(), Does.Not.Contain("おすすめ記事"));
        });
    }

    [Test]
    public void DocumentationPageBecomesFocusedLlmInput()
    {
        HtmlProcessingResult result = HtmlProcessor.Process(
            ReadFixture("documentation-page.html"),
            HtmlProcessingOptions.CreateLlmIngestion(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetArticle().IsContentSanitized(), Is.True);
            Assert.That(result.GetText(), Does.Contain(
                "Configure the widget client"));
            Assert.That(result.GetText(), Does.Not.Contain(
                "Sponsored integration"));
            Assert.That(result.GetMarkdown(), Does.Contain("```csharp"));
            Assert.That(result.GetMarkdown(),
                Does.Contain("1. Retry temporary failures."));
            Assert.That(result.GetHtml(), Is.Null);
        });
    }

    [Test]
    public void TablesAndCodeSurviveArticleHtmlTextAndMarkdown()
    {
        HtmlProcessingResult result = HtmlProcessor.Process(
            ReadFixture("tables-code.html"),
            HtmlProcessingOptions.CreateArticle(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetHtml(), Does.Contain("<table>"));
            Assert.That(result.GetHtml(), Does.Contain("<pre>"));
            Assert.That(result.GetText(), Does.Contain("Linux"));
            Assert.That(result.GetText(), Does.Contain("dotnet test"));
            Assert.That(result.GetMarkdown(), Does.Contain("| Runtime"));
            Assert.That(result.GetMarkdown(), Does.Contain("```bash"));
            Assert.That(result.GetMarkdown(),
                Does.Contain("dotnet pack --no-build"));
        });
    }

    [Test]
    public void MaliciousFixtureProducesNoActiveRenderableMarkup()
    {
        HtmlProcessingResult result = HtmlProcessor.Process(
            ReadFixture("malicious-markup.html"),
            HtmlProcessingOptions.CreateUntrustedRichText(),
            CancellationToken.None);
        string html = result.GetHtml();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(html, Does.Contain("Visible contribution"));
            Assert.That(html, Does.Not.Contain("<script"));
            Assert.That(html, Does.Not.Contain("<style"));
            Assert.That(html, Does.Not.Contain("javascript"));
            Assert.That(html, Does.Not.Contain("onclick"));
            Assert.That(html, Does.Not.Contain("onerror"));
            Assert.That(html, Does.Not.Contain("srcdoc"));
            Assert.That(html, Does.Not.Contain("<iframe"));
            Assert.That(html, Does.Not.Contain("<form"));
            Assert.That(html, Does.Not.Contain("<svg"));
            Assert.That(html, Does.Not.Contain("<math"));
            Assert.That(result.GetSanitizerReport().GetTotalCount(),
                Is.GreaterThan(8));
        });
    }

    [Test]
    public void TruncationFixtureReturnsBoundedMarkedTextAndStageStatus()
    {
        HtmlProcessingOptions options =
            HtmlProcessingOptions.CreateSearchIndex();
        HtmlLimits limits = options.GetLimits();
        limits.SetMaximumOutputCharacters(90);
        options.SetLimits(limits);

        HtmlProcessingResult result = HtmlProcessor.Process(
            ReadFixture("truncation.html"), options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetText().Length,
                Is.LessThanOrEqualTo(90));
            Assert.That(result.GetText(), Does.EndWith("…"));
            Assert.That(result.GetStageStatus(HtmlProcessingStage.Text),
                Is.EqualTo(HtmlProcessingStageStatus.Incomplete));
            Assert.That(result.GetStageDiagnostics(HtmlProcessingStage.Text)
                .Select(value => value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.TextConversionLimitExceeded));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.PipelineLimitExceeded));
        });
    }

    static HtmlProcessingOptions CreateRichConversionOptions()
    {
        HtmlProcessingOptions options =
            HtmlProcessingOptions.CreateUntrustedRichText();
        options.SetConvertText(true);
        options.SetConvertMarkdown(true);
        HtmlMarkdownOptions markdown =
            HtmlMarkdownOptions.CreateGitHubFlavored();
        options.SetMarkdownOptions(markdown);
        return options;
    }

    static string ReadFixture(string fileName)
    {
        DirectoryInfo directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string path = Path.Combine(directory.FullName, "tests", "data",
                "pipeline", fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not find the pipeline fixture.", fileName);
    }
}
