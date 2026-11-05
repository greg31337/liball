using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class AnimationTests
{
    [Test]
    public void Slide_ParsesCommonAnimationEffectsFromTimingTree()
    {
        PptxPresentation presentation = LoadAnimationPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        List<PptxAnimationEffect> effects = slide.GetAnimations();

        Assert.That(slide.HasTiming(), Is.True);
        Assert.That(slide.GetTimingXml(), Does.Contain("Preserved Timing Extension"));
        Assert.That(effects.Count, Is.EqualTo(5));
        Assert.That(presentation.GetAnimations().Count, Is.EqualTo(5));
        AssertAnimation(effects[0], "animEffect", "entrance", "fade", "in", "2", "2", "500", "100", "2", "onClick");
        AssertAnimation(effects[1], "animEffect", "exit", "wipe", "out", "3", "3", "750", "0", "", "onBegin");
        AssertAnimation(effects[2], "animRot", "emphasis", "animRot", "", "4", "4", "400", "25", "", "onClick");
        Assert.That(effects[3].NodeType, Is.EqualTo("animMotion"));
        Assert.That(effects[3].Type, Is.EqualTo("motion"));
        Assert.That(effects[3].Effect, Is.EqualTo("motionPath"));
        Assert.That(effects[3].Path, Is.EqualTo("M 0 0 L 1 1 E"));
        Assert.That(effects[3].TargetShapeId, Is.EqualTo("5"));
        Assert.That(effects[4].NodeType, Is.EqualTo("cmd"));
        Assert.That(effects[4].Type, Is.EqualTo("media"));
        Assert.That(effects[4].Effect, Is.EqualTo("playFrom(0.0)"));
        Assert.That(effects[4].TargetShapeId, Is.EqualTo("6"));
        Assert.That(effects[4].RawXml, Does.Contain("playFrom"));
    }

    [Test]
    public void TimingTreePreservesOnRoundTrip()
    {
        PptxPresentation presentation = LoadAnimationPresentation();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.Package.GetPart(PptxPartNames.FirstSlide).GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateAnimationSlideXml())));
        Assert.That(reopened.GetSlide(0).GetAnimations().Count, Is.EqualTo(5));
        Assert.That(reopened.GetSlide(0).GetTimingXml(), Does.Contain("Preserved Timing Extension"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void SlideWithoutTimingHasNoAnimations()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        Assert.That(slide.HasTiming(), Is.False);
        Assert.That(slide.GetTimingXml(), Is.EqualTo(""));
        Assert.That(slide.GetAnimations().Count, Is.EqualTo(0));
    }

    static PptxPresentation LoadAnimationPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateAnimationSlideXml()));
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

    static void AssertAnimation(PptxAnimationEffect effect, string nodeType, string type, string effectName, string transition, string targetShapeId, string timingNodeId, string duration, string delay, string repeatCount, string trigger)
    {
        Assert.That(effect.NodeType, Is.EqualTo(nodeType));
        Assert.That(effect.Type, Is.EqualTo(type));
        Assert.That(effect.Effect, Is.EqualTo(effectName));
        Assert.That(effect.Transition, Is.EqualTo(transition));
        Assert.That(effect.TargetShapeId, Is.EqualTo(targetShapeId));
        Assert.That(effect.TimingNodeId, Is.EqualTo(timingNodeId));
        Assert.That(effect.Duration, Is.EqualTo(duration));
        Assert.That(effect.Delay, Is.EqualTo(delay));
        Assert.That(effect.RepeatCount, Is.EqualTo(repeatCount));
        Assert.That(effect.Trigger, Is.EqualTo(trigger));
    }

    static string CreateAnimationSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"http://example.com/timing\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateShapeXml("2", "Entrance Target") +
            CreateShapeXml("3", "Exit Target") +
            CreateShapeXml("4", "Emphasis Target") +
            CreateShapeXml("5", "Motion Target") +
            CreateShapeXml("6", "Media Target") +
            "</p:spTree>" +
            "</p:cSld>" +
            CreateTimingXml() +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateShapeXml(string id, string name)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr>" +
            "</p:sp>";
    }

    static string CreateTimingXml()
    {
        return
            "<p:timing>" +
            "<p:tnLst><p:par><p:cTn id=\"1\" dur=\"indefinite\" restart=\"never\" nodeType=\"tmRoot\"><p:childTnLst>" +
            CreateAnimatedParagraph("2", "500", "2", "onClick", "100", "<p:animEffect transition=\"in\" filter=\"fade\"><p:cBhvr><p:cTn id=\"200\" dur=\"500\"/><p:tgtEl><p:spTgt spid=\"2\"/></p:tgtEl></p:cBhvr></p:animEffect>") +
            CreateAnimatedParagraph("3", "750", "", "onBegin", "0", "<p:animEffect transition=\"out\" filter=\"wipe\"><p:cBhvr><p:cTn id=\"300\" dur=\"750\"/><p:tgtEl><p:spTgt spid=\"3\"/></p:tgtEl></p:cBhvr></p:animEffect>") +
            CreateAnimatedParagraph("4", "400", "", "onClick", "25", "<p:animRot by=\"5400000\"><p:cBhvr><p:cTn id=\"400\" dur=\"400\"/><p:tgtEl><p:spTgt spid=\"4\"/></p:tgtEl></p:cBhvr></p:animRot>") +
            CreateAnimatedParagraph("5", "1000", "", "onClick", "0", "<p:animMotion path=\"M 0 0 L 1 1 E\"><p:cBhvr><p:cTn id=\"500\" dur=\"1000\"/><p:tgtEl><p:spTgt spid=\"5\"/></p:tgtEl></p:cBhvr></p:animMotion>") +
            CreateAnimatedParagraph("6", "indefinite", "", "onClick", "0", "<p:cmd type=\"call\" cmd=\"playFrom(0.0)\"><p:cBhvr><p:cTn id=\"600\" dur=\"indefinite\"/><p:tgtEl><p:spTgt spid=\"6\"/></p:tgtEl></p:cBhvr></p:cmd>") +
            "</p:childTnLst></p:cTn></p:par></p:tnLst>" +
            "<p:bldLst/>" +
            "<p:extLst><p:ext uri=\"preserve\"><x:data>Preserved Timing Extension</x:data></p:ext></p:extLst>" +
            "</p:timing>";
    }

    static string CreateAnimatedParagraph(string timingId, string duration, string repeatCount, string trigger, string delay, string effectXml)
    {
        string repeat = "";
        if (repeatCount.Length > 0)
        {
            repeat = " repeatCount=\"" + repeatCount + "\"";
        }

        return
            "<p:par><p:cTn id=\"" + timingId + "\" dur=\"" + duration + "\"" + repeat + "><p:stCondLst><p:cond evt=\"" + trigger + "\" delay=\"" + delay + "\"/></p:stCondLst><p:childTnLst>" +
            effectXml +
            "</p:childTnLst></p:cTn></p:par>";
    }
}
