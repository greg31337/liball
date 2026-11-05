namespace tiff;

/// <summary>Specifies the OME order of the Z, C, and T axes after the spatial X and Y axes.</summary>
public enum TiffOmeDimensionOrder
{
    /// <summary>Specifies an absent or unrecognized dimension order.</summary>
    Unknown = 0,
    /// <summary>Specifies XYZCT order.</summary>
    XYZCT = 1,
    /// <summary>Specifies XYZTC order.</summary>
    XYZTC = 2,
    /// <summary>Specifies XYCZT order.</summary>
    XYCZT = 3,
    /// <summary>Specifies XYCTZ order.</summary>
    XYCTZ = 4,
    /// <summary>Specifies XYTCZ order.</summary>
    XYTCZ = 5,
    /// <summary>Specifies XYTZC order.</summary>
    XYTZC = 6
}
