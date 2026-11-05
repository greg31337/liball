using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxCoreProperties values.
/// </summary>
public class PptxCorePropertiesOperations : IPptxCorePropertiesOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxCorePropertiesOperations()
    {
    }

    /// <summary>
    /// Serializes the current core properties and writes it to the requested destination.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void Save(PptxCoreProperties target)
    {
        target.Save();
    }
}
