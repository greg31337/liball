namespace image;

/// <summary>Describes how stored image samples must be transformed for normal display.</summary>
public enum ImageOrientation
{
    /// <summary>No reliable orientation value is available.</summary>
    Unknown = 0,
    /// <summary>Display the samples without mirroring or rotation.</summary>
    Normal = 1,
    /// <summary>Mirror the samples across the vertical axis.</summary>
    MirrorHorizontal = 2,
    /// <summary>Rotate the samples by 180 degrees.</summary>
    Rotate180 = 3,
    /// <summary>Mirror the samples across the horizontal axis.</summary>
    MirrorVertical = 4,
    /// <summary>Mirror horizontally and then rotate by 270 degrees clockwise.</summary>
    MirrorHorizontalRotate270 = 5,
    /// <summary>Rotate the samples by 90 degrees clockwise.</summary>
    Rotate90 = 6,
    /// <summary>Mirror horizontally and then rotate by 90 degrees clockwise.</summary>
    MirrorHorizontalRotate90 = 7,
    /// <summary>Rotate the samples by 270 degrees clockwise.</summary>
    Rotate270 = 8
}
