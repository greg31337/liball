using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownDiagnosticCollectionTests
{
    [Test]
    public void Collection_AddsDiagnosticsAndCountsSeverity()
    {
        MarkdownDiagnosticCollection diagnostics = new MarkdownDiagnosticCollection();
        MarkdownSourceSpan span = MarkdownSourceSpan.Create(1, 1, 2, 2, 1, 3);

        diagnostics.AddInfo("I001", "info", span);
        diagnostics.AddWarning("W001", "warning", span);
        diagnostics.AddError("E001", "error", span);

        Assert.That(diagnostics.HasDiagnostics(), Is.True);
        Assert.That(diagnostics.GetCount(), Is.EqualTo(3));
        Assert.That(diagnostics.CountBySeverity(MarkdownDiagnosticSeverity.Info), Is.EqualTo(1));
        Assert.That(diagnostics.CountBySeverity(MarkdownDiagnosticSeverity.Warning), Is.EqualTo(1));
        Assert.That(diagnostics.CountBySeverity(MarkdownDiagnosticSeverity.Error), Is.EqualTo(1));
        Assert.That(diagnostics.HasWarnings(), Is.True);
        Assert.That(diagnostics.HasErrors(), Is.True);
    }

    [Test]
    public void Collection_CopiesAddedAndReturnedDiagnostics()
    {
        MarkdownDiagnosticCollection diagnostics = new MarkdownDiagnosticCollection();
        MarkdownDiagnostic diagnostic = MarkdownDiagnostic.Warning(
            "W001",
            "warning",
            MarkdownSourceSpan.Create(1, 1, 2, 4, 1, 5));

        diagnostics.Add(diagnostic);
        diagnostic.Span.Start.Offset = 99;

        MarkdownDiagnostic stored = diagnostics.Get(0);
        MarkdownDiagnostic[] all = diagnostics.GetDiagnostics();
        stored.Span.Start.Offset = 77;
        all[0].Span.Start.Offset = 66;

        Assert.That(diagnostics.Get(0).Span.Start.Offset, Is.EqualTo(1));
    }

    [Test]
    public void Collection_ClearRemovesDiagnostics()
    {
        MarkdownDiagnosticCollection diagnostics = new MarkdownDiagnosticCollection();

        diagnostics.AddError("E001", "error", new MarkdownSourceSpan());
        diagnostics.Clear();

        Assert.That(diagnostics.GetCount(), Is.EqualTo(0));
        Assert.That(diagnostics.HasDiagnostics(), Is.False);
        Assert.That(diagnostics.HasErrors(), Is.False);
    }

    [Test]
    public void Collection_RejectsInvalidArguments()
    {
        MarkdownDiagnosticCollection diagnostics = new MarkdownDiagnosticCollection();

        Assert.That(() => diagnostics.Add(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => diagnostics.Get(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => diagnostics.Get(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => diagnostics.AddInfo(null!, "message", new MarkdownSourceSpan()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => diagnostics.AddWarning("W001", null!, new MarkdownSourceSpan()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => diagnostics.AddError("E001", "message", null!), Throws.TypeOf<ArgumentNullException>());
    }
}
