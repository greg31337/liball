using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class EffectTests
{
    [Test]
    public void Shape_LoadsAndPreservesCommonEffectsAndCustomEffectDag()
    {
        PptxPresentation presentation = LoadEffectPresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();
        PptxEffect common = tree.Get(0).GetEffect();
        PptxEffect custom = tree.Get(1).GetEffect();

        Assert.That(common.HasShadow, Is.True);
        Assert.That(common.ShadowBlurRadius, Is.EqualTo(40000L));
        Assert.That(common.ShadowDistance, Is.EqualTo(20000L));
        Assert.That(common.ShadowDirection, Is.EqualTo(5400000));
        Assert.That(common.ShadowAlignment, Is.EqualTo("br"));
        Assert.That(common.ShadowColor.Value, Is.EqualTo("000000"));
        Assert.That(common.ShadowTransparency, Is.EqualTo(50000));
        Assert.That(common.HasGlow, Is.True);
        Assert.That(common.GlowRadius, Is.EqualTo(60000L));
        Assert.That(common.GlowColor.Value, Is.EqualTo("00FF00"));
        Assert.That(common.GlowTransparency, Is.EqualTo(25000));
        Assert.That(common.HasReflection, Is.True);
        Assert.That(common.ReflectionBlurRadius, Is.EqualTo(10000L));
        Assert.That(common.ReflectionDistance, Is.EqualTo(20000L));
        Assert.That(common.ReflectionDirection, Is.EqualTo(2700000));
        Assert.That(common.ReflectionStartAlpha, Is.EqualTo(40000));
        Assert.That(common.ReflectionEndAlpha, Is.EqualTo(0));
        Assert.That(common.Xml, Does.Contain("effectLst"));
        Assert.That(custom.HasCustomEffect, Is.True);
        Assert.That(custom.Xml, Does.Contain("effectDag"));
    }

    [Test]
    public void Shape_SetEffectWritesShadowGlowAndReflectionAndRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxEffect effect = PptxEffect.CreateOuterShadow(PptxColor.CreateRgb("000000"), 40000L, 20000L, 5400000);
        effect.ShadowAlignment = "br";
        effect.ShadowTransparency = 30000;
        effect.SetGlow(PptxColor.CreateRgb("00FF00"), 60000L);
        effect.GlowTransparency = 25000;
        effect.SetReflection(10000L, 20000L, 2700000, 45000, 0);

        shape.SetEffect(effect);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxEffect reopenedEffect = reopened.GetSlide(0).GetShapeTree().Get(0).GetEffect();

        Assert.That(reopenedEffect.HasShadow, Is.True);
        Assert.That(reopenedEffect.ShadowColor.Value, Is.EqualTo("000000"));
        Assert.That(reopenedEffect.ShadowTransparency, Is.EqualTo(30000));
        Assert.That(reopenedEffect.ShadowAlignment, Is.EqualTo("br"));
        Assert.That(reopenedEffect.HasGlow, Is.True);
        Assert.That(reopenedEffect.GlowColor.Value, Is.EqualTo("00FF00"));
        Assert.That(reopenedEffect.GlowTransparency, Is.EqualTo(25000));
        Assert.That(reopenedEffect.HasReflection, Is.True);
        Assert.That(reopenedEffect.ReflectionStartAlpha, Is.EqualTo(45000));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_SetEffectReplacesExistingEffectsWithEmptyEffectList()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));

        shape.SetEffect(PptxEffect.CreateGlow(PptxColor.CreateRgb("00FF00"), 60000L));
        shape.SetEffect(PptxEffect.CreateNone());

        Assert.That(shape.GetEffect().HasGlow, Is.False);
        Assert.That(shape.GetEffect().Xml, Does.Contain("effectLst"));
        Assert.That(shape.GetEffect().Xml, Does.Not.Contain("glow"));
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation LoadEffectPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateEffectSlideXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string CreateEffectSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("2", "Common Effects", CreateEffectListXml()) +
            CreateShapeXml("3", "Custom Effect", "<a:effectDag><a:cont type=\"tree\"/></a:effectDag>") +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateEffectListXml()
    {
        return
            "<a:effectLst>" +
            "<a:outerShdw blurRad=\"40000\" dist=\"20000\" dir=\"5400000\" algn=\"br\">" +
            "<a:srgbClr val=\"000000\"><a:alpha val=\"50000\"/></a:srgbClr>" +
            "</a:outerShdw>" +
            "<a:glow rad=\"60000\"><a:srgbClr val=\"00FF00\"><a:alpha val=\"75000\"/></a:srgbClr></a:glow>" +
            "<a:reflection blurRad=\"10000\" dist=\"20000\" dir=\"2700000\" stA=\"40000\" endA=\"0\"/>" +
            "</a:effectLst>";
    }

    static string CreateShapeXml(string id, string name, string effectXml)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr>" +
            "<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm>" +
            "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" +
            effectXml +
            "</p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>";
    }
}
