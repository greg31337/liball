using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class AnimationEditingTests
{
    [Test]
    public void Slide_AddAnimationCreatesTimingTreeAndRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape first = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxShape second = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(120L, 0L, 100L, 100L));

        slide.AddAnimationWithTiming(first.Id, "entrance", "fade", "onClick", "500", "100", "2");
        slide.AddAnimationWithTiming(second.Id, "motion", "M 0 0 L 1 1 E", "onClick", "1000", "0", "");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxAnimationEffect> effects = reopened.GetSlide(0).GetAnimations();

        Assert.That(reopened.GetSlide(0).HasTiming(), Is.True);
        Assert.That(effects.Count, Is.EqualTo(2));
        Assert.That(effects[0].Type, Is.EqualTo("entrance"));
        Assert.That(effects[0].Effect, Is.EqualTo("fade"));
        Assert.That(effects[0].TargetShapeId, Is.EqualTo(first.Id.ToString()));
        Assert.That(effects[0].Duration, Is.EqualTo("500"));
        Assert.That(effects[0].Delay, Is.EqualTo("100"));
        Assert.That(effects[0].RepeatCount, Is.EqualTo("2"));
        Assert.That(effects[1].Type, Is.EqualTo("motion"));
        Assert.That(effects[1].Path, Is.EqualTo("M 0 0 L 1 1 E"));
        Assert.That(effects[1].TargetShapeId, Is.EqualTo(second.Id.ToString()));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Animation_SetTimingTargetReorderAndRemoveRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape first = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxShape second = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(120L, 0L, 100L, 100L));
        PptxShape third = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(240L, 0L, 100L, 100L));

        slide.AddAnimationWithTiming(first.Id, "entrance", "fade", "onClick", "500", "0", "");
        slide.AddAnimationWithTiming(second.Id, "exit", "wipe", "onClick", "700", "0", "");
        slide.AddAnimationWithTiming(third.Id, "media", "playFrom(0.0)", "onClick", "indefinite", "0", "");
        PptxAnimationEffect firstEffect = slide.GetAnimations()[0];
        firstEffect.SetTiming("afterPrevious", "900", "250", "3");
        firstEffect.SetTargetShapeId(third.Id.ToString());
        slide.MoveAnimation(0, 2);
        slide.RemoveAnimation(1);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxAnimationEffect> effects = reopened.GetSlide(0).GetAnimations();

        Assert.That(effects.Count, Is.EqualTo(2));
        Assert.That(effects[0].Type, Is.EqualTo("exit"));
        Assert.That(effects[0].TargetShapeId, Is.EqualTo(second.Id.ToString()));
        Assert.That(effects[1].Type, Is.EqualTo("entrance"));
        Assert.That(effects[1].TargetShapeId, Is.EqualTo(third.Id.ToString()));
        Assert.That(effects[1].Trigger, Is.EqualTo("afterPrevious"));
        Assert.That(effects[1].Duration, Is.EqualTo("900"));
        Assert.That(effects[1].Delay, Is.EqualTo("250"));
        Assert.That(effects[1].RepeatCount, Is.EqualTo("3"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void AnimationOperations_SetTimingPreservesAdvancedEndCondition()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape shape = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxAnimationEffect effect = new PptxSlideOperations().AddEntranceAnimation(slide, shape.Id, "fade");
        XmlDocument document = slide.GetXmlDocument();
        XmlElement timing = PptxAnimationEffect.FindNearestTimingNode(effect.Element)!;
        XmlElement startConditionList = PptxXml.FindFirstChild(timing, PptxNamespaces.Presentation, "stCondLst")!;
        XmlElement endConditionList = document.CreateElement("p", "endCondLst", PptxNamespaces.Presentation);
        XmlElement endCondition = document.CreateElement("p", "cond", PptxNamespaces.Presentation);
        PptxXml.SetAttributeValue(endCondition, "", "", "evt", "onEnd");
        PptxXml.SetAttributeValue(endCondition, "", "", "delay", "999");
        XmlElement advanced = document.CreateElement("x", "advanced", "urn:example:animation");
        PptxXml.SetAttributeValue(advanced, "", "", "value", "preserve");
        endCondition.AppendChild(advanced);
        endConditionList.AppendChild(endCondition);
        timing.InsertBefore(endConditionList, startConditionList);
        slide.Part.SetXmlDocument(document);

        new PptxAnimationEffectOperations().SetTiming(effect, PptxAnimationTrigger.AfterPrevious, "900", "250", "2");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxAnimationEffect actual = reopened.GetSlide(0).GetAnimations()[0];
        XmlElement actualTiming = PptxAnimationEffect.FindNearestTimingNode(actual.Element)!;
        XmlElement actualEndList = PptxXml.FindFirstChild(actualTiming, PptxNamespaces.Presentation, "endCondLst")!;
        XmlElement actualEnd = PptxXml.FindFirstChild(actualEndList, PptxNamespaces.Presentation, "cond")!;
        XmlElement actualStartList = PptxXml.FindFirstChild(actualTiming, PptxNamespaces.Presentation, "stCondLst")!;
        XmlElement actualStart = PptxXml.FindFirstChild(actualStartList, PptxNamespaces.Presentation, "cond")!;

        Assert.That(actual.Trigger, Is.EqualTo(PptxAnimationTrigger.AfterPrevious));
        Assert.That(actual.Delay, Is.EqualTo("250"));
        Assert.That(PptxXml.GetAttributeValue(actualEnd, "", "evt"), Is.EqualTo("onEnd"));
        Assert.That(PptxXml.GetAttributeValue(actualEnd, "", "delay"), Is.EqualTo("999"));
        Assert.That(actualEnd.OuterXml, Does.Contain("advanced"));
        Assert.That(actualEnd.OuterXml, Does.Contain("value=\"preserve\""));
        Assert.That(PptxXml.GetAttributeValue(actualStart, "", "evt"), Is.EqualTo("onBegin"));
        Assert.That(PptxXml.GetAttributeValue(actualStart, "", "delay"), Is.EqualTo("250"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void AnimationEditingRejectsInvalidIndexes()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape shape = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));

        slide.AddAnimation(shape.Id, "entrance", "fade");

        Assert.Throws<System.ArgumentOutOfRangeException>(() => slide.RemoveAnimation(2));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => slide.MoveAnimation(0, 2));
    }

    [Test]
    public void CommonAnimationOperationsSupportCategoriesSequencesMutationAndOrdering()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxSlideOperations slideOperations = new PptxSlideOperations();
        PptxAnimationEffectOperations effectOperations = new PptxAnimationEffectOperations();
        PptxShape entranceShape = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxShape emphasisShape = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(120L, 0L, 100L, 100L));
        PptxShape exitShape = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(240L, 0L, 100L, 100L));
        PptxShape motionShape = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(360L, 0L, 100L, 100L));
        PptxShape mediaShape = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(480L, 0L, 100L, 100L));

        PptxAnimationEffect entrance = slideOperations.AddEntranceAnimation(slide, entranceShape.Id, "fade");
        PptxAnimationEffect emphasis = slideOperations.AddEmphasisAnimation(slide, emphasisShape.Id, "pulse");
        PptxAnimationEffect exit = slideOperations.AddExitAnimation(slide, exitShape.Id, "fade");
        slideOperations.AddMotionPathAnimation(slide, motionShape.Id, "M 0 0 L 1 1 E");
        PptxAnimationEffect media = slideOperations.AddMediaAnimation(slide, mediaShape.Id, "playFrom(0.0)");

        effectOperations.SetEffect(entrance, PptxAnimationType.Entrance, "fly");
        effectOperations.SetTiming(emphasis, PptxAnimationTrigger.WithPrevious, "600", "50", "3");
        effectOperations.SetTrigger(exit, PptxAnimationTrigger.AfterPrevious);
        effectOperations.MoveTo(media, 0);

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxAnimationEffect> effects = reopened.GetSlide(0).GetAnimations();
        string slideXml = System.Text.Encoding.UTF8.GetString(reopened.Package.GetPart(PptxPartNames.FirstSlide).GetBytes());

        Assert.That(effects.Count, Is.EqualTo(5));
        Assert.That(effects[0].Type, Is.EqualTo(PptxAnimationType.Media));
        Assert.That(effects[0].Effect, Is.EqualTo("playFrom(0.0)"));
        Assert.That(effects[1].Type, Is.EqualTo(PptxAnimationType.Entrance));
        Assert.That(effects[1].Effect, Is.EqualTo("fly"));
        Assert.That(effects[1].Trigger, Is.EqualTo(PptxAnimationTrigger.OnClick));
        Assert.That(effects[2].Type, Is.EqualTo(PptxAnimationType.Emphasis));
        Assert.That(effects[2].Trigger, Is.EqualTo(PptxAnimationTrigger.WithPrevious));
        Assert.That(effects[2].Duration, Is.EqualTo("600"));
        Assert.That(effects[2].Delay, Is.EqualTo("50"));
        Assert.That(effects[2].RepeatCount, Is.EqualTo("3"));
        Assert.That(effects[3].Type, Is.EqualTo(PptxAnimationType.Exit));
        Assert.That(effects[3].Trigger, Is.EqualTo(PptxAnimationTrigger.AfterPrevious));
        Assert.That(effects[4].Type, Is.EqualTo(PptxAnimationType.MotionPath));
        Assert.That(effects[4].Path, Is.EqualTo("M 0 0 L 1 1 E"));
        Assert.That(slideXml, Does.Contain("nodeType=\"clickEffect\""));
        Assert.That(slideXml, Does.Contain("nodeType=\"withEffect\""));
        Assert.That(slideXml, Does.Contain("nodeType=\"afterEffect\""));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void AnimationCreationRejectsMissingTargetsAndUnsupportedCategories()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape shape = slide.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));

        Assert.That(() => slide.AddAnimation(9999, PptxAnimationType.Entrance, "fade"), Throws.ArgumentException);
        Assert.That(() => slide.AddAnimation(shape.Id, "unsupported", "fade"), Throws.ArgumentException);
        Assert.That(() => slide.AddAnimation(shape.Id, PptxAnimationType.Entrance, ""), Throws.ArgumentException);
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }
}
