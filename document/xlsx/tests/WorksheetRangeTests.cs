using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetRangeTests
{
    [Test]
    public void RangeClearMethods_ClearValuesAndStylesIndependently()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetNumberValue("A1", 5d);
        part.SetTextValue("B1", "text");

        XlsxWorksheetRangeResult styleResult = part.GetRange("A1:B1").SetStyle(7u);
        Assert.That(styleResult.IsValid, Is.True);
        Assert.That(styleResult.StyledCellCount, Is.EqualTo(2));
        AssertStyle(part.GetCell("A1"), 7u, true);
        AssertStyle(part.GetCell("B1"), 7u, true);

        XlsxWorksheetRangeResult clearValues = part.Range("A1").ClearValues();
        Assert.That(clearValues.IsValid, Is.True);
        Assert.That(clearValues.ClearedCellCount, Is.EqualTo(1));
        AssertCell(part.GetCell("A1"), "", "", "", false);
        AssertStyle(part.GetCell("A1"), 7u, true);

        XlsxWorksheetRangeResult clearStyles = part.GetRange("B1").ClearStyles();
        Assert.That(clearStyles.IsValid, Is.True);
        AssertCell(part.GetCell("B1"), "str", "text", "", false);
        AssertStyle(part.GetCell("B1"), 0u, false);

        XlsxWorksheetRangeResult clearAll = part.GetRange("A1:B1").Clear();
        Assert.That(clearAll.IsValid, Is.True);
        AssertCell(part.GetCell("A1"), "", "", "", false);
        AssertCell(part.GetCell("B1"), "", "", "", false);
        AssertStyle(part.GetCell("A1"), 0u, false);
        AssertStyle(part.GetCell("B1"), 0u, false);
    }

    [Test]
    public void CopyTo_ClonesValuesFormulasStylesAndUnknownXml()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetNumberValue("A1", 12d);
        part.SetFormulaCachedValue("B1", "A1*2", "24", "n");
        part.GetCell("A1")!.Style = 3u;
        part.GetCell("A1")!.HasStyle = true;
        part.GetCell("A1")!.UnknownXml.Add("<ext />");
        part.GetCell("B1")!.Style = 4u;
        part.GetCell("B1")!.HasStyle = true;

        XlsxWorksheetRangeResult result = part.GetRange("A1:B1").CopyTo("C2");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CopiedCellCount, Is.EqualTo(2));
        AssertCell(part.GetCell("C2"), "n", "12", "", false);
        AssertCell(part.GetCell("D2"), "n", "24", "C2*2", true);
        AssertStyle(part.GetCell("C2"), 3u, true);
        AssertStyle(part.GetCell("D2"), 4u, true);
        Assert.That(part.GetCell("C2")!.UnknownXml.Count, Is.EqualTo(1));

        part.SetNumberValue("A1", 99d);
        Assert.That(part.GetCell("C2")!.Value, Is.EqualTo("12"));
    }

    [Test]
    public void MoveTo_CopiesCellsToTargetAndClearsSourceCells()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetTextValue("A1", "left");
        part.SetNumberValue("B1", 8d);
        part.GetCell("B1")!.Style = 9u;
        part.GetCell("B1")!.HasStyle = true;

        XlsxWorksheetRangeResult result = part.GetRange("A1:B1").MoveTo("A3");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.MovedCellCount, Is.EqualTo(2));
        AssertCell(part.GetCell("A3"), "str", "left", "", false);
        AssertCell(part.GetCell("B3"), "n", "8", "", false);
        AssertStyle(part.GetCell("B3"), 9u, true);
        AssertCell(part.GetCell("A1"), "", "", "", false);
        AssertCell(part.GetCell("B1"), "", "", "", false);
        AssertStyle(part.GetCell("B1"), 0u, false);
    }

    [Test]
    public void SetFormula_AppliesFormulaToEveryCellInRange()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetRangeResult result = part.GetRange("C1:D2").SetFormula("A1+1");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FormulaCellCount, Is.EqualTo(4));
        Assert.That(result.CreatedCellCount, Is.EqualTo(4));
        AssertCell(part.GetCell("C1"), "", "", "A1+1", true);
        AssertCell(part.GetCell("D2"), "", "", "A1+1", true);
    }

    [Test]
    public void RangeOperations_ReportInvalidReferencesAndTargets()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetRangeResult invalidRange = part.GetRange("bad").SetStyle(1u);
        XlsxWorksheetRangeResult invalidTarget = part.GetRange("XFC1048576:XFD1048576").CopyTo("XFD1048576");

        Assert.That(invalidRange.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRange, "worksheetRange.referenceInvalid"), Is.True);
        Assert.That(invalidTarget.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidTarget, "worksheetRange.targetOutsideWorksheet"), Is.True);
    }

    static void AssertCell(XlsxWorksheetCell? cell, string type, string value, string formula, bool hasFormula)
    {
        Assert.That(cell, Is.Not.Null);
        Assert.That(cell!.Type, Is.EqualTo(type));
        Assert.That(cell.Value, Is.EqualTo(value));
        Assert.That(cell.Formula, Is.EqualTo(formula));
        Assert.That(cell.HasFormula, Is.EqualTo(hasFormula));
    }

    static void AssertStyle(XlsxWorksheetCell? cell, uint style, bool hasStyle)
    {
        Assert.That(cell, Is.Not.Null);
        Assert.That(cell!.Style, Is.EqualTo(style));
        Assert.That(cell.HasStyle, Is.EqualTo(hasStyle));
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
