using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxNotesSlideOperations"/>.
/// </summary>
public interface IPptxNotesSlideOperations
{
    /// <summary>
    /// Returns the text without changing the notes slide.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetText(PptxNotesSlide target);
    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetText(PptxNotesSlide target, string text);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ShapeCount(PptxNotesSlide target);
    /// <summary>
    /// Returns the shape names without changing the notes slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<string> GetShapeNames(PptxNotesSlide target);
    /// <summary>
    /// Returns the shape name without changing the notes slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetShapeName(PptxNotesSlide target, int index);
    /// <summary>
    /// Returns common part and display metadata without changing the notes slide.
    /// </summary>
    /// <param name="target">The notes slide to inspect.</param>
    /// <returns>The metadata snapshot.</returns>
    PptxPartMetadata GetMetadata(PptxNotesSlide target);
    /// <summary>
    /// Returns the editable notes shape tree.
    /// </summary>
    /// <param name="target">The notes slide to inspect.</param>
    /// <returns>The shape-tree snapshot.</returns>
    PptxPartShapeTree GetShapeTree(PptxNotesSlide target);
    /// <summary>
    /// Removes the notes slide and its relationship from the owning slide.
    /// </summary>
    /// <param name="target">The notes slide to remove.</param>
    /// <returns><c>true</c> when the notes slide was removed; otherwise, <c>false</c>.</returns>
    bool Remove(PptxNotesSlide target);
}
