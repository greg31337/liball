namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPublicTextMarkdownApiCompletionTests
{
    [Test]
    public void FactoryExposesEveryTextMarkdownPresetAndRule()
    {
        IHtmlFactory factory = new HtmlFactory();
        HtmlTextOptions text = factory.CreateTextOptions();
        HtmlTextOptions search = factory.CreateSearchTextOptions();
        HtmlMarkdownOptions common = factory.CreateCommonMarkOptions();
        HtmlMarkdownOptions github = factory.CreateGitHubMarkdownOptions();
        HtmlMarkdownOptions readable =
            factory.CreateReadableMarkdownOptions();
        HtmlTextRule include = factory.CreateIncludeTextRule("main");
        HtmlTextRule exclude = factory.CreateExcludeTextRule("nav");
        HtmlTextRule replace = factory.CreateReplaceTextRule(
            ".price", "[price]");
        HtmlMarkdownRule useDefault =
            factory.CreateDefaultMarkdownRule("p");
        HtmlMarkdownRule keep = factory.CreateKeepMarkdownRule(
            "custom-element");
        HtmlMarkdownRule remove = factory.CreateRemoveMarkdownRule(
            ".advertisement");

        Assert.Multiple(() =>
        {
            Assert.That(text.GetImageStyle(),
                Is.EqualTo(HtmlTextImageStyle.AltText));
            Assert.That(search.GetImageStyle(),
                Is.EqualTo(HtmlTextImageStyle.Omit));
            Assert.That(common.GetEnableTables(), Is.False);
            Assert.That(github.GetEnableTables(), Is.True);
            Assert.That(readable.GetLinkStyle(),
                Is.EqualTo(HtmlMarkdownLinkStyle.Reference));
            Assert.That(include.GetAction(),
                Is.EqualTo(HtmlTextRuleAction.Include));
            Assert.That(exclude.GetAction(),
                Is.EqualTo(HtmlTextRuleAction.Exclude));
            Assert.That(replace.GetAction(),
                Is.EqualTo(HtmlTextRuleAction.Replace));
            Assert.That(replace.GetReplacement(), Is.EqualTo("[price]"));
            Assert.That(useDefault.GetAction(),
                Is.EqualTo(HtmlMarkdownRuleAction.Default));
            Assert.That(keep.GetAction(),
                Is.EqualTo(HtmlMarkdownRuleAction.Keep));
            Assert.That(remove.GetAction(),
                Is.EqualTo(HtmlMarkdownRuleAction.Remove));
        });
    }

    [Test]
    public void TextConverterApiSupportsEveryInputAndOptionEntryPoint()
    {
        IHtmlTextConverterApi api = new HtmlTextConverterApi();
        HtmlTextOptions options = new HtmlFactory().CreateTextOptions();
        HtmlDocument document = Html.Parse(
            "<main><p>Alpha <b>beta</b></p></main>");
        HtmlNode paragraph = document.QuerySelector("p");

        HtmlTextResult html = api.ConvertHtml(
            "<p>Alpha <b>beta</b></p>");
        HtmlTextResult configuredHtml = api.ConvertHtmlWithOptions(
            "<p>Alpha <b>beta</b></p>", options,
            CancellationToken.None);
        HtmlTextResult whole = api.ConvertDocument(document);
        HtmlTextResult configuredWhole = api.ConvertDocumentWithOptions(
            document, options, CancellationToken.None);
        HtmlTextResult node = api.ConvertNode(paragraph);
        HtmlTextResult configuredNode = api.ConvertNodeWithOptions(
            paragraph, options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { html, configuredHtml, whole,
                configuredWhole, node, configuredNode }.Select(value =>
                    value.GetText()), Has.All.EqualTo("Alpha beta"));
            Assert.That(new[] { html, configuredHtml, whole,
                configuredWhole, node, configuredNode },
                Has.All.Matches<HtmlTextResult>(value =>
                    value.IsSuccess() && value.IsComplete()
                    && !value.WasTruncated()));
        });
    }

    [Test]
    public void MarkdownConverterApiSupportsEveryInputAndOptionEntryPoint()
    {
        IHtmlMarkdownConverterApi api = new HtmlMarkdownConverterApi();
        HtmlMarkdownOptions options =
            new HtmlFactory().CreateCommonMarkOptions();
        HtmlDocument document = Html.Parse(
            "<main><p>Alpha <strong>beta</strong></p></main>");
        HtmlNode paragraph = document.QuerySelector("p");

        HtmlMarkdownResult html = api.ConvertHtml(
            "<p>Alpha <strong>beta</strong></p>");
        HtmlMarkdownResult configuredHtml = api.ConvertHtmlWithOptions(
            "<p>Alpha <strong>beta</strong></p>", options,
            CancellationToken.None);
        HtmlMarkdownResult whole = api.ConvertDocument(document);
        HtmlMarkdownResult configuredWhole =
            api.ConvertDocumentWithOptions(document, options,
                CancellationToken.None);
        HtmlMarkdownResult node = api.ConvertNode(paragraph);
        HtmlMarkdownResult configuredNode = api.ConvertNodeWithOptions(
            paragraph, options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { html, configuredHtml, whole,
                configuredWhole, node, configuredNode }.Select(value =>
                    value.GetMarkdown()),
                Has.All.EqualTo("Alpha **beta**"));
            Assert.That(new[] { html, configuredHtml, whole,
                configuredWhole, node, configuredNode },
                Has.All.Matches<HtmlMarkdownResult>(value =>
                    value.IsSuccess() && value.IsComplete()
                    && !value.WasTruncated()));
        });
    }

    [Test]
    public void ConverterApisExposeLimitsCancellationAndInvalidArguments()
    {
        IHtmlTextConverterApi textApi = new HtmlTextConverterApi();
        IHtmlMarkdownConverterApi markdownApi =
            new HtmlMarkdownConverterApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(8);
        HtmlTextOptions textOptions = new HtmlTextOptions();
        textOptions.SetLimits(limits);
        textOptions.SetTruncationMarker("[cut]");
        HtmlMarkdownOptions markdownOptions = new HtmlMarkdownOptions();
        markdownOptions.SetLimits(limits);
        markdownOptions.SetTruncationMarker("[cut]");
        HtmlTextResult text = textApi.ConvertHtmlWithOptions(
            "<p>abcdefghijklmnop</p>", textOptions,
            CancellationToken.None);
        HtmlMarkdownResult markdown =
            markdownApi.ConvertHtmlWithOptions(
                "<p>abcdefghijklmnop</p>", markdownOptions,
                CancellationToken.None);
        HtmlNode node = Html.ParseFragment("<p>value</p>", "body")
            .GetFragment();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(text.GetText(), Is.EqualTo("abc[cut]"));
            Assert.That(text.IsPartial(), Is.True);
            Assert.That(text.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.TextConversionLimitExceeded));
            Assert.That(markdown.GetMarkdown(), Is.EqualTo("abc[cut]"));
            Assert.That(markdown.IsPartial(), Is.True);
            Assert.That(markdown.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.MarkdownConversionLimitExceeded));
            Assert.Throws<OperationCanceledException>(() =>
                textApi.ConvertNodeWithOptions(node, textOptions,
                    cancellation.Token));
            Assert.Throws<OperationCanceledException>(() =>
                markdownApi.ConvertNodeWithOptions(node, markdownOptions,
                    cancellation.Token));
            Assert.Throws<ArgumentNullException>(() =>
                textApi.ConvertHtmlWithOptions("value", null,
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                markdownApi.ConvertDocumentWithOptions(null,
                    markdownOptions, CancellationToken.None));
        });
    }
}
