using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Describes one shape in a read-only part shape-tree snapshot.
/// </summary>
public class PptxShapeMetadata
{
    /// <summary>
    /// Stores the non-visual shape id.
    /// </summary>
    public int Id;
    /// <summary>
    /// Stores the non-visual shape name.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the PresentationML element type.
    /// </summary>
    public string ElementType;
    /// <summary>
    /// Stores the placeholder type, or an empty value for a non-placeholder shape.
    /// </summary>
    public string PlaceholderType;
    /// <summary>
    /// Stores the placeholder index, or <c>-1</c> when it is not declared.
    /// </summary>
    public int PlaceholderIndex;
    /// <summary>
    /// Stores the shape text.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the preserved raw XML represented by this snapshot.
    /// </summary>
    public string RawXml;
    /// <summary>
    /// Contains nested group shapes in document order.
    /// </summary>
    public List<PptxShapeMetadata> Children;

    /// <summary>
    /// Initializes an empty shape metadata snapshot.
    /// </summary>
    public PptxShapeMetadata()
    {
        Id = 0;
        Name = "";
        ElementType = "";
        PlaceholderType = "";
        PlaceholderIndex = -1;
        Text = "";
        RawXml = "";
        Children = new List<PptxShapeMetadata>();
    }
}
