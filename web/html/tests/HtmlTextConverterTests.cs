
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTextConverterTests
{
    [Test]
    public void StringConversionParsesCorrectedHtmlAndReturnsStatus()
    {
        HtmlTextResult result = HtmlTextConverter.Convert(
            "<title>Ignored</title><main><h1>Heading</h1>"
            + "<p>First<p>Second</main>");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.WasTruncated(), Is.False);
            Assert.That(result.GetText(),
                Is.EqualTo("Heading\n\nFirst\n\nSecond"));
        });
    }

    [Test]
    public void DocumentFragmentAndElementConversionsHonorTheirScope()
    {
        HtmlDocument document = Html.Parse(
            "<main><p>First</p><p>Second <b>item</b></p></main>");
        HtmlNode fragment = Html.ParseFragment(
            "<p>Fragment one</p><p>Fragment two</p>", "body")
            .GetFragment();
        HtmlNode secondParagraph = document.QuerySelectorAll("p")[1];

        HtmlTextResult documentResult = HtmlTextConverter.Convert(document);
        HtmlTextResult fragmentResult = HtmlTextConverter.Convert(fragment);
        HtmlTextResult elementResult =
            HtmlTextConverter.Convert(secondParagraph);

        Assert.Multiple(() =>
        {
            Assert.That(documentResult.GetText(),
                Is.EqualTo("First\n\nSecond item"));
            Assert.That(fragmentResult.GetText(),
                Is.EqualTo("Fragment one\n\nFragment two"));
            Assert.That(elementResult.GetText(), Is.EqualTo("Second item"));
            Assert.That(documentResult.IsComplete(), Is.True);
            Assert.That(fragmentResult.IsComplete(), Is.True);
            Assert.That(elementResult.IsComplete(), Is.True);
        });
    }

    [Test]
    public void CompiledOptionsCanBeReusedAsAnIndependentBatchSnapshot()
    {
        HtmlTextOptions mutable = HtmlTextOptions.CreateDefault();
        mutable.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        mutable.SetWrapColumn(18);
        HtmlCompiledTextOptions compiled = mutable.Compile();
        mutable.SetLinkStyle(HtmlTextLinkStyle.UrlOnly);
        mutable.SetWrapColumn(0);
        HtmlDocument document = Html.Parse(
            "<p><a href='https://example.test/path'>Example</a> tail</p>");

        HtmlTextResult first = HtmlTextConverter.Convert(document,
            compiled, CancellationToken.None);
        HtmlTextResult second = HtmlTextConverter.Convert(
            document.GetBody(), compiled, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetText(), Is.EqualTo(
                "Example\n(https://example.test/path)\ntail"));
            Assert.That(second.GetText(), Is.EqualTo(first.GetText()));
            Assert.That(compiled.GetLinkStyle(),
                Is.EqualTo(HtmlTextLinkStyle.TextAndUrl));
            Assert.That(compiled.GetWrapColumn(), Is.EqualTo(18));
        });
    }

    [Test]
    public void OutputLimitReturnsBoundedPartialTextAndConfiguredMarker()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(8);
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLimits(limits);
        options.SetTruncationMarker("[cut]");

        HtmlTextResult result = HtmlTextConverter.Convert(
            "<p>abcdefghijklmnop</p>", options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetText(), Is.EqualTo("abc[cut]"));
            Assert.That(result.GetText().Length, Is.LessThanOrEqualTo(8));
            Assert.That(result.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.TextConversionLimitExceeded));
        });
    }

    [Test]
    public void VisitLimitAlsoMarksPartialOutputWithoutExceedingOutputLimit()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(3);
        limits.SetMaximumOutputCharacters(5);
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLimits(limits);
        options.SetTruncationMarker("🙂🙂🙂");
        HtmlNode fragment = Html.ParseFragment(
            "<span>one</span><span>two</span>", "body").GetFragment();

        HtmlTextResult result = HtmlTextConverter.Convert(fragment,
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.GetText(), Is.EqualTo("🙂🙂"));
            Assert.That(result.GetText().Length, Is.EqualTo(4));
        });
    }

    [Test]
    public void EmptyMarkerCanDisableTheVisibleTruncationSuffix()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(4);
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLimits(limits);
        options.SetTruncationMarker("");

        HtmlTextResult result = HtmlTextConverter.Convert(
            "abcdefgh", options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetText(), Is.EqualTo("abcd"));
            Assert.That(result.WasTruncated(), Is.True);
        });
    }

    [Test]
    public void StringInputLimitsPropagateParseTruncationToTheTextResult()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(12);
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLimits(limits);
        options.SetTruncationMarker("[more]");

        HtmlTextResult result = HtmlTextConverter.Convert(
            "<p>first second third</p>", options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetText(), Does.EndWith("[more]"));
            Assert.That(result.GetDiagnostics(), Is.Not.Empty);
        });
    }

    [Test]
    public void ConversionHonorsPreCancelledTokensAndValidatesArguments()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        HtmlCompiledTextOptions compiled = options.Compile();
        HtmlNode fragment = Html.ParseFragment("text", "body").GetFragment();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                HtmlTextConverter.Convert("text", compiled, source.Token));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlTextConverter.Convert(fragment, compiled, source.Token));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlTextConverter.Convert((string)null, options,
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlTextConverter.Convert((HtmlNode)null, options,
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlTextConverter.Convert(fragment,
                    (HtmlTextOptions)null, CancellationToken.None));
        });
    }
}
