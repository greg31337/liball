using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlBuiltInBlockCollectionConformanceTests
{
    public static IEnumerable<TestCaseData> BlockCollectionCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (IsBlockCollectionCase(testCase))
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    [TestCaseSource(nameof(BlockCollectionCases))]
    public void ParserBlockCollections_MatchPinnedEvents(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(scanner.Success, Is.True, testCase.Id + " scanner " + YamlTestSuiteConformanceTests.FormatDiagnostics(scanner.Diagnostics));
        Assert.That(result.Success, Is.True, testCase.Id + " parser " + YamlTestSuiteConformanceTests.FormatDiagnostics(result.Diagnostics));
        Assert.That(YamlTestSuiteConformanceTests.EventFixture(result.Events, testCase.InputYaml), Is.EqualTo(YamlTestSuiteConformanceTests.NormalizeLines(testCase.ExpectedEvents)), testCase.Id);
    }

    [TestCase("root:\n  child: value\n bad: value\n", "YAML002")]
    [TestCase("\"multi\n line\": value\n", "YAML014")]
    public void ParserBlockCollections_PropagateStructuralScannerDiagnostics(string text, string diagnosticCode)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = true;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(text, options);

        global::yaml.YamlParserResult result = global::yaml.YamlApi.Pipeline.ParseScannerResult(scanner, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(item => item.Code == diagnosticCode), Is.True);
    }

    [Test]
    public void ParserBlockCollections_PreserveNestedCollectionStylesAndSourceStarts()
    {
        string text = "- ? earth: blue\n  : moon: white\n";
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(text, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);
        List<global::yaml.YamlEvent> mappings = result.Events.Where(item => item.Kind == global::yaml.YamlEventKind.MappingStart).ToList();
        global::yaml.YamlEvent sequence = result.Events.Single(item => item.Kind == global::yaml.YamlEventKind.SequenceStart);

        Assert.That(result.Success, Is.True);
        Assert.That(sequence.CollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Block));
        Assert.That(sequence.Span.StartLine, Is.EqualTo(1));
        Assert.That(sequence.Span.StartColumn, Is.EqualTo(1));
        Assert.That(mappings.Select(item => item.CollectionStyle), Is.All.EqualTo(global::yaml.YamlCollectionStyle.Block));
        Assert.That(mappings.Select(item => item.Span.StartLine), Is.EqualTo(new int[] { 1, 1, 2 }));
        Assert.That(mappings.Select(item => item.Span.StartColumn), Is.EqualTo(new int[] { 3, 5, 5 }));
    }

    public static bool IsBlockCollectionCase(YamlTestSuiteCase testCase)
    {
        if (!testCase.ShouldParse)
        {
            return false;
        }

        string events = testCase.ExpectedEvents;
        bool hasBlockCollection = events.Contains("+SEQ\n") || events.Contains("+MAP\n");
        return hasBlockCollection
            && !events.Contains("+SEQ []")
            && !events.Contains("+MAP {}")
            && !events.Contains("=ALI")
            && !events.Contains("<")
            && !events.Contains("&")
            && !testCase.InputYaml.StartsWith("%");
    }
}
