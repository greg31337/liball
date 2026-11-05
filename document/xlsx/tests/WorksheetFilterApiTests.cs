using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetFilterApiTests
{
    [Test]
    public void WorksheetAutoFilterApis_WriteReadAndClearFilters()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetFilterResult add = worksheet.AddAutoFilter("A1:C10");
        XlsxWorksheetFilterResult values = worksheet.SetAutoFilterValues(0, CreateValues("North", "South"), true);
        XlsxWorksheetFilterResult custom = worksheet.SetAutoFilterCustom(1, "greaterThanOrEqual", "10", "lessThan", "20", true);
        XlsxWorksheetFilterResult dateGroup = worksheet.SetAutoFilterDateGroup(2, 2024, 8, 9, "day");
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);
        XlsxWorksheetFilterResult clear = worksheet.ClearAutoFilter();

        Assert.That(add.IsValid, Is.True);
        Assert.That(values.IsValid, Is.True);
        Assert.That(custom.IsValid, Is.True);
        Assert.That(dateGroup.IsValid, Is.True);
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<autoFilter"));
        Assert.That(write.Xml, Does.Contain("ref=\"A1:C10\""));
        Assert.That(write.Xml, Does.Contain("<filter val=\"North\""));
        Assert.That(write.Xml, Does.Contain("blank=\"1\""));
        Assert.That(write.Xml, Does.Contain("<customFilters and=\"1\""));
        Assert.That(write.Xml, Does.Contain("<dateGroupItem"));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.HasAutoFilter, Is.True);
        Assert.That(readBack.AutoFilter.Reference, Is.EqualTo("A1:C10"));
        Assert.That(readBack.AutoFilter.FilterColumns.Count, Is.EqualTo(3));
        Assert.That(readBack.AutoFilter.FilterColumns[0].Values.Values.Count, Is.EqualTo(2));
        Assert.That(readBack.AutoFilter.FilterColumns[1].CustomFilters.Filters.Count, Is.EqualTo(2));
        Assert.That(readBack.AutoFilter.FilterColumns[2].Values.DateGroupItems.Count, Is.EqualTo(1));
        Assert.That(clear.IsValid, Is.True);
        Assert.That(worksheet.HasAutoFilter, Is.False);
    }

    [Test]
    public void TableFilterApis_WriteAllFilterKindsAndClearColumnFilter()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");
        worksheet.SetTextValue("C1", "When");
        worksheet.SetTextValue("D1", "Score");
        worksheet.SetTextValue("E1", "Shade");
        worksheet.SetTextValue("F1", "Signal");
        worksheet.SetTextValue("G1", "Trend");
        XlsxWorksheetTableResult tableAdd = worksheet.AddTable("A1:G8", "Sales");

        XlsxWorksheetFilterResult values = worksheet.SetTableFilterValues("Sales", "Region", CreateValues("North", "South"), true);
        XlsxWorksheetFilterResult custom = worksheet.SetTableCustomFilter("Sales", "Amount", "greaterThanOrEqual", "10", "lessThan", "20", false);
        XlsxWorksheetFilterResult dateGroup = worksheet.SetTableDateGroupFilter("Sales", "When", 2024, 8, 9, "day");
        XlsxWorksheetFilterResult top10 = worksheet.SetTableTop10Filter("Sales", "Score", 10, true, false);
        XlsxWorksheetFilterResult color = worksheet.SetTableColorFilter("Sales", "Shade", 2, true);
        XlsxWorksheetFilterResult icon = worksheet.SetTableIconFilter("Sales", "Signal", "3TrafficLights1", 1);
        XlsxWorksheetFilterResult dynamicFilter = worksheet.SetTableDynamicFilter("Sales", "Trend", "lastMonth");
        XlsxTablePart? table = worksheet.GetTable("Sales");
        XlsxTablePartWriteResult write = new XlsxTablePartWriter().WriteString(table);
        XlsxTablePart readTable = new XlsxTablePartReader().ReadString(write.Xml);
        XlsxAutoFilter readFilter = new XlsxAutoFilterReader().ReadString(readTable.AutoFilterRawXml);
        XlsxWorksheetFilterResult clearAmount = worksheet.ClearTableColumnFilter("Sales", "Amount");
        XlsxAutoFilter clearedFilter = new XlsxAutoFilterReader().ReadString(table!.AutoFilterRawXml);

        Assert.That(tableAdd.IsValid, Is.True);
        Assert.That(values.IsValid, Is.True);
        Assert.That(custom.IsValid, Is.True);
        Assert.That(dateGroup.IsValid, Is.True);
        Assert.That(top10.IsValid, Is.True);
        Assert.That(color.IsValid, Is.True);
        Assert.That(icon.IsValid, Is.True);
        Assert.That(dynamicFilter.IsValid, Is.True);
        Assert.That(table.AutoFilterEnabled, Is.True);
        Assert.That(table.AutoFilterRawXml, Does.Contain("<filterColumn"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(readTable.IsValid, Is.True);
        Assert.That(readFilter.IsValid, Is.True);
        Assert.That(readFilter.Reference, Is.EqualTo("A1:G8"));
        Assert.That(readFilter.FilterColumns.Count, Is.EqualTo(7));
        Assert.That(readFilter.FilterColumns[0].HasValues, Is.True);
        Assert.That(readFilter.FilterColumns[1].HasCustomFilters, Is.True);
        Assert.That(readFilter.FilterColumns[2].Values.DateGroupItems.Count, Is.EqualTo(1));
        Assert.That(readFilter.FilterColumns[3].HasTop10, Is.True);
        Assert.That(readFilter.FilterColumns[4].HasColorFilter, Is.True);
        Assert.That(readFilter.FilterColumns[5].HasIconFilter, Is.True);
        Assert.That(readFilter.FilterColumns[6].HasDynamicFilter, Is.True);
        Assert.That(clearAmount.IsValid, Is.True);
        Assert.That(FindFilterColumn(clearedFilter, 1), Is.Null);
        Assert.That(clearedFilter.FilterColumns.Count, Is.EqualTo(6));
    }

    [Test]
    public void FilterApis_ReportInvalidStates()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetFilterResult missingWorksheetFilter = worksheet.SetAutoFilterValues(0, CreateValues("A"), false);
        XlsxWorksheetFilterResult invalidRange = worksheet.AddAutoFilter("XFE1:XFE2");
        worksheet.AddAutoFilter("A1:B5");
        XlsxWorksheetFilterResult outOfRange = worksheet.SetAutoFilterValues(2, CreateValues("A"), false);
        worksheet.AddTable("A1:B1", "HeaderOnly");
        worksheet.AddTableWithHeaderRow("D1:E3", "NoHeader", false);
        XlsxWorksheetFilterResult missingTable = worksheet.SetTableFilterValues("Missing", "A", CreateValues("A"), false);
        XlsxWorksheetFilterResult missingColumn = worksheet.SetTableFilterValues("HeaderOnly", "Missing", CreateValues("A"), false);
        XlsxWorksheetFilterResult noHeader = worksheet.SetTableFilterValues("NoHeader", "Column1", CreateValues("A"), false);

        Assert.That(missingWorksheetFilter.IsValid, Is.False);
        Assert.That(invalidRange.IsValid, Is.False);
        Assert.That(outOfRange.IsValid, Is.False);
        Assert.That(missingTable.IsValid, Is.False);
        Assert.That(missingColumn.IsValid, Is.False);
        Assert.That(noHeader.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingWorksheetFilter, "worksheetFilter.autoFilterMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidRange, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(outOfRange, "worksheetFilter.columnOutOfRange"), Is.True);
        Assert.That(HasDiagnostic(missingTable, "worksheetTable.notFound"), Is.True);
        Assert.That(HasDiagnostic(missingColumn, "worksheetTable.columnNotFound"), Is.True);
        Assert.That(HasDiagnostic(noHeader, "worksheetTable.autoFilterHeaderMissing"), Is.True);
    }

    static List<string> CreateValues(string first)
    {
        List<string> values = new List<string>();
        values.Add(first);
        return values;
    }

    static List<string> CreateValues(string first, string second)
    {
        List<string> values = new List<string>();
        values.Add(first);
        values.Add(second);
        return values;
    }

    static XlsxAutoFilterColumn? FindFilterColumn(XlsxAutoFilter filter, uint columnId)
    {
        foreach (XlsxAutoFilterColumn column in filter.FilterColumns)
        {
            if (column.ColumnId == columnId)
            {
                return column;
            }
        }

        return null;
    }

    static bool HasDiagnostic(XlsxWorksheetFilterResult result, string code)
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
