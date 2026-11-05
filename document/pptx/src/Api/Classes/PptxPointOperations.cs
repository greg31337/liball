using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxPoint values.
/// </summary>
public class PptxPointOperations : IPptxPointOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxPointOperations()
    {
    }

    /// <summary>
    /// Returns the rectangle spanning this point and the supplied endpoint.
    /// </summary>
    /// <param name="end">The end value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxBounds BoundsTo(PptxPoint target, PptxPoint end)
    {
        return target.BoundsTo(end);
    }
}
