using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StyleBuilderTests
{
    [Test]
    public void AddToStylesPart_CreatesInternedStyleRecordsAndRawFormattingXml()
    {
        XlsxStylesPart styles = new XlsxStylesPart();
        XlsxStyleBuilder builder = CreateFullBuilder();

        int firstStyleId = builder.AddToStylesPart(styles);
        int secondStyleId = builder.AddToStylesPart(styles);
        XlsxStylesPartWriteResult writeResult = new XlsxStylesPartWriter().WriteString(styles);
        XlsxStylesPart roundTrip = new XlsxStylesPartReader().ReadString(writeResult.Xml);

        Assert.That(firstStyleId, Is.EqualTo(0));
        Assert.That(secondStyleId, Is.EqualTo(0));
        Assert.That(styles.NumberFormats.Count, Is.EqualTo(1));
        Assert.That(styles.Fonts.Count, Is.EqualTo(1));
        Assert.That(styles.Fills.Count, Is.EqualTo(1));
        Assert.That(styles.Borders.Count, Is.EqualTo(1));
        Assert.That(styles.CellFormats.Count, Is.EqualTo(1));
        Assert.That(styles.Fonts[0].RawXml, Does.Contain("<b/>"));
        Assert.That(styles.Fonts[0].RawXml, Does.Contain("<i/>"));
        Assert.That(styles.Fonts[0].RawXml, Does.Contain("Calibri"));
        Assert.That(styles.Fills[0].RawXml, Does.Contain("FFFFCC00"));
        Assert.That(styles.Borders[0].RawXml, Does.Contain("<left style=\"thin\">"));
        Assert.That(styles.CellFormats[0].RawXml, Does.Contain("<alignment"));
        Assert.That(styles.CellFormats[0].RawXml, Does.Contain("horizontal=\"center\""));
        Assert.That(styles.CellFormats[0].RawXml, Does.Contain("wrapText=\"1\""));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.NumberFormats.Count, Is.EqualTo(1));
        Assert.That(roundTrip.CellFormats.Count, Is.EqualTo(1));
        Assert.That(roundTrip.CellFormats[0].NumberFormatId, Is.EqualTo(styles.NumberFormats[0].NumberFormatId));
        Assert.That(roundTrip.CellFormats[0].HasApplyNumberFormat, Is.True);
    }

    [Test]
    public void AddToWorkbook_SavesAndOpensStylesPart()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxStyleBuilder builder = new XlsxStyleBuilder();
        builder.SetNumberFormat("0.000");
        builder.SetFillColor("00AA11");

        int styleId = builder.AddToWorkbook(workbook);
        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());

        Assert.That(styleId, Is.EqualTo(0));
        Assert.That(workbook.HasStyles, Is.True);
        Assert.That(workbook.Styles.CellFormats.Count, Is.EqualTo(1));
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(package.GetEntry(XlsxPackagePaths.Styles), Is.Not.Null);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.HasStyles, Is.True);
        Assert.That(openResult.Workbook.Styles.NumberFormats.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.Styles.Fills.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.Styles.CellFormats.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.Styles.NumberFormats[0].FormatCode, Is.EqualTo("0.000"));
    }

    [Test]
    public void Setters_NormalizeColorsAndRejectNullTargetsWithoutProperties()
    {
        XlsxStyleBuilder builder = new XlsxStyleBuilder();

        builder.SetFontColor("112233");
        builder.SetFillColor(null);

        Assert.That(builder.FontColor, Is.EqualTo("FF112233"));
        Assert.That(builder.HasFontColor, Is.True);
        Assert.That(builder.FillColor, Is.EqualTo(""));
        Assert.That(builder.HasFillColor, Is.False);
        Assert.That(builder.AddToStylesPart(null), Is.EqualTo(-1));
        Assert.That(builder.AddToWorkbook(null), Is.EqualTo(-1));
    }

    static XlsxStyleBuilder CreateFullBuilder()
    {
        XlsxStyleBuilder builder = new XlsxStyleBuilder();
        builder.SetNumberFormat("yyyy-mm-dd");
        builder.SetFontName("Calibri");
        builder.SetFontSize(11);
        builder.SetBold(true);
        builder.SetItalic(true);
        builder.SetFontColor("336699");
        builder.SetFillColor("FFCC00");
        builder.SetBorder("left", "thin", "000000");
        builder.SetHorizontalAlignment("center");
        builder.SetVerticalAlignment("top");
        builder.SetWrapText(true);
        return builder;
    }
}
