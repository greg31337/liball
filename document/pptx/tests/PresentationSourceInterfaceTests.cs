using System.Collections.Generic;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PresentationSourceInterfaceTests
{
    [Test]
    public void PptxDocument_ImplementsPresentationSource()
    {
        IPptxPresentationSource source = PptxDocument.CreatePresentation();

        Assert.That(source.LoadPresentation().SlideCount(), Is.EqualTo(1));
    }

    [Test]
    public void PresentationWorkflows_AcceptMinimalSourceImplementation()
    {
        FakePresentationSource importSource = CreateSource();
        PptxPresentation importDestination = PptxDocument.CreatePresentation().LoadPresentation();
        PptxSlide imported = importDestination.ImportSourceSlide(importSource, 0);

        FakePresentationSource mergeSource = CreateSource();
        PptxPresentation mergeDestination = PptxDocument.CreatePresentation().LoadPresentation();
        int merged = mergeDestination.MergeSource(mergeSource);

        FakePresentationSource splitSource = CreateSource();
        List<PptxDocument> split = PptxSplitterCore.Split(splitSource);

        Assert.That(imported, Is.Not.Null);
        Assert.That(importDestination.SlideCount(), Is.EqualTo(2));
        Assert.That(importSource.LoadCount, Is.EqualTo(1));
        Assert.That(merged, Is.EqualTo(1));
        Assert.That(mergeDestination.SlideCount(), Is.EqualTo(2));
        Assert.That(mergeSource.LoadCount, Is.EqualTo(1));
        Assert.That(split.Count, Is.EqualTo(1));
        Assert.That(split[0].LoadPresentation().SlideCount(), Is.EqualTo(1));
        Assert.That(splitSource.LoadCount, Is.EqualTo(1));
    }

    [Test]
    public void StaticMerge_AcceptsInterfaceForSecondSource()
    {
        PptxDocument first = PptxDocument.CreatePresentation();
        FakePresentationSource second = CreateSource();

        PptxDocument merged = PptxMergerCore.Merge(first, second);

        Assert.That(merged.LoadPresentation().SlideCount(), Is.EqualTo(2));
        Assert.That(first.LoadPresentation().SlideCount(), Is.EqualTo(1));
        Assert.That(second.LoadCount, Is.EqualTo(1));
    }

    static FakePresentationSource CreateSource()
    {
        return new FakePresentationSource(PptxDocument.CreatePresentation().LoadPresentation());
    }

    class FakePresentationSource : IPptxPresentationSource
    {
        public PptxPresentation Presentation;
        public int LoadCount;

        public FakePresentationSource(PptxPresentation presentation)
        {
            Presentation = presentation;
            LoadCount = 0;
        }

        public PptxPresentation LoadPresentation()
        {
            LoadCount = LoadCount + 1;
            return Presentation;
        }
    }
}
