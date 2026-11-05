using System.Collections.Generic;

namespace pptx;

internal static class PptxSplitterCore
{
    internal static List<PptxDocument> Split(PptxPresentation source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        List<PptxDocument> documents = new List<PptxDocument>();
        for (int index = 0; index < source.SlideCount(); index++)
        {
            documents.Add(SplitRange(source, index, 1));
        }

        return documents;
    }
    internal static List<PptxDocument> Split(PptxDocument sourceDocument)
    {
        return Split((IPptxPresentationSource)sourceDocument);
    }
    internal static List<PptxDocument> Split(IPptxPresentationSource source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        return Split(source.LoadPresentation());
    }
    internal static PptxDocument SplitRange(PptxPresentation source, int startIndex, int count)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        ValidateRange(source, startIndex, count);
        PptxDocument document = CreateEmptyPresentationDocument();
        PptxPresentation destination = document.LoadPresentation();

        for (int index = 0; index < count; index++)
        {
            destination.ImportSlide(source, startIndex + index);
        }

        return document;
    }
    internal static PptxDocument SplitRange(PptxDocument sourceDocument, int startIndex, int count)
    {
        return SplitRange((IPptxPresentationSource)sourceDocument, startIndex, count);
    }
    internal static PptxDocument SplitRange(IPptxPresentationSource source, int startIndex, int count)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        return SplitRange(source.LoadPresentation(), startIndex, count);
    }

    internal static PptxDocument CreateEmptyPresentationDocument()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxPresentation presentation = document.LoadPresentation();
        while (presentation.SlideCount() > 0)
        {
            presentation.RemoveSlideAt(0);
        }

        return document;
    }

    internal static void ValidateRange(PptxPresentation source, int startIndex, int count)
    {
        if (startIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("startIndex");
        }

        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        int slideCount = source.SlideCount();
        if (startIndex > slideCount || count > slideCount - startIndex)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }
    }
}

