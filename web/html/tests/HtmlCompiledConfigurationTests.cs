using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlCompiledConfigurationTests
{
    [Test]
    public void ParseConfigurationIsImmutableAndReusable()
    {
        HtmlParseOptions options = new HtmlParseOptions();
        options.SetResponseUrl("https://example.test/original");
        options.SetBaseUrl("https://example.test/base/");
        options.SetContentLanguage("en-US");
        options.SetEncoding(Encoding.UTF8);
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(4321);
        options.SetLimits(limits);
        HtmlCompiledParseOptions compiled = options.Compile();
        options.SetResponseUrl("https://changed.test/");
        options.SetBaseUrl("https://changed.test/base/");
        options.SetContentLanguage("fr");
        options.SetEncoding(Encoding.Unicode);
        limits.SetMaximumVisits(1);
        HtmlLimits exposedLimits = compiled.GetLimits();
        exposedLimits.SetMaximumVisits(2);

        HtmlApi api = new HtmlApi();
        Task<HtmlParseResult>[] tasks = new Task<HtmlParseResult>[12];
        int index = 0;
        while (index < tasks.Length)
        {
            tasks[index] = Task.Run(() => api.ParseBytesWithOptions(
                Encoding.UTF8.GetBytes("<p>parallel</p>"), compiled,
                CancellationToken.None));
            index = index + 1;
        }

        HtmlParseResult[] results = Task.WhenAll(tasks).GetAwaiter()
            .GetResult();
        HtmlParser parser = new HtmlParser(compiled);
        parser.Feed("<p>incremental</p>".AsSpan(), true);
        HtmlParseResult incremental = parser.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Compile(), Is.SameAs(compiled));
            Assert.That(compiled.GetResponseUrl(),
                Is.EqualTo("https://example.test/original"));
            Assert.That(compiled.GetBaseUrl(),
                Is.EqualTo("https://example.test/base/"));
            Assert.That(compiled.GetContentLanguage(), Is.EqualTo("en-US"));
            Assert.That(compiled.GetEncoding().WebName,
                Is.EqualTo(Encoding.UTF8.WebName));
            Assert.That(compiled.GetLimits().GetMaximumVisits(),
                Is.EqualTo(4321));
            Assert.That(results.All(value => value.IsSuccess()), Is.True);
            Assert.That(results.All(value => value.GetDocument()
                .GetResponseUrl() == "https://example.test/original"),
                Is.True);
            Assert.That(incremental.IsSuccess(), Is.True);
            Assert.That(incremental.GetDocument().QuerySelector("p")
                .GetTextContent(), Is.EqualTo("incremental"));
        });
    }

    [Test]
    public void SerializerConfigurationIsImmutableAndReusable()
    {
        HtmlDocument document = new HtmlApi().ParseString(
            "<p title='résumé'>café</p>");
        HtmlNode paragraph = document.QuerySelector("p");
        document.Freeze();
        HtmlSerializerOptions options = new HtmlSerializerOptions();
        options.SetQuoteMode(HtmlSerializerQuoteMode.Single);
        options.SetAsciiOnly(true);
        HtmlCompiledSerializerOptions compiled = options.Compile();
        options.SetQuoteMode(HtmlSerializerQuoteMode.Double);
        options.SetAsciiOnly(false);
        HtmlLimits exposedLimits = compiled.GetLimits();
        exposedLimits.SetMaximumOutputCharacters(1);
        HtmlSerializerApi api = new HtmlSerializerApi();
        Task<HtmlSerializationResult>[] tasks =
            new Task<HtmlSerializationResult>[12];
        int index = 0;
        while (index < tasks.Length)
        {
            tasks[index] = Task.Run(() => api.SerializeSubtree(paragraph,
                compiled, CancellationToken.None));
            index = index + 1;
        }

        HtmlSerializationResult[] results = Task.WhenAll(tasks)
            .GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Compile(), Is.SameAs(compiled));
            Assert.That(compiled.GetQuoteMode(),
                Is.EqualTo(HtmlSerializerQuoteMode.Single));
            Assert.That(compiled.GetAsciiOnly(), Is.True);
            Assert.That(results.All(value => value.IsComplete()), Is.True);
            Assert.That(results.Select(value => value.GetString()),
                Is.All.EqualTo(
                    "<p title='r&eacute;sum&eacute;'>caf&eacute;</p>"));
        });
    }

    [Test]
    public void ArticleConfigurationIsImmutableAndReusable()
    {
        string prose = "This article contains enough meaningful prose, "
            + "punctuation, detail, and context for readable extraction. ";
        HtmlDocument document = new HtmlApi().ParseString(
            "<article><h1>Compiled article</h1><p>" + prose + prose
            + prose + prose + "</p></article>");
        document.Freeze();
        HtmlArticleOptions options = new HtmlArticleOptions();
        options.SetMinimumContentCharacters(20);
        options.SetMinimumCandidateCharacters(10);
        options.SetScoreThreshold(1);
        options.SetProduceText(true);
        HtmlCompiledArticleOptions compiled = options.Compile();
        options.SetMinimumContentCharacters(100000);
        options.SetScoreThreshold(100000);
        options.SetProduceText(false);
        HtmlArticleExtractorApi api = new HtmlArticleExtractorApi();
        Task<HtmlArticleResult>[] tasks = new Task<HtmlArticleResult>[8];
        int index = 0;
        while (index < tasks.Length)
        {
            tasks[index] = Task.Run(() => api.ExtractWithCancellation(
                document, compiled, CancellationToken.None));
            index = index + 1;
        }

        HtmlArticleResult[] results = Task.WhenAll(tasks).GetAwaiter()
            .GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Compile(), Is.SameAs(compiled));
            Assert.That(compiled.GetMinimumContentCharacters(),
                Is.EqualTo(20));
            Assert.That(compiled.GetScoreThreshold(), Is.EqualTo(1));
            Assert.That(compiled.GetProduceText(), Is.True);
            Assert.That(results.All(value => value.IsSuccess()), Is.True);
            Assert.That(results.All(value => value.GetArticle().GetText()
                .Contains("meaningful prose")), Is.True);
        });
    }

    [Test]
    public void ProcessingConfigurationIsImmutableAndReusable()
    {
        HtmlFactory factory = new HtmlFactory();
        HtmlProcessingOptions options =
            factory.CreateSearchProcessingOptions();
        HtmlParseOptions parseOptions = options.GetParseOptions();
        parseOptions.SetResponseUrl("https://example.test/source");
        options.SetParseOptions(parseOptions);
        HtmlCompiledProcessingOptions compiled = options.Compile();
        options.SetConvertText(false);
        parseOptions.SetResponseUrl("https://changed.test/");
        options.SetParseOptions(parseOptions);
        HtmlParseOptions exposedParse = compiled.GetParseOptions();
        exposedParse.SetResponseUrl("https://exposed.test/");
        HtmlLimits exposedLimits = compiled.GetLimits();
        exposedLimits.SetMaximumVisits(1);
        HtmlProcessorApi api = new HtmlProcessorApi();
        Task<HtmlProcessingResult>[] tasks =
            new Task<HtmlProcessingResult>[12];
        int index = 0;
        while (index < tasks.Length)
        {
            tasks[index] = Task.Run(() => api.ProcessString(
                "<title>Compiled</title><main><p>parallel text</p></main>",
                compiled, CancellationToken.None));
            index = index + 1;
        }

        HtmlProcessingResult[] results = Task.WhenAll(tasks).GetAwaiter()
            .GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.Compile(), Is.SameAs(compiled));
            Assert.That(compiled.GetConvertText(), Is.True);
            Assert.That(compiled.GetParseOptions().GetResponseUrl(),
                Is.EqualTo("https://example.test/source"));
            Assert.That(results.All(value => value.IsSuccess()), Is.True);
            Assert.That(results.Select(value => value.GetText()),
                Is.All.EqualTo("parallel text"));
            Assert.That(results.All(value => value.GetDocument()
                .GetResponseUrl() == "https://example.test/source"),
                Is.True);
        });
    }
}
