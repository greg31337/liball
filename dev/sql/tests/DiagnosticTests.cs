using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DiagnosticTests
{
    [Test]
    public void Diagnostic_StoresSeverityMessageAndSpan()
    {
        SqlTextSpan span = new SqlTextSpan(2, 4);
        SqlDiagnostic diagnostic = SqlDiagnostic.Create(SqlDiagnosticSeverity.Error, "Unexpected token.", span);

        Assert.That(diagnostic.Severity, Is.EqualTo(SqlDiagnosticSeverity.Error));
        Assert.That(diagnostic.Code, Is.Null);
        Assert.That(diagnostic.HasCode(), Is.False);
        Assert.That(diagnostic.Message, Is.EqualTo("Unexpected token."));
        Assert.That(diagnostic.Span, Is.SameAs(span));
        Assert.That(diagnostic.SourceText, Is.Null);
        Assert.That(diagnostic.HasSourceText(), Is.False);
        Assert.That(diagnostic.GetLineNumber(), Is.EqualTo(0));
        Assert.That(diagnostic.GetColumnNumber(), Is.EqualTo(0));
    }

    [Test]
    public void Diagnostic_StoresCodeSourceAndResolvesLocation()
    {
        SqlSourceText sourceText = SqlSourceText.Create("select\nbad", "query.sql");
        SqlTextSpan span = new SqlTextSpan(7, 3);
        SqlDiagnostic diagnostic = SqlDiagnostic.Create(SqlDiagnosticSeverity.Warning, "SQL001", "Example warning.", span, sourceText);

        Assert.That(diagnostic.Severity, Is.EqualTo(SqlDiagnosticSeverity.Warning));
        Assert.That(diagnostic.Code, Is.EqualTo("SQL001"));
        Assert.That(diagnostic.HasCode(), Is.True);
        Assert.That(diagnostic.Message, Is.EqualTo("Example warning."));
        Assert.That(diagnostic.Span, Is.SameAs(span));
        Assert.That(diagnostic.SourceText, Is.SameAs(sourceText));
        Assert.That(diagnostic.HasSourceText(), Is.True);
        Assert.That(diagnostic.GetLineNumber(), Is.EqualTo(2));
        Assert.That(diagnostic.GetColumnNumber(), Is.EqualTo(1));
    }

    [Test]
    public void Diagnostic_SeverityValuesAreStable()
    {
        Assert.That((int)SqlDiagnosticSeverity.Hidden, Is.EqualTo(0));
        Assert.That((int)SqlDiagnosticSeverity.Info, Is.EqualTo(1));
        Assert.That((int)SqlDiagnosticSeverity.Warning, Is.EqualTo(2));
        Assert.That((int)SqlDiagnosticSeverity.Error, Is.EqualTo(3));
    }

    [Test]
    public void Diagnostic_RejectsNullMessageAndSpan()
    {
        Assert.Throws<ArgumentNullException>(() => SqlDiagnostic.Create(SqlDiagnosticSeverity.Error, null!, new SqlTextSpan(0, 0)));
        Assert.Throws<ArgumentNullException>(() => SqlDiagnostic.Create(SqlDiagnosticSeverity.Error, "message", null!));
        Assert.Throws<ArgumentNullException>(() => SqlDiagnostic.Create(SqlDiagnosticSeverity.Error, "SQL001", null!, new SqlTextSpan(0, 0), null));
        Assert.Throws<ArgumentNullException>(() => SqlDiagnostic.Create(SqlDiagnosticSeverity.Error, "SQL001", "message", null!, null));
    }

    [Test]
    public void Diagnostic_EmptyCodeIsTreatedAsAbsentAndInvalidSourceSpanFailsOnLookup()
    {
        SqlSourceText sourceText = new SqlSourceText("x");
        SqlDiagnostic emptyCode = new SqlDiagnostic(SqlDiagnosticSeverity.Hidden, "", "hidden", new SqlTextSpan(0, 0), sourceText);
        SqlDiagnostic invalidLocation = new SqlDiagnostic(SqlDiagnosticSeverity.Info, null, "outside", new SqlTextSpan(2, 0), sourceText);

        Assert.That(emptyCode.HasCode(), Is.False);
        Assert.That(emptyCode.GetLineNumber(), Is.EqualTo(1));
        Assert.That(emptyCode.GetColumnNumber(), Is.EqualTo(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidLocation.GetLineNumber());
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidLocation.GetColumnNumber());
    }
}
