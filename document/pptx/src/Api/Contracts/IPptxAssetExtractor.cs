using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxAssetExtractor"/>.
/// </summary>
public interface IPptxAssetExtractor
{
    /// <summary>
    /// Extracts presentation images from the asset extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    PptxAssetExtractionResult ExtractPresentationImages(PptxPresentation presentation, string directoryPath);
    /// <summary>
    /// Extracts slide images from the asset extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    PptxAssetExtractionResult ExtractSlideImages(PptxPresentation presentation, PptxSlide slide, string directoryPath);
    /// <summary>
    /// Extracts presentation media from the asset extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    PptxAssetExtractionResult ExtractPresentationMedia(PptxPresentation presentation, string directoryPath);
    /// <summary>
    /// Extracts slide media from the asset extractor without removing it.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="directoryPath">The destination directory. It is created when necessary.</param>
    /// <returns>The resulting asset extraction result value.</returns>
    PptxAssetExtractionResult ExtractSlideMedia(PptxPresentation presentation, PptxSlide slide, string directoryPath);
}
