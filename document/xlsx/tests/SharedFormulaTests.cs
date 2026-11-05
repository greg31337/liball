using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SharedFormulaTests
{
    [Test]
    public void ExpandSharedFormulas_ExpandsFollowersAndKeepsCachedValues()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<sheetData>" +
            "<row r=\"1\"><c r=\"B1\"><f t=\"shared\" ref=\"B1:B3\" si=\"0\">A1*2</f><v>2</v></c></row>" +
            "<row r=\"2\"><c r=\"B2\"><f t=\"shared\" si=\"0\" /><v>4</v></c></row>" +
            "<row r=\"3\"><c r=\"B3\"><f t=\"shared\" si=\"0\" /><v>6</v></c></row>" +
            "</sheetData>" +
            "</worksheet>";
        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);

        XlsxSharedFormulaResult result = part.ExpandSharedFormulas();
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);

        Assert.That(part.IsValid, Is.True);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ExpandedFormulaCount, Is.EqualTo(2));
        AssertExpandedFormula(part.GetCell("B1"), "A1*2", "2");
        AssertExpandedFormula(part.GetCell("B2"), "A2*2", "4");
        AssertExpandedFormula(part.GetCell("B3"), "A3*2", "6");
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<f>A2*2</f><v>4</v>"));
        Assert.That(writeResult.Xml, Does.Not.Contain("t=\"shared\""));
    }

    [Test]
    public void TranslateFormulaReferences_AdjustsOnlyRelativeReferenceParts()
    {
        string translated = XlsxWorksheetPart.TranslateFormulaReferences("A1+$A$1+B$1+$A1", 1, 1);
        string normalized = XlsxWorksheetPart.NormalizeFormulaForShared("A2+$A$1+B$1+$A2", 2, 2);

        Assert.That(translated, Is.EqualTo("B2+$A$1+C$1+$A2"));
        Assert.That(normalized, Is.EqualTo("R[0]C[-1]+R$1C$1+R$1C[0]+R[0]C$1"));
    }

    [Test]
    public void CompactSharedFormulas_CreatesSharedMasterAndFollowers()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row1 = CreateRow(1);
        XlsxWorksheetRow row2 = CreateRow(2);
        row1.Cells.Add(CreateFormulaCell("B1", "A1*2", "2"));
        row2.Cells.Add(CreateFormulaCell("B2", "A2*2", "4"));
        part.Rows.Add(row1);
        part.Rows.Add(row2);

        XlsxSharedFormulaResult result = part.CompactSharedFormulas();
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CompactedGroupCount, Is.EqualTo(1));
        Assert.That(result.CompactedFollowerCount, Is.EqualTo(1));
        Assert.That(part.GetCell("B1")!.FormulaInfo.Type, Is.EqualTo("shared"));
        Assert.That(part.GetCell("B1")!.FormulaInfo.Reference, Is.EqualTo("B1:B2"));
        Assert.That(part.GetCell("B1")!.FormulaInfo.SharedIndex, Is.EqualTo(0));
        Assert.That(part.GetCell("B1")!.FormulaInfo.Text, Is.EqualTo("A1*2"));
        Assert.That(part.GetCell("B2")!.FormulaInfo.Type, Is.EqualTo("shared"));
        Assert.That(part.GetCell("B2")!.FormulaInfo.Text, Is.EqualTo(""));
        Assert.That(part.GetCell("B2")!.Value, Is.EqualTo("4"));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<f t=\"shared\" ref=\"B1:B2\" si=\"0\">A1*2</f><v>2</v>"));
        Assert.That(writeResult.Xml, Does.Contain("<f t=\"shared\" si=\"0\" />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.GetCell("B2")!.FormulaInfo.HasSharedIndex, Is.True);
    }

    [Test]
    public void SharedFormulaHelpers_ReportMissingMasters()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = CreateRow(1);
        XlsxWorksheetCell cell = CreateFormulaCell("B1", "", "2");
        cell.FormulaInfo.Type = "shared";
        cell.FormulaInfo.SharedIndex = 5;
        cell.FormulaInfo.HasSharedIndex = true;
        row.Cells.Add(cell);
        part.Rows.Add(row);

        XlsxSharedFormulaResult result = part.ExpandSharedFormulas();

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "sharedFormula.masterMissing"), Is.True);
    }

    static XlsxWorksheetRow CreateRow(uint rowIndex)
    {
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = rowIndex;
        return row;
    }

    static XlsxWorksheetCell CreateFormulaCell(string reference, string formulaText, string cachedValue)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Type = "n";
        cell.Value = cachedValue;
        cell.Formula = formulaText;
        cell.FormulaInfo = XlsxCellFormula.CreateNormal(formulaText);
        cell.FormulaInfo.CachedValue = cachedValue;
        cell.FormulaInfo.CachedType = "n";
        cell.HasFormula = true;
        return cell;
    }

    static void AssertExpandedFormula(XlsxWorksheetCell? cell, string formulaText, string cachedValue)
    {
        Assert.That(cell!.HasFormula, Is.True);
        Assert.That(cell.FormulaInfo.Type, Is.EqualTo(""));
        Assert.That(cell.FormulaInfo.Text, Is.EqualTo(formulaText));
        Assert.That(cell.Formula, Is.EqualTo(formulaText));
        Assert.That(cell.Value, Is.EqualTo(cachedValue));
    }

    static bool HasDiagnostic(XlsxSharedFormulaResult result, string code)
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
