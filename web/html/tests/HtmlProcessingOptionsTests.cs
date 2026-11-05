
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlProcessingOptionsTests
{
    const string HtmlNamespace = "http://www.w3.org/1999/xhtml";

    [Test]
    public void PresetsExposePurposeSpecificSafeStageProfiles()
    {
        HtmlProcessingOptions richText =
            HtmlProcessingOptions.CreateUntrustedRichText();
        HtmlProcessingOptions article =
            HtmlProcessingOptions.CreateArticle();
        HtmlProcessingOptions search =
            HtmlProcessingOptions.CreateSearchIndex();
        HtmlProcessingOptions llm =
            HtmlProcessingOptions.CreateLlmIngestion();

        Assert.Multiple(() =>
        {
            Assert.That(richText.GetExtractMetadata(), Is.False);
            Assert.That(richText.GetExtractArticle(), Is.False);
            Assert.That(richText.GetSanitize(), Is.True);
            Assert.That(richText.GetSerializeHtml(), Is.True);
            Assert.That(richText.GetConvertText(), Is.False);
            Assert.That(richText.GetConvertMarkdown(), Is.False);
            Assert.That(richText.GetSanitizerPolicy().IsElementAllowedInNamespace(
                HtmlNamespace, "p"), Is.True);
            Assert.That(richText.GetSanitizerPolicy().IsElementAllowedInNamespace(
                HtmlNamespace, "script"), Is.False);

            Assert.That(article.GetExtractMetadata(), Is.True);
            Assert.That(article.GetExtractArticle(), Is.True);
            Assert.That(article.GetSanitize(), Is.True);
            Assert.That(article.GetSerializeHtml(), Is.True);
            Assert.That(article.GetConvertText(), Is.True);
            Assert.That(article.GetConvertMarkdown(), Is.True);
            Assert.That(article.GetMarkdownOptions().GetLinkStyle(),
                Is.EqualTo(HtmlMarkdownLinkStyle.Reference));
            Assert.That(article.GetMarkdownOptions().GetEnableTables(),
                Is.True);

            Assert.That(search.GetExtractMetadata(), Is.True);
            Assert.That(search.GetExtractArticle(), Is.False);
            Assert.That(search.GetSanitize(), Is.True);
            Assert.That(search.GetSerializeHtml(), Is.False);
            Assert.That(search.GetConvertText(), Is.True);
            Assert.That(search.GetConvertMarkdown(), Is.False);
            Assert.That(search.GetTextOptions().GetImageStyle(),
                Is.EqualTo(HtmlTextImageStyle.Omit));
            Assert.That(search.GetTextOptions().GetRules(),
                Has.Length.EqualTo(2));

            Assert.That(llm.GetExtractMetadata(), Is.True);
            Assert.That(llm.GetExtractArticle(), Is.True);
            Assert.That(llm.GetSanitize(), Is.True);
            Assert.That(llm.GetSerializeHtml(), Is.False);
            Assert.That(llm.GetConvertText(), Is.True);
            Assert.That(llm.GetConvertMarkdown(), Is.True);
            Assert.That(llm.GetTextOptions().GetLinkStyle(),
                Is.EqualTo(HtmlTextLinkStyle.TextAndUrl));
            Assert.That(llm.GetTextOptions().GetImageStyle(),
                Is.EqualTo(HtmlTextImageStyle.AltTextAndUrl));
            Assert.That(llm.GetMarkdownOptions().GetEnableFootnotes(),
                Is.True);
        });
    }

    [Test]
    public void SettersAndGettersDefensivelyCopyStageConfigurations()
    {
        HtmlProcessingOptions options =
            HtmlProcessingOptions.CreateUntrustedRichText();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(1234);
        options.SetLimits(limits);

        HtmlParseOptions parse = HtmlParseOptions.CreateDefault();
        parse.SetBaseUrl("https://example.test/original/");
        HtmlMetadataOptions metadataOptions = new HtmlMetadataOptions();
        metadataOptions.SetIncludeSource(HtmlMetadataSource.JsonLd, false);
        HtmlSerializerOptions serializerOptions =
            new HtmlSerializerOptions();
        serializerOptions.SetQuoteMode(HtmlSerializerQuoteMode.Single);
        HtmlTextOptions textOptions = HtmlTextOptions.CreateDefault();
        textOptions.SetWrapColumn(72);
        HtmlMarkdownOptions markdownOptions =
            HtmlMarkdownOptions.CreateCommonMark();
        markdownOptions.SetHeadingStyle(HtmlMarkdownHeadingStyle.Setext);
        HtmlArticleOptions articleOptions =
            HtmlArticleOptions.CreateDefault();
        articleOptions.SetScoreThreshold(42);
        HtmlCompiledSanitizerPolicy sanitizer =
            HtmlSanitizerPolicy.CreateBasicFormatting().Compile();

        options.SetParseOptions(parse);
        options.SetMetadataOptions(metadataOptions);
        options.SetSerializerOptions(serializerOptions);
        options.SetTextOptions(textOptions);
        options.SetMarkdownOptions(markdownOptions);
        options.SetArticleOptions(articleOptions);
        options.SetSanitizerPolicy(sanitizer);
        parse.SetBaseUrl("https://changed.test/");
        metadataOptions.SetIncludeSource(HtmlMetadataSource.JsonLd, true);
        serializerOptions.SetQuoteMode(HtmlSerializerQuoteMode.Double);
        textOptions.SetWrapColumn(10);
        markdownOptions.SetHeadingStyle(HtmlMarkdownHeadingStyle.Atx);
        articleOptions.SetScoreThreshold(1);
        limits.SetMaximumVisits(9999);

        HtmlParseOptions exposedParse = options.GetParseOptions();
        HtmlMetadataOptions exposedMetadata = options.GetMetadataOptions();
        HtmlSerializerOptions exposedSerializer =
            options.GetSerializerOptions();
        HtmlTextOptions exposedText = options.GetTextOptions();
        HtmlMarkdownOptions exposedMarkdown = options.GetMarkdownOptions();
        HtmlArticleOptions exposedArticle = options.GetArticleOptions();
        HtmlLimits exposedLimits = options.GetLimits();
        exposedParse.SetBaseUrl("https://exposed.test/");
        exposedMetadata.SetIncludeSource(HtmlMetadataSource.JsonLd, true);
        exposedSerializer.SetQuoteMode(HtmlSerializerQuoteMode.Double);
        exposedText.SetWrapColumn(20);
        exposedMarkdown.SetHeadingStyle(HtmlMarkdownHeadingStyle.Atx);
        exposedArticle.SetScoreThreshold(2);
        exposedLimits.SetMaximumVisits(8888);

        Assert.Multiple(() =>
        {
            Assert.That(options.GetParseOptions().GetBaseUrl(),
                Is.EqualTo("https://example.test/original/"));
            Assert.That(options.GetMetadataOptions().GetIncludeSource(
                HtmlMetadataSource.JsonLd), Is.False);
            Assert.That(options.GetSerializerOptions().GetQuoteMode(),
                Is.EqualTo(HtmlSerializerQuoteMode.Single));
            Assert.That(options.GetTextOptions().GetWrapColumn(),
                Is.EqualTo(72));
            Assert.That(options.GetMarkdownOptions().GetHeadingStyle(),
                Is.EqualTo(HtmlMarkdownHeadingStyle.Setext));
            Assert.That(options.GetArticleOptions().GetScoreThreshold(),
                Is.EqualTo(42));
            Assert.That(options.GetSanitizerPolicy().IsElementAllowedInNamespace(
                HtmlNamespace, "mark"), Is.True);
            Assert.That(options.GetSanitizerPolicy().IsElementAllowedInNamespace(
                HtmlNamespace, "a"), Is.False);
            Assert.That(options.GetLimits().GetMaximumVisits(),
                Is.EqualTo(1234));
            Assert.That(options.GetParseOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(1234));
            Assert.That(options.GetMetadataOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(1234));
            Assert.That(options.GetSerializerOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(1234));
            Assert.That(options.GetTextOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(1234));
            Assert.That(options.GetMarkdownOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(1234));
            Assert.That(options.GetArticleOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(1234));
        });
    }

    [Test]
    public void CopyAndCompileCreateIndependentFrozenSnapshots()
    {
        HtmlProcessingOptions original =
            HtmlProcessingOptions.CreateArticle();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(4321);
        original.SetLimits(limits);
        HtmlParseOptions parse = original.GetParseOptions();
        parse.SetBaseUrl("https://example.test/article/");
        original.SetParseOptions(parse);
        HtmlTextOptions textOptions = original.GetTextOptions();
        textOptions.SetWrapColumn(80);
        original.SetTextOptions(textOptions);
        HtmlMetadataOptions metadataOptions = original.GetMetadataOptions();
        metadataOptions.SetIncludeSource(HtmlMetadataSource.Document, false);
        original.SetMetadataOptions(metadataOptions);
        HtmlSerializerOptions serializerOptions =
            original.GetSerializerOptions();
        serializerOptions.SetQuoteMode(HtmlSerializerQuoteMode.Single);
        original.SetSerializerOptions(serializerOptions);

        HtmlProcessingOptions copy = original.Copy();
        HtmlCompiledProcessingOptions compiled = original.Compile();
        original.SetExtractArticle(false);
        original.SetSerializeHtml(false);
        parse.SetBaseUrl("https://changed.test/");
        original.SetParseOptions(parse);
        textOptions.SetWrapColumn(20);
        original.SetTextOptions(textOptions);
        metadataOptions.SetIncludeSource(HtmlMetadataSource.Document, true);
        original.SetMetadataOptions(metadataOptions);
        serializerOptions.SetQuoteMode(HtmlSerializerQuoteMode.Double);
        original.SetSerializerOptions(serializerOptions);
        HtmlLimits changedLimits = original.GetLimits();
        changedLimits.SetMaximumVisits(9000);
        original.SetLimits(changedLimits);
        copy.SetConvertMarkdown(false);

        HtmlParseOptions exposedParse = compiled.GetParseOptions();
        exposedParse.SetBaseUrl("https://exposed.test/");
        HtmlArticleOptions exposedArticle = compiled.GetArticleOptions();
        exposedArticle.SetScoreThreshold(0);
        HtmlLimits exposedLimits = compiled.GetLimits();
        exposedLimits.SetMaximumVisits(1);
        HtmlMetadataOptions exposedMetadata =
            compiled.GetMetadataOptions();
        exposedMetadata.SetIncludeSource(HtmlMetadataSource.Document, true);
        HtmlSerializerOptions exposedSerializer =
            compiled.GetSerializerOptions();
        exposedSerializer.SetQuoteMode(HtmlSerializerQuoteMode.Double);

        Assert.Multiple(() =>
        {
            Assert.That(copy.GetExtractArticle(), Is.True);
            Assert.That(copy.GetConvertMarkdown(), Is.False);
            Assert.That(copy.GetTextOptions().GetWrapColumn(),
                Is.EqualTo(80));
            Assert.That(compiled.GetExtractArticle(), Is.True);
            Assert.That(compiled.GetSerializeHtml(), Is.True);
            Assert.That(compiled.GetConvertMarkdown(), Is.True);
            Assert.That(compiled.GetParseOptions().GetBaseUrl(),
                Is.EqualTo("https://example.test/article/"));
            Assert.That(compiled.GetMetadataOptions().GetIncludeSource(
                HtmlMetadataSource.Document), Is.False);
            Assert.That(compiled.GetSerializerOptions().GetQuoteMode(),
                Is.EqualTo(HtmlSerializerQuoteMode.Single));
            Assert.That(compiled.GetTextOptions().GetWrapColumn(),
                Is.EqualTo(80));
            Assert.That(compiled.GetArticleOptions().GetScoreThreshold(),
                Is.EqualTo(20));
            Assert.That(compiled.GetLimits().GetMaximumVisits(),
                Is.EqualTo(4321));
            Assert.That(compiled.GetParseOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(4321));
            Assert.That(compiled.GetMetadataOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(4321));
            Assert.That(compiled.GetSerializerOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(4321));
            Assert.That(compiled.GetTextOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(4321));
            Assert.That(compiled.GetMarkdownOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(4321));
            Assert.That(compiled.GetArticleOptions().GetLimits()
                .GetMaximumVisits(), Is.EqualTo(4321));
        });
    }

    [Test]
    public void InvalidOrUnsafeConfigurationsAreRejected()
    {
        HtmlProcessingOptions options =
            HtmlProcessingOptions.CreateUntrustedRichText();
        options.SetSanitize(false);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                options.Validate());
            Assert.Throws<InvalidOperationException>(() =>
                options.Compile());
            Assert.Throws<ArgumentNullException>(() =>
                options.SetParseOptions(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetMetadataOptions(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetSanitizerPolicy(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetSerializerOptions(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetTextOptions(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetMarkdownOptions(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetArticleOptions(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetLimits(null));
        });

        options.SetSerializeHtml(false);

        Assert.That(options.Compile(), Is.Not.Null);
    }
}
