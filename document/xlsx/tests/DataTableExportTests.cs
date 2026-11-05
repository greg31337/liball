using NUnit.Framework;
using System.Data;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DataTableExportTests
{
    [Test]
    public void ExportDataTable_WithHeadersReturnsTypedValuesAndUniqueColumnNames()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Name");
        worksheet.SetTextValue("B1", "Amount");
        worksheet.SetTextValue("C1", "Active");
        worksheet.SetTextValue("D1", "When");
        worksheet.SetTextValue("E1", "Name");
        worksheet.SetTextValue("A2", "Ada");
        worksheet.SetNumberValue("B2", 12.5d);
        worksheet.SetBooleanValue("C2", true);
        worksheet.SetDateTimeValue("D2", new System.DateTime(2024, 1, 2));
        worksheet.SetTextValue("E2", "duplicate");

        XlsxDataTableExportResult result = worksheet.ExportDataTableWithHeaders("A1:F2", true);
        DataTable? table = result.DataTable;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasDataTable, Is.True);
        Assert.That(result.Reference, Is.EqualTo("A1:F2"));
        Assert.That(result.HeaderRowCount, Is.EqualTo(1));
        Assert.That(result.DataRowCount, Is.EqualTo(1));
        Assert.That(result.ColumnCount, Is.EqualTo(6));
        Assert.That(result.ExportedCellCount, Is.EqualTo(6));
        Assert.That(table!.Columns[0].ColumnName, Is.EqualTo("Name"));
        Assert.That(table.Columns[1].ColumnName, Is.EqualTo("Amount"));
        Assert.That(table.Columns[2].ColumnName, Is.EqualTo("Active"));
        Assert.That(table.Columns[3].ColumnName, Is.EqualTo("When"));
        Assert.That(table.Columns[4].ColumnName, Is.EqualTo("Name_2"));
        Assert.That(table.Columns[5].ColumnName, Is.EqualTo("Column6"));
        Assert.That(table.Rows[0][0], Is.EqualTo("Ada"));
        Assert.That(table.Rows[0][1], Is.EqualTo(12.5d));
        Assert.That(table.Rows[0][2], Is.EqualTo(true));
        Assert.That(table.Rows[0][3], Is.EqualTo(new System.DateTime(2024, 1, 2)));
        Assert.That(table.Rows[0][4], Is.EqualTo("duplicate"));
        Assert.That(table.Rows[0][5], Is.EqualTo(System.DBNull.Value));
    }

    [Test]
    public void ExportDataTableWithoutHeaders_GeneratesColumnNamesAndIncludesFirstRow()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("B2", "Pencil");
        worksheet.SetNumberValue("C2", 3);
        worksheet.SetTextValue("B3", "Paper");
        worksheet.SetFormulaCachedValue("C3", "SUM(C2:C2)", "3", "");

        XlsxDataTableExportResult result = worksheet.ExportDataTableWithHeaders("B2:C3", false);
        DataTable? table = result.DataTable;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HeaderRowCount, Is.EqualTo(0));
        Assert.That(result.DataRowCount, Is.EqualTo(2));
        Assert.That(table!.Columns[0].ColumnName, Is.EqualTo("Column1"));
        Assert.That(table.Columns[1].ColumnName, Is.EqualTo("Column2"));
        Assert.That(table.Rows[0][0], Is.EqualTo("Pencil"));
        Assert.That(table.Rows[0][1], Is.EqualTo(3d));
        Assert.That(table.Rows[1][0], Is.EqualTo("Paper"));
        Assert.That(table.Rows[1][1], Is.EqualTo(3d));
    }

    [Test]
    public void ExportTableToDataTable_UsesTableColumnsAndSkipsHeaderAndTotalsRows()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Product");
        worksheet.SetTextValue("B1", "Qty");
        worksheet.SetTextValue("A2", "Pencil");
        worksheet.SetNumberValue("B2", 3);
        worksheet.SetTextValue("A3", "Paper");
        worksheet.SetNumberValue("B3", 5);
        worksheet.AddTable("A1:B3", "Inventory");
        worksheet.SetTableTotalsRow("Inventory", true);
        worksheet.SetTableColumnTotalsRowLabel("Inventory", "Product", "Total");
        worksheet.SetTableColumnTotalsRowFormula("Inventory", "Qty", "SUBTOTAL(109,[Qty])");

        XlsxDataTableExportResult result = worksheet.ExportTableToDataTable("Inventory");
        DataTable? table = result.DataTable;

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Reference, Is.EqualTo("A1:B4"));
        Assert.That(result.HeaderRowCount, Is.EqualTo(1));
        Assert.That(result.DataRowCount, Is.EqualTo(2));
        Assert.That(table!.Columns[0].ColumnName, Is.EqualTo("Product"));
        Assert.That(table.Columns[1].ColumnName, Is.EqualTo("Qty"));
        Assert.That(table.Rows[0][0], Is.EqualTo("Pencil"));
        Assert.That(table.Rows[0][1], Is.EqualTo(3d));
        Assert.That(table.Rows[1][0], Is.EqualTo("Paper"));
        Assert.That(table.Rows[1][1], Is.EqualTo(5d));
    }

    [Test]
    public void ExportDataTable_ReportsInvalidInputs()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetPart nullRows = new XlsxWorksheetPart();
        nullRows.Rows = null!;

        XlsxDataTableExportResult invalidRange = worksheet.ExportDataTable("XFE1:XFE2");
        XlsxDataTableExportResult missingTable = worksheet.ExportTableToDataTable("Missing");
        XlsxDataTableExportResult rowsNull = nullRows.ExportDataTable("A1:B2");

        Assert.That(invalidRange.IsValid, Is.False);
        Assert.That(missingTable.IsValid, Is.False);
        Assert.That(rowsNull.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRange, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(missingTable, "worksheetTable.notFound"), Is.True);
        Assert.That(HasDiagnostic(rowsNull, "dataTableExport.rowsNull"), Is.True);
    }

    static bool HasDiagnostic(XlsxDataTableExportResult result, string code)
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
