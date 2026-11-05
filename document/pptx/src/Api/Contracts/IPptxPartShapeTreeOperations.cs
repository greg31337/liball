namespace pptx;

/// <summary>
/// Defines editing operations for master, layout, notes, and handout shape trees.
/// </summary>
public interface IPptxPartShapeTreeOperations
{
    /// <summary>
    /// Returns the number of top-level shapes.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The number of top-level shapes.</returns>
    int Count(PptxPartShapeTree target);
    /// <summary>
    /// Returns top-level shape metadata by index.
    /// </summary>
    /// <param name="index">The zero-based shape index.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The shape metadata.</returns>
    PptxShapeMetadata Get(PptxPartShapeTree target, int index);
    /// <summary>
    /// Finds top-level or nested shape metadata by identifier.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The matching shape, or <c>null</c> when absent.</returns>
    PptxShapeMetadata? FindById(PptxPartShapeTree target, int id);
    /// <summary>
    /// Adds an auto shape to the part shape tree.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry name.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created shape metadata.</returns>
    PptxShapeMetadata AddAutoShape(PptxPartShapeTree target, string presetGeometry, PptxBounds bounds);
    /// <summary>
    /// Adds an empty top-level group.
    /// </summary>
    /// <param name="bounds">The group bounds in its parent coordinate space.</param>
    /// <param name="childBounds">The coordinate space used by group children.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created group metadata.</returns>
    PptxShapeMetadata AddGroup(PptxPartShapeTree target, PptxBounds bounds, PptxBounds childBounds);
    /// <summary>
    /// Adds a generic graphic frame to the part shape tree.
    /// </summary>
    /// <param name="dataUri">The DrawingML graphic-data URI.</param>
    /// <param name="dataXml">The XML element stored inside graphic data, or an empty value.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created frame metadata.</returns>
    PptxShapeMetadata AddGraphicFrame(PptxPartShapeTree target, string dataUri, string dataXml, PptxBounds bounds);
    /// <summary>
    /// Adds a marked background shape behind all other top-level shapes.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry name.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created background-shape metadata.</returns>
    PptxShapeMetadata AddBackgroundShape(PptxPartShapeTree target, string presetGeometry, PptxBounds bounds);
    /// <summary>
    /// Adds an auto shape to an existing group.
    /// </summary>
    /// <param name="groupId">The group shape identifier.</param>
    /// <param name="presetGeometry">The preset geometry name.</param>
    /// <param name="bounds">The bounds in the group's child coordinate space.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created child metadata.</returns>
    PptxShapeMetadata AddAutoShapeToGroup(PptxPartShapeTree target, int groupId, string presetGeometry, PptxBounds bounds);
    /// <summary>
    /// Adds a nested group to an existing group.
    /// </summary>
    /// <param name="groupId">The parent group identifier.</param>
    /// <param name="bounds">The nested group bounds.</param>
    /// <param name="childBounds">The nested child coordinate space.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created nested group metadata.</returns>
    PptxShapeMetadata AddNestedGroup(PptxPartShapeTree target, int groupId, PptxBounds bounds, PptxBounds childBounds);
    /// <summary>
    /// Deep-clones a top-level or nested shape and assigns unique descendant identifiers.
    /// </summary>
    /// <param name="id">The source shape identifier.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The cloned shape metadata.</returns>
    PptxShapeMetadata Clone(PptxPartShapeTree target, int id);
    /// <summary>
    /// Removes a top-level or nested shape by identifier.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns><c>true</c> when a shape was removed; otherwise, <c>false</c>.</returns>
    bool RemoveById(PptxPartShapeTree target, int id);
    /// <summary>
    /// Moves a top-level shape to a new z-order index.
    /// </summary>
    /// <param name="fromIndex">The current zero-based index.</param>
    /// <param name="toIndex">The destination zero-based index.</param>
    /// <param name="target">The data value to operate on.</param>
    void Move(PptxPartShapeTree target, int fromIndex, int toIndex);
    /// <summary>
    /// Moves a direct group child to a new z-order index.
    /// </summary>
    /// <param name="groupId">The group shape identifier.</param>
    /// <param name="fromIndex">The current zero-based index.</param>
    /// <param name="toIndex">The destination zero-based index.</param>
    /// <param name="target">The data value to operate on.</param>
    void MoveInGroup(PptxPartShapeTree target, int groupId, int fromIndex, int toIndex);
    /// <summary>
    /// Renames a top-level or nested shape.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="name">The new shape name.</param>
    /// <param name="target">The data value to operate on.</param>
    void Rename(PptxPartShapeTree target, int id, string name);
    /// <summary>
    /// Returns a shape-level custom data value, or an empty value when absent.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="name">The custom data name.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The stored custom data value.</returns>
    string GetCustomData(PptxPartShapeTree target, int id, string name);
    /// <summary>
    /// Adds or updates shape-level custom data while preserving unrelated extensions.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="name">The custom data name.</param>
    /// <param name="value">The custom data value.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetCustomData(PptxPartShapeTree target, int id, string name, string value);
    /// <summary>
    /// Removes shape-level custom data while preserving unrelated extensions.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="name">The custom data name.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns><c>true</c> when an entry was removed; otherwise, <c>false</c>.</returns>
    bool RemoveCustomData(PptxPartShapeTree target, int id, string name);
    /// <summary>
    /// Returns preserved non-visual shape extension XML in document order.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The preserved extension XML values.</returns>
    System.Collections.Generic.List<string> GetExtensionXml(PptxPartShapeTree target, int id);
}
