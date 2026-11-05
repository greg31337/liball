using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxDiagram values.
/// </summary>
public class PptxDiagramOperations : IPptxDiagramOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxDiagramOperations()
    {
    }

    /// <summary>
    /// Extracts text from the diagram without removing it.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractText(PptxDiagram target)
    {
        return target.ExtractText();
    }

    /// <summary>
    /// Returns the parts without changing the diagram.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxPart> GetParts(PptxDiagram target)
    {
        return target.GetParts();
    }

    /// <summary>
    /// Resolves a diagram relationship identifier to its internal package part.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <returns>The related internal part, or <c>null</c> when the relationship cannot be resolved.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart? ResolvePart(PptxDiagram target, string relationshipId)
    {
        return target.ResolvePart(relationshipId);
    }
}
