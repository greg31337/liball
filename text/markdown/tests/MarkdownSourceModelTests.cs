using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownSourceModelTests
{
    [Test]
    public void Position_DefaultsToDocumentStart()
    {
        MarkdownSourcePosition position = new MarkdownSourcePosition();

        Assert.That(position.Offset, Is.EqualTo(0));
        Assert.That(position.Line, Is.EqualTo(1));
        Assert.That(position.Column, Is.EqualTo(1));
    }

    [Test]
    public void Position_RejectsInvalidCoordinates()
    {
        Assert.That(() => MarkdownSourcePosition.Create(-1, 1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownSourcePosition.Create(0, 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => MarkdownSourcePosition.Create(0, 1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Position_ComparisonsAcceptMissingPositions()
    {
        MarkdownSourcePosition position = MarkdownSourcePosition.Create(2, 1, 3);

        Assert.That(position.IsBeforeOrEqual(null), Is.False);
        Assert.That(position.EqualsPosition(null), Is.False);
    }

    [Test]
    public void Span_CalculatesLengthAndContainment()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(4, 2, 1, 12, 2, 9);

        Assert.That(span.GetLength(), Is.EqualTo(8));
        Assert.That(span.IsEmpty(), Is.False);
        Assert.That(span.ContainsOffset(4), Is.True);
        Assert.That(span.ContainsOffset(11), Is.True);
        Assert.That(span.ContainsOffset(12), Is.False);
    }

    [Test]
    public void Span_CopiesPositionsFromConstructorArguments()
    {
        MarkdownSourcePosition start = MarkdownSourcePosition.Create(2, 1, 3);
        MarkdownSourcePosition end = MarkdownSourcePosition.Create(7, 1, 8);
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(start, end);

        start.Offset = 99;
        end.Offset = 100;

        Assert.That(span.Start.Offset, Is.EqualTo(2));
        Assert.That(span.End.Offset, Is.EqualTo(7));
    }

    [Test]
    public void Span_RejectsEndBeforeStart()
    {
        MarkdownSourcePosition start = MarkdownSourcePosition.Create(10, 2, 5);
        MarkdownSourcePosition end = MarkdownSourcePosition.Create(8, 2, 3);

        Assert.That(() => MarkdownSourceSpan.Create(start, end), Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void Diagnostic_CapturesSeverityCodeMessageAndSpan()
    {
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(1, 1, 2, 5, 1, 6);
        MarkdownDiagnostic diagnostic = MarkdownDiagnostic.Warning("MD001", "sample warning", span);

        span.Start.Offset = 99;

        Assert.That(diagnostic.Severity, Is.EqualTo(MarkdownDiagnosticSeverity.Warning));
        Assert.That(diagnostic.Code, Is.EqualTo("MD001"));
        Assert.That(diagnostic.Message, Is.EqualTo("sample warning"));
        Assert.That(diagnostic.Span.Start.Offset, Is.EqualTo(1));
        Assert.That(diagnostic.Span.End.Offset, Is.EqualTo(5));
    }

    [Test]
    public void Diagnostic_CopyCreatesIndependentSpan()
    {
        MarkdownDiagnostic diagnostic = MarkdownDiagnostic.Error(
            "MD002",
            "sample error",
            MarkdownSourceSpan.Create(1, 1, 2, 5, 1, 6));

        MarkdownDiagnostic copy = diagnostic.Copy();
        diagnostic.Span.Start.Offset = 99;

        Assert.That(copy.Severity, Is.EqualTo(MarkdownDiagnosticSeverity.Error));
        Assert.That(copy.Code, Is.EqualTo("MD002"));
        Assert.That(copy.Message, Is.EqualTo("sample error"));
        Assert.That(copy.Span.Start.Offset, Is.EqualTo(1));
    }

    [Test]
    public void Diagnostic_RejectsNullValues()
    {
        MarkdownSourceSpan span = new MarkdownSourceSpan();

        Assert.That(() => MarkdownDiagnostic.Create(MarkdownDiagnosticSeverity.Info, null!, "message", span), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownDiagnostic.Create(MarkdownDiagnosticSeverity.Info, "MD001", null!, span), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownDiagnostic.Create(MarkdownDiagnosticSeverity.Info, "MD001", "message", null!), Throws.TypeOf<ArgumentNullException>());
    }
}
