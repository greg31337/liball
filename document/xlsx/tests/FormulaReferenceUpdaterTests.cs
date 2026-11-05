using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class FormulaReferenceUpdaterTests
{
    [Test]
    public void UpdateFormulaForRowInsert_ShiftsReferencesAndExpandsRanges()
    {
        XlsxFormulaReferenceUpdateResult result = XlsxWorksheetPart.UpdateFormulaForRowInsert("SUM(A1:B2)+$C$3", 2, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Formula, Is.EqualTo("SUM(A1:B3)+$C$4"));
        Assert.That(result.UpdatedReferenceCount, Is.EqualTo(2));
        Assert.That(result.DeletedReferenceCount, Is.EqualTo(0));
    }

    [Test]
    public void UpdateFormulaForRowDelete_RewritesDeletedCellsAndShrinksRanges()
    {
        XlsxFormulaReferenceUpdateResult result = XlsxWorksheetPart.UpdateFormulaForRowDelete("A1+B2+C5+SUM(A1:A5)", 2, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Formula, Is.EqualTo("A1+#REF!+C4+SUM(A1:A4)"));
        Assert.That(result.UpdatedReferenceCount, Is.EqualTo(3));
        Assert.That(result.DeletedReferenceCount, Is.EqualTo(1));
    }

    [Test]
    public void UpdateFormulaForColumnOperations_ShiftsDeletesAndShrinksRanges()
    {
        XlsxFormulaReferenceUpdateResult inserted = XlsxWorksheetPart.UpdateFormulaForColumnInsert("A1+$B$2+C1:D1", 2, 1);
        XlsxFormulaReferenceUpdateResult deleted = XlsxWorksheetPart.UpdateFormulaForColumnDelete("A1+B1+D1+SUM(A1:D1)", 2, 1);

        Assert.That(inserted.IsValid, Is.True);
        Assert.That(inserted.Formula, Is.EqualTo("A1+$C$2+D1:E1"));
        Assert.That(inserted.UpdatedReferenceCount, Is.EqualTo(2));
        Assert.That(deleted.IsValid, Is.True);
        Assert.That(deleted.Formula, Is.EqualTo("A1+#REF!+C1+SUM(A1:C1)"));
        Assert.That(deleted.UpdatedReferenceCount, Is.EqualTo(3));
        Assert.That(deleted.DeletedReferenceCount, Is.EqualTo(1));
    }

    [Test]
    public void CopyFormulaReferences_ShiftsOnlyRelativeReferenceParts()
    {
        XlsxFormulaReferenceUpdateResult result = XlsxWorksheetPart.CopyFormulaReferences("A1+$A1+A$1+$A$1", 1, 2);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Formula, Is.EqualTo("C2+$A2+C$1+$A$1"));
        Assert.That(result.UpdatedReferenceCount, Is.EqualTo(3));
    }

    [Test]
    public void MoveFormulaReferences_ShiftsReferencesInsideSourceRangeIncludingAbsoluteCoordinates()
    {
        XlsxFormulaReferenceUpdateResult result = XlsxWorksheetPart.MoveFormulaReferences("A1+B2+C3+$A$1", 1, 1, 2, 2, 2, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Formula, Is.EqualTo("B3+C4+C3+$B$3"));
        Assert.That(result.UpdatedReferenceCount, Is.EqualTo(3));
    }

    [Test]
    public void RenameFormulaSheetReferences_RequotesNewSheetNames()
    {
        XlsxFormulaReferenceUpdateResult simple = XlsxWorksheetPart.RenameFormulaSheetReferences("SUM(Sheet1!A1,'Old Sheet'!B2,Other!C3)", "Sheet1", "New Sheet");
        XlsxFormulaReferenceUpdateResult quoted = XlsxWorksheetPart.RenameFormulaSheetReferences("'Old Sheet'!A1+Sheet1!B1", "Old Sheet", "Data");

        Assert.That(simple.IsValid, Is.True);
        Assert.That(simple.Formula, Is.EqualTo("SUM('New Sheet'!A1,'Old Sheet'!B2,Other!C3)"));
        Assert.That(simple.RenamedSheetCount, Is.EqualTo(1));
        Assert.That(quoted.IsValid, Is.True);
        Assert.That(quoted.Formula, Is.EqualTo("Data!A1+Sheet1!B1"));
        Assert.That(quoted.RenamedSheetCount, Is.EqualTo(1));
    }

    [Test]
    public void WorksheetReferenceUpdater_RewritesLoadedFormulaCells()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = CreateRow(1);
        row.Cells.Add(CreateFormulaCell("A1", "B1+C1"));
        part.Rows.Add(row);

        XlsxFormulaReferenceUpdateResult result = part.UpdateFormulaReferencesForColumnDelete(2, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.UpdatedReferenceCount, Is.EqualTo(2));
        Assert.That(result.DeletedReferenceCount, Is.EqualTo(1));
        Assert.That(part.GetCell("A1")!.FormulaInfo.Text, Is.EqualTo("#REF!+B1"));
        Assert.That(part.GetCell("A1")!.Formula, Is.EqualTo("#REF!+B1"));
    }

    static XlsxWorksheetRow CreateRow(uint rowIndex)
    {
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = rowIndex;
        return row;
    }

    static XlsxWorksheetCell CreateFormulaCell(string reference, string formula)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Formula = formula;
        cell.FormulaInfo = XlsxCellFormula.CreateNormal(formula);
        cell.HasFormula = true;
        return cell;
    }
}
