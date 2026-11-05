using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ThemePartWriterTests
{
    [Test]
    public void WriteString_RoundTripsReaderPreservedThemeXml()
    {
        string xml = CreateThemeXml();
        XlsxThemePart part = new XlsxThemePartReader().ReadString(xml);

        XlsxThemePartWriteResult writeResult = new XlsxThemePartWriter().WriteString(part);
        XlsxThemePart roundTrip = new XlsxThemePartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("x:root=\"r\""));
        Assert.That(writeResult.Xml, Does.Contain("accent1"));
        Assert.That(writeResult.Xml, Does.Contain("fmtScheme"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.HasColorScheme, Is.True);
        Assert.That(roundTrip.ColorScheme.Colors.Count, Is.EqualTo(4));
        Assert.That(FindColor(roundTrip, "accent1").Value, Is.EqualTo("4472C4"));
        Assert.That(roundTrip.HasFontScheme, Is.True);
        Assert.That(roundTrip.FontScheme.MinorFont.LatinTypeface, Is.EqualTo("Aptos"));
        Assert.That(roundTrip.HasFormatScheme, Is.True);
        Assert.That(roundTrip.FormatScheme.FillStyleCount, Is.EqualTo(2));
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void CreateDefaultOfficeTheme_WritesReadableThemeWithExpectedSchemes()
    {
        XlsxThemePart theme = XlsxThemePartWriter.CreateDefaultOfficeTheme();

        XlsxThemePartWriteResult writeResult = new XlsxThemePartWriter().WriteString(theme);
        XlsxThemePart roundTrip = new XlsxThemePartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<a:theme"));
        Assert.That(writeResult.Xml, Does.Contain("Office Theme"));
        Assert.That(writeResult.Xml, Does.Contain("accent1"));
        Assert.That(writeResult.Xml, Does.Contain("Aptos"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Name, Is.EqualTo("Office Theme"));
        Assert.That(roundTrip.HasColorScheme, Is.True);
        Assert.That(roundTrip.ColorScheme.Colors.Count, Is.EqualTo(12));
        Assert.That(FindColor(roundTrip, "accent1").Value, Is.EqualTo("4472C4"));
        Assert.That(roundTrip.HasFontScheme, Is.True);
        Assert.That(roundTrip.FontScheme.MajorFont.LatinTypeface, Is.EqualTo("Aptos Display"));
        Assert.That(roundTrip.FontScheme.MinorFont.LatinTypeface, Is.EqualTo("Aptos"));
        Assert.That(roundTrip.HasFormatScheme, Is.True);
        Assert.That(roundTrip.FormatScheme.FillStyleCount, Is.EqualTo(2));
        Assert.That(roundTrip.FormatScheme.LineStyleCount, Is.EqualTo(1));
        Assert.That(roundTrip.FormatScheme.EffectStyleCount, Is.EqualTo(1));
        Assert.That(roundTrip.FormatScheme.BackgroundFillStyleCount, Is.EqualTo(1));
    }

    [Test]
    public void WorkbookSaveAndOpen_RoundTripsDefaultThemePart()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.Theme = XlsxThemePartWriter.CreateDefaultOfficeTheme();
        workbook.HasTheme = true;

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());

        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(package.GetEntry(XlsxPackagePaths.Theme), Is.Not.Null);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.HasTheme, Is.True);
        Assert.That(openResult.Workbook.Theme.HasColorScheme, Is.True);
        Assert.That(openResult.Workbook.Theme.ColorScheme.Colors.Count, Is.EqualTo(12));
        Assert.That(openResult.Workbook.Theme.FontScheme.MinorFont.LatinTypeface, Is.EqualTo("Aptos"));
    }

    [Test]
    public void WriteBytes_RejectsNullTheme()
    {
        XlsxThemePartWriteResult? result = new XlsxThemePartWriter().WriteBytes(null!);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "theme.partNull"), Is.True);
    }

    static string CreateThemeXml()
    {
        return "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:x=\"urn:x\" name=\"Office\" x:root=\"r\">" +
            "<a:themeElements>" +
            "<a:clrScheme name=\"Office Colors\">" +
            "<a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\" /></a:dk1>" +
            "<a:lt1><a:srgbClr val=\"FFFFFF\" /></a:lt1>" +
            "<a:accent1><a:srgbClr val=\"4472C4\"><a:tint val=\"20000\" /><a:shade val=\"50000\" /></a:srgbClr></a:accent1>" +
            "<a:hlink><a:srgbClr val=\"0563C1\"><a:lumMod val=\"80000\" /><a:lumOff val=\"20000\" /></a:srgbClr></a:hlink>" +
            "</a:clrScheme>" +
            "<a:fontScheme name=\"Office Fonts\">" +
            "<a:majorFont><a:latin typeface=\"Aptos Display\" /><a:ea typeface=\"\" /><a:cs typeface=\"\" /><a:font script=\"Jpan\" typeface=\"Yu Gothic\" /></a:majorFont>" +
            "<a:minorFont><a:latin typeface=\"Aptos\" /><a:ea typeface=\"\" /><a:cs typeface=\"\" /></a:minorFont>" +
            "</a:fontScheme>" +
            "<a:fmtScheme name=\"Office Formats\">" +
            "<a:fillStyleLst><a:solidFill><a:schemeClr val=\"phClr\" /></a:solidFill><a:gradFill /></a:fillStyleLst>" +
            "<a:lnStyleLst><a:ln w=\"6350\" /></a:lnStyleLst>" +
            "<a:effectStyleLst><a:effectStyle /></a:effectStyleLst>" +
            "<a:bgFillStyleLst><a:solidFill /></a:bgFillStyleLst>" +
            "</a:fmtScheme>" +
            "</a:themeElements>" +
            "<a:extra />" +
            "</a:theme>";
    }

    static XlsxThemeColor FindColor(XlsxThemePart part, string slot)
    {
        foreach (XlsxThemeColor color in part.ColorScheme.Colors)
        {
            if (color.Slot == slot)
            {
                return color;
            }
        }

        return new XlsxThemeColor();
    }

    static bool HasDiagnostic(XlsxThemePartWriteResult result, string code)
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
