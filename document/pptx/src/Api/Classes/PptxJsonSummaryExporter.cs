using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxJsonSummaryExporter values.
/// </summary>
public class PptxJsonSummaryExporter : IPptxJsonSummaryExporter
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxJsonSummaryExporter()
    {
    }

    /// <summary>
    /// Exports package in a structural, non-rendered representation.
    /// </summary>
    /// <param name="package">The package value to use.</param>
    /// <returns>The resulting text.</returns>
    public string ExportPackage(PptxPackage package)
    {
        return PptxJsonSummaryExporterCore.ExportPackage(package);
    }

    /// <summary>
    /// Exports presentation in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    public string ExportPresentation(PptxPresentation presentation)
    {
        return PptxJsonSummaryExporterCore.ExportPresentation(presentation);
    }
}
