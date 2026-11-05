using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class MergeSplitTests
{
    [Test]
    public void Merge_ImportsAllSourceSlidesInOrder()
    {
        PptxPresentation destination = LoadGeneratedPresentation();
        PptxPresentation source = LoadGeneratedPresentation();
        source.AddSlide();
        source.AddSlide();

        int imported = destination.Merge(source);
        PptxPresentation reopened = SaveReopenAndLoad(destination);

        Assert.That(imported, Is.EqualTo(3));
        Assert.That(reopened.SlideCount(), Is.EqualTo(4));
        Assert.That(reopened.GetSlide(0).SlideId, Is.EqualTo(256));
        Assert.That(reopened.GetSlide(1).SlideId, Is.EqualTo(257));
        Assert.That(reopened.GetSlide(2).SlideId, Is.EqualTo(258));
        Assert.That(reopened.GetSlide(3).SlideId, Is.EqualTo(259));
        Assert.That(reopened.GetSlide(1).PartName, Is.EqualTo("/ppt/slides/slide2.xml"));
        Assert.That(reopened.GetSlide(2).PartName, Is.EqualTo("/ppt/slides/slide3.xml"));
        Assert.That(reopened.GetSlide(3).PartName, Is.EqualTo("/ppt/slides/slide4.xml"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Merge_UsesSlideImportForDependentPartCopy()
    {
        PptxPresentation destination = LoadGeneratedPresentation();
        PptxPresentation source = LoadGeneratedPresentation();
        AddChartDependency(source);
        AddExistingChartNameCollisions(destination);

        int imported = destination.Merge(source);
        PptxPresentation reopened = SaveReopenAndLoad(destination);

        Assert.That(imported, Is.EqualTo(1));
        Assert.That(reopened.Package.HasPart("/ppt/charts/chart2.xml"), Is.True);
        Assert.That(reopened.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet2.xlsx"), Is.True);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Merge_AcceptsSourceDocument()
    {
        PptxPresentation destination = LoadGeneratedPresentation();
        PptxDocument sourceDocument = PptxDocument.CreateMinimumPresentation();

        int imported = destination.MergeDocument(sourceDocument);

        Assert.That(imported, Is.EqualTo(1));
        Assert.That(destination.SlideCount(), Is.EqualTo(2));
    }

    [Test]
    public void StaticMerge_ReturnsMergedCloneWithoutMutatingInputs()
    {
        PptxDocument first = PptxDocument.CreateMinimumPresentation();
        PptxDocument second = PptxDocument.CreateMinimumPresentation();

        PptxDocument merged = PptxMergerCore.Merge(first, second);
        PptxPresentation firstPresentation = first.LoadPresentation();
        PptxPresentation secondPresentation = second.LoadPresentation();
        PptxPresentation mergedPresentation = merged.LoadPresentation();

        Assert.That(firstPresentation.SlideCount(), Is.EqualTo(1));
        Assert.That(secondPresentation.SlideCount(), Is.EqualTo(1));
        Assert.That(mergedPresentation.SlideCount(), Is.EqualTo(2));
        Assert.That(merged.Validate().Succeeded(), Is.True, merged.Validate().FormatDiagnostics());
    }

    [Test]
    public void Split_CreatesOneDocumentPerSlideWithoutMutatingSource()
    {
        PptxPresentation source = LoadGeneratedPresentation();
        source.AddSlide();
        source.AddSlide();

        List<PptxDocument> documents = source.Split();

        Assert.That(documents.Count, Is.EqualTo(3));
        Assert.That(source.SlideCount(), Is.EqualTo(3));
        for (int index = 0; index < documents.Count; index++)
        {
            PptxPresentation split = documents[index].LoadPresentation();
            Assert.That(split.SlideCount(), Is.EqualTo(1));
            Assert.That(split.GetSlide(0).SlideId, Is.EqualTo(256));
            Assert.That(documents[index].Validate().Succeeded(), Is.True, documents[index].Validate().FormatDiagnostics());
        }
    }

    [Test]
    public void SplitRange_CreatesNewDocumentWithRequestedSlideRange()
    {
        PptxPresentation source = LoadGeneratedPresentation();
        source.AddSlide();
        source.AddSlide();

        PptxDocument document = source.SplitRange(1, 2);
        PptxPresentation split = document.LoadPresentation();

        Assert.That(source.SlideCount(), Is.EqualTo(3));
        Assert.That(split.SlideCount(), Is.EqualTo(2));
        Assert.That(split.GetSlide(0).SlideId, Is.EqualTo(256));
        Assert.That(split.GetSlide(1).SlideId, Is.EqualTo(257));
        Assert.That(document.Validate().Succeeded(), Is.True, document.Validate().FormatDiagnostics());
    }

    [Test]
    public void SplitRange_PreservesSlideDependencies()
    {
        PptxPresentation source = LoadGeneratedPresentation();
        AddChartDependency(source);

        PptxDocument document = PptxSplitterCore.SplitRange(source, 0, 1);
        PptxPresentation split = document.LoadPresentation();

        Assert.That(split.SlideCount(), Is.EqualTo(1));
        Assert.That(split.Package.HasPart("/ppt/charts/chart1.xml"), Is.True);
        Assert.That(split.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"), Is.True);
        Assert.That(document.Validate().Succeeded(), Is.True, document.Validate().FormatDiagnostics());
    }

    [Test]
    public void SplitRange_RejectsInvalidRanges()
    {
        PptxPresentation source = LoadGeneratedPresentation();

        Assert.Throws<System.ArgumentOutOfRangeException>(() => source.SplitRange(-1, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => source.SplitRange(0, -1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => source.SplitRange(1, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => source.SplitRange(1, int.MaxValue));
    }

    static void AddChartDependency(PptxPresentation presentation)
    {
        AddExistingChartNameCollisions(presentation);
        presentation.Package.AddRelationship(PptxPartNames.FirstSlide, PptxRelationshipTypes.Chart, "../charts/chart1.xml", PptxRelationshipTargetMode.Internal);
        presentation.Package.AddRelationship("/ppt/charts/chart1.xml", PptxRelationshipTypes.Package, "../embeddings/Microsoft_Excel_Worksheet1.xlsx", PptxRelationshipTargetMode.Internal);
    }

    static void AddExistingChartNameCollisions(PptxPresentation presentation)
    {
        if (!presentation.Package.HasPart("/ppt/charts/chart1.xml"))
        {
            presentation.Package.AddPart(
                "/ppt/charts/chart1.xml",
                PptxContentTypes.Chart,
                Encoding.UTF8.GetBytes("<c:chartSpace xmlns:c=\"" + PptxNamespaces.Chart + "\"><c:chart/></c:chartSpace>"));
        }

        if (!presentation.Package.HasPart("/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx"))
        {
            presentation.Package.AddPart(
                "/ppt/embeddings/Microsoft_Excel_Worksheet1.xlsx",
                PptxContentTypes.Binary,
                new byte[] { 1, 2, 3 });
        }
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
