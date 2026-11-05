using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class TablePartWriterTests
{
    [Test]
    public void WriteString_WritesTableMetadataColumnsFormulasStyleAndUnknowns()
    {
        XlsxTablePart part = CreateTablePart();
        part.AutoFilterRawXml = "<autoFilter xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ref=\"A1:C5\"><filterColumn colId=\"0\" /></autoFilter>";
        part.SortStateRawXml = "<sortState xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ref=\"A2:C5\" />";
        part.UnknownRootAttributes.Add(CreateAttribute("x", "root", "urn:x", "r"));
        part.TableColumnsUnknownAttributes.Add(CreateAttribute("x", "cols", "urn:x", "c"));
        part.UnknownXml.Add("<x:futureRoot xmlns:x=\"urn:x\" />");

        XlsxTableColumn region = CreateColumn(1, "Region");
        region.UnknownAttributes.Add(CreateAttribute("x", "col", "urn:x", "r"));
        part.Columns.Add(region);

        XlsxTableColumn amount = CreateColumn(2, "Amount");
        amount.TotalsRowFunction = "sum";
        amount.CalculatedColumnFormula = "[@Qty]*[@Price]";
        amount.TotalsRowFormula = "SUM([Amount])";
        amount.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");
        part.Columns.Add(amount);

        XlsxTableColumn quantity = CreateColumn(3, "Qty");
        quantity.UniqueName = "_1";
        quantity.QueryTableFieldId = 5;
        quantity.HasQueryTableFieldId = true;
        quantity.DataDxfId = 2;
        quantity.HasDataDxfId = true;
        quantity.HeaderRowCellStyle = "Heading";
        part.Columns.Add(quantity);

        part.HasStyleInfo = true;
        part.StyleInfo.Name = "TableStyleMedium2";
        part.StyleInfo.ShowFirstColumn = false;
        part.StyleInfo.HasShowFirstColumn = true;
        part.StyleInfo.ShowLastColumn = true;
        part.StyleInfo.HasShowLastColumn = true;
        part.StyleInfo.ShowRowStripes = true;
        part.StyleInfo.HasShowRowStripes = true;
        part.StyleInfo.ShowColumnStripes = false;
        part.StyleInfo.HasShowColumnStripes = true;
        part.StyleInfo.UnknownAttributes.Add(CreateAttribute("x", "style", "urn:x", "s"));

        XlsxTablePartWriteResult result = new XlsxTablePartWriter().WriteString(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<table"));
        Assert.That(result.Xml, Does.Contain("id=\"3\""));
        Assert.That(result.Xml, Does.Contain("name=\"Sales\""));
        Assert.That(result.Xml, Does.Contain("displayName=\"SalesTable\""));
        Assert.That(result.Xml, Does.Contain("ref=\"A1:C5\""));
        Assert.That(result.Xml, Does.Contain("totalsRowShown=\"1\""));
        Assert.That(result.Xml, Does.Contain("x:root=\"r\""));
        Assert.That(result.Xml, Does.Contain("<autoFilter"));
        Assert.That(result.Xml, Does.Contain("<sortState"));
        Assert.That(result.Xml, Does.Contain("<tableColumns count=\"3\""));
        Assert.That(result.Xml, Does.Contain("x:cols=\"c\""));
        Assert.That(result.Xml, Does.Contain("totalsRowFunction=\"sum\""));
        Assert.That(result.Xml, Does.Contain("<calculatedColumnFormula>[@Qty]*[@Price]</calculatedColumnFormula>"));
        Assert.That(result.Xml, Does.Contain("<totalsRowFormula>SUM([Amount])</totalsRowFormula>"));
        Assert.That(result.Xml, Does.Contain("<tableStyleInfo"));
        Assert.That(result.Xml, Does.Contain("showLastColumn=\"1\""));
        Assert.That(result.Xml, Does.Contain("showColumnStripes=\"0\""));
        Assert.That(result.Xml, Does.Contain("<x:futureRoot"));

        XlsxTablePart readBack = new XlsxTablePartReader().ReadString(result.Xml);
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.Columns.Count, Is.EqualTo(3));
        Assert.That(readBack.Columns[1].CalculatedColumnFormula, Is.EqualTo("[@Qty]*[@Price]"));
        Assert.That(readBack.Columns[1].TotalsRowFormula, Is.EqualTo("SUM([Amount])"));
        Assert.That(readBack.StyleInfo.ShowLastColumn, Is.True);
        Assert.That(readBack.StyleInfo.ShowColumnStripes, Is.False);
        Assert.That(readBack.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_PreservesRawFormulaAndColumnChildXml()
    {
        XlsxTablePart part = CreateTablePart();
        XlsxTableColumn column = CreateColumn(1, "Amount");
        column.CalculatedColumnFormula = "ignored";
        column.CalculatedColumnFormulaRawXml = "<calculatedColumnFormula xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" array=\"1\">SUM(A2:A5)</calculatedColumnFormula>";
        column.TotalsRowFormulaRawXml = "<totalsRowFormula xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">SUBTOTAL(109,[Amount])</totalsRowFormula>";
        column.XmlColumnPropertiesRawXml = "<xmlColumnPr xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" mapId=\"1\" xpath=\"/root/value\" />";
        column.ExtensionListRawXml = "<extLst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><ext uri=\"x\" /></extLst>";
        part.Columns.Add(column);

        XlsxTablePartWriteResult result = new XlsxTablePartWriter().WriteString(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("array=\"1\""));
        Assert.That(result.Xml, Does.Contain("SUM(A2:A5)"));
        Assert.That(result.Xml, Does.Contain("SUBTOTAL(109,[Amount])"));
        Assert.That(result.Xml, Does.Contain("<xmlColumnPr"));
        Assert.That(result.Xml, Does.Contain("<extLst"));
        Assert.That(result.Xml, Does.Not.Contain(">ignored<"));

        XlsxTablePart readBack = new XlsxTablePartReader().ReadString(result.Xml);
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.Columns[0].CalculatedColumnFormula, Is.EqualTo("SUM(A2:A5)"));
        Assert.That(readBack.Columns[0].TotalsRowFormula, Is.EqualTo("SUBTOTAL(109,[Amount])"));
        Assert.That(readBack.Columns[0].XmlColumnPropertiesRawXml, Does.Contain("xmlColumnPr"));
        Assert.That(readBack.Columns[0].ExtensionListRawXml, Does.Contain("extLst"));
    }

    [Test]
    public void WriteString_PreservesWholeRawTableXmlWhenPresent()
    {
        XlsxTablePart part = CreateTablePart();
        part.RawXml = "<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" id=\"9\" name=\"Raw\" displayName=\"Raw\" ref=\"B1:B2\"><tableColumns count=\"1\"><tableColumn id=\"1\" name=\"Raw\" /></tableColumns></table>";

        XlsxTablePartWriteResult result = new XlsxTablePartWriter().WriteString(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Is.EqualTo(part.RawXml));
    }

    [Test]
    public void WriteString_ReportsInvalidInputs()
    {
        XlsxTablePartWriteResult? nullResult = new XlsxTablePartWriter().WriteString(null);
        XlsxTablePart invalid = CreateTablePart();
        invalid.NamespaceUri = "";
        invalid.HasId = false;
        invalid.Name = "";
        invalid.DisplayName = "";
        invalid.Reference = "XFE1:XFE2";
        invalid.HasTableColumnCount = true;
        invalid.TableColumnCount = 2;
        invalid.UnknownRootAttributes = null!;
        invalid.TableColumnsUnknownAttributes = null!;
        invalid.UnknownXml = null!;
        invalid.HasStyleInfo = true;
        invalid.StyleInfo.UnknownAttributes = null!;
        XlsxTableColumn column = new XlsxTableColumn();
        column.UnknownAttributes = null!;
        column.UnknownXml = null!;
        invalid.Columns.Add(column);

        XlsxTablePartWriteResult result = new XlsxTablePartWriter().WriteString(invalid);

        Assert.That(nullResult.IsValid, Is.False);
        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullResult, "tableWriter.partNull"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.namespaceMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.idMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.nameMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.displayNameMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.tableColumnCountMismatch"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.unknownRootAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.tableColumnsUnknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.unknownXmlNull"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.styleInfoUnknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.columnIdMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.columnNameMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.columnUnknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(result, "tableWriter.columnUnknownXmlNull"), Is.True);
    }

    static XlsxTablePart CreateTablePart()
    {
        XlsxTablePart part = new XlsxTablePart();
        part.Id = 3;
        part.HasId = true;
        part.Name = "Sales";
        part.DisplayName = "SalesTable";
        part.Reference = "A1:C5";
        part.HeaderRowCount = 1;
        part.HasHeaderRowCount = true;
        part.TotalsRowShown = true;
        part.HasTotalsRowShown = true;
        return part;
    }

    static XlsxTableColumn CreateColumn(uint id, string name)
    {
        XlsxTableColumn column = new XlsxTableColumn();
        column.Id = id;
        column.HasId = true;
        column.Name = name;
        return column;
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

    static bool HasDiagnostic(XlsxTablePartWriteResult result, string code)
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
