using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorHasTests
{
    [Test]
    public void ParsesImplicitAndExplicitLeadingCombinators()
    {
        HtmlSimpleSelectorAst has = ParseHas(
            ":has(.descendant, > .child, + .next, ~ .later)");
        HtmlRelativeSelectorAst[] arguments =
            has.GetRelativeSelectorArguments();

        Assert.Multiple(() =>
        {
            Assert.That(arguments, Has.Length.EqualTo(4));
            Assert.That(arguments.Select(value =>
                value.GetLeadingCombinator()), Is.EqualTo(new[]
                {
                    HtmlSelectorCombinator.Descendant,
                    HtmlSelectorCombinator.Child,
                    HtmlSelectorCombinator.NextSibling,
                    HtmlSelectorCombinator.SubsequentSibling
                }));
            Assert.That(arguments.Select(value => GetOnlyClass(
                value.GetSelector())), Is.EqualTo(new[]
                {
                    "descendant", "child", "next", "later"
                }));
        });
    }

    [Test]
    public void ParsesComplexRelativeSelectorsAndPreservesSpans()
    {
        string source = @"  :\68 as(> section .item + span) ";
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(source)
            .Parse();
        HtmlSimpleSelectorAst has = GetOnlySimple(result);
        HtmlRelativeSelectorAst relative =
            has.GetRelativeSelectorArguments()[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(has.GetPseudoClassKind(),
                Is.EqualTo(HtmlSelectorPseudoClassKind.Has));
            Assert.That(relative.GetLeadingCombinator(),
                Is.EqualTo(HtmlSelectorCombinator.Child));
            Assert.That(relative.GetSelector().GetCombinators(),
                Is.EqualTo(new[]
                {
                    HtmlSelectorCombinator.Descendant,
                    HtmlSelectorCombinator.NextSibling
                }));
            Assert.That(has.GetSourceSpan().GetStart().GetCharacterOffset(),
                Is.EqualTo(2));
            Assert.That(has.GetSourceSpan().GetEnd().GetCharacterOffset(),
                Is.EqualTo(source.LastIndexOf(')') + 1));
            Assert.That(relative.GetSourceSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(source.IndexOf('>')));
            Assert.That(relative.GetSourceSpan().GetEnd()
                .GetCharacterOffset(), Is.EqualTo(source.IndexOf(')')));
        });
    }

    [TestCase(":has()")]
    [TestCase(":has(,)")]
    [TestCase(":has(.a,)")]
    [TestCase(":has(.a,,.b)")]
    [TestCase(":has(>)")]
    [TestCase(":has(.a")]
    [TestCase(":has(:has(.x))")]
    public void RejectsMalformedAndNestedHasArguments(string selector)
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(selector)
            .Parse();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetSelectorList(), Is.Null);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(result.GetDiagnostics()[0].GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorInvalidSyntax));
        });
    }

    [Test]
    public void ReportsLeadingColumnCombinatorAsUnsupported()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            ":has(|| .cell)").Parse();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(result.GetDiagnostics()[0].GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorUnsupportedFeature));
            Assert.That(result.GetDiagnostics()[0].GetIdentifier(),
                Is.EqualTo("UH05002"));
        });
    }

    [Test]
    public void ForgivingLogicalListsCanDiscardNestedHasBranches()
    {
        HtmlSelectorParseResult inside = HtmlSelectorParser.Create(
            ":has(:is(:has(.x), .safe))").Parse();
        HtmlSelectorParseResult outside = HtmlSelectorParser.Create(
            ":is(:has(:has(.x)), .safe)").Parse();
        HtmlSimpleSelectorAst nestedIs = GetFirstSimple(
            ParseHas(inside).GetRelativeSelectorArguments()[0]
                .GetSelector());
        HtmlSimpleSelectorAst outerIs = GetOnlySimple(outside);

        Assert.Multiple(() =>
        {
            Assert.That(inside.IsSuccess(), Is.True);
            Assert.That(inside.GetDiagnostics(), Is.Empty);
            Assert.That(nestedIs.GetSelectorArguments(),
                Has.Length.EqualTo(1));
            Assert.That(GetOnlyClass(nestedIs.GetSelectorArguments()[0]),
                Is.EqualTo("safe"));
            Assert.That(outside.IsSuccess(), Is.True);
            Assert.That(outside.GetDiagnostics(), Is.Empty);
            Assert.That(outerIs.GetSelectorArguments(),
                Has.Length.EqualTo(1));
            Assert.That(GetOnlyClass(outerIs.GetSelectorArguments()[0]),
                Is.EqualTo("safe"));
        });
    }

    [Test]
    public void RelativeArgumentCollectionsAreSnapshots()
    {
        HtmlSimpleSelectorAst has = ParseHas(":has(.a,.b)");
        HtmlRelativeSelectorAst[] arguments =
            has.GetRelativeSelectorArguments();
        HtmlRelativeSelectorAst first = arguments[0];
        arguments[0] = arguments[1];

        Assert.That(has.GetRelativeSelectorArguments()[0],
            Is.SameAs(first));
    }

    [Test]
    public void MatchesAllRelativeCombinatorsAndComplexChains()
    {
        HasFixture fixture = CreateFixture();

        Assert.Multiple(() =>
        {
            Assert.That(Matches(fixture.Anchor, ":has(.deep)"), Is.True);
            Assert.That(Matches(fixture.Anchor, ":has(> .child)"),
                Is.True);
            Assert.That(Matches(fixture.Anchor, ":has(+ aside.next)"),
                Is.True);
            Assert.That(Matches(fixture.Anchor, ":has(~ footer.later)"),
                Is.True);
            Assert.That(Matches(fixture.Anchor,
                ":has(> section.child > span.deep)"), Is.True);
            Assert.That(Matches(fixture.Anchor,
                ":has(> .missing, + .next)"), Is.True);
            Assert.That(Matches(fixture.Anchor, ":has(> .deep)"),
                Is.False);
            Assert.That(Matches(fixture.Anchor, ":has(+ footer)"),
                Is.False);
            Assert.That(Matches(fixture.Anchor, ":has(~ header)"),
                Is.False);
        });
    }

    [Test]
    public void MatchesSimpleLogicalAttributeAndStructuralSelectors()
    {
        HasFixture fixture = CreateFixture();

        Assert.Multiple(() =>
        {
            Assert.That(Matches(fixture.Anchor,
                ":has(> section.child[data-role=ITEM i]:first-child)"),
                Is.True);
            Assert.That(Matches(fixture.Anchor,
                ":has(> :is(.other, .child))"), Is.True);
            Assert.That(Matches(fixture.Anchor,
                ":has(> .child:is(div > .child))"), Is.True);
            Assert.That(Matches(fixture.Anchor,
                ":has(> .child:not([data-role=wrong]))"), Is.True);
            Assert.That(Matches(fixture.Anchor,
                ":has(> .child:only-child)"), Is.False);
        });
    }

    [Test]
    public void WorkBudgetHasAnExactSuccessfulBoundary()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode anchor = document.CreateElement("div");
        HtmlNode hit = document.CreateElement("span");
        hit.SetAttribute("class", "hit");
        anchor.InitializeChildrenStorage(new[] { hit });
        HtmlSimpleSelectorAst has = ParseHas(":has(> .hit)");
        HtmlBudget exact = CreateBudget(4);
        HtmlBudget shortBudget = CreateBudget(3);

        bool exactMatch = HtmlSelectorMatcher.MatchesHas(anchor, has,
            exact);
        bool shortMatch = HtmlSelectorMatcher.MatchesHas(anchor, has,
            shortBudget);

        Assert.Multiple(() =>
        {
            Assert.That(exactMatch, Is.True);
            Assert.That(exact.HasExceededLimit(), Is.False);
            Assert.That(exact.GetUsed(HtmlLimitKind.HasWork), Is.EqualTo(4));
            Assert.That(shortMatch, Is.False);
            Assert.That(shortBudget.HasExceededLimit(), Is.True);
            Assert.That(shortBudget.GetFirstExceededLimit(),
                Is.EqualTo(HtmlLimitKind.HasWork));
            Assert.That(shortBudget.GetUsed(HtmlLimitKind.HasWork),
                Is.EqualTo(3));
        });
    }

    [Test]
    public void ExhaustionStopsTraversalDeterministically()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode anchor = document.CreateElement("div");
        anchor.InitializeChildrenStorage(new[]
        {
            document.CreateText("one"),
            document.CreateComment("two"),
            document.CreateElement("span")
        });
        HtmlBudget limited = CreateBudget(1);
        HtmlBudget sufficient = CreateBudget(100);
        HtmlSimpleSelectorAst has = ParseHas(":has(.missing)");

        bool limitedMatch = HtmlSelectorMatcher.MatchesHas(anchor, has,
            limited);
        bool sufficientMatch = HtmlSelectorMatcher.MatchesHas(anchor, has,
            sufficient);

        Assert.Multiple(() =>
        {
            Assert.That(limitedMatch, Is.False);
            Assert.That(limited.HasExceededLimit(), Is.True);
            Assert.That(limited.GetUsed(HtmlLimitKind.HasWork),
                Is.EqualTo(1));
            Assert.That(sufficientMatch, Is.False);
            Assert.That(sufficient.HasExceededLimit(), Is.False);
        });
    }

    [Test]
    public void EveryRelativeSelectorBranchConsumesWork()
    {
        HtmlNode anchor = CreateDocument().CreateElement("div");
        HtmlSimpleSelectorAst has = ParseHas(
            ":has(> .missing, + .also-missing)");
        HtmlBudget limited = CreateBudget(1);

        bool match = HtmlSelectorMatcher.MatchesHas(anchor, has, limited);

        Assert.Multiple(() =>
        {
            Assert.That(match, Is.False);
            Assert.That(limited.HasExceededLimit(), Is.True);
            Assert.That(limited.GetFirstExceededLimit(),
                Is.EqualTo(HtmlLimitKind.HasWork));
            Assert.That(limited.GetUsed(HtmlLimitKind.HasWork),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void NestedStructuralTraversalUsesTheSameWorkBudget()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode anchor = document.CreateElement("div");
        HtmlNode candidate = document.CreateElement("span");
        anchor.InitializeChildrenStorage(new[]
        {
            candidate, document.CreateText("one"),
            document.CreateComment("two")
        });
        HtmlSimpleSelectorAst has = ParseHas(
            ":has(> span:last-child)");
        HtmlBudget limited = CreateBudget(5);
        HtmlBudget sufficient = CreateBudget(100);

        bool limitedMatch = HtmlSelectorMatcher.MatchesHas(anchor, has,
            limited);
        bool sufficientMatch = HtmlSelectorMatcher.MatchesHas(anchor, has,
            sufficient);

        Assert.Multiple(() =>
        {
            Assert.That(limitedMatch, Is.False);
            Assert.That(limited.HasExceededLimit(), Is.True);
            Assert.That(sufficientMatch, Is.True);
            Assert.That(sufficient.HasExceededLimit(), Is.False);
        });
    }

    [Test]
    public void MatchingObservesCancellationAtWorkBoundaries()
    {
        HasFixture fixture = CreateFixture();
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(),
            source.Token);

        Assert.Throws<OperationCanceledException>(() =>
            HtmlSelectorMatcher.MatchesHas(fixture.Anchor,
                ParseHas(":has(.deep)"), budget));
    }

    static bool Matches(HtmlNode anchor, string selector)
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(),
            CancellationToken.None);
        bool result = HtmlSelectorMatcher.MatchesHas(anchor,
            ParseHas(selector), budget);
        Assert.That(budget.HasExceededLimit(), Is.False);
        return result;
    }

    static HtmlSimpleSelectorAst ParseHas(string selector)
    {
        return ParseHas(HtmlSelectorParser.Create(selector).Parse());
    }

    static HtmlSimpleSelectorAst ParseHas(HtmlSelectorParseResult result)
    {
        HtmlSimpleSelectorAst has = GetOnlySimple(result);
        Assert.That(has.GetPseudoClassKind(),
            Is.EqualTo(HtmlSelectorPseudoClassKind.Has));
        return has;
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

    static string GetOnlyClass(HtmlComplexSelectorAst selector)
    {
        HtmlSimpleSelectorAst simple = GetFirstSimple(selector);
        Assert.That(simple.GetKind(),
            Is.EqualTo(HtmlSelectorSimpleKind.Class));
        return simple.GetName();
    }

    static HtmlBudget CreateBudget(int maximumHasWork)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumHasWork(maximumHasWork);
        return new HtmlBudget(limits, CancellationToken.None);
    }

    static HasFixture CreateFixture()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("main");
        HtmlNode anchor = document.CreateElement("div");
        HtmlNode child = document.CreateElement("section");
        HtmlNode deep = document.CreateElement("span");
        HtmlNode other = document.CreateElement("p");
        HtmlNode next = document.CreateElement("aside");
        HtmlNode later = document.CreateElement("footer");
        anchor.SetAttribute("class", "anchor");
        child.SetAttribute("class", "child");
        child.SetAttribute("data-role", "item");
        deep.SetAttribute("class", "deep");
        other.SetAttribute("class", "other");
        next.SetAttribute("class", "next");
        later.SetAttribute("class", "later");
        child.InitializeChildrenStorage(new[] { deep });
        anchor.InitializeChildrenStorage(new[] { child, other });
        parent.InitializeChildrenStorage(new[]
        {
            anchor, document.CreateText("gap"), next,
            document.CreateComment("gap"), later
        });
        return new HasFixture(anchor);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, new HtmlDiagnostic[0],
            HtmlSourceSpan.CreateUnknown());
    }
}

class HasFixture
{
    public HasFixture(HtmlNode anchor)
    {
        this.Anchor = anchor;
    }

    public HtmlNode Anchor { get; }
}
