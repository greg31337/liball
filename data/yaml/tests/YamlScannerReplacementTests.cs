using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlScannerReplacementTests
{
    [Test]
    public void Scan_FlowAdjacentValues_RecognizesColonsOnlyAfterJsonLikeKeys()
    {
        string text = "[\"json\":adjacent, plain:inside, {nested: value}:after, \"literal\"::value]\n";

        global::yaml.YamlScannerResult result = Scan(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Count(item => item.Kind == global::yaml.YamlTokenKind.BlockMappingValue), Is.EqualTo(4));
        Assert.That(PlainValues(result), Is.EqualTo(new string[]
        {
            "adjacent",
            "plain:inside",
            "nested",
            "value",
            "after",
            ":value"
        }));
        Assert.That(result.Tokens.Where(item => item.Kind == global::yaml.YamlTokenKind.BlockMappingValue).Select(item => SpanText(text, item)), Is.EqualTo(new string[] { ":", ":", ":", ":" }));
    }

    [Test]
    public void Scan_FlowExplicitKeys_DistinguishesQuestionIndicatorsFromPlainContent()
    {
        global::yaml.YamlScannerResult result = Scan("[? foo: bar, question? mark, ?key]\n");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Count(item => item.Kind == global::yaml.YamlTokenKind.BlockMappingKey), Is.EqualTo(1));
        Assert.That(PlainValues(result), Is.EqualTo(new string[] { "foo", "bar", "question? mark", "?key" }));
    }

    [TestCase("\"multi\n line\": value\n", false)]
    [TestCase("[\"multi\n line\": value]\n", false)]
    [TestCase("{\"multi\n line\": value}\n", true)]
    [TestCase("? \"multi\n line\"\n: value\n", true)]
    public void Scan_SimpleKeys_EnforcesMultilineRulesByContext(string text, bool expectedSuccess)
    {
        global::yaml.YamlScannerResult result = Scan(text);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
        Assert.That(result.Diagnostics.Any(item => item.Code == "YAML014"), Is.EqualTo(!expectedSuccess));
    }

    [Test]
    public void Scan_SimpleKeys_EnforcesThe1024UnicodeCharacterLimit()
    {
        string acceptedKey = new string('a', 1024);
        string rejectedKey = acceptedKey + "b";

        global::yaml.YamlScannerResult accepted = Scan(acceptedKey + ": value\n");
        global::yaml.YamlScannerResult rejected = Scan(rejectedKey + ": value\n");

        Assert.That(accepted.Success, Is.True);
        Assert.That(rejected.Success, Is.False);
        Assert.That(rejected.Diagnostics.Single(item => item.Code == "YAML014").Message, Does.Contain("1024"));
    }

    [TestCase("\tplain\n")]
    [TestCase("[\n\tplain\n]\n")]
    [TestCase("-\t-\n")]
    [TestCase("- \t-\n")]
    [TestCase("?\tkey:\n")]
    public void Scan_StructuralTabs_ReturnIndentationDiagnostics(string text)
    {
        global::yaml.YamlScannerResult result = Scan(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(item => item.Code == "YAML001"), Is.True);
    }

    [TestCase("\t{}\n")]
    [TestCase("[\n\t\n value\n]\n")]
    [TestCase("root:\n \tvalue\n")]
    [TestCase("first\n\n second \n\tthird\n")]
    public void Scan_LegalTabSeparation_RemainsAccepted(string text)
    {
        global::yaml.YamlScannerResult result = Scan(text);

        Assert.That(result.Success, Is.True, Diagnostics(result));
    }

    [Test]
    public void Scan_CompactCollectionsAfterExplicitIndicators_EmitNestedBlockTokens()
    {
        global::yaml.YamlScannerResult result = Scan("? - one\n: - two\n");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Count(item => item.Kind == global::yaml.YamlTokenKind.BlockMappingKey), Is.EqualTo(1));
        Assert.That(result.Tokens.Count(item => item.Kind == global::yaml.YamlTokenKind.BlockMappingValue), Is.EqualTo(1));
        Assert.That(result.Tokens.Count(item => item.Kind == global::yaml.YamlTokenKind.BlockSequenceEntry), Is.EqualTo(2));
        Assert.That(PlainValues(result), Is.EqualTo(new string[] { "one", "two" }));
    }

    [Test]
    public void Scan_DirectivesAndDocumentMarkers_RequireExactSourcePositionsAndBoundaries()
    {
        string text = "%YAML 1.2\n---\n %TAG ! !example!\n ---\n----\n...x\n...\n";

        global::yaml.YamlScannerResult result = Scan(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Tokens.Count(item => item.Kind == global::yaml.YamlTokenKind.Directive), Is.EqualTo(1));
        Assert.That(result.Tokens.Count(item => item.Kind == global::yaml.YamlTokenKind.DocumentStart), Is.EqualTo(1));
        Assert.That(result.Tokens.Count(item => item.Kind == global::yaml.YamlTokenKind.DocumentEnd), Is.EqualTo(1));
        Assert.That(result.Tokens.Any(item => item.Kind == global::yaml.YamlTokenKind.Error && item.Value == "%"), Is.True);
        Assert.That(PlainValues(result), Does.Contain("---"));
        Assert.That(PlainValues(result), Does.Contain("---- ...x"));
    }

    [Test]
    public void Scan_YamlPrintableUnicode_AcceptsNelReplacementAndAstralScalars()
    {
        string value = "printable\u0085\u00A0\uFFFD" + char.ConvertFromUtf32(128512);

        global::yaml.YamlScannerResult result = Scan(value + "\n");

        Assert.That(result.Success, Is.True);
        Assert.That(PlainValues(result), Is.EqualTo(new string[] { value }));
    }

    [Test]
    public void Scan_NonPrintableUnicodeAcrossTokenKinds_ReturnsPreciseDiagnostics()
    {
        string text = "plain\u0080\nquoted: \"\u0086\"\n# \u009F\nblock: |\n  \uD800\nlast: \uFFFE\uFFFF\n";

        global::yaml.YamlScannerResult result = Scan(text);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(item => item.Code), Is.All.EqualTo("YAML013"));
        Assert.That(result.Diagnostics.Select(item => item.Span.StartLine), Is.EqualTo(new int[] { 1, 2, 3, 5, 6, 6 }));
        Assert.That(result.Diagnostics.Select(item => item.Span.StartColumn), Is.EqualTo(new int[] { 6, 10, 3, 3, 7, 8 }));
        Assert.That(result.Diagnostics.Select(item => item.Span.EndIndex - item.Span.StartIndex), Is.All.EqualTo(1));
    }

    [Test]
    public void Scan_TriviaInsideScalars_IsNotEmittedAsStructure()
    {
        string text = "quoted: \"# value\"\n\nblock: |+\n  # content\n\n# outside\n";

        global::yaml.YamlScannerResult result = Scan(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Single(item => item.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar).Value, Is.EqualTo("# value"));
        Assert.That(result.Tokens.Single(item => item.Kind == global::yaml.YamlTokenKind.LiteralScalar).Value, Is.EqualTo("# content\n\n"));
        Assert.That(result.Tokens.Where(item => item.Kind == global::yaml.YamlTokenKind.BlankLine).Select(item => item.Span.StartLine), Is.EqualTo(new int[] { 2 }));
        Assert.That(result.Tokens.Where(item => item.Kind == global::yaml.YamlTokenKind.Comment).Select(item => item.Value), Is.EqualTo(new string[] { "# outside" }));
    }

    [Test]
    public void Scan_TriviaOptionOmitsCommentsAndBlankLinesTogether()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan("# comment\n\nvalue\n", options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Any(item => item.Kind == global::yaml.YamlTokenKind.Comment), Is.False);
        Assert.That(result.Tokens.Any(item => item.Kind == global::yaml.YamlTokenKind.BlankLine), Is.False);
        Assert.That(PlainValues(result), Is.EqualTo(new string[] { "value" }));
    }

    [Test]
    public void Scan_FlowIndentation_DoesNotChangeTheBlockIndentStack()
    {
        string text = "root: [\n  one,\n    {two: three}\n]\nend: done\n";

        global::yaml.YamlScannerResult result = Scan(text);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Tokens.Any(item => item.Kind == global::yaml.YamlTokenKind.Indent), Is.False);
        Assert.That(result.Tokens.Any(item => item.Kind == global::yaml.YamlTokenKind.Dedent), Is.False);
        Assert.That(result.Tokens.Single(item => item.Kind == global::yaml.YamlTokenKind.FlowMappingStart).Span.StartLine, Is.EqualTo(3));
        Assert.That(result.Tokens.Single(item => item.Kind == global::yaml.YamlTokenKind.FlowMappingStart).Span.StartColumn, Is.EqualTo(5));
    }

    static global::yaml.YamlScannerResult Scan(string text)
    {
        return global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());
    }

    static string[] PlainValues(global::yaml.YamlScannerResult result)
    {
        return result.Tokens.Where(item => item.Kind == global::yaml.YamlTokenKind.PlainScalar).Select(item => item.Value).ToArray();
    }

    static string SpanText(string text, global::yaml.YamlToken token)
    {
        return text.Substring(token.Span.StartIndex, token.Span.EndIndex - token.Span.StartIndex);
    }

    static string Diagnostics(global::yaml.YamlScannerResult result)
    {
        return string.Join("\n", result.Diagnostics.Select(item => item.Code + " " + item.Message));
    }
}
