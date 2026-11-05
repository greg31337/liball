using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfLabelSheet"/>.
/// </summary>
public interface IPdfLabelSheet
{
    /// <summary>
    /// Adds a label to this label sheet.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the object is not in a state that permits this operation.</exception>
    void AddLabel(string text);

    /// <summary>
    /// Builds a label sheet from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    PdfDocument Build();
}
