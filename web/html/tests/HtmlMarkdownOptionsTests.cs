
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownOptionsTests
{
    [Test]
    public void PresetsExposeDistinctSafeFeatureProfiles()
    {
        HtmlMarkdownOptions common =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlMarkdownOptions github =
            HtmlMarkdownOptions.CreateGitHubFlavored();
        HtmlMarkdownOptions readable =
            HtmlMarkdownOptions.CreateReadable();

        Assert.Multiple(() =>
        {
            Assert.That(common.GetHeadingStyle(),
                Is.EqualTo(HtmlMarkdownHeadingStyle.Atx));
            Assert.That(common.GetLinkStyle(),
                Is.EqualTo(HtmlMarkdownLinkStyle.Inline));
            Assert.That(common.GetUnknownElementAction(),
                Is.EqualTo(HtmlMarkdownUnknownElementAction.Unwrap));
            Assert.That(common.GetOrderedListNumbering(),
                Is.EqualTo(HtmlMarkdownOrderedListNumbering.Preserve));
            Assert.That(common.GetUnorderedListMarker(), Is.EqualTo("-"));
            Assert.That(common.GetEnableTables(), Is.False);
            Assert.That(common.GetEnableStrikethrough(), Is.False);
            Assert.That(common.GetEnableTaskLists(), Is.False);
            Assert.That(common.GetEnableAutolinks(), Is.False);
            Assert.That(common.GetEnableDefinitionLists(), Is.False);
            Assert.That(common.GetEnableFootnotes(), Is.False);
            Assert.That(common.GetEnableSubscript(), Is.False);
            Assert.That(common.GetEnableSuperscript(), Is.False);
            Assert.That(common.GetEnableTrustedRawHtml(), Is.False);
            Assert.That(common.GetNewLine(), Is.EqualTo("\n"));
            Assert.That(common.GetTruncationMarker(), Is.EqualTo("…"));
            Assert.That(common.GetRules(), Is.Empty);

            Assert.That(github.GetEnableTables(), Is.True);
            Assert.That(github.GetEnableStrikethrough(), Is.True);
            Assert.That(github.GetEnableTaskLists(), Is.True);
            Assert.That(github.GetEnableAutolinks(), Is.True);
            Assert.That(github.GetEnableDefinitionLists(), Is.False);
            Assert.That(github.GetEnableFootnotes(), Is.False);
            Assert.That(github.GetEnableTrustedRawHtml(), Is.False);

            Assert.That(readable.GetLinkStyle(),
                Is.EqualTo(HtmlMarkdownLinkStyle.Reference));
            Assert.That(readable.GetEnableTables(), Is.True);
            Assert.That(readable.GetEnableDefinitionLists(), Is.True);
            Assert.That(readable.GetEnableFootnotes(), Is.True);
            Assert.That(readable.GetEnableSubscript(), Is.True);
            Assert.That(readable.GetEnableSuperscript(), Is.True);
            Assert.That(readable.GetEnableTrustedRawHtml(), Is.False);
        });
    }

    [Test]
    public void CopyAndCompileCreateIndependentCompleteSnapshots()
    {
        HtmlMarkdownOptions original =
            HtmlMarkdownOptions.CreateCommonMark();
        original.SetHeadingStyle(HtmlMarkdownHeadingStyle.Setext);
        original.SetLinkStyle(HtmlMarkdownLinkStyle.CollapsedReference);
        original.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.EscapeHtml);
        original.SetOrderedListNumbering(
            HtmlMarkdownOrderedListNumbering.One);
        original.SetUnorderedListMarker("+");
        original.SetEnableTables(true);
        original.SetEnableStrikethrough(true);
        original.SetEnableTaskLists(true);
        original.SetEnableAutolinks(true);
        original.SetEnableDefinitionLists(true);
        original.SetEnableFootnotes(true);
        original.SetEnableSubscript(true);
        original.SetEnableSuperscript(true);
        original.SetEnableTrustedRawHtml(true);
        original.SetNewLine("\r\n");
        original.SetTruncationMarker("[cut]");
        original.Keep("custom-element");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(123);
        original.SetLimits(limits);

        HtmlMarkdownOptions copy = original.Copy();
        HtmlCompiledMarkdownOptions compiled = original.Compile();
        original.SetLinkStyle(HtmlMarkdownLinkStyle.Inline);
        original.Remove(".later");
        copy.SetEnableTables(false);
        copy.SetTruncationMarker("changed");
        limits.SetMaximumVisits(999);
        HtmlCompiledMarkdownRule[] exposedRules = compiled.GetRules();
        exposedRules[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(copy.GetEnableTables(), Is.False);
            Assert.That(copy.GetRules(), Has.Length.EqualTo(1));
            Assert.That(original.GetRules(), Has.Length.EqualTo(2));
            Assert.That(compiled.GetHeadingStyle(),
                Is.EqualTo(HtmlMarkdownHeadingStyle.Setext));
            Assert.That(compiled.GetLinkStyle(),
                Is.EqualTo(HtmlMarkdownLinkStyle.CollapsedReference));
            Assert.That(compiled.GetUnknownElementAction(),
                Is.EqualTo(HtmlMarkdownUnknownElementAction.EscapeHtml));
            Assert.That(compiled.GetOrderedListNumbering(),
                Is.EqualTo(HtmlMarkdownOrderedListNumbering.One));
            Assert.That(compiled.GetUnorderedListMarker(), Is.EqualTo("+"));
            Assert.That(compiled.GetEnableTables(), Is.True);
            Assert.That(compiled.GetEnableStrikethrough(), Is.True);
            Assert.That(compiled.GetEnableTaskLists(), Is.True);
            Assert.That(compiled.GetEnableAutolinks(), Is.True);
            Assert.That(compiled.GetEnableDefinitionLists(), Is.True);
            Assert.That(compiled.GetEnableFootnotes(), Is.True);
            Assert.That(compiled.GetEnableSubscript(), Is.True);
            Assert.That(compiled.GetEnableSuperscript(), Is.True);
            Assert.That(compiled.GetEnableTrustedRawHtml(), Is.True);
            Assert.That(compiled.GetNewLine(), Is.EqualTo("\r\n"));
            Assert.That(compiled.GetTruncationMarker(), Is.EqualTo("[cut]"));
            Assert.That(compiled.GetRules(), Has.Length.EqualTo(1));
            Assert.That(compiled.GetRules()[0].GetAction(),
                Is.EqualTo(HtmlMarkdownRuleAction.Keep));
            Assert.That(compiled.GetLimits().GetMaximumVisits(),
                Is.EqualTo(123));
        });
    }

    [Test]
    public void OrderedRulesUseLastMatchingActionDeterministically()
    {
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetEnableTrustedRawHtml(true);
        options.Keep(".item");
        options.Remove("article .item");
        options.UseDefault("#restore");
        HtmlCompiledMarkdownOptions compiled = options.Compile();
        HtmlDocument document = Html.Parse(
            "<article><p class=item>remove</p>"
            + "<p id=restore class=item>default</p></article>");
        HtmlNode[] paragraphs = document.QuerySelectorAll("p");

        HtmlCompiledMarkdownRule removed = compiled.FindRule(paragraphs[0],
            document.GetRoot(), CancellationToken.None);
        HtmlCompiledMarkdownRule restored = compiled.FindRule(paragraphs[1],
            document.GetRoot(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetRules().Select(value =>
                value.GetSelectorSource()), Is.EqualTo(new[]
                {
                    ".item", "article .item", "#restore"
                }));
            Assert.That(removed.GetAction(),
                Is.EqualTo(HtmlMarkdownRuleAction.Remove));
            Assert.That(restored.GetAction(),
                Is.EqualTo(HtmlMarkdownRuleAction.Default));
        });
    }

    [Test]
    public void InvalidRulesReturnBoundedStableCompilationDiagnostics()
    {
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumDiagnostics(2);
        options.SetLimits(limits);
        options.SetEnableTrustedRawHtml(true);
        options.Keep("div >> p");
        options.Remove("::before");
        options.UseDefault("a:visited");

        HtmlMarkdownCompileResult result =
            options.CompileWithDiagnostics();
        HtmlDiagnostic[] diagnostics = result.GetDiagnostics();
        diagnostics[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetOptions(), Is.Null);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(2));
            Assert.That(result.GetDiagnostics().All(value =>
                value.GetCode() == HtmlDiagnosticCode.MarkdownInvalidRule),
                Is.True);
            Assert.That(result.GetDiagnosticCount(), Is.EqualTo(3));
            Assert.That(result.GetDroppedDiagnosticCount(), Is.EqualTo(1));
            Assert.That(result.WereDiagnosticsTruncated(), Is.True);
            Assert.Throws<ArgumentException>(() => options.Compile());
        });
    }

    [Test]
    public void TotalRuleCompilationWorkUsesTheConfiguredVisitBudget()
    {
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(1);
        options.SetLimits(limits);
        options.SetEnableTrustedRawHtml(true);
        options.Keep(".first");
        options.Remove(".second");

        HtmlMarkdownCompileResult result =
            options.CompileWithDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(result.GetDiagnostics()[0].GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.MarkdownConversionLimitExceeded));
        });
    }

    [Test]
    public void RawHtmlActionsRequireTheExplicitTrustSetting()
    {
        HtmlMarkdownOptions keep =
            HtmlMarkdownOptions.CreateCommonMark();
        keep.Keep("custom-element");
        HtmlMarkdownOptions preserve =
            HtmlMarkdownOptions.CreateCommonMark();
        preserve.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.PreserveHtml);

        HtmlMarkdownCompileResult keepFailure =
            keep.CompileWithDiagnostics();
        HtmlMarkdownCompileResult preserveFailure =
            preserve.CompileWithDiagnostics();
        keep.SetEnableTrustedRawHtml(true);
        preserve.SetEnableTrustedRawHtml(true);

        Assert.Multiple(() =>
        {
            Assert.That(keepFailure.IsSuccess(), Is.False);
            Assert.That(keepFailure.GetDiagnostics()[0].GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.MarkdownInvalidConfiguration));
            Assert.That(preserveFailure.IsSuccess(), Is.False);
            Assert.That(preserveFailure.GetDiagnostics()[0].GetCode(),
                Is.EqualTo(
                    HtmlDiagnosticCode.MarkdownInvalidConfiguration));
            Assert.That(keep.Compile(), Is.Not.Null);
            Assert.That(preserve.Compile(), Is.Not.Null);
        });
    }

    [Test]
    public void InvalidSettingsAndRuleArgumentsAreRejectedImmediately()
    {
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetHeadingStyle((HtmlMarkdownHeadingStyle)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetLinkStyle((HtmlMarkdownLinkStyle)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetUnknownElementAction(
                    (HtmlMarkdownUnknownElementAction)99));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetOrderedListNumbering(
                    (HtmlMarkdownOrderedListNumbering)99));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetUnorderedListMarker(null));
            Assert.Throws<ArgumentException>(() =>
                options.SetUnorderedListMarker(""));
            Assert.Throws<ArgumentException>(() =>
                options.SetUnorderedListMarker("•"));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetNewLine(null));
            Assert.Throws<ArgumentException>(() =>
                options.SetNewLine(""));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetTruncationMarker(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetLimits(null));
            Assert.Throws<ArgumentNullException>(() =>
                options.AddRule(null));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlMarkdownRule(null, HtmlMarkdownRuleAction.Keep));
            Assert.Throws<ArgumentException>(() =>
                new HtmlMarkdownRule("", HtmlMarkdownRuleAction.Keep));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new HtmlMarkdownRule("p", (HtmlMarkdownRuleAction)99));
        });
    }
}
