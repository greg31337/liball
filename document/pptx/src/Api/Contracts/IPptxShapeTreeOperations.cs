using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxShapeTreeOperations"/>.
/// </summary>
public interface IPptxShapeTreeOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int Count(PptxShapeTree target);
    /// <summary>
    /// Returns the top-level shape at the requested zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape Get(PptxShapeTree target, int index);
    /// <summary>
    /// Finds the item with the requested identifier; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape? FindById(PptxShapeTree target, int id);
    /// <summary>
    /// Finds the item with the requested name; returns <c>null</c> when no match exists.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>The matching shape, or <c>null</c> when no match exists.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape? FindByName(PptxShapeTree target, string name);
    /// <summary>
    /// Returns the number of top-level and nested shapes in the tree.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int CountAll(PptxShapeTree target);
    /// <summary>
    /// Returns a flattened list containing top-level and nested shapes in drawing order.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxShape> GetAllShapes(PptxShapeTree target);
    /// <summary>
    /// Adds the shape and returns the created model when applicable.
    /// </summary>
    /// <param name="shapeType">The shape type value to use.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddShape(PptxShapeTree target, string shapeType, PptxBounds bounds);
    /// <summary>
    /// Adds the auto shape and returns the created model when applicable.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry value to use.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddAutoShape(PptxShapeTree target, string presetGeometry, PptxBounds bounds);
    /// <summary>
    /// Adds the connector and returns the created model when applicable.
    /// </summary>
    /// <param name="connectorType">The connector type value to use.</param>
    /// <param name="start">The start value to use.</param>
    /// <param name="end">The end value to use.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddConnector(PptxShapeTree target, string connectorType, PptxPoint start, PptxPoint end);
    /// <summary>
    /// Adds the freeform and returns the created model when applicable.
    /// </summary>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="points">The points value to use.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddFreeform(PptxShapeTree target, PptxBounds bounds, List<PptxPoint> points);
    /// <summary>
    /// Adds the picture and returns the created model when applicable.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddPicture(PptxShapeTree target, string relationshipId, PptxBounds bounds);
    /// <summary>
    /// Adds the table and returns the created model when applicable.
    /// </summary>
    /// <param name="rowCount">The number of rows. The value must be positive where a new table or range is created.</param>
    /// <param name="columnCount">The number of columns. The value must be positive where a new table or range is created.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddTable(PptxShapeTree target, int rowCount, int columnCount, PptxBounds bounds);
    /// <summary>
    /// Adds the chart and returns the created model when applicable.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddChart(PptxShapeTree target, string relationshipId, PptxBounds bounds);
    /// <summary>
    /// Adds an empty group shape using parent and child coordinate bounds.
    /// </summary>
    /// <param name="bounds">The group bounds in its parent coordinate space.</param>
    /// <param name="childBounds">The coordinate space used by group children.</param>
    /// <returns>The created group shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddGroup(PptxShapeTree target, PptxBounds bounds, PptxBounds childBounds);
    /// <summary>
    /// Adds a generic graphic frame containing caller-supplied graphic data XML.
    /// </summary>
    /// <param name="dataUri">The DrawingML graphic-data URI.</param>
    /// <param name="dataXml">The XML element stored inside graphic data, or an empty value.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The created graphic-frame shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddGraphicFrame(PptxShapeTree target, string dataUri, string dataXml, PptxBounds bounds);
    /// <summary>
    /// Adds a shape behind all other shapes and marks it as a background shape.
    /// </summary>
    /// <param name="shapeType">The preset geometry name.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <returns>The created background shape.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape AddBackgroundShape(PptxShapeTree target, string shapeType, PptxBounds bounds);
    /// <summary>
    /// Removes the at and updates dependent presentation data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="target">The data value to operate on.</param>
    void RemoveAt(PptxShapeTree target, int index);
    /// <summary>
    /// Removes the remove and updates dependent presentation data.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <param name="target">The data value to operate on.</param>
    void Remove(PptxShapeTree target, PptxShape shape);
    /// <summary>
    /// Removes the by id and updates dependent presentation data.
    /// </summary>
    /// <param name="id">The id value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool RemoveById(PptxShapeTree target, int id);
    /// <summary>
    /// Moves move while preserving its related data.
    /// </summary>
    /// <param name="fromIndex">The from index value to use.</param>
    /// <param name="toIndex">The to index value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void Move(PptxShapeTree target, int fromIndex, int toIndex);
    /// <summary>
    /// Returns a deep copy that can be modified independently of the original shape tree.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape Clone(PptxShapeTree target, int index);
    /// <summary>
    /// Returns a deep copy that can be modified independently of the original shape tree.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting shape value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxShape CloneShape(PptxShapeTree target, PptxShape shape);
    /// <summary>
    /// Deep-clones a shape into this tree and assigns unique identifiers to every cloned descendant.
    /// </summary>
    /// <param name="shape">The source shape to clone.</param>
    /// <returns>The cloned shape.</returns>
    /// <param name="target">The destination shape tree.</param>
    PptxShape CloneInto(PptxShapeTree target, PptxShape shape);
    /// <summary>
    /// Moves a top-level shape above every other shape.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    void BringToFront(PptxShapeTree target, PptxShape shape);
    /// <summary>
    /// Moves a top-level shape behind every other shape.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    void SendToBack(PptxShapeTree target, PptxShape shape);
    /// <summary>
    /// Moves a top-level shape forward by one z-order position.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    void MoveForward(PptxShapeTree target, PptxShape shape);
    /// <summary>
    /// Moves a top-level shape backward by one z-order position.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    /// <param name="target">The data value to operate on.</param>
    void MoveBackward(PptxShapeTree target, PptxShape shape);
    /// <summary>
    /// Applies the rename operation to the current shape tree.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <param name="name">The name to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void Rename(PptxShapeTree target, int index, string name);
    /// <summary>
    /// Returns the zero-based index of the shape, or <c>-1</c> when it is absent.
    /// </summary>
    /// <param name="shape">The shape to inspect or modify.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    int IndexOf(PptxShapeTree target, PptxShape shape);
}
