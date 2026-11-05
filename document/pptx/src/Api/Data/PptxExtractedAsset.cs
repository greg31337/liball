namespace pptx;

/// <summary>
/// Represents extracted asset data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxExtractedAsset
{
    /// <summary>
    /// Stores the kind associated with this object.
    /// </summary>
    public string Kind;
    /// <summary>
    /// Stores the slide index value associated with this object.
    /// </summary>
    public int SlideIndex;
    /// <summary>
    /// Stores the slide part name associated with this object.
    /// </summary>
    public string SlidePartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the target associated with this object.
    /// </summary>
    public string Target;
    /// <summary>
    /// Stores the content type associated with this object.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Stores the media type associated with this object.
    /// </summary>
    public string MediaType;
    /// <summary>
    /// Indicates whether the embedded setting is enabled.
    /// </summary>
    public bool Embedded;
    /// <summary>
    /// Indicates whether the linked setting is enabled.
    /// </summary>
    public bool Linked;
    /// <summary>
    /// Indicates whether the written setting is enabled.
    /// </summary>
    public bool Written;
    /// <summary>
    /// Stores the file name associated with this object.
    /// </summary>
    public string FileName;
    /// <summary>
    /// Stores the file path associated with this object.
    /// </summary>
    public string FilePath;
    /// <summary>
    /// Contains the binary payload bytes. Callers should treat the array as mutable data.
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Stores the image info value associated with this object.
    /// </summary>
    public PptxImageInfo ImageInfo;

    /// <summary>
    /// Initializes a new extracted asset with safe defaults or the supplied values.
    /// </summary>
    public PptxExtractedAsset()
    {
        Kind = "";
        SlideIndex = -1;
        SlidePartName = "";
        RelationshipId = "";
        PartName = "";
        Target = "";
        ContentType = "";
        MediaType = "";
        Embedded = false;
        Linked = false;
        Written = false;
        FileName = "";
        FilePath = "";
        Bytes = new byte[0];
        ImageInfo = new PptxImageInfo();
    }
}

