using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxShapeTree values.
/// </summary>
public class PptxShapeTreeOperations : IPptxShapeTreeOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxShapeTreeOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int Count(PptxShapeTree target)
    {
        return target.Count();
    }

    /// <summary>
    /// Returns the top-level shape at the requested zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape Get(PptxShapeTree target, int index)
    {
        return target.Get(index);
    }

    /// <summary>
    /// Finds the item with the requested identifier; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape? FindById(PptxShapeTree target, int id)
    {
        return target.FindById(id);
    }

    /// <summary>
    /// Finds the item with the requested name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape? FindByName(PptxShapeTree target, string name)
    {
        return target.FindByName(name);
    }

    /// <summary>
    /// Returns the number of top-level and nested shapes in the tree.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int CountAll(PptxShapeTree target)
    {
        return target.CountAll();
    }

    /// <summary>
    /// Returns a flattened list containing top-level and nested shapes in drawing order.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxShape> GetAllShapes(PptxShapeTree target)
    {
        return target.GetAllShapes();
    }

    /// <summary>
    /// Adds the shape and returns the created model when applicable.
    /// </summary>
    /// <param name="shapeType">The shape type value to use.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddShape(PptxShapeTree target, string shapeType, PptxBounds bounds)
    {
        return target.AddShape(shapeType, bounds);
    }

    /// <summary>
    /// Adds the auto shape and returns the created model when applicable.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry value to use.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddAutoShape(PptxShapeTree target, string presetGeometry, PptxBounds bounds)
    {
        return target.AddAutoShape(presetGeometry, bounds);
    }

    /// <summary>
    /// Adds the connector and returns the created model when applicable.
    /// </summary>
    /// <param name="connectorType">The connector type value to use.</param>
    /// <param name="start">The start value to use.</param>
    /// <param name="end">The end value to use.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddConnector(PptxShapeTree target, string connectorType, PptxPoint start, PptxPoint end)
    {
        return target.AddConnector(connectorType, start, end);
    }

    /// <summary>
    /// Adds the freeform and returns the created model when applicable.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="points">The points value to use.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddFreeform(PptxShapeTree target, PptxBounds bounds, List<PptxPoint> points)
    {
        return target.AddFreeform(bounds, points);
    }

    /// <summary>
    /// Adds the picture and returns the created model when applicable.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddPicture(PptxShapeTree target, string relationshipId, PptxBounds bounds)
    {
        return target.AddPicture(relationshipId, bounds);
    }

    /// <summary>
    /// Adds the table and returns the created model when applicable.
    /// </summary>
    /// <param name="rowCount">The number of rows. The value must be positive where a new table or range is created.</param>
    /// <param name="columnCount">The number of columns. The value must be positive where a new table or range is created.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddTable(PptxShapeTree target, int rowCount, int columnCount, PptxBounds bounds)
    {
        return target.AddTable(rowCount, columnCount, bounds);
    }

    /// <summary>
    /// Adds the chart and returns the created model when applicable.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddChart(PptxShapeTree target, string relationshipId, PptxBounds bounds)
    {
        return target.AddChart(relationshipId, bounds);
    }

    /// <summary>
    /// Adds an empty group shape using parent and child coordinate bounds.
    /// </summary>
    /// <param name="bounds">The group bounds in its parent coordinate space.</param>
    /// <param name="childBounds">The coordinate space used by group children.</param>
    /// <returns>The created group shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddGroup(PptxShapeTree target, PptxBounds bounds, PptxBounds childBounds)
    {
        return target.AddGroup(bounds, childBounds);
    }

    /// <summary>
    /// Adds a generic graphic frame containing caller-supplied graphic data XML.
    /// </summary>
    /// <param name="dataUri">The DrawingML graphic-data URI.</param>
    /// <param name="dataXml">The XML element stored inside graphic data, or an empty value.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The created graphic-frame shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddGraphicFrame(PptxShapeTree target, string dataUri, string dataXml, PptxBounds bounds)
    {
        return target.AddGraphicFrame(dataUri, dataXml, bounds);
    }

    /// <summary>
    /// Adds a shape behind all other shapes and marks it as a background shape.
    /// </summary>
    /// <param name="shapeType">The preset geometry name.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The created background shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape AddBackgroundShape(PptxShapeTree target, string shapeType, PptxBounds bounds)
    {
        return target.AddBackgroundShape(shapeType, bounds);
    }

    /// <summary>
    /// Removes the at and updates dependent presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="target">The data value to operate on.</param>
    public void RemoveAt(PptxShapeTree target, int index)
    {
        target.RemoveAt(index);
    }

    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Remove(PptxShapeTree target, PptxShape shape)
    {
        target.Remove(shape);
    }

    /// <summary>
    /// Removes the by id and updates dependent presentation data.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool RemoveById(PptxShapeTree target, int id)
    {
        return target.RemoveById(id);
    }

    /// <summary>
    /// Moves move while preserving its related data.
    /// </summary>
    /// <param name="fromIndex">The from index value to use.</param>
    /// <param name="toIndex">The to index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Move(PptxShapeTree target, int fromIndex, int toIndex)
    {
        target.Move(fromIndex, toIndex);
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original shape tree.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape Clone(PptxShapeTree target, int index)
    {
        return target.Clone(index);
    }

    /// <summary>
    /// Returns a deep copy that can be modified independently of the original shape tree.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxShape CloneShape(PptxShapeTree target, PptxShape shape)
    {
        return target.CloneShape(shape);
    }

    /// <summary>
    /// Deep-clones a shape into this tree and assigns unique identifiers to every cloned descendant.
    /// </summary>
    /// <param name="shape">The source shape to clone.</param>
    /// <returns>The cloned shape.</returns>
    /// <param name="target">The destination shape tree.</param>
    public PptxShape CloneInto(PptxShapeTree target, PptxShape shape)
    {
        return target.CloneInto(shape);
    }

    /// <summary>
    /// Moves a top-level shape above every other shape.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    public void BringToFront(PptxShapeTree target, PptxShape shape)
    {
        target.BringToFront(shape);
    }

    /// <summary>
    /// Moves a top-level shape behind every other shape.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SendToBack(PptxShapeTree target, PptxShape shape)
    {
        target.SendToBack(shape);
    }

    /// <summary>
    /// Moves a top-level shape forward by one z-order position.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    public void MoveForward(PptxShapeTree target, PptxShape shape)
    {
        target.MoveForward(shape);
    }

    /// <summary>
    /// Moves a top-level shape backward by one z-order position.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    public void MoveBackward(PptxShapeTree target, PptxShape shape)
    {
        target.MoveBackward(shape);
    }

    /// <summary>
    /// Applies the rename operation to the current shape tree.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Rename(PptxShapeTree target, int index, string name)
    {
        target.Rename(index, name);
    }

    /// <summary>
    /// Returns the zero-based index of the shape, or <c>-1</c> when it is absent.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int IndexOf(PptxShapeTree target, PptxShape shape)
    {
        return target.IndexOf(shape);
    }
}
