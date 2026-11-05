namespace pptx;

/// <summary>
/// Represents image info data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxImageInfo
{
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the content type associated with this object.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Stores the extension associated with this object.
    /// </summary>
    public string Extension;
    /// <summary>
    /// Stores the width pixels value associated with this object.
    /// </summary>
    public int WidthPixels;
    /// <summary>
    /// Stores the height pixels value associated with this object.
    /// </summary>
    public int HeightPixels;

    /// <summary>
    /// Initializes a new image info with safe defaults or the supplied values.
    /// </summary>
    public PptxImageInfo()
    {
        Type = PptxImageType.Unknown;
        ContentType = PptxContentTypes.Binary;
        Extension = "";
        WidthPixels = 0;
        HeightPixels = 0;
    }
    internal bool HasDimensions()
    {
        if (WidthPixels > 0 && HeightPixels > 0)
        {
            return true;
        }

        return false;
    }
}

