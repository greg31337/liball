
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownConverterTests
{
    [Test]
    public void StringConversionParsesCorrectedHtmlAndReturnsReport()
    {
        HtmlMarkdownResult result = HtmlMarkdownConverter.Convert(
            "<title>Ignored</title><main><h1>Heading</h1>"
            + "<p>First<p>Second</main>");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(result.GetMarkdown(), Is.EqualTo(
                "# Heading\n\nFirst\n\nSecond"));
            Assert.That(result.GetDiagnosticCount(),
                Is.EqualTo(result.GetDiagnostics().Length));
        });
    }

    [Test]
    public void DocumentFragmentAndElementConversionsHonorTheirScope()
    {
        HtmlDocument document = Html.Parse(
            "<main><h2>Title</h2><p>First</p>"
            + "<p>Second <strong>item</strong></p></main>");
        HtmlNode fragment = Html.ParseFragment(
            "<p>Fragment one</p><p>Fragment two</p>", "body")
            .GetFragment();
        HtmlNode secondParagraph = document.QuerySelectorAll("p")[1];

        HtmlMarkdownResult documentResult =
            HtmlMarkdownConverter.Convert(document);
        HtmlMarkdownResult fragmentResult =
            HtmlMarkdownConverter.Convert(fragment);
        HtmlMarkdownResult elementResult =
            HtmlMarkdownConverter.Convert(secondParagraph);

        Assert.Multiple(() =>
        {
            Assert.That(documentResult.GetMarkdown(), Is.EqualTo(
                "## Title\n\nFirst\n\nSecond **item**"));
            Assert.That(fragmentResult.GetMarkdown(), Is.EqualTo(
                "Fragment one\n\nFragment two"));
            Assert.That(elementResult.GetMarkdown(),
                Is.EqualTo("Second **item**"));
            Assert.That(documentResult.IsComplete(), Is.True);
            Assert.That(fragmentResult.IsComplete(), Is.True);
            Assert.That(elementResult.IsComplete(), Is.True);
        });
    }

    [Test]
    public void CompiledOptionsAreReusableIndependentBatchSnapshots()
    {
        HtmlMarkdownOptions mutable =
            HtmlMarkdownOptions.CreateCommonMark();
        mutable.SetHeadingStyle(HtmlMarkdownHeadingStyle.Setext);
        mutable.SetNewLine("\r\n");
        HtmlCompiledMarkdownOptions compiled = mutable.Compile();
        mutable.SetHeadingStyle(HtmlMarkdownHeadingStyle.Atx);
        mutable.SetNewLine("\n");
        HtmlDocument document = Html.Parse("<main><h1>Title</h1></main>");

        HtmlMarkdownResult first = HtmlMarkdownConverter.Convert(document,
            compiled, CancellationToken.None);
        HtmlMarkdownResult second = HtmlMarkdownConverter.Convert(
            document.GetBody(), compiled, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetMarkdown(), Is.EqualTo("Title\r\n==="));
            Assert.That(second.GetMarkdown(),
                Is.EqualTo(first.GetMarkdown()));
            Assert.That(compiled.GetHeadingStyle(),
                Is.EqualTo(HtmlMarkdownHeadingStyle.Setext));
            Assert.That(compiled.GetNewLine(), Is.EqualTo("\r\n"));
        });
    }

    [Test]
    public void OutputLimitReturnsBoundedGraphemeSafeMarkdownAndDiagnostic()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(8);
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetLimits(limits);
        options.SetTruncationMarker("[cut]");

        HtmlMarkdownResult result = HtmlMarkdownConverter.Convert(
            "<p>abcdefghijklmnop</p>", options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetMarkdown(), Is.EqualTo("abc[cut]"));
            Assert.That(result.GetMarkdown().Length,
                Is.LessThanOrEqualTo(8));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.MarkdownConversionLimitExceeded));
        });

        limits.SetMaximumOutputCharacters(5);
        options.SetLimits(limits);
        options.SetTruncationMarker("🙂🙂🙂");
        result = HtmlMarkdownConverter.Convert("<p>long output</p>",
            options, CancellationToken.None);
        Assert.That(result.GetMarkdown(), Is.EqualTo("🙂🙂"));
    }

    [Test]
    public void VisitDepthAndTableLimitsReturnStablePartialReports()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<section><p>one <strong>two</strong></p>"
            + "<table><tr><td>a</td><td>b</td></tr></table></section>",
            "body").GetFragment();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(4);
        limits.SetMaximumDepth(2);
        limits.SetMaximumTableCells(1);
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateGitHubFlavored();
        options.SetLimits(limits);
        options.SetTruncationMarker("~");

        HtmlMarkdownResult first = HtmlMarkdownConverter.Convert(fragment,
            options, CancellationToken.None);
        HtmlMarkdownResult second = HtmlMarkdownConverter.Convert(fragment,
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsSuccess(), Is.True);
            Assert.That(first.IsComplete(), Is.False);
            Assert.That(first.WasTruncated(), Is.True);
            Assert.That(first.GetMarkdown(), Is.EqualTo("~"));
            Assert.That(second.GetMarkdown(),
                Is.EqualTo(first.GetMarkdown()));
            Assert.That(first.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.MarkdownConversionLimitExceeded));
        });
    }

    [Test]
    public void StringParseTruncationPropagatesToTheMarkdownReport()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(12);
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetLimits(limits);
        options.SetTruncationMarker("[more]");

        HtmlMarkdownResult result = HtmlMarkdownConverter.Convert(
            "<p>first second third</p>", options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetMarkdown(), Does.EndWith("[more]"));
            Assert.That(result.GetDiagnostics(), Is.Not.Empty);
        });
    }

    [Test]
    public void UnknownElementErrorBecomesAConversionDiagnostic()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<p>Before</p><x-panel>value</x-panel>", "body")
            .GetFragment();
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.Error);

        HtmlMarkdownResult result = HtmlMarkdownConverter.Convert(fragment,
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(result.GetMarkdown(), Is.Empty);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(result.GetDiagnostics()[0].GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.MarkdownUnknownElement));
            Assert.That(result.GetDiagnostics()[0].GetMessage(),
                Does.Contain("x-panel"));
        });
    }

    [Test]
    public void HtmlFacadeUsesTheMarkdownConverter()
    {
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetHeadingStyle(HtmlMarkdownHeadingStyle.Setext);

        string markdown = Html.ToMarkdown("<h1>Title</h1>");
        HtmlMarkdownResult configured = Html.ToMarkdown(
            "<h1>Title</h1>", options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(markdown, Is.EqualTo("# Title"));
            Assert.That(configured.GetMarkdown(),
                Is.EqualTo("Title\n==="));
        });
    }

    [Test]
    public void ResultCopiesDiagnosticsAndConversionValidatesArguments()
    {
        HtmlDiagnostic[] diagnostics =
        [
            HtmlDiagnostic.Create(HtmlDiagnosticCode.OperationIncomplete,
                HtmlDiagnosticSeverity.Warning, "partial",
                HtmlSourceSpan.CreateUnknown())
        ];
        HtmlMarkdownResult result = new HtmlMarkdownResult(true, false,
            true, "partial", diagnostics, 1, 0);
        diagnostics[0] = null;
        HtmlDiagnostic[] snapshot = result.GetDiagnostics();
        snapshot[0] = null;
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlCompiledMarkdownOptions compiled = options.Compile();
        HtmlNode fragment = Html.ParseFragment("text", "body")
            .GetFragment();
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDiagnostics()[0], Is.Not.Null);
            Assert.Throws<ArgumentException>(() =>
                new HtmlMarkdownResult(true, true, true, "",
                    Array.Empty<HtmlDiagnostic>(), 0, 0));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlMarkdownConverter.Convert("text", compiled,
                    source.Token));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlMarkdownConverter.Convert(fragment, compiled,
                    source.Token));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlMarkdownConverter.Convert((string)null, options,
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlMarkdownConverter.Convert((HtmlDocument)null, options,
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlMarkdownConverter.Convert((HtmlNode)null, options,
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlMarkdownConverter.Convert(fragment,
                    (HtmlMarkdownOptions)null, CancellationToken.None));
        });
    }
}
