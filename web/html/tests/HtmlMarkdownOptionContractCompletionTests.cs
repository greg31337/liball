using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownOptionContractCompletionTests
{
    [Test]
    public void SyntaxAndSafetySettingsCompileIntoIndependentSnapshot()
    {
        HtmlMarkdownOptions defaults =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlMarkdownOptions mutable =
            HtmlMarkdownOptions.CreateCommonMark();
        mutable.SetEmphasisMarker("_");
        mutable.SetStrongMarker("__");
        mutable.SetHardBreakStyle(HtmlMarkdownHardBreakStyle.Backslash);
        mutable.SetCodeFenceStyle(HtmlMarkdownCodeFenceStyle.Tildes);
        mutable.SetThematicBreakMarker("***");
        mutable.SetUnsafeLinkFallback(
            HtmlMarkdownUnsafeLinkFallback.Omit);
        mutable.SetBaseUrl("https://example.test/docs/page.html");

        HtmlCompiledMarkdownOptions compiled = mutable.Compile();
        HtmlMarkdownOptions copy = mutable.Copy();
        mutable.SetEmphasisMarker("*");
        mutable.SetStrongMarker("**");
        mutable.SetHardBreakStyle(HtmlMarkdownHardBreakStyle.TwoSpaces);
        mutable.SetCodeFenceStyle(HtmlMarkdownCodeFenceStyle.Adaptive);
        mutable.SetThematicBreakMarker("---");
        mutable.SetUnsafeLinkFallback(
            HtmlMarkdownUnsafeLinkFallback.Text);
        mutable.SetBaseUrl("");

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetEmphasisMarker(), Is.EqualTo("*"));
            Assert.That(defaults.GetStrongMarker(), Is.EqualTo("**"));
            Assert.That(defaults.GetHardBreakStyle(),
                Is.EqualTo(HtmlMarkdownHardBreakStyle.TwoSpaces));
            Assert.That(defaults.GetCodeFenceStyle(),
                Is.EqualTo(HtmlMarkdownCodeFenceStyle.Adaptive));
            Assert.That(defaults.GetThematicBreakMarker(),
                Is.EqualTo("---"));
            Assert.That(defaults.GetUnsafeLinkFallback(),
                Is.EqualTo(HtmlMarkdownUnsafeLinkFallback.Text));
            Assert.That(defaults.GetBaseUrl(), Is.Empty);
            Assert.That(compiled.GetEmphasisMarker(), Is.EqualTo("_"));
            Assert.That(compiled.GetStrongMarker(), Is.EqualTo("__"));
            Assert.That(compiled.GetHardBreakStyle(),
                Is.EqualTo(HtmlMarkdownHardBreakStyle.Backslash));
            Assert.That(compiled.GetCodeFenceStyle(),
                Is.EqualTo(HtmlMarkdownCodeFenceStyle.Tildes));
            Assert.That(compiled.GetThematicBreakMarker(),
                Is.EqualTo("***"));
            Assert.That(compiled.GetUnsafeLinkFallback(),
                Is.EqualTo(HtmlMarkdownUnsafeLinkFallback.Omit));
            Assert.That(compiled.GetBaseUrl(),
                Is.EqualTo("https://example.test/docs/page.html"));
            Assert.That(copy.GetEmphasisMarker(), Is.EqualTo("_"));
            Assert.That(copy.GetStrongMarker(), Is.EqualTo("__"));
            Assert.That(copy.GetBaseUrl(),
                Is.EqualTo("https://example.test/docs/page.html"));
        });
    }

    [Test]
    public void ConfiguredInlineBreakAndThematicSyntaxIsEmitted()
    {
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetEmphasisMarker("_");
        options.SetStrongMarker("__");
        options.SetHardBreakStyle(HtmlMarkdownHardBreakStyle.Backslash);
        options.SetThematicBreakMarker("***");

        HtmlMarkdownResult result = HtmlMarkdownConverter.Convert(
            "<p><em>em</em> <strong>strong</strong><br>next</p>"
                + "<hr><p>after</p>",
            options, CancellationToken.None);

        Assert.That(result.GetMarkdown(), Is.EqualTo(
            "_em_ __strong__\\\nnext\n\n***\n\nafter"));
    }

    [Test]
    public void FenceChoiceRemainsCollisionFree()
    {
        HtmlMarkdownOptions backticks =
            HtmlMarkdownOptions.CreateCommonMark();
        backticks.SetCodeFenceStyle(HtmlMarkdownCodeFenceStyle.Backticks);
        HtmlMarkdownOptions tildes =
            HtmlMarkdownOptions.CreateCommonMark();
        tildes.SetCodeFenceStyle(HtmlMarkdownCodeFenceStyle.Tildes);
        const string HtmlSource = "<pre>before ``` after\n~~~~</pre>";

        string backtickResult = HtmlMarkdownConverter.Convert(HtmlSource,
            backticks, CancellationToken.None).GetMarkdown();
        string tildeResult = HtmlMarkdownConverter.Convert(HtmlSource,
            tildes, CancellationToken.None).GetMarkdown();

        Assert.Multiple(() =>
        {
            Assert.That(backtickResult, Is.EqualTo(
                "````\nbefore ``` after\n~~~~\n````"));
            Assert.That(tildeResult, Is.EqualTo(
                "~~~~~\nbefore ``` after\n~~~~\n~~~~~"));
        });
    }

    [Test]
    public void StringBaseResolutionAndUnsafeFallbackAreConfigurable()
    {
        const string HtmlSource = "<p><a href=guide>Guide</a> "
            + "<a href='javascript:bad'>Unsafe</a> "
            + "<img alt=Bad src='data:text/html,bad'></p>";
        HtmlMarkdownOptions textFallback =
            HtmlMarkdownOptions.CreateCommonMark();
        textFallback.SetBaseUrl(
            "https://example.test/docs/page.html");
        HtmlMarkdownOptions omitFallback = textFallback.Copy();
        omitFallback.SetUnsafeLinkFallback(
            HtmlMarkdownUnsafeLinkFallback.Omit);

        HtmlMarkdownResult textResult = HtmlMarkdownConverter.Convert(
            HtmlSource, textFallback, CancellationToken.None);
        HtmlMarkdownResult omitResult = HtmlMarkdownConverter.Convert(
            HtmlSource, omitFallback, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(textResult.GetMarkdown(), Is.EqualTo(
                "[Guide](https://example.test/docs/guide) Unsafe Bad"));
            Assert.That(omitResult.GetMarkdown(), Is.EqualTo(
                "[Guide](https://example.test/docs/guide)"));
        });
    }

    [Test]
    public void NewMutableSettingsRejectInvalidValues()
    {
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();

        Assert.Multiple(() =>
        {
            Assert.That(() => options.SetEmphasisMarker(null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => options.SetEmphasisMarker("**"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => options.SetStrongMarker("*"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => options.SetHardBreakStyle(
                (HtmlMarkdownHardBreakStyle)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => options.SetCodeFenceStyle(
                (HtmlMarkdownCodeFenceStyle)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => options.SetThematicBreakMarker("--"),
                Throws.TypeOf<ArgumentException>());
            Assert.That(() => options.SetUnsafeLinkFallback(
                (HtmlMarkdownUnsafeLinkFallback)99),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => options.SetBaseUrl(null),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
