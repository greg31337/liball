using System.Text;

namespace odf.Tests;

public class TableStructureEditorTests
{
    [Test]
    public void InsertsAndDeletesSparseRowsWhileUpdatingReferences()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1' table:print-ranges='.A1:.B3'>"
            + "<table:table-row table:number-rows-repeated='3'>"
            + "<table:table-cell table:number-columns-repeated='2'/>"
            + "</table:table-row>"
            + "<table:table-row><table:table-cell table:formula='of:=[.A3]+[.$A$3]'/></table:table-row>"
            + "</table:table>");

        OdfTable inserted = TableStructureEditor.InsertRows(
            document,
            document.Tables[0],
            1,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(inserted.RowCount, Is.EqualTo(6));
            Assert.That(inserted.Rows.Any(row => row.RepeatCount == 5), Is.True);
            Assert.That(inserted.PrintRangeText, Is.EqualTo(".A1:.B5"));
            Assert.That(
                TableDocumentModel.GetCell(inserted, 5, 0)!.Formula,
                Is.EqualTo("of:=[.A5]+[.$A$5]"));
        });

        OdfTable restored = TableStructureEditor.DeleteRows(
            document,
            inserted,
            1,
            2);
        OdfTable reread = WriteAndRead(document).Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(restored.RowCount, Is.EqualTo(4));
            Assert.That(restored.PrintRangeText, Is.EqualTo(".A1:.B3"));
            Assert.That(
                TableDocumentModel.GetCell(restored, 3, 0)!.Formula,
                Is.EqualTo("of:=[.A3]+[.$A$3]"));
            Assert.That(reread.RowCount, Is.EqualTo(4));
            Assert.That(reread.PrintRangeText, Is.EqualTo(".A1:.B3"));
        });
    }

    [Test]
    public void InsertsAndDeletesColumnsAndCellsWithoutExpandingRows()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'>"
            + "<table:table-column table:number-columns-repeated='4'/>"
            + "<table:table-row table:number-rows-repeated='20'>"
            + "<table:table-cell table:number-columns-repeated='4'/>"
            + "</table:table-row>"
            + "</table:table>");

        OdfTable columns = TableStructureEditor.InsertColumns(
            document,
            document.Tables[0],
            2,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(columns.RowCount, Is.EqualTo(20));
            Assert.That(columns.ColumnCount, Is.EqualTo(7));
            Assert.That(columns.Rows, Has.Length.EqualTo(1));
            Assert.That(columns.Rows[0].RepeatCount, Is.EqualTo(20));
        });

        OdfTable cells = TableStructureEditor.InsertCells(
            document,
            columns,
            5,
            1,
            2);
        Assert.That(cells.Rows, Has.Length.EqualTo(3));
        Assert.That(TableDocumentModel.GetCell(cells, 5, 8), Is.Not.Null);

        OdfTable cellsDeleted = TableStructureEditor.DeleteCells(
            document,
            cells,
            5,
            1,
            2);
        OdfTable columnsDeleted = TableStructureEditor.DeleteColumns(
            document,
            cellsDeleted,
            2,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(columnsDeleted.ColumnCount, Is.EqualTo(4));
            Assert.That(columnsDeleted.Rows, Has.Length.EqualTo(1));
            Assert.That(columnsDeleted.Rows[0].RepeatCount, Is.EqualTo(20));
            Assert.That(columnsDeleted.Rows[0].Cells, Has.Length.EqualTo(1));
            Assert.That(columnsDeleted.Rows[0].Cells[0].RepeatCount,
                Is.EqualTo(4));
        });
    }

    [Test]
    public void CopiesRowsAndColumnsWithRelativeReferenceRebasing()
    {
        OdfDocument rowDocument = ReadSpreadsheet(
            "<table:table table:name='Rows'>"
            + "<table:table-row><table:table-cell "
            + "table:formula='of:=[.A1]+[.$A$1]+[.A$1]+[.$A1]'/></table:table-row>"
            + "<table:table-row><table:table-cell/></table:table-row>"
            + "</table:table>");
        OdfTable rows = TableStructureEditor.CopyRows(
            rowDocument,
            rowDocument.Tables[0],
            0,
            1,
            2);

        OdfDocument columnDocument = ReadSpreadsheet(
            "<table:table table:name='Columns'>"
            + "<table:table-column table:number-columns-repeated='3'/>"
            + "<table:table-row><table:table-cell "
            + "table:formula='of:=[.A1]+[.$A$1]+[.A$1]+[.$A1]'/><table:table-cell "
            + "table:number-columns-repeated='2'/></table:table-row>"
            + "</table:table>");
        OdfTable columns = TableStructureEditor.CopyColumns(
            columnDocument,
            columnDocument.Tables[0],
            0,
            1,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(
                TableDocumentModel.GetCell(rows, 2, 0)!.Formula,
                Is.EqualTo("of:=[.A3]+[.$A$1]+[.A$1]+[.$A3]"));
            Assert.That(
                TableDocumentModel.GetCell(columns, 0, 3)!.Formula,
                Is.EqualTo("of:=[.D1]+[.$A$1]+[.D$1]+[.$A1]"));
        });
    }

    [Test]
    public void MovesRowsAndColumnsAndMakesReferencesFollowMovedCells()
    {
        OdfDocument rowDocument = ReadSpreadsheet(
            "<table:table table:name='Rows'>"
            + StringRow("first")
            + "<table:table-row><table:table-cell table:formula='of:=[.A1]'/></table:table-row>"
            + StringRow("third")
            + "</table:table>");
        OdfTable rows = TableStructureEditor.MoveRows(
            rowDocument,
            rowDocument.Tables[0],
            0,
            1,
            3);

        OdfDocument columnDocument = ReadSpreadsheet(
            "<table:table table:name='Columns'>"
            + "<table:table-column table:number-columns-repeated='3'/>"
            + "<table:table-row>"
            + StringCell("first")
            + "<table:table-cell table:formula='of:=[.A1]'/><table:table-cell/>"
            + "</table:table-row></table:table>");
        OdfTable columns = TableStructureEditor.MoveColumns(
            columnDocument,
            columnDocument.Tables[0],
            0,
            1,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(CellText(rows, 2, 0), Is.EqualTo("first"));
            Assert.That(
                TableDocumentModel.GetCell(rows, 0, 0)!.Formula,
                Is.EqualTo("of:=[.A3]"));
            Assert.That(CellText(columns, 0, 2), Is.EqualTo("first"));
            Assert.That(
                TableDocumentModel.GetCell(columns, 0, 0)!.Formula,
                Is.EqualTo("of:=[.C1]"));
        });
    }

    [Test]
    public void CopiesAndMovesRectangularRangesWithReferenceUpdates()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'>"
            + "<table:table-row>" + StringCell("one")
            + "<table:table-cell table:formula='of:=[.A1]'/></table:table-row>"
            + "<table:table-row><table:table-cell table:number-columns-repeated='2'/></table:table-row>"
            + "<table:table-row><table:table-cell table:number-columns-repeated='2'/></table:table-row>"
            + "</table:table>");

        OdfTable copied = TableStructureEditor.CopyRange(
            document,
            document.Tables[0],
            0,
            0,
            1,
            2,
            1,
            0);
        OdfTable moved = TableStructureEditor.MoveRange(
            document,
            copied,
            1,
            0,
            1,
            2,
            2,
            0);
        OdfTable reread = WriteAndRead(document).Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(CellText(moved, 1, 0), Is.Empty);
            Assert.That(CellText(moved, 2, 0), Is.EqualTo("one"));
            Assert.That(
                TableDocumentModel.GetCell(moved, 2, 1)!.Formula,
                Is.EqualTo("of:=[.A3]"));
            Assert.That(CellText(reread, 2, 0), Is.EqualTo("one"));
            Assert.That(
                TableDocumentModel.GetCell(reread, 2, 1)!.Formula,
                Is.EqualTo("of:=[.A3]"));
        });
    }

    private static string StringRow(string value)
    {
        return "<table:table-row>" + StringCell(value)
            + "</table:table-row>";
    }

    private static string StringCell(string value)
    {
        return "<table:table-cell office:value-type='string' "
            + "office:string-value='" + value + "'/>";
    }

    private static string CellText(
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        OdfCell? cell = TableDocumentModel.GetCell(
            table,
            rowIndex,
            columnIndex);
        return cell == null ? "" : cell.Value.StringValue;
    }

    private static OdfDocument ReadSpreadsheet(string table)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:body><office:spreadsheet>"
            + table
            + "</office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "structure.fods",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "structure.fods",
            new OdfLoadOptions());
    }
}
