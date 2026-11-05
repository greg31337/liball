namespace pptx;

internal static class PptxMergerCore
{
    internal static int Merge(PptxPresentation destination, PptxPresentation source)
    {
        if (destination == null)
        {
            throw new System.ArgumentNullException("destination");
        }

        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        int count = source.SlideCount();
        for (int index = 0; index < count; index++)
        {
            destination.ImportSlide(source, index);
        }

        return count;
    }
    internal static int Merge(PptxPresentation destination, PptxDocument sourceDocument)
    {
        return Merge(destination, (IPptxPresentationSource)sourceDocument);
    }
    internal static int Merge(PptxPresentation destination, IPptxPresentationSource source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        return Merge(destination, source.LoadPresentation());
    }
    internal static PptxDocument Merge(PptxDocument first, PptxDocument second)
    {
        return Merge(first, (IPptxPresentationSource)second);
    }
    internal static PptxDocument Merge(PptxDocument first, IPptxPresentationSource second)
    {
        if (first == null)
        {
            throw new System.ArgumentNullException("first");
        }

        if (second == null)
        {
            throw new System.ArgumentNullException("second");
        }

        PptxDocument result = first.CloneDocument();
        PptxPresentation destination = result.LoadPresentation();
        Merge(destination, second.LoadPresentation());
        return result;
    }
}

