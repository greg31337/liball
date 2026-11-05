using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxZipReadOptionsOperations"/>.
/// </summary>
public interface IPptxZipReadOptionsOperations
{
    /// <summary>
    /// Creates a new zip read options using the supplied values.
    /// </summary>
    /// <returns>The resulting zip read options value.</returns>
    PptxZipReadOptions CreateDefault();
}
