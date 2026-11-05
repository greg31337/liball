namespace pptx;

/// <summary>
/// Represents image file data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxImageFile
{
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Contains the binary payload bytes. Callers should treat the array as mutable data.
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Stores the info value associated with this object.
    /// </summary>
    public PptxImageInfo Info;
    /// <summary>
    /// Stores whether the relationship points to an external image.
    /// </summary>
    public bool Linked;
    /// <summary>
    /// Stores the relationship target. For embedded images this is the resolved part name.
    /// </summary>
    public string Target;

    /// <summary>
    /// Initializes a new image file with safe defaults or the supplied values.
    /// </summary>
    public PptxImageFile()
    {
        RelationshipId = "";
        PartName = "";
        Bytes = new byte[0];
        Info = new PptxImageInfo();
        Linked = false;
        Target = "";
    }
}
