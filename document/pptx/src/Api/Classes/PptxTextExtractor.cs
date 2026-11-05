using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTextExtractor values.
/// </summary>
public class PptxTextExtractor : IPptxTextExtractor
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTextExtractor()
    {
    }

    /// <summary>
    /// Extracts presentation text from the text extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExtractPresentationText(PptxPresentation presentation)
    {
        return PptxTextExtractorCore.ExtractPresentationText(presentation);
    }

    /// <summary>
    /// Extracts slide text from the text extractor without removing it.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExtractSlideText(PptxSlide slide)
    {
        return PptxTextExtractorCore.ExtractSlideText(slide);
    }

    /// <summary>
    /// Extracts notes text from the text extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExtractNotesText(PptxPresentation presentation, PptxSlide slide)
    {
        return PptxTextExtractorCore.ExtractNotesText(presentation, slide);
    }

    /// <summary>
    /// Extracts related parts text from the text extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="relationshipType">The Open Packaging Convention relationship type URI.</param>
    /// <returns>The resulting text.</returns>
    public string ExtractRelatedPartsText(PptxPresentation presentation, PptxSlide slide, string relationshipType)
    {
        return PptxTextExtractorCore.ExtractRelatedPartsText(presentation, slide, relationshipType);
    }
}
