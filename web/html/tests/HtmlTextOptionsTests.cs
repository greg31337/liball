
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTextOptionsTests
{
    [Test]
    public void DefaultAndSearchPresetsHaveConservativeConcreteSettings()
    {
        HtmlTextOptions defaults = HtmlTextOptions.CreateDefault();
        HtmlTextOptions search = HtmlTextOptions.CreateSearchIndex();

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetLinkStyle(),
                Is.EqualTo(HtmlTextLinkStyle.TextOnly));
            Assert.That(defaults.GetImageStyle(),
                Is.EqualTo(HtmlTextImageStyle.AltText));
            Assert.That(defaults.GetTableStyle(),
                Is.EqualTo(HtmlTextTableStyle.ReadingOrder));
            Assert.That(defaults.GetWrapColumn(), Is.Zero);
            Assert.That(defaults.GetTruncationMarker(), Is.EqualTo("…"));
            Assert.That(defaults.GetNewLine(), Is.EqualTo("\n"));
            Assert.That(defaults.GetTrim(), Is.True);
            Assert.That(defaults.GetMaximumBlankLines(), Is.EqualTo(1));
            Assert.That(defaults.GetUnorderedListBullets(),
                Is.EqualTo(new[] { "-", "*", "•" }));
            Assert.That(defaults.GetMaximumListDepth(), Is.EqualTo(16));
            Assert.That(defaults.GetIncludeFormControls(), Is.False);
            Assert.That(defaults.GetRules(), Has.Length.EqualTo(1));
            Assert.That(defaults.GetRules()[0].GetSelector(),
                Is.EqualTo("script, style, template, head, noscript"));
            Assert.That(defaults.GetRules()[0].GetAction(),
                Is.EqualTo(HtmlTextRuleAction.Exclude));
            Assert.That(search.GetImageStyle(),
                Is.EqualTo(HtmlTextImageStyle.Omit));
            Assert.That(search.GetRules(), Has.Length.EqualTo(2));
            Assert.That(search.GetRules()[1].GetSelector(),
                Is.EqualTo("nav, aside, form"));
        });
    }

    [Test]
    public void CopyAndCompileCreateIndependentSnapshots()
    {
        HtmlTextOptions original = HtmlTextOptions.CreateDefault();
        original.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        original.SetImageStyle(HtmlTextImageStyle.AltTextAndUrl);
        original.SetTableStyle(HtmlTextTableStyle.Tsv);
        original.SetWrapColumn(72);
        original.SetTruncationMarker("[cut]");
        original.SetNewLine("\r\n");
        original.SetTrim(false);
        original.SetMaximumBlankLines(2);
        string[] bullets = ["→", "·"];
        original.SetUnorderedListBullets(bullets);
        original.SetMaximumListDepth(4);
        original.SetIncludeFormControls(true);
        original.AddExcludedSelector(".advertisement");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(123);
        original.SetLimits(limits);

        HtmlTextOptions copy = original.Copy();
        HtmlCompiledTextOptions compiled = original.Compile();
        copy.SetWrapColumn(40);
        copy.AddReplacementSelector(".price", "[price]");
        limits.SetMaximumVisits(999);
        original.SetLinkStyle(HtmlTextLinkStyle.UrlOnly);
        HtmlCompiledTextRule[] rules = compiled.GetRules();
        rules[0] = null;
        bullets[0] = "changed";
        string[] compiledBullets = compiled.GetUnorderedListBullets();
        compiledBullets[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(original.GetWrapColumn(), Is.EqualTo(72));
            Assert.That(original.GetRules(), Has.Length.EqualTo(2));
            Assert.That(copy.GetWrapColumn(), Is.EqualTo(40));
            Assert.That(copy.GetRules(), Has.Length.EqualTo(3));
            Assert.That(compiled.GetLinkStyle(),
                Is.EqualTo(HtmlTextLinkStyle.TextAndUrl));
            Assert.That(compiled.GetImageStyle(),
                Is.EqualTo(HtmlTextImageStyle.AltTextAndUrl));
            Assert.That(compiled.GetTableStyle(),
                Is.EqualTo(HtmlTextTableStyle.Tsv));
            Assert.That(compiled.GetWrapColumn(), Is.EqualTo(72));
            Assert.That(compiled.GetTruncationMarker(), Is.EqualTo("[cut]"));
            Assert.That(compiled.GetNewLine(), Is.EqualTo("\r\n"));
            Assert.That(compiled.GetTrim(), Is.False);
            Assert.That(compiled.GetMaximumBlankLines(), Is.EqualTo(2));
            Assert.That(compiled.GetUnorderedListBullets(),
                Is.EqualTo(new[] { "→", "·" }));
            Assert.That(compiled.GetMaximumListDepth(), Is.EqualTo(4));
            Assert.That(compiled.GetIncludeFormControls(), Is.True);
            Assert.That(compiled.GetLimits().GetMaximumVisits(),
                Is.EqualTo(123));
            Assert.That(compiled.GetRules()[0], Is.Not.Null);
        });
    }

    [Test]
    public void OrderedRulesCompileSelectorsAndMatchNodes()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.AddIncludedSelector("main > .keep");
        options.AddExcludedSelector("nav, .hidden");
        options.AddReplacementSelector(".price", "[price omitted]");
        HtmlCompiledTextRule[] rules = options.Compile().GetRules();
        HtmlDocument document = Html.Parse(
            "<main><p class='keep price'>x</p><nav>n</nav></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode paragraph = document.QuerySelector("p");
        HtmlNode navigation = document.QuerySelector("nav");

        Assert.Multiple(() =>
        {
            Assert.That(rules.Select(value => value.GetAction()),
                Is.EqualTo(new[]
                {
                    HtmlTextRuleAction.Exclude,
                    HtmlTextRuleAction.Include,
                    HtmlTextRuleAction.Exclude,
                    HtmlTextRuleAction.Replace
                }));
            Assert.That(rules[1].Matches(paragraph, main,
                CancellationToken.None), Is.True);
            Assert.That(rules[2].Matches(navigation, main,
                CancellationToken.None), Is.True);
            Assert.That(rules[3].Matches(paragraph, main,
                CancellationToken.None), Is.True);
            Assert.That(rules[3].GetReplacement(),
                Is.EqualTo("[price omitted]"));
        });
    }

    [Test]
    public void InvalidSettingsRulesAndSelectorsAreRejected()
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        HtmlTextOptions invalidSelector = HtmlTextOptions.CreateDefault();
        invalidSelector.AddExcludedSelector("div >> p");
        HtmlTextOptions limitedSelector = HtmlTextOptions.CreateDefault();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumSelectorComplexity(1);
        limitedSelector.SetLimits(limits);
        limitedSelector.AddExcludedSelector("div.item");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetLinkStyle((HtmlTextLinkStyle)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetImageStyle((HtmlTextImageStyle)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetTableStyle((HtmlTextTableStyle)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetWrapColumn(-1));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetTruncationMarker(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetNewLine(null));
            Assert.Throws<ArgumentException>(() =>
                options.SetNewLine(""));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMaximumBlankLines(-1));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetUnorderedListBullets(null));
            Assert.Throws<ArgumentException>(() =>
                options.SetUnorderedListBullets([]));
            Assert.Throws<ArgumentException>(() =>
                options.SetUnorderedListBullets([null]));
            Assert.Throws<ArgumentException>(() =>
                options.SetUnorderedListBullets(["bad bullet"]));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMaximumListDepth(0));
            Assert.Throws<ArgumentNullException>(() => options.AddRule(null));
            Assert.Throws<ArgumentException>(() =>
                new HtmlTextRule("", HtmlTextRuleAction.Include, ""));
            Assert.Throws<ArgumentException>(() =>
                new HtmlTextRule("p", HtmlTextRuleAction.Exclude, "text"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlTextRule("p", (HtmlTextRuleAction)99, ""));
            Assert.Throws<ArgumentException>(() => invalidSelector.Compile());
            Assert.Throws<ArgumentException>(() => limitedSelector.Compile());
        });
    }

    [Test]
    public void OperationContextSharesLimitsDiagnosticsAndCancellation()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(2);
        limits.SetMaximumDepth(1);
        limits.SetMaximumOutputCharacters(5);
        limits.SetMaximumTableCells(2);
        limits.SetMaximumDiagnostics(1);
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetLimits(limits);
        HtmlTextOperationContext context = new HtmlTextOperationContext(
            options.Compile(), CancellationToken.None, null);

        Assert.That(context.TryVisit(0), Is.True);
        Assert.That(context.TryVisit(1), Is.True);
        Assert.That(context.TryVisit(1), Is.False);
        Assert.That(context.TryConsumeOutput(5), Is.True);
        Assert.That(context.TryConsumeOutput(1), Is.False);
        Assert.That(context.TryConsumeTableCells(2), Is.True);
        Assert.That(context.TryConsumeTableCells(1), Is.False);
        Assert.That(context.AddDiagnostic(HtmlDiagnostic.Create(
            HtmlDiagnosticCode.OperationIncomplete,
            HtmlDiagnosticSeverity.Warning, "additional",
            HtmlSourceSpan.CreateUnknown())), Is.False);

        HtmlTextResult result = context.CreateResult("hello", true);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetText(), Is.EqualTo("hello"));
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(result.GetDiagnostics()[0].GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.TextConversionLimitExceeded));
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(2));
            Assert.That(result.GetDroppedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.WereDiagnosticsTruncated(), Is.True);
        });

        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        HtmlTextOperationContext cancelled = new HtmlTextOperationContext(
            options.Compile(), source.Token, null);
        Assert.That(() => cancelled.TryVisit(0),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void TextResultDefensivelyCopiesDiagnostics()
    {
        HtmlDiagnostic[] diagnostics =
        [
            HtmlDiagnostic.Create(HtmlDiagnosticCode.OperationIncomplete,
                HtmlDiagnosticSeverity.Warning, "partial",
                HtmlSourceSpan.CreateUnknown())
        ];
        HtmlTextResult result = new HtmlTextResult(true, false, true,
            "partial", diagnostics, 1, 0);
        diagnostics[0] = null;
        HtmlDiagnostic[] snapshot = result.GetDiagnostics();
        snapshot[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDiagnostics()[0], Is.Not.Null);
            Assert.Throws<ArgumentException>(() => new HtmlTextResult(true,
                true, true, "", Array.Empty<HtmlDiagnostic>(), 0, 0));
            Assert.Throws<ArgumentNullException>(() => new HtmlTextResult(
                true, true, false, null, Array.Empty<HtmlDiagnostic>(), 0, 0));
        });
    }
}
