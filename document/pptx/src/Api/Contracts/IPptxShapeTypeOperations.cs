using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxShapeTypeOperations"/>.
/// </summary>
public interface IPptxShapeTypeOperations
{
    /// <summary>
    /// Reports whether the shape type is common preset geometry.
    /// </summary>
    /// <param name="geometry">The geometry value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsCommonPresetGeometry(string geometry);
}
