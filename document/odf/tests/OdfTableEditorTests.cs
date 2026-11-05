using System.Text;

namespace odf.Tests;

public class OdfTableEditorTests
{
    [TestCase("text", OdfMediaTypes.Text)]
    [TestCase("spreadsheet", OdfMediaTypes.Spreadsheet)]
    [TestCase("presentation", OdfMediaTypes.Presentation)]
    [TestCase("chart", OdfMediaTypes.Chart)]
    public void FindsTablesInEverySupportedContentKind(
        string bodyKind,
        string mediaType)
    {
        OdfDocument document = ReadFlat(
            bodyKind,
            mediaType,
            TableXml("Shared", BlankRows(2, 3)));
        IOdfTableEditor editor = new OdfTableEditor();

        OdfTable[] tables = editor.GetTables(document);
        tables[0] = new OdfTable();
        OdfTable? table = editor.FindTable(document, "Shared");

        Assert.Multiple(() =>
        {
            Assert.That(table, Is.SameAs(document.Tables[0]));
            Assert.That(document.Tables[0].Name, Is.EqualTo("Shared"));
            Assert.That(editor.GetTableRow(document.Tables[0], 1), Is.Not.Null);
            Assert.That(editor.GetTableColumn(document.Tables[0], 2), Is.Not.Null);
            Assert.That(editor.GetTableCell(document.Tables[0], 1, 2), Is.Not.Null);
            Assert.That(editor.GetTableCell(document.Tables[0], 2, 0), Is.Null);
        });
    }

