using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetFormulaModelTests
{
    [Test]
    public void ReadString_ParsesFormulaKindsAndCachedValues()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\">" +
            "<sheetData><row r=\"1\">" +
            "<c r=\"A1\"><f>A2+A3</f><v>3</v></c>" +
            "<c r=\"B1\"><f t=\"shared\" ref=\"B1:B3\" si=\"2\">SUM(A1:A3)</f><v>6</v></c>" +
            "<c r=\"B2\"><f t=\"shared\" si=\"2\" /><v>6</v></c>" +
            "<c r=\"C1\"><f t=\"array\" ref=\"C1:C2\" aca=\"1\">A1:A2*2</f><v>2</v></c>" +
            "<c r=\"D1\"><f t=\"dynamicArray\" ref=\"D1:D3\" dynamicArray=\"1\">UNIQUE(A1:A3)</f><v>1</v></c>" +
            "<c r=\"E1\"><f t=\"dataTable\" ref=\"E1:F2\" dt2D=\"1\" dtr=\"0\" del1=\"0\" del2=\"1\" r1=\"A1\" r2=\"B1\" ca=\"1\" bx=\"0\" x:future=\"f\">TABLE()</f><v>9</v></c>" +
            "</row></sheetData>" +
            "</worksheet>";

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        AssertFormula(part.GetCell("A1"), "", "A2+A3", "3", "");
        AssertFormula(part.GetCell("B1"), "shared", "SUM(A1:A3)", "6", "B1:B3");
        Assert.That(part.GetCell("B1")!.FormulaInfo.SharedIndex, Is.EqualTo(2));
        AssertFormula(part.GetCell("B2"), "shared", "", "6", "");
        Assert.That(part.GetCell("B2")!.FormulaInfo.HasSharedIndex, Is.True);
        AssertFormula(part.GetCell("C1"), "array", "A1:A2*2", "2", "C1:C2");
        Assert.That(part.GetCell("C1")!.FormulaInfo.AlwaysCalculateArray, Is.True);
        AssertFormula(part.GetCell("D1"), "dynamicArray", "UNIQUE(A1:A3)", "1", "D1:D3");
        Assert.That(part.GetCell("D1")!.FormulaInfo.DynamicArray, Is.True);
        AssertFormula(part.GetCell("E1"), "dataTable", "TABLE()", "9", "E1:F2");
        Assert.That(part.GetCell("E1")!.FormulaInfo.DataTable2D, Is.True);
        Assert.That(part.GetCell("E1")!.FormulaInfo.DataTableRow, Is.False);
        Assert.That(part.GetCell("E1")!.FormulaInfo.Input2Deleted, Is.True);
        Assert.That(part.GetCell("E1")!.FormulaInfo.Input1Reference, Is.EqualTo("A1"));
        Assert.That(part.GetCell("E1")!.FormulaInfo.Input2Reference, Is.EqualTo("B1"));
        Assert.That(part.GetCell("E1")!.FormulaInfo.CalculateCell, Is.True);
        Assert.That(part.GetCell("E1")!.FormulaInfo.AssignsToName, Is.False);
        Assert.That(part.GetCell("E1")!.FormulaInfo.UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesFormulaKindsCachedValuesAndRoundTrips()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = 1;
        row.Cells.Add(CreateFormulaCell("A1", XlsxCellFormula.CreateNormal("A2+A3"), "3", "n"));
        row.Cells.Add(CreateFormulaCell("B1", XlsxCellFormula.CreateShared("SUM(A1:A3)", 2, "B1:B3"), "6", "n"));
        row.Cells.Add(CreateFormulaCell("B2", XlsxCellFormula.CreateShared("", 2, ""), "6", "n"));
        row.Cells.Add(CreateFormulaCell("C1", XlsxCellFormula.CreateArray("A1:A2*2", "C1:C2"), "2", "n"));
        row.Cells.Add(CreateFormulaCell("D1", XlsxCellFormula.CreateDynamicArray("UNIQUE(A1:A3)", "D1:D3"), "1", "n"));
        row.Cells.Add(CreateFormulaCell("E1", CreateDataTableFormula(), "9", "n"));
        part.Rows.Add(row);

        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<f>A2+A3</f><v>3</v>"));
        Assert.That(result.Xml, Does.Contain("<f t=\"shared\" ref=\"B1:B3\" si=\"2\">SUM(A1:A3)</f><v>6</v>"));
        Assert.That(result.Xml, Does.Contain("<f t=\"shared\" si=\"2\" />"));
        Assert.That(result.Xml, Does.Contain("<f t=\"array\" ref=\"C1:C2\">A1:A2*2</f><v>2</v>"));
        Assert.That(result.Xml, Does.Contain("<f t=\"dynamicArray\" ref=\"D1:D3\" dynamicArray=\"1\">UNIQUE(A1:A3)</f><v>1</v>"));
        Assert.That(result.Xml, Does.Contain("<f t=\"dataTable\" ref=\"E1:F2\" ca=\"1\" dt2D=\"1\" dtr=\"0\" del1=\"0\" del2=\"1\" r1=\"A1\" r2=\"B1\">TABLE()</f><v>9</v>"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.GetCell("B1")!.FormulaInfo.Type, Is.EqualTo("shared"));
        Assert.That(roundTrip.GetCell("B2")!.HasFormula, Is.True);
        Assert.That(roundTrip.GetCell("B2")!.FormulaInfo.Text, Is.EqualTo(""));
        Assert.That(roundTrip.GetCell("D1")!.FormulaInfo.DynamicArray, Is.True);
        Assert.That(roundTrip.GetCell("E1")!.FormulaInfo.Type, Is.EqualTo("dataTable"));
    }

    [Test]
    public void WriteString_UsesFormulaInfoCachedValueWhenCellValueIsEmpty()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = "A1";
        cell.FormulaInfo = XlsxCellFormula.CreateNormal("A2*2");
        cell.FormulaInfo.CachedValue = "10";
        cell.FormulaInfo.CachedType = "n";
        cell.Formula = cell.FormulaInfo.Text;
        cell.HasFormula = true;
        row.Cells.Add(cell);
        part.Rows.Add(row);

        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<c r=\"A1\" t=\"n\"><f>A2*2</f><v>10</v></c>"));
        Assert.That(roundTrip.GetCell("A1")!.Value, Is.EqualTo("10"));
        Assert.That(roundTrip.GetCell("A1")!.FormulaInfo.CachedValue, Is.EqualTo("10"));
    }

    [Test]
    public void WriteBytes_RejectsInvalidFormulaModels()
    {
        XlsxWorksheetPart? nullFormula = CreatePartWithFormula("A1", null!);
        XlsxWorksheetPart missingSharedIndex = CreatePartWithFormula("A1", XlsxCellFormula.CreateNormal("A2"));
        XlsxWorksheetPart unsupportedType = CreatePartWithFormula("A1", XlsxCellFormula.CreateNormal("A2"));
        XlsxWorksheetPart nullUnknownAttributes = CreatePartWithFormula("A1", XlsxCellFormula.CreateNormal("A2"));

        nullFormula.Rows[0].Cells[0].HasFormula = true;
        nullFormula.Rows[0].Cells[0].FormulaInfo = null!;
        missingSharedIndex.Rows[0].Cells[0].FormulaInfo.Type = "shared";
        unsupportedType.Rows[0].Cells[0].FormulaInfo.Type = "future";
        nullUnknownAttributes.Rows[0].Cells[0].FormulaInfo.UnknownAttributes = null!;

        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();
        XlsxWorksheetPartWriteResult nullFormulaResult = writer.WriteBytes(nullFormula);
        XlsxWorksheetPartWriteResult missingSharedIndexResult = writer.WriteBytes(missingSharedIndex);
        XlsxWorksheetPartWriteResult unsupportedTypeResult = writer.WriteBytes(unsupportedType);
        XlsxWorksheetPartWriteResult nullUnknownAttributesResult = writer.WriteBytes(nullUnknownAttributes);

        Assert.That(HasDiagnostic(nullFormulaResult, "worksheet.cellFormulaNull"), Is.True);
        Assert.That(HasDiagnostic(missingSharedIndexResult, "worksheet.cellFormulaSharedIndexMissing"), Is.True);
        Assert.That(HasDiagnostic(unsupportedTypeResult, "worksheet.cellFormulaTypeUnsupported"), Is.True);
        Assert.That(HasDiagnostic(nullUnknownAttributesResult, "worksheet.cellFormulaUnknownAttributesNull"), Is.True);
    }

    static XlsxCellFormula CreateDataTableFormula()
    {
        XlsxCellFormula formula = XlsxCellFormula.CreateDataTable("TABLE()", "E1:F2", "A1", "B1");
        formula.DataTable2D = true;
        formula.HasDataTable2D = true;
        formula.DataTableRow = false;
        formula.HasDataTableRow = true;
        formula.Input1Deleted = false;
        formula.HasInput1Deleted = true;
        formula.Input2Deleted = true;
        formula.HasInput2Deleted = true;
        formula.CalculateCell = true;
        formula.HasCalculateCell = true;
        return formula;
    }

    static XlsxWorksheetCell CreateFormulaCell(string reference, XlsxCellFormula formula, string cachedValue, string cachedType)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.FormulaInfo = formula;
        cell.Formula = formula.Text;
        cell.Value = cachedValue;
        cell.Type = cachedType;
        cell.HasFormula = true;
        formula.CachedValue = cachedValue;
        formula.CachedType = cachedType;
        return cell;
    }

    static XlsxWorksheetPart CreatePartWithFormula(string reference, XlsxCellFormula formula)
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.FormulaInfo = formula;
        cell.HasFormula = true;
        if (formula != null)
        {
            cell.Formula = formula.Text;
        }

        row.Cells.Add(cell);
        part.Rows.Add(row);
        return part;
    }

    static void AssertFormula(XlsxWorksheetCell? cell, string type, string text, string cachedValue, string reference)
    {
        Assert.That(cell!.HasFormula, Is.True);
        Assert.That(cell.Formula, Is.EqualTo(text));
        Assert.That(cell.FormulaInfo.Type, Is.EqualTo(type));
        Assert.That(cell.FormulaInfo.Text, Is.EqualTo(text));
        Assert.That(cell.FormulaInfo.CachedValue, Is.EqualTo(cachedValue));
        Assert.That(cell.FormulaInfo.Reference, Is.EqualTo(reference));
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
