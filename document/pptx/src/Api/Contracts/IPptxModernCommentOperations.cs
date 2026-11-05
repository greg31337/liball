using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxModernCommentOperations"/>.
/// </summary>
public interface IPptxModernCommentOperations
{
    /// <summary>
    /// Extracts comment text from the modern comment without removing it.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string ExtractCommentText(PptxModernComment target);
}
