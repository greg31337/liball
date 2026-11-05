namespace tiff;

/// <summary>Specifies the supported orientation values used by TIFF data.</summary>
public enum TiffOrientation
{
    /// <summary>Specifies unknown.</summary>
    Unknown = 0,
    /// <summary>Specifies top left.</summary>
    TopLeft = 1,
    /// <summary>Specifies top right.</summary>
    TopRight = 2,
    /// <summary>Specifies bottom right.</summary>
    BottomRight = 3,
    /// <summary>Specifies bottom left.</summary>
    BottomLeft = 4,
    /// <summary>Specifies left top.</summary>
    LeftTop = 5,
    /// <summary>Specifies right top.</summary>
    RightTop = 6,
    /// <summary>Specifies right bottom.</summary>
    RightBottom = 7,
    /// <summary>Specifies left bottom.</summary>
    LeftBottom = 8
}
