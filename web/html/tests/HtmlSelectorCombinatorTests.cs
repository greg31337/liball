
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorCombinatorTests
{
    [Test]
    public void ParsesSupportedCombinatorsInSourceOrder()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            "main > section.item + aside ~ footer a").Parse();

        HtmlComplexSelectorAst selector = result.GetSelectorList()
            .GetSelectors()[0];
        HtmlCompoundSelectorAst[] compounds = selector.GetCompounds();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(compounds, Has.Length.EqualTo(5));
            Assert.That(selector.GetCombinators(), Is.EqualTo(new[]
            {
                HtmlSelectorCombinator.Child,
                HtmlSelectorCombinator.NextSibling,
                HtmlSelectorCombinator.SubsequentSibling,
                HtmlSelectorCombinator.Descendant
            }));
            Assert.That(compounds.Select(GetTypeName), Is.EqualTo(new[]
            {
                "main", "section", "aside", "footer", "a"
            }));
            Assert.That(compounds[1].GetSimpleSelectors(),
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void AllowsCombinatorWhitespaceAndComments()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            "a>b, a + b, a~ b, a\tb, a\r\nb, a/**/>/**/b").Parse();

        HtmlComplexSelectorAst[] selectors = result.GetSelectorList()
            .GetSelectors();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(selectors, Has.Length.EqualTo(6));
            Assert.That(GetOnlyCombinator(selectors[0]),
                Is.EqualTo(HtmlSelectorCombinator.Child));
            Assert.That(GetOnlyCombinator(selectors[1]),
                Is.EqualTo(HtmlSelectorCombinator.NextSibling));
            Assert.That(GetOnlyCombinator(selectors[2]),
                Is.EqualTo(HtmlSelectorCombinator.SubsequentSibling));
            Assert.That(GetOnlyCombinator(selectors[3]),
                Is.EqualTo(HtmlSelectorCombinator.Descendant));
            Assert.That(GetOnlyCombinator(selectors[4]),
                Is.EqualTo(HtmlSelectorCombinator.Descendant));
            Assert.That(GetOnlyCombinator(selectors[5]),
                Is.EqualTo(HtmlSelectorCombinator.Child));
        });
    }

    [Test]
    public void KeepsNamespaceSeparatorsDistinctFromCombinators()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            "svg|a > *|b |c").Parse();

        HtmlComplexSelectorAst selector = result.GetSelectorList()
            .GetSelectors()[0];
        HtmlCompoundSelectorAst[] compounds = selector.GetCompounds();
        HtmlSelectorQualifiedNameAst first = GetTypeNameAst(compounds[0]);
        HtmlSelectorQualifiedNameAst second = GetTypeNameAst(compounds[1]);
        HtmlSelectorQualifiedNameAst third = GetTypeNameAst(compounds[2]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(selector.GetCombinators(), Is.EqualTo(new[]
            {
                HtmlSelectorCombinator.Child,
                HtmlSelectorCombinator.Descendant
            }));
            Assert.That(first.GetNamespaceKind(),
                Is.EqualTo(HtmlSelectorNamespaceKind.Prefix));
            Assert.That(first.GetNamespacePrefix(), Is.EqualTo("svg"));
            Assert.That(second.GetNamespaceKind(),
                Is.EqualTo(HtmlSelectorNamespaceKind.Any));
            Assert.That(third.GetNamespaceKind(),
                Is.EqualTo(HtmlSelectorNamespaceKind.None));
            Assert.That(new[]
            {
                first.GetLocalName(), second.GetLocalName(),
                third.GetLocalName()
            }, Is.EqualTo(new[] { "a", "b", "c" }));
        });
    }

    [TestCase("col||cell", 3)]
    [TestCase("col || cell", 4)]
    [TestCase("ns|col||cell", 6)]
    [TestCase("a > b||c", 5)]
    [TestCase("a, b || c", 5)]
    public void ReportsColumnCombinatorsAsAStableUnsupportedFeature(
        string selector, int offset)
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(selector)
            .Parse();
        HtmlDiagnostic diagnostic = result.GetDiagnostics()[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetSelectorList(), Is.Null);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorUnsupportedFeature));
            Assert.That(diagnostic.GetIdentifier(), Is.EqualTo("UH05002"));
            Assert.That(diagnostic.GetSeverity(),
                Is.EqualTo(HtmlDiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Is.EqualTo(
                "The CSS column combinator '||' is not supported."));
            Assert.That(diagnostic.GetSpan().GetStart().GetCharacterOffset(),
                Is.EqualTo(offset));
            Assert.That(diagnostic.GetSpan().GetCharacterLength(),
                Is.EqualTo(2));
        });
    }

    [TestCase("> a")]
    [TestCase("a >")]
    [TestCase("a > > b")]
    [TestCase("a + ~ b")]
    [TestCase("a ~ , b")]
    [TestCase("a b >")]
    [TestCase("a/**/b")]
    public void RejectsMalformedCombinatorSequences(string selector)
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
    public void ComplexSelectorSpansAndCollectionsAreSnapshots()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            " \r\na > b + c ~ d e ").Parse();
        HtmlComplexSelectorAst selector = result.GetSelectorList()
            .GetSelectors()[0];
        HtmlCompoundSelectorAst[] compounds = selector.GetCompounds();
        HtmlSelectorCombinator[] combinators = selector.GetCombinators();
        HtmlCompoundSelectorAst first = compounds[0];
        compounds[0] = compounds[1];
        combinators[0] = HtmlSelectorCombinator.Descendant;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(selector.GetCompounds()[0], Is.SameAs(first));
            Assert.That(selector.GetCombinators()[0],
                Is.EqualTo(HtmlSelectorCombinator.Child));
            Assert.That(selector.GetSourceSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(3));
            Assert.That(selector.GetSourceSpan().GetEnd()
                .GetCharacterOffset(), Is.EqualTo(18));
            Assert.That(selector.GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(15));
        });
    }

    static HtmlSelectorCombinator GetOnlyCombinator(
        HtmlComplexSelectorAst selector)
    {
        Assert.That(selector.GetCombinators(), Has.Length.EqualTo(1));
        return selector.GetCombinators()[0];
    }

    static string GetTypeName(HtmlCompoundSelectorAst compound)
    {
        return GetTypeNameAst(compound).GetLocalName();
    }

    static HtmlSelectorQualifiedNameAst GetTypeNameAst(
        HtmlCompoundSelectorAst compound)
    {
        HtmlSimpleSelectorAst simple = compound.GetSimpleSelectors()[0];
        Assert.That(simple.GetKind(), Is.EqualTo(
            HtmlSelectorSimpleKind.Type));
        return simple.GetQualifiedName();
    }
}
