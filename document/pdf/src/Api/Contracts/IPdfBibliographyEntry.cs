using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfBibliographyEntry"/>.
/// </summary>
public interface IPdfBibliographyEntry
{
    /// <summary>
    /// Formats the result for use in generated content.
    /// </summary>
    /// <returns>The resulting text.</returns>
    string Format();
}
