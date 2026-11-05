using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StylesPartReaderTests
{
    [Test]
    public void ReadString_ParsesStyleCollectionsAndMetadata()
    {
        string xml =
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"yyyy-mm-dd\" x:fmt=\"f\" /></numFmts>" +
            "<fonts count=\"1\"><font><b/><color rgb=\"FFFF0000\"/></font></fonts>" +
            "<fills count=\"1\"><fill><patternFill patternType=\"solid\"/></fill></fills>" +
            "<borders count=\"1\"><border><left/><right/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" /></cellStyleXfs>" +
            "<cellXfs count=\"1\"><xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"0\" /></cellXfs>" +
            "<dxfs count=\"1\"><dxf><font><i/></font></dxf></dxfs>" +
            "<tableStyles count=\"1\" defaultTableStyle=\"TableStyleMedium2\" defaultPivotStyle=\"PivotStyleLight16\" />" +
            "<colors><indexedColors><rgbColor rgb=\"FF000000\" /></indexedColors></colors>" +
            "<extLst><ext uri=\"{11111111-1111-1111-1111-111111111111}\"><x:future /></ext></extLst>" +
            "<x:futureRoot />" +
            "</styleSheet>";
        XlsxStylesPart part = new XlsxStylesPartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.NamespaceUri, Is.EqualTo(XlsxNamespaces.Spreadsheet));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.HasNumberFormatsCount, Is.True);
        Assert.That(part.NumberFormatsCount, Is.EqualTo(1));
        Assert.That(part.NumberFormats.Count, Is.EqualTo(1));
        Assert.That(part.NumberFormats[0].HasNumberFormatId, Is.True);
        Assert.That(part.NumberFormats[0].NumberFormatId, Is.EqualTo(164));
        Assert.That(part.NumberFormats[0].FormatCode, Is.EqualTo("yyyy-mm-dd"));
        Assert.That(part.NumberFormats[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.HasFontsCount, Is.True);
        Assert.That(part.FontsCount, Is.EqualTo(1));
        Assert.That(part.Fonts.Count, Is.EqualTo(1));
        Assert.That(part.Fonts[0].RawXml, Does.Contain("<font"));
        Assert.That(part.FillsCount, Is.EqualTo(1));
        Assert.That(part.Fills[0].RawXml, Does.Contain("patternFill"));
        Assert.That(part.BordersCount, Is.EqualTo(1));
        Assert.That(part.Borders[0].RawXml, Does.Contain("<left"));
        Assert.That(part.CellStyleFormatsCount, Is.EqualTo(1));
        Assert.That(part.CellStyleFormats.Count, Is.EqualTo(1));
        Assert.That(part.CellFormatsCount, Is.EqualTo(1));
        Assert.That(part.CellFormats.Count, Is.EqualTo(1));
        Assert.That(part.CellFormats[0].NumberFormatId, Is.EqualTo(164));
        Assert.That(part.CellFormats[0].HasApplyNumberFormat, Is.True);
        Assert.That(part.CellFormats[0].ApplyNumberFormat, Is.True);
        Assert.That(part.CellFormats[0].HasApplyFont, Is.True);
        Assert.That(part.CellFormats[0].ApplyFont, Is.False);
        Assert.That(part.CellFormats[0].RawXml, Does.Contain("applyNumberFormat"));
        Assert.That(part.DifferentialFormatsCount, Is.EqualTo(1));
        Assert.That(part.DifferentialFormats[0].RawXml, Does.Contain("<dxf"));
        Assert.That(part.HasTableStyles, Is.True);
        Assert.That(part.TableStyles.HasCount, Is.True);
        Assert.That(part.TableStyles.Count, Is.EqualTo(1));
        Assert.That(part.TableStyles.DefaultTableStyle, Is.EqualTo("TableStyleMedium2"));
        Assert.That(part.TableStyles.DefaultPivotStyle, Is.EqualTo("PivotStyleLight16"));
        Assert.That(part.TableStyles.UnknownAttributes.Count, Is.EqualTo(0));
        Assert.That(part.HasColors, Is.True);
        Assert.That(part.Colors.RawXml, Does.Contain("indexedColors"));
        Assert.That(part.HasExtensionList, Is.True);
        Assert.That(part.ExtensionList.Extensions.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_AcceptsStrictSpreadsheetNamespace()
    {
        string xml = "<styleSheet xmlns=\"http://purl.oclc.org/ooxml/spreadsheetml/main\"><fonts count=\"1\"><font /></fonts></styleSheet>";
        XlsxStylesPart part = new XlsxStylesPartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.NamespaceUri, Is.EqualTo("http://purl.oclc.org/ooxml/spreadsheetml/main"));
        Assert.That(part.FontsCount, Is.EqualTo(1));
        Assert.That(part.Fonts.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidValues()
    {
        XlsxStylesPart missingRoot = new XlsxStylesPartReader().ReadString("");
        XlsxStylesPart invalidRoot = new XlsxStylesPartReader().ReadString("<bad />");
        string invalidValuesXml =
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<numFmts count=\"bad\"><numFmt numFmtId=\"bad\" formatCode=\"x\" /></numFmts>" +
            "<cellXfs count=\"bad\"><xf applyNumberFormat=\"maybe\" /></cellXfs>" +
            "<tableStyles count=\"bad\" />" +
            "</styleSheet>";
        XlsxStylesPart invalidValues = new XlsxStylesPartReader().ReadString(invalidValuesXml);

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "styles.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "styles.invalidRoot"), Is.True);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidValues, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
    }

    static bool HasDiagnostic(XlsxStylesPart part, string code)
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
