using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StylesPartWriterTests
{
    [Test]
    public void WriteString_RoundTripsReaderPreservedStyleMetadata()
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
        XlsxStylesPartWriteResult writeResult = new XlsxStylesPartWriter().WriteString(part);
        XlsxStylesPart roundTrip = new XlsxStylesPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<styleSheet"));
        Assert.That(writeResult.Xml, Does.Contain("numFmtId=\"164\""));
        Assert.That(writeResult.Xml, Does.Contain("patternFill"));
        Assert.That(writeResult.Xml, Does.Contain("TableStyleMedium2"));
        Assert.That(writeResult.Xml, Does.Contain("indexedColors"));
        Assert.That(writeResult.Xml, Does.Contain("futureRoot"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.NumberFormats.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Fonts.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Fills.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Borders.Count, Is.EqualTo(1));
        Assert.That(roundTrip.CellStyleFormats.Count, Is.EqualTo(1));
        Assert.That(roundTrip.CellFormats.Count, Is.EqualTo(1));
        Assert.That(roundTrip.DifferentialFormats.Count, Is.EqualTo(1));
        Assert.That(roundTrip.HasTableStyles, Is.True);
        Assert.That(roundTrip.HasColors, Is.True);
        Assert.That(roundTrip.HasExtensionList, Is.True);
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesConstructedTypedStylesPart()
    {
        XlsxStylesPart part = new XlsxStylesPart();
        XlsxNumberFormatDefinition numberFormat = new XlsxNumberFormatDefinition();
        XlsxRawStyleElement font = new XlsxRawStyleElement();
        XlsxRawStyleElement fill = new XlsxRawStyleElement();
        XlsxRawStyleElement border = new XlsxRawStyleElement();
        XlsxCellFormatDefinition format = new XlsxCellFormatDefinition();
        XlsxExtension extension = new XlsxExtension();

        numberFormat.NumberFormatId = 165;
        numberFormat.HasNumberFormatId = true;
        numberFormat.FormatCode = "0.000";
        part.NumberFormats.Add(numberFormat);
        font.RawXml = "<font><sz val=\"11\" /></font>";
        fill.RawXml = "<fill><patternFill patternType=\"none\" /></fill>";
        border.RawXml = "<border><left /></border>";
        part.Fonts.Add(font);
        part.Fills.Add(fill);
        part.Borders.Add(border);
        format.NumberFormatId = 165;
        format.HasNumberFormatId = true;
        format.FontId = 0;
        format.HasFontId = true;
        format.FillId = 0;
        format.HasFillId = true;
        format.BorderId = 0;
        format.HasBorderId = true;
        format.ApplyNumberFormat = true;
        format.HasApplyNumberFormat = true;
        format.ApplyFont = false;
        format.HasApplyFont = true;
        part.CellFormats.Add(format);
        part.HasTableStyles = true;
        part.TableStyles.DefaultTableStyle = "TableStyleMedium9";
        part.TableStyles.DefaultPivotStyle = "PivotStyleLight16";
        part.HasColors = true;
        part.Colors.RawXml = "<colors><indexedColors><rgbColor rgb=\"FFFFFFFF\" /></indexedColors></colors>";
        part.HasExtensionList = true;
        extension.Uri = "{22222222-2222-2222-2222-222222222222}";
        part.ExtensionList.Extensions.Add(extension);
        part.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");

        XlsxStylesPartWriteResult writeResult = new XlsxStylesPartWriter().WriteString(part);
        XlsxStylesPart roundTrip = new XlsxStylesPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<numFmts count=\"1\""));
        Assert.That(writeResult.Xml, Does.Contain("formatCode=\"0.000\""));
        Assert.That(writeResult.Xml, Does.Contain("<cellXfs count=\"1\""));
        Assert.That(writeResult.Xml, Does.Contain("applyNumberFormat=\"1\""));
        Assert.That(writeResult.Xml, Does.Contain("applyFont=\"0\""));
        Assert.That(writeResult.Xml, Does.Contain("TableStyleMedium9"));
        Assert.That(writeResult.Xml, Does.Contain("{22222222-2222-2222-2222-222222222222}"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.NumberFormats[0].NumberFormatId, Is.EqualTo(165));
        Assert.That(roundTrip.CellFormats[0].ApplyNumberFormat, Is.True);
        Assert.That(roundTrip.CellFormats[0].ApplyFont, Is.False);
        Assert.That(roundTrip.TableStyles.DefaultTableStyle, Is.EqualTo("TableStyleMedium9"));
        Assert.That(roundTrip.HasColors, Is.True);
        Assert.That(roundTrip.ExtensionList.Extensions.Count, Is.EqualTo(1));
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteBytes_RejectsNullPartAndNullCollections()
    {
        XlsxStylesPart nullCollections = new XlsxStylesPart();
        nullCollections.NumberFormats = null!;
        XlsxStylesPart nullItem = new XlsxStylesPart();
        nullItem.Fonts.Add(null!);

        XlsxStylesPartWriteResult? nullPartResult = new XlsxStylesPartWriter().WriteBytes(null!);
        XlsxStylesPartWriteResult nullCollectionsResult = new XlsxStylesPartWriter().WriteBytes(nullCollections);
        XlsxStylesPartWriteResult nullItemResult = new XlsxStylesPartWriter().WriteBytes(nullItem);

        Assert.That(nullPartResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullPartResult, "styles.partNull"), Is.True);
        Assert.That(nullCollectionsResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullCollectionsResult, "styles.numberFormatsNull"), Is.True);
        Assert.That(nullItemResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullItemResult, "styles.fontNull"), Is.True);
    }

    static bool HasDiagnostic(XlsxStylesPartWriteResult result, string code)
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
