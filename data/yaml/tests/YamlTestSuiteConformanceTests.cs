using System.Text.Json;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlTestSuiteConformanceTests
{
    public static IEnumerable<TestCaseData> AllCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
        }
    }

    public static IEnumerable<TestCaseData> ValidCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (testCase.ShouldParse)
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    public static IEnumerable<TestCaseData> InvalidCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (!testCase.ShouldParse)
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    public static IEnumerable<TestCaseData> JsonCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (testCase.ShouldParse && testCase.ExpectedJson != "")
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    public static IEnumerable<TestCaseData> EmitterCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (testCase.ShouldParse && testCase.EmitterYaml != "")
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    [TestCaseSource(nameof(AllCases))]
    public void PublicScanner_ReturnsBoundedOrderedTokens(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = ParserOptions();
        global::yaml.YamlScannerResult result = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);

        Assert.That(result.Tokens, Is.Not.Empty, testCase.Id);
        Assert.That(result.Tokens[0].Kind, Is.EqualTo(global::yaml.YamlTokenKind.StreamStart), testCase.Id);
        Assert.That(result.Tokens[result.Tokens.Count - 1].Kind, Is.EqualTo(global::yaml.YamlTokenKind.StreamEnd), testCase.Id);
        if (testCase.ShouldParse)
        {
            Assert.That(result.Success, Is.True, testCase.Id + " " + FormatDiagnostics(result.Diagnostics));
            Assert.That(result.Tokens.Any(item => item.Kind == global::yaml.YamlTokenKind.Error), Is.False, testCase.Id);
        }

        int previousStart = 0;
        foreach (global::yaml.YamlToken token in result.Tokens)
        {
            Assert.That(token.Span.StartIndex, Is.GreaterThanOrEqualTo(0), testCase.Id);
            Assert.That(token.Span.EndIndex, Is.GreaterThanOrEqualTo(token.Span.StartIndex), testCase.Id);
            Assert.That(token.Span.EndIndex, Is.LessThanOrEqualTo(testCase.InputYaml.Length), testCase.Id);
            Assert.That(token.Span.StartIndex, Is.GreaterThanOrEqualTo(previousStart), testCase.Id);
            previousStart = token.Span.StartIndex;
        }
    }

    [TestCaseSource(nameof(ValidCases))]
    public void PublicParser_MatchesPinnedEventFixture(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserResult result = ParseEvents(testCase.InputYaml);

        Assert.That(result.Success, Is.True, testCase.Id + " " + FormatDiagnostics(result.Diagnostics));
        Assert.That(EventFixture(result.Events, testCase.InputYaml), Is.EqualTo(NormalizeLines(testCase.ExpectedEvents)), testCase.Id);
    }

    [TestCaseSource(nameof(ValidCases))]
    public void StandaloneTokenParser_MatchesPinnedEventFixture(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = ParserOptions();
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);

        global::yaml.YamlParserResult result = global::yaml.YamlApi.Pipeline.ParseTokens(scanner.Tokens, options);

        Assert.That(scanner.Success, Is.True, testCase.Id + " scanner " + FormatDiagnostics(scanner.Diagnostics));
        Assert.That(result.Success, Is.True, testCase.Id + " parser " + FormatDiagnostics(result.Diagnostics));
        Assert.That(EventFixture(result.Events, testCase.InputYaml), Is.EqualTo(NormalizeLines(testCase.ExpectedEvents)), testCase.Id);
    }

    [TestCaseSource(nameof(InvalidCases))]
    public void PublicParser_InvalidCasesReturnStructuredDiagnostics(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = Processor().TryParseAll(testCase.InputYaml, result);

        Assert.That(success, Is.False, testCase.Id);
        Assert.That(result.Success, Is.False, testCase.Id);
        Assert.That(result.Value, Is.Null, testCase.Id);
        Assert.That(result.Diagnostics, Is.Not.Empty, testCase.Id);
        Assert.That(result.Diagnostics.Any(item => item.Severity == global::yaml.YamlDiagnosticSeverity.Error), Is.True, testCase.Id);
        Assert.That(result.Diagnostics.All(item => item.Code != "" && item.Message != ""), Is.True, testCase.Id);
    }

    [TestCaseSource(nameof(InvalidCases))]
    public void ScannerOrStandaloneTokenParser_RejectsEveryInvalidCase(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = ParserOptions();
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);

        global::yaml.YamlParserResult result = global::yaml.YamlApi.Pipeline.ParseTokens(scanner.Tokens, options);
        IReadOnlyList<global::yaml.YamlDiagnostic> diagnostics = scanner.Success ? result.Diagnostics : scanner.Diagnostics;

        Assert.That(scanner.Success && result.Success, Is.False, testCase.Id);
        Assert.That(diagnostics, Is.Not.Empty, testCase.Id);
        Assert.That(diagnostics.Any(item => item.Severity == global::yaml.YamlDiagnosticSeverity.Error), Is.True, testCase.Id);
        Assert.That(diagnostics.All(item => item.Code != "" && item.Message != ""), Is.True, testCase.Id);
    }

    [TestCaseSource(nameof(JsonCases))]
    public void PublicLoader_MatchesPinnedJsonCompatibleValue(YamlTestSuiteCase testCase)
    {
        List<object?> expected = JsonCompatibleStream(testCase.ExpectedJson);
        List<object?> actual = Processor().LoadAll(testCase.InputYaml);

        AssertJsonCompatibleEqual(expected, actual, testCase.Id);
    }

    [TestCaseSource(nameof(EmitterCases))]
    public void PublicEmitter_RoundTripsPinnedEmitterFixture(YamlTestSuiteCase testCase)
    {
        global::yaml.IYamlProcessor processor = Processor();
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;
        global::yaml.YamlStream input = processor.ParseAll(testCase.InputYaml);
        string emitted = pipeline.EmitStream(input, null);
        global::yaml.YamlParserResult emittedEvents = ParseEvents(emitted);
        global::yaml.YamlParserResult fixtureEvents = ParseEvents(testCase.EmitterYaml);

        Assert.That(emittedEvents.Success, Is.True, testCase.Id + " emitted:\n" + emitted + "\n" + FormatDiagnostics(emittedEvents.Diagnostics));
        Assert.That(fixtureEvents.Success, Is.True, testCase.Id + " fixture " + FormatDiagnostics(fixtureEvents.Diagnostics));
        Assert.That(SemanticEvents(emittedEvents.Events), Is.EqualTo(SemanticEvents(fixtureEvents.Events)), testCase.Id);
    }

    public static global::yaml.YamlParserOptions ParserOptions()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;
        return options;
    }

    public static global::yaml.IYamlProcessor Processor()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Parser.IncludeComments = false;
        return global::yaml.YamlApi.Processors.CreateProcessor(options);
    }

    public static global::yaml.YamlParserResult ParseEvents(string text)
    {
        global::yaml.YamlParserOptions options = ParserOptions();
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(text, options);
        return global::yaml.YamlApi.Pipeline.ParseScannerResult(scanner, options);
    }

    public static string EventFixture(IReadOnlyList<global::yaml.YamlEvent> events, string source)
    {
        List<string> lines = new List<string>();
        foreach (global::yaml.YamlEvent yamlEvent in events)
        {
            string line = EventLine(yamlEvent, source, true);
            if (line != "")
            {
                lines.Add(line);
            }
        }

        return string.Join("\n", lines) + "\n";
    }

    public static string SemanticEvents(IReadOnlyList<global::yaml.YamlEvent> events)
    {
        List<string> lines = new List<string>();
        foreach (global::yaml.YamlEvent yamlEvent in events)
        {
            string line = EventLine(yamlEvent, "", false);
            if (line != "")
            {
                lines.Add(line);
            }
        }

        return string.Join("\n", lines) + "\n";
    }

    public static string EventLine(global::yaml.YamlEvent yamlEvent, string source, bool includePresentation)
    {
        if (yamlEvent.Kind == global::yaml.YamlEventKind.StreamStart)
        {
            return "+STR";
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.StreamEnd)
        {
            return "-STR";
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.DocumentStart)
        {
            return includePresentation && SpanText(source, yamlEvent.Span) == "---" ? "+DOC ---" : "+DOC";
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.DocumentEnd)
        {
            return includePresentation && SpanText(source, yamlEvent.Span) == "..." ? "-DOC ..." : "-DOC";
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.SequenceStart)
        {
            string line = "+SEQ";
            if (includePresentation && yamlEvent.CollectionStyle == global::yaml.YamlCollectionStyle.Flow)
            {
                line = line + " []";
            }

            return line + EventProperties(yamlEvent);
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.SequenceEnd)
        {
            return "-SEQ";
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.MappingStart)
        {
            string line = "+MAP";
            if (includePresentation && yamlEvent.CollectionStyle == global::yaml.YamlCollectionStyle.Flow)
            {
                line = line + " {}";
            }

            return line + EventProperties(yamlEvent);
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.MappingEnd)
        {
            return "-MAP";
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.Scalar)
        {
            string style = includePresentation ? ScalarStyle(yamlEvent.ScalarStyle) : ":";
            string value = !includePresentation && yamlEvent.Value == "null" && yamlEvent.ScalarStyle == global::yaml.YamlScalarStyle.Plain ? "" : yamlEvent.Value;
            return "=VAL" + EventProperties(yamlEvent) + " " + style + EscapeEventValue(value);
        }

        if (yamlEvent.Kind == global::yaml.YamlEventKind.Alias)
        {
            return "=ALI *" + yamlEvent.AliasName;
        }

        return "";
    }

    public static string EventProperties(global::yaml.YamlEvent yamlEvent)
    {
        string properties = "";
        if (yamlEvent.Anchor != "")
        {
            properties = properties + " &" + yamlEvent.Anchor;
        }

        if (yamlEvent.Tag != "")
        {
            properties = properties + " <" + yamlEvent.Tag + ">";
        }

        return properties;
    }

    public static string ScalarStyle(global::yaml.YamlScalarStyle style)
    {
        if (style == global::yaml.YamlScalarStyle.SingleQuoted)
        {
            return "'";
        }

        if (style == global::yaml.YamlScalarStyle.DoubleQuoted)
        {
            return "\"";
        }

        if (style == global::yaml.YamlScalarStyle.Literal)
        {
            return "|";
        }

        if (style == global::yaml.YamlScalarStyle.Folded)
        {
            return ">";
        }

        return ":";
    }

    public static string EscapeEventValue(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\b", "\\b")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    public static string SpanText(string source, global::yaml.YamlSourceSpan span)
    {
        if (source == "" || span.StartIndex < 0 || span.EndIndex < span.StartIndex || span.EndIndex > source.Length)
        {
            return "";
        }

        return source.Substring(span.StartIndex, span.EndIndex - span.StartIndex);
    }

    public static string NormalizeLines(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public static List<object?> JsonCompatibleStream(string json)
    {
        List<object?> values = new List<object?>();
        JsonReaderOptions options = new JsonReaderOptions();
        options.AllowMultipleValues = true;
        Utf8JsonReader reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json), options);
        while (reader.Read())
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            values.Add(JsonElementValue(document.RootElement));
        }

        return values;
    }

    public static object? JsonElementValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt64(out long longValue))
            {
                return longValue;
            }

            return element.GetDouble();
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            List<object?> values = new List<object?>();
            foreach (JsonElement item in element.EnumerateArray())
            {
                values.Add(JsonElementValue(item));
            }

            return values;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            Dictionary<string, object?> values = new Dictionary<string, object?>();
            foreach (JsonProperty property in element.EnumerateObject())
            {
                values[property.Name] = JsonElementValue(property.Value);
            }

            return values;
        }

        return null;
    }

    public static void AssertJsonCompatibleEqual(object? expected, object? actual, string path)
    {
        Dictionary<string, object?>? expectedMapping = expected as Dictionary<string, object?>;
        Dictionary<string, object?>? actualMapping = actual as Dictionary<string, object?>;
        if (expectedMapping != null || actualMapping != null)
        {
            Assert.That(actualMapping, Is.Not.Null, path);
            Assert.That(expectedMapping, Is.Not.Null, path);
            Assert.That(actualMapping!.Count, Is.EqualTo(expectedMapping!.Count), path);
            foreach (string key in expectedMapping.Keys)
            {
                Assert.That(actualMapping.ContainsKey(key), Is.True, path + "." + key);
                AssertJsonCompatibleEqual(expectedMapping[key], actualMapping[key], path + "." + key);
            }

            return;
        }

        List<object?>? expectedList = expected as List<object?>;
        List<object?>? actualList = actual as List<object?>;
        if (expectedList != null || actualList != null)
        {
            Assert.That(actualList, Is.Not.Null, path);
            Assert.That(expectedList, Is.Not.Null, path);
            Assert.That(actualList!.Count, Is.EqualTo(expectedList!.Count), path);
            for (int index = 0; index < expectedList.Count; index = index + 1)
            {
                AssertJsonCompatibleEqual(expectedList[index], actualList[index], path + "[" + index.ToString() + "]");
            }

            return;
        }

        Assert.That(actual, Is.EqualTo(expected), path);
    }

    public static string FormatDiagnostics(IReadOnlyList<global::yaml.YamlDiagnostic> diagnostics)
    {
        return string.Join("\n", diagnostics.Select(item => item.Severity.ToString() + " " + item.Code + " at " + item.Span.StartLine.ToString() + ":" + item.Span.StartColumn.ToString() + ": " + item.Message));
    }
}
