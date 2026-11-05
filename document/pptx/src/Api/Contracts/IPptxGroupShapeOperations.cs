using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxGroupShapeOperations"/>.
/// </summary>
public interface IPptxGroupShapeOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int Count(PptxGroupShape target);
    /// <summary>
    /// Returns the child shape at the requested zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape Get(PptxGroupShape target, int index);
    /// <summary>
    /// Finds the item with the requested identifier; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape? FindById(PptxGroupShape target, int id);
    /// <summary>
    /// Finds the item with the requested name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape? FindByName(PptxGroupShape target, string name);
    /// <summary>
    /// Maps a child shape's bounds into the group's parent coordinate space.
    /// </summary>
    /// <param name="child">The child value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds MapChildBounds(PptxGroupShape target, PptxShape child);
    /// <summary>
    /// Maps a point from the group's child coordinate space into its parent coordinate space.
    /// </summary>
    /// <param name="point">The child point value to use.</param>
    /// <returns>The resulting point value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPoint MapChildPoint(PptxGroupShape target, PptxPoint point);
    /// <summary>
    /// Maps a child's rotated anchor bounds into the group's parent coordinate space.
    /// </summary>
    /// <param name="child">The child value to use.</param>
    /// <returns>The resulting bounds value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxBounds MapChildAnchorBounds(PptxGroupShape target, PptxShape child);
    /// <summary>
    /// Adds an auto shape as a direct child of the group.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry name.</param>
    /// <param name="bounds">The bounds in the group's child coordinate space.</param>
    /// <returns>The created child shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddAutoShape(PptxGroupShape target, string presetGeometry, PptxBounds bounds);
    /// <summary>
    /// Adds a nested group as a direct child.
    /// </summary>
    /// <param name="bounds">The group bounds in its parent coordinate space.</param>
    /// <param name="childBounds">The coordinate space used by nested children.</param>
    /// <returns>The created nested group shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddGroup(PptxGroupShape target, PptxBounds bounds, PptxBounds childBounds);
    /// <summary>
    /// Adds a generic graphic frame as a direct child.
    /// </summary>
    /// <param name="dataUri">The DrawingML graphic-data URI.</param>
    /// <param name="dataXml">The XML element stored inside graphic data, or an empty value.</param>
    /// <param name="bounds">The bounds in the group's child coordinate space.</param>
    /// <returns>The created graphic-frame shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddGraphicFrame(PptxGroupShape target, string dataUri, string dataXml, PptxBounds bounds);
    /// <summary>
    /// Deep-clones a shape into this group with unique descendant identifiers.
    /// </summary>
    /// <param name="source">The source shape to clone.</param>
    /// <returns>The cloned child shape.</returns>
    /// <param name="target">The destination group.</param>
    PptxShape CloneShape(PptxGroupShape target, PptxShape source);
    /// <summary>
    /// Clones a direct child by index.
    /// </summary>
    /// <param name="index">The zero-based child index.</param>
    /// <returns>The cloned child shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape Clone(PptxGroupShape target, int index);
    /// <summary>
    /// Removes a direct child by index.
    /// </summary>
    /// <param name="index">The zero-based child index.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveAt(PptxGroupShape target, int index);
    /// <summary>
    /// Moves a direct child to a new z-order index.
    /// </summary>
    /// <param name="fromIndex">The current zero-based index.</param>
    /// <param name="toIndex">The destination zero-based index.</param>
    /// <param name="target">The data value to operate on.</param>
    void Move(PptxGroupShape target, int fromIndex, int toIndex);
    /// <summary>
    /// Moves a direct child above all siblings.
    /// </summary>
    /// <param name="child">The child shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    void BringToFront(PptxGroupShape target, PptxShape child);
    /// <summary>
    /// Moves a direct child behind all siblings.
    /// </summary>
    /// <param name="child">The child shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    void SendToBack(PptxGroupShape target, PptxShape child);
}
