using NUnit.Framework;

namespace yaml.Tests;

public class YamlExpectedScalar
{
    public global::yaml.YamlScalarStyle Style;
    public string Value;

    public YamlExpectedScalar()
    {
        Style = global::yaml.YamlScalarStyle.Plain;
        Value = "";
    }
}

[TestFixture]
public class YamlBuiltInScalarConformanceTests
{
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

    [TestCaseSource(nameof(ValidCases))]
    public void ScannerScalarTokens_MatchPinnedScalarEvents(YamlTestSuiteCase testCase)
    {
        global::yaml.YamlParserOptions options = new global::yaml.YamlParserOptions();
        options.IncludeComments = false;

        global::yaml.YamlScannerResult result = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, options);
        List<global::yaml.YamlToken> actual = result.Tokens.Where(IsScalarToken).ToList();
        List<YamlExpectedScalar> expected = ExpectedScalars(testCase.ExpectedEvents);

        Assert.That(result.Success, Is.True, testCase.Id + " diagnostics: " + string.Join(" | ", result.Diagnostics.Select(item => item.Code + "=" + item.Message)));
        Assert.That(actual, Has.Count.EqualTo(expected.Count), testCase.Id + " scalar count actual: " + string.Join(" | ", actual.Select(item => item.Kind.ToString() + "=" + item.Value.Replace("\n", "\\n"))) + " expected: " + string.Join(" | ", expected.Select(item => item.Style.ToString() + "=" + item.Value.Replace("\n", "\\n"))));
        for (int index = 0; index < expected.Count; index = index + 1)
        {
            Assert.That(TokenStyle(actual[index]), Is.EqualTo(expected[index].Style), testCase.Id + " scalar " + index.ToString() + " style");
            Assert.That(actual[index].Value, Is.EqualTo(expected[index].Value), testCase.Id + " scalar " + index.ToString() + " value, indent " + actual[index].Indent.ToString() + ", header " + actual[index].Header);
        }
    }

    [TestCase("5LLU")]
    [TestCase("S4GJ")]
    [TestCase("S98Z")]
    [TestCase("W9L4")]
    [TestCase("X4QW")]
    public void ScannerBlockScalars_RejectInvalidPinnedGrammar(string fixtureId)
    {
        YamlTestSuiteCase testCase = YamlTestSuiteArchive.Cases().Single(item => item.Id == fixtureId);

        global::yaml.YamlScannerResult result = global::yaml.YamlApi.Pipeline.Scan(testCase.InputYaml, new global::yaml.YamlParserOptions());

        Assert.That(testCase.ShouldParse, Is.False, fixtureId);
        Assert.That(result.Success, Is.False, fixtureId);
        Assert.That(result.Diagnostics.Any(item => item.Code == "YAML012"), Is.True, fixtureId);
    }

    public static List<YamlExpectedScalar> ExpectedScalars(string fixture)
    {
        List<YamlExpectedScalar> values = new List<YamlExpectedScalar>();
        string normalized = fixture.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] lines = normalized.Split('\n');
        foreach (string line in lines)
        {
            if (!line.StartsWith("=VAL"))
            {
                continue;
            }

            int marker = ScalarMarkerIndex(line);
            if (marker < 0)
            {
                continue;
            }

            char styleMarker = line[marker];
            string value = DecodeEventValue(line.Substring(marker + 1));
            if (styleMarker == ':' && value == "")
            {
                continue;
            }

            YamlExpectedScalar scalar = new YamlExpectedScalar();
            scalar.Style = MarkerStyle(styleMarker);
            scalar.Value = value;
            values.Add(scalar);
        }

        return values;
    }

    public static int ScalarMarkerIndex(string line)
    {
        int index = 4;
        while (index < line.Length)
        {
            while (index < line.Length && line[index] == ' ')
            {
                index = index + 1;
            }

            if (index >= line.Length)
            {
                return -1;
            }

            if (line[index] == '&')
            {
                while (index < line.Length && line[index] != ' ')
                {
                    index = index + 1;
                }

                continue;
            }

            if (line[index] == '<')
            {
                while (index < line.Length && line[index] != '>')
                {
                    index = index + 1;
                }

                if (index < line.Length)
                {
                    index = index + 1;
                }

                continue;
            }

            return index;
        }

        return -1;
    }

    public static string DecodeEventValue(string value)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] != '\\' || index + 1 >= value.Length)
            {
                builder.Append(value[index]);
                index = index + 1;
                continue;
            }

            char escaped = value[index + 1];
            if (escaped == 'n')
            {
                builder.Append('\n');
            }
            else if (escaped == 'r')
            {
                builder.Append('\r');
            }
            else if (escaped == 't')
            {
                builder.Append('\t');
            }
            else if (escaped == 'b')
            {
                builder.Append('\b');
            }
            else
            {
                builder.Append(escaped);
            }

            index = index + 2;
        }

        return builder.ToString();
    }

    public static global::yaml.YamlScalarStyle MarkerStyle(char marker)
    {
        if (marker == '\'')
        {
            return global::yaml.YamlScalarStyle.SingleQuoted;
        }

        if (marker == '"')
        {
            return global::yaml.YamlScalarStyle.DoubleQuoted;
        }

        if (marker == '|')
        {
            return global::yaml.YamlScalarStyle.Literal;
        }

        if (marker == '>')
        {
            return global::yaml.YamlScalarStyle.Folded;
        }

        return global::yaml.YamlScalarStyle.Plain;
    }

    public static bool IsScalarToken(global::yaml.YamlToken token)
    {
        return token.Kind == global::yaml.YamlTokenKind.PlainScalar
            || token.Kind == global::yaml.YamlTokenKind.SingleQuotedScalar
            || token.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar
            || token.Kind == global::yaml.YamlTokenKind.LiteralScalar
            || token.Kind == global::yaml.YamlTokenKind.FoldedScalar;
    }

    public static global::yaml.YamlScalarStyle TokenStyle(global::yaml.YamlToken token)
    {
        if (token.Kind == global::yaml.YamlTokenKind.SingleQuotedScalar)
        {
            return global::yaml.YamlScalarStyle.SingleQuoted;
        }

        if (token.Kind == global::yaml.YamlTokenKind.DoubleQuotedScalar)
        {
            return global::yaml.YamlScalarStyle.DoubleQuoted;
        }

        if (token.Kind == global::yaml.YamlTokenKind.LiteralScalar)
        {
            return global::yaml.YamlScalarStyle.Literal;
        }

        if (token.Kind == global::yaml.YamlTokenKind.FoldedScalar)
        {
            return global::yaml.YamlScalarStyle.Folded;
        }

        return global::yaml.YamlScalarStyle.Plain;
    }
}
