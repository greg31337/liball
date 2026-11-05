using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class TableFilterValidationConditionalFormattingIntegrationTests
{
    [Test]
    public void WorksheetWriter_RoundTripsTablesFiltersValidationsAndConditionalFormattingTogether()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        WriteSalesHeaders(worksheet);
        XlsxWorksheetTableResult table = worksheet.AddTable("A1:D6", "Sales");
        XlsxWorksheetFilterResult worksheetFilter = worksheet.AddAutoFilter("F1:H6");
        XlsxWorksheetFilterResult worksheetValues = worksheet.SetAutoFilterValues(0, CreateValues("Open", "Closed"), true);
        XlsxWorksheetFilterResult worksheetTop10 = worksheet.SetAutoFilterTop10(1, 5, true, false);
        XlsxWorksheetDataValidationResult listValidation = worksheet.AddListDataValidation("A2:A6", CreateValues("North", "South"));
        XlsxWorksheetDataValidationResult decimalValidation = worksheet.AddDecimalDataValidation("B2:B6", "greaterThan", "0", "");
        XlsxWorksheetConditionalFormattingResult expression = worksheet.AddExpressionConditionalFormatting("B2:B6", "B2>100");
        XlsxWorksheetConditionalFormattingResult colorScale = worksheet.AddColorScaleConditionalFormatting("C2:C6", "FFFF0000", "percentile", "50", "FFFFFF00", "FF00B050");
        XlsxWorksheetFilterResult tableValues = worksheet.SetTableFilterValues("Sales", "Region", CreateValues("North", "South"), false);
        XlsxWorksheetFilterResult tableCustom = worksheet.SetTableCustomFilter("Sales", "Amount", "greaterThanOrEqual", "100", "lessThan", "1000", true);
        XlsxWorksheetTableResult tableSort = worksheet.SetTableSortState("Sales", "Amount", true);

        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart worksheetReadBack = new XlsxWorksheetPartReader().ReadString(worksheetWrite.Xml);
        XlsxTablePart tableReadBack = new XlsxTablePartReader().ReadString(new XlsxTablePartWriter().WriteString(table.Table).Xml);
        XlsxAutoFilter tableFilterReadBack = new XlsxAutoFilterReader().ReadString(tableReadBack.AutoFilterRawXml);

        Assert.That(table.IsValid, Is.True);
        Assert.That(worksheetFilter.IsValid, Is.True);
        Assert.That(worksheetValues.IsValid, Is.True);
        Assert.That(worksheetTop10.IsValid, Is.True);
        Assert.That(listValidation.IsValid, Is.True);
        Assert.That(decimalValidation.IsValid, Is.True);
        Assert.That(expression.IsValid, Is.True);
        Assert.That(colorScale.IsValid, Is.True);
        Assert.That(tableValues.IsValid, Is.True);
        Assert.That(tableCustom.IsValid, Is.True);
        Assert.That(tableSort.IsValid, Is.True);
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(worksheetWrite.Xml, Does.Contain("<autoFilter ref=\"F1:H6\">"));
        Assert.That(worksheetWrite.Xml, Does.Contain("<dataValidations count=\"2\">"));
        Assert.That(worksheetWrite.Xml, Does.Contain("<conditionalFormatting sqref=\"B2:B6\">"));
        Assert.That(worksheetWrite.Xml, Does.Contain("<tableParts count=\"1\">"));
        Assert.That(worksheetReadBack.IsValid, Is.True);
        Assert.That(worksheetReadBack.HasAutoFilter, Is.True);
        Assert.That(worksheetReadBack.AutoFilter.FilterColumns.Count, Is.EqualTo(2));
        Assert.That(worksheetReadBack.DataValidations.Items.Count, Is.EqualTo(2));
        Assert.That(worksheetReadBack.ConditionalFormattings.Count, Is.EqualTo(2));
        Assert.That(worksheetReadBack.TableReferences.Count, Is.EqualTo(1));
        Assert.That(tableReadBack.IsValid, Is.True);
        Assert.That(tableReadBack.AutoFilterEnabled, Is.True);
        Assert.That(tableReadBack.SortStateEnabled, Is.True);
        Assert.That(tableFilterReadBack.IsValid, Is.True);
        Assert.That(tableFilterReadBack.Reference, Is.EqualTo("A1:D6"));
        Assert.That(tableFilterReadBack.FilterColumns.Count, Is.EqualTo(2));
        Assert.That(tableFilterReadBack.FilterColumns[0].Values.Values.Count, Is.EqualTo(2));
        Assert.That(tableFilterReadBack.FilterColumns[1].CustomFilters.Filters.Count, Is.EqualTo(2));
        Assert.That(tableReadBack.SortStateRawXml, Does.Contain("ref=\"A2:D6\""));
        Assert.That(tableReadBack.SortStateRawXml, Does.Contain("ref=\"B2:B6\""));
    }

    [Test]
    public void TableRefresh_PreservesFilterColumnsWhenSortAndResizeUpdateReferences()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        WriteSalesHeaders(worksheet);
        worksheet.AddTable("A1:C5", "Sales");
        XlsxWorksheetFilterResult values = worksheet.SetTableFilterValues("Sales", "Region", CreateValues("North", "South"), true);
        XlsxWorksheetFilterResult dynamic = worksheet.SetTableDynamicFilter("Sales", "When", "lastMonth");
        XlsxWorksheetTableResult sort = worksheet.SetTableSortState("Sales", "Amount", false);
        XlsxWorksheetTableResult resize = worksheet.ResizeTable("Sales", "A1:C8");
        XlsxTablePart? table = worksheet.GetTable("Sales");
        XlsxAutoFilter filter = new XlsxAutoFilterReader().ReadString(table!.AutoFilterRawXml);

        Assert.That(values.IsValid, Is.True);
        Assert.That(dynamic.IsValid, Is.True);
        Assert.That(sort.IsValid, Is.True);
        Assert.That(resize.IsValid, Is.True);
        Assert.That(filter.IsValid, Is.True);
        Assert.That(filter.Reference, Is.EqualTo("A1:C8"));
        Assert.That(filter.FilterColumns.Count, Is.EqualTo(2));
        Assert.That(FindFilterColumn(filter, 0)!.Values.Values[0], Is.EqualTo("North"));
        Assert.That(FindFilterColumn(filter, 0)!.Values.Blank, Is.True);
        Assert.That(FindFilterColumn(filter, 2)!.DynamicFilter.Type, Is.EqualTo("lastMonth"));
        Assert.That(table.SortStateRawXml, Does.Contain("ref=\"A2:C8\""));
        Assert.That(table.SortStateRawXml, Does.Contain("ref=\"B2:B8\""));
    }

    static void WriteSalesHeaders(XlsxWorksheetPart worksheet)
    {
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");
        worksheet.SetTextValue("C1", "When");
        worksheet.SetTextValue("D1", "Status");
        worksheet.SetTextValue("F1", "Status");
        worksheet.SetTextValue("G1", "Score");
        worksheet.SetTextValue("H1", "Flag");
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
}