    [Test]
    public void PerformsStructuralEditsThroughPublicApi()
    {
        OdfDocument document = ReadSpreadsheet(
            TableXml("Structure", BlankRows(3, 4)));
        IOdfTableEditor editor = new OdfTableEditor();
        OdfTable table = document.Tables[0];

        table = editor.InsertTableRows(document, table, 1, 1);
        table = editor.DeleteTableRows(document, table, 1, 1);
        table = editor.CopyTableRows(document, table, 0, 1, 3);
        table = editor.MoveTableRows(document, table, 0, 1, 4);
        table = editor.InsertTableColumns(document, table, 2, 1);
        table = editor.DeleteTableColumns(document, table, 2, 1);
        table = editor.CopyTableColumns(document, table, 0, 1, 4);
        table = editor.MoveTableColumns(document, table, 0, 1, 5);
        table = editor.InsertTableCells(document, table, 0, 1, 1);
        table = editor.DeleteTableCells(document, table, 0, 1, 1);
        table = editor.CopyTableCells(document, table, 0, 0, 1, 2);
        table = editor.MoveTableCells(document, table, 0, 2, 1, 3);
        table = editor.CopyTableRange(document, table, 0, 0, 1, 1, 1, 1);
        table = editor.MoveTableRange(document, table, 1, 1, 1, 1, 2, 2);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(table.RowCount, Is.EqualTo(4));
            Assert.That(table.ColumnCount, Is.EqualTo(5));
            Assert.That(editor.GetTableCell(table, 2, 2), Is.Not.Null);
            Assert.That(reread.Tables[0].RowCount, Is.EqualTo(4));
            Assert.That(reread.Tables[0].ColumnCount, Is.EqualTo(5));
        });
    }

    [Test]
    public void MergesAndManagesMatrixFormulasThroughPublicApi()
    {
        OdfDocument document = ReadSpreadsheet(
            TableXml("Matrix", BlankRows(3, 3)));
        IOdfTableEditor editor = new OdfTableEditor();
        OdfTable table = document.Tables[0];

        table = editor.MergeTableRange(document, table, 0, 0, 2, 2);
        Assert.That(editor.GetTableCell(table, 0, 0)!.IsMergeOrigin, Is.True);
        table = editor.UnmergeTableRange(document, table, 0, 0);
        table = editor.SetTableMatrixFormula(
            document,
            table,
            0,
            0,
            2,
            2,
            "of:=SUM([.A1:.B2])");
        Assert.That(editor.GetTableCell(table, 0, 0)!.IsMatrixFormula, Is.True);
        table = editor.ClearTableMatrixFormula(document, table, 0, 0);
        table = editor.SetTableArrayFormula(
            document,
            table,
            1,
            1,
            2,
            2,
            "of:=[.A1]*2");
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(editor.GetTableCell(table, 0, 0)!.Formula, Is.Empty);
            Assert.That(editor.GetTableCell(table, 1, 1)!.IsMatrixFormula, Is.True);
            Assert.That(editor.GetTableCell(reread.Tables[0], 1, 1)!.Formula,
                Is.EqualTo("of:=[.A1]*2"));
        });
    }

    [Test]
    public void EditsAndSearchesRangesThroughPublicApi()
    {
        string rows = "<table:table-row>"
            + StringCell("Alpha", "Keep")
            + StringCell("Beta", "Remove")
            + "<table:table-cell table:number-columns-repeated='2'/>"
            + "</table:table-row>"
            + BlankRows(2, 4);
        OdfDocument document = ReadSpreadsheet(TableXml("Ranges", rows));
        IOdfTableEditor editor = new OdfTableEditor();
        OdfTable table = document.Tables[0];

        table = editor.ClearTableContents(document, table, 0, 0, 1, 1);
        table = editor.ClearTableFormats(document, table, 0, 1, 1, 1);
        table = editor.FillTableSeries(document, table, 1, 0, 1, 3, 2, 1.5);
        table = editor.CopyTableSeries(document, table, 0, 1, 1, 1, 2, 0, 1, 2);
        OdfSearchMatch[] found = editor.SearchTableRange(
            table,
            0,
            0,
            3,
            4,
            "Beta",
            true,
            true);
        OdfSearchMatch[] replaced = editor.ReplaceTableRange(
            document,
            table,
            0,
            0,
            3,
            4,
            "Beta",
            "Gamma",
            true,
            true);
        table = document.Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(editor.GetTableCell(table, 0, 0)!.Value.StringValue,
                Is.Empty);
            Assert.That(editor.GetTableCell(table, 0, 0)!.StyleName,
                Is.EqualTo("Keep"));
            Assert.That(editor.GetTableCell(table, 0, 1)!.StyleName, Is.Empty);
            Assert.That(editor.GetTableCell(table, 1, 2)!.Value.NumericValue,
                Is.EqualTo(5));
            Assert.That(found, Has.Length.EqualTo(3));
            Assert.That(replaced, Has.Length.EqualTo(3));
            Assert.That(editor.GetTableCell(table, 2, 1)!.Value.StringValue,
                Is.EqualTo("Gamma"));
        });
    }

    [Test]
    public void TransposesRangesThroughPublicApi()
    {
        string rows = "<table:table-row>"
            + StringCell("A", "") + StringCell("B", "")
            + "<table:table-cell table:number-columns-repeated='2'/></table:table-row>"
            + "<table:table-row>"
            + StringCell("C", "") + StringCell("D", "")
            + "<table:table-cell table:number-columns-repeated='2'/></table:table-row>"
            + BlankRows(2, 4);
        OdfDocument document = ReadSpreadsheet(TableXml("Transpose", rows));
        IOdfTableEditor editor = new OdfTableEditor();

        OdfTable table = editor.TransposeTableRange(
            document,
            document.Tables[0],
            0,
            0,
            2,
            2,
            2,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(editor.GetTableCell(table, 2, 0)!.Value.StringValue,
                Is.EqualTo("A"));
            Assert.That(editor.GetTableCell(table, 2, 1)!.Value.StringValue,
                Is.EqualTo("C"));
            Assert.That(editor.GetTableCell(table, 3, 0)!.Value.StringValue,
                Is.EqualTo("B"));
            Assert.That(editor.GetTableCell(table, 3, 1)!.Value.StringValue,
                Is.EqualTo("D"));
        });
    }

    [Test]
    public void ReadsSparseTypedValuesAndNestedTablesThroughPublicApi()
    {
        string cells = "<table:table-cell/>"
            + "<table:table-cell office:value-type='string' "
            + "office:string-value='Hello'/>"
            + "<table:table-cell office:value-type='float' office:value='12.5'/>"
            + "<table:table-cell office:value-type='percentage' office:value='0.25'/>"
            + "<table:table-cell office:value-type='currency' office:value='9.75' "
            + "office:currency='USD'/>"
            + "<table:table-cell office:value-type='date' "
            + "office:date-value='2026-09-02'/>"
            + "<table:table-cell office:value-type='date' "
            + "office:date-value='2026-09-02T13:14:15Z'/>"
            + "<table:table-cell office:value-type='time' "
            + "office:time-value='PT2H3M4.5S'/>"
            + "<table:table-cell office:value-type='boolean' "
            + "office:boolean-value='true'/>"
            + "<table:table-cell table:formula='of:=1/0' "
            + "office:value-type='string' office:string-value='#DIV/0!'/>";
        string tables = "<table:table table:name='Sparse'>"
            + "<table:table-column table:number-columns-repeated='10'/>"
            + "<table:table-row>" + cells + "</table:table-row>"
            + "<table:table-row table:number-rows-repeated='1000000'>"
            + "<table:table-cell table:number-columns-repeated='10'/>"
            + "</table:table-row>"
            + "<table:table-row><table:table-cell>"
            + "<table:table table:name='Nested'><table:table-row>"
            + "<table:table-cell office:value-type='string' "
            + "office:string-value='Inner'/></table:table-row></table:table>"
            + "</table:table-cell><table:table-cell "
            + "table:number-columns-repeated='9'/></table:table-row>"
            + "</table:table>";
        OdfDocument document = ReadSpreadsheet(tables);
        IOdfTableEditor editor = new OdfTableEditor();
        OdfTable[] tablesSnapshot = editor.GetTables(document);
        OdfTable outer = editor.FindTable(document, "Sparse")!;
        OdfTable nested = editor.FindTable(document, "Nested")!;
        OdfRow repeated = editor.GetTableRow(outer, 500000)!;
        OdfColumn column = editor.GetTableColumn(outer, 9)!;
        OdfCell nestedCell = editor.GetTableCell(outer, 1000001, 0)!;

        Assert.Multiple(() =>
        {
            Assert.That(tablesSnapshot, Is.Not.SameAs(document.Tables));
            Assert.That(tablesSnapshot, Has.Length.EqualTo(2));
            Assert.That(outer.RowCount, Is.EqualTo(1000002));
            Assert.That(outer.ColumnCount, Is.EqualTo(10));
            Assert.That(outer.Rows, Has.Length.EqualTo(3));
            Assert.That(repeated.RepeatCount, Is.EqualTo(1000000));
            Assert.That(column.RepeatCount, Is.EqualTo(10));
            Assert.That(editor.GetTableCell(outer, 500000, 9), Is.Not.Null);
            Assert.That(editor.GetTableCell(outer, 1000002, 0), Is.Null);
            Assert.That(editor.GetTableCell(outer, 0, 0)!.Value.Kind,
                Is.EqualTo(OdfValueKind.Empty));
            Assert.That(editor.GetTableCell(outer, 0, 1)!.Value.Kind,
                Is.EqualTo(OdfValueKind.String));
            Assert.That(editor.GetTableCell(outer, 0, 2)!.Value.NumericValue,
                Is.EqualTo(12.5));
            Assert.That(editor.GetTableCell(outer, 0, 3)!.Value.Kind,
                Is.EqualTo(OdfValueKind.Percentage));
            Assert.That(editor.GetTableCell(outer, 0, 4)!.Value.Currency,
                Is.EqualTo("USD"));
            Assert.That(editor.GetTableCell(outer, 0, 5)!.Value.Kind,
                Is.EqualTo(OdfValueKind.Date));
            Assert.That(editor.GetTableCell(outer, 0, 6)!.Value.Kind,
                Is.EqualTo(OdfValueKind.DateTime));
            Assert.That(editor.GetTableCell(outer, 0, 7)!.Value.Kind,
                Is.EqualTo(OdfValueKind.Duration));
            Assert.That(editor.GetTableCell(outer, 0, 8)!.Value.BooleanValue,
                Is.True);
            Assert.That(editor.GetTableCell(outer, 0, 9)!.Value.Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(nestedCell.NestedTables, Is.EqualTo(new[] { nested }));
            Assert.That(nested.ParentTable, Is.SameAs(outer));
            Assert.That(nested.ParentCell, Is.SameAs(nestedCell));
        });
    }

    [Test]
    public void PreservesSparseRunsAndRebasesReferencesThroughPublicApi()
    {
        OdfDocument rowsDocument = ReadSpreadsheet(
            "<table:table table:name='Rows' table:print-ranges='.A1:.B3'>"
            + "<table:table-row table:number-rows-repeated='3'>"
            + "<table:table-cell table:number-columns-repeated='2'/>"
            + "</table:table-row><table:table-row><table:table-cell "
            + "table:formula='of:=[.A3]+[.$A$3]'/></table:table-row>"
            + "</table:table>");
        IOdfTableEditor editor = new OdfTableEditor();
        OdfTable rows = editor.InsertTableRows(
            rowsDocument,
            rowsDocument.Tables[0],
            1,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(rows.RowCount, Is.EqualTo(6));
            Assert.That(rows.Rows.Any(row => row.RepeatCount == 5), Is.True);
            Assert.That(rows.PrintRangeText, Is.EqualTo(".A1:.B5"));
            Assert.That(editor.GetTableCell(rows, 5, 0)!.Formula,
                Is.EqualTo("of:=[.A5]+[.$A$5]"));
        });

        rows = editor.DeleteTableRows(rowsDocument, rows, 1, 2);

        OdfDocument copyRowsDocument = ReadSpreadsheet(
            "<table:table table:name='CopyRows'><table:table-row>"
            + "<table:table-cell table:formula="
            + "'of:=[.A1]+[.$A$1]+[.A$1]+[.$A1]'/></table:table-row>"
            + "<table:table-row><table:table-cell/></table:table-row>"
            + "</table:table>");
        OdfTable copiedRows = editor.CopyTableRows(
            copyRowsDocument,
            copyRowsDocument.Tables[0],
            0,
            1,
            2);

        OdfDocument copyColumnsDocument = ReadSpreadsheet(
            "<table:table table:name='CopyColumns'>"
            + "<table:table-column table:number-columns-repeated='3'/>"
            + "<table:table-row><table:table-cell table:formula="
            + "'of:=[.A1]+[.$A$1]+[.A$1]+[.$A1]'/>"
            + "<table:table-cell table:number-columns-repeated='2'/>"
            + "</table:table-row></table:table>");
        OdfTable copiedColumns = editor.CopyTableColumns(
            copyColumnsDocument,
            copyColumnsDocument.Tables[0],
            0,
            1,
            3);

        OdfDocument rangeDocument = ReadSpreadsheet(
            "<table:table table:name='Ranges'><table:table-row>"
            + StringCell("one", "")
            + "<table:table-cell table:formula='of:=[.A1]'/></table:table-row>"
            + BlankRows(2, 2)
            + "</table:table>");
        OdfTable copiedRange = editor.CopyTableRange(
            rangeDocument,
            rangeDocument.Tables[0],
            0,
            0,
            1,
            2,
            1,
            0);
        OdfTable movedRange = editor.MoveTableRange(
            rangeDocument,
            copiedRange,
            1,
            0,
            1,
            2,
            2,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(rows.RowCount, Is.EqualTo(4));
            Assert.That(rows.PrintRangeText, Is.EqualTo(".A1:.B3"));
            Assert.That(editor.GetTableCell(copiedRows, 2, 0)!.Formula,
                Is.EqualTo("of:=[.A3]+[.$A$1]+[.A$1]+[.$A3]"));
            Assert.That(editor.GetTableCell(copiedColumns, 0, 3)!.Formula,
                Is.EqualTo("of:=[.D1]+[.$A$1]+[.D$1]+[.$A1]"));
            Assert.That(editor.GetTableCell(movedRange, 1, 0)!.Value.StringValue,
                Is.Empty);
            Assert.That(editor.GetTableCell(movedRange, 2, 0)!.Value.StringValue,
                Is.EqualTo("one"));
            Assert.That(editor.GetTableCell(movedRange, 2, 1)!.Formula,
                Is.EqualTo("of:=[.A3]"));
        });
    }

    [Test]
    public void PreservesMergeMatrixAndFormattingSemanticsThroughPublicApi()
    {
        IOdfTableEditor editor = new OdfTableEditor();
        OdfDocument mergeDocument = ReadSpreadsheet(
            TableXml("Merge", BlankRows(4, 4)));
        OdfTable merged = editor.MergeTableRange(
            mergeDocument,
            mergeDocument.Tables[0],
            1,
            1,
            2,
            2);
        OdfCell mergeOrigin = editor.GetTableCell(merged, 1, 1)!;

        Assert.Multiple(() =>
        {
            Assert.That(mergeOrigin.IsMergeOrigin, Is.True);
            Assert.That(mergeOrigin.RowSpan, Is.EqualTo(2));
            Assert.That(mergeOrigin.ColumnSpan, Is.EqualTo(2));
            Assert.That(editor.GetTableCell(merged, 1, 2)!.IsCovered, Is.True);
            Assert.That(editor.GetTableCell(merged, 2, 1)!.IsCovered, Is.True);
            Assert.That(editor.GetTableCell(merged, 2, 2)!.IsCovered, Is.True);
        });

        OdfTable unmerged = editor.UnmergeTableRange(
            mergeDocument,
            merged,
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(unmerged.Rows, Has.Length.EqualTo(1));
            Assert.That(unmerged.Rows[0].RepeatCount, Is.EqualTo(4));
            Assert.That(unmerged.Rows[0].Cells, Has.Length.EqualTo(1));
            Assert.That(unmerged.Rows[0].Cells[0].RepeatCount, Is.EqualTo(4));
        });

        OdfTable matrix = editor.SetTableMatrixFormula(
            mergeDocument,
            unmerged,
            0,
            0,
            2,
            2,
            "of:=SUM([.A1:.B2])");
        OdfCell matrixOrigin = editor.GetTableCell(matrix, 0, 0)!;

        Assert.Multiple(() =>
        {
            Assert.That(matrixOrigin.IsMatrixFormula, Is.True);
            Assert.That(matrixOrigin.MatrixRowSpan, Is.EqualTo(2));
            Assert.That(matrixOrigin.MatrixColumnSpan, Is.EqualTo(2));
            Assert.That(editor.GetTableCell(matrix, 0, 1)!.IsCovered, Is.False);
            Assert.That(editor.GetTableCell(matrix, 1, 0)!.IsCovered, Is.False);
        });

        matrix = editor.ClearTableMatrixFormula(mergeDocument, matrix, 0, 0);
        matrix = editor.SetTableArrayFormula(
            mergeDocument,
            matrix,
            1,
            1,
            2,
            2,
            "of:=[.A1:.B2]*2");
        OdfTable rereadMatrix = WriteAndRead(mergeDocument).Tables[0];

        OdfDocument formatDocument = ReadSpreadsheet(
            "<table:table table:name='Formats'><table:table-row>"
            + "<table:table-cell table:style-name='KeepStyle' table:protect='true' "
            + "office:value-type='string' office:string-value='remove' "
            + "table:formula='of:=1'/>"
            + "<table:table-cell table:style-name='RemoveStyle' table:protect='true' "
            + "office:value-type='float' office:value='42' "
            + "table:formula='of:=40+2'/></table:table-row></table:table>");
        OdfTable contentCleared = editor.ClearTableContents(
            formatDocument,
            formatDocument.Tables[0],
            0,
            0,
            1,
            1);
        OdfTable formatCleared = editor.ClearTableFormats(
            formatDocument,
            contentCleared,
            0,
            1,
            1,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(editor.GetTableCell(matrix, 0, 0)!.Formula, Is.Empty);
            Assert.That(editor.GetTableCell(rereadMatrix, 1, 1)!.Formula,
                Is.EqualTo("of:=[.A1:.B2]*2"));
            Assert.That(editor.GetTableCell(formatCleared, 0, 0)!.Value.Kind,
                Is.EqualTo(OdfValueKind.Empty));
            Assert.That(editor.GetTableCell(formatCleared, 0, 0)!.StyleName,
                Is.EqualTo("KeepStyle"));
            Assert.That(editor.GetTableCell(formatCleared, 0, 0)!.IsProtected,
                Is.True);
            Assert.That(editor.GetTableCell(formatCleared, 0, 1)!.Value.NumericValue,
                Is.EqualTo(42));
            Assert.That(editor.GetTableCell(formatCleared, 0, 1)!.Formula,
                Is.EqualTo("of:=40+2"));
            Assert.That(editor.GetTableCell(formatCleared, 0, 1)!.StyleName,
                Is.Empty);
            Assert.That(editor.GetTableCell(formatCleared, 0, 1)!.IsProtected,
                Is.False);
        });
    }

    private static string TableXml(string name, string rows)
    {
        return "<table:table table:name='" + name + "'>"
            + "<table:table-column table:number-columns-repeated='4'/>"
            + rows
            + "</table:table>";
    }

    private static string BlankRows(int rowCount, int columnCount)
    {
        return "<table:table-row table:number-rows-repeated='" + rowCount + "'>"
            + "<table:table-cell table:number-columns-repeated='" + columnCount
            + "'/></table:table-row>";
    }

    private static string StringCell(string value, string styleName)
    {
        string style = styleName.Length == 0
            ? ""
            : " table:style-name='" + styleName + "'";
        return "<table:table-cell" + style
            + " office:value-type='string' office:string-value='" + value
            + "'/>";
    }

    private static OdfDocument ReadSpreadsheet(string tables)
    {
        return ReadFlat("spreadsheet", OdfMediaTypes.Spreadsheet, tables);
    }

    private static OdfDocument ReadFlat(
        string bodyKind,
        string mediaType,
        string tables)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:of='" + OdfNamespaces.OpenFormula + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + mediaType + "'>"
            + "<office:body><office:" + bodyKind + ">"
            + tables
            + "</office:" + bodyKind + "></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "table-editor.xml",
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
            "table-editor.fods",
            new OdfLoadOptions());
    }
}
