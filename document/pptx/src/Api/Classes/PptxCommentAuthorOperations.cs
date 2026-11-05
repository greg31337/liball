using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxCommentAuthor values.
/// </summary>
public class PptxCommentAuthorOperations : IPptxCommentAuthorOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxCommentAuthorOperations()
    {
    }

    /// <summary>
    /// Updates the name and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetName(PptxCommentAuthor target, string name)
    {
        target.SetName(name);
    }

    /// <summary>
    /// Updates the initials and the underlying presentation data.
    /// </summary>
    /// <param name="initials">The initials value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetInitials(PptxCommentAuthor target, string initials)
    {
        target.SetInitials(initials);
    }

    /// <summary>
    /// Updates the last index and the underlying presentation data.
    /// </summary>
    /// <param name="lastIndex">The last index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetLastIndex(PptxCommentAuthor target, int lastIndex)
    {
        target.SetLastIndex(lastIndex);
    }
    /// <summary>
    /// Updates the color index and the underlying presentation data.
    /// </summary>
    /// <param name="colorIndex">The non-negative color index to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetColorIndex(PptxCommentAuthor target, int colorIndex)
    {
        target.SetColorIndex(colorIndex);
    }
    /// <summary>
    /// Removes an unreferenced comment author and its empty author part when applicable.
    /// </summary>
    /// <param name="target">The comment author to remove.</param>
    /// <returns><c>true</c> when the author was removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a comment still references the author.</exception>
    public bool Remove(PptxCommentAuthor target)
    {
        return target.Remove();
    }
}
