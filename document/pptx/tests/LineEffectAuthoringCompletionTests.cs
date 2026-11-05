using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class LineEffectAuthoringCompletionTests
{
    [Test]
    public void LineAuthoring_RoundTripsCustomDashJoinArrowSizesAndTransparency()
    {
        PptxColorOperations colors = new PptxColorOperations();
        PptxLineOperations lines = new PptxLineOperations();
        PptxLine line = lines.CreateSolid(colors.CreatePreset("cornflowerBlue"), 25400L);
        lines.SetAppearance(line, "round", "dbl", "ctr");
        List<PptxLineDash> dashes = new List<PptxLineDash>();
        dashes.Add(lines.CreateCustomDash(150000, 50000));
        dashes.Add(lines.CreateCustomDash(25000, 75000));
        lines.SetCustomDash(line, dashes);
        lines.SetJoin(line, PptxLine.JoinMiter, 800000);
        lines.SetArrows(line, "triangle", "lg", "sm", "stealth", "sm", "lg");
        lines.SetTransparency(line, 35000);

        string xml = lines.ToXml(line);
        PptxLine reopened = lines.ParseXml(xml);

        Assert.That(reopened.Color.Type, Is.EqualTo(PptxColor.Preset));
        Assert.That(reopened.Color.Alpha, Is.EqualTo(65000));
        Assert.That(reopened.Transparency, Is.EqualTo(35000));
        Assert.That(reopened.Cap, Is.EqualTo("round"));
        Assert.That(reopened.Compound, Is.EqualTo("dbl"));
        Assert.That(reopened.Alignment, Is.EqualTo("ctr"));
        Assert.That(reopened.CustomDashes.Count, Is.EqualTo(2));
        Assert.That(reopened.CustomDashes[0].DashLength, Is.EqualTo(150000));
        Assert.That(reopened.CustomDashes[1].SpaceLength, Is.EqualTo(75000));
        Assert.That(reopened.Join, Is.EqualTo(PptxLine.JoinMiter));
        Assert.That(reopened.MiterLimit, Is.EqualTo(800000));
        Assert.That(reopened.HeadArrowWidth, Is.EqualTo("lg"));
        Assert.That(reopened.HeadArrowLength, Is.EqualTo("sm"));
        Assert.That(reopened.TailArrowWidth, Is.EqualTo("sm"));
        Assert.That(reopened.TailArrowLength, Is.EqualTo("lg"));
        Assert.That(xml, Does.Contain("custDash"));

        lines.SetPresetDash(reopened, "dashDot");
        lines.SetAppearance(reopened, "square", "sng", "in");
        string edited = lines.ToXml(reopened);
        Assert.That(edited, Does.Contain("prstDash val=\"dashDot\""));
        Assert.That(edited, Does.Not.Contain("custDash"));
        Assert.That(edited, Does.Contain("cap=\"square\""));
    }

    [Test]
    public void EffectAuthoring_RoundTripsShadowGlowReflectionAndSoftEdges()
    {
        PptxColorOperations colors = new PptxColorOperations();
        PptxEffectOperations effects = new PptxEffectOperations();
        PptxEffect effect = effects.CreateInnerShadow(colors.CreateScheme("accent1"), 40000L, 20000L, 5400000);
        effects.SetShadowTransform(effect, "br", false, 90000, 80000, 300000, -300000, 45000);
        effects.SetGlow(effect, colors.CreatePreset("gold"), 60000L);
        effects.SetGlowTransparency(effect, 25000);
        effects.SetReflection(effect, 10000L, 22000L, 2700000, 65000, 5000);
        effects.SetReflectionTransform(effect, 10000, 90000, 8100000, false, 95000, 85000, 120000, -120000, "b");
        effects.SetSoftEdge(effect, 18000L);

        string xml = effects.ToShapePropertiesXml(effect);
        PptxEffect reopened = effects.ParseShapePropertiesXml(xml);

        Assert.That(reopened.HasShadow, Is.True);
        Assert.That(reopened.ShadowType, Is.EqualTo(PptxEffect.InnerShadow));
        Assert.That(reopened.ShadowColor.Type, Is.EqualTo(PptxColor.Scheme));
        Assert.That(reopened.ShadowTransparency, Is.EqualTo(45000));
        Assert.That(reopened.ShadowRotateWithShape, Is.False);
        Assert.That(reopened.ShadowScaleX, Is.EqualTo(90000));
        Assert.That(reopened.ShadowScaleY, Is.EqualTo(80000));
        Assert.That(reopened.ShadowSkewX, Is.EqualTo(300000));
        Assert.That(reopened.ShadowSkewY, Is.EqualTo(-300000));
        Assert.That(reopened.HasGlow, Is.True);
        Assert.That(reopened.GlowColor.Type, Is.EqualTo(PptxColor.Preset));
        Assert.That(reopened.GlowTransparency, Is.EqualTo(25000));
        Assert.That(reopened.ReflectionStartAlpha, Is.EqualTo(65000));
        Assert.That(reopened.ReflectionEndAlpha, Is.EqualTo(5000));
        Assert.That(reopened.ReflectionStartPosition, Is.EqualTo(10000));
        Assert.That(reopened.ReflectionEndPosition, Is.EqualTo(90000));
        Assert.That(reopened.ReflectionFadeDirection, Is.EqualTo(8100000));
        Assert.That(reopened.ReflectionRotateWithShape, Is.False);
        Assert.That(reopened.ReflectionAlignment, Is.EqualTo("b"));
        Assert.That(reopened.HasSoftEdge, Is.True);
        Assert.That(reopened.SoftEdgeRadius, Is.EqualTo(18000L));
    }

    [Test]
    public void ThreeDEffects_RoundTripSceneShapeBevelMaterialAndColorsThroughShape()
    {
        PptxColorOperations colors = new PptxColorOperations();
        PptxEffectOperations effects = new PptxEffectOperations();
        PptxEffect effect = effects.CreateNone();
        effects.SetTopBevel(effect, 40000L, 30000L, "circle");
        effects.SetBottomBevel(effect, 20000L, 10000L, "angle");
        effects.SetShapeThreeD(effect, 12000L, 90000L, 5000L, "metal", colors.CreateScheme("accent2"), colors.CreateRgb("102030"));
        effects.SetSceneThreeD(effect, "perspectiveFront", 2700000, 125000, 600000, 1200000, 1800000, "threePt", "t", 300000, 400000, 500000);
        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        shape.SetEffect(effect);

        PptxPresentation reopenedPresentation = Reopen(presentation);
        PptxShape reopenedShape = reopenedPresentation.GetSlide(0).GetShapeTree().Get(0);
        PptxEffect reopened = reopenedShape.GetEffect();

        Assert.That(reopened.ThreeD.HasScene, Is.True);
        Assert.That(reopened.ThreeD.CameraPreset, Is.EqualTo("perspectiveFront"));
        Assert.That(reopened.ThreeD.CameraFieldOfView, Is.EqualTo(2700000));
        Assert.That(reopened.ThreeD.CameraZoom, Is.EqualTo(125000));
        Assert.That(reopened.ThreeD.CameraLongitude, Is.EqualTo(1200000));
        Assert.That(reopened.ThreeD.LightRig, Is.EqualTo("threePt"));
        Assert.That(reopened.ThreeD.LightRevolution, Is.EqualTo(500000));
        Assert.That(reopened.ThreeD.HasShape, Is.True);
        Assert.That(reopened.ThreeD.Z, Is.EqualTo(12000L));
        Assert.That(reopened.ThreeD.ExtrusionHeight, Is.EqualTo(90000L));
        Assert.That(reopened.ThreeD.ContourWidth, Is.EqualTo(5000L));
        Assert.That(reopened.ThreeD.Material, Is.EqualTo("metal"));
        Assert.That(reopened.ThreeD.TopBevel.Enabled, Is.True);
        Assert.That(reopened.ThreeD.TopBevel.Width, Is.EqualTo(40000L));
        Assert.That(reopened.ThreeD.TopBevel.Preset, Is.EqualTo("circle"));
        Assert.That(reopened.ThreeD.BottomBevel.Preset, Is.EqualTo("angle"));
        Assert.That(reopened.ThreeD.ExtrusionColor.Type, Is.EqualTo(PptxColor.Scheme));
        Assert.That(reopened.ThreeD.ContourColor.Value, Is.EqualTo("102030"));
        Assert.That(reopenedShape.Element.OuterXml, Does.Contain("scene3d"));
        Assert.That(reopenedShape.Element.OuterXml, Does.Contain("sp3d"));
    }

    [Test]
    public void EffectRewrite_PreservesUnsupportedEffectsWhileUpdatingSupportedEffects()
    {
        PptxEffectOperations effects = new PptxEffectOperations();
        PptxColorOperations colors = new PptxColorOperations();
        string xml =
            "<a:spPr xmlns:a=\"" + PptxNamespaces.Drawing + "\">" +
            "<a:effectLst><a:blur rad=\"12700\" grow=\"1\"/><a:glow rad=\"10000\"><a:srgbClr val=\"FF0000\"/></a:glow></a:effectLst>" +
            "</a:spPr>";
        PptxEffect effect = effects.ParseShapePropertiesXml(xml);
        effects.SetGlow(effect, colors.CreateRgb("00FF00"), 22000L);
        effects.SetSoftEdge(effect, 5000L);

        string rewritten = effects.ToShapePropertiesXml(effect);
        PptxEffect reopened = effects.ParseShapePropertiesXml(rewritten);

        Assert.That(rewritten, Does.Contain("<a:blur"));
        Assert.That(rewritten, Does.Contain("rad=\"12700\""));
        Assert.That(reopened.UnsupportedEffectXml.Count, Is.EqualTo(1));
        Assert.That(reopened.GlowColor.Value, Is.EqualTo("00FF00"));
        Assert.That(reopened.GlowRadius, Is.EqualTo(22000L));
        Assert.That(reopened.HasSoftEdge, Is.True);
    }

    [Test]
    public void LineAndEffectAuthoring_RejectInvalidMeasurementsAndPercentages()
    {
        PptxLineOperations lines = new PptxLineOperations();
        PptxEffectOperations effects = new PptxEffectOperations();
        PptxColor color = new PptxColorOperations().CreateRgb("000000");
        Assert.Throws<System.ArgumentOutOfRangeException>(() => lines.CreateCustomDash(0, 100000));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => lines.SetTransparency(lines.CreateSolid(color, 1L), 100001));
        Assert.Throws<System.ArgumentException>(() => lines.SetJoin(lines.CreateSolid(color, 1L), "invalid", 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => effects.CreateOuterShadow(color, -1L, 0L, 0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => effects.SetGlowTransparency(effects.CreateGlow(color, 1L), -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => effects.SetSoftEdge(effects.CreateNone(), -1L));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => effects.SetTopBevel(effects.CreateNone(), -1L, 1L, "circle"));
    }

    static PptxPresentation Reopen(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
