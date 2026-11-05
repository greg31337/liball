using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfCitation"/>.
/// </summary>
public interface IPdfCitation
{
    /// <summary>
    /// Formats the result for use in generated content.
    /// </summary>
    /// <returns>The resulting text.</returns>
    string Format();
}
