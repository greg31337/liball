using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxCommentAuthorOperations"/>.
/// </summary>
public interface IPptxCommentAuthorOperations
{
    /// <summary>
    /// Updates the name and the underlying presentation data.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetName(PptxCommentAuthor target, string name);
    /// <summary>
    /// Updates the initials and the underlying presentation data.
    /// </summary>
    /// <param name="initials">The initials value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetInitials(PptxCommentAuthor target, string initials);
    /// <summary>
    /// Updates the last index and the underlying presentation data.
    /// </summary>
    /// <param name="lastIndex">The last index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetLastIndex(PptxCommentAuthor target, int lastIndex);
    /// <summary>
    /// Updates the color index and the underlying presentation data.
    /// </summary>
    /// <param name="colorIndex">The non-negative color index to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetColorIndex(PptxCommentAuthor target, int colorIndex);
    /// <summary>
    /// Removes an unreferenced comment author and its empty author part when applicable.
    /// </summary>
    /// <param name="target">The comment author to remove.</param>
    /// <returns><c>true</c> when the author was removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentException">Thrown when a comment still references the author.</exception>
    bool Remove(PptxCommentAuthor target);
}
