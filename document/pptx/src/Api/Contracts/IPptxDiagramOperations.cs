using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxDiagramOperations"/>.
/// </summary>
public interface IPptxDiagramOperations
{
    /// <summary>
    /// Extracts text from the diagram without removing it.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractText(PptxDiagram target);
    /// <summary>
    /// Returns the parts without changing the diagram.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxPart> GetParts(PptxDiagram target);
    /// <summary>
    /// Resolves a diagram relationship identifier to its internal package part.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <returns>The related internal part, or <c>null</c> when the relationship cannot be resolved.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart? ResolvePart(PptxDiagram target, string relationshipId);
}
