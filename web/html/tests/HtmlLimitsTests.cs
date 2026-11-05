
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlLimitsTests
{
    [Test]
    public void DefaultAndUntrustedPresetsAreValidAndBounded()
    {
        HtmlLimits defaults = HtmlLimits.CreateDefault();
        HtmlLimits untrusted = HtmlLimits.CreateUntrusted();

        Assert.DoesNotThrow(defaults.Validate);
        Assert.DoesNotThrow(untrusted.Validate);
        Assert.That(untrusted.GetMaximumInputBytes(), Is.LessThan(defaults.GetMaximumInputBytes()));
        Assert.That(untrusted.GetMaximumInputCharacters(), Is.LessThan(defaults.GetMaximumInputCharacters()));
        Assert.That(untrusted.GetMaximumTokens(), Is.LessThan(defaults.GetMaximumTokens()));
        Assert.That(untrusted.GetMaximumAttributesPerTag(), Is.LessThan(defaults.GetMaximumAttributesPerTag()));
        Assert.That(untrusted.GetMaximumTokenCharacters(), Is.LessThan(defaults.GetMaximumTokenCharacters()));
        Assert.That(untrusted.GetMaximumNameCharacters(), Is.LessThan(defaults.GetMaximumNameCharacters()));
        Assert.That(untrusted.GetMaximumAttributeValueCharacters(), Is.LessThan(defaults.GetMaximumAttributeValueCharacters()));
        Assert.That(untrusted.GetMaximumNodes(), Is.LessThan(defaults.GetMaximumNodes()));
        Assert.That(untrusted.GetMaximumDepth(), Is.LessThan(defaults.GetMaximumDepth()));
        Assert.That(untrusted.GetMaximumSelectorComplexity(), Is.LessThan(defaults.GetMaximumSelectorComplexity()));
        Assert.That(untrusted.GetMaximumSelectorResults(), Is.LessThan(defaults.GetMaximumSelectorResults()));
        Assert.That(untrusted.GetMaximumHasWork(), Is.LessThan(defaults.GetMaximumHasWork()));
        Assert.That(untrusted.GetMaximumCssTokens(), Is.LessThan(defaults.GetMaximumCssTokens()));
        Assert.That(untrusted.GetMaximumCssDeclarations(), Is.LessThan(defaults.GetMaximumCssDeclarations()));
        Assert.That(untrusted.GetMaximumUrlCharacters(), Is.LessThan(defaults.GetMaximumUrlCharacters()));
        Assert.That(untrusted.GetMaximumDataUriBytes(), Is.LessThan(defaults.GetMaximumDataUriBytes()));
        Assert.That(untrusted.GetMaximumTableCells(), Is.LessThan(defaults.GetMaximumTableCells()));
        Assert.That(untrusted.GetMaximumJsonDepth(), Is.LessThan(defaults.GetMaximumJsonDepth()));
        Assert.That(untrusted.GetMaximumJsonProperties(), Is.LessThan(defaults.GetMaximumJsonProperties()));
        Assert.That(untrusted.GetMaximumVisits(), Is.LessThan(defaults.GetMaximumVisits()));
        Assert.That(untrusted.GetMaximumDiagnostics(), Is.LessThan(defaults.GetMaximumDiagnostics()));
        Assert.That(untrusted.GetMaximumOutputCharacters(), Is.LessThan(defaults.GetMaximumOutputCharacters()));
    }

    [Test]
    public void SettersRoundTripAllLimits()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();

        limits.SetMaximumInputBytes(101);
        limits.SetMaximumInputCharacters(102);
        limits.SetMaximumTokens(103);
        limits.SetMaximumAttributesPerTag(104);
        limits.SetMaximumTokenCharacters(105);
        limits.SetMaximumNameCharacters(106);
        limits.SetMaximumAttributeValueCharacters(107);
        limits.SetMaximumNodes(108);
        limits.SetMaximumDepth(109);
        limits.SetMaximumSelectorComplexity(110);
        limits.SetMaximumSelectorResults(111);
        limits.SetMaximumHasWork(112);
        limits.SetMaximumCssTokens(113);
        limits.SetMaximumCssDeclarations(114);
        limits.SetMaximumUrlCharacters(115);
        limits.SetMaximumDataUriBytes(116);
        limits.SetMaximumTableCells(117);
        limits.SetMaximumJsonDepth(118);
        limits.SetMaximumJsonProperties(119);
        limits.SetMaximumVisits(120);
        limits.SetMaximumDiagnostics(121);
        limits.SetMaximumOutputCharacters(122);

        Assert.Multiple(() =>
        {
            Assert.That(limits.GetMaximumInputBytes(), Is.EqualTo(101));
            Assert.That(limits.GetMaximumInputCharacters(), Is.EqualTo(102));
            Assert.That(limits.GetMaximumTokens(), Is.EqualTo(103));
            Assert.That(limits.GetMaximumAttributesPerTag(), Is.EqualTo(104));
            Assert.That(limits.GetMaximumTokenCharacters(), Is.EqualTo(105));
            Assert.That(limits.GetMaximumNameCharacters(), Is.EqualTo(106));
            Assert.That(limits.GetMaximumAttributeValueCharacters(), Is.EqualTo(107));
            Assert.That(limits.GetMaximumNodes(), Is.EqualTo(108));
            Assert.That(limits.GetMaximumDepth(), Is.EqualTo(109));
            Assert.That(limits.GetMaximumSelectorComplexity(), Is.EqualTo(110));
            Assert.That(limits.GetMaximumSelectorResults(), Is.EqualTo(111));
            Assert.That(limits.GetMaximumHasWork(), Is.EqualTo(112));
            Assert.That(limits.GetMaximumCssTokens(), Is.EqualTo(113));
            Assert.That(limits.GetMaximumCssDeclarations(), Is.EqualTo(114));
            Assert.That(limits.GetMaximumUrlCharacters(), Is.EqualTo(115));
            Assert.That(limits.GetMaximumDataUriBytes(), Is.EqualTo(116));
            Assert.That(limits.GetMaximumTableCells(), Is.EqualTo(117));
            Assert.That(limits.GetMaximumJsonDepth(), Is.EqualTo(118));
            Assert.That(limits.GetMaximumJsonProperties(), Is.EqualTo(119));
            Assert.That(limits.GetMaximumVisits(), Is.EqualTo(120));
            Assert.That(limits.GetMaximumDiagnostics(), Is.EqualTo(121));
            Assert.That(limits.GetMaximumOutputCharacters(), Is.EqualTo(122));
        });
    }

    [Test]
    public void CopyIsIndependent()
    {
        HtmlLimits original = HtmlLimits.CreateUntrusted();
        original.SetMaximumNodes(1234);

        HtmlLimits copy = original.Copy();
        copy.SetMaximumNodes(5678);

        Assert.That(original.GetMaximumNodes(), Is.EqualTo(1234));
        Assert.That(copy.GetMaximumNodes(), Is.EqualTo(5678));
    }

    [Test]
    public void SettersRejectNonPositiveValues()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();

        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumInputBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumInputCharacters(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumTokens(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumAttributesPerTag(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumTokenCharacters(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumNameCharacters(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumAttributeValueCharacters(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumNodes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumDepth(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumSelectorComplexity(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumSelectorResults(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumHasWork(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumCssTokens(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumCssDeclarations(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumUrlCharacters(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumDataUriBytes(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumTableCells(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumJsonDepth(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumJsonProperties(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumVisits(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumDiagnostics(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => limits.SetMaximumOutputCharacters(0));
    }

    [Test]
    public void BudgetSharesCumulativeUseAndDoesNotSpendFailedWork()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(10);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);

        Assert.That(budget.TryConsume(HtmlLimitKind.Visits, 4), Is.True);
        Assert.That(budget.TryConsume(HtmlLimitKind.Visits, 6), Is.True);
        Assert.That(budget.GetUsed(HtmlLimitKind.Visits), Is.EqualTo(10));
        Assert.That(budget.TryConsume(HtmlLimitKind.Visits, 1), Is.False);
        Assert.That(budget.GetUsed(HtmlLimitKind.Visits), Is.EqualTo(10));
        Assert.That(budget.HasExceededLimit(), Is.True);
        Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.Visits));
    }

    [Test]
    public void BudgetSnapshotsLimitsAndTracksFirstExceededCategory()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumNodes(2);
        limits.SetMaximumDepth(3);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        limits.SetMaximumNodes(100);

        Assert.That(budget.TryConsume(HtmlLimitKind.Nodes, 2), Is.True);
        Assert.That(budget.TryConsume(HtmlLimitKind.Nodes, 1), Is.False);
        Assert.That(budget.Allows(HtmlLimitKind.Depth, 4), Is.False);
        Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.Nodes));
        Assert.That(budget.GetLimits().GetMaximumNodes(), Is.EqualTo(2));
    }

    [Test]
    public void BudgetRejectsInvalidAccountingRequests()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);

        Assert.Throws<ArgumentOutOfRangeException>(() => budget.TryConsume(HtmlLimitKind.Visits, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => budget.Allows(HtmlLimitKind.Depth, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => budget.TryConsume(HtmlLimitKind.None, 1));
    }

    [Test]
    public void BudgetAccountsForEveryLimitCategory()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        (HtmlLimitKind Kind, int Maximum)[] cases =
        [
            (HtmlLimitKind.InputBytes, limits.GetMaximumInputBytes()),
            (HtmlLimitKind.InputCharacters, limits.GetMaximumInputCharacters()),
            (HtmlLimitKind.Tokens, limits.GetMaximumTokens()),
            (HtmlLimitKind.AttributesPerTag, limits.GetMaximumAttributesPerTag()),
            (HtmlLimitKind.TokenCharacters, limits.GetMaximumTokenCharacters()),
            (HtmlLimitKind.NameCharacters, limits.GetMaximumNameCharacters()),
            (HtmlLimitKind.AttributeValueCharacters, limits.GetMaximumAttributeValueCharacters()),
            (HtmlLimitKind.Nodes, limits.GetMaximumNodes()),
            (HtmlLimitKind.Depth, limits.GetMaximumDepth()),
            (HtmlLimitKind.SelectorComplexity, limits.GetMaximumSelectorComplexity()),
            (HtmlLimitKind.SelectorResults, limits.GetMaximumSelectorResults()),
            (HtmlLimitKind.HasWork, limits.GetMaximumHasWork()),
            (HtmlLimitKind.CssTokens, limits.GetMaximumCssTokens()),
            (HtmlLimitKind.CssDeclarations, limits.GetMaximumCssDeclarations()),
            (HtmlLimitKind.UrlCharacters, limits.GetMaximumUrlCharacters()),
            (HtmlLimitKind.DataUriBytes, limits.GetMaximumDataUriBytes()),
            (HtmlLimitKind.TableCells, limits.GetMaximumTableCells()),
            (HtmlLimitKind.JsonDepth, limits.GetMaximumJsonDepth()),
            (HtmlLimitKind.JsonProperties, limits.GetMaximumJsonProperties()),
            (HtmlLimitKind.Visits, limits.GetMaximumVisits()),
            (HtmlLimitKind.Diagnostics, limits.GetMaximumDiagnostics()),
            (HtmlLimitKind.OutputCharacters, limits.GetMaximumOutputCharacters())
        ];
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);

        foreach ((HtmlLimitKind Kind, int Maximum) item in cases)
        {
            Assert.That(budget.TryConsume(item.Kind, item.Maximum), Is.True, item.Kind.ToString());
            Assert.That(budget.GetUsed(item.Kind), Is.EqualTo(item.Maximum), item.Kind.ToString());
            Assert.That(budget.TryConsume(item.Kind, 1), Is.False, item.Kind.ToString());
        }
    }

    [Test]
    public void BudgetAccountsWithoutIntegerOverflow()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(int.MaxValue);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);

        Assert.That(budget.TryConsume(HtmlLimitKind.Visits, int.MaxValue), Is.True);
        Assert.That(budget.GetUsed(HtmlLimitKind.Visits), Is.EqualTo(int.MaxValue));
        Assert.That(budget.TryConsume(HtmlLimitKind.Visits, 1), Is.False);
        Assert.That(budget.GetUsed(HtmlLimitKind.Visits), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void CombinedSecurityLimitsLeaveNoPartialSanitizerOutput()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(24);
        limits.SetMaximumTokens(3);
        limits.SetMaximumNodes(3);
        limits.SetMaximumDepth(1);
        limits.SetMaximumDiagnostics(1);
        limits.SetMaximumOutputCharacters(8);
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetLimits(limits);

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(
            "<div><p onclick=x>safe</p><script>bad()</script></div>",
            HtmlSanitizerPolicy.CreateRichText().Compile(), options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetNode(), Is.Null);
            Assert.That(result.GetHtml(), Is.Empty);
            Assert.That(result.GetReport().WasLimitExceeded(), Is.True);
        });
    }

    [Test]
    public void CombinedConverterLimitsReturnBoundedSafeTruncation()
    {
        HtmlNode fragment = Html.ParseFragment(
            "<section><p>one <strong>two</strong></p>"
            + "<table><tr><td>a</td><td>b</td></tr></table></section>",
            "body").GetFragment();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumVisits(4);
        limits.SetMaximumDepth(2);
        limits.SetMaximumTableCells(1);
        limits.SetMaximumDiagnostics(1);
        limits.SetMaximumOutputCharacters(6);
        HtmlTextOptions textOptions = HtmlTextOptions.CreateDefault();
        textOptions.SetLimits(limits);
        textOptions.SetTruncationMarker("~");
        HtmlMarkdownOptions markdownOptions =
            HtmlMarkdownOptions.CreateGitHubFlavored();
        markdownOptions.SetLimits(limits);
        markdownOptions.SetTruncationMarker("~");

        HtmlTextResult text = HtmlTextConverter.Convert(fragment,
            textOptions, CancellationToken.None);
        HtmlMarkdownResult markdown = HtmlMarkdownConverter.Convert(fragment,
            markdownOptions, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(text.IsSuccess(), Is.True);
            Assert.That(text.IsComplete(), Is.False);
            Assert.That(text.WasTruncated(), Is.True);
            Assert.That(text.GetText().Length, Is.LessThanOrEqualTo(6));
            Assert.That(text.GetText(), Does.EndWith("~"));
            Assert.That(markdown.IsSuccess(), Is.True);
            Assert.That(markdown.IsComplete(), Is.False);
            Assert.That(markdown.WasTruncated(), Is.True);
            Assert.That(markdown.GetMarkdown().Length,
                Is.LessThanOrEqualTo(6));
            Assert.That(markdown.GetMarkdown(), Does.EndWith("~"));
        });
    }

    [Test]
    public void BudgetChecksCancellationForEveryOperation()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), source.Token);
        source.Cancel();

        Assert.Throws<OperationCanceledException>(budget.CheckCancellation);
        Assert.Throws<OperationCanceledException>(() => budget.TryConsume(HtmlLimitKind.Tokens, 0));
        Assert.Throws<OperationCanceledException>(() => budget.Allows(HtmlLimitKind.Depth, 0));
    }
}
