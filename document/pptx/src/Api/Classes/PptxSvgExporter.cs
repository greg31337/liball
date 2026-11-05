using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxSvgExporter values.
/// </summary>
public class PptxSvgExporter : IPptxSvgExporter
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxSvgExporter()
    {
    }

    /// <summary>
    /// Exports presentation in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportPresentation(PptxPresentation presentation)
    {
        return PptxSvgExporterCore.ExportPresentation(presentation);
    }

    /// <summary>
    /// Exports slide in a structural, non-rendered representation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportSlide(PptxSlide slide)
    {
        return PptxSvgExporterCore.ExportSlide(slide);
    }

    /// <summary>
    /// Returns the fidelity limitations without changing the svg exporter.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string GetFidelityLimitations()
    {
        return PptxSvgExporterCore.GetFidelityLimitations();
    }
}
