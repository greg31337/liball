using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTextOutlineExporter"/>.
/// </summary>
public interface IPptxTextOutlineExporter
{
    /// <summary>
    /// Exports presentation text in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExportPresentationText(PptxPresentation presentation);
    /// <summary>
    /// Exports slide text in a structural, non-rendered representation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExportSlideText(PptxSlide slide);
    /// <summary>
    /// Exports presentation outline in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExportPresentationOutline(PptxPresentation presentation);
    /// <summary>
    /// Exports slide outline in a structural, non-rendered representation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExportSlideOutline(PptxSlide slide);
    /// <summary>
    /// Returns the presentation outline entries without changing the text outline exporter.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    List<PptxOutlineEntry> GetPresentationOutlineEntries(PptxPresentation presentation);
    /// <summary>
    /// Returns the slide outline entries without changing the text outline exporter.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    List<PptxOutlineEntry> GetSlideOutlineEntries(PptxSlide slide);
    /// <summary>
    /// Returns the slide outline entries without changing the text outline exporter.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    List<PptxOutlineEntry> GetSlideOutlineEntriesAtIndex(PptxSlide slide, int slideIndex);
    /// <summary>
    /// Returns the bullet prefix without changing the text outline exporter.
    /// </summary>
    /// <param name="entry">The entry value to use.</param>
    /// <returns>The resulting text.</returns>
    string GetBulletPrefix(PptxOutlineEntry entry);
}
