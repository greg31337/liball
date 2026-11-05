using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PivotFieldApiTests
{
    [Test]
    public void PivotFieldApis_AddRowColumnPageAndDataFieldsAndRewriteXml()
    {
        XlsxPivotTableDefinitionPart part = CreatePivotTableDefinition();

        XlsxPivotTableFieldOperationResult row = part.AddRowField("Region");
        XlsxPivotTableFieldOperationResult column = part.AddColumnField("Quarter");
        XlsxPivotTableFieldOperationResult page = part.AddPageFieldWithItem("Channel", 1);
        XlsxPivotTableFieldOperationResult data = part.AddDataFieldWithOptions("Amount", "sum", "Total Amount");
        XlsxPivotTableDefinitionWriteResult write = new XlsxPivotTableDefinitionWriter().WriteString(part);
        XlsxPivotTableDefinitionPart readBack = new XlsxPivotTableDefinitionReader().ReadStringFromPart("/xl/pivotTables/pivotTable1.xml", write.Xml);

        Assert.That(row.IsValid, Is.True);
        Assert.That(column.IsValid, Is.True);
        Assert.That(page.IsValid, Is.True);
        Assert.That(data.IsValid, Is.True);
        Assert.That(part.RowFields.Count, Is.EqualTo(1));
        Assert.That(part.RowFields[0].FieldIndex, Is.EqualTo(0));
        Assert.That(part.ColumnFields.Count, Is.EqualTo(1));
        Assert.That(part.ColumnFields[0].FieldIndex, Is.EqualTo(1));
        Assert.That(part.PageFields.Count, Is.EqualTo(1));
        Assert.That(part.PageFields[0].FieldIndex, Is.EqualTo(2));
        Assert.That(part.PageFields[0].ItemIndex, Is.EqualTo(1));
        Assert.That(part.DataFields.Count, Is.EqualTo(1));
        Assert.That(part.DataFields[0].FieldIndex, Is.EqualTo(3));
        Assert.That(part.DataFields[0].Name, Is.EqualTo("Total Amount"));
        Assert.That(part.Fields[0].Axis, Is.EqualTo("axisRow"));
        Assert.That(part.Fields[1].Axis, Is.EqualTo("axisCol"));
        Assert.That(part.Fields[2].Axis, Is.EqualTo("axisPage"));
        Assert.That(part.Fields[3].DataField, Is.True);
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<rowFields count=\"1\"><field x=\"0\" /></rowFields>"));
        Assert.That(write.Xml, Does.Contain("<colFields count=\"1\"><field x=\"1\" /></colFields>"));
        Assert.That(write.Xml, Does.Contain("<pageFields count=\"1\"><pageField fld=\"2\" item=\"1\" name=\"Channel\" /></pageFields>"));
        Assert.That(write.Xml, Does.Contain("<dataFields count=\"1\"><dataField name=\"Total Amount\" fld=\"3\" subtotal=\"sum\" /></dataFields>"));
        Assert.That(readBack.RowFields.Count, Is.EqualTo(1));
        Assert.That(readBack.ColumnFields.Count, Is.EqualTo(1));
        Assert.That(readBack.PageFields.Count, Is.EqualTo(1));
        Assert.That(readBack.DataFields.Count, Is.EqualTo(1));
    }

    [Test]
    public void PivotFieldApis_MoveAxisFieldsAndUseDataFieldDefaults()
    {
        XlsxPivotTableDefinitionPart part = CreatePivotTableDefinition();

        XlsxPivotTableFieldOperationResult row = part.AddRowField("Region");
        XlsxPivotTableFieldOperationResult column = part.AddColumnField("Region");
        XlsxPivotTableFieldOperationResult data = part.AddDataField("Amount");

        Assert.That(row.IsValid, Is.True);
        Assert.That(column.IsValid, Is.True);
        Assert.That(data.IsValid, Is.True);
        Assert.That(part.RowFields.Count, Is.EqualTo(0));
        Assert.That(part.ColumnFields.Count, Is.EqualTo(1));
        Assert.That(part.ColumnFields[0].FieldIndex, Is.EqualTo(0));
        Assert.That(part.DataFields[0].Name, Is.EqualTo("Sum of Amount"));
        Assert.That(part.DataFields[0].Subtotal, Is.EqualTo("sum"));
        Assert.That(part.RawXml, Does.Contain("<colFields count=\"1\"><field x=\"0\" /></colFields>"));
    }

    [Test]
    public void PivotFieldApis_ReportInvalidRequests()
    {
        XlsxPivotTableDefinitionPart part = CreatePivotTableDefinition();
        XlsxPivotTableFieldOperationResult missing = part.AddRowField("Missing");
        XlsxPivotTableFieldOperationResult empty = part.AddColumnField("");
        XlsxPivotTableFieldOperationResult data = part.AddDataField("Amount");
        XlsxPivotTableFieldOperationResult duplicateData = part.AddDataField("Amount");
        XlsxPivotTableDefinitionPart nullFieldsPart = CreatePivotTableDefinition();
        nullFieldsPart.Fields = null!;
        XlsxPivotTableFieldOperationResult nullFields = nullFieldsPart.AddRowField("Region");

        Assert.That(missing.IsValid, Is.False);
        Assert.That(empty.IsValid, Is.False);
        Assert.That(data.IsValid, Is.True);
        Assert.That(duplicateData.IsValid, Is.False);
        Assert.That(nullFields.IsValid, Is.False);
        Assert.That(HasDiagnostic(missing, "pivotTableField.notFound"), Is.True);
        Assert.That(HasDiagnostic(empty, "pivotTableField.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(duplicateData, "pivotTableField.duplicateDataField"), Is.True);
        Assert.That(HasDiagnostic(nullFields, "pivotTableField.fieldsNull"), Is.True);
    }

    [Test]
    public void PivotOperationFacade_ExercisesEveryFieldRelationshipAndRebuildOperation()
    {
        IXlsxPivotTableDefinitionPartOperations operations = new XlsxPivotTableDefinitionPartOperations();
        XlsxPivotTableDefinitionPart part = CreatePivotTableDefinition();
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        XlsxRelationship cacheRelationship = new XlsxRelationship();
        cacheRelationship.Id = "rIdCache";
        cacheRelationship.Type = XlsxRelationshipTypes.PivotCacheDefinition;
        cacheRelationship.Target = "../pivotCache/pivotCacheDefinition1.xml";
        cacheRelationship.TargetPath = "/xl/pivotCache/pivotCacheDefinition1.xml";
        relationships.Relationships.Add(cacheRelationship);

        XlsxPivotTableCacheRelationshipResult cache = operations.ResolveCacheRelationship(part, relationships);
        XlsxPivotTableFieldOperationResult resolved = operations.ResolveFieldForOperation(part, "Region");
        XlsxPivotTableFieldOperationResult row = operations.AddRowField(part, "Region");
        XlsxPivotTableFieldOperationResult column = operations.AddColumnField(part, "Quarter");
        XlsxPivotTableDefinitionPart pagePart = CreatePivotTableDefinition();
        XlsxPivotTableFieldOperationResult page = operations.AddPageField(pagePart, "Channel");
        XlsxPivotTableFieldOperationResult pageItem = operations.AddPageFieldWithItem(part, "Channel", 1);
        XlsxPivotTableFieldOperationResult data = operations.AddDataField(part, "Amount");
        XlsxPivotTableDefinitionPart optionsPart = CreatePivotTableDefinition();
        XlsxPivotTableFieldOperationResult dataOptions = operations.AddDataFieldWithOptions(optionsPart, "Amount", "average", "Average Amount");
        List<string> groups = new List<string>();
        groups.Add("Q1");
        groups.Add("Q2");
        XlsxPivotTableOperationResult grouping = operations.GroupFieldItems(part, "Quarter", groups);
        XlsxPivotTableOperationResult calculated = operations.AddCalculatedField(part, "Margin", "Amount*0.2");
        XlsxPivotTableOperationResult filter = operations.AddFilter(part, "Region", "captionEqual", "Region Filter");
        XlsxPivotTableOperationResult style = operations.SetStyle(part, "PivotStyleMedium2", true, true, false, true);
        List<XlsxPivotTableFieldReference> references = new List<XlsxPivotTableFieldReference>();
        XlsxPivotTableFieldOperationResult directAxis = operations.AddAxisField(part, "Amount", "axisRow", references, "customFields");
        operations.RebuildRawXmlAfterFieldOperation(part, directAxis);
        operations.RebuildRawXmlAfterOperation(part, style);

        Assert.That(cache.IsValid, Is.True);
        Assert.That(resolved.IsValid, Is.True);
        Assert.That(row.IsValid && column.IsValid && page.IsValid && pageItem.IsValid, Is.True);
        Assert.That(data.IsValid && dataOptions.IsValid, Is.True);
        Assert.That(grouping.IsValid && calculated.IsValid && filter.IsValid && style.IsValid, Is.True);
        Assert.That(directAxis.IsValid, Is.True);
        Assert.That(operations.FindFieldIndex(part, "Quarter"), Is.EqualTo(1));
        Assert.That(operations.ContainsCalculatedField(part, "Margin"), Is.True);
        Assert.That(part.RawXml, Does.Contain("pivotTableDefinition"));
    }

    static XlsxPivotTableDefinitionPart CreatePivotTableDefinition()
    {
        List<string> fields = new List<string>();
        fields.Add("Region");
        fields.Add("Quarter");
        fields.Add("Channel");
        fields.Add("Amount");
        XlsxPivotTableCreateResult result = new XlsxPivotTableDefinitionWriter().CreateFromRange("SalesPivot", 1, "Data", "A1:D20", "F3:H12", fields);
        Assert.That(result.IsValid, Is.True);
        return result.PivotTableDefinition;
    }

    static bool HasDiagnostic(XlsxPivotTableFieldOperationResult result, string code)
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
