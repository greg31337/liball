using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxRelationshipOperations"/>.
/// </summary>
public interface IPptxRelationshipOperations
{
    /// <summary>
    /// Reports whether the relationship is external.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsExternal(PptxRelationship target);
    /// <summary>
    /// Reports whether the relationship is internal.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsInternal(PptxRelationship target);
}
