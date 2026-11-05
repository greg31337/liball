
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSelectorParserTests
{
    [Test]
    public void ParsesSelectorListsCompoundsAndPresenceAttributes()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            "svg|a#main.primary[data-id], *|*#other, "
            + "|button[|disabled], ns|item[*|href]").Parse();

        HtmlComplexSelectorAst[] selectors = result.GetSelectorList()
            .GetSelectors();
        HtmlSimpleSelectorAst[] first = GetSimpleSelectors(selectors[0]);
        HtmlSimpleSelectorAst[] second = GetSimpleSelectors(selectors[1]);
        HtmlSimpleSelectorAst[] third = GetSimpleSelectors(selectors[2]);
        HtmlSimpleSelectorAst[] fourth = GetSimpleSelectors(selectors[3]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDiagnostics(), Is.Empty);
            Assert.That(selectors, Has.Length.EqualTo(4));
            Assert.That(first.Select(value => value.GetKind()), Is.EqualTo(
                new[]
                {
                    HtmlSelectorSimpleKind.Type,
                    HtmlSelectorSimpleKind.Id,
                    HtmlSelectorSimpleKind.Class,
                    HtmlSelectorSimpleKind.Attribute
                }));
            AssertQualifiedName(first[0].GetQualifiedName(),
                HtmlSelectorNamespaceKind.Prefix, "svg", "a", false);
            Assert.That(first[1].GetName(), Is.EqualTo("main"));
            Assert.That(first[2].GetName(), Is.EqualTo("primary"));
            AssertQualifiedName(first[3].GetQualifiedName(),
                HtmlSelectorNamespaceKind.Unspecified, "", "data-id", false);
            Assert.That(first[3].GetAttributeOperator(),
                Is.EqualTo(HtmlSelectorAttributeOperator.Exists));
            AssertQualifiedName(second[0].GetQualifiedName(),
                HtmlSelectorNamespaceKind.Any, "", "", true);
            Assert.That(second[1].GetName(), Is.EqualTo("other"));
            AssertQualifiedName(third[0].GetQualifiedName(),
                HtmlSelectorNamespaceKind.None, "", "button", false);
            AssertQualifiedName(third[1].GetQualifiedName(),
                HtmlSelectorNamespaceKind.None, "", "disabled", false);
            AssertQualifiedName(fourth[0].GetQualifiedName(),
                HtmlSelectorNamespaceKind.Prefix, "ns", "item", false);
            AssertQualifiedName(fourth[1].GetQualifiedName(),
                HtmlSelectorNamespaceKind.Any, "", "href", false);
        });
    }

    [Test]
    public void CompoundsCanOmitATypeAndRepeatSubclassSelectors()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            ".alpha.beta#one#two[data-a][data-b]").Parse();

        HtmlSimpleSelectorAst[] simple = GetSimpleSelectors(
            result.GetSelectorList().GetSelectors()[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(simple.Select(value => value.GetKind()), Is.EqualTo(
                new[]
                {
                    HtmlSelectorSimpleKind.Class,
                    HtmlSelectorSimpleKind.Class,
                    HtmlSelectorSimpleKind.Id,
                    HtmlSelectorSimpleKind.Id,
                    HtmlSelectorSimpleKind.Attribute,
                    HtmlSelectorSimpleKind.Attribute
                }));
            Assert.That(simple.Select(value => value.GetName()).Take(4),
                Is.EqualTo(new[] { "alpha", "beta", "one", "two" }));
            Assert.That(simple.Any(value =>
                value.GetKind() == HtmlSelectorSimpleKind.Universal), Is.False);
        });
    }

    [Test]
    public void QualifiedTypeFormsPreserveNamespaceIntent()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            "item, *, ns|item, ns|*, *|item, *|*, |item, |*").Parse();

        HtmlComplexSelectorAst[] selectors = result.GetSelectorList()
            .GetSelectors();
        HtmlSelectorQualifiedNameAst[] names = selectors.Select(selector =>
            GetSimpleSelectors(selector)[0].GetQualifiedName()).ToArray();

        Assert.Multiple(() =>
        {
            AssertQualifiedName(names[0],
                HtmlSelectorNamespaceKind.Unspecified, "", "item", false);
            AssertQualifiedName(names[1],
                HtmlSelectorNamespaceKind.Unspecified, "", "", true);
            AssertQualifiedName(names[2],
                HtmlSelectorNamespaceKind.Prefix, "ns", "item", false);
            AssertQualifiedName(names[3],
                HtmlSelectorNamespaceKind.Prefix, "ns", "", true);
            AssertQualifiedName(names[4],
                HtmlSelectorNamespaceKind.Any, "", "item", false);
            AssertQualifiedName(names[5],
                HtmlSelectorNamespaceKind.Any, "", "", true);
            AssertQualifiedName(names[6],
                HtmlSelectorNamespaceKind.None, "", "item", false);
            AssertQualifiedName(names[7],
                HtmlSelectorNamespaceKind.None, "", "", true);
        });
    }

    [Test]
    public void EscapesAndCommentsFlowIntoDecodedAstNames()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            @"svg|my\:tag/**/.foo\+bar#\69 d[da\74 a]").Parse();

        HtmlSimpleSelectorAst[] simple = GetSimpleSelectors(
            result.GetSelectorList().GetSelectors()[0]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            AssertQualifiedName(simple[0].GetQualifiedName(),
                HtmlSelectorNamespaceKind.Prefix, "svg", "my:tag", false);
            Assert.That(simple[1].GetName(), Is.EqualTo("foo+bar"));
            Assert.That(simple[2].GetName(), Is.EqualTo("id"));
            Assert.That(simple[3].GetQualifiedName().GetLocalName(),
                Is.EqualTo("data"));
        });
    }

    [Test]
    public void AstCollectionsAndDiagnosticsAreSnapshots()
    {
        HtmlSelectorParseResult parsed = HtmlSelectorParser.Create(
            "div.a,span.b").Parse();
        HtmlSelectorListAst list = parsed.GetSelectorList();
        HtmlComplexSelectorAst[] selectors = list.GetSelectors();
        HtmlComplexSelectorAst originalFirst = selectors[0];
        HtmlCompoundSelectorAst[] compounds = originalFirst.GetCompounds();
        HtmlCompoundSelectorAst originalCompound = compounds[0];
        HtmlSimpleSelectorAst[] simple = originalCompound.GetSimpleSelectors();
        HtmlSimpleSelectorAst originalSimple = simple[0];
        selectors[0] = selectors[1];
        compounds[0] = selectors[1].GetCompounds()[0];
        simple[0] = simple[1];

        HtmlSelectorParseResult failed = HtmlSelectorParser.Create("div,").Parse();
        HtmlDiagnostic[] diagnostics = failed.GetDiagnostics();
        diagnostics[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(list.GetSelectors()[0], Is.SameAs(originalFirst));
            Assert.That(originalFirst.GetCompounds()[0],
                Is.SameAs(originalCompound));
            Assert.That(originalCompound.GetSimpleSelectors()[0],
                Is.SameAs(originalSimple));
            Assert.That(originalFirst.GetCombinators(), Is.Empty);
            Assert.That(failed.GetDiagnostics()[0], Is.Not.Null);
            Assert.That(failed.GetSelectorList(), Is.Null);
            Assert.That(failed.IsSuccess(), Is.False);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(",div")]
    [TestCase("div,")]
    [TestCase("div,,span")]
    [TestCase("#123")]
    [TestCase(".")]
    [TestCase("div.")]
    [TestCase("div*#id")]
    public void RejectsMalformedListsAndCompounds(string selector)
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

    [TestCase("svg|")]
    [TestCase("*|")]
    [TestCase("|")]
    [TestCase("div|.class")]
    [TestCase("[ns|]")]
    [TestCase("[*]")]
    [TestCase("[ns|*]")]
    public void RejectsMalformedQualifiedNames(string selector)
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(selector)
            .Parse();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.GetSelectorList(), Is.Null);
            Assert.That(result.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void AllowsAttributeWhitespaceButNotCompoundWhitespace()
    {
        HtmlSelectorParseResult valid = HtmlSelectorParser.Create(
            "  div[ data-id ][ *|href ] , [ |hidden ]  ").Parse();
        HtmlSelectorParseResult invalidClass = HtmlSelectorParser.Create(
            "div. class").Parse();
        HtmlSelectorParseResult invalidNamespace = HtmlSelectorParser.Create(
            "svg | item").Parse();

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsSuccess(), Is.True);
            Assert.That(valid.GetSelectorList().GetSelectors(),
                Has.Length.EqualTo(2));
            Assert.That(invalidClass.IsSuccess(), Is.False);
            Assert.That(invalidNamespace.IsSuccess(), Is.False);
        });
    }

    [Test]
    public void SourceSpansExcludeOuterWhitespaceAndMapOriginalOffsets()
    {
        HtmlSelectorParseResult result = HtmlSelectorParser.Create(
            " \r\ndiv.a,\r\n [data-x] ").Parse();

        HtmlSelectorListAst list = result.GetSelectorList();
        HtmlComplexSelectorAst[] selectors = list.GetSelectors();
        HtmlSimpleSelectorAst[] first = GetSimpleSelectors(selectors[0]);

        Assert.Multiple(() =>
        {
            Assert.That(list.GetSourceSpan().GetStart().GetCharacterOffset(),
                Is.EqualTo(3));
            Assert.That(list.GetSourceSpan().GetStart().GetLine(), Is.EqualTo(2));
            Assert.That(list.GetSourceSpan().GetStart().GetColumn(), Is.EqualTo(1));
            Assert.That(list.GetSourceSpan().GetEnd().GetCharacterOffset(),
                Is.EqualTo(20));
            Assert.That(first[0].GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(3));
            Assert.That(first[1].GetSourceSpan().GetCharacterLength(),
                Is.EqualTo(2));
            Assert.That(selectors[1].GetSourceSpan().GetStart().GetLine(),
                Is.EqualTo(3));
        });
    }

    [Test]
    public void LexerDiagnosticsMakeTheParseFailAndRetainTheirSpan()
    {
        HtmlSelectorParser parser = HtmlSelectorParser.Create("name\\");

        HtmlSelectorParseResult first = parser.Parse();
        HtmlSelectorParseResult second = parser.Parse();
        HtmlDiagnostic diagnostic = first.GetDiagnostics()[0];

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(second));
            Assert.That(first.IsSuccess(), Is.False);
            Assert.That(first.GetSelectorList(), Is.Null);
            Assert.That(diagnostic.GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.SelectorInvalidSyntax));
            Assert.That(diagnostic.GetSpan().GetStart().GetCharacterOffset(),
                Is.Zero);
            Assert.That(diagnostic.GetSpan().GetEnd().GetCharacterOffset(),
                Is.EqualTo(5));
        });
    }

    static HtmlSimpleSelectorAst[] GetSimpleSelectors(
        HtmlComplexSelectorAst selector)
    {
        Assert.That(selector.GetCompounds(), Has.Length.EqualTo(1));
        return selector.GetCompounds()[0].GetSimpleSelectors();
    }

    static void AssertQualifiedName(HtmlSelectorQualifiedNameAst name,
        HtmlSelectorNamespaceKind namespaceKind, string namespacePrefix,
        string localName, bool universal)
    {
        Assert.Multiple(() =>
        {
            Assert.That(name.GetNamespaceKind(), Is.EqualTo(namespaceKind));
            Assert.That(name.GetNamespacePrefix(), Is.EqualTo(namespacePrefix));
            Assert.That(name.GetLocalName(), Is.EqualTo(localName));
            Assert.That(name.IsUniversal(), Is.EqualTo(universal));
            Assert.That(name.GetSourceSpan().IsKnown(), Is.True);
        });
    }
}
