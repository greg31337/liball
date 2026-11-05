namespace pptx;

/// <summary>
/// Describes common metadata for a notes slide, notes master, or handout master part.
/// </summary>
public class PptxPartMetadata
{
    /// <summary>
    /// Stores the package part name.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id that exposes the part.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the common slide data name.
    /// </summary>
    public string Name;
    /// <summary>
    /// Stores the root PresentationML element name.
    /// </summary>
    public string RootElementName;
    /// <summary>
    /// Stores the number of top-level shapes.
    /// </summary>
    public int ShapeCount;
    /// <summary>
    /// Stores the number of placeholder shapes at all nesting levels.
    /// </summary>
    public int PlaceholderCount;
    /// <summary>
    /// Stores the number of related themes.
    /// </summary>
    public int ThemeCount;
    /// <summary>
    /// Indicates whether master shapes are shown.
    /// </summary>
    public bool ShowMasterShapes;
    /// <summary>
    /// Indicates whether master placeholder animations are shown.
    /// </summary>
    public bool ShowMasterPlaceholderAnimations;

    /// <summary>
    /// Initializes an empty metadata snapshot.
    /// </summary>
    public PptxPartMetadata()
    {
        PartName = "";
        RelationshipId = "";
        Name = "";
        RootElementName = "";
        ShapeCount = 0;
        PlaceholderCount = 0;
        ThemeCount = 0;
        ShowMasterShapes = true;
        ShowMasterPlaceholderAnimations = true;
    }
}
