using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxShapeType values.
/// </summary>
public class PptxShapeTypeOperations : IPptxShapeTypeOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxShapeTypeOperations()
    {
    }

    /// <summary>
    /// Reports whether the shape type is common preset geometry.
    /// </summary>
    /// <param name="geometry">The geometry value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool IsCommonPresetGeometry(string geometry)
    {
        return PptxShapeTypeCore.IsCommonPresetGeometry(geometry);
    }
}
