using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Supplies an editable presentation model to workflows that read presentation content.
/// </summary>
/// <remarks>Implement this interface to provide presentation content from a custom source or a test double without exposing document storage details.</remarks>
public interface IPptxPresentationSource
{
    /// <summary>
    /// Loads the presentation model supplied by this source.
    /// </summary>
    /// <returns>The editable presentation model.</returns>
    PptxPresentation LoadPresentation();
}
