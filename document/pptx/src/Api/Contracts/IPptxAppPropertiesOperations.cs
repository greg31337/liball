using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxAppPropertiesOperations"/>.
/// </summary>
public interface IPptxAppPropertiesOperations
{
    /// <summary>
    /// Serializes the current app properties and writes it to the requested destination.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void Save(PptxAppProperties target);
}
