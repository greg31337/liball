using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class CoreTypesTests
{
    [Test]
    public void TemplateSource_StoresTextAndIdentity()
    {
        TemplateSource source = new TemplateSource("welcome", "/templates/welcome.txt", "v1", "abc123", "Hello");

        Assert.That(source.Name, Is.EqualTo("welcome"));
        Assert.That(source.Path, Is.EqualTo("/templates/welcome.txt"));
        Assert.That(source.Version, Is.EqualTo("v1"));
        Assert.That(source.ContentHash, Is.EqualTo("abc123"));
        Assert.That(source.Text, Is.EqualTo("Hello"));
        Assert.That(source.Length, Is.EqualTo(5));
        Assert.That(source.GetDisplayName(), Is.EqualTo("welcome"));
    }

    [Test]
    public void TemplateSource_UsesPathOrAnonymousDisplayNameWhenNameIsMissing()
    {
        TemplateSource pathSource = new TemplateSource("", "/tmp/template.txt", null, null, "Text");
        TemplateSource anonymousSource = TemplateSource.FromString("Text");

        Assert.That(pathSource.GetDisplayName(), Is.EqualTo("/tmp/template.txt"));
        Assert.That(anonymousSource.GetDisplayName(), Is.EqualTo("(anonymous)"));
    }

    [Test]
    public void TemplateSourceSpan_StoresRangeAndExtractsText()
    {
        TemplateSource source = TemplateSource.FromNamedString("sample", "Hello template");
        TemplateSourceSpan span = source.CreateSpan(6, 8);

        Assert.That(span.Source, Is.SameAs(source));
        Assert.That(span.Start, Is.EqualTo(6));
        Assert.That(span.Length, Is.EqualTo(8));
        Assert.That(span.End, Is.EqualTo(14));
        Assert.That(span.GetText(), Is.EqualTo("template"));
        Assert.That(span.Contains(6), Is.True);
        Assert.That(span.Contains(13), Is.True);
        Assert.That(span.Contains(14), Is.False);
        Assert.That(span.IsEmpty(), Is.False);
    }

    [Test]
    public void TemplateSourceSpan_AllowsEmptySpanAtEnd()
    {
        TemplateSource source = TemplateSource.FromString("abc");
        TemplateSourceSpan span = source.CreateSpan(3, 0);

        Assert.That(span.Start, Is.EqualTo(3));
        Assert.That(span.End, Is.EqualTo(3));
        Assert.That(span.GetText(), Is.EqualTo(""));
        Assert.That(span.IsEmpty(), Is.True);
    }

    [Test]
    public void TemplateSourceSpan_RejectsInvalidRanges()
    {
        TemplateSource source = TemplateSource.FromString("abc");

        Assert.Throws<ArgumentOutOfRangeException>(() => source.CreateSpan(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CreateSpan(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CreateSpan(4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.CreateSpan(2, 2));
    }

    [Test]
    public void TemplateDiagnostic_StoresSeveritySpanAndHint()
    {
        TemplateSource source = TemplateSource.FromString("Hello");
        TemplateSourceSpan span = source.CreateSpan(0, 5);
        TemplateDiagnostic diagnostic = new TemplateDiagnostic("TEMPLATE001", TemplateDiagnosticSeverity.Error, "Invalid tag", span, "Close the tag.");

        Assert.That(diagnostic.Code, Is.EqualTo("TEMPLATE001"));
        Assert.That(diagnostic.Severity, Is.EqualTo(TemplateDiagnosticSeverity.Error));
        Assert.That(diagnostic.Message, Is.EqualTo("Invalid tag"));
        Assert.That(diagnostic.Span, Is.SameAs(span));
        Assert.That(diagnostic.Hint, Is.EqualTo("Close the tag."));
        Assert.That(diagnostic.IsError(), Is.True);
        Assert.That(diagnostic.GetSeverityName(), Is.EqualTo("error"));
        Assert.That(diagnostic.FormatSummary(), Is.EqualTo("TEMPLATE001 error: Invalid tag"));
    }
}
