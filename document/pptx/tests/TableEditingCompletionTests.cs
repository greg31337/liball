using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TableEditingCompletionTests
{
    [Test]
    public void TableOperations_InsertAndRemoveRowsColumnsPreserveDataSizingAndCoordinates()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(2, 2, new PptxBounds(0L, 0L, 400L, 200L));
        PptxTableOperations tables = new PptxTableOperations();
        table.SetData(CreateTwoByTwoData());

        tables.InsertRow(table, 1, 150L);
        tables.InsertColumn(table, 1, 250L);
        tables.SetText(table, 1, 0, "X");
        tables.SetText(table, 1, 1, "Y");
        tables.SetText(table, 1, 2, "Z");
        tables.SetText(table, 2, 1, "N2");
        tables.RemoveRow(table, 0);
        tables.RemoveColumn(table, 2);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxTable reopenedTable = reopened.GetSlide(0).GetTable(0);
        PptxTableCell cell = reopenedTable.GetCell(1, 1);
        PptxTableCellOperations cells = new PptxTableCellOperations();

        Assert.That(reopenedTable.RowCount(), Is.EqualTo(2));
        Assert.That(reopenedTable.ColumnCount(), Is.EqualTo(2));
        Assert.That(reopenedTable.GetText(0, 0), Is.EqualTo("X"));
        Assert.That(reopenedTable.GetText(0, 1), Is.EqualTo("Y"));
        Assert.That(reopenedTable.GetText(1, 0), Is.EqualTo("C"));
        Assert.That(reopenedTable.GetText(1, 1), Is.EqualTo("N2"));
        Assert.That(reopenedTable.GetRowHeight(0), Is.EqualTo(150L));
        Assert.That(reopenedTable.GetColumnWidth(1), Is.EqualTo(250L));
        Assert.That(cells.GetRowIndex(cell), Is.EqualTo(1));
        Assert.That(cells.GetColumnIndex(cell), Is.EqualTo(1));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void TableOperations_ImportAndExportBuiltInInterfacesResizeTable()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(1, 1, new PptxBounds(0L, 0L, 300L, 100L));
        PptxTableOperations tables = new PptxTableOperations();
        List<System.Collections.Generic.IEnumerable<string>> source = new List<System.Collections.Generic.IEnumerable<string>>();
        source.Add(new string[] { "North", "South" });
        source.Add(new List<string>() { "East", "West" });
        source.Add(new string[] { "Up", "Down" });

        tables.ImportData(table, source);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxTable reopenedTable = reopened.GetSlide(0).GetTable(0);
        System.Collections.Generic.IEnumerable<System.Collections.Generic.IReadOnlyList<string>> exported = tables.ExportData(reopenedTable);
        List<System.Collections.Generic.IReadOnlyList<string>> rows = new List<System.Collections.Generic.IReadOnlyList<string>>();
        foreach (System.Collections.Generic.IReadOnlyList<string> row in exported)
        {
            rows.Add(row);
        }

        Assert.That(reopenedTable.RowCount(), Is.EqualTo(3));
        Assert.That(reopenedTable.ColumnCount(), Is.EqualTo(2));
        Assert.That(rows, Has.Count.EqualTo(3));
        Assert.That(rows[0][0], Is.EqualTo("North"));
        Assert.That(rows[1][1], Is.EqualTo("West"));
        Assert.That(rows[2][0], Is.EqualTo("Up"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void TableOperations_StructuralEditingAndMergeRejectInvalidRegions()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(3, 3, new PptxBounds(0L, 0L, 600L, 300L));
        PptxTableOperations tables = new PptxTableOperations();
        tables.MergeCells(table, 0, 0, 2, 2);

        Assert.That(() => tables.MergeCells(table, 1, 1, 2, 2), Throws.ArgumentException);
        Assert.That(() => tables.InsertRow(table, 1, 100L), Throws.ArgumentException);
        Assert.That(() => tables.RemoveColumn(table, 2), Throws.ArgumentException);

        tables.SplitCell(table, 1, 1);
        Assert.That(() => tables.SplitCell(table, 0, 0), Throws.ArgumentException);
        tables.InsertRow(table, 1, 100L);
        tables.RemoveColumn(table, 2);

        Assert.That(table.RowCount(), Is.EqualTo(4));
        Assert.That(table.ColumnCount(), Is.EqualTo(2));
    }

    [Test]
    public void TableOperations_RejectRemovalOfLastDimensionAndRaggedInterfaceData()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(1, 1, new PptxBounds(0L, 0L, 100L, 100L));
        PptxTableOperations tables = new PptxTableOperations();
        List<System.Collections.Generic.IEnumerable<string>> ragged = new List<System.Collections.Generic.IEnumerable<string>>();
        ragged.Add(new string[] { "A", "B" });
        ragged.Add(new string[] { "C" });

        Assert.That(() => tables.RemoveRow(table, 0), Throws.ArgumentException);
        Assert.That(() => tables.RemoveColumn(table, 0), Throws.ArgumentException);
        Assert.That(() => tables.ImportData(table, ragged), Throws.ArgumentException);
        Assert.That(table.RowCount(), Is.EqualTo(1));
        Assert.That(table.ColumnCount(), Is.EqualTo(1));
    }

    static List<List<string>> CreateTwoByTwoData()
    {
        List<List<string>> data = new List<List<string>>();
        data.Add(new List<string>() { "A", "B" });
        data.Add(new List<string>() { "C", "D" });
        return data;
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
