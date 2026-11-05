namespace pptx;

/// <summary>
/// Represents the package-level document thumbnail.
/// </summary>
public class PptxThumbnail
{
    /// <summary>Stores the root thumbnail relationship identifier.</summary>
    public string RelationshipId;
    /// <summary>Stores the normalized thumbnail part name.</summary>
    public string PartName;
    /// <summary>Stores the thumbnail MIME content type.</summary>
    public string ContentType;
    /// <summary>Contains a detached copy of the thumbnail bytes.</summary>
    public byte[] Bytes;
    /// <summary>Stores detected image format and dimension information.</summary>
    public PptxImageInfo Info;

    /// <summary>
    /// Initializes an empty document thumbnail.
    /// </summary>
    public PptxThumbnail()
    {
        RelationshipId = "";
        PartName = "";
        ContentType = "";
        Bytes = new byte[0];
        Info = new PptxImageInfo();
    }
}
