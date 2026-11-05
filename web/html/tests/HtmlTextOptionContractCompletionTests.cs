using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTextOptionContractCompletionTests
{
    [Test]
    public void NewSettingsCompileIntoIndependentReusableSnapshot()
    {
        HtmlTextOptions defaults = HtmlTextOptions.CreateDefault();
        HtmlTextOptions mutable = HtmlTextOptions.CreateDefault();
        mutable.SetWhitespaceMode(HtmlTextWhitespaceMode.Preserve);
        mutable.SetNonBreakingSpaceMode(
            HtmlTextNonBreakingSpaceMode.ConvertToSpace);
        mutable.SetLineBoundaryBreaks(0);
        mutable.SetBlockBoundaryBreaks(1);
        mutable.SetIncludeHiddenContent(true);
        mutable.SetBaseUrl("https://example.test/docs/page.html");

        HtmlCompiledTextOptions compiled = mutable.Compile();
        HtmlTextOptions copy = mutable.Copy();
        mutable.SetWhitespaceMode(HtmlTextWhitespaceMode.Collapse);
        mutable.SetNonBreakingSpaceMode(
            HtmlTextNonBreakingSpaceMode.Preserve);
        mutable.SetLineBoundaryBreaks(1);
        mutable.SetBlockBoundaryBreaks(2);
        mutable.SetIncludeHiddenContent(false);
        mutable.SetBaseUrl("");

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetWhitespaceMode(),
                Is.EqualTo(HtmlTextWhitespaceMode.Collapse));
            Assert.That(defaults.GetNonBreakingSpaceMode(),
                Is.EqualTo(HtmlTextNonBreakingSpaceMode.Preserve));
            Assert.That(defaults.GetLineBoundaryBreaks(), Is.EqualTo(1));
            Assert.That(defaults.GetBlockBoundaryBreaks(), Is.EqualTo(2));
            Assert.That(defaults.GetIncludeHiddenContent(), Is.False);
            Assert.That(defaults.GetBaseUrl(), Is.Empty);
            Assert.That(compiled.GetWhitespaceMode(),
                Is.EqualTo(HtmlTextWhitespaceMode.Preserve));
            Assert.That(compiled.GetNonBreakingSpaceMode(),
                Is.EqualTo(HtmlTextNonBreakingSpaceMode.ConvertToSpace));
            Assert.That(compiled.GetLineBoundaryBreaks(), Is.Zero);
            Assert.That(compiled.GetBlockBoundaryBreaks(), Is.EqualTo(1));
            Assert.That(compiled.GetIncludeHiddenContent(), Is.True);
            Assert.That(compiled.GetBaseUrl(),
                Is.EqualTo("https://example.test/docs/page.html"));
            Assert.That(copy.GetWhitespaceMode(),
                Is.EqualTo(HtmlTextWhitespaceMode.Preserve));
            Assert.That(copy.GetNonBreakingSpaceMode(),
                Is.EqualTo(HtmlTextNonBreakingSpaceMode.ConvertToSpace));
            Assert.That(copy.GetLineBoundaryBreaks(), Is.Zero);
            Assert.That(copy.GetBlockBoundaryBreaks(), Is.EqualTo(1));
            Assert.That(copy.GetIncludeHiddenContent(), Is.True);
            Assert.That(copy.GetBaseUrl(),
                Is.EqualTo("https://example.test/docs/page.html"));
        });
    }

    [Test]
    public void WhitespaceAndNonBreakingSpaceModesAreIndependent()
    {
        const string HtmlSource =
            "<span>alpha  beta&nbsp; gamma</span>";
        HtmlTextOptions collapsed = HtmlTextOptions.CreateDefault();
        collapsed.SetNonBreakingSpaceMode(
            HtmlTextNonBreakingSpaceMode.ConvertToSpace);
        HtmlTextOptions preserved = HtmlTextOptions.CreateDefault();
        preserved.SetWhitespaceMode(HtmlTextWhitespaceMode.Preserve);
        HtmlTextOptions preservedWithOrdinarySpaces = preserved.Copy();
        preservedWithOrdinarySpaces.SetNonBreakingSpaceMode(
            HtmlTextNonBreakingSpaceMode.ConvertToSpace);

        HtmlTextResult collapsedResult = HtmlTextConverter.Convert(
            HtmlSource, collapsed, CancellationToken.None);
        HtmlTextResult preservedResult = HtmlTextConverter.Convert(
            HtmlSource, preserved, CancellationToken.None);
        HtmlTextResult ordinaryResult = HtmlTextConverter.Convert(
            HtmlSource, preservedWithOrdinarySpaces,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(collapsedResult.GetText(),
                Is.EqualTo("alpha beta gamma"));
            Assert.That(preservedResult.GetText(),
                Is.EqualTo("alpha  beta\u00A0 gamma"));
            Assert.That(ordinaryResult.GetText(),
                Is.EqualTo("alpha  beta  gamma"));
        });
    }

    [Test]
    public void ConfiguredLineAndBlockBoundariesControlLayout()
    {
        HtmlTextOptions compact = HtmlTextOptions.CreateDefault();
        compact.SetLineBoundaryBreaks(0);
        compact.SetBlockBoundaryBreaks(1);
        HtmlTextOptions spacious = HtmlTextOptions.CreateDefault();
        spacious.SetMaximumBlankLines(3);
        spacious.SetLineBoundaryBreaks(2);
        spacious.SetBlockBoundaryBreaks(3);
        const string HtmlSource =
            "<p>one<br>next</p><p>two</p><dl><dt>term</dt>"
            + "<dd>definition</dd></dl>";

        HtmlTextResult compactResult = HtmlTextConverter.Convert(HtmlSource,
            compact, CancellationToken.None);
        HtmlTextResult spaciousResult = HtmlTextConverter.Convert(HtmlSource,
            spacious, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(compactResult.GetText(),
                Is.EqualTo("onenext\ntwo\ntermdefinition"));
            Assert.That(spaciousResult.GetText(), Is.EqualTo(
                "one\n\nnext\n\n\ntwo\n\n\nterm\n\ndefinition"));
        });
    }

    [Test]
    public void StringConversionResolvesSafeUrlsAgainstConfiguredBase()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetBaseUrl("https://example.test/docs/page.html");
        options.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        options.SetImageStyle(HtmlTextImageStyle.AltTextAndUrl);

        HtmlTextResult result = HtmlTextConverter.Convert(
            "<p><a href=guide>Guide</a>"
                + " <img alt=Map src=images/map.png>"
                + " <a href='javascript:bad'>Unsafe</a></p>",
            options, CancellationToken.None);

        Assert.That(result.GetText(), Is.EqualTo(
            "Guide (https://example.test/docs/guide) "
                + "Map (https://example.test/docs/images/map.png) Unsafe"));
    }

    [Test]
    public void HiddenAndInertContentRemainsExcludedUnlessEnabled()
    {
        const string HtmlSource = "<p>visible "
            + "<span hidden>hidden</span> "
            + "<span inert>inert</span> "
            + "<span aria-hidden=true>aria</span></p>";
        HtmlTextOptions excluded = HtmlTextOptions.CreateDefault();
        HtmlTextOptions included = HtmlTextOptions.CreateDefault();
        included.SetIncludeHiddenContent(true);

        HtmlTextResult excludedResult = HtmlTextConverter.Convert(
            HtmlSource, excluded, CancellationToken.None);
        HtmlTextResult includedResult = HtmlTextConverter.Convert(
            HtmlSource, included, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(excludedResult.GetText(), Is.EqualTo("visible"));
            Assert.That(includedResult.GetText(),
                Is.EqualTo("visible hidden inert aria"));
        });
    }

    [Test]
    public void NewMutableSettingsRejectInvalidValues()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();

        Assert.Multiple(() =>
        {
            Assert.That(() => options.SetWhitespaceMode(
                (HtmlTextWhitespaceMode)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => options.SetNonBreakingSpaceMode(
                (HtmlTextNonBreakingSpaceMode)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => options.SetLineBoundaryBreaks(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => options.SetBlockBoundaryBreaks(-1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => options.SetBaseUrl(null),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
