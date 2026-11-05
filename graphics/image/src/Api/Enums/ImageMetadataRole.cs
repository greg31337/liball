namespace image;

/// <summary>Identifies the display or relationship role of an image frame or preserved image profile.</summary>
public enum ImageMetadataRole
{
    /// <summary>The image role is unknown.</summary>
    Unknown = 0,
    /// <summary>The image is the primary display image.</summary>
    Primary = 1,
    /// <summary>The image is a page in a multi-page or multi-image source.</summary>
    Page = 2,
    /// <summary>The image is a small thumbnail representation.</summary>
    Thumbnail = 3,
    /// <summary>The image is a larger preview representation.</summary>
    Preview = 4,
    /// <summary>The image is an auxiliary representation whose purpose is not known.</summary>
    Auxiliary = 5,
    /// <summary>The image supplies auxiliary alpha samples.</summary>
    AuxiliaryAlpha = 6,
    /// <summary>The image supplies auxiliary depth or disparity samples.</summary>
    AuxiliaryDepth = 7
}
