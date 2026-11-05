using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetRangeFillTests
{
    [Test]
    public void FillValue_WritesConstantValuesAcrossRange()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.GetOrCreateCellByReference("A1").Cell.Style = 6u;
        part.GetCell("A1")!.HasStyle = true;

        XlsxWorksheetRangeResult result = part.GetRange("A1:B2").FillText("same");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FilledCellCount, Is.EqualTo(4));
        Assert.That(result.CreatedCellCount, Is.EqualTo(3));
        AssertCell(part.GetCell("A1"), "str", "same", "", false);
        AssertCell(part.GetCell("B1"), "str", "same", "", false);
        AssertCell(part.GetCell("A2"), "str", "same", "", false);
        AssertCell(part.GetCell("B2"), "str", "same", "", false);
        Assert.That(part.GetCell("A1")!.Style, Is.EqualTo(6u));
    }

    [Test]
    public void FillFormula_AdjustsRelativeReferencesFromTopLeftCell()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetRangeResult result = part.GetRange("B2:C3").FillFormula("A1+$A$1");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FilledCellCount, Is.EqualTo(4));
        Assert.That(result.FormulaCellCount, Is.EqualTo(4));
        AssertCell(part.GetCell("B2"), "", "", "A1+$A$1", true);
        AssertCell(part.GetCell("C2"), "", "", "B1+$A$1", true);
        AssertCell(part.GetCell("B3"), "", "", "A2+$A$1", true);
        AssertCell(part.GetCell("C3"), "", "", "B2+$A$1", true);
    }

    [Test]
    public void FillLinearSeries_WritesRowMajorNumberSeries()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetRangeResult result = part.GetRange("A1:B2").FillSeries(10d, 2.5d);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FilledCellCount, Is.EqualTo(4));
        Assert.That(result.SeriesCellCount, Is.EqualTo(4));
        AssertCell(part.GetCell("A1"), "n", "10", "", false);
        AssertCell(part.GetCell("B1"), "n", "12.5", "", false);
        AssertCell(part.GetCell("A2"), "n", "15", "", false);
        AssertCell(part.GetCell("B2"), "n", "17.5", "", false);
    }

    [Test]
    public void FillOperations_ReportInvalidInputs()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetRangeResult? nullValue = part.GetRange("A1").FillValue(null!);
        XlsxWorksheetRangeResult badRange = part.GetRange("bad").FillNumber(1d);

        Assert.That(nullValue.IsValid, Is.False);
        Assert.That(badRange.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullValue, "worksheetRange.valueNull"), Is.True);
        Assert.That(HasDiagnostic(badRange, "worksheetRange.referenceInvalid"), Is.True);
    }

    static void AssertCell(XlsxWorksheetCell? cell, string type, string value, string formula, bool hasFormula)
    {
        Assert.That(cell, Is.Not.Null);
        Assert.That(cell!.Type, Is.EqualTo(type));
        Assert.That(cell.Value, Is.EqualTo(value));
        Assert.That(cell.Formula, Is.EqualTo(formula));
        Assert.That(cell.HasFormula, Is.EqualTo(hasFormula));
    }

    static bool HasDiagnostic(XlsxWorksheetRangeResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
