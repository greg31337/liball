using System.Collections.Generic;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PublicMergeSplitCompletionTests
{
    [Test]
    public void MergeDocumentIntoPresentation_ImportsInOrderAndLeavesSourceIndependent()
    {
        PptxDocumentFactory factory = new PptxDocumentFactory();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxDocument destinationDocument = factory.CreatePresentation();
        PptxDocument sourceDocument = factory.CreatePresentation();
        PptxPresentation destination = destinationDocument.LoadPresentation();
        PptxPresentation source = sourceDocument.LoadPresentation();
        presentations.AddSlide(source);
        presentations.InsertSlide(source, 1);
        presentations.MoveSlide(source, 2, 1);

        int imported = new PptxMerger().MergeDocumentIntoPresentation(destination, sourceDocument);
        PptxDocument reopened = factory.OpenBytes(destinationDocument.SaveBytes());

        Assert.That(imported, Is.EqualTo(3));
        Assert.That(presentations.SlideCount(destination), Is.EqualTo(4));
        Assert.That(presentations.SlideCount(source), Is.EqualTo(3));
        Assert.That(presentations.SlideCount(reopened.LoadPresentation()), Is.EqualTo(4));
        Assert.That(reopened.Validate().Succeeded(), Is.True, reopened.Validate().FormatDiagnostics());

        presentations.RemoveSlideAt(source, 0);
        Assert.That(presentations.SlideCount(source), Is.EqualTo(2));
        Assert.That(presentations.SlideCount(destination), Is.EqualTo(4));
    }

    [Test]
    public void MergeAndSplit_PreserveHiddenSlideStateContentAndValidity()
    {
        PptxDocumentFactory factory = new PptxDocumentFactory();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxSlideOperations slides = new PptxSlideOperations();
        PptxDocument sourceDocument = factory.CreatePresentation();
        PptxPresentation source = sourceDocument.LoadPresentation();
        slides.AddTextBox(source.GetSlide(0), "Hidden workflow content", new PptxBounds(0L, 0L, 914400L, 914400L));
        presentations.SetSlideHidden(source, 0, true);
        PptxDocument preparedSource = factory.OpenBytes(sourceDocument.SaveBytes());

        PptxDocument destinationDocument = factory.CreatePresentation();
        PptxPresentation destination = destinationDocument.LoadPresentation();
        int imported = new PptxMerger().MergeDocumentIntoPresentation(destination, preparedSource);
        PptxDocument mergedDocument = factory.OpenBytes(destinationDocument.SaveBytes());
        PptxPresentation merged = mergedDocument.LoadPresentation();

        List<PptxDocument> splitDocuments = new PptxSplitter().SplitDocument(preparedSource);
        PptxDocument splitDocument = factory.OpenBytes(splitDocuments[0].SaveBytes());
        PptxPresentation split = splitDocument.LoadPresentation();

        Assert.That(imported, Is.EqualTo(1));
        Assert.That(merged.GetSlide(1).Hidden, Is.True);
        Assert.That(presentations.ExtractSlideText(merged, 1), Does.Contain("Hidden workflow content"));
        Assert.That(mergedDocument.Validate().Succeeded(), Is.True, mergedDocument.Validate().FormatDiagnostics());
        Assert.That(split.GetSlide(0).Hidden, Is.True);
        Assert.That(presentations.ExtractSlideText(split, 0), Does.Contain("Hidden workflow content"));
        Assert.That(splitDocument.Validate().Succeeded(), Is.True, splitDocument.Validate().FormatDiagnostics());
    }

    [Test]
    public void SplitDocumentAndRange_ReturnDetachedValidDocumentsWithoutChangingSource()
    {
        PptxDocumentFactory factory = new PptxDocumentFactory();
        PptxPresentationOperations presentations = new PptxPresentationOperations();
        PptxDocument sourceDocument = factory.CreatePresentation();
        PptxPresentation source = sourceDocument.LoadPresentation();
        presentations.AddSlide(source);
        presentations.AddSlide(source);
        PptxSplitter splitter = new PptxSplitter();

        List<PptxDocument> individual = splitter.SplitDocument(sourceDocument);
        PptxDocument rangeDocument = splitter.SplitDocumentRange(sourceDocument, 1, 2);

        Assert.That(individual, Has.Count.EqualTo(3));
        Assert.That(presentations.SlideCount(source), Is.EqualTo(3));
        for (int index = 0; index < individual.Count; index++)
        {
            Assert.That(presentations.SlideCount(individual[index].LoadPresentation()), Is.EqualTo(1));
            Assert.That(individual[index].Validate().Succeeded(), Is.True, individual[index].Validate().FormatDiagnostics());
        }

        Assert.That(presentations.SlideCount(rangeDocument.LoadPresentation()), Is.EqualTo(2));
        Assert.That(rangeDocument.Validate().Succeeded(), Is.True, rangeDocument.Validate().FormatDiagnostics());

        presentations.RemoveSlideAt(source, 2);
        Assert.That(presentations.SlideCount(source), Is.EqualTo(2));
        Assert.That(presentations.SlideCount(individual[2].LoadPresentation()), Is.EqualTo(1));
        Assert.That(presentations.SlideCount(rangeDocument.LoadPresentation()), Is.EqualTo(2));
    }

    [Test]
    public void DocumentBasedMergeAndSplit_ReportTheirPublicNullParameterName()
    {
        PptxPresentation destination = new PptxDocumentFactory().CreatePresentation().LoadPresentation();

        ArgumentNullException mergeException = Assert.Throws<ArgumentNullException>(() => new PptxMerger().MergeDocumentIntoPresentation(destination, null!))!;
        ArgumentNullException splitException = Assert.Throws<ArgumentNullException>(() => new PptxSplitter().SplitDocument(null!))!;
        ArgumentNullException rangeException = Assert.Throws<ArgumentNullException>(() => new PptxSplitter().SplitDocumentRange(null!, 0, 0))!;

        Assert.That(mergeException.ParamName, Is.EqualTo("sourceDocument"));
        Assert.That(splitException.ParamName, Is.EqualTo("sourceDocument"));
        Assert.That(rangeException.ParamName, Is.EqualTo("sourceDocument"));
    }
}
