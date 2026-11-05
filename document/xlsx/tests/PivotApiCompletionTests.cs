using NUnit.Framework;
using System.Collections.Generic;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PivotApiCompletionTests
{
    [Test]
    public void CacheDefinitionReaderWriter_RoundTripsTypedSourceFieldsGroupingRefreshAndCalculatedItems()
    {
        string xml = "<pivotCacheDefinition xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\" r:id=\"rIdRecords1\" recordCount=\"2\" refreshedBy=\"Ada\" saveData=\"1\" refreshOnLoad=\"0\" createdVersion=\"3\"><cacheSource type=\"worksheet\"><worksheetSource ref=\"A1:C3\" sheet=\"Data\" /></cacheSource><cacheFields count=\"3\"><cacheField name=\"Category\"><sharedItems count=\"2\"><s v=\"A\" /><s v=\"B\" /></sharedItems></cacheField><cacheField name=\"Amount\" numFmtId=\"4\"><sharedItems count=\"2\"><n v=\"10\" /><n v=\"20\" /></sharedItems></cacheField><cacheField name=\"When\"><sharedItems count=\"1\"><d v=\"2026-01-02T00:00:00Z\" /></sharedItems></cacheField></cacheFields></pivotCacheDefinition>";
        XlsxPivotCacheDefinitionPart cache = new XlsxPivotCacheDefinitionReader().ReadString(xml);
        XlsxPivotService service = new XlsxPivotService();

        XlsxPivotTableOperationResult numeric = service.GroupNumericField(cache, "Amount", 0, 100, 10);
        XlsxPivotTableOperationResult dates = service.GroupDateField(cache, "When", new System.DateTime(2026, 1, 1), new System.DateTime(2026, 12, 31), XlsxPivotDateGroupBy.Months);
        XlsxPivotTableOperationResult calculated = service.AddCalculatedItem(cache, "Category", "Other", "Category*2");
        XlsxPivotCacheRefreshSettings refresh = new XlsxPivotCacheRefreshSettings();
        refresh.RefreshOnLoad = true;
        refresh.HasRefreshOnLoad = true;
        refresh.EnableRefresh = true;
        refresh.HasEnableRefresh = true;
        refresh.MissingItemsLimit = 25;
        refresh.HasMissingItemsLimit = true;
        XlsxPivotTableOperationResult refreshResult = service.SetCacheRefresh(cache, refresh);
        XlsxPivotCacheDefinitionWriteResult write = new XlsxPivotCacheDefinitionWriter().WriteString(cache);
        XlsxPivotCacheDefinitionPart roundTrip = new XlsxPivotCacheDefinitionReader().ReadString(write.Xml);

        Assert.That(cache.IsValid, Is.True);
        Assert.That(cache.HasSource, Is.True);
        Assert.That(cache.Source.WorksheetSource.Sheet, Is.EqualTo("Data"));
        Assert.That(cache.RecordsRelationshipId, Is.EqualTo("rIdRecords1"));
        Assert.That(cache.Fields.Count, Is.EqualTo(3));
        Assert.That(cache.Fields[0].SharedItems[1].Text, Is.EqualTo("B"));
        Assert.That(cache.Fields[1].SharedItems[0].Number, Is.EqualTo(10));
        Assert.That(numeric.IsValid, Is.True);
        Assert.That(dates.IsValid, Is.True);
        Assert.That(calculated.IsValid, Is.True);
        Assert.That(refreshResult.IsValid, Is.True);
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("groupInterval=\"10\""));
        Assert.That(write.Xml, Does.Contain("groupBy=\"months\""));
        Assert.That(write.Xml, Does.Contain("calculatedItem field=\"0\" name=\"Other\" formula=\"Category*2\""));
        Assert.That(write.Xml, Does.Contain("refreshOnLoad=\"1\""));
        Assert.That(roundTrip.Fields[1].Grouping.Interval, Is.EqualTo(10));
        Assert.That(roundTrip.Fields[2].Grouping.DateGroupBy, Is.EqualTo(XlsxPivotDateGroupBy.Months));
        Assert.That(roundTrip.CalculatedItems[0].Name, Is.EqualTo("Other"));
    }

    [Test]
    public void CacheRecordsAndRelationshipOperations_ExposeTypedValuesAndTargets()
    {
        string xml = "<pivotCacheRecords xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" count=\"2\"><r><x v=\"1\" /><n v=\"12.5\" /><b v=\"1\" /></r><r><s v=\"Direct\" /><m /><e v=\"#N/A\" /></r></pivotCacheRecords>";
        XlsxPivotService service = new XlsxPivotService();
        XlsxPivotCacheRecordsPart records = service.ReadRecords("/xl/pivotCache/pivotCacheRecords1.xml", xml);
        XlsxPivotCacheRecordsWriteResult write = service.WriteRecords(records);
        XlsxPivotCacheDefinitionPart cache = new XlsxPivotCacheDefinitionPart();
        cache.RecordsRelationshipId = "rIdRecords1";
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = "rIdRecords1";
        relationship.Type = XlsxRelationshipTypes.PivotCacheRecords;
        relationship.Target = "pivotCacheRecords1.xml";
        relationship.TargetPath = "/xl/pivotCache/pivotCacheRecords1.xml";
        relationships.Relationships.Add(relationship);
        XlsxPivotCacheRelationshipResult resolved = service.ResolveCacheRelationships(cache, relationships);

        Assert.That(records.IsValid, Is.True);
        Assert.That(records.Records.Count, Is.EqualTo(2));
        Assert.That(records.Records[0].Values[0].Kind, Is.EqualTo(XlsxPivotCacheValueKind.SharedItemIndex));
        Assert.That(records.Records[0].Values[0].SharedItemIndex, Is.EqualTo(1));
        Assert.That(records.Records[0].Values[1].Number, Is.EqualTo(12.5));
        Assert.That(records.Records[1].Values[1].Kind, Is.EqualTo(XlsxPivotCacheValueKind.Missing));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<x v=\"1\" />"));
        Assert.That(write.Xml, Does.Contain("<e v=\"#N/A\" />"));
        Assert.That(resolved.IsValid, Is.True);
        Assert.That(cache.HasRecordsRelationship, Is.True);
        Assert.That(cache.RecordsTargetPath, Is.EqualTo("/xl/pivotCache/pivotCacheRecords1.xml"));
    }

    [Test]
    public void PivotOperations_WriteTypedSummaryFiltersFormatsAndAreas()
    {
        List<string> fields = new List<string>();
        fields.Add("Region");
        fields.Add("Amount");
        XlsxPivotTableCreateResult create = new XlsxPivotTableDefinitionWriter().CreateFromRange("SalesPivot", 1, "Data", "A1:B20", "D3:F12", fields);
        XlsxPivotTableDefinitionPart pivot = create.PivotTableDefinition;
        pivot.AddDataFieldWithOptions("Amount", "sum", "Total Amount");
        XlsxPivotService service = new XlsxPivotService();
        XlsxPivotTableOperationResult summary = service.SetSummaryFunction(pivot, "Total Amount", XlsxPivotSummaryFunction.Average);
        XlsxPivotTableOperationResult labelFilter = service.AddLabelFilter(pivot, "Region", XlsxPivotFilterKind.LabelContains, "West");
        XlsxPivotTableOperationResult valueFilter = service.AddValueFilter(pivot, "Region", "Total Amount", XlsxPivotFilterKind.ValueGreaterThan, 100);
        XlsxPivotTableArea area = new XlsxPivotTableArea();
        area.Type = "data";
        area.FieldIndex = 1;
        area.HasFieldIndex = true;
        XlsxPivotTableOperationResult reference = service.AddAreaReference(area, 1, true, false);
        XlsxPivotTableOperationResult format = service.AddFormat(pivot, area, 3, "formatting");
        XlsxPivotTableDefinitionWriteResult write = new XlsxPivotTableDefinitionWriter().WriteString(pivot);
        XlsxPivotTableDefinitionPart roundTrip = new XlsxPivotTableDefinitionReader().ReadString(write.Xml);

        Assert.That(summary.IsValid, Is.True);
        Assert.That(labelFilter.IsValid, Is.True);
        Assert.That(valueFilter.IsValid, Is.True);
        Assert.That(reference.IsValid, Is.True);
        Assert.That(format.IsValid, Is.True);
        Assert.That(pivot.DataFields[0].SummaryFunction, Is.EqualTo(XlsxPivotSummaryFunction.Average));
        Assert.That(pivot.Filters[0].LabelValue, Is.EqualTo("West"));
        Assert.That(pivot.Filters[1].NumericValue, Is.EqualTo(100));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("subtotal=\"average\""));
        Assert.That(write.Xml, Does.Contain("type=\"captionContains\""));
        Assert.That(write.Xml, Does.Contain("stringValue1=\"West\""));
        Assert.That(write.Xml, Does.Contain("iMeasureFld=\"0\" stringValue1=\"100\""));
        Assert.That(write.Xml, Does.Contain("<formats count=\"1\"><format action=\"formatting\" dxfId=\"3\""));
        Assert.That(roundTrip.DataFields[0].SummaryFunction, Is.EqualTo(XlsxPivotSummaryFunction.Average));
        Assert.That(roundTrip.Filters[0].FilterKind, Is.EqualTo(XlsxPivotFilterKind.LabelContains));
        Assert.That(roundTrip.Filters[1].NumericValue, Is.EqualTo(100));
        Assert.That(roundTrip.Formats.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Formats[0].Area.References.Count, Is.EqualTo(1));
    }

    [Test]
    public void TypedPivotOperations_ReportInvalidInputs()
    {
        XlsxPivotCacheDefinitionPart cache = new XlsxPivotCacheDefinitionPart();
        XlsxPivotService service = new XlsxPivotService();
        XlsxPivotTableDefinitionPart pivot = new XlsxPivotTableDefinitionPart();

        XlsxPivotTableOperationResult numeric = service.GroupNumericField(cache, "Missing", 10, 1, 0);
        XlsxPivotTableOperationResult calculated = service.AddCalculatedItem(cache, "Missing", "", "");
        XlsxPivotTableOperationResult refresh = service.SetCacheRefresh(cache, null);
        XlsxPivotTableOperationResult label = service.AddLabelFilter(pivot, "Missing", XlsxPivotFilterKind.ValueEquals, "x");
        XlsxPivotCacheRecordsWriteResult records = service.WriteRecords(null);

        Assert.That(numeric.IsValid, Is.False);
        Assert.That(calculated.IsValid, Is.False);
        Assert.That(refresh.IsValid, Is.False);
        Assert.That(label.IsValid, Is.False);
        Assert.That(records.IsValid, Is.False);
    }
}
