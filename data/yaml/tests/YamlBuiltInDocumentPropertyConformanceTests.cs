using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlBuiltInDocumentPropertyConformanceTests
{
    public static readonly HashSet<string> MalformedDocumentPropertyIds = new HashSet<string>
    {
        "4JVG",
        "9HCY",
        "9MMA",
        "B63P",
        "EB22",
        "H7TQ",
        "MUS6/00",
        "MUS6/01",
        "QLJ7",
        "RHX7",
        "SF5V",
        "SR86"
    };

    public static IEnumerable<TestCaseData> DocumentPropertyCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (IsDocumentPropertyCase(testCase))
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    public static IEnumerable<TestCaseData> MalformedDocumentPropertyCases()
    {
        foreach (YamlTestSuiteCase testCase in YamlTestSuiteArchive.Cases())
        {
            if (MalformedDocumentPropertyIds.Contains(testCase.Id))
            {
                yield return new TestCaseData(testCase).SetArgDisplayNames(testCase.Id);
            }
        }
    }

    [TestCaseSource(nameof(DocumentPropertyCases))]
    public void ParserDocumentsAndProperties_MatchPinnedEvents(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(scanner.Success, Is.True, testCase.Id + " scanner " + YamlTestSuiteConformanceTests.FormatDiagnostics(scanner.Diagnostics));
        Assert.That(result.Success, Is.True, testCase.Id + " parser " + YamlTestSuiteConformanceTests.FormatDiagnostics(result.Diagnostics));
        Assert.That(YamlTestSuiteConformanceTests.EventFixture(result.Events, testCase.InputYaml), Is.EqualTo(YamlTestSuiteConformanceTests.NormalizeLines(testCase.ExpectedEvents)), testCase.Id);
    }

    [TestCaseSource(nameof(MalformedDocumentPropertyCases))]
    public void ParserDocumentsAndProperties_RejectMalformedGrammar(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;
        options.Recover = true;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(result.Success, Is.False, testCase.Id);
        Assert.That(result.Diagnostics, Is.Not.Empty, testCase.Id);
    }

    [Test]
    public void ParserDocumentsAndProperties_PreservePresentationTriviaAndPerDocumentTagExpansion()
    {
        string text = "%TAG !e! tag:first/\n--- # first\n!e!kind &one value # inline\n...\n# between\n%TAG !e! tag:second/\n---\n!e!kind &two [*one]\n";
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = true;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(text, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);
        List<global::yaml.YamlEvent> tagged = result.Events.Where(item => item.Tag != "").ToList();
        List<global::yaml.YamlEvent> comments = result.Events.Where(item => item.Kind == global::yaml.YamlEventKind.Comment).ToList();

        Assert.That(result.Success, Is.True, YamlTestSuiteConformanceTests.FormatDiagnostics(result.Diagnostics));
        Assert.That(result.Events.Count(item => item.Kind == global::yaml.YamlEventKind.TagDirective), Is.EqualTo(2));
        Assert.That(tagged.Select(item => item.Tag), Is.EqualTo(new string[] { "tag:first/kind", "tag:second/kind" }));
        Assert.That(tagged.Select(item => item.TagPresentation), Is.EqualTo(new string[] { "!e!kind", "!e!kind" }));
        Assert.That(tagged.Select(item => item.Anchor), Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(result.Events.Single(item => item.Kind == global::yaml.YamlEventKind.Alias).AliasName, Is.EqualTo("one"));
        Assert.That(comments.Select(item => item.Value), Is.EqualTo(new string[] { "# first", "# inline", "# between" }));
        Assert.That(comments.Select(item => item.Trivia.Placement), Is.EqualTo(new global::yaml.YamlTriviaPlacement[]
        {
            global::yaml.YamlTriviaPlacement.Inline,
            global::yaml.YamlTriviaPlacement.Inline,
            global::yaml.YamlTriviaPlacement.Before
        }));
    }

    [Test]
    public void ParserDocumentsAndProperties_AcceptsFutureMinorVersionWithWarning()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan("%YAML 1.3\n---\nvalue\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);
        global::yaml.YamlEvent directive = result.Events.Single(item => item.Kind == global::yaml.YamlEventKind.YamlVersionDirective);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("YAMLP008"));
        Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(global::yaml.YamlDiagnosticSeverity.Warning));
        Assert.That(directive.Directive.Version, Is.EqualTo(global::yaml.YamlVersion.Version12));
        Assert.That(directive.Directive.Value, Is.EqualTo("1.3"));
    }

    [TestCase("&a &b value\n")]
    [TestCase("&a *a\n")]
    public void ParserDocumentsAndProperties_RejectInvalidNodeProperties(string text)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(text, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("YAMLP019"));
    }

    [Test]
    public void ParserDocumentsAndProperties_RequireMarkerAfterDirectives()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan("%YAML 1.2\nvalue\n", options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("YAMLP020"));
    }

    [Test]
    public void ParserDocumentsAndProperties_ResetTagHandlesBetweenDocuments()
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.Recover = true;
        string text = "%TAG !e! tag:first/\n---\n!e!kind one\n...\n---\n!e!kind two\n";
        global::yaml.YamlScannerResult scanner = global::yaml.YamlApi.Pipeline.Scan(text, options);

        global::yaml.YamlParserResult result = global::yaml.YamlParser.Parse(scanner.Tokens, options);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("YAMLP013"));
        Assert.That(result.Events.Where(item => item.Kind == global::yaml.YamlEventKind.Scalar).Select(item => item.Tag), Is.EqualTo(new string[]
        {
            "tag:first/kind",
            "!e!kind"
        }));
    }

    public static bool IsDocumentPropertyCase(YamlTestSuiteCase testCase)
    {
        if (!testCase.ShouldParse)
        {
            return false;
        }

        string events = testCase.ExpectedEvents;
        int firstDocument = events.IndexOf("+DOC");
        int lastDocument = events.LastIndexOf("+DOC");
        return events.Contains("=ALI")
            || events.Contains(" &")
            || events.Contains(" <")
            || testCase.InputYaml.StartsWith("%")
            || firstDocument != lastDocument;
    }
}
