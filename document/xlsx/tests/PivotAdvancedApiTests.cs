using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PivotAdvancedApiTests
{
    [Test]
    public void PivotAdvancedApis_AddGroupingCalculatedFieldsFiltersAndStyles()
    {
        XlsxPivotTableDefinitionPart part = CreatePivotTableDefinition();
        List<string> groups = new List<string>();
        groups.Add("Q1");
        groups.Add("Q2");

        XlsxPivotTableOperationResult grouping = part.GroupFieldItems("Quarter", groups);
        XlsxPivotTableOperationResult calculated = part.AddCalculatedField("Margin", "'Amount'*0.2");
        XlsxPivotTableOperationResult filter = part.AddFilter("Region", "captionEqual", "Region Filter");
        XlsxPivotTableOperationResult style = part.SetStyle("PivotStyleDark3", true, false, true, false);
        XlsxPivotTableDefinitionWriteResult write = new XlsxPivotTableDefinitionWriter().WriteString(part);
        XlsxPivotTableDefinitionPart readBack = new XlsxPivotTableDefinitionReader().ReadStringFromPart("/xl/pivotTables/pivotTable1.xml", write.Xml);

        Assert.That(grouping.IsValid, Is.True);
        Assert.That(calculated.IsValid, Is.True);
        Assert.That(filter.IsValid, Is.True);
        Assert.That(style.IsValid, Is.True);
        Assert.That(part.Fields[1].HasGrouping, Is.True);
        Assert.That(part.Fields[1].GroupItems.Count, Is.EqualTo(2));
        Assert.That(part.CalculatedFields.Count, Is.EqualTo(1));
        Assert.That(part.CalculatedFields[0].Name, Is.EqualTo("Margin"));
        Assert.That(part.CalculatedFields[0].Formula, Is.EqualTo("'Amount'*0.2"));
        Assert.That(part.Filters.Count, Is.EqualTo(1));
        Assert.That(part.Filters[0].Type, Is.EqualTo("captionEqual"));
        Assert.That(part.Filters[0].Areas.Count, Is.EqualTo(1));
        Assert.That(part.StyleInfo.Name, Is.EqualTo("PivotStyleDark3"));
        Assert.That(part.StyleInfo.ShowColumnHeaders, Is.False);
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<groupItems count=\"2\"><s v=\"Q1\" /><s v=\"Q2\" /></groupItems>"));
        Assert.That(write.Xml, Does.Contain("<calculatedFields count=\"1\"><calculatedField name=\"Margin\" formula=\"'Amount'*0.2\""));
        Assert.That(write.Xml, Does.Contain("<pivotFilters count=\"1\"><filter fld=\"0\" type=\"captionEqual\" id=\"1\" name=\"Region Filter\">"));
        Assert.That(write.Xml, Does.Contain("<pivotTableStyleInfo name=\"PivotStyleDark3\" showRowHeaders=\"1\" showColHeaders=\"0\" showRowStripes=\"1\" showColStripes=\"0\""));
        Assert.That(readBack.Fields[1].HasGrouping, Is.True);
        Assert.That(readBack.Fields[1].GroupItems.Count, Is.EqualTo(2));
        Assert.That(readBack.Fields[1].GroupItems[0].Name, Is.EqualTo("Q1"));
        Assert.That(readBack.CalculatedFields.Count, Is.EqualTo(1));
        Assert.That(readBack.CalculatedFields[0].Name, Is.EqualTo("Margin"));
        Assert.That(readBack.Filters.Count, Is.EqualTo(1));
        Assert.That(readBack.HasStyleInfo, Is.True);
        Assert.That(readBack.StyleInfo.Name, Is.EqualTo("PivotStyleDark3"));
    }

    [Test]
    public void PivotAdvancedApis_ReportInvalidRequests()
    {
        XlsxPivotTableDefinitionPart part = CreatePivotTableDefinition();
        List<string> badGroups = new List<string>();
        badGroups.Add("");

        XlsxPivotTableOperationResult missingGroupField = part.GroupFieldItems("Missing", new List<string>());
        XlsxPivotTableOperationResult badGroupName = part.GroupFieldItems("Quarter", badGroups);
        XlsxPivotTableOperationResult badCalculated = part.AddCalculatedField("", "");
        XlsxPivotTableOperationResult calculated = part.AddCalculatedField("Margin", "Amount*0.2");
        XlsxPivotTableOperationResult duplicateCalculated = part.AddCalculatedField("Margin", "Amount*0.3");
        XlsxPivotTableOperationResult badFilter = part.AddFilter("Region", "", "Region Filter");
        XlsxPivotTableOperationResult badStyle = part.SetStyle("", true, true, false, false);

        Assert.That(missingGroupField.IsValid, Is.False);
        Assert.That(badGroupName.IsValid, Is.False);
        Assert.That(badCalculated.IsValid, Is.False);
        Assert.That(calculated.IsValid, Is.True);
        Assert.That(duplicateCalculated.IsValid, Is.False);
        Assert.That(badFilter.IsValid, Is.False);
        Assert.That(badStyle.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingGroupField, "pivotTableField.notFound"), Is.True);
        Assert.That(HasDiagnostic(badGroupName, "pivotTableGrouping.itemNameMissing"), Is.True);
        Assert.That(HasDiagnostic(badCalculated, "pivotTableCalculatedFields.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(badCalculated, "pivotTableCalculatedFields.formulaMissing"), Is.True);
        Assert.That(HasDiagnostic(duplicateCalculated, "pivotTableCalculatedFields.duplicateName"), Is.True);
        Assert.That(HasDiagnostic(badFilter, "pivotTableFilters.typeMissing"), Is.True);
        Assert.That(HasDiagnostic(badStyle, "pivotTableStyle.nameMissing"), Is.True);
    }

    static XlsxPivotTableDefinitionPart CreatePivotTableDefinition()
    {
        List<string> fields = new List<string>();
        fields.Add("Region");
        fields.Add("Quarter");
        fields.Add("Amount");
        XlsxPivotTableCreateResult result = new XlsxPivotTableDefinitionWriter().CreateFromRange("SalesPivot", 1, "Data", "A1:C20", "F3:H12", fields);
        Assert.That(result.IsValid, Is.True);
        XlsxPivotTableFieldOperationResult row = result.PivotTableDefinition.AddRowField("Region");
        Assert.That(row.IsValid, Is.True);
        return result.PivotTableDefinition;
    }

    static bool HasDiagnostic(XlsxPivotTableOperationResult result, string code)
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
