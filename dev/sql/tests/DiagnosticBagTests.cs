using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class DiagnosticBagTests
{
    [Test]
    public void DiagnosticBag_StartsEmpty()
    {
        SqlDiagnosticBag bag = new SqlDiagnosticBag();

        Assert.That(bag.GetCount(), Is.EqualTo(0));
        Assert.That(bag.IsEmpty(), Is.True);
        Assert.That(bag.HasErrors(), Is.False);
        Assert.That(bag.ToArray(), Is.Empty);
    }

    [Test]
    public void DiagnosticBag_AddStoresDiagnosticsInOrder()
    {
        SqlDiagnosticBag bag = new SqlDiagnosticBag();
        SqlDiagnostic first = SqlDiagnostic.Create(SqlDiagnosticSeverity.Warning, "First.", new SqlTextSpan(0, 1));
        SqlDiagnostic second = SqlDiagnostic.Create(SqlDiagnosticSeverity.Error, "Second.", new SqlTextSpan(2, 1));

        bag.Add(first);
        bag.Add(second);

        Assert.That(bag.GetCount(), Is.EqualTo(2));
        Assert.That(bag.IsEmpty(), Is.False);
        Assert.That(bag.GetAt(0), Is.SameAs(first));
        Assert.That(bag.GetAt(1), Is.SameAs(second));
        Assert.That(bag.ToArray(), Is.EqualTo(new SqlDiagnostic[] { first, second }));
    }

    [Test]
    public void DiagnosticBag_AddMethodsCreateAndStoreDiagnostics()
    {
        SqlSourceText sourceText = SqlSourceText.Create("select\nbad");
        SqlDiagnosticBag bag = new SqlDiagnosticBag();

        SqlDiagnostic first = bag.AddDiagnostic(SqlDiagnosticSeverity.Info, "Informational.", new SqlTextSpan(0, 6));
        SqlDiagnostic second = bag.AddDiagnosticWithDetails(SqlDiagnosticSeverity.Error, "SQL001", "Bad token.", new SqlTextSpan(7, 3), sourceText);

        Assert.That(bag.GetCount(), Is.EqualTo(2));
        Assert.That(first.Message, Is.EqualTo("Informational."));
        Assert.That(second.Code, Is.EqualTo("SQL001"));
        Assert.That(second.GetLineNumber(), Is.EqualTo(2));
        Assert.That(second.GetColumnNumber(), Is.EqualTo(1));
    }

    [Test]
    public void DiagnosticBag_MergeAppendsDiagnostics()
    {
        SqlDiagnosticBag target = new SqlDiagnosticBag();
        SqlDiagnosticBag source = new SqlDiagnosticBag();
        SqlDiagnostic first = target.AddDiagnostic(SqlDiagnosticSeverity.Warning, "First.", new SqlTextSpan(0, 1));
        SqlDiagnostic second = source.AddDiagnostic(SqlDiagnosticSeverity.Error, "Second.", new SqlTextSpan(2, 1));
        SqlDiagnostic third = source.AddDiagnostic(SqlDiagnosticSeverity.Info, "Third.", new SqlTextSpan(4, 1));

        target.Merge(source);

        Assert.That(target.GetCount(), Is.EqualTo(3));
        Assert.That(target.GetAt(0), Is.SameAs(first));
        Assert.That(target.GetAt(1), Is.SameAs(second));
        Assert.That(target.GetAt(2), Is.SameAs(third));
    }

    [Test]
    public void DiagnosticBag_MergeWithSelfDuplicatesOriginalDiagnostics()
    {
        SqlDiagnosticBag bag = new SqlDiagnosticBag();
        SqlDiagnostic diagnostic = bag.AddDiagnostic(SqlDiagnosticSeverity.Warning, "Warning.", new SqlTextSpan(0, 1));

        bag.Merge(bag);

        Assert.That(bag.GetCount(), Is.EqualTo(2));
        Assert.That(bag.GetAt(0), Is.SameAs(diagnostic));
        Assert.That(bag.GetAt(1), Is.SameAs(diagnostic));
    }

    [Test]
    public void DiagnosticBag_CountsErrorsAndSeverities()
    {
        SqlDiagnosticBag bag = new SqlDiagnosticBag();

        bag.AddDiagnostic(SqlDiagnosticSeverity.Info, "Info.", new SqlTextSpan(0, 1));
        bag.AddDiagnostic(SqlDiagnosticSeverity.Warning, "Warning.", new SqlTextSpan(2, 1));
        bag.AddDiagnostic(SqlDiagnosticSeverity.Error, "Error.", new SqlTextSpan(4, 1));
        bag.AddDiagnostic(SqlDiagnosticSeverity.Error, "Error.", new SqlTextSpan(6, 1));

        Assert.That(bag.HasErrors(), Is.True);
        Assert.That(bag.CountBySeverity(SqlDiagnosticSeverity.Info), Is.EqualTo(1));
        Assert.That(bag.CountBySeverity(SqlDiagnosticSeverity.Warning), Is.EqualTo(1));
        Assert.That(bag.CountBySeverity(SqlDiagnosticSeverity.Error), Is.EqualTo(2));
        Assert.That(bag.CountBySeverity(SqlDiagnosticSeverity.Hidden), Is.EqualTo(0));
    }

    [Test]
    public void DiagnosticBag_ClearRemovesDiagnostics()
    {
        SqlDiagnosticBag bag = new SqlDiagnosticBag();

        bag.AddDiagnostic(SqlDiagnosticSeverity.Error, "Error.", new SqlTextSpan(0, 1));
        bag.Clear();

        Assert.That(bag.GetCount(), Is.EqualTo(0));
        Assert.That(bag.IsEmpty(), Is.True);
    }

    [Test]
    public void DiagnosticBag_RejectsNullDiagnosticsAndBags()
    {
        SqlDiagnosticBag bag = new SqlDiagnosticBag();

        Assert.Throws<ArgumentNullException>(() => bag.Add(null!));
        Assert.Throws<ArgumentNullException>(() => bag.Merge(null!));
    }
}
