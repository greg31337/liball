using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlComplexInvalidInputTests
{
    [Test]
    public void Scan_ComplexInvalidDocument_ReportsMultipleIndependentScannerDiagnostics()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "root:",
                "  badEscape: \"x\\q\"",
                "  badBlock: >--",
                "  badTag: !<missing",
                "  reserved: @",
                ""
            });

        global::yaml.YamlScannerResult result = global::yaml.YamlScanner.Scan(text, new global::yaml.YamlParserOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code), Is.EqualTo(new string[]
        {
            "YAML010",
            "YAML012",
            "YAML007",
            "YAML003"
        }));
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Span.StartLine), Is.EqualTo(new int[] { 2, 3, 4, 5 }));
        Assert.That(result.Tokens.Where(token => token.Kind == global::yaml.YamlTokenKind.Error).Select(token => token.Value), Is.EqualTo(new string[]
        {
            ">--",
            "!<missing",
            "@"
        }));
        Assert.That(result.Tokens.Single(token => token.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar).Value, Is.EqualTo("x"));
    }

    [Test]
    public void TryParse_ComplexInvalidDocument_ReportsComposerAndSchemaDiagnosticsTogether()
    {
        string text = string.Join(
            "\n",
            new string[]
            {
                "root:",
                "  first: &same one",
                "  second: &same two",
                "  missing: *missing",
                "  tagged: !unsafe value",
                ""
            });
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse(text, result, options);

        Assert.That(success, Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLC006"), Is.False);
        Assert.That(HasDiagnostic(result, "YAMLC007"), Is.True);
        Assert.That(HasDiagnostic(result, "YAMLS006"), Is.True);
        Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Code == "YAMLC007").Single().Span.StartLine, Is.EqualTo(4));
        Assert.That(result.Diagnostics.Where(diagnostic => diagnostic.Code == "YAMLS006").Single().Span.StartLine, Is.EqualTo(5));
        Assert.That(result.Value, Is.Null);
    }

    static bool HasDiagnostic(global::yaml.YamlParseResult result, string code)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            if (result.Diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
