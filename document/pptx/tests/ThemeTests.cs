using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ThemeTests
{
    [Test]
    public void Theme_LoadsGeneratedColorFontAndFormatSchemes()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTheme theme = presentation.GetTheme(0);

        Assert.That(theme.Name, Is.EqualTo("pptx"));
        Assert.That(theme.ColorSchemeName, Is.EqualTo("pptx"));
        Assert.That(theme.GetColorValue("dk1"), Is.EqualTo("000000"));
        Assert.That(theme.GetColorValue("lt1"), Is.EqualTo("FFFFFF"));
        Assert.That(theme.GetColorValue("accent1"), Is.EqualTo("2563EB"));
        Assert.That(theme.FontSchemeName, Is.EqualTo("pptx"));
        Assert.That(theme.GetFont("major"), Is.EqualTo("Aptos Display"));
        Assert.That(theme.GetFont("minor"), Is.EqualTo("Aptos"));
        Assert.That(theme.FormatSchemeName, Is.EqualTo("pptx"));
        Assert.That(theme.FillStyleCount(), Is.EqualTo(1));
        Assert.That(theme.GetFillStyle(0).Type, Is.EqualTo(PptxFill.Solid));
        Assert.That(theme.LineStyleCount(), Is.EqualTo(1));
        Assert.That(theme.GetLineStyle(0).Width, Is.EqualTo(9525L));
        Assert.That(theme.GetLineStyle(0).Color.Type, Is.EqualTo(PptxColor.Scheme));
        Assert.That(theme.EffectStyleCount(), Is.EqualTo(1));
        Assert.That(theme.GetEffectStyle(0).Xml, Does.Contain("effectLst"));
        Assert.That(theme.BackgroundFillStyleCount(), Is.EqualTo(1));
    }

    [Test]
    public void Theme_LoadsCustomColorFontAndFormatSchemes()
    {
        PptxPresentation presentation = LoadCustomThemePresentation();
        PptxTheme theme = presentation.Theme!;

        Assert.That(theme.Name, Is.EqualTo("Custom Theme"));
        Assert.That(theme.ColorSchemeName, Is.EqualTo("Custom Colors"));
        Assert.That(theme.GetColorValue("dk1"), Is.EqualTo("101010"));
        Assert.That(theme.GetColorValue("accent1"), Is.EqualTo("ABCDEF"));
        Assert.That(theme.GetColorValue("hlink"), Is.EqualTo("0000AA"));
        Assert.That(theme.FontSchemeName, Is.EqualTo("Custom Fonts"));
        Assert.That(theme.MajorLatinFont, Is.EqualTo("Source Serif 4"));
        Assert.That(theme.MinorLatinFont, Is.EqualTo("Source Sans 3"));
        Assert.That(theme.FormatSchemeName, Is.EqualTo("Custom Formats"));
        Assert.That(theme.FillStyleCount(), Is.EqualTo(2));
        Assert.That(theme.GetFillStyle(0).Color.Value, Is.EqualTo("111111"));
        Assert.That(theme.GetFillStyle(1).Type, Is.EqualTo(PptxFill.Gradient));
        Assert.That(theme.LineStyleCount(), Is.EqualTo(1));
        Assert.That(theme.GetLineStyle(0).Width, Is.EqualTo(25400L));
        Assert.That(theme.GetLineStyle(0).Dash, Is.EqualTo("dash"));
        Assert.That(theme.EffectStyleCount(), Is.EqualTo(1));
        Assert.That(theme.GetEffectStyle(0).HasGlow, Is.True);
        Assert.That(theme.GetEffectStyle(0).GlowColor.Value, Is.EqualTo("00FF00"));
        Assert.That(theme.BackgroundFillStyleCount(), Is.EqualTo(1));
        Assert.That(theme.GetBackgroundFillStyle(0).Type, Is.EqualTo(PptxFill.Pattern));
    }

    [Test]
    public void Theme_GetStyleRejectsOutOfRangeIndex()
    {
        PptxTheme theme = LoadGeneratedPresentation().GetTheme(0);

        Assert.Throws<System.ArgumentOutOfRangeException>(() => theme.GetFillStyle(99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => theme.GetLineStyle(99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => theme.GetEffectStyle(99));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => theme.GetBackgroundFillStyle(99));
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation LoadCustomThemePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstTheme, PptxContentTypes.Theme, Encoding.UTF8.GetBytes(CreateCustomThemeXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static string CreateCustomThemeXml()
    {
        return
            "<a:theme xmlns:a=\"" + PptxNamespaces.Drawing + "\" name=\"Custom Theme\">" +
            "<a:themeElements>" +
            CreateColorSchemeXml() +
            "<a:fontScheme name=\"Custom Fonts\"><a:majorFont><a:latin typeface=\"Source Serif 4\"/></a:majorFont><a:minorFont><a:latin typeface=\"Source Sans 3\"/></a:minorFont></a:fontScheme>" +
            CreateFormatSchemeXml() +
            "</a:themeElements>" +
            "</a:theme>";
    }

    static string CreateColorSchemeXml()
    {
        return
            "<a:clrScheme name=\"Custom Colors\">" +
            "<a:dk1><a:sysClr val=\"windowText\" lastClr=\"101010\"/></a:dk1>" +
            "<a:lt1><a:srgbClr val=\"FAFAFA\"/></a:lt1>" +
            "<a:dk2><a:srgbClr val=\"202020\"/></a:dk2>" +
            "<a:lt2><a:srgbClr val=\"EFEFEF\"/></a:lt2>" +
            "<a:accent1><a:srgbClr val=\"ABCDEF\"/></a:accent1>" +
            "<a:accent2><a:srgbClr val=\"222222\"/></a:accent2>" +
            "<a:accent3><a:srgbClr val=\"333333\"/></a:accent3>" +
            "<a:accent4><a:srgbClr val=\"444444\"/></a:accent4>" +
            "<a:accent5><a:srgbClr val=\"555555\"/></a:accent5>" +
            "<a:accent6><a:srgbClr val=\"666666\"/></a:accent6>" +
            "<a:hlink><a:srgbClr val=\"0000AA\"/></a:hlink>" +
            "<a:folHlink><a:srgbClr val=\"550088\"/></a:folHlink>" +
            "</a:clrScheme>";
    }

    static string CreateFormatSchemeXml()
    {
        return
            "<a:fmtScheme name=\"Custom Formats\">" +
            "<a:fillStyleLst>" +
            "<a:solidFill><a:srgbClr val=\"111111\"/></a:solidFill>" +
            "<a:gradFill><a:gsLst><a:gs pos=\"0\"><a:srgbClr val=\"000000\"/></a:gs><a:gs pos=\"100000\"><a:srgbClr val=\"FFFFFF\"/></a:gs></a:gsLst><a:lin ang=\"5400000\" scaled=\"1\"/></a:gradFill>" +
            "</a:fillStyleLst>" +
            "<a:lnStyleLst><a:ln w=\"25400\"><a:solidFill><a:srgbClr val=\"333333\"/></a:solidFill><a:prstDash val=\"dash\"/></a:ln></a:lnStyleLst>" +
            "<a:effectStyleLst><a:effectStyle><a:effectLst><a:glow rad=\"60000\"><a:srgbClr val=\"00FF00\"/></a:glow></a:effectLst></a:effectStyle></a:effectStyleLst>" +
            "<a:bgFillStyleLst><a:pattFill prst=\"pct5\"><a:fgClr><a:srgbClr val=\"123456\"/></a:fgClr><a:bgClr><a:srgbClr val=\"654321\"/></a:bgClr></a:pattFill></a:bgFillStyleLst>" +
            "</a:fmtScheme>";
    }
}
