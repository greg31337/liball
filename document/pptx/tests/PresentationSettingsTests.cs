using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PresentationSettingsTests
{
    [Test]
    public void SlideSizeAndOrientation_ParseAndWriteRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();

        Assert.That(presentation.SlideWidth, Is.EqualTo(12192000L));
        Assert.That(presentation.SlideHeight, Is.EqualTo(6858000L));
        Assert.That(presentation.SlideSizeType, Is.EqualTo("wide"));
        Assert.That(presentation.GetSlideOrientation(), Is.EqualTo("landscape"));

        presentation.SetSlideSizeWithType(6858000L, 12192000L, "custom");
        PptxPresentation portrait = SaveReopenAndLoad(presentation);

        Assert.That(portrait.SlideWidth, Is.EqualTo(6858000L));
        Assert.That(portrait.SlideHeight, Is.EqualTo(12192000L));
        Assert.That(portrait.SlideSizeType, Is.EqualTo("custom"));
        Assert.That(portrait.GetSlideOrientation(), Is.EqualTo("portrait"));

        portrait.SetSlideOrientation("landscape");
        PptxPresentation landscape = SaveReopenAndLoad(portrait);

        Assert.That(landscape.SlideWidth, Is.EqualTo(12192000L));
        Assert.That(landscape.SlideHeight, Is.EqualTo(6858000L));
        Assert.That(landscape.GetSlideOrientation(), Is.EqualTo("landscape"));
        Assert.That(landscape.Package.Validate().Succeeded(), Is.True, landscape.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void HiddenSlides_SaveAndReopen()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.AddSlide();

        presentation.SetSlideHidden(1, true);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.GetSlide(0).Hidden, Is.False);
        Assert.That(reopened.GetSlide(1).Hidden, Is.True);

        reopened.SetSlideHidden(1, false);
        PptxPresentation visible = SaveReopenAndLoad(reopened);

        Assert.That(visible.GetSlide(1).Hidden, Is.False);
        Assert.That(visible.Package.Validate().Succeeded(), Is.True, visible.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Sections_SaveAndReopen()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.AddSlide();

        PptxSection section = presentation.AddSection("Intro");
        presentation.AddSlideToSection(0, presentation.GetSlide(0).SlideId);
        presentation.AddSlideToSection(0, presentation.GetSlide(1).SlideId);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(section.Id, Is.EqualTo(1));
        Assert.That(reopened.Sections.Count, Is.EqualTo(1));
        Assert.That(reopened.Sections[0].Name, Is.EqualTo("Intro"));
        Assert.That(reopened.Sections[0].SlideIds.Count, Is.EqualTo(2));
        Assert.That(reopened.Sections[0].SlideIds[0], Is.EqualTo(256));
        Assert.That(reopened.Sections[0].SlideIds[1], Is.EqualTo(257));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void CustomShows_SaveAndReopen()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.AddSlide();

        PptxCustomShow show = presentation.AddCustomShow("Main");
        presentation.AddSlideToCustomShow(0, presentation.GetSlide(1).SlideId);
        presentation.AddSlideToCustomShow(0, presentation.GetSlide(0).SlideId);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(show.Id, Is.EqualTo(1));
        Assert.That(reopened.CustomShows.Count, Is.EqualTo(1));
        Assert.That(reopened.CustomShows[0].Name, Is.EqualTo("Main"));
        Assert.That(reopened.CustomShows[0].SlideIds.Count, Is.EqualTo(2));
        Assert.That(reopened.CustomShows[0].SlideIds[0], Is.EqualTo(257));
        Assert.That(reopened.CustomShows[0].SlideIds[1], Is.EqualTo(256));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
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
