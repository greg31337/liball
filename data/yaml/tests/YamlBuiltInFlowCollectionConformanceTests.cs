using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlBuiltInFlowCollectionConformanceTests
{
    public static readonly HashSet<string> MalformedFlowCollectionIds = new HashSet<string>
    {
        "4H7K",
        "6JTT",
        "9MAG",
        "C2SP",
        "CML9",
        "CTN5",
        "DK4H",
        "T833",
        "ZXT5"
    };

    public static IEnumerable<TestCaseData> FlowCollectionCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (IsFlowCollectionCase(testCase))
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    public static IEnumerable<TestCaseData> MalformedFlowCollectionCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (MalformedFlowCollectionIds.Contains(testCase.Id))
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    [TestCaseSource(nameof(FlowCollectionCases))]
    public void ParserFlowCollections_MatchPinnedEvents(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(scanner.Success, Is.True, testCase.Id + " scanner " + YamlTestSuiteConformanceTests.FormatDiagnostics(scanner.Diagnostics));
        Assert.That(result.Success, Is.True, testCase.Id + " parser " + YamlTestSuiteConformanceTests.FormatDiagnostics(result.Diagnostics));
        Assert.That(YamlTestSuiteConformanceTests.EventFixture(result.Events, testCase.InputYaml), Is.EqualTo(YamlTestSuiteConformanceTests.NormalizeLines(testCase.ExpectedEvents)), testCase.Id);
    }

    [TestCaseSource(nameof(MalformedFlowCollectionCases))]
    public void ParserFlowCollections_RejectMalformedGrammar(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(result.Success, Is.False, testCase.Id);
        Assert.That(result.Diagnostics, Is.Not.Empty, testCase.Id);
    }

    [Test]
    public void ParserFlowCollections_PreserveNestedStylesAndSourceStarts()
    {
        string text = "[one, {two: three}, key: [value,],]\n";
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(text, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);
        List<global::yaml.YamlEvent> sequences = result.Events.Where(item => item.Kind == global::yaml.YamlEventKind.SequenceStart).ToList();
        List<global::yaml.YamlEvent> mappings = result.Events.Where(item => item.Kind == global::yaml.YamlEventKind.MappingStart).ToList();

        Assert.That(result.Success, Is.True);
        Assert.That(sequences.Select(item => item.CollectionStyle), Is.All.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(sequences.Select(item => item.Span.StartColumn), Is.EqualTo(new int[] { 1, 26 }));
        Assert.That(mappings.Select(item => item.CollectionStyle), Is.All.EqualTo(global::yaml.YamlCollectionStyle.Flow));
        Assert.That(mappings.Select(item => item.Span.StartColumn), Is.EqualTo(new int[] { 7, 21 }));
    }

    public static bool IsFlowCollectionCase(YamlTestSuiteCase testCase)
    {
        if (!testCase.ShouldParse)
        {
            return false;
        }

        string events = testCase.ExpectedEvents;
        bool hasFlowCollection = events.Contains("+SEQ []") || events.Contains("+MAP {}");
        return hasFlowCollection
            && !events.Contains("=ALI")
            && !events.Contains("<")
            && !events.Contains("&")
            && !testCase.InputYaml.StartsWith("%");
    }
}
