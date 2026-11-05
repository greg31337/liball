using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class StyleResolverTests
{
    [Test]
    public void ResolveColor_UsesThemeColorAndAppliesAlphaTintAndShade()
    {
        PptxTheme theme = LoadCustomThemePresentation().GetTheme(0);
        PptxColor color = PptxColor.CreateScheme("accent1");
        color.Alpha = 50000;
        color.Tint = 50000;

        PptxColor resolved = theme.ResolveColor(color);

        Assert.That(resolved.Type, Is.EqualTo(PptxColor.Rgb));
        Assert.That(resolved.Value, Is.EqualTo("92B1F5"));
        Assert.That(resolved.Alpha, Is.EqualTo(50000));

        PptxColor shaded = PptxColor.CreateScheme("accent1");
        shaded.Shade = 50000;
        Assert.That(PptxStyleResolverCore.ResolveColor(theme, shaded).Value, Is.EqualTo("123175"));
    }

    [Test]
    public void ResolveFillAndLine_ResolveSchemeColors()
    {
        PptxTheme theme = LoadCustomThemePresentation().GetTheme(0);
        PptxFill fill = PptxFill.CreateSolid(PptxColor.CreateScheme("accent1"));
        PptxLine line = PptxLine.CreateSolid(PptxColor.CreateScheme("accent2"), 25400L);

        PptxFill resolvedFill = PptxStyleResolverCore.ResolveFill(theme, fill);
        PptxLine resolvedLine = PptxStyleResolverCore.ResolveLine(theme, line);

        Assert.That(resolvedFill.Color.Type, Is.EqualTo(PptxColor.Rgb));
        Assert.That(resolvedFill.Color.Value, Is.EqualTo("2563EB"));
        Assert.That(resolvedLine.Color.Value, Is.EqualTo("16A34A"));
        Assert.That(resolvedLine.Width, Is.EqualTo(25400L));
    }

    [Test]
    public void ResolveShapeFillAndLine_UsesThemeDefaultsWhenShapeHasNoDirectFormatting()
    {
        PptxPresentation presentation = LoadCustomThemePresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxTheme theme = presentation.GetTheme(0);

        PptxFill resolvedFill = PptxStyleResolverCore.ResolveShapeFill(theme, shape);
        PptxLine resolvedLine = PptxStyleResolverCore.ResolveShapeLine(theme, shape);

        Assert.That(shape.GetFill().Present, Is.False);
        Assert.That(shape.GetLine().Present, Is.False);
        Assert.That(resolvedFill.Color.Value, Is.EqualTo("2563EB"));
        Assert.That(resolvedLine.Color.Value, Is.EqualTo("16A34A"));
        Assert.That(resolvedLine.Width, Is.EqualTo(19050L));
    }

    [Test]
    public void ResolveShapeFill_RespectsExplicitNoFill()
    {
        PptxPresentation presentation = LoadCustomThemePresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        shape.SetFill(PptxFill.CreateNone());

        PptxFill resolved = PptxStyleResolverCore.ResolveShapeFill(presentation.GetTheme(0), shape);

        Assert.That(shape.GetFill().Present, Is.True);
        Assert.That(resolved.Type, Is.EqualTo(PptxFill.None));
    }

    [Test]
    public void ResolveTextDefaults_UsesThemeFontsAndDarkColor()
    {
        PptxTheme theme = LoadCustomThemePresentation().GetTheme(0);

        Assert.That(PptxStyleResolverCore.ResolveTextFont(theme, "major"), Is.EqualTo("Aptos Display"));
        Assert.That(PptxStyleResolverCore.ResolveTextFont(theme, "minor"), Is.EqualTo("Aptos"));
        Assert.That(PptxStyleResolverCore.ResolveTextFont(theme, "unknown"), Is.EqualTo("Aptos"));
        Assert.That(PptxStyleResolverCore.ResolveTextColor(theme).Value, Is.EqualTo("000000"));
    }

    static PptxPresentation LoadCustomThemePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstTheme, PptxContentTypes.Theme, Encoding.UTF8.GetBytes(CreateThemeXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static string CreateThemeXml()
    {
        return
            "<a:theme xmlns:a=\"" + PptxNamespaces.Drawing + "\" name=\"Resolver Theme\">" +
            "<a:themeElements>" +
            "<a:clrScheme name=\"Resolver Colors\">" +
            "<a:dk1><a:srgbClr val=\"000000\"/></a:dk1>" +
            "<a:lt1><a:srgbClr val=\"FFFFFF\"/></a:lt1>" +
            "<a:dk2><a:srgbClr val=\"1F2937\"/></a:dk2>" +
            "<a:lt2><a:srgbClr val=\"F9FAFB\"/></a:lt2>" +
            "<a:accent1><a:srgbClr val=\"2563EB\"/></a:accent1>" +
            "<a:accent2><a:srgbClr val=\"16A34A\"/></a:accent2>" +
            "<a:accent3><a:srgbClr val=\"DC2626\"/></a:accent3>" +
            "<a:accent4><a:srgbClr val=\"F59E0B\"/></a:accent4>" +
            "<a:accent5><a:srgbClr val=\"7C3AED\"/></a:accent5>" +
            "<a:accent6><a:srgbClr val=\"0891B2\"/></a:accent6>" +
            "<a:hlink><a:srgbClr val=\"0000EE\"/></a:hlink>" +
            "<a:folHlink><a:srgbClr val=\"551A8B\"/></a:folHlink>" +
            "</a:clrScheme>" +
            "<a:fontScheme name=\"Resolver Fonts\"><a:majorFont><a:latin typeface=\"Aptos Display\"/></a:majorFont><a:minorFont><a:latin typeface=\"Aptos\"/></a:minorFont></a:fontScheme>" +
            "<a:fmtScheme name=\"Resolver Formats\">" +
            "<a:fillStyleLst><a:solidFill><a:schemeClr val=\"accent1\"/></a:solidFill></a:fillStyleLst>" +
            "<a:lnStyleLst><a:ln w=\"19050\"><a:solidFill><a:schemeClr val=\"accent2\"/></a:solidFill><a:prstDash val=\"solid\"/></a:ln></a:lnStyleLst>" +
            "<a:effectStyleLst><a:effectStyle><a:effectLst/></a:effectStyle></a:effectStyleLst>" +
            "<a:bgFillStyleLst><a:solidFill><a:schemeClr val=\"lt1\"/></a:solidFill></a:bgFillStyleLst>" +
            "</a:fmtScheme>" +
            "</a:themeElements>" +
            "</a:theme>";
    }
}
