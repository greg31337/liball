namespace pptx;

/// <summary>
/// Represents media file data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxMediaFile
{
    /// <summary>
    /// Stores the slide part name associated with this object.
    /// </summary>
    public string SlidePartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Indicates whether the embedded setting is enabled.
    /// </summary>
    public bool Embedded;
    /// <summary>
    /// Indicates whether the linked setting is enabled.
    /// </summary>
    public bool Linked;
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
    /// Contains the binary payload bytes. Callers should treat the array as mutable data.
    /// </summary>
    public byte[] Bytes;
    /// <summary>
    /// Indicates whether the has poster frame setting is enabled.
    /// </summary>
    public bool HasPosterFrame;
    /// <summary>
    /// Stores the poster relationship id used to identify or connect related presentation data.
    /// </summary>
    public string PosterRelationshipId;
    /// <summary>
    /// Stores the poster part name associated with this object.
    /// </summary>
    public string PosterPartName;
    /// <summary>
    /// Stores the poster bytes value associated with this object.
    /// </summary>
    public byte[] PosterBytes;
    /// <summary>
    /// Stores the poster info value associated with this object.
    /// </summary>
    public PptxImageInfo PosterInfo;

    /// <summary>
    /// Initializes a new media file with safe defaults or the supplied values.
    /// </summary>
    public PptxMediaFile()
    {
        SlidePartName = "";
        RelationshipId = "";
        Type = PptxMediaType.Unknown;
        Embedded = false;
        Linked = false;
        PartName = "";
        Target = "";
        ContentType = "";
        Bytes = new byte[0];
        HasPosterFrame = false;
        PosterRelationshipId = "";
        PosterPartName = "";
        PosterBytes = new byte[0];
        PosterInfo = new PptxImageInfo();
    }
}

