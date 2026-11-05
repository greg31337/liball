using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxRelationship values.
/// </summary>
public class PptxRelationshipOperations : IPptxRelationshipOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxRelationshipOperations()
    {
    }

    /// <summary>
    /// Reports whether the relationship is external.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsExternal(PptxRelationship target)
    {
        return target.IsExternal();
    }

    /// <summary>
    /// Reports whether the relationship is internal.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsInternal(PptxRelationship target)
    {
        return target.IsInternal();
    }
}
