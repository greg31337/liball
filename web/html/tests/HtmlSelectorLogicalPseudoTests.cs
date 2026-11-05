using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorLogicalPseudoTests
{
    [Test]
    public void ParsesNestedLogicalComplexSelectorLists()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            ":is(.a, section > .item, :where(#x, :unknown), "
            + ":not(.hidden, article))").Parse();

        HtmlSimpleSelectorAst outer = GetOnlySimple(result);
        HtmlComplexSelectorAst[] arguments = outer.GetSelectorArguments();
        HtmlSimpleSelectorAst where = GetFirstSimple(arguments[2]);
        HtmlSimpleSelectorAst not = GetFirstSimple(arguments[3]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDiagnostics(), Is.Empty);
            Assert.That(outer.GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Is));
            Assert.That(arguments, Has.Length.EqualTo(4));
            Assert.That(arguments[1].GetCombinators(), Is.EqualTo(new[]
            {
                HtmlSelectorCombinator.Child
            }));
            Assert.That(where.GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Where));
            Assert.That(where.GetSelectorArguments(), Has.Length.EqualTo(1));
            Assert.That(not.GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Not));
            Assert.That(not.GetSelectorArguments(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ForgivingListsDropInvalidBranchesWithoutDiagnostics()
    {
        HtmlSelectorParseResult mixed = HtmlSelectorParser.Create(
            ":is(.a, :unknown, :future(.x,.y), div||span, .b)").Parse();
        HtmlSelectorParseResult empty = HtmlSelectorParser.Create(
            ":where(:unknown, ::before, )").Parse();
        HtmlSelectorParseResult omitted = HtmlSelectorParser.Create(
            ":is(,,.kept,,)").Parse();

        HtmlComplexSelectorAst[] mixedArguments = GetOnlySimple(mixed)
            .GetSelectorArguments();
        HtmlComplexSelectorAst[] emptyArguments = GetOnlySimple(empty)
            .GetSelectorArguments();
        HtmlComplexSelectorAst[] omittedArguments = GetOnlySimple(omitted)
            .GetSelectorArguments();

        Assert.Multiple(() =>
        {
            Assert.That(mixed.IsSuccess(), Is.True);
            Assert.That(mixed.GetDiagnostics(), Is.Empty);
            Assert.That(mixedArguments, Has.Length.EqualTo(2));
            Assert.That(GetFirstSimple(mixedArguments[0]).GetName(),
                Is.EqualTo("a"));
            Assert.That(GetFirstSimple(mixedArguments[1]).GetName(),
                Is.EqualTo("b"));
            Assert.That(empty.IsSuccess(), Is.True);
            Assert.That(empty.GetDiagnostics(), Is.Empty);
            Assert.That(emptyArguments, Is.Empty);
            Assert.That(omitted.IsSuccess(), Is.True);
            Assert.That(omittedArguments, Has.Length.EqualTo(1));
            Assert.That(GetFirstSimple(omittedArguments[0]).GetName(),
                Is.EqualTo("kept"));
        });
    }

    [Test]
    public void NotUsesAStrictListButCanContainForgivingPseudoClasses()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            ":not(.a, section > .item, :is(#kept, :unknown))").Parse();

        HtmlSimpleSelectorAst not = GetOnlySimple(result);
        HtmlSimpleSelectorAst nestedIs = GetFirstSimple(
            not.GetSelectorArguments()[2]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(not.GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Not));
            Assert.That(not.GetSelectorArguments(), Has.Length.EqualTo(3));
            Assert.That(nestedIs.GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Is));
            Assert.That(nestedIs.GetSelectorArguments(),
                Has.Length.EqualTo(1));
        });
    }

    [TestCase(".a, :unknown, .b")]
    [TestCase(":not()")]
    [TestCase(":not(,)")]
    [TestCase(":not(.a,)")]
    [TestCase(":not(.a,,.b)")]
    [TestCase(":not(.a, :unknown, .b)")]
    [TestCase(":not(.a")]
    public void StrictSelectorListsRejectAnyInvalidOrEmptyBranch(
        string selector)
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(selector)
            .Parse();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetSelectorList(), Is.Null);
            Assert.That(result.GetDiagnostics(), Is.Not.Empty);
            Assert.That(result.GetDiagnostics()[0].GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorInvalidSyntax));
        });
    }

    [Test]
    public void LogicalNamesAndArgumentsPreserveEscapesAndSourceSpans()
    {
        string source = @"  :\69s(.a, .b) ";
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(source)
            .Parse();

        HtmlSimpleSelectorAst logical = GetOnlySimple(result);
        HtmlComplexSelectorAst second = logical.GetSelectorArguments()[1];
        int secondStart = source.IndexOf(".b", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(logical.GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Is));
            Assert.That(logical.GetSourceSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(2));
            Assert.That(logical.GetSourceSpan().GetEnd()
                .GetCharacterOffset(), Is.EqualTo(source.LastIndexOf(')') + 1));
            Assert.That(second.GetSourceSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(secondStart));
        });
    }

    [Test]
    public void LogicalArgumentCollectionsAreSnapshots()
    {
        HtmlSimpleSelectorAst logical = GetOnlySimple(
            HtmlSelectorParser.Create(":is(.a,.b)").Parse());
        HtmlComplexSelectorAst[] arguments = logical.GetSelectorArguments();
        HtmlComplexSelectorAst first = arguments[0];
        arguments[0] = arguments[1];

        Assert.That(logical.GetSelectorArguments()[0], Is.SameAs(first));
    }

    [Test]
    public void LogicalMatchingUsesDisjunctionAndNegationSemantics()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlSelectorLogicalMatcher.Matches(
                HtmlSelectorPseudoClassKind.Is,
                new[] { false, true, false }), Is.True);
            Assert.That(HtmlSelectorLogicalMatcher.Matches(
                HtmlSelectorPseudoClassKind.Where,
                new[] { false, false }), Is.False);
            Assert.That(HtmlSelectorLogicalMatcher.Matches(
                HtmlSelectorPseudoClassKind.Is, new bool[0]), Is.False);
            Assert.That(HtmlSelectorLogicalMatcher.Matches(
                HtmlSelectorPseudoClassKind.Not,
                new[] { false, false }), Is.True);
            Assert.That(HtmlSelectorLogicalMatcher.Matches(
                HtmlSelectorPseudoClassKind.Not,
                new[] { false, true }), Is.False);
            Assert.Throws<ArgumentException>(() =>
                HtmlSelectorLogicalMatcher.Matches(
                    HtmlSelectorPseudoClassKind.Not, new bool[0]));
        });
    }

    [Test]
    public void ConfiguredComplexityLimitCountsNestedAndInvalidBranches()
    {
        HtmlLimits exactLimits = HtmlLimits.CreateDefault();
        exactLimits.SetMaximumSelectorComplexity(5);
        HtmlLimits exceededLimits = HtmlLimits.CreateDefault();
        exceededLimits.SetMaximumSelectorComplexity(5);
        HtmlLimits invalidBranchLimits = HtmlLimits.CreateDefault();
        invalidBranchLimits.SetMaximumSelectorComplexity(3);

        HtmlSelectorParseResult exact = HtmlSelectorParser.Create(
            ":is(.a,.b)", exactLimits).Parse();
        HtmlSelectorParseResult exceeded = HtmlSelectorParser.Create(
            ":is(.a,.b,.c)", exceededLimits).Parse();
        HtmlSelectorParseResult invalidBranches = HtmlSelectorParser.Create(
            ":is(:unknown,:unknown,.a)", invalidBranchLimits).Parse();

        Assert.Multiple(() =>
        {
            Assert.That(exact.IsSuccess(), Is.True);
            AssertComplexityExceeded(exceeded);
            AssertComplexityExceeded(invalidBranches);
        });
    }

    [Test]
    public void LogicalNestingHasAStackSafeLimit()
    {
        HtmlSelectorParseResult withinLimit = HtmlSelectorParser.Create(
            CreateNestedSelector(128)).Parse();
        HtmlSelectorParseResult exceeded = HtmlSelectorParser.Create(
            CreateNestedSelector(129)).Parse();

        Assert.Multiple(() =>
        {
            Assert.That(withinLimit.IsSuccess(), Is.True);
            AssertComplexityExceeded(exceeded);
        });
    }

    [Test]
    public void LogicalAstRejectsInvalidArgumentStates()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreateLogicalPseudoClass(
                    HtmlSelectorPseudoClassKind.Root,
                    new HtmlComplexSelectorAst[0], span));
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreateLogicalPseudoClass(
                    HtmlSelectorPseudoClassKind.Not,
                    new HtmlComplexSelectorAst[0], span));
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreatePseudoClass(
                    HtmlSelectorPseudoClassKind.Is, null, span));
        });
    }

    static HtmlSimpleSelectorAst GetOnlySimple(
        HtmlSelectorParseResult result)
    {
        Assert.That(result.IsSuccess(), Is.True);
        HtmlComplexSelectorAst[] selectors = result.GetSelectorList()
            .GetSelectors();
        Assert.That(selectors, Has.Length.EqualTo(1));
        HtmlCompoundSelectorAst[] compounds = selectors[0].GetCompounds();
        Assert.That(compounds, Has.Length.EqualTo(1));
        HtmlSimpleSelectorAst[] simple = compounds[0].GetSimpleSelectors();
        Assert.That(simple, Has.Length.EqualTo(1));
        return simple[0];
    }

    static HtmlSimpleSelectorAst GetFirstSimple(
        HtmlComplexSelectorAst selector)
    {
        return selector.GetCompounds()[0].GetSimpleSelectors()[0];
    }

    static string CreateNestedSelector(int depth)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < depth)
        {
            value.Append(":is(");
            index = index + 1;
        }

        value.Append(".a");
        index = 0;
        while (index < depth)
        {
            value.Append(')');
            index = index + 1;
        }

        return value.ToString();
    }

    static void AssertComplexityExceeded(HtmlSelectorParseResult result)
    {
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetSelectorList(), Is.Null);
        Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
        Assert.That(result.GetDiagnostics()[0].GetCode(),
            Is.EqualTo(HtmlDiagnosticCode.SelectorComplexityExceeded));
    }
}
