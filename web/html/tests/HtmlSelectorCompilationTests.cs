using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorCompilationTests
{
    [Test]
    public void CompilesImmutableRightToLeftPlansWithCandidateHints()
    {
        HtmlSelectorCompileResult result = HtmlSelectors.Compile(
            ".item, article#main.card, [data-value]");
        HtmlSelector selector = result.GetSelector();
        HtmlSelectorMatchPlan[] plans = selector.GetMatchPlans();
        plans[0] = plans[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDiagnostics(), Is.Empty);
            Assert.That(selector.GetSource(), Is.EqualTo(
                ".item, article#main.card, [data-value]"));
            Assert.That(selector.GetMatchPlans().Select(value =>
                value.GetCandidateKind()), Is.EqualTo(new[]
                {
                    HtmlSelectorCandidateKind.Class,
                    HtmlSelectorCandidateKind.Id,
                    HtmlSelectorCandidateKind.AllElements
                }));
            Assert.That(selector.GetMatchPlans().Select(value =>
                value.GetCandidateValue()), Is.EqualTo(new[]
                {
                    "item", "main", ""
                }));
            Assert.That(selector.GetMatchPlans().Select(value =>
                value.GetCompoundCount()), Is.EqualTo(new[] { 1, 1, 1 }));
        });
    }

    [Test]
    public void ComputesSelectorLevelFourSpecificityPerListBranch()
    {
        HtmlSelector selector = Compile(
            "article#main.card[data-x]"
            + ":is(.active,#override):where(#ignored)"
            + ":not(span):has(> a.link), *");
        HtmlSelectorSpecificity[] values = selector.GetMatchPlans()
            .Select(value => value.GetSpecificity()).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(values[0].GetIdCount(), Is.EqualTo(2));
            Assert.That(values[0].GetClassCount(), Is.EqualTo(3));
            Assert.That(values[0].GetTypeCount(), Is.EqualTo(3));
            Assert.That(values[1].GetIdCount(), Is.Zero);
            Assert.That(values[1].GetClassCount(), Is.Zero);
            Assert.That(values[1].GetTypeCount(), Is.Zero);
            Assert.That(values[0].CompareTo(values[1]), Is.GreaterThan(0));
            Assert.That(values[1].CompareTo(null), Is.GreaterThan(0));
        });
    }

    [Test]
    public void CompilationPreservesParseDiagnosticsAndValidatesPrefixes()
    {
        HtmlSelectorCompileResult syntax = HtmlSelectors.Compile("div>");
        HtmlSelectorCompileResult unsupported = HtmlSelectors.Compile(
            "div || span");
        HtmlSelectorCompileResult prefix = HtmlSelectors.Compile(
            "svg|circle");
        HtmlSelectorCompileResult namespaceWildcards =
            HtmlSelectors.Compile("*|circle, |circle");
        HtmlDiagnostic[] snapshot = prefix.GetDiagnostics();
        snapshot[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(syntax.IsSuccess(), Is.False);
            Assert.That(syntax.GetDiagnostics()[0].GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.SelectorInvalidSyntax));
            Assert.That(unsupported.IsSuccess(), Is.False);
            Assert.That(unsupported.GetDiagnostics()[0].GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorUnsupportedFeature));
            Assert.That(prefix.IsSuccess(), Is.False);
            Assert.That(prefix.GetSelector(), Is.Null);
            Assert.That(prefix.GetDiagnostics()[0].GetMessage(),
                Does.Contain("svg"));
            Assert.That(prefix.GetDiagnostics()[0].GetSpan().GetStart()
                .GetCharacterOffset(), Is.Zero);
            Assert.That(namespaceWildcards.IsSuccess(), Is.True);
        });
    }

    [Test]
    public void CompileArgumentsAndLimitsAreValidated()
    {
        HtmlLimits invalid = HtmlLimits.CreateDefault();
        invalid.SetMaximumSelectorComplexity(1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSelectors.Compile(null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSelectors.Compile("div", null));
            Assert.That(HtmlSelectors.Compile("div.x", invalid)
                .GetDiagnostics()[0].GetCode(), Is.EqualTo(
                    HtmlDiagnosticCode.SelectorComplexityExceeded));
        });
    }

    static HtmlSelector Compile(string source)
    {
        HtmlSelectorCompileResult result = HtmlSelectors.Compile(source);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetSelector();
    }
}
