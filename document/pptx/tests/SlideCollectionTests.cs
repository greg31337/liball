using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class SlideCollectionTests
{
    [Test]
    public void SlideCollection_CountGetAndFindByIdUsePresentationSlideIds()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();

        Assert.That(presentation.SlideCollection.Count(), Is.EqualTo(1));
        Assert.That(presentation.SlideCount(), Is.EqualTo(1));
        Assert.That(presentation.SlideCollection.Get(0).SlideId, Is.EqualTo(256));
        Assert.That(presentation.GetSlide(0).PartName, Is.EqualTo(PptxPartNames.FirstSlide));
        Assert.That(presentation.FindSlideById(256), Is.SameAs(presentation.GetSlide(0)));
        Assert.That(presentation.SlideCollection.FindById(999), Is.Null);
    }

    [Test]
    public void AddSlide_AppendsPartRelationshipAndPresentationSlideId()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();

        PptxSlide slide = presentation.AddSlide();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(slide.SlideId, Is.EqualTo(257));
        Assert.That(slide.PartName, Is.EqualTo("/ppt/slides/slide2.xml"));
        Assert.That(presentation.Package.HasPart("/ppt/slides/slide2.xml"), Is.True);
        Assert.That(slide.Layout, Is.Not.Null);
        Assert.That(reopened.SlideCount(), Is.EqualTo(2));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(256));
        Assert.That(reopened.GetSlide(1).SlideId, Is.EqualTo(257));
        Assert.That(reopened.GetSlide(1).PartName, Is.EqualTo("/ppt/slides/slide2.xml"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void InsertSlide_AddsSlideAtRequestedPosition()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();

        PptxSlide inserted = presentation.InsertSlide(0);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(inserted.SlideId, Is.EqualTo(257));
        Assert.That(presentation.GetSlide(0).SlideId, Is.EqualTo(257));
        Assert.That(reopened.SlideCount(), Is.EqualTo(2));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(257));
        Assert.That(reopened.GetSlide(1).SlideId, Is.EqualTo(256));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void RemoveSlideById_RemovesSlidePartRelationshipAndSlideId()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide added = presentation.AddSlide();

        bool removed = presentation.RemoveSlideById(256);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(removed, Is.True);
        Assert.That(presentation.Package.HasPart(PptxPartNames.FirstSlide), Is.False);
        Assert.That(reopened.SlideCount(), Is.EqualTo(1));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(added.SlideId));
        Assert.That(reopened.GetSlide(0).PartName, Is.EqualTo(added.PartName));
        Assert.That(reopened.FindSlideById(256), Is.Null);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void RemoveSlideById_ReturnsFalseWhenSlideIdIsMissing()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();

        bool removed = presentation.RemoveSlideById(999);

        Assert.That(removed, Is.False);
        Assert.That(presentation.SlideCount(), Is.EqualTo(1));
    }

    [Test]
    public void MoveSlide_ReordersPresentationSlideIds()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.AddSlide();
        presentation.AddSlide();

        presentation.MoveSlide(2, 0);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.SlideCount(), Is.EqualTo(3));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(258));
        Assert.That(reopened.GetSlide(1).SlideId, Is.EqualTo(256));
        Assert.That(reopened.GetSlide(2).SlideId, Is.EqualTo(257));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void RemoveAt_RemovesSlideByIndex()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.AddSlide();

        presentation.RemoveSlideAt(1);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(reopened.SlideCount(), Is.EqualTo(1));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(256));
        Assert.That(reopened.Package.HasPart("/ppt/slides/slide2.xml"), Is.False);
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
