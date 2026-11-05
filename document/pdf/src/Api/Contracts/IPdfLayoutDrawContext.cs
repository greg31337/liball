using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfLayoutDrawContext"/>.
/// </summary>
public interface IPdfLayoutDrawContext
{
    /// <summary>
    /// Converts the object's current state to a layout context.
    /// </summary>
    /// <returns>The resulting layout context.</returns>
    PdfLayoutContext ToLayoutContext();
}
