using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxImageInfoOperations"/>.
/// </summary>
public interface IPptxImageInfoOperations
{
    /// <summary>
    /// Reports whether the image info contains or enables dimensions.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasDimensions(PptxImageInfo target);
}
