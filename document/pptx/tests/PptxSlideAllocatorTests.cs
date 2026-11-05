using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxSlideAllocatorTests
{
    [Test]
    public void AllocateSlideId_UsesMaximumExistingSlideIdPlusOne()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide firstAdded = presentation.AddSlide();
        PptxSlide secondAdded = presentation.AddSlide();

        presentation.RemoveSlideById(firstAdded.SlideId);
        int allocated = PptxSlideAllocator.AllocateSlideId(presentation);
        PptxSlide thirdAdded = presentation.AddSlide();

        Assert.That(secondAdded.SlideId, Is.EqualTo(258));
        Assert.That(allocated, Is.EqualTo(259));
        Assert.That(thirdAdded.SlideId, Is.EqualTo(259));
        Assert.That(thirdAdded.PartName, Is.EqualTo("/ppt/slides/slide2.xml"));
    }

    [Test]
    public void AllocateSlidePartName_UsesFirstFreeSlidePartNumber()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.AddPart("/ppt/slides/slide3.xml", PptxContentTypes.Slide, Encoding.UTF8.GetBytes(PptxMinimumPresentation.SlideXml()));

        string first = PptxSlideAllocator.AllocateSlidePartName(package);
        package.AddPart(first, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(PptxMinimumPresentation.SlideXml()));
        string second = PptxSlideAllocator.AllocateSlidePartName(package);

        Assert.That(first, Is.EqualTo("/ppt/slides/slide2.xml"));
        Assert.That(second, Is.EqualTo("/ppt/slides/slide4.xml"));
    }

    [Test]
    public void AllocateSlideRelationshipId_UsesFirstFreePresentationRelationshipId()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();

        string next = PptxSlideAllocator.AllocateSlideRelationshipId(presentation);
        presentation.Package.RemoveRelationship(PptxPartNames.Presentation, "rId4");
        string afterHole = PptxSlideAllocator.AllocateSlideRelationshipId(presentation);

        Assert.That(next, Is.EqualTo("rId6"));
        Assert.That(afterHole, Is.EqualTo("rId4"));
    }

    [Test]
    public void AddSlide_UsesDeterministicFirstFreeRelationshipId()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        presentation.Package.RemoveRelationship(PptxPartNames.Presentation, "rId4");

        PptxSlide slide = presentation.AddSlide();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(slide.RelationshipId, Is.EqualTo("rId4"));
        Assert.That(slide.PartName, Is.EqualTo("/ppt/slides/slide2.xml"));
        Assert.That(reopened.GetSlide(1).RelationshipId, Is.EqualTo("rId4"));
        Assert.That(reopened.GetSlide(1).SlideId, Is.EqualTo(257));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void AddRelationshipWithId_RejectsDuplicateRelationshipId()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();

        Assert.Throws<System.ArgumentException>(
            () => package.AddRelationshipWithId(PptxPartNames.Presentation, "rId2", PptxRelationshipTypes.Slide, "slides/slide2.xml", PptxRelationshipTargetMode.Internal));
    }

    [Test]
    public void TargetHelpers_ReturnDeterministicRelativeTargets()
    {
        Assert.That(
            PptxSlideAllocator.GetPresentationTargetForSlidePartName("/ppt/slides/slide12.xml"),
            Is.EqualTo("slides/slide12.xml"));
        Assert.That(
            PptxSlideAllocator.GetSlideLayoutTargetForSlidePartName("/ppt/slideLayouts/slideLayout2.xml"),
            Is.EqualTo("../slideLayouts/slideLayout2.xml"));
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
