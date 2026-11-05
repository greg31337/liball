using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxAssetExtractor values.
/// </summary>
public class PptxAssetExtractor : IPptxAssetExtractor
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxAssetExtractor()
    {
    }

    /// <summary>
    /// Extracts presentation images from the asset extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    public PptxAssetExtractionResult ExtractPresentationImages(PptxPresentation presentation, string directoryPath)
    {
        return PptxAssetExtractorCore.ExtractPresentationImages(presentation, directoryPath);
    }

    /// <summary>
    /// Extracts slide images from the asset extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    public PptxAssetExtractionResult ExtractSlideImages(PptxPresentation presentation, PptxSlide slide, string directoryPath)
    {
        return PptxAssetExtractorCore.ExtractSlideImages(presentation, slide, directoryPath);
    }

    /// <summary>
    /// Extracts presentation media from the asset extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    public PptxAssetExtractionResult ExtractPresentationMedia(PptxPresentation presentation, string directoryPath)
    {
        return PptxAssetExtractorCore.ExtractPresentationMedia(presentation, directoryPath);
    }

    /// <summary>
    /// Extracts slide media from the asset extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    public PptxAssetExtractionResult ExtractSlideMedia(PptxPresentation presentation, PptxSlide slide, string directoryPath)
    {
        return PptxAssetExtractorCore.ExtractSlideMedia(presentation, slide, directoryPath);
    }
}
