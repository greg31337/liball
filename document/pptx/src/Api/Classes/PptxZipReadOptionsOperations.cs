using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxZipReadOptions values.
/// </summary>
public class PptxZipReadOptionsOperations : IPptxZipReadOptionsOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxZipReadOptionsOperations()
    {
    }

    /// <summary>
    /// Creates a new zip read options using the supplied values.
    /// </summary>
    /// <returns>The resulting zip read options value.</returns>
    public PptxZipReadOptions CreateDefault()
    {
        return PptxZipReadOptions.CreateDefault();
    }
}
