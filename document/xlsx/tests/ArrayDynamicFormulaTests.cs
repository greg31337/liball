using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ArrayDynamicFormulaTests
{
    [Test]
    public void SetArrayAndDynamicArrayFormulas_WriteSpillRangesAndRoundTrip()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetCellAccessResult array = part.SetArrayFormula("C1", "C1:C3", "A1:A3*2", "2", "n");
        XlsxWorksheetCellAccessResult dynamic = part.SetDynamicArrayFormula("D1", "D1:D4", "UNIQUE(A1:A4)", "1", "n");
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(array.IsValid, Is.True);
        Assert.That(array.CreatedRow, Is.True);
        Assert.That(array.CreatedCell, Is.True);
        Assert.That(dynamic.IsValid, Is.True);
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<f t=\"array\" ref=\"C1:C3\">A1:A3*2</f><v>2</v>"));
        Assert.That(writeResult.Xml, Does.Contain("<f t=\"dynamicArray\" ref=\"D1:D4\" dynamicArray=\"1\">UNIQUE(A1:A4)</f><v>1</v>"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.GetCell("C1")!.FormulaInfo.Type, Is.EqualTo("array"));
        Assert.That(roundTrip.GetCell("C1")!.FormulaInfo.Reference, Is.EqualTo("C1:C3"));
        Assert.That(roundTrip.GetCell("D1")!.FormulaInfo.Type, Is.EqualTo("dynamicArray"));
        Assert.That(roundTrip.GetCell("D1")!.FormulaInfo.DynamicArray, Is.True);
        Assert.That(roundTrip.GetCell("D1")!.FormulaInfo.Reference, Is.EqualTo("D1:D4"));
    }

    [Test]
    public void SetArrayAndDynamicArrayFormulas_ReportInvalidInputs()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetCellAccessResult badCell = part.SetArrayFormula("bad", "A1:A2", "A1:A2", "", "");
        XlsxWorksheetCellAccessResult badRange = part.SetArrayFormula("A1", "bad", "A1:A2", "", "");
        XlsxWorksheetCellAccessResult outsideRange = part.SetDynamicArrayFormula("C1", "A1:A2", "UNIQUE(A1:A2)", "", "");

        Assert.That(badCell.IsValid, Is.False);
        Assert.That(HasDiagnostic(badCell, "a1.cellReference.row"), Is.True);
        Assert.That(badRange.IsValid, Is.False);
        Assert.That(HasDiagnostic(badRange, "a1.cellReference.row"), Is.True);
        Assert.That(outsideRange.IsValid, Is.False);
        Assert.That(HasDiagnostic(outsideRange, "worksheetFormula.anchorOutsideRange"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsInvalidArrayFormulaRanges()
    {
        XlsxWorksheetPart missingRange = CreatePartWithFormula(XlsxCellFormula.CreateArray("A1:A2", ""));
        XlsxWorksheetPart invalidRange = CreatePartWithFormula(XlsxCellFormula.CreateDynamicArray("UNIQUE(A1:A2)", "bad"));

        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();
        XlsxWorksheetPartWriteResult missingRangeResult = writer.WriteBytes(missingRange);
        XlsxWorksheetPartWriteResult invalidRangeResult = writer.WriteBytes(invalidRange);

        Assert.That(missingRangeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRangeResult, "worksheet.cellFormulaRangeMissing"), Is.True);
        Assert.That(invalidRangeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRangeResult, "worksheet.cellFormulaRangeInvalid"), Is.True);
    }

    static XlsxWorksheetPart CreatePartWithFormula(XlsxCellFormula formula)
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = "A1";
        cell.FormulaInfo = formula;
        cell.Formula = formula.Text;
        cell.HasFormula = true;
        row.Cells.Add(cell);
        part.Rows.Add(row);
        return part;
    }

    static bool HasDiagnostic(XlsxWorksheetCellAccessResult result, string code)
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

    static bool HasDiagnostic(XlsxWorksheetPartWriteResult result, string code)
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
