namespace pptx;

/// <summary>
/// Provides editing operations for master, layout, notes, and handout shape trees.
/// </summary>
public class PptxPartShapeTreeOperations : IPptxPartShapeTreeOperations
{
    /// <summary>
    /// Creates a part shape-tree operations instance.
    /// </summary>
    public PptxPartShapeTreeOperations()
    {
    }
    /// <summary>
    /// Returns the number of top-level shapes.
    /// </summary>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The number of top-level shapes.</returns>
    public int Count(PptxPartShapeTree target)
    {
        return target.Count();
    }
    /// <summary>
    /// Returns top-level shape metadata by index.
    /// </summary>
    /// <param name="index">The zero-based shape index.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The shape metadata.</returns>
    public PptxShapeMetadata Get(PptxPartShapeTree target, int index)
    {
        return target.Get(index);
    }
    /// <summary>
    /// Finds top-level or nested shape metadata by identifier.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The matching shape, or <c>null</c> when absent.</returns>
    public PptxShapeMetadata? FindById(PptxPartShapeTree target, int id)
    {
        return target.FindById(id);
    }
    /// <summary>
    /// Adds an auto shape to the part shape tree.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry name.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created shape metadata.</returns>
    public PptxShapeMetadata AddAutoShape(PptxPartShapeTree target, string presetGeometry, PptxBounds bounds)
    {
        return target.AddAutoShape(presetGeometry, bounds);
    }
    /// <summary>
    /// Adds an empty top-level group.
    /// </summary>
    /// <param name="bounds">The group bounds in its parent coordinate space.</param>
    /// <param name="childBounds">The coordinate space used by group children.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created group metadata.</returns>
    public PptxShapeMetadata AddGroup(PptxPartShapeTree target, PptxBounds bounds, PptxBounds childBounds)
    {
        return target.AddGroup(bounds, childBounds);
    }
    /// <summary>
    /// Adds a generic graphic frame to the part shape tree.
    /// </summary>
    /// <param name="dataUri">The DrawingML graphic-data URI.</param>
    /// <param name="dataXml">The XML element stored inside graphic data, or an empty value.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created frame metadata.</returns>
    public PptxShapeMetadata AddGraphicFrame(PptxPartShapeTree target, string dataUri, string dataXml, PptxBounds bounds)
    {
        return target.AddGraphicFrame(dataUri, dataXml, bounds);
    }
    /// <summary>
    /// Adds a marked background shape behind all other top-level shapes.
    /// </summary>
    /// <param name="presetGeometry">The preset geometry name.</param>
    /// <param name="bounds">The position and size in English Metric Units (EMUs).</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created background-shape metadata.</returns>
    public PptxShapeMetadata AddBackgroundShape(PptxPartShapeTree target, string presetGeometry, PptxBounds bounds)
    {
        return target.AddBackgroundShape(presetGeometry, bounds);
    }
    /// <summary>
    /// Adds an auto shape to an existing group.
    /// </summary>
    /// <param name="groupId">The group shape identifier.</param>
    /// <param name="presetGeometry">The preset geometry name.</param>
    /// <param name="bounds">The bounds in the group's child coordinate space.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created child metadata.</returns>
    public PptxShapeMetadata AddAutoShapeToGroup(PptxPartShapeTree target, int groupId, string presetGeometry, PptxBounds bounds)
    {
        return target.AddAutoShapeToGroup(groupId, presetGeometry, bounds);
    }
    /// <summary>
    /// Adds a nested group to an existing group.
    /// </summary>
    /// <param name="groupId">The parent group identifier.</param>
    /// <param name="bounds">The nested group bounds.</param>
    /// <param name="childBounds">The nested child coordinate space.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The created nested group metadata.</returns>
    public PptxShapeMetadata AddNestedGroup(PptxPartShapeTree target, int groupId, PptxBounds bounds, PptxBounds childBounds)
    {
        return target.AddNestedGroup(groupId, bounds, childBounds);
    }
    /// <summary>
    /// Deep-clones a top-level or nested shape and assigns unique descendant identifiers.
    /// </summary>
    /// <param name="id">The source shape identifier.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The cloned shape metadata.</returns>
    public PptxShapeMetadata Clone(PptxPartShapeTree target, int id)
    {
        return target.Clone(id);
    }
    /// <summary>
    /// Removes a top-level or nested shape by identifier.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns><c>true</c> when a shape was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveById(PptxPartShapeTree target, int id)
    {
        return target.RemoveById(id);
    }
    /// <summary>
    /// Moves a top-level shape to a new z-order index.
    /// </summary>
    /// <param name="fromIndex">The current zero-based index.</param>
    /// <param name="toIndex">The destination zero-based index.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Move(PptxPartShapeTree target, int fromIndex, int toIndex)
    {
        target.Move(fromIndex, toIndex);
    }
    /// <summary>
    /// Moves a direct group child to a new z-order index.
    /// </summary>
    /// <param name="groupId">The group shape identifier.</param>
    /// <param name="fromIndex">The current zero-based index.</param>
    /// <param name="toIndex">The destination zero-based index.</param>
    /// <param name="target">The data value to operate on.</param>
    public void MoveInGroup(PptxPartShapeTree target, int groupId, int fromIndex, int toIndex)
    {
        target.MoveInGroup(groupId, fromIndex, toIndex);
    }
    /// <summary>
    /// Renames a top-level or nested shape.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="name">The new shape name.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Rename(PptxPartShapeTree target, int id, string name)
    {
        target.Rename(id, name);
    }
    /// <summary>
    /// Returns a shape-level custom data value, or an empty value when absent.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="name">The custom data name.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The stored custom data value.</returns>
    public string GetCustomData(PptxPartShapeTree target, int id, string name)
    {
        return target.GetCustomData(id, name);
    }
    /// <summary>
    /// Adds or updates shape-level custom data while preserving unrelated extensions.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="name">The custom data name.</param>
    /// <param name="value">The custom data value.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetCustomData(PptxPartShapeTree target, int id, string name, string value)
    {
        target.SetCustomData(id, name, value);
    }
    /// <summary>
    /// Removes shape-level custom data while preserving unrelated extensions.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="name">The custom data name.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns><c>true</c> when an entry was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveCustomData(PptxPartShapeTree target, int id, string name)
    {
        return target.RemoveCustomData(id, name);
    }
    /// <summary>
    /// Returns preserved non-visual shape extension XML in document order.
    /// </summary>
    /// <param name="id">The shape identifier.</param>
    /// <param name="target">The data value to operate on.</param>
    /// <returns>The preserved extension XML values.</returns>
    public System.Collections.Generic.List<string> GetExtensionXml(PptxPartShapeTree target, int id)
    {
        return target.GetExtensionXml(id);
    }
}
