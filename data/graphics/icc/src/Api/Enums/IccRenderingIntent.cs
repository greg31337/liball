namespace icc;

/// <summary>
/// Identifies an ICC rendering intent.
/// </summary>
public enum IccRenderingIntent
{
    /// <summary>
    /// Identifies the perceptual rendering intent.
    /// </summary>
    Perceptual = 0,

    /// <summary>
    /// Identifies the media-relative colorimetric rendering intent.
    /// </summary>
    MediaRelativeColorimetric = 1,

    /// <summary>
    /// Identifies the saturation rendering intent.
    /// </summary>
    Saturation = 2,

    /// <summary>
    /// Identifies the ICC-absolute colorimetric rendering intent.
    /// </summary>
    IccAbsoluteColorimetric = 3
}
