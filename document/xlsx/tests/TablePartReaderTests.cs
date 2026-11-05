using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class TablePartReaderTests
{
    [Test]
    public void ReadString_ParsesTableMetadataColumnsFormulasStyleAndUnknowns()
    {
        string xml = "<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" id=\"3\" name=\"Sales\" displayName=\"SalesTable\" ref=\"A1:C5\" totalsRowShown=\"1\" headerRowCount=\"1\" x:root=\"r\">" +
            "<autoFilter ref=\"A1:C5\"><filterColumn colId=\"0\" /></autoFilter>" +
            "<sortState ref=\"A2:C5\" />" +
            "<tableColumns count=\"3\" x:cols=\"c\">" +
            "<tableColumn id=\"1\" name=\"Region\" x:col=\"r\" />" +
            "<tableColumn id=\"2\" name=\"Amount\" totalsRowFunction=\"sum\"><calculatedColumnFormula>[@Qty]*[@Price]</calculatedColumnFormula><totalsRowFormula>SUM([Amount])</totalsRowFormula><x:future /></tableColumn>" +
            "<tableColumn id=\"3\" name=\"Qty\" uniqueName=\"_1\" queryTableFieldId=\"5\" dataDxfId=\"2\" headerRowCellStyle=\"Heading\" />" +
            "</tableColumns>" +
            "<tableStyleInfo name=\"TableStyleMedium2\" showFirstColumn=\"0\" showLastColumn=\"1\" showRowStripes=\"1\" showColumnStripes=\"0\" x:style=\"s\" />" +
            "<x:futureRoot />" +
            "</table>";

        XlsxTablePart part = new XlsxTablePartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.RawXml, Does.Contain("<table"));
        Assert.That(part.HasId, Is.True);
        Assert.That(part.Id, Is.EqualTo(3));
        Assert.That(part.Name, Is.EqualTo("Sales"));
        Assert.That(part.DisplayName, Is.EqualTo("SalesTable"));
        Assert.That(part.Reference, Is.EqualTo("A1:C5"));
        Assert.That(part.HasTotalsRowShown, Is.True);
        Assert.That(part.TotalsRowShown, Is.True);
        Assert.That(part.HeaderRowCount, Is.EqualTo(1));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.AutoFilterRawXml, Does.Contain("filterColumn"));
        Assert.That(part.SortStateRawXml, Does.Contain("sortState"));
        Assert.That(part.TableColumnCount, Is.EqualTo(3));
        Assert.That(part.TableColumnsUnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Columns.Count, Is.EqualTo(3));
        Assert.That(part.Columns[0].Name, Is.EqualTo("Region"));
        Assert.That(part.Columns[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Columns[1].TotalsRowFunction, Is.EqualTo("sum"));
        Assert.That(part.Columns[1].CalculatedColumnFormula, Is.EqualTo("[@Qty]*[@Price]"));
        Assert.That(part.Columns[1].TotalsRowFormula, Is.EqualTo("SUM([Amount])"));
        Assert.That(part.Columns[1].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.Columns[2].UniqueName, Is.EqualTo("_1"));
        Assert.That(part.Columns[2].QueryTableFieldId, Is.EqualTo(5));
        Assert.That(part.Columns[2].DataDxfId, Is.EqualTo(2));
        Assert.That(part.Columns[2].HeaderRowCellStyle, Is.EqualTo("Heading"));
        Assert.That(part.HasStyleInfo, Is.True);
        Assert.That(part.StyleInfo.Name, Is.EqualTo("TableStyleMedium2"));
        Assert.That(part.StyleInfo.ShowLastColumn, Is.True);
        Assert.That(part.StyleInfo.ShowRowStripes, Is.True);
        Assert.That(part.StyleInfo.ShowColumnStripes, Is.False);
        Assert.That(part.StyleInfo.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_PreservesTableColumnRawChildXml()
    {
        string xml = "<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" id=\"1\" name=\"T\" displayName=\"T\" ref=\"A1:A2\">" +
            "<tableColumns count=\"1\">" +
            "<tableColumn id=\"1\" name=\"Xml\"><xmlColumnPr mapId=\"1\" xpath=\"/root/value\" /><extLst><ext uri=\"x\" /></extLst></tableColumn>" +
            "</tableColumns>" +
            "</table>";

        XlsxTablePart part = new XlsxTablePartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Columns.Count, Is.EqualTo(1));
        Assert.That(part.Columns[0].XmlColumnPropertiesRawXml, Does.Contain("xmlColumnPr"));
        Assert.That(part.Columns[0].ExtensionListRawXml, Does.Contain("extLst"));
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidValues()
    {
        XlsxTablePart missingRoot = new XlsxTablePartReader().ReadString("");
        XlsxTablePart invalidRoot = new XlsxTablePartReader().ReadString("<bad />");
        XlsxTablePart invalidValues = new XlsxTablePartReader().ReadString("<table xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" id=\"bad\" ref=\"XFE1:XFE2\" totalsRowShown=\"maybe\"><tableColumns count=\"bad\"><tableColumn id=\"bad\" name=\"A\" /></tableColumns><tableStyleInfo showRowStripes=\"maybe\" /></table>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "table.missingRoot"), Is.True);
        Assert.That(HasDiagnostic(invalidRoot, "table.invalidRoot"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "a1.cellReference.columnRange"), Is.True);
        Assert.That(invalidValues.Columns.Count, Is.EqualTo(1));
    }

    static bool HasDiagnostic(XlsxTablePart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
