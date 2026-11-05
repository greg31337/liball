namespace pptx;

/// <summary>
/// Represents placeholder data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxPlaceholder
{
    /// <summary>
    /// Stores the source part name associated with this object.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// Stores the shape id used to identify or connect related presentation data.
    /// </summary>
    public string ShapeId;
    /// <summary>
    /// Stores the shape name associated with this object.
    /// </summary>
    public string ShapeName;
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the index value associated with this object.
    /// </summary>
    public int Index;
    /// <summary>
    /// Stores the size associated with this object.
    /// </summary>
    public string Size;
    /// <summary>
    /// Stores the text associated with this object.
    /// </summary>
    public string Text;
    /// <summary>
    /// Indicates whether the inherited setting is enabled.
    /// </summary>
    public bool Inherited;
    /// <summary>
    /// Stores the inherited from part name associated with this object.
    /// </summary>
    public string InheritedFromPartName;
    /// <summary>
    /// Stores the inherited shape id used to identify or connect related presentation data.
    /// </summary>
    public string InheritedShapeId;
    /// <summary>
    /// Stores the inherited shape name associated with this object.
    /// </summary>
    public string InheritedShapeName;

    /// <summary>
    /// Initializes a new placeholder with safe defaults or the supplied values.
    /// </summary>
    public PptxPlaceholder()
    {
        SourcePartName = "";
        ShapeId = "";
        ShapeName = "";
        Type = "";
        Index = -1;
        Size = "";
        Text = "";
        Inherited = false;
        InheritedFromPartName = "";
        InheritedShapeId = "";
        InheritedShapeName = "";
    }
}

