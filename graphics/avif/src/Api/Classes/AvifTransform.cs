namespace avif;

/// <summary>
/// Collects clean-aperture, rotation, mirror, pixel-aspect, and Exif transforms.
/// </summary>
public class AvifTransform : IAvifTransform
{
    AvifCrop? cleanAperture;
    AvifPixelAspectRatio? pixelAspectRatio;
    int quarterTurnsCounterClockwise;
    int mirrorAxis;
    int exifOrientation;

    /// <summary>Creates an identity transform with square pixels.</summary>
    public AvifTransform()
    {
        cleanAperture = null;
        pixelAspectRatio = null;
        quarterTurnsCounterClockwise = -1;
        mirrorAxis = -1;
        exifOrientation = 0;
    }

    internal static AvifTransform CreateCopy(AvifTransform source)
    {
        AvifTransform result = new AvifTransform();
        result.cleanAperture = source.cleanAperture == null
            ? null
            : source.cleanAperture.CreateCopy();
        result.pixelAspectRatio = source.pixelAspectRatio == null
            ? null
            : source.pixelAspectRatio.CreateCopy();
        result.quarterTurnsCounterClockwise = source.quarterTurnsCounterClockwise;
        result.mirrorAxis = source.mirrorAxis;
        result.exifOrientation = source.exifOrientation;
        return result;
    }

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
    public AvifResultCode SetCleanAperture(
        uint widthNumerator,
        uint widthDenominator,
        uint heightNumerator,
        uint heightDenominator,
        int horizontalOffsetNumerator,
        uint horizontalOffsetDenominator,
        int verticalOffsetNumerator,
        uint verticalOffsetDenominator)
    {
        if (widthNumerator == 0
            || widthDenominator == 0
            || heightNumerator == 0
            || heightDenominator == 0
            || horizontalOffsetDenominator == 0
            || verticalOffsetDenominator == 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        cleanAperture = new AvifCrop(
            widthNumerator,
            widthDenominator,
            heightNumerator,
            heightDenominator,
            horizontalOffsetNumerator,
            horizontalOffsetDenominator,
            verticalOffsetNumerator,
            verticalOffsetDenominator);
        return AvifResultCode.Success;
    }

    /// <summary>Clears the clean-aperture crop.</summary>
    public void ClearCleanAperture()
    {
        cleanAperture = null;
    }

    /// <summary>Sets the counter-clockwise item rotation.</summary>
    /// <param name="quarterTurns">The number of quarter turns, from zero through three.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetRotation(int quarterTurns)
    {
        if (quarterTurns < 0 || quarterTurns > 3)
        {
            return AvifResultCode.InvalidArgument;
        }

        quarterTurnsCounterClockwise = quarterTurns;
        return AvifResultCode.Success;
    }

    /// <summary>Clears the item rotation.</summary>
    public void ClearRotation()
    {
        quarterTurnsCounterClockwise = -1;
    }

    /// <summary>Sets the item mirror axis.</summary>
    /// <param name="axis">Zero exchanges top and bottom; one exchanges left and right.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetMirror(int axis)
    {
        if (axis < 0 || axis > 1)
        {
            return AvifResultCode.InvalidArgument;
        }

        mirrorAxis = axis;
        return AvifResultCode.Success;
    }

    /// <summary>Clears the item mirror.</summary>
    public void ClearMirror()
    {
        mirrorAxis = -1;
    }

    /// <summary>Sets the encoded pixel aspect ratio.</summary>
    /// <param name="horizontalSpacing">The positive horizontal spacing.</param>
    /// <param name="verticalSpacing">The positive vertical spacing.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetPixelAspectRatio(
        uint horizontalSpacing,
        uint verticalSpacing)
    {
        if (horizontalSpacing == 0 || verticalSpacing == 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        pixelAspectRatio = new AvifPixelAspectRatio(
            horizontalSpacing,
            verticalSpacing);
        return AvifResultCode.Success;
    }

    /// <summary>Clears explicit pixel-aspect metadata.</summary>
    public void ClearPixelAspectRatio()
    {
        pixelAspectRatio = null;
    }

    /// <summary>Sets a TIFF Exif orientation to apply after item transforms.</summary>
    /// <param name="orientation">The TIFF value from one through eight.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetExifOrientation(int orientation)
    {
        if (orientation < 1 || orientation > 8)
        {
            return AvifResultCode.InvalidArgument;
        }

        exifOrientation = orientation;
        return AvifResultCode.Success;
    }

    /// <summary>Clears the retained Exif orientation.</summary>
    public void ClearExifOrientation()
    {
        exifOrientation = 0;
    }

    /// <summary>Reconciles an Exif orientation against equivalent item transforms.</summary>
    /// <param name="exif">The parsed Exif metadata.</param>
    /// <returns>Success, an Exif error, or nonconforming data for conflicting transforms.</returns>
    public AvifResultCode ReconcileExifOrientation(AvifExif exif)
    {
        if (exif == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (exif.GetCode() != AvifResultCode.Success)
        {
            return exif.GetCode();
        }

        int orientation = exif.GetOrientation();
        if (orientation == 0)
        {
            exifOrientation = 0;
            return AvifResultCode.Success;
        }

        int itemOrientation = GetItemOrientation();
        if (itemOrientation == 1 || orientation == 1)
        {
            exifOrientation = orientation;
            return AvifResultCode.Success;
        }

        if (orientation != itemOrientation)
        {
            return AvifResultCode.NonConformingData;
        }

        exifOrientation = 1;
        return AvifResultCode.Success;
    }

    /// <summary>Reports whether a clean-aperture crop is present.</summary>
    /// <returns><c>true</c> when a crop is present.</returns>
    public bool HasCleanAperture()
    {
        return cleanAperture != null;
    }

    /// <summary>Returns a copy of the clean-aperture crop.</summary>
    /// <returns>The crop, or null when absent.</returns>
    public AvifCrop? GetCleanAperture()
    {
        return cleanAperture == null ? null : cleanAperture.CreateCopy();
    }

    /// <summary>Reports whether an item rotation property is present.</summary>
    /// <returns><c>true</c> when rotation is present.</returns>
    public bool HasRotation()
    {
        return quarterTurnsCounterClockwise >= 0;
    }

    /// <summary>Returns the item rotation in counter-clockwise quarter turns.</summary>
    /// <returns>Zero through three, or minus one when absent.</returns>
    public int GetRotationQuarterTurnsCounterClockwise()
    {
        return quarterTurnsCounterClockwise;
    }

    /// <summary>Reports whether an item mirror property is present.</summary>
    /// <returns><c>true</c> when mirror is present.</returns>
    public bool HasMirror()
    {
        return mirrorAxis >= 0;
    }

    /// <summary>Returns the item mirror axis.</summary>
    /// <returns>Zero or one, or minus one when absent.</returns>
    public int GetMirrorAxis()
    {
        return mirrorAxis;
    }

    /// <summary>Reports whether explicit pixel-aspect metadata is present.</summary>
    /// <returns><c>true</c> when pixel-aspect metadata is present.</returns>
    public bool HasPixelAspectRatio()
    {
        return pixelAspectRatio != null;
    }

    /// <summary>Returns the encoded pixel aspect ratio.</summary>
    /// <returns>A ratio copy, or square pixels when no property is present.</returns>
    public AvifPixelAspectRatio GetPixelAspectRatio()
    {
        return pixelAspectRatio == null
            ? new AvifPixelAspectRatio(1, 1)
            : pixelAspectRatio.CreateCopy();
    }

    /// <summary>Returns the pixel aspect ratio after selected axis-exchanging transforms.</summary>
    /// <param name="options">The active rendering options.</param>
    /// <returns>The rendered pixel aspect ratio, or null for invalid options.</returns>
    public AvifPixelAspectRatio? GetRenderedPixelAspectRatio(
        AvifRenderOptions? options)
    {
        if (options == null)
        {
            return null;
        }

        bool exchanged = options.ApplyRotation
            && quarterTurnsCounterClockwise >= 0
            && (quarterTurnsCounterClockwise & 1) != 0;
        if (options.ApplyExifOrientation
            && exifOrientation >= 5
            && exifOrientation <= 8)
        {
            exchanged = !exchanged;
        }

        AvifPixelAspectRatio ratio = GetPixelAspectRatio();
        return exchanged ? ratio.CreateInverse() : ratio;
    }

    /// <summary>Returns the retained TIFF Exif orientation.</summary>
    /// <returns>One through eight, or zero when absent.</returns>
    public int GetExifOrientation()
    {
        return exifOrientation;
    }

    /// <summary>Applies selected transforms to packed pixels in normative order.</summary>
    /// <param name="source">The source packed pixels.</param>
    /// <param name="options">The transform-selection and resource options.</param>
    /// <returns>A transformed owning buffer or a buffer carrying an error code.</returns>
    public AvifPixelBuffer Apply(
        AvifPixelBuffer source,
        AvifRenderOptions options)
    {
        return ImageTransformer.Transform(source, this, options);
    }

    internal AvifCrop? GetCleanApertureValue()
    {
        return cleanAperture;
    }

    internal int GetRotationValue()
    {
        return quarterTurnsCounterClockwise;
    }

    internal int GetMirrorValue()
    {
        return mirrorAxis;
    }

    internal int GetExifOrientationValue()
    {
        return exifOrientation;
    }

    internal AvifPixelAspectRatio? GetPixelAspectRatioValue()
    {
        return pixelAspectRatio;
    }

    internal AvifResultCode Merge(AvifTransform candidate)
    {
        if (candidate == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (cleanAperture != null
            && candidate.cleanAperture != null
            && !cleanAperture.ValuesEqual(candidate.cleanAperture)
            || pixelAspectRatio != null
                && candidate.pixelAspectRatio != null
                && !pixelAspectRatio.ValuesEqual(candidate.pixelAspectRatio)
            || quarterTurnsCounterClockwise >= 0
                && candidate.quarterTurnsCounterClockwise >= 0
                && quarterTurnsCounterClockwise
                    != candidate.quarterTurnsCounterClockwise
            || mirrorAxis >= 0
                && candidate.mirrorAxis >= 0
                && mirrorAxis != candidate.mirrorAxis)
        {
            return AvifResultCode.NonConformingData;
        }

        if (cleanAperture == null && candidate.cleanAperture != null)
        {
            cleanAperture = candidate.cleanAperture.CreateCopy();
        }

        if (pixelAspectRatio == null && candidate.pixelAspectRatio != null)
        {
            pixelAspectRatio = candidate.pixelAspectRatio.CreateCopy();
        }

        if (quarterTurnsCounterClockwise < 0
            && candidate.quarterTurnsCounterClockwise >= 0)
        {
            quarterTurnsCounterClockwise = candidate.quarterTurnsCounterClockwise;
        }

        if (mirrorAxis < 0 && candidate.mirrorAxis >= 0)
        {
            mirrorAxis = candidate.mirrorAxis;
        }

        return AvifResultCode.Success;
    }

    internal static AvifTransform FromProperty(ParsedItemProperty property)
    {
        AvifTransform transform = new AvifTransform();
        if (property == null)
        {
            return transform;
        }

        ParsedCleanAperture? crop = property.GetCleanAperture();
        if (crop != null)
        {
            transform.SetCleanAperture(
                crop.GetWidthNumerator(),
                crop.GetWidthDenominator(),
                crop.GetHeightNumerator(),
                crop.GetHeightDenominator(),
                crop.GetHorizontalOffsetNumerator(),
                crop.GetHorizontalOffsetDenominator(),
                crop.GetVerticalOffsetNumerator(),
                crop.GetVerticalOffsetDenominator());
        }

        ParsedPixelAspectRatio? ratio = property.GetPixelAspectRatio();
        if (ratio != null)
        {
            transform.SetPixelAspectRatio(
                ratio.GetHorizontalSpacing(),
                ratio.GetVerticalSpacing());
        }

        ParsedImageRotation? rotation = property.GetImageRotation();
        if (rotation != null)
        {
            transform.SetRotation(rotation.GetQuarterTurnsCounterClockwise());
        }

        ParsedImageMirror? mirror = property.GetImageMirror();
        if (mirror != null)
        {
            transform.SetMirror(mirror.GetAxis());
        }

        return transform;
    }

    int GetItemOrientation()
    {
        int rotation = quarterTurnsCounterClockwise < 0
            ? 0
            : quarterTurnsCounterClockwise;
        if (mirrorAxis < 0)
        {
            if (rotation == 1)
            {
                return 8;
            }
            if (rotation == 2)
            {
                return 3;
            }
            if (rotation == 3)
            {
                return 6;
            }
            return 1;
        }

        if (rotation == 0)
        {
            return mirrorAxis == 1 ? 2 : 4;
        }
        if (rotation == 1)
        {
            return mirrorAxis == 1 ? 7 : 5;
        }
        if (rotation == 2)
        {
            return mirrorAxis == 1 ? 4 : 2;
        }
        return mirrorAxis == 1 ? 5 : 7;
    }
}
