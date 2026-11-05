using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxHtmlExporter values.
/// </summary>
public class PptxHtmlExporter : IPptxHtmlExporter
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxHtmlExporter()
    {
    }

    /// <summary>
    /// Exports presentation in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportPresentation(PptxPresentation presentation)
    {
        return PptxHtmlExporterCore.ExportPresentation(presentation);
    }

    /// <summary>
    /// Exports slide in a structural, non-rendered representation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportSlide(PptxSlide slide)
    {
        return PptxHtmlExporterCore.ExportSlide(slide);
    }
}
