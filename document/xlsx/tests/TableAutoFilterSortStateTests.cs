using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class TableAutoFilterSortStateTests
{
    [Test]
    public void AddTable_EnablesAutoFilterAndWritesItBeforeColumns()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");

        XlsxWorksheetTableResult add = worksheet.AddTable("A1:B4", "Sales");
        XlsxTablePartWriteResult write = new XlsxTablePartWriter().WriteString(add.Table);
        XlsxTablePart roundTrip = new XlsxTablePartReader().ReadString(write.Xml);

        Assert.That(add.IsValid, Is.True);
        Assert.That(add.Table!.AutoFilterEnabled, Is.True);
        Assert.That(add.Table.AutoFilterRawXml, Does.Contain("ref=\"A1:B4\""));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml.IndexOf("<autoFilter", System.StringComparison.Ordinal), Is.LessThan(write.Xml.IndexOf("<tableColumns", System.StringComparison.Ordinal)));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.AutoFilterEnabled, Is.True);
        Assert.That(roundTrip.AutoFilterRawXml, Does.Contain("ref=\"A1:B4\""));
    }

    [Test]
    public void ResizeAndTotalsRow_UpdateAutoFilterReferenceExcludingTotalsRow()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.AddTable("A1:B4", "Sales");

        XlsxWorksheetTableResult totals = worksheet.SetTableTotalsRow("Sales", true);
        XlsxWorksheetTableResult resize = worksheet.ResizeTable("Sales", "A1:C7");
        XlsxTablePart? table = worksheet.GetTable("Sales");

        Assert.That(totals.IsValid, Is.True);
        Assert.That(resize.IsValid, Is.True);
        Assert.That(table!.Reference, Is.EqualTo("A1:C7"));
        Assert.That(table.AutoFilterRawXml, Does.Contain("ref=\"A1:C6\""));
    }

    [Test]
    public void SetAndClearSortState_WritesDataBodySortConditionAndRequiresAutoFilter()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");
        worksheet.AddTable("A1:B5", "Sales");

        XlsxWorksheetTableResult sort = worksheet.SetTableSortState("Sales", "Amount", true);
        XlsxTablePart? table = worksheet.GetTable("Sales");
        XlsxTablePartWriteResult sortedWrite = new XlsxTablePartWriter().WriteString(table);

        Assert.That(sort.IsValid, Is.True);
        Assert.That(table!.AutoFilterEnabled, Is.True);
        Assert.That(table.SortStateEnabled, Is.True);
        Assert.That(table.SortColumnName, Is.EqualTo("Amount"));
        Assert.That(table.SortDescending, Is.True);
        Assert.That(table.SortStateRawXml, Does.Contain("ref=\"A2:B5\""));
        Assert.That(table.SortStateRawXml, Does.Contain("descending=\"1\""));
        Assert.That(table.SortStateRawXml, Does.Contain("ref=\"B2:B5\""));
        Assert.That(sortedWrite.IsValid, Is.True);
        Assert.That(sortedWrite.Xml, Does.Contain("<sortState"));

        XlsxWorksheetTableResult clearSort = worksheet.ClearTableSortState("Sales");
        XlsxWorksheetTableResult clearFilter = worksheet.ClearTableAutoFilter("Sales");

        Assert.That(clearSort.IsValid, Is.True);
        Assert.That(table.SortStateEnabled, Is.False);
        Assert.That(table.SortStateRawXml, Is.EqualTo(""));
        Assert.That(clearFilter.IsValid, Is.True);
        Assert.That(table.AutoFilterEnabled, Is.False);
        Assert.That(table.AutoFilterRawXml, Is.EqualTo(""));
    }

    [Test]
    public void RowAndColumnEdits_UpdateAutoFilterAndSortReferences()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.AddTable("A1:B5", "Sales");
        worksheet.SetTableSortState("Sales", "Column2", false);

        XlsxWorksheetRowEditResult rowInsert = worksheet.InsertRows(2, 1);
        XlsxWorksheetColumnEditResult columnInsert = worksheet.InsertColumns(1, 1);
        XlsxTablePart? table = worksheet.GetTable("Sales");

        Assert.That(rowInsert.IsValid, Is.True);
        Assert.That(columnInsert.IsValid, Is.True);
        Assert.That(table!.Reference, Is.EqualTo("B1:C6"));
        Assert.That(table.AutoFilterRawXml, Does.Contain("ref=\"B1:C6\""));
        Assert.That(table.SortStateRawXml, Does.Contain("ref=\"B2:C6\""));
        Assert.That(table.SortStateRawXml, Does.Contain("ref=\"C2:C6\""));
    }

    [Test]
    public void SortAndFilterApis_ReportInvalidStates()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.AddTable("A1:B1", "HeaderOnly");
        worksheet.AddTableWithHeaderRow("D1:E3", "NoHeader", false);

        XlsxWorksheetTableResult emptySort = worksheet.SetTableSortState("HeaderOnly", "Column1", false);
        XlsxWorksheetTableResult noHeaderSort = worksheet.SetTableSortState("NoHeader", "Column1", false);
        XlsxWorksheetTableResult noHeaderFilter = worksheet.SetTableAutoFilter("NoHeader", true);
        XlsxWorksheetTableResult missingColumn = worksheet.SetTableSortState("HeaderOnly", "Missing", false);

        Assert.That(emptySort.IsValid, Is.False);
        Assert.That(noHeaderSort.IsValid, Is.False);
        Assert.That(noHeaderFilter.IsValid, Is.False);
        Assert.That(missingColumn.IsValid, Is.False);
        Assert.That(HasDiagnostic(emptySort, "worksheetTable.sortRangeEmpty"), Is.True);
        Assert.That(HasDiagnostic(noHeaderSort, "worksheetTable.sortHeaderMissing"), Is.True);
        Assert.That(HasDiagnostic(noHeaderFilter, "worksheetTable.autoFilterHeaderMissing"), Is.True);
        Assert.That(HasDiagnostic(missingColumn, "worksheetTable.columnNotFound"), Is.True);
    }

    static bool HasDiagnostic(XlsxWorksheetTableResult result, string code)
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
