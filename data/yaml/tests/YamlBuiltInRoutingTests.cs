using System.Text;
using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlBuiltInRoutingTests
{
    [TestCase("FQ7F")]
    [TestCase("ZF4X")]
    [TestCase("M7NX")]
    [TestCase("3GZX")]
    [TestCase("7FWL")]
    [TestCase("G4RS")]
    [TestCase("4WA9")]
    [TestCase("RTP8")]
    public void BuiltInParser_ProducesRequiredEventShapes(string fixtureId)
    {
        AssertFixtureEvents(fixtureId);
    }

    [TestCase("4MUZ/00")]
    [TestCase("4MUZ/01")]
    [TestCase("4MUZ/02")]
    [TestCase("DK95/03")]
    [TestCase("DK95/04")]
    [TestCase("DK95/05")]
    [TestCase("6FWR")]
    [TestCase("JEF9/01")]
    [TestCase("L24T/00")]
    public void BuiltInParser_HandlesNormalizedAndMultilineScalars(string fixtureId)
    {
        AssertFixtureEvents(fixtureId);
    }

    [TestCase("UKK6/00")]
    [TestCase("M2N8/00")]
    [TestCase("L383")]
    public void BuiltInParser_HandlesFormerSpecialCaseGrammar(string fixtureId)
    {
        AssertFixtureEvents(fixtureId);
    }

    [TestCase("MUS6/00", "YAMLP006")]
    [TestCase("DK4H", "YAMLP018")]
    [TestCase("DK95/01", "YAML001")]
    public void BuiltInParser_MalformedInputReturnsStructuredErrors(string fixtureId, string diagnosticCode)
    {
        YamlTestSuiteCase fixture = Fixture(fixtureId);

        global::yaml.YamlParserResult result = ParseBuiltIn(fixture.InputYaml);

        Assert.That(fixture.ShouldParse, Is.False, fixtureId);
        Assert.That(result.Success, Is.False, fixtureId);
        Assert.That(result.Diagnostics, Has.Count.EqualTo(1), fixtureId);
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(diagnosticCode), fixtureId);
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(global::yaml.YamlDiagnosticSeverity.Error), fixtureId);
        Assert.That(result.Diagnostics[0].Message, Is.Not.Empty, fixtureId);
    }

    [TestCase("CQ3W")]
    [TestCase("55WF")]
    [TestCase("CML9")]
    public void BuiltInParser_InvalidFixturesReturnStructuredErrors(string fixtureId)
    {
        YamlTestSuiteCase fixture = Fixture(fixtureId);

        global::yaml.YamlParserResult result = ParseBuiltIn(fixture.InputYaml);

        Assert.That(fixture.ShouldParse, Is.False, fixtureId);
        Assert.That(result.Success, Is.False, fixtureId);
        Assert.That(result.Diagnostics, Is.Not.Empty, fixtureId);
        Assert.That(result.Diagnostics.All(item => item.Severity == global::yaml.YamlDiagnosticSeverity.Error), Is.True, fixtureId);
        Assert.That(result.Diagnostics.All(item => item.Code != "" && item.Message != ""), Is.True, fixtureId);
    }

    [Test]
    public void BuiltInParser_PreservesOneBasedSpansAndExplicitDocumentMarkers()
    {
        string text = "---\n[one, two]\n...\n";

        global::yaml.YamlParserResult result = ParseBuiltIn(text);

        Assert.That(result.Success, Is.True);
        global::yaml.YamlEvent documentStart = result.Events.Single(item => item.Kind == global::yaml.YamlEventKind.DocumentStart);
        global::yaml.YamlEvent sequenceStart = result.Events.Single(item => item.Kind == global::yaml.YamlEventKind.SequenceStart);
        global::yaml.YamlEvent documentEnd = result.Events.Single(item => item.Kind == global::yaml.YamlEventKind.DocumentEnd);
        Assert.That(SpanText(text, documentStart.Span), Is.EqualTo("---"));
        Assert.That(documentStart.Span.StartLine, Is.EqualTo(1));
        Assert.That(documentStart.Span.StartColumn, Is.EqualTo(1));
        Assert.That(sequenceStart.Span.StartIndex, Is.EqualTo(4));
        Assert.That(sequenceStart.Span.StartLine, Is.EqualTo(2));
        Assert.That(sequenceStart.Span.StartColumn, Is.EqualTo(1));
        Assert.That(SpanText(text, documentEnd.Span), Is.EqualTo("..."));
        Assert.That(documentEnd.Span.StartLine, Is.EqualTo(3));
        Assert.That(documentEnd.Span.StartColumn, Is.EqualTo(1));
    }

    [Test]
    public void BuiltInParser_IsUsedByTextByteAndProcessorEntryPoints()
    {
        YamlTestSuiteCase fixture = Fixture("4MUZ/02");
        global::yaml.YamlParserOptions parserOptions = BuiltInOptions();
        global::yaml.IYamlPipeline pipeline = global::yaml.YamlApi.Pipeline;

        global::yaml.YamlScannerResult scanner = pipeline.Scan(fixture.InputYaml, parserOptions);
        global::yaml.YamlParserResult fromScanner = pipeline.ParseScannerResult(scanner, parserOptions);
        global::yaml.YamlParserResult fromBytes = pipeline.ParseBytes(Encoding.UTF8.GetBytes(fixture.InputYaml), parserOptions, null);

        global::yaml.YamlOptions processorOptions = new global::yaml.YamlOptions();
        processorOptions.Parser.IncludeComments = false;
        global::yaml.IYamlProcessor processor = global::yaml.YamlApi.Processors.CreateProcessor(processorOptions);
        global::yaml.YamlStream fromTextProcessor = processor.ParseAll(fixture.InputYaml);
        global::yaml.YamlStream fromByteProcessor = processor.ParseAllBytes(Encoding.UTF8.GetBytes(fixture.InputYaml));
        Dictionary<string, object?> loaded = (Dictionary<string, object?>)processor.Load(fixture.InputYaml)!;
        object? deserialized = processor.Deserialize("37\n", typeof(int));
        object? deserializedBytes = processor.DeserializeBytes(Encoding.UTF8.GetBytes("38\n"), typeof(int));

        string expected = NormalizeLines(fixture.ExpectedEvents);
        Assert.That(EventFixture(fromScanner.Events, fixture.InputYaml), Is.EqualTo(expected));
        Assert.That(EventFixture(fromBytes.Events, fixture.InputYaml), Is.EqualTo(expected));
        Assert.That(fromTextProcessor.Documents, Has.Count.EqualTo(1));
        Assert.That(fromByteProcessor.Documents, Has.Count.EqualTo(1));
        Assert.That(loaded, Contains.Key("foo"));
        Assert.That(loaded["foo"], Is.EqualTo("bar"));
        Assert.That(deserialized, Is.EqualTo(37));
        Assert.That(deserializedBytes, Is.EqualTo(38));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BuiltInParser_ProducesEquivalentEventsFromScannerTokensAndBytes(bool includeComments)
    {
        string text = "%YAML 1.2\n%TAG !e! tag:example/\n---\n# before\nroot: &item [!e!kind \"one\", {two: three}] # inline\nalias: *item\n...\n";
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = includeComments;
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlScannerResult scanner = pipeline.Scan(text, options);

        global::yaml.YamlParserResult fromScanner = pipeline.ParseScannerResult(scanner, options);
        global::yaml.YamlParserResult fromTokens = pipeline.ParseTokens(scanner.Tokens, options);
        global::yaml.YamlParserResult fromBytes = pipeline.ParseBytes(Encoding.UTF8.GetBytes(text), options, null);

        Assert.That(scanner.Success, Is.True);
        Assert.That(fromScanner.Success, Is.True, FormatDiagnostics(fromScanner.Diagnostics));
        Assert.That(fromTokens.Success, Is.True, FormatDiagnostics(fromTokens.Diagnostics));
        Assert.That(fromBytes.Success, Is.True, FormatDiagnostics(fromBytes.Diagnostics));
        AssertEquivalentEvents(fromScanner.Events, fromTokens.Events, "standalone tokens");
        AssertEquivalentEvents(fromScanner.Events, fromBytes.Events, "bytes");
    }

    [Test]
    public void BuiltInParser_PreservesScannerDiagnosticMetadataAcrossTextAndBytes()
    {
        string text = "bad\u0001\n";
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        global::yaml.IYamlPipeline pipeline = new global::yaml.YamlPipeline();
        global::yaml.YamlScannerResult scanner = pipeline.Scan(text, options);

        global::yaml.YamlParserResult fromScanner = pipeline.ParseScannerResult(scanner, options);
        global::yaml.YamlParserResult fromBytes = pipeline.ParseBytes(Encoding.UTF8.GetBytes(text), options, null);

        global::yaml.YamlDiagnostic scannerDiagnostic = scanner.Diagnostics.Single();
        global::yaml.YamlDiagnostic textDiagnostic = fromScanner.Diagnostics.Single(item => item.Code == scannerDiagnostic.Code);
        global::yaml.YamlDiagnostic byteDiagnostic = fromBytes.Diagnostics.Single(item => item.Code == scannerDiagnostic.Code);
        AssertEquivalentDiagnostic(scannerDiagnostic, textDiagnostic, "scanner result");
        AssertEquivalentDiagnostic(scannerDiagnostic, byteDiagnostic, "bytes");
    }

    static void AssertEquivalentEvents(
        IReadOnlyList<global::yaml.YamlEvent> expected,
        IReadOnlyList<global::yaml.YamlEvent> actual,
        string route)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count), route);
        int index = 0;
        while (index < expected.Count)
        {
            global::yaml.YamlEvent left = expected[index];
            global::yaml.YamlEvent right = actual[index];
            string context = route + " event " + index.ToString();
            Assert.Multiple(() =>
            {
                Assert.That(right.Kind, Is.EqualTo(left.Kind), context + " kind");
                Assert.That(right.Value, Is.EqualTo(left.Value), context + " value");
                Assert.That(right.Tag, Is.EqualTo(left.Tag), context + " tag");
                Assert.That(right.TagPresentation, Is.EqualTo(left.TagPresentation), context + " tag presentation");
                Assert.That(right.ParsedTag, Is.EqualTo(left.ParsedTag), context + " parsed tag");
                Assert.That(right.Anchor, Is.EqualTo(left.Anchor), context + " anchor");
                Assert.That(right.AliasName, Is.EqualTo(left.AliasName), context + " alias");
                Assert.That(right.ScalarStyle, Is.EqualTo(left.ScalarStyle), context + " scalar style");
                Assert.That(right.CollectionStyle, Is.EqualTo(left.CollectionStyle), context + " collection style");
                Assert.That(right.ImplicitEmpty, Is.EqualTo(left.ImplicitEmpty), context + " implicit empty");
                AssertEquivalentSpan(left.Span, right.Span, context + " span");
                Assert.That(right.Trivia.Kind, Is.EqualTo(left.Trivia.Kind), context + " trivia kind");
                Assert.That(right.Trivia.Placement, Is.EqualTo(left.Trivia.Placement), context + " trivia placement");
                Assert.That(right.Trivia.Text, Is.EqualTo(left.Trivia.Text), context + " trivia text");
                AssertEquivalentSpan(left.Trivia.Span, right.Trivia.Span, context + " trivia span");
                Assert.That(right.Directive.Name, Is.EqualTo(left.Directive.Name), context + " directive name");
                Assert.That(right.Directive.Value, Is.EqualTo(left.Directive.Value), context + " directive value");
                Assert.That(right.Directive.Version, Is.EqualTo(left.Directive.Version), context + " directive version");
                Assert.That(right.Directive.Handle, Is.EqualTo(left.Directive.Handle), context + " directive handle");
                Assert.That(right.Directive.Prefix, Is.EqualTo(left.Directive.Prefix), context + " directive prefix");
                AssertEquivalentSpan(left.Directive.Span, right.Directive.Span, context + " directive span");
            });
            index = index + 1;
        }
    }

    static void AssertEquivalentDiagnostic(
        global::yaml.YamlDiagnostic expected,
        global::yaml.YamlDiagnostic actual,
        string route)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Severity, Is.EqualTo(expected.Severity), route + " severity");
            Assert.That(actual.Code, Is.EqualTo(expected.Code), route + " code");
            Assert.That(actual.Message, Is.EqualTo(expected.Message), route + " message");
            AssertEquivalentSpan(expected.Span, actual.Span, route + " span");
        });
    }

    static void AssertEquivalentSpan(
        global::yaml.YamlSourceSpan expected,
        global::yaml.YamlSourceSpan actual,
        string context)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.StartIndex, Is.EqualTo(expected.StartIndex), context + " start index");
            Assert.That(actual.StartLine, Is.EqualTo(expected.StartLine), context + " start line");
            Assert.That(actual.StartColumn, Is.EqualTo(expected.StartColumn), context + " start column");
            Assert.That(actual.EndIndex, Is.EqualTo(expected.EndIndex), context + " end index");
            Assert.That(actual.EndLine, Is.EqualTo(expected.EndLine), context + " end line");
            Assert.That(actual.EndColumn, Is.EqualTo(expected.EndColumn), context + " end column");
        });
    }

    static void AssertFixtureEvents(string fixtureId)
    {
        YamlTestSuiteCase fixture = Fixture(fixtureId);

        global::yaml.YamlParserResult result = ParseBuiltIn(fixture.InputYaml);

        Assert.That(fixture.ShouldParse, Is.True, fixtureId);
        Assert.That(result.Success, Is.True, fixtureId + " " + FormatDiagnostics(result.Diagnostics));
        Assert.That(EventFixture(result.Events, fixture.InputYaml), Is.EqualTo(NormalizeLines(fixture.ExpectedEvents)), fixtureId);
    }

    static YamlTestSuiteCase Fixture(string fixtureId)
    {
        return YamlTestSuiteArchive.Cases().Single(item => item.Id == fixtureId);
    }

    static global::yaml.YamlParserOptions BuiltInOptions()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;
        return options;
    }

    static global::yaml.YamlParserResult ParseBuiltIn(string text)
    {
        global::yaml.YamlParserOptions options = BuiltInOptions();
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(text, options);
        return global::yaml.YamlApi.Pipeline.ParseScannerResult(scanner, options);
    }

    static string EventFixture(IReadOnlyList<global::yaml.YamlEvent> events, string source)
    {
        return YamlTestSuiteConformanceTests.EventFixture(events, source);
    }

    static string SpanText(string source, global::yaml.YamlSourceSpan span)
    {
        return YamlTestSuiteConformanceTests.SpanText(source, span);
    }

    static string NormalizeLines(string text)
    {
        return YamlTestSuiteConformanceTests.NormalizeLines(text);
    }

    static string FormatDiagnostics(IReadOnlyList<global::yaml.YamlDiagnostic> diagnostics)
    {
        return YamlTestSuiteConformanceTests.FormatDiagnostics(diagnostics);
    }
}
