using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class DiagnosticFormatterTests
{
    [Test]
    public void Format_IncludesSourceLineColumnLineTextMarkerAndHint()
    {
        TemplateSource source = new TemplateSource("invoice", null, null, null, "first\nHello {{ name }}\nlast");
        TemplateSourceSpan span = source.CreateSpan(12, 10);
        TemplateDiagnostic diagnostic = new TemplateDiagnostic("TEMPLATE001", TemplateDiagnosticSeverity.Error, "Invalid tag", span, "Close the tag.");

        string formatted = diagnostic.Format();

        Assert.That(
            formatted,
            Is.EqualTo("invoice:2:7: error TEMPLATE001: Invalid tag\nHello {{ name }}\n      ^^^^^^^^^^\nhint: Close the tag."));
    }

    [Test]
    public void Format_UsesPathDisplayNameAndHandlesCrLfLineEndings()
    {
        TemplateSource source = new TemplateSource("", "/templates/main.txt", null, null, "one\r\ntwo\r\nthree");
        TemplateSourceSpan span = source.CreateSpan(10, 5);
        TemplateDiagnostic diagnostic = TemplateDiagnostic.Create("TEMPLATE002", TemplateDiagnosticSeverity.Warning, "Check this value", span);

        string formatted = diagnostic.Format();

        Assert.That(
            formatted,
            Is.EqualTo("/templates/main.txt:3:1: warning TEMPLATE002: Check this value\nthree\n^^^^^"));
    }

    [Test]
    public void Format_UsesAnonymousSourceWhenNoNameOrPathExists()
    {
        TemplateSource source = TemplateSource.FromString("abc");
        TemplateSourceSpan span = source.CreateSpan(1, 1);
        TemplateDiagnostic diagnostic = TemplateDiagnostic.Create("TEMPLATE003", TemplateDiagnosticSeverity.Information, "Note", span);

        string formatted = diagnostic.Format();

        Assert.That(
            formatted,
            Is.EqualTo("(anonymous):1:2: information TEMPLATE003: Note\nabc\n ^"));
    }

    [Test]
    public void Format_WithoutSpanOmitsLocationAndSourceExcerpt()
    {
        TemplateDiagnostic diagnostic = new TemplateDiagnostic("TEMPLATE004", TemplateDiagnosticSeverity.Warning, "No span", null, "Add a span when available.");

        string formatted = diagnostic.Format();

        Assert.That(
            formatted,
            Is.EqualTo("warning TEMPLATE004: No span\nhint: Add a span when available."));
    }

    [Test]
    public void Formatter_ReportsLineColumnForEndOfSource()
    {
        TemplateDiagnosticFormatter formatter = new TemplateDiagnosticFormatter();

        Assert.That(formatter.GetLineNumber("one\ntwo", 7), Is.EqualTo(2));
        Assert.That(formatter.GetColumnNumber("one\ntwo", 7), Is.EqualTo(4));
        Assert.That(formatter.GetLineText("one\ntwo", 7), Is.EqualTo("two"));
    }
}
