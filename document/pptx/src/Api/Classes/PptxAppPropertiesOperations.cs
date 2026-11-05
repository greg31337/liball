using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxAppProperties values.
/// </summary>
public class PptxAppPropertiesOperations : IPptxAppPropertiesOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxAppPropertiesOperations()
    {
    }

    /// <summary>
    /// Serializes the current app properties and writes it to the requested destination.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void Save(PptxAppProperties target)
    {
        target.Save();
    }
}
