using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetStructuralEditUpdateTests
{
    [Test]
    public void InsertRows_UpdatesFormulaAndWorksheetStructuralReferences()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetFormulaCachedValue("A1", "SUM(A2:B3)", "", "");
        part.MergeCells("A2:B3");
        part.AddInternalHyperlink("C3", "Sheet1!A1", "link", "");
        part.AddCommentWithVisibility("D3", "me", "note", true);
        part.RowBreaks.Add(CreatePageBreak(3));

        XlsxWorksheetRowEditResult result = part.InsertRows(2, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FormulaReferenceUpdateCount, Is.EqualTo(1));
        Assert.That(part.GetCell("A1")!.Formula, Is.EqualTo("SUM(A3:B4)"));
        Assert.That(part.MergedCells[0].Reference, Is.EqualTo("A3:B4"));
        Assert.That(part.Hyperlinks[0].Reference, Is.EqualTo("C4"));
        Assert.That(part.CommentsPart.Comments[0].Reference, Is.EqualTo("D4"));
        Assert.That(part.CommentsPart.Comments[0].Row, Is.EqualTo(3));
        Assert.That(part.CommentsPart.Comments[0].Column, Is.EqualTo(3));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Reference, Is.EqualTo("D4"));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Row, Is.EqualTo(3));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Column, Is.EqualTo(3));
        Assert.That(part.RowBreaks[0].Id, Is.EqualTo(4));
    }

    [Test]
    public void DeleteRows_RemovesDeletedStructuresAndUpdatesSurvivors()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetFormulaCachedValue("A1", "B2+C5+SUM(A1:A5)", "", "");
        part.MergeCells("A2:B3");
        part.AddInternalHyperlink("C2", "Sheet1!A1", "link", "");
        part.AddCommentWithVisibility("D2", "me", "note", false);
        part.RowBreaks.Add(CreatePageBreak(2));
        part.RowBreaks.Add(CreatePageBreak(5));

        XlsxWorksheetRowEditResult result = part.DeleteRows(2, 2);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.DeletedFormulaReferenceCount, Is.EqualTo(1));
        Assert.That(part.GetCell("A1")!.Formula, Is.EqualTo("#REF!+C3+SUM(A1:A3)"));
        Assert.That(part.MergedCells.Count, Is.EqualTo(0));
        Assert.That(part.Hyperlinks.Count, Is.EqualTo(0));
        Assert.That(part.CommentsPart.Comments.Count, Is.EqualTo(0));
        Assert.That(part.LegacyCommentDrawingPart.Drawings.Count, Is.EqualTo(0));
        Assert.That(part.RowBreaks.Count, Is.EqualTo(1));
        Assert.That(part.RowBreaks[0].Id, Is.EqualTo(3));
    }

    [Test]
    public void InsertColumns_UpdatesFormulaAndWorksheetStructuralReferences()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.SetFormulaCachedValue("A1", "SUM(B1:C2)", "", "");
        part.MergeCells("B1:C2");
        part.AddInternalHyperlink("D1", "Sheet1!A1", "link", "");
        part.AddCommentWithVisibility("E2", "me", "note", true);
        part.ColumnBreaks.Add(CreatePageBreak(4));

        XlsxWorksheetColumnEditResult result = part.InsertColumns(2, 1);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.FormulaReferenceUpdateCount, Is.EqualTo(1));
        Assert.That(part.GetCell("A1")!.Formula, Is.EqualTo("SUM(C1:D2)"));
        Assert.That(part.MergedCells[0].Reference, Is.EqualTo("C1:D2"));
        Assert.That(part.Hyperlinks[0].Reference, Is.EqualTo("E1"));
        Assert.That(part.CommentsPart.Comments[0].Reference, Is.EqualTo("F2"));
        Assert.That(part.LegacyCommentDrawingPart.Drawings[0].Reference, Is.EqualTo("F2"));
        Assert.That(part.ColumnBreaks[0].Id, Is.EqualTo(5));
    }

    [Test]
    public void WorkbookDefinedNameUpdates_AdjustLocalPrintAreaReferences()
    {
        XlsxWorkbookPart workbook = new XlsxWorkbookPart();
        workbook.SetPrintArea(0, "Sheet1", "$A$1:$B$2");

        XlsxFormulaReferenceUpdateResult rowInsert = workbook.UpdateDefinedNameReferencesForRowInsert(0, 2, 1);
        XlsxFormulaReferenceUpdateResult columnInsert = workbook.UpdateDefinedNameReferencesForColumnInsert(0, 2, 1);

        Assert.That(rowInsert.IsValid, Is.True);
        Assert.That(columnInsert.IsValid, Is.True);
        Assert.That(workbook.GetPrintArea(0)!.Text, Is.EqualTo("Sheet1!$A$1:$C$3"));
        Assert.That(rowInsert.UpdatedReferenceCount, Is.EqualTo(1));
        Assert.That(columnInsert.UpdatedReferenceCount, Is.EqualTo(1));
    }

    static XlsxWorksheetPageBreak CreatePageBreak(uint id)
    {
        XlsxWorksheetPageBreak pageBreak = new XlsxWorksheetPageBreak();
        pageBreak.Id = id;
        pageBreak.HasId = true;
        pageBreak.Manual = true;
        pageBreak.HasManual = true;
        return pageBreak;
    }
}
