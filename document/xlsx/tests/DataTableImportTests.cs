using NUnit.Framework;
using System.Data;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DataTableImportTests
{
    [Test]
    public void ImportDataTable_WritesHeadersAndTypedRows()
    {
        DataTable table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Amount", typeof(double));
        table.Columns.Add("Active", typeof(bool));
        table.Columns.Add("When", typeof(System.DateTime));
        table.Columns.Add("Missing", typeof(string));
        table.Rows.Add("Ada", 12.5d, true, new System.DateTime(2024, 1, 2), "x");
        table.Rows.Add("Grace", 20d, false, new System.DateTime(2024, 1, 3), System.DBNull.Value);
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();

        XlsxDataTableImportResult result = worksheet.ImportDataTableWithHeaders("B2", table, true);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reference, Is.EqualTo("B2:F4"));
        Assert.That(result.HeaderRowCount, Is.EqualTo(1));
        Assert.That(result.DataRowCount, Is.EqualTo(2));
        Assert.That(result.ColumnCount, Is.EqualTo(5));
        Assert.That(result.ImportedCellCount, Is.EqualTo(15));
        Assert.That(worksheet.GetCell("B2")!.Value, Is.EqualTo("Name"));
        Assert.That(worksheet.GetCell("C2")!.Value, Is.EqualTo("Amount"));
        Assert.That(worksheet.GetCell("B3")!.Type, Is.EqualTo("str"));
        Assert.That(worksheet.GetCell("B3")!.Value, Is.EqualTo("Ada"));
        Assert.That(worksheet.GetCell("C3")!.Type, Is.EqualTo("n"));
        Assert.That(worksheet.GetCell("C3")!.Value, Is.EqualTo("12.5"));
        Assert.That(worksheet.GetCell("D3")!.Type, Is.EqualTo("b"));
        Assert.That(worksheet.GetCell("D3")!.Value, Is.EqualTo("1"));
        Assert.That(worksheet.GetCell("E3")!.Type, Is.EqualTo("d"));
        Assert.That(worksheet.GetCell("F4")!.Type, Is.EqualTo(""));
        Assert.That(worksheet.GetCell("F4")!.Value, Is.EqualTo(""));
    }

    [Test]
    public void ImportDataTableWithoutHeaders_WritesDataOnly()
    {
        DataTable table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Qty", typeof(int));
        table.Rows.Add("Pencil", 3);
        table.Rows.Add("Paper", 5);
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();

        XlsxDataTableImportResult result = worksheet.ImportDataTableWithHeaders("A1", table, false);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reference, Is.EqualTo("A1:B2"));
        Assert.That(result.HeaderRowCount, Is.EqualTo(0));
        Assert.That(worksheet.GetCell("A1")!.Value, Is.EqualTo("Pencil"));
        Assert.That(worksheet.GetCell("B1")!.Value, Is.EqualTo("3"));
        Assert.That(worksheet.GetCell("A2")!.Value, Is.EqualTo("Paper"));
        Assert.That(worksheet.GetCell("B2")!.Value, Is.EqualTo("5"));
    }

    [Test]
    public void ImportDataTableAsTable_CreatesTableOverImportedRange()
    {
        DataTable table = new DataTable();
        table.Columns.Add("Product", typeof(string));
        table.Columns.Add("Qty", typeof(int));
        table.Rows.Add("Pencil", 3);
        table.Rows.Add("Paper", 5);
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();

        XlsxDataTableImportResult result = worksheet.ImportDataTableAsTable("A1", table, "Inventory");
        XlsxTablePartWriteResult tableWrite = new XlsxTablePartWriter().WriteString(result.Table);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasTable, Is.True);
        Assert.That(result.Reference, Is.EqualTo("A1:B3"));
        Assert.That(worksheet.Tables.Count, Is.EqualTo(1));
        Assert.That(result.Table!.Name, Is.EqualTo("Inventory"));
        Assert.That(result.Table.Reference, Is.EqualTo("A1:B3"));
        Assert.That(result.Table.Columns.Count, Is.EqualTo(2));
        Assert.That(result.Table.Columns[0].Name, Is.EqualTo("Product"));
        Assert.That(result.Table.Columns[1].Name, Is.EqualTo("Qty"));
        Assert.That(result.Table.AutoFilterRawXml, Does.Contain("ref=\"A1:B3\""));
        Assert.That(worksheet.GetCell("A3")!.Value, Is.EqualTo("Paper"));
        Assert.That(worksheet.GetCell("B3")!.Value, Is.EqualTo("5"));
        Assert.That(tableWrite.IsValid, Is.True);
        Assert.That(tableWrite.Xml, Does.Contain("<autoFilter"));
    }

    [Test]
    public void ImportDataTable_ReportsInvalidInputs()
    {
        DataTable emptyColumns = new DataTable();
        DataTable twoColumns = new DataTable();
        twoColumns.Columns.Add("A", typeof(string));
        twoColumns.Columns.Add("B", typeof(string));
        twoColumns.Rows.Add("x", "y");
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();

        XlsxDataTableImportResult nullTable = worksheet.ImportDataTable("A1", null);
        XlsxDataTableImportResult noColumns = worksheet.ImportDataTable("A1", emptyColumns);
        XlsxDataTableImportResult invalidAddress = worksheet.ImportDataTable("XFE1", twoColumns);
        XlsxDataTableImportResult outside = worksheet.ImportDataTable("XFD1048576", twoColumns);
        XlsxDataTableImportResult emptyNoHeaders = worksheet.ImportDataTableWithHeaders("A1", twoColumns.Clone(), false);
        XlsxDataTableImportResult invalidTableName = worksheet.ImportDataTableAsTable("A1", twoColumns, "A1");

        Assert.That(nullTable.IsValid, Is.False);
        Assert.That(noColumns.IsValid, Is.False);
        Assert.That(invalidAddress.IsValid, Is.False);
        Assert.That(outside.IsValid, Is.False);
        Assert.That(emptyNoHeaders.IsValid, Is.False);
        Assert.That(invalidTableName.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullTable, "dataTableImport.tableNull"), Is.True);
        Assert.That(HasDiagnostic(noColumns, "dataTableImport.columnsMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidAddress, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(outside, "dataTableImport.rangeOutsideWorksheet"), Is.True);
        Assert.That(HasDiagnostic(emptyNoHeaders, "dataTableImport.emptyRange"), Is.True);
        Assert.That(HasDiagnostic(invalidTableName, "worksheetTable.nameCellReference"), Is.True);
    }

    static bool HasDiagnostic(XlsxDataTableImportResult result, string code)
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
