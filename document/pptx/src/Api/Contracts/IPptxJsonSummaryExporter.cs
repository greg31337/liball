using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxJsonSummaryExporter"/>.
/// </summary>
public interface IPptxJsonSummaryExporter
{
    /// <summary>
    /// Exports package in a structural, non-rendered representation.
    /// </summary>
    /// <param name="package">The package value to use.</param>
    /// <returns>The resulting text.</returns>
    string ExportPackage(PptxPackage package);
    /// <summary>
    /// Exports presentation in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExportPresentation(PptxPresentation presentation);
}
