using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class AutoFilterReaderTests
{
    [Test]
    public void ReadString_ParsesValueBlankAndDateGroupFilters()
    {
        string xml = "<autoFilter xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" ref=\"A1:D20\" x:root=\"r\">" +
            "<filterColumn colId=\"0\" hiddenButton=\"1\" showButton=\"0\" x:col=\"c\">" +
            "<filters blank=\"1\" calendarType=\"gregorian\" x:filters=\"f\">" +
            "<filter val=\"North\" />" +
            "<filter val=\"South\" />" +
            "<dateGroupItem year=\"2024\" month=\"1\" day=\"2\" dateTimeGrouping=\"day\" x:date=\"d\" />" +
            "<x:future />" +
            "</filters>" +
            "</filterColumn>" +
            "<x:futureRoot />" +
            "</autoFilter>";

        XlsxAutoFilter filter = new XlsxAutoFilterReader().ReadString(xml);

        Assert.That(filter.IsValid, Is.True);
        Assert.That(filter.RawXml, Does.Contain("<autoFilter"));
        Assert.That(filter.Reference, Is.EqualTo("A1:D20"));
        Assert.That(filter.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(filter.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(filter.FilterColumns.Count, Is.EqualTo(1));

        XlsxAutoFilterColumn column = filter.FilterColumns[0];
        Assert.That(column.HasColumnId, Is.True);
        Assert.That(column.ColumnId, Is.EqualTo(0));
        Assert.That(column.HasHiddenButton, Is.True);
        Assert.That(column.HiddenButton, Is.True);
        Assert.That(column.HasShowButton, Is.True);
        Assert.That(column.ShowButton, Is.False);
        Assert.That(column.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(column.HasValues, Is.True);

        XlsxAutoFilterValues values = column.Values;
        Assert.That(values.HasBlank, Is.True);
        Assert.That(values.Blank, Is.True);
        Assert.That(values.CalendarType, Is.EqualTo("gregorian"));
        Assert.That(values.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(values.Values.Count, Is.EqualTo(2));
        Assert.That(values.Values[0], Is.EqualTo("North"));
        Assert.That(values.Values[1], Is.EqualTo("South"));
        Assert.That(values.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(values.DateGroupItems.Count, Is.EqualTo(1));

        XlsxDateGroupItem dateGroup = values.DateGroupItems[0];
        Assert.That(dateGroup.HasYear, Is.True);
        Assert.That(dateGroup.Year, Is.EqualTo(2024));
        Assert.That(dateGroup.HasMonth, Is.True);
        Assert.That(dateGroup.Month, Is.EqualTo(1));
        Assert.That(dateGroup.HasDay, Is.True);
        Assert.That(dateGroup.Day, Is.EqualTo(2));
        Assert.That(dateGroup.DateTimeGrouping, Is.EqualTo("day"));
        Assert.That(dateGroup.UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ParsesCustomDynamicTopColorAndIconFilters()
    {
        string xml = "<autoFilter xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ref=\"A1:E20\">" +
            "<filterColumn colId=\"0\"><customFilters and=\"1\"><customFilter operator=\"greaterThanOrEqual\" val=\"10\" /><customFilter operator=\"lessThan\" val=\"20\" /></customFilters></filterColumn>" +
            "<filterColumn colId=\"1\"><dynamicFilter type=\"lastMonth\" val=\"1\" maxVal=\"2\" valIso=\"2024-01-01T00:00:00\" maxValIso=\"2024-01-31T00:00:00\" /></filterColumn>" +
            "<filterColumn colId=\"2\"><top10 top=\"1\" percent=\"0\" val=\"10\" filterVal=\"50\" /></filterColumn>" +
            "<filterColumn colId=\"3\"><colorFilter dxfId=\"2\" cellColor=\"1\" /></filterColumn>" +
            "<filterColumn colId=\"4\"><iconFilter iconSet=\"3TrafficLights1\" iconId=\"2\" /></filterColumn>" +
            "</autoFilter>";

        XlsxAutoFilter filter = new XlsxAutoFilterReader().ReadString(xml);

        Assert.That(filter.IsValid, Is.True);
        Assert.That(filter.FilterColumns.Count, Is.EqualTo(5));

        XlsxCustomFilters customFilters = filter.FilterColumns[0].CustomFilters;
        Assert.That(filter.FilterColumns[0].HasCustomFilters, Is.True);
        Assert.That(customFilters.HasAnd, Is.True);
        Assert.That(customFilters.And, Is.True);
        Assert.That(customFilters.Filters.Count, Is.EqualTo(2));
        Assert.That(customFilters.Filters[0].Operator, Is.EqualTo("greaterThanOrEqual"));
        Assert.That(customFilters.Filters[0].Value, Is.EqualTo("10"));
        Assert.That(customFilters.Filters[1].Operator, Is.EqualTo("lessThan"));
        Assert.That(customFilters.Filters[1].Value, Is.EqualTo("20"));

        XlsxDynamicFilter dynamicFilter = filter.FilterColumns[1].DynamicFilter;
        Assert.That(filter.FilterColumns[1].HasDynamicFilter, Is.True);
        Assert.That(dynamicFilter.Type, Is.EqualTo("lastMonth"));
        Assert.That(dynamicFilter.HasValue, Is.True);
        Assert.That(dynamicFilter.Value, Is.EqualTo(1));
        Assert.That(dynamicFilter.HasMaxValue, Is.True);
        Assert.That(dynamicFilter.MaxValue, Is.EqualTo(2));
        Assert.That(dynamicFilter.ValueIso, Is.EqualTo("2024-01-01T00:00:00"));
        Assert.That(dynamicFilter.MaxValueIso, Is.EqualTo("2024-01-31T00:00:00"));

        XlsxTop10Filter top10 = filter.FilterColumns[2].Top10;
        Assert.That(filter.FilterColumns[2].HasTop10, Is.True);
        Assert.That(top10.HasTop, Is.True);
        Assert.That(top10.Top, Is.True);
        Assert.That(top10.HasPercent, Is.True);
        Assert.That(top10.Percent, Is.False);
        Assert.That(top10.HasValue, Is.True);
        Assert.That(top10.Value, Is.EqualTo(10));
        Assert.That(top10.HasFilterValue, Is.True);
        Assert.That(top10.FilterValue, Is.EqualTo(50));

        XlsxColorFilter colorFilter = filter.FilterColumns[3].ColorFilter;
        Assert.That(filter.FilterColumns[3].HasColorFilter, Is.True);
        Assert.That(colorFilter.HasDxfId, Is.True);
        Assert.That(colorFilter.DxfId, Is.EqualTo(2));
        Assert.That(colorFilter.HasCellColor, Is.True);
        Assert.That(colorFilter.CellColor, Is.True);

        XlsxIconFilter iconFilter = filter.FilterColumns[4].IconFilter;
        Assert.That(filter.FilterColumns[4].HasIconFilter, Is.True);
        Assert.That(iconFilter.IconSet, Is.EqualTo("3TrafficLights1"));
        Assert.That(iconFilter.HasIconId, Is.True);
        Assert.That(iconFilter.IconId, Is.EqualTo(2));
    }

    [Test]
    public void ReadString_ParsesSortStateRootAndNestedSortConditions()
    {
        string xml = "<sortState xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ref=\"A2:D20\" columnSort=\"1\" caseSensitive=\"0\" sortMethod=\"stroke\">" +
            "<sortCondition descending=\"1\" sortBy=\"cellColor\" ref=\"B2:B20\" dxfId=\"3\" />" +
            "<sortCondition sortBy=\"icon\" ref=\"C2:C20\" iconSet=\"3Arrows\" iconId=\"1\" customList=\"High,Low\" />" +
            "</sortState>";

        XlsxAutoFilter filter = new XlsxAutoFilterReader().ReadString(xml);

        Assert.That(filter.IsValid, Is.True);
        Assert.That(filter.HasSortState, Is.True);
        Assert.That(filter.SortState.Reference, Is.EqualTo("A2:D20"));
        Assert.That(filter.SortState.HasColumnSort, Is.True);
        Assert.That(filter.SortState.ColumnSort, Is.True);
        Assert.That(filter.SortState.HasCaseSensitive, Is.True);
        Assert.That(filter.SortState.CaseSensitive, Is.False);
        Assert.That(filter.SortState.SortMethod, Is.EqualTo("stroke"));
        Assert.That(filter.SortState.Conditions.Count, Is.EqualTo(2));

        XlsxSortCondition first = filter.SortState.Conditions[0];
        Assert.That(first.HasDescending, Is.True);
        Assert.That(first.Descending, Is.True);
        Assert.That(first.SortBy, Is.EqualTo("cellColor"));
        Assert.That(first.Reference, Is.EqualTo("B2:B20"));
        Assert.That(first.HasDxfId, Is.True);
        Assert.That(first.DxfId, Is.EqualTo(3));

        XlsxSortCondition second = filter.SortState.Conditions[1];
        Assert.That(second.SortBy, Is.EqualTo("icon"));
        Assert.That(second.Reference, Is.EqualTo("C2:C20"));
        Assert.That(second.IconSet, Is.EqualTo("3Arrows"));
        Assert.That(second.HasIconId, Is.True);
        Assert.That(second.IconId, Is.EqualTo(1));
        Assert.That(second.CustomList, Is.EqualTo("High,Low"));
    }

    [Test]
    public void ReadString_ReportsMissingInvalidRootAndInvalidValues()
    {
        XlsxAutoFilter missingRoot = new XlsxAutoFilterReader().ReadString("");
        XlsxAutoFilter invalidRoot = new XlsxAutoFilterReader().ReadString("<bad />");
        XlsxAutoFilter invalidValues = new XlsxAutoFilterReader().ReadString("<autoFilter xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ref=\"XFE1:XFE2\"><filterColumn colId=\"bad\" hiddenButton=\"maybe\"><filters blank=\"maybe\"><dateGroupItem year=\"bad\" /></filters></filterColumn><sortState ref=\"A1:XFE1\" columnSort=\"maybe\"><sortCondition ref=\"XFE1:XFE2\" descending=\"maybe\" dxfId=\"bad\" /></sortState></autoFilter>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "autoFilter.missingRoot"), Is.True);
        Assert.That(HasDiagnostic(invalidRoot, "autoFilter.invalidRoot"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "a1.cellReference.columnRange"), Is.True);
        Assert.That(invalidValues.FilterColumns.Count, Is.EqualTo(1));
        Assert.That(invalidValues.HasSortState, Is.True);
    }

    static bool HasDiagnostic(XlsxAutoFilter filter, string code)
    {
        foreach (XlsxDiagnostic diagnostic in filter.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
