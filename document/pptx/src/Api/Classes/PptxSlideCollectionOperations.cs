using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxSlideCollection values.
/// </summary>
public class PptxSlideCollectionOperations : IPptxSlideCollectionOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxSlideCollectionOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int Count(PptxSlideCollection target)
    {
        return target.Count();
    }

    /// <summary>
    /// Returns the slide at the requested zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide Get(PptxSlideCollection target, int index)
    {
        return target.Get(index);
    }

    /// <summary>
    /// Finds the item with the requested identifier; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <returns>The matching slide, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide? FindById(PptxSlideCollection target, int slideId)
    {
        return target.FindById(slideId);
    }

    /// <summary>
    /// Adds an item and returns the created model when applicable.
    /// </summary>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide Add(PptxSlideCollection target)
    {
        return target.Add();
    }

    /// <summary>
    /// Adds an item and returns the created model when applicable.
    /// </summary>
    /// <param name="layoutName">The layout name value to use.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide AddWithLayout(PptxSlideCollection target, string layoutName)
    {
        return target.AddWithLayout(layoutName);
    }

    /// <summary>
    /// Inserts the insert at the requested position.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide Insert(PptxSlideCollection target, int index)
    {
        return target.Insert(index);
    }

    /// <summary>
    /// Inserts the insert at the requested position.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="layoutName">The layout name value to use.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide InsertWithLayoutName(PptxSlideCollection target, int index, string layoutName)
    {
        return target.InsertWithLayoutName(index, layoutName);
    }

    /// <summary>
    /// Inserts the with layout at the requested position.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="layout">The layout to use, or <c>null</c> to create a slide without a layout relationship.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide InsertWithLayout(PptxSlideCollection target, int index, PptxSlideLayout? layout)
    {
        return target.InsertWithLayout(index, layout);
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original slide collection.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide Clone(PptxSlideCollection target, int index)
    {
        return target.Clone(index);
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original slide collection.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="insertIndex">The zero-based destination index.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide CloneToIndex(PptxSlideCollection target, int index, int insertIndex)
    {
        return target.CloneToIndex(index, insertIndex);
    }

    /// <summary>
    /// Imports a source slide and its dependent parts into this collection.
    /// </summary>
    /// <param name="sourcePresentation">The source presentation value to use.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide Import(PptxSlideCollection target, PptxPresentation sourcePresentation, int sourceIndex)
    {
        return target.Import(sourcePresentation, sourceIndex);
    }

    /// <summary>
    /// Imports a source slide and its dependent parts into this collection.
    /// </summary>
    /// <param name="sourcePresentation">The source presentation value to use.</param>
    /// <param name="sourceIndex">The zero-based index in the source presentation.</param>
    /// <param name="insertIndex">The zero-based destination index.</param>
    /// <returns>The resulting slide value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxSlide ImportToIndex(PptxSlideCollection target, PptxPresentation sourcePresentation, int sourceIndex, int insertIndex)
    {
        return target.ImportToIndex(sourcePresentation, sourceIndex, insertIndex);
    }

    /// <summary>
    /// Removes the at and updates dependent presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveAt(PptxSlideCollection target, int index)
    {
        target.RemoveAt(index);
    }

    /// <summary>
    /// Removes the by id and updates dependent presentation data.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool RemoveById(PptxSlideCollection target, int slideId)
    {
        return target.RemoveById(slideId);
    }

    /// <summary>
    /// Moves move while preserving its related data.
    /// </summary>
    /// <param name="fromIndex">The from index value to use.</param>
    /// <param name="toIndex">The to index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Move(PptxSlideCollection target, int fromIndex, int toIndex)
    {
        target.Move(fromIndex, toIndex);
    }
}
