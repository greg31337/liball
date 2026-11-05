namespace pptx;

internal static class PptxSlideAllocator
{
    internal const int FirstSlideId = 256;

    internal static int AllocateSlideId(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        int maximum = FirstSlideId - 1;
        for (int index = 0; index < presentation.Slides.Count; index++)
        {
            if (presentation.Slides[index].SlideId > maximum)
            {
                maximum = presentation.Slides[index].SlideId;
            }
        }

        return maximum + 1;
    }

    internal static string AllocateSlidePartName(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        int number = 1;
        string partName = "/ppt/slides/slide" + number.ToString() + ".xml";

        while (package.HasPart(partName))
        {
            number = number + 1;
            partName = "/ppt/slides/slide" + number.ToString() + ".xml";
        }

        return partName;
    }

    internal static string AllocateSlideRelationshipId(PptxPresentation presentation)
    {
        if (presentation == null)
        {
            throw new System.ArgumentNullException("presentation");
        }

        if (presentation.Part == null)
        {
            throw new System.ArgumentException("Presentation part is required.");
        }

        return presentation.Package.AllocateRelationshipId(presentation.Part.Relationships);
    }

    internal static string GetPresentationTargetForSlidePartName(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        if (normalized.StartsWith("/ppt/", System.StringComparison.Ordinal))
        {
            return normalized.Substring(5);
        }

        return PptxUriCore.ToZipEntryName(normalized);
    }

    internal static string GetSlideLayoutTargetForSlidePartName(string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        if (normalized.StartsWith("/ppt/slideLayouts/", System.StringComparison.Ordinal))
        {
            return "../slideLayouts/" + normalized.Substring(18);
        }

        return normalized;
    }
}
