using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxNotesSlide values.
/// </summary>
public class PptxNotesSlideOperations : IPptxNotesSlideOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxNotesSlideOperations()
    {
    }

    /// <summary>
    /// Returns the text without changing the notes slide.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetText(PptxNotesSlide target)
    {
        return target.GetText();
    }

    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetText(PptxNotesSlide target, string text)
    {
        target.SetText(text);
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ShapeCount(PptxNotesSlide target)
    {
        return target.ShapeCount();
    }

    /// <summary>
    /// Returns the shape names without changing the notes slide.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<string> GetShapeNames(PptxNotesSlide target)
    {
        return target.GetShapeNames();
    }

    /// <summary>
    /// Returns the shape name without changing the notes slide.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetShapeName(PptxNotesSlide target, int index)
    {
        return target.GetShapeName(index);
    }
    /// <summary>
    /// Returns common part and display metadata without changing the notes slide.
    /// </summary>
    /// <param name="target">The notes slide to inspect.</param>
    /// <returns>The metadata snapshot.</returns>
    public PptxPartMetadata GetMetadata(PptxNotesSlide target)
    {
        return target.GetMetadata();
    }
    /// <summary>
    /// Returns the editable notes shape tree.
    /// </summary>
    /// <param name="target">The notes slide to inspect.</param>
    /// <returns>The shape-tree snapshot.</returns>
    public PptxPartShapeTree GetShapeTree(PptxNotesSlide target)
    {
        return target.GetShapeTree();
    }
    /// <summary>
    /// Removes the notes slide and its relationship from the owning slide.
    /// </summary>
    /// <param name="target">The notes slide to remove.</param>
    /// <returns><c>true</c> when the notes slide was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(PptxNotesSlide target)
    {
        return target.Remove();
    }
}
