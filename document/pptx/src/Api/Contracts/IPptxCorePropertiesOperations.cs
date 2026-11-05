using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxCorePropertiesOperations"/>.
/// </summary>
public interface IPptxCorePropertiesOperations
{
    /// <summary>
    /// Serializes the current core properties and writes it to the requested destination.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void Save(PptxCoreProperties target);
}
