using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ThemePartReaderTests
{
    [Test]
    public void ReadString_ParsesColorFontAndFormatSchemes()
    {
        string xml = CreateThemeXml("20000");
        XlsxThemePart part = new XlsxThemePartReader().ReadString(xml);
        XlsxThemeColor accent1 = FindColor(part, "accent1");
        XlsxThemeColor hyperlink = FindColor(part, "hlink");
        XlsxThemeColor dark1 = FindColor(part, "dk1");

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Name, Is.EqualTo("Office"));
        Assert.That(part.NamespaceUri, Is.EqualTo(XlsxNamespaces.Drawing));
        Assert.That(part.RawXml, Does.Contain("<a:theme"));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.HasColorScheme, Is.True);
        Assert.That(part.ColorScheme.Name, Is.EqualTo("Office Colors"));
        Assert.That(part.ColorScheme.Colors.Count, Is.EqualTo(4));
        Assert.That(dark1.Kind, Is.EqualTo("sysClr"));
        Assert.That(dark1.Value, Is.EqualTo("windowText"));
        Assert.That(dark1.LastColor, Is.EqualTo("000000"));
        Assert.That(accent1.Kind, Is.EqualTo("srgbClr"));
        Assert.That(accent1.Value, Is.EqualTo("4472C4"));
        Assert.That(accent1.HasTint, Is.True);
        Assert.That(accent1.Tint, Is.EqualTo(20000));
        Assert.That(accent1.HasShade, Is.True);
        Assert.That(accent1.Shade, Is.EqualTo(50000));
        Assert.That(hyperlink.HasLuminanceModulation, Is.True);
        Assert.That(hyperlink.LuminanceModulation, Is.EqualTo(80000));
        Assert.That(hyperlink.HasLuminanceOffset, Is.True);
        Assert.That(hyperlink.LuminanceOffset, Is.EqualTo(20000));
        Assert.That(part.HasFontScheme, Is.True);
        Assert.That(part.FontScheme.Name, Is.EqualTo("Office Fonts"));
        Assert.That(part.FontScheme.HasMajorFont, Is.True);
        Assert.That(part.FontScheme.MajorFont.LatinTypeface, Is.EqualTo("Aptos Display"));
        Assert.That(part.FontScheme.MajorFont.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.FontScheme.HasMinorFont, Is.True);
        Assert.That(part.FontScheme.MinorFont.LatinTypeface, Is.EqualTo("Aptos"));
        Assert.That(part.HasFormatScheme, Is.True);
        Assert.That(part.FormatScheme.Name, Is.EqualTo("Office Formats"));
        Assert.That(part.FormatScheme.FillStyleCount, Is.EqualTo(2));
        Assert.That(part.FormatScheme.LineStyleCount, Is.EqualTo(1));
        Assert.That(part.FormatScheme.EffectStyleCount, Is.EqualTo(1));
        Assert.That(part.FormatScheme.BackgroundFillStyleCount, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidTransforms()
    {
        XlsxThemePart missingRoot = new XlsxThemePartReader().ReadString("");
        XlsxThemePart invalidRoot = new XlsxThemePartReader().ReadString("<bad />");
        XlsxThemePart invalidTransform = new XlsxThemePartReader().ReadString(CreateThemeXml("bad"));

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "theme.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "theme.invalidRoot"), Is.True);
        Assert.That(invalidTransform.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidTransform, "xml.unsignedInteger.invalid"), Is.True);
    }

    static string CreateThemeXml(string tintValue)
    {
        return "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:x=\"urn:x\" name=\"Office\" x:root=\"r\">" +
            "<a:themeElements>" +
            "<a:clrScheme name=\"Office Colors\">" +
            "<a:dk1><a:sysClr val=\"windowText\" lastClr=\"000000\" /></a:dk1>" +
            "<a:lt1><a:srgbClr val=\"FFFFFF\" /></a:lt1>" +
            "<a:accent1><a:srgbClr val=\"4472C4\"><a:tint val=\"" + tintValue + "\" /><a:shade val=\"50000\" /></a:srgbClr></a:accent1>" +
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

    static bool HasDiagnostic(XlsxThemePart part, string code)
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
