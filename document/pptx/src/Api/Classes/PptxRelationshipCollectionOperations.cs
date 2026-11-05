using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxRelationshipCollection values.
/// </summary>
public class PptxRelationshipCollectionOperations : IPptxRelationshipCollectionOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxRelationshipCollectionOperations()
    {
    }

    /// <summary>
    /// Returns <c>true</c> when the operation has no error diagnostics.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Succeeded(PptxRelationshipCollection target)
    {
        return target.Succeeded();
    }

    /// <summary>
    /// Returns <c>true</c> when the operation contains at least one error diagnostic.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Failed(PptxRelationshipCollection target)
    {
        return target.Failed();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int Count(PptxRelationshipCollection target)
    {
        return target.Count();
    }

    /// <summary>
    /// Adds an item and returns the created model when applicable.
    /// </summary>
    /// <param name="relationship">The relationship value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Add(PptxRelationshipCollection target, PptxRelationship relationship)
    {
        target.Add(relationship);
    }

    /// <summary>
    /// Reports whether the collection contains the requested relationship identifier.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool ContainsId(PptxRelationshipCollection target, string id)
    {
        return target.ContainsId(id);
    }

    /// <summary>
    /// Returns the zero-based index of a relationship identifier, or <c>-1</c> when it is absent.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int IndexOfId(PptxRelationshipCollection target, string id)
    {
        return target.IndexOfId(id);
    }
}
