using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ColorModelCompletionTests
{
    [Test]
    public void ColorXml_RoundTripsEverySupportedTransformAndPreservesUnknownTransforms()
    {
        PptxColorOperations operations = new PptxColorOperations();
        string xml =
            "<a:sysClr xmlns:a=\"" + PptxNamespaces.Drawing + "\" val=\"windowText\" lastClr=\"102030\">" +
            "<a:alpha val=\"75000\"/><a:tint val=\"12000\"/><a:shade val=\"88000\"/>" +
            "<a:lum val=\"45000\"/><a:lumMod val=\"90000\"/><a:lumOff val=\"-5000\"/>" +
            "<a:sat val=\"55000\"/><a:satMod val=\"80000\"/><a:satOff val=\"6000\"/>" +
            "<a:hue val=\"3600000\"/><a:hueMod val=\"70000\"/><a:hueOff val=\"1200000\"/>" +
            "<a:comp/><a:inv/><a:gray/><a:gamma/>" +
            "</a:sysClr>";

        PptxColor color = operations.ParseXml(xml);
        string written = operations.ToXml(color);
        PptxColor reopened = operations.ParseXml(written);

        Assert.That(reopened.Type, Is.EqualTo(PptxColor.System));
        Assert.That(reopened.Value, Is.EqualTo("windowText"));
        Assert.That(reopened.FallbackValue, Is.EqualTo("102030"));
        Assert.That(reopened.Alpha, Is.EqualTo(75000));
        Assert.That(reopened.Tint, Is.EqualTo(12000));
        Assert.That(reopened.Shade, Is.EqualTo(88000));
        Assert.That(reopened.Luminance, Is.EqualTo(45000));
        Assert.That(reopened.LuminanceModulation, Is.EqualTo(90000));
        Assert.That(reopened.LuminanceOffset, Is.EqualTo(-5000));
        Assert.That(reopened.Saturation, Is.EqualTo(55000));
        Assert.That(reopened.SaturationModulation, Is.EqualTo(80000));
        Assert.That(reopened.SaturationOffset, Is.EqualTo(6000));
        Assert.That(reopened.HueAngleUnits, Is.EqualTo(3600000));
        Assert.That(reopened.HueModulation, Is.EqualTo(70000));
        Assert.That(reopened.HueOffsetAngleUnits, Is.EqualTo(1200000));
        Assert.That(reopened.Complement, Is.True);
        Assert.That(reopened.Inverse, Is.True);
        Assert.That(reopened.Grayscale, Is.True);
        Assert.That(written, Does.Contain("<a:gamma"));
    }

    [Test]
    public void Resolve_ConvertsPresetSystemAndHslColorsToRgb()
    {
        PptxColorOperations operations = new PptxColorOperations();

        Assert.That(operations.Resolve(operations.CreatePreset("cornflowerBlue")).Value, Is.EqualTo("6495ED"));
        Assert.That(operations.Resolve(operations.CreateSystem("windowText", "102030")).Value, Is.EqualTo("102030"));
        Assert.That(operations.Resolve(operations.CreateSystem("window", "")).Value, Is.EqualTo("FFFFFF"));
        Assert.That(operations.Resolve(operations.CreateHsl(0.0, 100000, 50000)).Value, Is.EqualTo("FF0000"));
        Assert.That(operations.Resolve(operations.CreateHsl(120.0, 100000, 50000)).Value, Is.EqualTo("00FF00"));
        Assert.That(operations.Resolve(operations.CreateHsl(240.0, 100000, 50000)).Value, Is.EqualTo("0000FF"));
    }

    [Test]
    public void Resolve_CombinesHueSaturationLuminanceComplementInverseAndGrayscale()
    {
        PptxColorOperations operations = new PptxColorOperations();
        PptxColor color = operations.CreateRgb("FF0000");
        color.HueOffsetAngleUnits = 7200000;
        color.Saturation = 100000;
        color.Luminance = 25000;
        color.Complement = true;
        color.Inverse = true;
        color.Grayscale = true;
        color.Alpha = 42000;

        PptxColor resolved = operations.Resolve(color);

        Assert.That(resolved.Type, Is.EqualTo(PptxColor.Rgb));
        Assert.That(resolved.Value, Is.EqualTo("CACACA"));
        Assert.That(resolved.Alpha, Is.EqualTo(42000));
        Assert.That(operations.Resolve(CreateTintedColor(operations)).Value, Is.EqualTo("BF7F7F"));
    }

    [Test]
    public void Resolve_AppliesAbsoluteModulationAndOffsetTransforms()
    {
        PptxColorOperations operations = new PptxColorOperations();
        PptxColor hue = operations.CreateRgb("00FF00");
        hue.HueModulation = 50000;
        Assert.That(operations.Resolve(hue).Value, Is.EqualTo("FFFF00"));

        PptxColor saturation = operations.CreateRgb("FF0000");
        saturation.SaturationModulation = 0;
        Assert.That(operations.Resolve(saturation).Value, Is.EqualTo("808080"));

        PptxColor luminance = operations.CreateRgb("FF0000");
        luminance.LuminanceModulation = 50000;
        Assert.That(operations.Resolve(luminance).Value, Is.EqualTo("800000"));

        PptxColor offsets = operations.CreateRgb("808080");
        offsets.HueAngleUnits = 14400000;
        offsets.SaturationOffset = 100000;
        offsets.LuminanceOffset = -25000;
        Assert.That(operations.Resolve(offsets).Value, Is.EqualTo("000080"));
    }

    [Test]
    public void ThemeColors_RetainTheirKindsResolveNestedTransformsAndPersistReplacements()
    {
        PptxPresentation presentation = LoadTypedThemePresentation();
        PptxTheme theme = presentation.GetTheme(0);
        PptxThemeOperations themes = new PptxThemeOperations();
        PptxColorOperations colors = new PptxColorOperations();

        Assert.That(themes.GetColor(theme, "accent1").Type, Is.EqualTo(PptxColor.Preset));
        Assert.That(themes.GetColor(theme, "dk1").Type, Is.EqualTo(PptxColor.System));
        Assert.That(themes.GetColorValue(theme, "accent1"), Is.EqualTo("7F0000"));
        Assert.That(themes.GetColorValue(theme, "dk1"), Is.EqualTo("101010"));

        PptxColor reference = colors.CreateScheme("accent1");
        reference.Tint = 50000;
        reference.Alpha = 50000;
        PptxColor resolved = themes.ResolveColor(theme, reference);
        Assert.That(resolved.Value, Is.EqualTo("BF7F7F"));
        Assert.That(resolved.Alpha, Is.EqualTo(40000));

        PptxColor replacement = colors.CreateHsl(240.0, 100000, 50000);
        replacement.Alpha = 60000;
        themes.SetColor(theme, "accent2", replacement);
        PptxPresentation reopenedPresentation = Reopen(presentation);
        PptxTheme reopened = reopenedPresentation.GetTheme(0);
        Assert.That(themes.GetColor(reopened, "accent2").Type, Is.EqualTo(PptxColor.Hsl));
        Assert.That(themes.GetColorValue(reopened, "accent2"), Is.EqualTo("0000FF"));
        Assert.That(themes.ResolveColor(reopened, colors.CreateScheme("accent2")).Alpha, Is.EqualTo(60000));
    }

    static PptxColor CreateTintedColor(PptxColorOperations operations)
    {
        PptxColor color = operations.CreateRgb("800000");
        color.Tint = 50000;
        return color;
    }

    static PptxPresentation LoadTypedThemePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstTheme, PptxContentTypes.Theme, Encoding.UTF8.GetBytes(CreateThemeXml()));
        return PptxDocument.OpenBytes(package.SaveBytes()).LoadPresentation();
    }

    static PptxPresentation Reopen(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }

    static string CreateThemeXml()
    {
        return
            "<a:theme xmlns:a=\"" + PptxNamespaces.Drawing + "\" name=\"Typed Colors\">" +
            "<a:themeElements><a:clrScheme name=\"Typed Colors\">" +
            "<a:dk1><a:sysClr val=\"windowText\" lastClr=\"101010\"/></a:dk1>" +
            "<a:lt1><a:prstClr val=\"white\"/></a:lt1>" +
            "<a:dk2><a:prstClr val=\"black\"/></a:dk2>" +
            "<a:lt2><a:srgbClr val=\"EEEEEE\"/></a:lt2>" +
            "<a:accent1><a:prstClr val=\"red\"><a:alpha val=\"80000\"/><a:shade val=\"50000\"/></a:prstClr></a:accent1>" +
            "<a:accent2><a:hslClr hue=\"7200000\" sat=\"100000\" lum=\"50000\"/></a:accent2>" +
            "<a:accent3><a:srgbClr val=\"333333\"/></a:accent3>" +
            "<a:accent4><a:srgbClr val=\"444444\"/></a:accent4>" +
            "<a:accent5><a:srgbClr val=\"555555\"/></a:accent5>" +
            "<a:accent6><a:srgbClr val=\"666666\"/></a:accent6>" +
            "<a:hlink><a:prstClr val=\"blue\"/></a:hlink>" +
            "<a:folHlink><a:prstClr val=\"purple\"/></a:folHlink>" +
            "</a:clrScheme>" +
            "<a:fontScheme name=\"Typed Fonts\"><a:majorFont><a:latin typeface=\"Aptos\"/></a:majorFont><a:minorFont><a:latin typeface=\"Aptos\"/></a:minorFont></a:fontScheme>" +
            "<a:fmtScheme name=\"Typed Formats\"><a:fillStyleLst/><a:lnStyleLst/><a:effectStyleLst/><a:bgFillStyleLst/></a:fmtScheme>" +
            "</a:themeElements></a:theme>";
    }
}
