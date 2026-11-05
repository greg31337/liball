using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PivotTableWriterTests
{
    [Test]
    public void CreateFromRange_CreatesPivotTableCacheRecordsAndRelationships()
    {
        List<string> fields = new List<string>();
        fields.Add("Region");
        fields.Add("Amount");

        XlsxPivotTableCreateResult result = new XlsxPivotTableDefinitionWriter().CreateFromRange("SalesPivot", 1, "Data", "A1:B20", "E3:G12", fields);
        XlsxPivotTableDefinitionWriteResult write = new XlsxPivotTableDefinitionWriter().WriteString(result.PivotTableDefinition);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.HasPivotTableDefinition, Is.True);
        Assert.That(result.PivotTableDefinition.Name, Is.EqualTo("SalesPivot"));
        Assert.That(result.PivotTableDefinition.CacheId, Is.EqualTo(1));
        Assert.That(result.PivotTableDefinition.Location.Reference, Is.EqualTo("E3:G12"));
        Assert.That(result.PivotTableDefinition.Fields.Count, Is.EqualTo(2));
        Assert.That(result.PivotTableDefinition.Fields[0].Name, Is.EqualTo("Region"));
        Assert.That(result.PivotTableDefinition.HasCacheRelationship, Is.True);
        Assert.That(result.PivotTableDefinition.CacheTargetPath, Is.EqualTo("/xl/pivotCache/pivotCacheDefinition1.xml"));
        Assert.That(result.HasPivotCacheDefinition, Is.True);
        Assert.That(result.PivotCacheDefinition.RawXml, Does.Contain("<worksheetSource ref=\"A1:B20\" sheet=\"Data\""));
        Assert.That(result.PivotCacheDefinition.RawXml, Does.Contain("<cacheField name=\"Region\""));
        Assert.That(result.PivotCacheRecordsPath, Is.EqualTo("/xl/pivotCache/pivotCacheRecords1.xml"));
        Assert.That(result.PivotCacheRecordsXml, Does.Contain("<pivotCacheRecords"));
        Assert.That(result.PivotTableRelationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(result.PivotTableRelationships.Relationships[0].Type, Is.EqualTo(XlsxRelationshipTypes.PivotCacheDefinition));
        Assert.That(result.PivotTableRelationships.Relationships[0].Target, Is.EqualTo("../pivotCache/pivotCacheDefinition1.xml"));
        Assert.That(result.PivotCacheRelationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(result.PivotCacheRelationships.Relationships[0].Type, Is.EqualTo(XlsxRelationshipTypes.PivotCacheRecords));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("name=\"SalesPivot\""));
    }

    [Test]
    public void CreateFromTable_CreatesNamedWorksheetSource()
    {
        List<string> fields = new List<string>();
        fields.Add("Category");
        fields.Add("Total");

        XlsxPivotTableCreateResult result = new XlsxPivotTableDefinitionWriter().CreateFromTable("InventoryPivot", 2, "InventoryTable", "J5:L14", fields);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PivotTableDefinition.PartPath, Is.EqualTo("/xl/pivotTables/pivotTable2.xml"));
        Assert.That(result.PivotTableDefinition.CacheId, Is.EqualTo(2));
        Assert.That(result.PivotCacheDefinition.PartPath, Is.EqualTo("/xl/pivotCache/pivotCacheDefinition2.xml"));
        Assert.That(result.PivotCacheDefinition.RawXml, Does.Contain("<worksheetSource name=\"InventoryTable\""));
        Assert.That(result.PivotCacheDefinition.RawXml, Does.Not.Contain(" sheet="));
        Assert.That(result.PivotTableDefinition.Fields[1].Name, Is.EqualTo("Total"));
        Assert.That(result.PivotTableRelationships.Relationships[0].Target, Is.EqualTo("../pivotCache/pivotCacheDefinition2.xml"));
        Assert.That(result.PivotCacheRelationships.Relationships[0].Target, Is.EqualTo("pivotCacheRecords2.xml"));
    }

    [Test]
    public void CreateFromRange_ReportsInvalidRequests()
    {
        List<string> fields = new List<string>();
        fields.Add("Region");
        fields.Add("");
        fields.Add("Region");

        XlsxPivotTableCreateResult invalid = new XlsxPivotTableDefinitionWriter().CreateFromRange("", 0, "", "XFE1:XFE2", "", fields);
        XlsxPivotTableCreateResult? nullFields = new XlsxPivotTableDefinitionWriter().CreateFromTable("Pivot", 1, "", "A1:B2", null!);

        Assert.That(invalid.IsValid, Is.False);
        Assert.That(nullFields.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalid, "pivotTableWriter.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(invalid, "pivotTableWriter.cacheIdMissing"), Is.True);
        Assert.That(HasDiagnostic(invalid, "pivotTableWriter.destinationMissing"), Is.True);
        Assert.That(HasDiagnostic(invalid, "pivotTableWriter.sourceSheetMissing"), Is.True);
        Assert.That(HasDiagnostic(invalid, "pivotTableWriter.fieldNameMissing"), Is.True);
        Assert.That(HasDiagnostic(invalid, "pivotTableWriter.fieldNameDuplicate"), Is.True);
        Assert.That(HasDiagnostic(nullFields, "pivotTableWriter.tableNameMissing"), Is.True);
        Assert.That(HasDiagnostic(nullFields, "pivotTableWriter.fieldNamesNull"), Is.True);
    }

    static bool HasDiagnostic(XlsxPivotTableCreateResult result, string code)
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
