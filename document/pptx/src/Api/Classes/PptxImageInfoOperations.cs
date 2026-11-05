using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxImageInfo values.
/// </summary>
public class PptxImageInfoOperations : IPptxImageInfoOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxImageInfoOperations()
    {
    }

    /// <summary>
    /// Reports whether the image info contains or enables dimensions.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasDimensions(PptxImageInfo target)
    {
        return target.HasDimensions();
    }
}
