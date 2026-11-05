using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxPointOperations"/>.
/// </summary>
public interface IPptxPointOperations
{
    /// <summary>
    /// Returns the rectangle spanning this point and the supplied endpoint.
    /// </summary>
    /// <param name="end">The end value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds BoundsTo(PptxPoint target, PptxPoint end);
}
