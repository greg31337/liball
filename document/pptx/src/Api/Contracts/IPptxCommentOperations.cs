using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxCommentOperations"/>.
/// </summary>
public interface IPptxCommentOperations
{
    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetText(PptxComment target, string text);
    /// <summary>
    /// Updates the position and the underlying presentation data.
    /// </summary>
    /// <param name="x">The x value to use.</param>
    /// <param name="y">The y value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetPosition(PptxComment target, long x, long y);
    /// <summary>
    /// Removes the delete and updates dependent presentation data.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    void Delete(PptxComment target);
}
