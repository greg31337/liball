namespace pptx;

/// <summary>
/// Represents outline entry data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxOutlineEntry
{
    /// <summary>
    /// Stores the slide index value associated with this object.
    /// </summary>
    public int SlideIndex;
    /// <summary>
    /// Stores the slide part name associated with this object.
    /// </summary>
    public string SlidePartName;
    /// <summary>
    /// Stores the shape id used to identify or connect related presentation data.
    /// </summary>
    public int ShapeId;
    /// <summary>
    /// Stores the shape name associated with this object.
    /// </summary>
    public string ShapeName;
    /// <summary>
    /// Stores the level value associated with this object.
    /// </summary>
    public int Level;
    /// <summary>
    /// Indicates whether the has bullet setting is enabled.
    /// </summary>
    public bool HasBullet;
    /// <summary>
    /// Stores the bullet type associated with this object.
    /// </summary>
    public string BulletType;
    /// <summary>
    /// Stores the bullet character associated with this object.
    /// </summary>
    public string BulletCharacter;
    /// <summary>
    /// Stores the numbering type associated with this object.
    /// </summary>
    public string NumberingType;
    /// <summary>
    /// Stores the numbering start at value associated with this object.
    /// </summary>
    public int NumberingStartAt;
    /// <summary>
    /// Stores the text associated with this object.
    /// </summary>
    public string Text;

    /// <summary>
    /// Initializes a new outline entry with safe defaults or the supplied values.
    /// </summary>
    public PptxOutlineEntry()
    {
        SlideIndex = -1;
        SlidePartName = "";
        ShapeId = 0;
        ShapeName = "";
        Level = 0;
        HasBullet = false;
        BulletType = PptxBullet.None;
        BulletCharacter = "";
        NumberingType = "";
        NumberingStartAt = 0;
        Text = "";
    }
}

