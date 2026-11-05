using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTextExtractor"/>.
/// </summary>
public interface IPptxTextExtractor
{
    /// <summary>
    /// Extracts presentation text from the text extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExtractPresentationText(PptxPresentation presentation);
    /// <summary>
    /// Extracts slide text from the text extractor without removing it.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExtractSlideText(PptxSlide slide);
    /// <summary>
    /// Extracts notes text from the text extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExtractNotesText(PptxPresentation presentation, PptxSlide slide);
    /// <summary>
    /// Extracts related parts text from the text extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="relationshipType">The Open Packaging Convention relationship type URI.</param>
    /// <returns>The resulting text.</returns>
    string ExtractRelatedPartsText(PptxPresentation presentation, PptxSlide slide, string relationshipType);
}
