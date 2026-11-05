using NUnit.Framework;

namespace molecule.Tests;

[TestFixture]
public class MoleculeDiagnosticsTests
{
    [Test]
    public void DiagnosticLocation_DefaultsToUnknownLocation()
    {
        DiagnosticLocation location = DiagnosticLocation.CreateEmpty();

        Assert.That(location.SourceName, Is.EqualTo(""));
        Assert.That(location.LineNumber, Is.EqualTo(0));
        Assert.That(location.ColumnNumber, Is.EqualTo(0));
        Assert.That(location.CharacterIndex, Is.EqualTo(-1));
        Assert.That(location.Context, Is.EqualTo(""));
        Assert.That(location.HasLineColumn(), Is.False);
        Assert.That(location.HasCharacterIndex(), Is.False);
    }

    [Test]
    public void DiagnosticLocation_StoresSourceCoordinates()
    {
        DiagnosticLocation location = new DiagnosticLocation("sample.smi", 4, 12, 31, "C1CC");

        Assert.That(location.SourceName, Is.EqualTo("sample.smi"));
        Assert.That(location.LineNumber, Is.EqualTo(4));
        Assert.That(location.ColumnNumber, Is.EqualTo(12));
        Assert.That(location.CharacterIndex, Is.EqualTo(31));
        Assert.That(location.Context, Is.EqualTo("C1CC"));
        Assert.That(location.HasLineColumn(), Is.True);
        Assert.That(location.HasCharacterIndex(), Is.True);
    }

    [Test]
    public void MoleculeError_StoresDiagnosticDetails()
    {
        DiagnosticLocation location = new DiagnosticLocation("mol.sdf", 7, 3, 82, "atom block");
        MoleculeError error = new MoleculeError("VALENCE", "invalid valence", location, 5, 9, ErrorSeverity.Fatal);

        Assert.That(error.Code, Is.EqualTo("VALENCE"));
        Assert.That(error.Message, Is.EqualTo("invalid valence"));
        Assert.That(error.Location, Is.SameAs(location));
        Assert.That(error.AtomId, Is.EqualTo(5));
        Assert.That(error.BondId, Is.EqualTo(9));
        Assert.That(error.Severity, Is.EqualTo(ErrorSeverity.Fatal));
        Assert.That(error.HasAtom(), Is.True);
        Assert.That(error.HasBond(), Is.True);
        Assert.That(error.IsError(), Is.True);
        Assert.That(error.IsWarning(), Is.False);
    }

    [Test]
    public void DiagnosticConstructors_NormalizeNullTextAndLocation()
    {
        DiagnosticLocation location = new DiagnosticLocation(null, 0, 0, -1, null);
        MoleculeError error = new MoleculeError(null, null, null, -1, -1, ErrorSeverity.Warning);

        Assert.That(location.SourceName, Is.EqualTo(""));
        Assert.That(location.Context, Is.EqualTo(""));
        Assert.That(error.Code, Is.EqualTo(""));
        Assert.That(error.Message, Is.EqualTo(""));
        Assert.That(error.Location.SourceName, Is.EqualTo(""));
        Assert.That(error.HasAtom(), Is.False);
        Assert.That(error.HasBond(), Is.False);
        Assert.That(error.IsError(), Is.False);
        Assert.That(error.IsWarning(), Is.True);
    }

    [Test]
    public void MoleculeReport_AddsAndCountsDiagnosticsBySeverity()
    {
        MoleculeReport report = new MoleculeReport();

        MoleculeError info = report.AddInfo("SOURCE", "read source");
        MoleculeError warning = report.AddWarning("AROMATICITY", "aromaticity was unspecified");
        MoleculeError error = report.AddError("BOND", "invalid bond");
        report.Add(MoleculeError.Create("FATAL", "cannot continue", ErrorSeverity.Fatal));
        report.Add(null);

        Assert.That(report.GetCount(), Is.EqualTo(4));
        Assert.That(report.GetWarningCount(), Is.EqualTo(1));
        Assert.That(report.GetErrorCount(), Is.EqualTo(2));
        Assert.That(report.HasWarnings(), Is.True);
        Assert.That(report.HasErrors(), Is.True);
        Assert.That(report.Get(0), Is.SameAs(info));
        Assert.That(report.Get(1), Is.SameAs(warning));
        Assert.That(report.Get(2), Is.SameAs(error));

        report.Clear();

        Assert.That(report.GetCount(), Is.EqualTo(0));
        Assert.That(report.HasWarnings(), Is.False);
        Assert.That(report.HasErrors(), Is.False);
    }
}
