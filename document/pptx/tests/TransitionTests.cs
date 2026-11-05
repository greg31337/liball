using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TransitionTests
{
    [Test]
    public void Slide_ParsesExistingTransitionAndAdvanceSettings()
    {
        PptxPresentation presentation = LoadTransitionPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxSlideTransition transition = slide.GetTransition();

        Assert.That(slide.HasTransition(), Is.True);
        Assert.That(transition.HasTransition(), Is.True);
        Assert.That(transition.Type, Is.EqualTo("push"));
        Assert.That(transition.Speed, Is.EqualTo("slow"));
        Assert.That(transition.Direction, Is.EqualTo("l"));
        Assert.That(transition.AdvanceOnClick, Is.False);
        Assert.That(transition.AdvanceAfter, Is.True);
        Assert.That(transition.AdvanceAfterTime, Is.EqualTo("3000"));
        Assert.That(transition.RawXml, Does.Contain("Preserved Transition Extension"));
        Assert.That(presentation.GetTransitions().Count, Is.EqualTo(1));
    }

    [Test]
    public void TransitionPreservesOnRoundTrip()
    {
        PptxPresentation presentation = LoadTransitionPresentation();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.Package.GetPart(PptxPartNames.FirstSlide).GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes(CreateTransitionSlideXml())));
        Assert.That(reopened.GetSlide(0).GetTransition().RawXml, Does.Contain("Preserved Transition Extension"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_SetTransitionEditsCommonSettingsAndPreservesNonChoiceChildren()
    {
        PptxPresentation presentation = LoadTransitionPresentation();
        PptxSlideTransition transition = presentation.GetSlide(0).GetTransition();

        transition.SetType("wipe");
        transition.SetDirection("r");
        transition.SetSpeed("fast");
        transition.SetAdvance(true, "5000");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxSlideTransition reopenedTransition = reopened.GetSlide(0).GetTransition();
        string slideXml = Encoding.UTF8.GetString(reopened.Package.GetPart(PptxPartNames.FirstSlide).GetBytes());

        Assert.That(reopenedTransition.Type, Is.EqualTo("wipe"));
        Assert.That(reopenedTransition.Direction, Is.EqualTo("r"));
        Assert.That(reopenedTransition.Speed, Is.EqualTo("fast"));
        Assert.That(reopenedTransition.AdvanceOnClick, Is.True);
        Assert.That(reopenedTransition.AdvanceAfterTime, Is.EqualTo("5000"));
        Assert.That(slideXml, Does.Contain("Preserved Transition Extension"));
        Assert.That(slideXml, Does.Contain("sndAc"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_CreatesAndRemovesTransition()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        Assert.That(slide.HasTransition(), Is.False);

        slide.SetTransitionOptions("fade", "med", true, "2500");
        PptxPresentation withTransition = SaveReopenAndLoad(presentation);
        PptxSlideTransition transition = withTransition.GetSlide(0).GetTransition();

        Assert.That(transition.Type, Is.EqualTo("fade"));
        Assert.That(transition.Speed, Is.EqualTo("med"));
        Assert.That(transition.AdvanceOnClick, Is.True);
        Assert.That(transition.AdvanceAfterTime, Is.EqualTo("2500"));

        withTransition.GetSlide(0).RemoveTransition();
        PptxPresentation removed = SaveReopenAndLoad(withTransition);

        Assert.That(removed.GetSlide(0).HasTransition(), Is.False);
        Assert.That(removed.GetSlide(0).GetTransition().Type, Is.EqualTo(""));
        Assert.That(removed.Package.Validate().Succeeded(), Is.True, removed.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void TransitionAdvanceAndPresentationLoopOperationsRoundTrip()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        PptxSlideTransition transition = presentation.GetSlide(0).GetTransition();
        PptxSlideTransitionOperations transitionOperations = new PptxSlideTransitionOperations();
        PptxPresentationPropertiesOperations propertiesOperations = new PptxPresentationPropertiesOperations();

        transitionOperations.SetType(transition, "fade");
        transitionOperations.SetAdvanceOnClick(transition, false);
        transitionOperations.SetAdvanceAfterMilliseconds(transition, 2400L);
        Assert.That(() => transitionOperations.SetAdvanceAfterMilliseconds(transition, -1L), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        Assert.That(presentation.Properties, Is.Not.Null);
        propertiesOperations.SetLoop(presentation.Properties!, true);
        propertiesOperations.SetUseTimings(presentation.Properties!, false);

        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxSlideTransition actual = reopened.GetSlide(0).GetTransition();

        Assert.That(actual.AdvanceOnClick, Is.False);
        Assert.That(actual.AdvanceAfter, Is.True);
        Assert.That(actual.AdvanceAfterMilliseconds, Is.EqualTo(2400L));
        Assert.That(reopened.Properties, Is.Not.Null);
        Assert.That(reopened.Properties!.Loop, Is.True);
        Assert.That(reopened.Properties.UseTimings, Is.False);

        transitionOperations.ClearAdvanceAfter(actual);
        PptxPresentation cleared = SaveReopenAndLoad(reopened);

        Assert.That(cleared.GetSlide(0).GetTransition().AdvanceAfter, Is.False);
        Assert.That(cleared.GetSlide(0).GetTransition().AdvanceAfterMilliseconds, Is.EqualTo(0L));
        Assert.That(cleared.Package.Validate().Succeeded(), Is.True, cleared.Package.Validate().FormatDiagnostics());
    }

    static PptxPresentation LoadTransitionPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateTransitionSlideXml()));
        package.AddPart("/ppt/media/click.mp3", PptxContentTypes.Mp3, new byte[] { 73, 68, 51 });
        package.AddRelationshipWithId(PptxPartNames.FirstSlide, "rIdSound", PptxRelationshipTypes.Audio, "../media/click.mp3", PptxRelationshipTargetMode.Internal);
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

    static string CreateTransitionSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:x=\"http://example.com/transition\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "<p:transition spd=\"slow\" advClick=\"0\" advTm=\"3000\">" +
            "<p:push dir=\"l\"/>" +
            "<p:sndAc><p:stSnd><p:snd r:embed=\"rIdSound\" name=\"Click\"/></p:stSnd></p:sndAc>" +
            "<p:extLst><p:ext uri=\"preserve\"><x:data>Preserved Transition Extension</x:data></p:ext></p:extLst>" +
            "</p:transition>" +
            "</p:sld>";
    }
}
