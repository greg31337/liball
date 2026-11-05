using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxModernComment values.
/// </summary>
public class PptxModernCommentOperations : IPptxModernCommentOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxModernCommentOperations()
    {
    }

    /// <summary>
    /// Extracts comment text from the modern comment without removing it.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string ExtractCommentText(PptxModernComment target)
    {
        return target.ExtractCommentText();
    }
}
