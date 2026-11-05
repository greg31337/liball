using NUnit.Framework;
using System.Data;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class TableApiCompletionTests
{
    [Test]
    public void TableReaderWriter_RoundTripsTypedXmlMappingTotalsAndStyleFields()
    {
        string xml = "<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" id=\"2\" name=\"Sales\" displayName=\"Sales\" ref=\"A1:A3\" headerRowDxfId=\"3\" dataDxfId=\"4\" totalsRowDxfId=\"5\" headerRowBorderDxfId=\"6\" tableBorderDxfId=\"7\" totalsRowBorderDxfId=\"8\" headerRowCellStyle=\"Header\" dataCellStyle=\"Data\" totalsRowCellStyle=\"Total\"><tableColumns count=\"1\"><tableColumn id=\"1\" name=\"Amount\" totalsRowFunction=\"sum\"><xmlColumnPr mapId=\"9\" xpath=\"/orders/order/amount\" xmlDataType=\"decimal\" denormalized=\"1\" /></tableColumn></tableColumns></table>";

        XlsxTablePart part = new XlsxTablePartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HeaderRowDxfId, Is.EqualTo(3));
        Assert.That(part.DataDxfId, Is.EqualTo(4));
        Assert.That(part.TotalsRowDxfId, Is.EqualTo(5));
        Assert.That(part.HeaderRowBorderDxfId, Is.EqualTo(6));
        Assert.That(part.TableBorderDxfId, Is.EqualTo(7));
        Assert.That(part.TotalsRowBorderDxfId, Is.EqualTo(8));
        Assert.That(part.HeaderRowCellStyle, Is.EqualTo("Header"));
        Assert.That(part.DataCellStyle, Is.EqualTo("Data"));
        Assert.That(part.TotalsRowCellStyle, Is.EqualTo("Total"));
        Assert.That(part.Columns[0].HasTotalFunction, Is.True);
        Assert.That(part.Columns[0].TotalFunction, Is.EqualTo(XlsxTableTotalFunction.Sum));
        Assert.That(part.Columns[0].HasXmlColumnProperties, Is.True);
        Assert.That(part.Columns[0].XmlColumnProperties.MapId, Is.EqualTo(9));
        Assert.That(part.Columns[0].XmlColumnProperties.XPath, Is.EqualTo("/orders/order/amount"));
        Assert.That(part.Columns[0].XmlColumnProperties.XmlDataType, Is.EqualTo("decimal"));
        Assert.That(part.Columns[0].XmlColumnProperties.Denormalized, Is.True);

        part.RawXml = "";
        XlsxTablePartWriteResult write = new XlsxTablePartWriter().WriteString(part);
        XlsxTablePart roundTrip = new XlsxTablePartReader().ReadString(write.Xml);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("tableBorderDxfId=\"7\""));
        Assert.That(write.Xml, Does.Contain("totalsRowFunction=\"sum\""));
        Assert.That(write.Xml, Does.Contain("<xmlColumnPr mapId=\"9\" xpath=\"/orders/order/amount\" denormalized=\"1\" xmlDataType=\"decimal\""));
        Assert.That(roundTrip.Columns[0].XmlColumnProperties.MapId, Is.EqualTo(9));
        Assert.That(roundTrip.Columns[0].TotalFunction, Is.EqualTo(XlsxTableTotalFunction.Sum));
    }

    [Test]
    public void ConvertRange_ProjectRowsAndCursor_PreserveTypedValuesAndBounds()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("B2", "Product");
        worksheet.SetTextValue("C2", "Qty");
        worksheet.SetTextValue("B3", "Pencil");
        worksheet.SetNumberValue("C3", 3);
        worksheet.SetTextValue("B4", "Paper");
        worksheet.SetNumberValue("C4", 5);
        XlsxTableService service = new XlsxTableService();

        XlsxWorksheetTableResult converted = service.ConvertRange(worksheet, "B2:C4", "Inventory", true);
        XlsxTableRowProjectionResult dataRows = service.ProjectRows(worksheet, "Inventory", false, false);
        XlsxTableRowProjectionResult allRows = service.ProjectRows(worksheet, "Inventory", true, true);
        XlsxTableRowCursor cursor = service.OpenRowCursor(worksheet, "Inventory", false, false);

        Assert.That(converted.IsValid, Is.True);
        Assert.That(converted.Table!.Columns[0].Name, Is.EqualTo("Product"));
        Assert.That(dataRows.IsValid, Is.True);
        Assert.That(dataRows.ColumnCount, Is.EqualTo(2));
        Assert.That(dataRows.Rows.Count, Is.EqualTo(2));
        Assert.That(dataRows.Rows[0][0].Text, Is.EqualTo("Pencil"));
        Assert.That(dataRows.Rows[0][1].Kind, Is.EqualTo(XlsxCellValueKind.Number));
        Assert.That(dataRows.Rows[0][1].Number, Is.EqualTo(3));
        Assert.That(allRows.Rows.Count, Is.EqualTo(3));

        Assert.That(service.ReadRow(cursor), Is.True);
        Assert.That(cursor.Current![0].Text, Is.EqualTo("Pencil"));
        Assert.That(cursor.RowIndex, Is.EqualTo(0));
        Assert.That(service.ReadRow(cursor), Is.True);
        Assert.That(cursor.Current![0].Text, Is.EqualTo("Paper"));
        Assert.That(service.ReadRow(cursor), Is.False);
        Assert.That(cursor.EndOfRows, Is.True);
        service.ResetRowCursor(cursor);
        Assert.That(service.ReadRow(cursor), Is.True);
        Assert.That(cursor.Current![1].Number, Is.EqualTo(3));
    }

    [Test]
    public void TypedTotalAndXmlMapping_UpdateTableAndWorksheetFormula()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");
        worksheet.SetTextValue("A2", "West");
        worksheet.SetNumberValue("B2", 10);
        worksheet.SetTextValue("A3", "East");
        worksheet.SetNumberValue("B3", 20);
        worksheet.AddTable("A1:B3", "Sales");
        worksheet.SetTableTotalsRow("Sales", true);
        XlsxTableXmlColumnProperties mapping = new XlsxTableXmlColumnProperties();
        mapping.MapId = 4;
        mapping.HasMapId = true;
        mapping.XPath = "/sales/amount";
        mapping.XmlDataType = "double";
        XlsxTableService service = new XlsxTableService();

        XlsxWorksheetTableResult total = service.SetTotalFunction(worksheet, "Sales", "Amount", XlsxTableTotalFunction.Sum);
        XlsxWorksheetTableResult mapped = service.SetXmlColumnMapping(worksheet, "Sales", "Amount", mapping);
        XlsxTablePartWriteResult write = new XlsxTablePartWriter().WriteString(total.Table);

        Assert.That(total.IsValid, Is.True);
        Assert.That(total.Column!.HasTotalFunction, Is.True);
        Assert.That(total.Column.TotalFunction, Is.EqualTo(XlsxTableTotalFunction.Sum));
        Assert.That(total.Column.TotalsRowFunction, Is.EqualTo("sum"));
        Assert.That(worksheet.GetCell("B4")!.Formula, Is.EqualTo("SUBTOTAL(109,[Amount])"));
        Assert.That(mapped.IsValid, Is.True);
        Assert.That(mapped.Column!.HasXmlColumnProperties, Is.True);
        Assert.That(mapped.Column.XmlColumnProperties.XPath, Is.EqualTo("/sales/amount"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("totalsRowFunction=\"sum\""));
        Assert.That(write.Xml, Does.Contain("xpath=\"/sales/amount\""));
    }

    [Test]
    public void DataTableTransfer_UsesOnlySystemDataAndReportsInvalidRequests()
    {
        DataTable source = new DataTable();
        source.Columns.Add("Name", typeof(string));
        source.Columns.Add("Qty", typeof(int));
        source.Rows.Add("Pencil", 3);
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxTableService service = new XlsxTableService();

        XlsxDataTableImportResult imported = service.ImportDataTable(worksheet, "D2", source, "Inventory", true);
        XlsxDataTableExportResult exported = service.ExportDataTable(worksheet, "Inventory");
        XlsxWorksheetTableResult missingWorksheet = service.ConvertRange(null!, "A1:B2", "Missing", true);
        XlsxWorksheetTableResult badFunction = service.SetTotalFunction(worksheet, "Inventory", "Qty", (XlsxTableTotalFunction)99);

        Assert.That(imported.IsValid, Is.True);
        Assert.That(imported.Table!.Reference, Is.EqualTo("D2:E3"));
        Assert.That(exported.IsValid, Is.True);
        Assert.That(exported.DataTable!.Columns[0].ColumnName, Is.EqualTo("Name"));
        Assert.That(exported.DataTable.Rows[0][0], Is.EqualTo("Pencil"));
        Assert.That(exported.DataTable.Rows[0][1], Is.EqualTo(3d));
        Assert.That(missingWorksheet.IsValid, Is.False);
        Assert.That(badFunction.IsValid, Is.False);
    }
}
