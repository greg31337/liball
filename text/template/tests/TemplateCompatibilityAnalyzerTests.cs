using System;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateCompatibilityAnalyzerTests
{
    [Test]
    public void Analyze_ReportsEscapingTrimAndComplexExpressionRisks()
    {
        TemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();

        TemplateCompatibilityAnalysis analysis = analyzer.Analyze("A {{- name | upper -}}", "native", "mustache");

        Assert.That(analysis.SourceProfileName, Is.EqualTo("native"));
        Assert.That(analysis.TargetProfileName, Is.EqualTo("mustache"));
        Assert.That(analysis.NativeTemplateText, Is.EqualTo("A {{- name | upper -}}"));
        AssertCode(analysis, "COMPAT002");
        AssertCode(analysis, "COMPAT003");
        AssertCode(analysis, "COMPAT004");
        AssertCode(analysis, "COMPAT006");
    }

    [Test]
    public void Analyze_ReportsUnsupportedBlocksForTargetProfile()
    {
        TemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();

        TemplateCompatibilityAnalysis analysis = analyzer.Analyze("{% for item in items %}{{ item }}{% endfor %}", "native", "mustache");

        AssertCode(analysis, "COMPAT008");
        Assert.That(FindDiagnostic(analysis, "COMPAT008").Message, Does.Contain("for"));
    }

    [Test]
    public void Analyze_ConvertsSourceProfileBeforeAnalyzingTarget()
    {
        TemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();

        TemplateCompatibilityAnalysis analysis = analyzer.Analyze("{% if missing %}x{% elsif ok %}y{% endif %}", "liquid", "jinja");

        Assert.That(analysis.NativeTemplateText, Is.EqualTo("{% if missing %}x{% elseif ok %}y{% endif %}"));
        AssertCode(analysis, "COMPAT002");
        AssertCode(analysis, "COMPAT003");
        AssertNoCode(analysis, "COMPAT008");
    }

    [Test]
    public void AnalyzeNative_AnalyzesExistingNativeDocument()
    {
        TemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();
        TemplateDocumentSyntax document = TemplateParser.ParseNative("{% macro greet name %}Hi {{ name }}{% endmacro %}").Document;

        TemplateCompatibilityAnalysis analysis = analyzer.AnalyzeNative(document, "liquid");

        Assert.That(analysis.SourceProfileName, Is.EqualTo("native"));
        Assert.That(analysis.TargetProfileName, Is.EqualTo("liquid"));
        AssertCode(analysis, "COMPAT008");
    }

    [Test]
    public void Format_BuildsStableDiagnosticSummary()
    {
        TemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();

        TemplateCompatibilityAnalysis analysis = analyzer.Analyze("{{ name | upper }}", "native", "mustache");

        Assert.That(analysis.Format(), Is.EqualTo(
            "CompatibilityAnalysis source=\"native\" target=\"mustache\" nativeLength=18\n" +
            "  Diagnostic COMPAT002 Warning message=\"Target profile changes automatic escaping behavior.\"\n" +
            "  Diagnostic COMPAT003 Warning message=\"Target profile changes the default escaping mode.\"\n" +
            "  Diagnostic COMPAT006 Warning message=\"Expression may be too complex for target profile 'mustache'.\"\n"));
    }

    [Test]
    public void Analyze_RejectsInvalidArguments()
    {
        TemplateCompatibilityAnalyzer analyzer = new TemplateCompatibilityAnalyzer();
        TemplateDocumentSyntax document = TemplateParser.ParseNative("x").Document;

        Assert.Throws<ArgumentNullException>(() => analyzer.Analyze(null!, "native", "mustache"));
        Assert.Throws<ArgumentNullException>(() => analyzer.Analyze("x", null!, "mustache"));
        Assert.Throws<ArgumentNullException>(() => analyzer.Analyze("x", "native", null!));
        Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeNative(null!, "mustache"));
        Assert.Throws<ArgumentNullException>(() => analyzer.AnalyzeNative(document, null!));
        Assert.Throws<ArgumentException>(() => analyzer.Analyze("x", "unknown", "mustache"));
        Assert.Throws<ArgumentException>(() => analyzer.Analyze("x", "native", "unknown"));
    }

    [Test]
    public void CompatibilityAnalysis_RejectsNullConstructorArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new TemplateCompatibilityAnalysis(null!, "target", ""));
        Assert.Throws<ArgumentNullException>(() => new TemplateCompatibilityAnalysis("source", null!, ""));
        Assert.Throws<ArgumentNullException>(() => new TemplateCompatibilityAnalysis("source", "target", null!));
    }

    static void AssertCode(TemplateCompatibilityAnalysis analysis, string code)
    {
        Assert.That(HasDiagnostic(analysis, code), Is.True);
    }

    static void AssertNoCode(TemplateCompatibilityAnalysis analysis, string code)
    {
        Assert.That(HasDiagnostic(analysis, code), Is.False);
    }

    static TemplateDiagnostic FindDiagnostic(TemplateCompatibilityAnalysis analysis, string code)
    {
        int index = 0;
        while (index < analysis.Diagnostics.Count)
        {
            if (analysis.Diagnostics[index].Code == code)
            {
                return analysis.Diagnostics[index];
            }

            index = index + 1;
        }

        return TemplateDiagnostic.Create("", TemplateDiagnosticSeverity.Information, "");
    }

    static bool HasDiagnostic(TemplateCompatibilityAnalysis analysis, string code)
    {
        int index = 0;
        while (index < analysis.Diagnostics.Count)
        {
            if (analysis.Diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
