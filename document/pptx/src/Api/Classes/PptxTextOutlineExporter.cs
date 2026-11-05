using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTextOutlineExporter values.
/// </summary>
public class PptxTextOutlineExporter : IPptxTextOutlineExporter
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTextOutlineExporter()
    {
    }

    /// <summary>
    /// Exports presentation text in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportPresentationText(PptxPresentation presentation)
    {
        return PptxTextOutlineExporterCore.ExportPresentationText(presentation);
    }

    /// <summary>
    /// Exports slide text in a structural, non-rendered representation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportSlideText(PptxSlide slide)
    {
        return PptxTextOutlineExporterCore.ExportSlideText(slide);
    }

    /// <summary>
    /// Exports presentation outline in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportPresentationOutline(PptxPresentation presentation)
    {
        return PptxTextOutlineExporterCore.ExportPresentationOutline(presentation);
    }

    /// <summary>
    /// Exports slide outline in a structural, non-rendered representation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportSlideOutline(PptxSlide slide)
    {
        return PptxTextOutlineExporterCore.ExportSlideOutline(slide);
    }

    /// <summary>
    /// Returns the presentation outline entries without changing the text outline exporter.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    public List<PptxOutlineEntry> GetPresentationOutlineEntries(PptxPresentation presentation)
    {
        return PptxTextOutlineExporterCore.GetPresentationOutlineEntries(presentation);
    }

    /// <summary>
    /// Returns the slide outline entries without changing the text outline exporter.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    public List<PptxOutlineEntry> GetSlideOutlineEntries(PptxSlide slide)
    {
        return PptxTextOutlineExporterCore.GetSlideOutlineEntries(slide);
    }

    /// <summary>
    /// Returns the slide outline entries without changing the text outline exporter.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <param name="slideIndex">The zero-based slide index.</param>
    /// <returns>The resulting collection in presentation or package order.</returns>
    public List<PptxOutlineEntry> GetSlideOutlineEntriesAtIndex(PptxSlide slide, int slideIndex)
    {
        return PptxTextOutlineExporterCore.GetSlideOutlineEntries(slide, slideIndex);
    }

    /// <summary>
    /// Returns the bullet prefix without changing the text outline exporter.
    /// </summary>
    /// <param name="entry">The entry value to use.</param>
    /// <returns>The resulting text.</returns>
    public string GetBulletPrefix(PptxOutlineEntry entry)
    {
        return PptxTextOutlineExporterCore.GetBulletPrefix(entry);
    }
}
