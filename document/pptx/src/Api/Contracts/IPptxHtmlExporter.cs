using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxHtmlExporter"/>.
/// </summary>
public interface IPptxHtmlExporter
{
    /// <summary>
    /// Exports presentation in a structural, non-rendered representation.
    /// </summary>
    /// <param name="presentation">The presentation to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExportPresentation(PptxPresentation presentation);
    /// <summary>
    /// Exports slide in a structural, non-rendered representation.
    /// </summary>
    /// <param name="slide">The slide to inspect or modify.</param>
    /// <returns>The resulting text.</returns>
    string ExportSlide(PptxSlide slide);
}
