
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorStructuralPseudoTests
{
    [Test]
    public void ParsesSimpleStructuralPseudoClasses()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            ":root:scope:empty:first-child:last-child:only-child"
            + ":first-of-type:last-of-type:only-of-type").Parse();

        HtmlSimpleSelectorAst[] simple = GetOnlyCompound(result);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(simple.Select(value => value.GetKind()).All(value =>
                value == HtmlSelectorSimpleKind.PseudoClass), Is.True);
            Assert.That(simple.Select(value => value.GetPseudoClassKind()),
                Is.EqualTo(new[]
                {
                    HtmlSelectorPseudoClassKind.Root,
                    HtmlSelectorPseudoClassKind.Scope,
                    HtmlSelectorPseudoClassKind.Empty,
                    HtmlSelectorPseudoClassKind.FirstChild,
                    HtmlSelectorPseudoClassKind.LastChild,
                    HtmlSelectorPseudoClassKind.OnlyChild,
                    HtmlSelectorPseudoClassKind.FirstOfType,
                    HtmlSelectorPseudoClassKind.LastOfType,
                    HtmlSelectorPseudoClassKind.OnlyOfType
                }));
            Assert.That(simple.Select(value => value.GetNthExpression()),
                Is.All.Null);
        });
    }

    [Test]
    public void ParsesFunctionalPseudoClassesAndDecodedCssEscapes()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            @":nth-child(odd),:nth-last-child(-n + 2),"
            + @":nth-of-type(+3n-2),:nth-last-of-type(5),"
            + @":NTH-CHILD(\6f dd),:nth-child(2\6e+1),"
            + @":f\69rst-child").Parse();

        HtmlComplexSelectorAst[] selectors = result.GetSelectorList()
            .GetSelectors();
        HtmlSimpleSelectorAst[] pseudo = selectors.Select(selector =>
            selector.GetCompounds()[0].GetSimpleSelectors()[0]).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(pseudo.Select(value => value.GetPseudoClassKind()),
                Is.EqualTo(new[]
                {
                    HtmlSelectorPseudoClassKind.NthChild,
                    HtmlSelectorPseudoClassKind.NthLastChild,
                    HtmlSelectorPseudoClassKind.NthOfType,
                    HtmlSelectorPseudoClassKind.NthLastOfType,
                    HtmlSelectorPseudoClassKind.NthChild,
                    HtmlSelectorPseudoClassKind.NthChild,
                    HtmlSelectorPseudoClassKind.FirstChild
                }));
            Assert.That(pseudo.Take(6).Select(value => new[]
            {
                value.GetNthExpression().GetStep(),
                value.GetNthExpression().GetOffset()
            }), Is.EqualTo(new[]
            {
                new[] { 2, 1 }, new[] { -1, 2 }, new[] { 3, -2 },
                new[] { 0, 5 }, new[] { 2, 1 }, new[] { 2, 1 }
            }));
        });
    }

    [Test]
    public void PseudoClassSpansCoverTheColonThroughTheClosingToken()
    {
        string source = "  div:nth-child(2n + 1) ";
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(source)
            .Parse();
        HtmlSimpleSelectorAst pseudo = GetOnlyCompound(result)[1];
        int start = source.IndexOf(':');
        int end = source.LastIndexOf(')') + 1;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(pseudo.GetSourceSpan().GetStart()
                .GetCharacterOffset(), Is.EqualTo(start));
            Assert.That(pseudo.GetSourceSpan().GetEnd()
                .GetCharacterOffset(), Is.EqualTo(end));
            Assert.That(pseudo.GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(end - start));
        });
    }

    [TestCase(":")]
    [TestCase(":unknown")]
    [TestCase(":root()")]
    [TestCase(":first-child(1)")]
    [TestCase(":nth-child")]
    [TestCase(":nth-child()")]
    [TestCase(":nth-child(3 n)")]
    [TestCase(":nth-child(n+)")]
    [TestCase(":nth-child(2n+1x)")]
    [TestCase(":nth-child(2n+1 of .item)")]
    [TestCase(":nth-child(2147483648n)")]
    [TestCase(":nth-child(2n+1")]
    public void RejectsMalformedOrUnsupportedPseudoClasses(string selector)
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
    public void MatchesRootScopeAndLevelFourEmptySemantics()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode body = document.CreateElement("body");
        HtmlNode whitespaceOnly = document.CreateElement("div");
        HtmlNode nonbreakingSpace = document.CreateElement("div");
        HtmlNode childElement = document.CreateElement("div");
        HtmlNode fragment = document.CreateFragment();
        whitespaceOnly.InitializeChildrenStorage(new[]
        {
            document.CreateComment("ignored"),
            document.CreateText(" \t\r\n\f")
        });
        nonbreakingSpace.InitializeChildrenStorage(new[]
        {
            document.CreateText("\u00A0")
        });
        childElement.InitializeChildrenStorage(new[]
        {
            document.CreateElement("span")
        });
        body.InitializeChildrenStorage(new[]
        {
            whitespaceOnly, nonbreakingSpace, childElement
        });
        html.InitializeChildrenStorage(new[] { body });
        document.GetRoot().InitializeChildrenStorage(new[] { html });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(html, HtmlSelectorPseudoClassKind.Root),
                Is.True);
            Assert.That(Matches(body, HtmlSelectorPseudoClassKind.Root),
                Is.False);
            Assert.That(Matches(html, HtmlSelectorPseudoClassKind.Scope,
                null, document.GetRoot()), Is.True);
            Assert.That(Matches(body, HtmlSelectorPseudoClassKind.Scope,
                null, body), Is.True);
            Assert.That(Matches(body, HtmlSelectorPseudoClassKind.Scope),
                Is.False);
            Assert.That(Matches(body, HtmlSelectorPseudoClassKind.Scope,
                null, fragment), Is.False);
            Assert.That(Matches(whitespaceOnly,
                HtmlSelectorPseudoClassKind.Empty), Is.True);
            Assert.That(Matches(nonbreakingSpace,
                HtmlSelectorPseudoClassKind.Empty), Is.False);
            Assert.That(Matches(childElement,
                HtmlSelectorPseudoClassKind.Empty), Is.False);
        });
    }

    [Test]
    public void MatchesChildAndOfTypePositionsUsingElementSiblingsOnly()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode parent = document.CreateElement("div");
        HtmlNode firstParagraph = document.CreateElement("p");
        HtmlNode span = document.CreateElement("span");
        HtmlNode secondParagraph = document.CreateElement("p");
        HtmlNode svgParagraph = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "p");
        HtmlNode thirdParagraph = document.CreateElement("p");
        parent.InitializeChildrenStorage(new[]
        {
            document.CreateText("ignored"), firstParagraph,
            document.CreateComment("ignored"), span, secondParagraph,
            svgParagraph, thirdParagraph
        });

        Assert.Multiple(() =>
        {
            Assert.That(Matches(firstParagraph,
                HtmlSelectorPseudoClassKind.FirstChild), Is.True);
            Assert.That(Matches(thirdParagraph,
                HtmlSelectorPseudoClassKind.LastChild), Is.True);
            Assert.That(Matches(span,
                HtmlSelectorPseudoClassKind.OnlyChild), Is.False);
            Assert.That(Matches(secondParagraph,
                HtmlSelectorPseudoClassKind.NthChild, Parse("3")), Is.True);
            Assert.That(Matches(secondParagraph,
                HtmlSelectorPseudoClassKind.NthLastChild, Parse("3")),
                Is.True);
            Assert.That(Matches(firstParagraph,
                HtmlSelectorPseudoClassKind.FirstOfType), Is.True);
            Assert.That(Matches(thirdParagraph,
                HtmlSelectorPseudoClassKind.LastOfType), Is.True);
            Assert.That(Matches(span,
                HtmlSelectorPseudoClassKind.OnlyOfType), Is.True);
            Assert.That(Matches(svgParagraph,
                HtmlSelectorPseudoClassKind.OnlyOfType), Is.True);
            Assert.That(Matches(secondParagraph,
                HtmlSelectorPseudoClassKind.NthOfType, Parse("2")), Is.True);
            Assert.That(Matches(secondParagraph,
                HtmlSelectorPseudoClassKind.NthLastOfType, Parse("2")),
                Is.True);
        });
    }

    [Test]
    public void ParentlessElementsAreTheirOnlyInclusiveSibling()
    {
        HtmlNode detached = CreateDocument().CreateElement("div");

        Assert.Multiple(() =>
        {
            Assert.That(Matches(detached,
                HtmlSelectorPseudoClassKind.FirstChild), Is.True);
            Assert.That(Matches(detached,
                HtmlSelectorPseudoClassKind.LastChild), Is.True);
            Assert.That(Matches(detached,
                HtmlSelectorPseudoClassKind.OnlyChild), Is.True);
            Assert.That(Matches(detached,
                HtmlSelectorPseudoClassKind.NthChild, Parse("1")), Is.True);
        });
    }

    static HtmlSimpleSelectorAst[] GetOnlyCompound(
        HtmlSelectorParseResult result)
    {
        HtmlComplexSelectorAst[] selectors = result.GetSelectorList()
            .GetSelectors();
        Assert.That(selectors, Has.Length.EqualTo(1));
        HtmlCompoundSelectorAst[] compounds = selectors[0].GetCompounds();
        Assert.That(compounds, Has.Length.EqualTo(1));
        return compounds[0].GetSimpleSelectors();
    }

    static bool Matches(HtmlNode node,
        HtmlSelectorPseudoClassKind pseudoClassKind)
    {
        return Matches(node, pseudoClassKind, null, null);
    }

    static bool Matches(HtmlNode node,
        HtmlSelectorPseudoClassKind pseudoClassKind,
        HtmlNthExpression nthExpression)
    {
        return Matches(node, pseudoClassKind, nthExpression, null);
    }

    static bool Matches(HtmlNode node,
        HtmlSelectorPseudoClassKind pseudoClassKind,
        HtmlNthExpression nthExpression, HtmlNode scopeRoot)
    {
        return HtmlSelectorStructuralMatcher.Matches(node, pseudoClassKind,
            nthExpression, scopeRoot);
    }

    static HtmlNthExpression Parse(string value)
    {
        HtmlNthExpressionParseResult result = HtmlNthExpression.Parse(value);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetExpression();
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, new HtmlDiagnostic[0],
            HtmlSourceSpan.CreateUnknown());
    }
}
