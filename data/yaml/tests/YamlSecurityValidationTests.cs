using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlSecurityValidationTests
{
    [Test]
    public void TryParse_ReportsInputLengthLimit()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxInputLength = 4;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("name: Ada\n", result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS001"), Is.True);
    }

    [Test]
    public void TryParse_ReportsScalarLengthLimit()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxScalarLength = 3;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("abcd\n", result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS002"), Is.True);
        Assert.That(result.Diagnostics.Count(item => item.Code == "YAMLS002"), Is.EqualTo(1));
    }

    [Test]
    public void TryParse_ReportsDepthLimit()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxDepth = 2;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("[[[1]]]\n", result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS003"), Is.True);
    }

    [Test]
    public void TryParse_ReportsAliasCountLimit()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxAliasCount = 1;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("base: &base value\na: *base\nb: *base\n", result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS004"), Is.True);
    }

    [Test]
    public void TryParse_ReportsCollectionSizeLimit()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxCollectionSize = 2;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("[1, 2, 3]\n", result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS005"), Is.True);
    }

    [Test]
    public void TryParse_AllowsLimitViolationsWhenSecurityDisabled()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.EnforceLimits = false;
        options.Security.MaxInputLength = 1;
        options.Security.MaxScalarLength = 1;
        options.Security.MaxDepth = 1;
        options.Security.MaxCollectionSize = 1;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("[long, scalar]\n", result, options);

        Assert.That(success, Is.True);
        Assert.That(result.HasErrors(), Is.False);
    }

    [Test]
    public void TryParse_SecurityScenarioReportsDeeplyNestedYaml()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxDepth = 8;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse(DeepFlowSequence(16), result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS003"), Is.True);
    }

    [Test]
    public void TryParse_SecurityScenarioReportsAliasHeavyYaml()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxAliasCount = 3;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse(AliasHeavyYaml(8), result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS004"), Is.True);
    }

    [Test]
    public void TryParse_SecurityScenarioReportsHugeScalars()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Security.MaxScalarLength = 1024;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse(new string('x', 4096) + "\n", result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS002"), Is.True);
    }

    [Test]
    public void TryParse_SecurityScenarioReportsDuplicateKeys()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("name: Ada\nname: Grace\n", result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLC009"), Is.True);
    }

    [Test]
    public void TryParse_SecurityScenarioRejectsUnknownTagsWhenConfigured()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("value: !unsafe tagged\n", result, options);

        Assert.That(success, Is.False);
        Assert.That(HasDiagnostic(result, "YAMLS006"), Is.True);
    }

    [Test]
    public void TryParse_SecurityScenarioAllowsConfiguredTags()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Schema.UnknownTagPolicy = global::yaml.YamlUnknownTagPolicy.Error;
        options.Schema.CustomTags.Register("!safe", global::yaml.YamlNodeKind.Scalar);
        global::yaml.YamlParseResult result = new global::yaml.YamlParseResult();

        bool success = global::yaml.Yaml.TryParse("value: !safe tagged\n", result, options);

        Assert.That(success, Is.True);
        Assert.That(HasDiagnostic(result, "YAMLS006"), Is.False);
    }

    public static bool HasDiagnostic(global::yaml.YamlParseResult result, string code)
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

    public static string DeepFlowSequence(int depth)
    {
        string text = "1";
        int index = 0;
        while (index < depth)
        {
            text = "[" + text + "]";
            index = index + 1;
        }

        return text + "\n";
    }

    public static string AliasHeavyYaml(int aliasCount)
    {
        string text = "base: &base value\n";
        int index = 0;
        while (index < aliasCount)
        {
            text = text + "alias" + index.ToString() + ": *base\n";
            index = index + 1;
        }

        return text;
    }
}
