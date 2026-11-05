using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxComment values.
/// </summary>
public class PptxCommentOperations : IPptxCommentOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxCommentOperations()
    {
    }

    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetText(PptxComment target, string text)
    {
        target.SetText(text);
    }

    /// <summary>
    /// Updates the position and the underlying presentation data.
    /// </summary>
    /// <param name="x">The x value to use.</param>
    /// <param name="y">The y value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetPosition(PptxComment target, long x, long y)
    {
        target.SetPosition(x, y);
    }

    /// <summary>
    /// Removes the delete and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    public void Delete(PptxComment target)
    {
        target.Delete();
    }
}
