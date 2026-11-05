using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class AutoFilterWriterTests
{
    [Test]
    public void WriteString_WritesValueDateGroupSortStateAndUnknowns()
    {
        XlsxAutoFilter filter = CreateAutoFilter("A1:D20");
        filter.UnknownAttributes.Add(CreateAttribute("x", "root", "urn:x", "r"));
        filter.UnknownXml.Add("<x:futureRoot xmlns:x=\"urn:x\" />");

        XlsxAutoFilterColumn column = CreateColumn(0);
        column.HiddenButton = true;
        column.HasHiddenButton = true;
        column.ShowButton = false;
        column.HasShowButton = true;
        column.UnknownAttributes.Add(CreateAttribute("x", "col", "urn:x", "c"));
        column.HasValues = true;
        column.Values.Blank = true;
        column.Values.HasBlank = true;
        column.Values.CalendarType = "gregorian";
        column.Values.UnknownAttributes.Add(CreateAttribute("x", "filters", "urn:x", "f"));
        column.Values.Values.Add("North");
        column.Values.Values.Add("South");
        column.Values.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");

        XlsxDateGroupItem dateGroup = new XlsxDateGroupItem();
        dateGroup.Year = 2024;
        dateGroup.HasYear = true;
        dateGroup.Month = 1;
        dateGroup.HasMonth = true;
        dateGroup.Day = 2;
        dateGroup.HasDay = true;
        dateGroup.DateTimeGrouping = "day";
        dateGroup.UnknownAttributes.Add(CreateAttribute("x", "date", "urn:x", "d"));
        column.Values.DateGroupItems.Add(dateGroup);
        filter.FilterColumns.Add(column);

        filter.HasSortState = true;
        filter.SortState.Reference = "A2:D20";
        filter.SortState.ColumnSort = true;
        filter.SortState.HasColumnSort = true;
        filter.SortState.CaseSensitive = false;
        filter.SortState.HasCaseSensitive = true;
        filter.SortState.SortMethod = "stroke";
        filter.SortState.Conditions.Add(CreateSortCondition("B2:B20", true));

        XlsxAutoFilterWriteResult result = new XlsxAutoFilterWriter().WriteString(filter);
        XlsxAutoFilter readBack = new XlsxAutoFilterReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<autoFilter"));
        Assert.That(result.Xml, Does.Contain("ref=\"A1:D20\""));
        Assert.That(result.Xml, Does.Contain("x:root=\"r\""));
        Assert.That(result.Xml, Does.Contain("<filterColumn"));
        Assert.That(result.Xml, Does.Contain("colId=\"0\""));
        Assert.That(result.Xml, Does.Contain("hiddenButton=\"1\""));
        Assert.That(result.Xml, Does.Contain("showButton=\"0\""));
        Assert.That(result.Xml, Does.Contain("<filters"));
        Assert.That(result.Xml, Does.Contain("blank=\"1\""));
        Assert.That(result.Xml, Does.Contain("calendarType=\"gregorian\""));
        Assert.That(result.Xml, Does.Contain("<filter val=\"North\""));
        Assert.That(result.Xml, Does.Contain("<dateGroupItem"));
        Assert.That(result.Xml, Does.Contain("dateTimeGrouping=\"day\""));
        Assert.That(result.Xml, Does.Contain("<sortState"));
        Assert.That(result.Xml, Does.Contain("caseSensitive=\"0\""));
        Assert.That(result.Xml, Does.Contain("<sortCondition"));
        Assert.That(result.Xml, Does.Contain("<x:futureRoot"));

        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.Reference, Is.EqualTo("A1:D20"));
        Assert.That(readBack.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(readBack.FilterColumns.Count, Is.EqualTo(1));
        Assert.That(readBack.FilterColumns[0].Values.Values.Count, Is.EqualTo(2));
        Assert.That(readBack.FilterColumns[0].Values.DateGroupItems.Count, Is.EqualTo(1));
        Assert.That(readBack.HasSortState, Is.True);
        Assert.That(readBack.SortState.Conditions.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesCustomDynamicTopColorAndIconFilters()
    {
        XlsxAutoFilter filter = CreateAutoFilter("A1:E20");

        XlsxAutoFilterColumn customColumn = CreateColumn(0);
        customColumn.HasCustomFilters = true;
        customColumn.CustomFilters.And = true;
        customColumn.CustomFilters.HasAnd = true;
        customColumn.CustomFilters.Filters.Add(CreateCustomFilter("greaterThanOrEqual", "10"));
        customColumn.CustomFilters.Filters.Add(CreateCustomFilter("lessThan", "20"));
        filter.FilterColumns.Add(customColumn);

        XlsxAutoFilterColumn dynamicColumn = CreateColumn(1);
        dynamicColumn.HasDynamicFilter = true;
        dynamicColumn.DynamicFilter.Type = "lastMonth";
        dynamicColumn.DynamicFilter.Value = 1;
        dynamicColumn.DynamicFilter.HasValue = true;
        dynamicColumn.DynamicFilter.MaxValue = 2;
        dynamicColumn.DynamicFilter.HasMaxValue = true;
        dynamicColumn.DynamicFilter.ValueIso = "2024-01-01T00:00:00";
        dynamicColumn.DynamicFilter.MaxValueIso = "2024-01-31T00:00:00";
        filter.FilterColumns.Add(dynamicColumn);

        XlsxAutoFilterColumn topColumn = CreateColumn(2);
        topColumn.HasTop10 = true;
        topColumn.Top10.Top = true;
        topColumn.Top10.HasTop = true;
        topColumn.Top10.Percent = false;
        topColumn.Top10.HasPercent = true;
        topColumn.Top10.Value = 10;
        topColumn.Top10.HasValue = true;
        topColumn.Top10.FilterValue = 50;
        topColumn.Top10.HasFilterValue = true;
        filter.FilterColumns.Add(topColumn);

        XlsxAutoFilterColumn colorColumn = CreateColumn(3);
        colorColumn.HasColorFilter = true;
        colorColumn.ColorFilter.DxfId = 2;
        colorColumn.ColorFilter.HasDxfId = true;
        colorColumn.ColorFilter.CellColor = true;
        colorColumn.ColorFilter.HasCellColor = true;
        filter.FilterColumns.Add(colorColumn);

        XlsxAutoFilterColumn iconColumn = CreateColumn(4);
        iconColumn.HasIconFilter = true;
        iconColumn.IconFilter.IconSet = "3TrafficLights1";
        iconColumn.IconFilter.IconId = 2;
        iconColumn.IconFilter.HasIconId = true;
        filter.FilterColumns.Add(iconColumn);

        XlsxAutoFilterWriteResult result = new XlsxAutoFilterWriter().WriteString(filter);
        XlsxAutoFilter readBack = new XlsxAutoFilterReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<customFilters and=\"1\""));
        Assert.That(result.Xml, Does.Contain("operator=\"greaterThanOrEqual\""));
        Assert.That(result.Xml, Does.Contain("<dynamicFilter"));
        Assert.That(result.Xml, Does.Contain("maxValIso=\"2024-01-31T00:00:00\""));
        Assert.That(result.Xml, Does.Contain("<top10"));
        Assert.That(result.Xml, Does.Contain("filterVal=\"50\""));
        Assert.That(result.Xml, Does.Contain("<colorFilter"));
        Assert.That(result.Xml, Does.Contain("cellColor=\"1\""));
        Assert.That(result.Xml, Does.Contain("<iconFilter"));
        Assert.That(result.Xml, Does.Contain("iconSet=\"3TrafficLights1\""));

        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.FilterColumns.Count, Is.EqualTo(5));
        Assert.That(readBack.FilterColumns[0].CustomFilters.Filters.Count, Is.EqualTo(2));
        Assert.That(readBack.FilterColumns[1].DynamicFilter.HasMaxValue, Is.True);
        Assert.That(readBack.FilterColumns[2].Top10.HasFilterValue, Is.True);
        Assert.That(readBack.FilterColumns[3].ColorFilter.HasCellColor, Is.True);
        Assert.That(readBack.FilterColumns[4].IconFilter.IconSet, Is.EqualTo("3TrafficLights1"));
    }

    [Test]
    public void WriteSortStateString_WritesStandaloneSortState()
    {
        XlsxSortState sortState = new XlsxSortState();
        sortState.Reference = "A2:D20";
        sortState.ColumnSort = true;
        sortState.HasColumnSort = true;
        sortState.Conditions.Add(CreateSortCondition("C2:C20", false));

        XlsxAutoFilterWriteResult result = new XlsxAutoFilterWriter().WriteSortStateString(sortState);
        XlsxAutoFilter readBack = new XlsxAutoFilterReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<sortState"));
        Assert.That(result.Xml, Does.Contain("ref=\"A2:D20\""));
        Assert.That(result.Xml, Does.Contain("columnSort=\"1\""));
        Assert.That(result.Xml, Does.Contain("ref=\"C2:C20\""));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.HasSortState, Is.True);
        Assert.That(readBack.SortState.Conditions.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_PreservesRawXmlAndReportsInvalidInputs()
    {
        XlsxAutoFilter raw = CreateAutoFilter("A1:A2");
        raw.RawXml = "<autoFilter xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ref=\"A1:A2\" />";
        XlsxAutoFilterWriteResult rawResult = new XlsxAutoFilterWriter().WriteString(raw);

        XlsxAutoFilter invalid = CreateAutoFilter("XFE1:XFE2");
        invalid.NamespaceUri = "";
        invalid.Reference = "XFE1:XFE2";
        invalid.UnknownAttributes = null!;
        invalid.UnknownXml = null!;
        XlsxAutoFilterColumn column = new XlsxAutoFilterColumn();
        column.UnknownAttributes = null!;
        column.UnknownXml = null!;
        column.HasValues = true;
        column.Values.Values = null!;
        column.Values.UnknownAttributes = null!;
        column.Values.UnknownXml = null!;
        invalid.FilterColumns.Add(column);
        invalid.HasSortState = true;
        invalid.SortState.Reference = "";
        invalid.SortState.Conditions = null!;
        invalid.SortState.UnknownAttributes = null!;
        invalid.SortState.UnknownXml = null!;

        XlsxAutoFilterWriteResult? nullResult = new XlsxAutoFilterWriter().WriteString(null!);
        XlsxAutoFilterWriteResult invalidResult = new XlsxAutoFilterWriter().WriteString(invalid);

        Assert.That(rawResult.IsValid, Is.True);
        Assert.That(rawResult.Xml, Is.EqualTo(raw.RawXml));
        Assert.That(nullResult.IsValid, Is.False);
        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullResult, "autoFilterWriter.filterNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.namespaceMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.unknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.unknownXmlNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.columnIdMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.columnUnknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.filterValuesNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.valuesUnknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.sortState.referenceMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.sortState.conditionsNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "autoFilterWriter.sortState.unknownAttributesNull"), Is.True);
    }

    static XlsxAutoFilter CreateAutoFilter(string reference)
    {
        XlsxAutoFilter filter = new XlsxAutoFilter();
        filter.Reference = reference;
        return filter;
    }

    static XlsxAutoFilterColumn CreateColumn(uint columnId)
    {
        XlsxAutoFilterColumn column = new XlsxAutoFilterColumn();
        column.ColumnId = columnId;
        column.HasColumnId = true;
        return column;
    }

    static XlsxCustomFilter CreateCustomFilter(string operation, string value)
    {
        XlsxCustomFilter filter = new XlsxCustomFilter();
        filter.Operator = operation;
        filter.Value = value;
        return filter;
    }

    static XlsxSortCondition CreateSortCondition(string reference, bool descending)
    {
        XlsxSortCondition condition = new XlsxSortCondition();
        condition.Reference = reference;
        condition.Descending = descending;
        condition.HasDescending = true;
        condition.SortBy = "value";
        return condition;
    }

    static XlsxXmlAttribute CreateAttribute(string prefix, string localName, string namespaceUri, string value)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.Prefix = prefix;
        attribute.LocalName = localName;
        attribute.NamespaceUri = namespaceUri;
        attribute.Value = value;
        return attribute;
    }

    static bool HasDiagnostic(XlsxAutoFilterWriteResult result, string code)
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
