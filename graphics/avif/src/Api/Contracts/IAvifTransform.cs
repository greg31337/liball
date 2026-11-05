namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifTransform"/>.</summary>
public interface IAvifTransform
{
    /// <summary>Sets an exact clean-aperture crop.</summary>
    /// <param name="widthNumerator">The aperture-width numerator.</param>
    /// <param name="widthDenominator">The positive aperture-width denominator.</param>
    /// <param name="heightNumerator">The aperture-height numerator.</param>
    /// <param name="heightDenominator">The positive aperture-height denominator.</param>
    /// <param name="horizontalOffsetNumerator">The horizontal centre-offset numerator.</param>
    /// <param name="horizontalOffsetDenominator">The positive horizontal-offset denominator.</param>
    /// <param name="verticalOffsetNumerator">The vertical centre-offset numerator.</param>
    /// <param name="verticalOffsetDenominator">The positive vertical-offset denominator.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetCleanAperture(
        uint widthNumerator,
        uint widthDenominator,
        uint heightNumerator,
        uint heightDenominator,
        int horizontalOffsetNumerator,
        uint horizontalOffsetDenominator,
        int verticalOffsetNumerator,
        uint verticalOffsetDenominator);

    /// <summary>Clears the clean-aperture crop.</summary>
    void ClearCleanAperture();

    /// <summary>Sets the counter-clockwise item rotation.</summary>
    /// <param name="quarterTurns">The number of quarter turns, from zero through three.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetRotation(int quarterTurns);

    /// <summary>Clears the item rotation.</summary>
    void ClearRotation();

    /// <summary>Sets the item mirror axis.</summary>
    /// <param name="axis">Zero exchanges top and bottom; one exchanges left and right.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetMirror(int axis);

    /// <summary>Clears the item mirror.</summary>
    void ClearMirror();

    /// <summary>Sets the encoded pixel aspect ratio.</summary>
    /// <param name="horizontalSpacing">The positive horizontal spacing.</param>
    /// <param name="verticalSpacing">The positive vertical spacing.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetPixelAspectRatio(
        uint horizontalSpacing,
        uint verticalSpacing);

    /// <summary>Clears explicit pixel-aspect metadata.</summary>
    void ClearPixelAspectRatio();

    /// <summary>Sets a TIFF Exif orientation to apply after item transforms.</summary>
    /// <param name="orientation">The TIFF value from one through eight.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetExifOrientation(int orientation);

    /// <summary>Clears the retained Exif orientation.</summary>
    void ClearExifOrientation();

    /// <summary>Reconciles an Exif orientation against equivalent item transforms.</summary>
    /// <param name="exif">The parsed Exif metadata.</param>
    /// <returns>Success, an Exif error, or nonconforming data for conflicting transforms.</returns>
    AvifResultCode ReconcileExifOrientation(AvifExif exif);

    /// <summary>Reports whether a clean-aperture crop is present.</summary>
    /// <returns><c>true</c> when a crop is present.</returns>
    bool HasCleanAperture();

    /// <summary>Returns a copy of the clean-aperture crop.</summary>
    /// <returns>The crop, or null when absent.</returns>
    AvifCrop? GetCleanAperture();

    /// <summary>Reports whether an item rotation property is present.</summary>
    /// <returns><c>true</c> when rotation is present.</returns>
    bool HasRotation();

    /// <summary>Returns the item rotation in counter-clockwise quarter turns.</summary>
    /// <returns>Zero through three, or minus one when absent.</returns>
    int GetRotationQuarterTurnsCounterClockwise();

    /// <summary>Reports whether an item mirror property is present.</summary>
    /// <returns><c>true</c> when mirror is present.</returns>
    bool HasMirror();

    /// <summary>Returns the item mirror axis.</summary>
    /// <returns>Zero or one, or minus one when absent.</returns>
    int GetMirrorAxis();

    /// <summary>Reports whether explicit pixel-aspect metadata is present.</summary>
    /// <returns><c>true</c> when pixel-aspect metadata is present.</returns>
    bool HasPixelAspectRatio();

    /// <summary>Returns the encoded pixel aspect ratio.</summary>
    /// <returns>A ratio copy, or square pixels when no property is present.</returns>
    AvifPixelAspectRatio GetPixelAspectRatio();

    /// <summary>Returns the pixel aspect ratio after selected axis-exchanging transforms.</summary>
    /// <param name="options">The active rendering options.</param>
    /// <returns>The rendered pixel aspect ratio, or null for invalid options.</returns>
    AvifPixelAspectRatio? GetRenderedPixelAspectRatio(
        AvifRenderOptions? options);

    /// <summary>Returns the retained TIFF Exif orientation.</summary>
    /// <returns>One through eight, or zero when absent.</returns>
    int GetExifOrientation();

    /// <summary>Applies selected transforms to packed pixels in normative order.</summary>
    /// <param name="source">The source packed pixels.</param>
    /// <param name="options">The transform-selection and resource options.</param>
    /// <returns>A transformed owning buffer or a buffer carrying an error code.</returns>
    AvifPixelBuffer Apply(
        AvifPixelBuffer source,
        AvifRenderOptions options);
}
