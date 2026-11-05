using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class TableStructuredReferenceCompletionTests
{
    [Test]
    public void RenameTable_UpdatesStructuredReferencesAcrossDependentWorksheetFeatures()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Region");
        worksheet.SetTextValue("B1", "Amount");
        worksheet.AddTable("A1:B3", "Sales");
        worksheet.SetTableColumnFormula("Sales", "Amount", "Sales[Amount]+1");
        worksheet.SetTableTotalsRow("Sales", true);
        worksheet.SetTableColumnTotalsRowFormula("Sales", "Amount", "SUBTOTAL(109,Sales[Amount])");
        worksheet.SetFormulaCachedValue("D1", "LEN(\"Sales[Amount]\")+SUM(Sales[Amount])", "0", "n");
        worksheet.AddListDataValidationFormula("E1:E3", "Sales[Region]");
        worksheet.AddExpressionConditionalFormatting("F1:F3", "COUNTIF(Sales[Region],F1)>0");

        XlsxWorksheetTableResult renamed = worksheet.RenameTable("Sales", "Orders");
        XlsxTablePart table = worksheet.GetTable("Orders")!;
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart worksheetRoundTrip = new XlsxWorksheetPartReader().ReadString(worksheetWrite.Xml);
        XlsxTablePartWriteResult tableWrite = new XlsxTablePartWriter().WriteString(table);
        XlsxTablePart tableRoundTrip = new XlsxTablePartReader().ReadString(tableWrite.Xml);

        Assert.That(renamed.IsValid, Is.True);
        Assert.That(renamed.UpdatedCellCount, Is.EqualTo(4));
        Assert.That(renamed.FormulaCellCount, Is.EqualTo(4));
        Assert.That(worksheet.GetCell("B2")!.Formula, Is.EqualTo("Orders[Amount]+1"));
        Assert.That(worksheet.GetCell("B3")!.Formula, Is.EqualTo("Orders[Amount]+1"));
        Assert.That(worksheet.GetCell("B4")!.Formula, Is.EqualTo("SUBTOTAL(109,Orders[Amount])"));
        Assert.That(worksheet.GetCell("D1")!.Formula, Is.EqualTo("LEN(\"Sales[Amount]\")+SUM(Orders[Amount])"));
        Assert.That(table.Columns[1].CalculatedColumnFormula, Is.EqualTo("Orders[Amount]+1"));
        Assert.That(table.Columns[1].TotalsRowFormula, Is.EqualTo("SUBTOTAL(109,Orders[Amount])"));
        Assert.That(worksheet.DataValidations.Items[0].Formula1, Is.EqualTo("Orders[Region]"));
        Assert.That(worksheet.ConditionalFormattings[0].Rules[0].Formulas[0], Is.EqualTo("COUNTIF(Orders[Region],F1)>0"));
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(worksheetRoundTrip.GetCell("D1")!.Formula, Is.EqualTo("LEN(\"Sales[Amount]\")+SUM(Orders[Amount])"));
        Assert.That(worksheetRoundTrip.DataValidations.Items[0].Formula1, Is.EqualTo("Orders[Region]"));
        Assert.That(worksheetRoundTrip.ConditionalFormattings[0].Rules[0].Formulas[0], Is.EqualTo("COUNTIF(Orders[Region],F1)>0"));
        Assert.That(tableWrite.IsValid, Is.True);
        Assert.That(tableRoundTrip.Name, Is.EqualTo("Orders"));
        Assert.That(tableRoundTrip.Columns[1].CalculatedColumnFormula, Is.EqualTo("Orders[Amount]+1"));
        Assert.That(tableRoundTrip.Columns[1].TotalsRowFormula, Is.EqualTo("SUBTOTAL(109,Orders[Amount])"));
    }

    [Test]
    public void SetXmlColumnMapping_ClonesCallerOwnedMappingAndRoundTripsIt()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetTextValue("A1", "Amount");
        worksheet.SetNumberValue("A2", 10);
        worksheet.AddTable("A1:A2", "Sales");
        XlsxTableXmlColumnProperties mapping = CreateMapping();
        XlsxTableService service = new XlsxTableService();

        XlsxWorksheetTableResult result = service.SetXmlColumnMapping(worksheet, "Sales", "Amount", mapping);
        XlsxTableXmlColumnProperties stored = result.Column!.XmlColumnProperties;
        mapping.XPath = "/changed";
        mapping.UnknownAttributes[0].Value = "changed";
        mapping.UnknownXml[0] = "<changed xmlns=\"urn:test\" />";
        XlsxTablePartWriteResult write = new XlsxTablePartWriter().WriteString(result.Table);
        XlsxTablePart roundTrip = new XlsxTablePartReader().ReadString(write.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(stored, Is.Not.SameAs(mapping));
        Assert.That(stored.XPath, Is.EqualTo("/sales/amount"));
        Assert.That(stored.UnknownAttributes[0], Is.Not.SameAs(mapping.UnknownAttributes[0]));
        Assert.That(stored.UnknownAttributes[0].Value, Is.EqualTo("source"));
        Assert.That(stored.UnknownXml[0], Does.Contain("future"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(roundTrip.Columns[0].XmlColumnProperties.XPath, Is.EqualTo("/sales/amount"));
        Assert.That(roundTrip.Columns[0].XmlColumnProperties.UnknownAttributes[0].Value, Is.EqualTo("source"));
        Assert.That(roundTrip.Columns[0].XmlColumnProperties.UnknownXml[0], Does.Contain("future"));
    }

    static XlsxTableXmlColumnProperties CreateMapping()
    {
        XlsxTableXmlColumnProperties mapping = new XlsxTableXmlColumnProperties();
        mapping.MapId = 7;
        mapping.HasMapId = true;
        mapping.XPath = "/sales/amount";
        mapping.XmlDataType = "decimal";
        mapping.Denormalized = true;
        mapping.HasDenormalized = true;
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.NamespaceUri = "urn:test";
        attribute.LocalName = "futureAttribute";
        attribute.Prefix = "t";
        attribute.Value = "source";
        mapping.UnknownAttributes.Add(attribute);
        mapping.UnknownXml.Add("<future xmlns=\"urn:test\" />");
        return mapping;
    }
}
