
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorAttributeMatcherTests
{
    [Test]
    public void ParsesEveryAttributeOperatorAndCaseModifier()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            @"[a=plain][b~=""two words""][c|=en][d^=Pre i]"
            + @"[e$='SUF' S][f*=m\69 d \69]").Parse();

        HtmlSimpleSelectorAst[] simple = result.GetSelectorList()
            .GetSelectors()[0].GetCompounds()[0].GetSimpleSelectors();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(simple.Select(value => value.GetAttributeOperator()),
                Is.EqualTo(new[]
                {
                    HtmlSelectorAttributeOperator.Equals,
                    HtmlSelectorAttributeOperator.Includes,
                    HtmlSelectorAttributeOperator.DashMatch,
                    HtmlSelectorAttributeOperator.PrefixMatch,
                    HtmlSelectorAttributeOperator.SuffixMatch,
                    HtmlSelectorAttributeOperator.SubstringMatch
                }));
            Assert.That(simple.Select(value => value.GetAttributeValue()),
                Is.EqualTo(new[]
                {
                    "plain", "two words", "en", "Pre", "SUF", "mid"
                }));
            Assert.That(simple.Select(value => value.GetAttributeCase()),
                Is.EqualTo(new[]
                {
                    HtmlSelectorAttributeCase.Default,
                    HtmlSelectorAttributeCase.Default,
                    HtmlSelectorAttributeCase.Default,
                    HtmlSelectorAttributeCase.Insensitive,
                    HtmlSelectorAttributeCase.Sensitive,
                    HtmlSelectorAttributeCase.Insensitive
                }));
        });
    }

    [Test]
    public void DistinguishesDashMatchFromNamespaceSeparators()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            "[lang|=en][ns|lang|=fr][*|rel~=next][|type='A' s]")
            .Parse();

        HtmlSimpleSelectorAst[] simple = result.GetSelectorList()
            .GetSelectors()[0].GetCompounds()[0].GetSimpleSelectors();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(simple.Select(value => value.GetAttributeOperator()),
                Is.EqualTo(new[]
                {
                    HtmlSelectorAttributeOperator.DashMatch,
                    HtmlSelectorAttributeOperator.DashMatch,
                    HtmlSelectorAttributeOperator.Includes,
                    HtmlSelectorAttributeOperator.Equals
                }));
            Assert.That(simple[0].GetQualifiedName().GetNamespaceKind(),
                Is.EqualTo(HtmlSelectorNamespaceKind.Unspecified));
            Assert.That(simple[1].GetQualifiedName().GetNamespaceKind(),
                Is.EqualTo(HtmlSelectorNamespaceKind.Prefix));
            Assert.That(simple[1].GetQualifiedName().GetNamespacePrefix(),
                Is.EqualTo("ns"));
            Assert.That(simple[2].GetQualifiedName().GetNamespaceKind(),
                Is.EqualTo(HtmlSelectorNamespaceKind.Any));
            Assert.That(simple[3].GetQualifiedName().GetNamespaceKind(),
                Is.EqualTo(HtmlSelectorNamespaceKind.None));
            Assert.That(simple[3].GetAttributeCase(),
                Is.EqualTo(HtmlSelectorAttributeCase.Sensitive));
        });
    }

    [Test]
    public void AllowsWhitespaceAndCommentsAtAttributeGrammarBoundaries()
    {
        HtmlSelectorParseResult whitespace = HtmlSelectorParser.Create(
            "[ data-x ^= \"Ab\" i ]").Parse();
        HtmlSelectorParseResult comments = HtmlSelectorParser.Create(
            "[data-x~/**/=word/**/i]").Parse();

        HtmlSimpleSelectorAst first = GetOnlyAttribute(whitespace);
        HtmlSimpleSelectorAst second = GetOnlyAttribute(comments);

        Assert.Multiple(() =>
        {
            Assert.That(whitespace.IsSuccess(), Is.True);
            Assert.That(first.GetAttributeOperator(),
                Is.EqualTo(HtmlSelectorAttributeOperator.PrefixMatch));
            Assert.That(first.GetAttributeCase(),
                Is.EqualTo(HtmlSelectorAttributeCase.Insensitive));
            Assert.That(comments.IsSuccess(), Is.True);
            Assert.That(second.GetAttributeOperator(),
                Is.EqualTo(HtmlSelectorAttributeOperator.Includes));
            Assert.That(second.GetAttributeCase(),
                Is.EqualTo(HtmlSelectorAttributeCase.Insensitive));
        });
    }

    [TestCase("[x=]")]
    [TestCase("[x=123]")]
    [TestCase("[x~=word q]")]
    [TestCase("[x=word i s]")]
    [TestCase("[x i]")]
    [TestCase("[x~ =word]")]
    [TestCase("[x!=word]")]
    [TestCase("[x==word]")]
    [TestCase("[x=word 1]")]
    [TestCase("[x=word i extra]")]
    public void RejectsMalformedAttributeOperatorsAndModifiers(string selector)
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
    public void RecoversAttributeClosingBracketAtEndOfInput()
    {
        HtmlSelectorParseResult presence = HtmlSelectorParser.Create(
            "[present").Parse();
        HtmlSelectorParseResult value = HtmlSelectorParser.Create(
            "[x=word i").Parse();

        Assert.Multiple(() =>
        {
            Assert.That(presence.IsSuccess(), Is.True);
            Assert.That(GetOnlyAttribute(presence).GetAttributeOperator(),
                Is.EqualTo(HtmlSelectorAttributeOperator.Exists));
            Assert.That(value.IsSuccess(), Is.True);
            Assert.That(GetOnlyAttribute(value).GetAttributeValue(),
                Is.EqualTo("word"));
            Assert.That(GetOnlyAttribute(value).GetAttributeCase(),
                Is.EqualTo(HtmlSelectorAttributeCase.Insensitive));
        });
    }

    [Test]
    public void ResolvesTheCompleteHtmlAsciiInsensitiveAttributeTable()
    {
        string[] names = new[]
        {
            "accept", "accept-charset", "align", "alink", "axis",
            "bgcolor", "charset", "checked", "clear", "codetype",
            "color", "compact", "declare", "defer", "dir", "direction",
            "disabled", "enctype", "face", "frame", "hreflang",
            "http-equiv", "lang", "language", "link", "media", "method",
            "multiple", "nohref", "noresize", "noshade", "nowrap",
            "readonly", "rel", "rev", "rules", "scope", "scrolling",
            "selected", "shape", "target", "text", "type", "valign",
            "valuetype", "vlink"
        };

        Assert.Multiple(() =>
        {
            Assert.That(names.All(name =>
                HtmlSelectorAttributeMatcher.ResolveValueCase(
                    HtmlSelectorAttributeCase.Default, HtmlNamespace.Html,
                    "", name) == HtmlSelectorAttributeCase.Insensitive),
                Is.True);
            Assert.That(HtmlSelectorAttributeMatcher.ResolveValueCase(
                HtmlSelectorAttributeCase.Default, HtmlNamespace.Html, "",
                "TYPE"), Is.EqualTo(
                    HtmlSelectorAttributeCase.Insensitive));
            Assert.That(HtmlSelectorAttributeMatcher.ResolveValueCase(
                HtmlSelectorAttributeCase.Default, HtmlNamespace.Html, "",
                "title"), Is.EqualTo(HtmlSelectorAttributeCase.Sensitive));
            Assert.That(HtmlSelectorAttributeMatcher.ResolveValueCase(
                HtmlSelectorAttributeCase.Default, HtmlNamespace.Svg, "",
                "type"), Is.EqualTo(HtmlSelectorAttributeCase.Sensitive));
            Assert.That(HtmlSelectorAttributeMatcher.ResolveValueCase(
                HtmlSelectorAttributeCase.Default, HtmlNamespace.Html,
                "urn:example", "type"), Is.EqualTo(
                    HtmlSelectorAttributeCase.Sensitive));
        });
    }

    [Test]
    public void ExplicitModifiersOverrideHtmlAndForeignDefaults()
    {
        Assert.Multiple(() =>
        {
            Assert.That(HtmlSelectorAttributeMatcher.ResolveValueCase(
                HtmlSelectorAttributeCase.Sensitive, HtmlNamespace.Html, "",
                "type"), Is.EqualTo(HtmlSelectorAttributeCase.Sensitive));
            Assert.That(HtmlSelectorAttributeMatcher.ResolveValueCase(
                HtmlSelectorAttributeCase.Insensitive, HtmlNamespace.Svg, "",
                "viewBox"), Is.EqualTo(
                    HtmlSelectorAttributeCase.Insensitive));
            Assert.That(HtmlSelectorAttributeMatcher.ResolveValueCase(
                HtmlSelectorAttributeCase.Insensitive, HtmlNamespace.Html,
                "urn:example", "custom"), Is.EqualTo(
                    HtmlSelectorAttributeCase.Insensitive));
        });
    }

    [TestCase("value", 1, "value", true)]
    [TestCase("Value", 1, "value", false)]
    [TestCase("one\ttwo\r\nthree\ffour", 2, "three", true)]
    [TestCase("stone", 2, "one", false)]
    [TestCase("en-US", 3, "en", true)]
    [TestCase("english", 3, "en", false)]
    [TestCase("prefix", 4, "pre", true)]
    [TestCase("suffix", 5, "fix", true)]
    [TestCase("contains", 6, "tain", true)]
    [TestCase("value", 2, "", false)]
    [TestCase("value", 4, "", false)]
    [TestCase("value", 5, "", false)]
    [TestCase("value", 6, "", false)]
    public void AppliesAttributeOperatorSemantics(string attributeValue,
        int attributeOperator, string selectorValue, bool expected)
    {
        bool actual = HtmlSelectorAttributeMatcher.Matches(attributeValue,
            (HtmlSelectorAttributeOperator)attributeOperator, selectorValue,
            HtmlSelectorAttributeCase.Sensitive, HtmlNamespace.Html, "",
            "data-x");

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void MatchingUsesResolvedAsciiOnlyCaseSensitivity()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Matches("NEXT", HtmlSelectorAttributeOperator.Includes,
                "next", HtmlSelectorAttributeCase.Default, "rel"), Is.True);
            Assert.That(Matches("A", HtmlSelectorAttributeOperator.Equals,
                "a", HtmlSelectorAttributeCase.Default, "type"), Is.True);
            Assert.That(Matches("A", HtmlSelectorAttributeOperator.Equals,
                "a", HtmlSelectorAttributeCase.Sensitive, "type"), Is.False);
            Assert.That(Matches("Alpha", HtmlSelectorAttributeOperator.Equals,
                "alpha", HtmlSelectorAttributeCase.Default, "data-x"),
                Is.False);
            Assert.That(Matches("Alpha", HtmlSelectorAttributeOperator.Equals,
                "alpha", HtmlSelectorAttributeCase.Insensitive, "data-x"),
                Is.True);
            Assert.That(Matches("Ä", HtmlSelectorAttributeOperator.Equals,
                "ä", HtmlSelectorAttributeCase.Insensitive, "data-x"),
                Is.False);
            Assert.That(HtmlSelectorAttributeMatcher.Matches(null,
                HtmlSelectorAttributeOperator.Exists, "",
                HtmlSelectorAttributeCase.Default, HtmlNamespace.Html, "",
                "hidden"), Is.False);
            Assert.That(HtmlSelectorAttributeMatcher.Matches("",
                HtmlSelectorAttributeOperator.Exists, "",
                HtmlSelectorAttributeCase.Default, HtmlNamespace.Html, "",
                "hidden"), Is.True);
        });
    }

    [Test]
    public void AttributeAstRejectsInvalidOperatorAndModifierStates()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlSelectorQualifiedNameAst name =
            new HtmlSelectorQualifiedNameAst(
                HtmlSelectorNamespaceKind.Unspecified, "", "data-x", false,
                span);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreateAttribute(name,
                    HtmlSelectorAttributeOperator.Exists, "value",
                    HtmlSelectorAttributeCase.Default, span));
            Assert.Throws<ArgumentException>(() =>
                HtmlSimpleSelectorAst.CreateAttribute(name,
                    HtmlSelectorAttributeOperator.Exists, "",
                    HtmlSelectorAttributeCase.Insensitive, span));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HtmlSimpleSelectorAst.CreateAttribute(name,
                    (HtmlSelectorAttributeOperator)99, "value",
                    HtmlSelectorAttributeCase.Default, span));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                HtmlSimpleSelectorAst.CreateAttribute(name,
                    HtmlSelectorAttributeOperator.Equals, "value",
                    (HtmlSelectorAttributeCase)99, span));
        });
    }

    static HtmlSimpleSelectorAst GetOnlyAttribute(
        HtmlSelectorParseResult result)
    {
        HtmlSimpleSelectorAst[] simple = result.GetSelectorList()
            .GetSelectors()[0].GetCompounds()[0].GetSimpleSelectors();
        Assert.That(simple, Has.Length.EqualTo(1));
        Assert.That(simple[0].GetKind(),
            Is.EqualTo(HtmlSelectorSimpleKind.Attribute));
        return simple[0];
    }

    static bool Matches(string attributeValue,
        HtmlSelectorAttributeOperator attributeOperator, string selectorValue,
        HtmlSelectorAttributeCase attributeCase, string attributeName)
    {
        return HtmlSelectorAttributeMatcher.Matches(attributeValue,
            attributeOperator, selectorValue, attributeCase,
            HtmlNamespace.Html, "", attributeName);
    }
}
