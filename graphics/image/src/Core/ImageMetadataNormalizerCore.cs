using System;
using System.Collections.Generic;

namespace image;

/// <summary>Builds a concise set of common fields from format-specific metadata tags.</summary>
internal static class ImageMetadataNormalizerCore
{
    /// <summary>Collects dimensions, camera details, location, descriptive text, and color information into one summary.</summary>
    /// <remarks>Known tag keys are checked in precedence order. Missing or unrecognized values remain at their default values.</remarks>
    /// <param name="document">The extracted document whose frames, profiles, and tags will be inspected.</param>
    /// <returns>A new normalized summary. A null document produces an empty summary.</returns>
    public static ImageMetadataSummary BuildSummary(ImageMetadataDocument? document)
    {
        ImageMetadataSummary summary = new ImageMetadataSummary();

        if (document == null)
        {
            return summary;
        }

        FillFrameFields(summary, document);
        FillProfileFields(summary, document);
        FillColorFields(summary, document);
        FillTextFields(summary, document);
        FillRating(summary, document);
        FillOrientation(summary, document);
        FillGps(summary, document);
        FillResolution(summary, document);
        FillCommonImageFields(summary, document);
        FillAnimationFields(summary, document);
        ApplyCommonFieldsToFrames(summary, document);

        document.Summary = summary;
        document.RefreshDiagnosticStatus();
        return summary;
    }

    internal static void FillFrameFields(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null || document.Frames == null)
        {
            return;
        }

        summary.FrameCount = document.Frames.Count;

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null)
            {
                if (summary.Width == 0 && frame.Width > 0)
                {
                    summary.Width = frame.Width;
                }

                if (summary.Height == 0 && frame.Height > 0)
                {
                    summary.Height = frame.Height;
                }

                if (summary.Orientation == ImageOrientation.Unknown && IsKnownOrientation(frame.Orientation))
                {
                    summary.Orientation = frame.Orientation;
                }

                if (summary.ColorSpace == ImageColorSpaceKind.Unknown && frame.ColorSpace != ImageColorSpaceKind.Unknown)
                {
                    summary.ColorSpace = frame.ColorSpace;
                }

                if (summary.PixelAspectRatio == 0 && frame.PixelAspectRatio > 0)
                {
                    summary.PixelAspectRatio = frame.PixelAspectRatio;
                }

                if (summary.HorizontalResolution == 0 && frame.HorizontalResolution > 0)
                {
                    summary.HorizontalResolution = frame.HorizontalResolution;
                }

                if (summary.VerticalResolution == 0 && frame.VerticalResolution > 0)
                {
                    summary.VerticalResolution = frame.VerticalResolution;
                }

                if (IsEmpty(summary.ResolutionUnit) && !IsEmpty(frame.ResolutionUnit))
                {
                    summary.ResolutionUnit = frame.ResolutionUnit;
                }

                if (IsEmpty(summary.SourceColorType) && !IsEmpty(frame.SourceColorType))
                {
                    summary.SourceColorType = frame.SourceColorType;
                }

                if (frame.IsInterlaced)
                {
                    summary.IsInterlaced = true;
                }

                if (frame.IsProgressive)
                {
                    summary.IsProgressive = true;
                }

                if (frame.IsAnimated)
                {
                    summary.IsAnimated = true;
                }

                if (frame.DurationMilliseconds > 0)
                {
                    long duration = frame.DurationMilliseconds;
                    if (summary.TotalDurationMilliseconds > long.MaxValue - duration)
                    {
                        summary.TotalDurationMilliseconds = long.MaxValue;
                    }
                    else
                    {
                        summary.TotalDurationMilliseconds = summary.TotalDurationMilliseconds + duration;
                    }
                }
            }
        }
    }

    internal static void FillProfileFields(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null || document.Profiles == null)
        {
            return;
        }

        string decodedIccName = FindText(document, new string[] { "Icc.desc.Description" });
        foreach (MetadataProfile profile in document.Profiles)
        {
            if (profile != null && profile.Kind == MetadataProfileKind.Icc)
            {
                summary.HasColorProfile = true;

                if (IsEmpty(summary.ColorProfileName) && !IsEmpty(decodedIccName))
                {
                    summary.ColorProfileName = decodedIccName;
                }
                else if (IsEmpty(summary.ColorProfileName) && !IsEmpty(profile.Name))
                {
                    summary.ColorProfileName = profile.Name;
                }
            }
        }

        if (!summary.HasColorProfile && HasDeclaredColorProfile(document))
        {
            summary.HasColorProfile = true;
        }

        if (IsEmpty(summary.ColorProfileName))
        {
            summary.ColorProfileName = FindText(document, new string[] { "Png.iCCP.ProfileName" });
        }
    }

    internal static void FillColorFields(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        ImageColorSpaceKind pngCicp = PngCicpColorSpace(document);
        if (pngCicp != ImageColorSpaceKind.Unknown)
        {
            summary.ColorSpace = pngCicp;
            return;
        }

        ImageColorSpaceKind icc = IccColorSpace(document, summary.ColorProfileName);
        if (icc != ImageColorSpaceKind.Unknown)
        {
            summary.ColorSpace = icc;
            return;
        }

        ImageColorSpaceKind native = NativeColorSpace(document);
        if (native != ImageColorSpaceKind.Unknown && (summary.ColorSpace == ImageColorSpaceKind.Unknown || IsNamedColorSpace(native)))
        {
            summary.ColorSpace = native;
        }
    }

    internal static bool HasDeclaredColorProfile(ImageMetadataDocument document)
    {
        if (FindTag(document, new string[] { "Png.iCCP.ProfileName", "WebP.ICCP.ProfileByteLength", "Jpeg2000.colr.IccProfileByteLength" }) != null)
        {
            return true;
        }

        string colorType = ValueTextValue(ImageMetadataQueryCore.FindFirst(document, "Bmff.Item.ColorType"));
        if (colorType == "rICC" || colorType == "prof")
        {
            return true;
        }

        return BooleanValue(ImageMetadataQueryCore.FindFirst(document, "JpegXl.Codestream.WantIcc"));
    }

    internal static ImageColorSpaceKind IccColorSpace(ImageMetadataDocument document, string profileName)
    {
        MetadataTag? tag = ImageMetadataQueryCore.FindFirst(document, "Icc.Header.ColorSpace");
        string signature = ValueTextValue(tag);
        ImageColorSpaceKind colorSpace = IccSignatureColorSpace(signature);
        if (colorSpace != ImageColorSpaceKind.Rgb)
        {
            return colorSpace;
        }

        string name = SafeText(profileName).ToLowerInvariant();
        if (name.IndexOf("display p3", StringComparison.Ordinal) >= 0 || name == "p3") return ImageColorSpaceKind.DisplayP3;
        if (name.IndexOf("adobe rgb", StringComparison.Ordinal) >= 0) return ImageColorSpaceKind.AdobeRgb;
        if (name.IndexOf("prophoto", StringComparison.Ordinal) >= 0 || name.IndexOf("romm", StringComparison.Ordinal) >= 0) return ImageColorSpaceKind.ProPhotoRgb;
        if (name.IndexOf("wide gamut", StringComparison.Ordinal) >= 0) return ImageColorSpaceKind.WideGamutRgb;
        return colorSpace;
    }

    internal static ImageColorSpaceKind IccSignatureColorSpace(string signature)
    {
        string value = SafeText(signature).Trim();
        if (value == "RGB") return ImageColorSpaceKind.Rgb;
        if (value == "GRAY") return ImageColorSpaceKind.Gray;
        if (value == "CMYK") return ImageColorSpaceKind.Cmyk;
        if (value == "Lab") return ImageColorSpaceKind.Lab;
        if (value == "XYZ") return ImageColorSpaceKind.Xyz;
        if (value == "YCbr") return ImageColorSpaceKind.YCbCr;
        if (value == "HSV") return ImageColorSpaceKind.Hsv;
        if (value == "HLS") return ImageColorSpaceKind.Hsl;
        return ImageColorSpaceKind.Unknown;
    }

    internal static ImageColorSpaceKind NativeColorSpace(ImageMetadataDocument document)
    {
        int bmffPrimaries = IntValue(ImageMetadataQueryCore.FindFirst(document, "Bmff.Item.ColorPrimaries"));
        ImageColorSpaceKind colorSpace = CicpPrimariesColorSpace(bmffPrimaries);
        if (colorSpace == ImageColorSpaceKind.DisplayP3 || colorSpace == ImageColorSpaceKind.WideGamutRgb) return colorSpace;

        int jpegXlPrimaries = IntValue(ImageMetadataQueryCore.FindFirst(document, "JpegXl.Codestream.Primaries"));
        if (jpegXlPrimaries == 11) return ImageColorSpaceKind.DisplayP3;
        if (jpegXlPrimaries == 9) return ImageColorSpaceKind.WideGamutRgb;

        MetadataTag? jpeg2000 = ImageMetadataQueryCore.FindFirst(document, "Jpeg2000.colr.EnumeratedColorSpace");
        if (jpeg2000 != null) return Jpeg2000ColorSpace(IntValue(jpeg2000));

        if (ImageMetadataQueryCore.FindFirst(document, "Png.sRGB.RenderingIntent") != null) return ImageColorSpaceKind.Rgb;

        MetadataTag? pngType = ImageMetadataQueryCore.FindFirst(document, "Png.IHDR.ColorType");
        if (pngType != null) return PngColorSpace(IntValue(pngType));

        MetadataTag? jpegXl = ImageMetadataQueryCore.FindFirst(document, "JpegXl.Codestream.ColorSpace");
        if (jpegXl != null) return JpegXlColorSpace(IntValue(jpegXl));

        string bmffType = ValueTextValue(ImageMetadataQueryCore.FindFirst(document, "Bmff.Item.ColorType"));
        if (bmffType == "rICC" || bmffType == "prof") return ImageColorSpaceKind.Rgb;
        MetadataTag? matrix = ImageMetadataQueryCore.FindFirst(document, "Bmff.Item.MatrixCoefficients");
        if ((bmffType == "nclx" || bmffType == "nclc") && matrix != null) return IntValue(matrix) == 0 ? ImageColorSpaceKind.Rgb : ImageColorSpaceKind.YCbCr;
        return ImageColorSpaceKind.Unknown;
    }

    internal static ImageColorSpaceKind PngCicpColorSpace(ImageMetadataDocument document)
    {
        MetadataTag? primaries = ImageMetadataQueryCore.FindFirst(document, "Png.cICP.ColorPrimaries");
        MetadataTag? matrix = ImageMetadataQueryCore.FindFirst(document, "Png.cICP.MatrixCoefficients");
        if (primaries == null || matrix == null || IntValue(matrix) != 0)
        {
            return ImageColorSpaceKind.Unknown;
        }

        return CicpPrimariesColorSpace(IntValue(primaries));
    }

    internal static ImageColorSpaceKind CicpPrimariesColorSpace(int primaries)
    {
        if (primaries == 12) return ImageColorSpaceKind.DisplayP3;
        if (primaries == 9) return ImageColorSpaceKind.WideGamutRgb;
        if (primaries == 1) return ImageColorSpaceKind.Rgb;
        return ImageColorSpaceKind.Unknown;
    }

    internal static ImageColorSpaceKind PngColorSpace(int colorType)
    {
        if (colorType == 0 || colorType == 4) return ImageColorSpaceKind.Gray;
        if (colorType == 2 || colorType == 6) return ImageColorSpaceKind.Rgb;
        if (colorType == 3) return ImageColorSpaceKind.Indexed;
        return ImageColorSpaceKind.Unknown;
    }

    internal static ImageColorSpaceKind JpegXlColorSpace(int colorSpace)
    {
        if (colorSpace == 1) return ImageColorSpaceKind.Gray;
        if (colorSpace == 0 || colorSpace == 2) return ImageColorSpaceKind.Rgb;
        return ImageColorSpaceKind.Unknown;
    }

    internal static ImageColorSpaceKind Jpeg2000ColorSpace(int value)
    {
        if (value == 0 || value == 15 || value == 17) return ImageColorSpaceKind.Gray;
        if (value == 1 || value == 3 || value == 4 || value == 9 || value == 13 || value == 18 || value == 22 || value == 23 || value == 24) return ImageColorSpaceKind.YCbCr;
        if (value == 11 || value == 12) return ImageColorSpaceKind.Cmyk;
        if (value == 14 || value == 19) return ImageColorSpaceKind.Lab;
        if (value == 16 || value == 20) return ImageColorSpaceKind.Rgb;
        if (value == 21) return ImageColorSpaceKind.ProPhotoRgb;
        return ImageColorSpaceKind.Unknown;
    }

    internal static bool IsNamedColorSpace(ImageColorSpaceKind colorSpace)
    {
        return colorSpace == ImageColorSpaceKind.WideGamutRgb || colorSpace == ImageColorSpaceKind.DisplayP3 || colorSpace == ImageColorSpaceKind.AdobeRgb || colorSpace == ImageColorSpaceKind.ProPhotoRgb;
    }

    internal static string ValueTextValue(MetadataTag? tag)
    {
        if (tag == null || tag.Value == null) return "";
        return ValueText(tag.Value);
    }

    internal static bool BooleanValue(MetadataTag? tag)
    {
        return tag != null && tag.Value != null && tag.Value.Kind == MetadataValueKind.Boolean && tag.Value.BooleanValue;
    }

    internal static void FillTextFields(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        summary.CameraMake = FindText(document, MakeKeys());
        summary.CameraModel = FindText(document, ModelKeys());
        summary.LensModel = FindText(document, LensKeys());
        summary.CaptureTimeText = ResolveCaptureTime(document);
        summary.CaptureTime = MetadataDateParserCore.Parse(summary.CaptureTimeText);
        summary.Author = FindNonEmptyText(document, AuthorKeys());
        summary.Copyright = FindNonEmptyText(document, CopyrightKeys());
        summary.Title = FindNonEmptyText(document, TitleKeys());
        summary.Description = FindNonEmptyText(document, DescriptionKeys());
        summary.Software = ResolveSoftware(document);
        FillKeywords(summary, document);
    }

    internal static void FillKeywords(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        string[] keys = KeywordKeys();
        int keyIndex = 0;
        while (keyIndex < keys.Length)
        {
            List<MetadataTag> tags = ImageMetadataQueryCore.Find(document, keys[keyIndex]);
            foreach (MetadataTag tag in tags)
            {
                MetadataValue? value = tag == null ? null : tag.Value;
                if (value != null && value.Kind == MetadataValueKind.StringList && value.TextValues != null)
                {
                    foreach (string keyword in value.TextValues)
                    {
                        AddKeyword(summary, keyword);
                    }
                }
                else
                {
                    AddKeywordText(summary, ContentText(tag));
                }
            }

            keyIndex = keyIndex + 1;
        }
    }

    internal static void FillRating(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        summary.Rating = ResolveRating(document);
    }

    internal static string ResolveSoftware(ImageMetadataDocument document)
    {
        string software = FindNonEmptyText(document, SoftwareKeys());
        if (!IsEmpty(software))
        {
            return software;
        }

        software = CombineText(
            FindNonEmptyText(document, IptcSoftwareNameKeys()),
            FindNonEmptyText(document, IptcSoftwareVersionKeys()));
        if (!IsEmpty(software))
        {
            return software;
        }

        return CombineText(
            FindNonEmptyText(document, TgaSoftwareNameKeys()),
            FindNonEmptyText(document, TgaSoftwareVersionKeys()));
    }

    internal static int ResolveRating(ImageMetadataDocument document)
    {
        int rating = FindRating(document, SimpleRatingKeys(), false);
        if (rating >= -1)
        {
            return rating;
        }

        rating = FindRating(document, PercentRatingKeys(), true);
        return rating < 0 ? 0 : rating;
    }

    internal static int FindRating(ImageMetadataDocument document, string[] keys, bool percent)
    {
        if (document == null || keys == null)
        {
            return -2;
        }

        int keyIndex = 0;
        while (keyIndex < keys.Length)
        {
            List<MetadataTag> tags = ImageMetadataQueryCore.Find(document, keys[keyIndex]);
            foreach (MetadataTag tag in tags)
            {
                long value = RatingIntegerValue(tag);
                if (percent && value >= 0 && value <= 99)
                {
                    return RatingFromPercent((int)value);
                }

                if (!percent && value >= -1 && value <= 5)
                {
                    return (int)value;
                }
            }

            keyIndex = keyIndex + 1;
        }

        return -2;
    }

    internal static long RatingIntegerValue(MetadataTag tag)
    {
        if (tag == null || tag.Value == null)
        {
            return long.MinValue;
        }

        if (tag.Value.Kind == MetadataValueKind.Integer)
        {
            return tag.Value.IntegerValue;
        }

        if (tag.Value.Kind == MetadataValueKind.UnsignedInteger && tag.Value.UnsignedIntegerValue <= 9223372036854775807)
        {
            return (long)tag.Value.UnsignedIntegerValue;
        }

        return long.MinValue;
    }

    internal static int RatingFromPercent(int value)
    {
        if (value <= 0) return 0;
        if (value <= 12) return 1;
        if (value <= 37) return 2;
        if (value <= 62) return 3;
        if (value <= 87) return 4;
        return 5;
    }

    internal static void FillOrientation(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        if (IsKnownOrientation(summary.Orientation))
        {
            return;
        }

        summary.Orientation = FindOrientation(document, OrientationKeys());
    }

    internal static void FillGps(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        string latitudeReference = FindText(document, LatitudeReferenceKeys());
        string longitudeReference = FindText(document, LongitudeReferenceKeys());
        string altitudeReference = FindText(document, AltitudeReferenceKeys());
        double latitude = GpsCoordinateValue(FindTag(document, LatitudeKeys()), latitudeReference);
        double longitude = GpsCoordinateValue(FindTag(document, LongitudeKeys()), longitudeReference);
        double altitude = GpsCoordinate.ApplyAltitudeReference(DoubleValue(FindTag(document, AltitudeKeys())), altitudeReference);

        if (latitude != 0 || longitude != 0 || !IsEmpty(latitudeReference) || !IsEmpty(longitudeReference))
        {
            summary.Gps.IsValid = true;
            summary.Gps.Latitude = latitude;
            summary.Gps.Longitude = longitude;
            summary.Gps.Altitude = altitude;
            summary.Gps.LatitudeReference = latitudeReference;
            summary.Gps.LongitudeReference = longitudeReference;
            summary.Gps.AltitudeReference = altitudeReference;
        }
    }

    internal static double GpsCoordinateValue(MetadataTag? tag, string reference)
    {
        if (tag == null || tag.Value == null)
        {
            return 0;
        }

        MetadataValue value = tag.Value;
        if ((value.Kind == MetadataValueKind.RationalArray || value.Kind == MetadataValueKind.SignedRationalArray) && value.Numerators != null && value.Denominators != null && value.Numerators.Length >= 3 && value.Denominators.Length >= 3)
        {
            double degrees = RationalArrayValue(value, 0);
            double minutes = RationalArrayValue(value, 1);
            double seconds = RationalArrayValue(value, 2);
            return GpsCoordinate.ToDecimalDegrees(degrees, minutes, seconds, reference);
        }

        if (value.Kind == MetadataValueKind.StringList && value.TextValues != null && value.TextValues.Length >= 3)
        {
            double degrees = RationalTextValue(value.TextValues[0]);
            double minutes = RationalTextValue(value.TextValues[1]);
            double seconds = RationalTextValue(value.TextValues[2]);
            return GpsCoordinate.ToDecimalDegrees(degrees, minutes, seconds, reference);
        }

        double coordinate = DoubleValue(tag);
        if (GpsCoordinate.IsNegativeReference(reference))
        {
            return -Math.Abs(coordinate);
        }

        return coordinate;
    }

    internal static double RationalArrayValue(MetadataValue value, int index)
    {
        if (value == null || value.Numerators == null || value.Denominators == null || index < 0 || index >= value.Numerators.Length || index >= value.Denominators.Length || value.Denominators[index] == 0)
        {
            return 0;
        }

        return (double)value.Numerators[index] / (double)value.Denominators[index];
    }

    internal static double RationalTextValue(string text)
    {
        if (text == null)
        {
            return 0;
        }

        int separator = text.IndexOf('/');
        if (separator < 0)
        {
            return NetpbmMetadataParser.ParseDecimal(text);
        }

        double numerator = NetpbmMetadataParser.ParseDecimal(text.Substring(0, separator));
        double denominator = NetpbmMetadataParser.ParseDecimal(text.Substring(separator + 1));
        if (denominator == 0)
        {
            return 0;
        }

        return numerator / denominator;
    }

    internal static void FillResolution(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        MetadataTag? xTag = FindTag(document, XResolutionKeys());
        MetadataTag? yTag = FindTag(document, YResolutionKeys());
        string xResolution = TagText(xTag);
        string yResolution = TagText(yTag);
        string unit = FindText(document, ResolutionUnitKeys());

        if (summary.HorizontalResolution == 0)
        {
            summary.HorizontalResolution = DoubleValue(xTag);
        }

        if (summary.VerticalResolution == 0)
        {
            summary.VerticalResolution = DoubleValue(yTag);
        }

        if (IsEmpty(summary.ResolutionUnit) && !IsEmpty(unit))
        {
            summary.ResolutionUnit = unit;
        }

        if (!IsEmpty(xResolution) && !IsEmpty(yResolution))
        {
            summary.ResolutionText = xResolution + " x " + yResolution;

            if (!IsEmpty(unit))
            {
                summary.ResolutionText = summary.ResolutionText + " " + unit;
            }
        }
        else if (!IsEmpty(xResolution))
        {
            summary.ResolutionText = xResolution;
        }
    }

    internal static void FillCommonImageFields(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        if (summary.PixelAspectRatio == 0)
        {
            summary.PixelAspectRatio = DoubleValue(FindTag(document, PixelAspectRatioKeys()));
        }

        if (IsEmpty(summary.SourceColorType))
        {
            summary.SourceColorType = FindText(document, SourceColorTypeKeys());
        }

        if (!summary.IsInterlaced)
        {
            summary.IsInterlaced = BooleanValue(FindTag(document, InterlacedKeys()));
        }

        if (!summary.IsInterlaced)
        {
            summary.IsInterlaced = IntValue(ImageMetadataQueryCore.FindFirst(document, "Png.IHDR.InterlaceMethod")) == 1;
        }

        if (!summary.IsProgressive)
        {
            summary.IsProgressive = BooleanValue(FindTag(document, ProgressiveKeys()));
        }
    }

    internal static void FillAnimationFields(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null)
        {
            return;
        }

        if (!summary.IsAnimated)
        {
            summary.IsAnimated = BooleanValue(FindTag(document, AnimatedKeys()));
        }

        if (!summary.IsAnimated)
        {
            MetadataTag? frameCount = FindTag(document, AnimationFrameCountKeys());
            summary.IsAnimated = frameCount != null && IntValue(frameCount) > 0;
        }

        MetadataTag? loopCount = FindTag(document, AnimationLoopCountKeys());
        if (loopCount != null)
        {
            summary.AnimationLoopCount = IntValue(loopCount);
        }
    }

    internal static void ApplyCommonFieldsToFrames(ImageMetadataSummary summary, ImageMetadataDocument document)
    {
        if (summary == null || document == null || document.Frames == null)
        {
            return;
        }

        foreach (ImageMetadataFrame frame in document.Frames)
        {
            if (frame != null)
            {
                if (frame.PixelAspectRatio == 0) frame.PixelAspectRatio = summary.PixelAspectRatio;
                if (frame.HorizontalResolution == 0) frame.HorizontalResolution = summary.HorizontalResolution;
                if (frame.VerticalResolution == 0) frame.VerticalResolution = summary.VerticalResolution;
                if (IsEmpty(frame.ResolutionUnit)) frame.ResolutionUnit = summary.ResolutionUnit;
                if (IsEmpty(frame.SourceColorType)) frame.SourceColorType = summary.SourceColorType;
                if (summary.IsAnimated) frame.IsAnimated = true;
            }
        }
    }

    internal static MetadataTag? FindTag(ImageMetadataDocument? document, string[]? keys)
    {
        if (document == null || keys == null)
        {
            return null;
        }

        int index = 0;
        while (index < keys.Length)
        {
            MetadataTag? tag = ImageMetadataQueryCore.FindFirst(document, keys[index]);
            if (tag != null)
            {
                return tag;
            }

            index = index + 1;
        }

        return null;
    }

    internal static string FindText(ImageMetadataDocument document, string[] keys)
    {
        return TagText(FindTag(document, keys));
    }

    internal static string ResolveCaptureTime(ImageMetadataDocument document)
    {
        string text = FindNonEmptyText(document, OriginalCaptureTimeKeys());
        if (!IsEmpty(text))
        {
            return text;
        }

        text = FindNonEmptyText(document, ContentCreationTimeKeys());
        if (!IsEmpty(text))
        {
            return text;
        }

        text = CombineDateAndTime(
            FindNonEmptyText(document, IptcContentCreationDateKeys()),
            FindNonEmptyText(document, IptcContentCreationTimeKeys()));
        if (!IsEmpty(text))
        {
            return text;
        }

        text = FindNonEmptyText(document, EmbeddedCreationTimeKeys());
        if (!IsEmpty(text))
        {
            return text;
        }

        text = FindNonEmptyText(document, DigitalCreationTimeKeys());
        if (!IsEmpty(text))
        {
            return text;
        }

        text = CombineDateAndTime(
            FindNonEmptyText(document, IptcDigitalCreationDateKeys()),
            FindNonEmptyText(document, IptcDigitalCreationTimeKeys()));
        if (!IsEmpty(text))
        {
            return text;
        }

        return FindNonEmptyText(document, ModificationTimeFallbackKeys());
    }

    internal static string FindNonEmptyText(ImageMetadataDocument document, string[] keys)
    {
        if (document == null || keys == null)
        {
            return "";
        }

        int keyIndex = 0;
        while (keyIndex < keys.Length)
        {
            List<MetadataTag> tags = ImageMetadataQueryCore.Find(document, keys[keyIndex]);
            int tagIndex = 0;
            while (tagIndex < tags.Count)
            {
                string text = ContentText(tags[tagIndex]);
                if (!IsBlank(text))
                {
                    return text;
                }

                tagIndex = tagIndex + 1;
            }

            keyIndex = keyIndex + 1;
        }

        return "";
    }

    internal static string CombineDateAndTime(string date, string time)
    {
        if (IsBlank(date))
        {
            return "";
        }

        if (IsBlank(time))
        {
            return date;
        }

        return date + " " + time;
    }

    internal static string CombineText(string first, string second)
    {
        if (IsBlank(first))
        {
            return "";
        }

        if (IsBlank(second))
        {
            return first;
        }

        return first + " " + second;
    }

    internal static string TagText(MetadataTag? tag)
    {
        if (tag == null)
        {
            return "";
        }

        if (!IsEmpty(tag.DisplayValue))
        {
            return tag.DisplayValue;
        }

        return ValueText(tag.Value);
    }

    internal static string ContentText(MetadataTag? tag)
    {
        if (tag == null || tag.Value == null)
        {
            return "";
        }

        MetadataValueKind kind = tag.Value.Kind;
        if (kind == MetadataValueKind.String || kind == MetadataValueKind.DateText || kind == MetadataValueKind.XmlText || kind == MetadataValueKind.StringList)
        {
            return ValueText(tag.Value);
        }

        return TagText(tag);
    }

    internal static string ValueText(MetadataValue? value)
    {
        if (value == null)
        {
            return "";
        }

        if (value.Kind == MetadataValueKind.String || value.Kind == MetadataValueKind.DateText || value.Kind == MetadataValueKind.XmlText)
        {
            return SafeText(value.Text);
        }

        if (value.Kind == MetadataValueKind.StringList)
        {
            return JoinTextValues(value.TextValues);
        }

        return ImageMetadataFormatterCore.FormatValue(value);
    }

    internal static string JoinTextValues(string[] values)
    {
        if (values == null)
        {
            return "";
        }

        string text = "";
        int index = 0;
        while (index < values.Length)
        {
            if (!IsEmpty(values[index]))
            {
                if (!IsEmpty(text))
                {
                    text = text + ", ";
                }

                text = text + values[index];
            }

            index = index + 1;
        }

        return text;
    }

    internal static void AddKeyword(ImageMetadataSummary summary, string keyword)
    {
        if (summary == null || IsBlank(keyword))
        {
            return;
        }

        string normalized = keyword.Trim();

        foreach (string existing in summary.Keywords)
        {
            if (string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        summary.Keywords.Add(normalized);
    }

    internal static void AddKeywordText(ImageMetadataSummary summary, string text)
    {
        if (summary == null || IsBlank(text))
        {
            return;
        }

        int start = 0;
        while (start <= text.Length)
        {
            int separator = text.IndexOf(';', start);
            if (separator < 0)
            {
                AddKeyword(summary, text.Substring(start));
                return;
            }

            AddKeyword(summary, text.Substring(start, separator - start));
            start = separator + 1;
        }
    }

    internal static int IntValue(MetadataTag? tag)
    {
        if (tag == null || tag.Value == null)
        {
            return 0;
        }

        MetadataValue value = tag.Value;
        if (value.Kind == MetadataValueKind.Integer)
        {
            return ClampToInt(value.IntegerValue);
        }

        if (value.Kind == MetadataValueKind.UnsignedInteger)
        {
            if (value.UnsignedIntegerValue > 2147483647)
            {
                return 2147483647;
            }

            return (int)value.UnsignedIntegerValue;
        }

        return 0;
    }

    internal static double DoubleValue(MetadataTag? tag)
    {
        if (tag == null || tag.Value == null)
        {
            return 0;
        }

        MetadataValue value = tag.Value;
        if (value.Kind == MetadataValueKind.Double)
        {
            return value.DoubleValue;
        }

        if (value.Kind == MetadataValueKind.Integer)
        {
            return value.IntegerValue;
        }

        if (value.Kind == MetadataValueKind.UnsignedInteger)
        {
            return value.UnsignedIntegerValue;
        }

        if ((value.Kind == MetadataValueKind.Rational || value.Kind == MetadataValueKind.SignedRational) && value.Denominator != 0)
        {
            return (double)value.Numerator / (double)value.Denominator;
        }

        return 0;
    }

    internal static ImageOrientation OrientationValue(MetadataTag? tag)
    {
        int value = IntValue(tag);
        if (value == 1)
        {
            return ImageOrientation.Normal;
        }

        if (value == 2)
        {
            return ImageOrientation.MirrorHorizontal;
        }

        if (value == 3)
        {
            return ImageOrientation.Rotate180;
        }

        if (value == 4)
        {
            return ImageOrientation.MirrorVertical;
        }

        if (value == 5)
        {
            return ImageOrientation.MirrorHorizontalRotate270;
        }

        if (value == 6)
        {
            return ImageOrientation.Rotate90;
        }

        if (value == 7)
        {
            return ImageOrientation.MirrorHorizontalRotate90;
        }

        if (value == 8)
        {
            return ImageOrientation.Rotate270;
        }

        return ImageOrientation.Unknown;
    }

    internal static ImageOrientation FindOrientation(ImageMetadataDocument document, string[] keys)
    {
        if (document == null || keys == null)
        {
            return ImageOrientation.Unknown;
        }

        int index = 0;
        while (index < keys.Length)
        {
            ImageOrientation orientation = OrientationValue(ImageMetadataQueryCore.FindFirst(document, keys[index]));
            if (IsKnownOrientation(orientation))
            {
                return orientation;
            }

            index = index + 1;
        }

        return ImageOrientation.Unknown;
    }

    internal static bool IsKnownOrientation(ImageOrientation orientation)
    {
        return orientation >= ImageOrientation.Normal && orientation <= ImageOrientation.Rotate270;
    }

    internal static ImageOrientation OrientationFromHeifTransform(int rotationDegrees, int mirrorAxis)
    {
        if (rotationDegrees != 0 && rotationDegrees != 90 && rotationDegrees != 180 && rotationDegrees != 270)
        {
            return ImageOrientation.Unknown;
        }

        if (mirrorAxis != -1 && mirrorAxis != 0 && mirrorAxis != 1)
        {
            return ImageOrientation.Unknown;
        }

        if (rotationDegrees == 0)
        {
            if (mirrorAxis == 0) return ImageOrientation.MirrorVertical;
            if (mirrorAxis == 1) return ImageOrientation.MirrorHorizontal;
            return ImageOrientation.Normal;
        }

        if (rotationDegrees == 90)
        {
            if (mirrorAxis == 0) return ImageOrientation.MirrorHorizontalRotate270;
            if (mirrorAxis == 1) return ImageOrientation.MirrorHorizontalRotate90;
            return ImageOrientation.Rotate270;
        }

        if (rotationDegrees == 180)
        {
            if (mirrorAxis == 0) return ImageOrientation.MirrorHorizontal;
            if (mirrorAxis == 1) return ImageOrientation.MirrorVertical;
            return ImageOrientation.Rotate180;
        }

        if (mirrorAxis == 0) return ImageOrientation.MirrorHorizontalRotate90;
        if (mirrorAxis == 1) return ImageOrientation.MirrorHorizontalRotate270;
        return ImageOrientation.Rotate90;
    }

    internal static int ClampToInt(long value)
    {
        if (value > 2147483647)
        {
            return 2147483647;
        }

        if (value < -2147483648)
        {
            return -2147483648;
        }

        return (int)value;
    }

    internal static string SafeText(string? text)
    {
        if (text == null)
        {
            return "";
        }

        return text;
    }

    internal static bool IsEmpty(string? text)
    {
        return text == null || text.Length == 0;
    }

    internal static bool IsBlank(string? text)
    {
        return text == null || text.Trim().Length == 0;
    }

    internal static string[] MakeKeys()
    {
        return new string[] { "Exif.IFD0.Make", "Exif.Image.Make", "Tiff.IFD0.Make", "Tiff.Image.Make", "Exif/IFD0/Make", "Raw.RAF.Header.CameraMake" };
    }

    internal static string[] ModelKeys()
    {
        return new string[] { "Exif.IFD0.Model", "Exif.Image.Model", "Tiff.IFD0.Model", "Tiff.Image.Model", "Exif/IFD0/Model", "Raw.RAF.Header.CameraModel" };
    }

    internal static string[] LensKeys()
    {
        return new string[] { "Exif.Photo.LensModel", "Exif.ExifIFD.LensModel", "Exif.Photo.Lens", "Xmp.exifEX.LensModel", "Exif/ExifIFD/LensModel" };
    }

    internal static string[] OriginalCaptureTimeKeys()
    {
        return new string[] { "Exif.Photo.DateTimeOriginal", "Exif.ExifIFD.DateTimeOriginal", "Xmp.exif.DateTimeOriginal" };
    }

    internal static string[] ContentCreationTimeKeys()
    {
        return new string[] { "Xmp.photoshop.DateCreated" };
    }

    internal static string[] IptcContentCreationDateKeys()
    {
        return new string[] { "Iptc.Application2.DateCreated" };
    }

    internal static string[] IptcContentCreationTimeKeys()
    {
        return new string[] { "Iptc.Application2.TimeCreated" };
    }

    internal static string[] EmbeddedCreationTimeKeys()
    {
        return new string[] { "Png.Text.CreationTime", "Tga.Extension.Timestamp", "Eps.DSC.CreationDate" };
    }

    internal static string[] DigitalCreationTimeKeys()
    {
        return new string[] { "Exif.Photo.DateTimeDigitized", "Exif.ExifIFD.DateTimeDigitized", "Xmp.exif.DateTimeDigitized", "Xmp.xmp.CreateDate" };
    }

    internal static string[] IptcDigitalCreationDateKeys()
    {
        return new string[] { "Iptc.Application2.DigitalCreationDate" };
    }

    internal static string[] IptcDigitalCreationTimeKeys()
    {
        return new string[] { "Iptc.Application2.DigitalCreationTime" };
    }

    internal static string[] ModificationTimeFallbackKeys()
    {
        return new string[] { "Exif.IFD0.DateTime", "Exif.Image.DateTime", "Tiff.IFD0.DateTime", "Tiff.Image.DateTime", "Png.tIME.LastModificationTime" };
    }

    internal static string[] AuthorKeys()
    {
        return new string[] { "Xmp.dc.creator", "Iptc.Application2.Byline", "Xmp.tiff.Artist", "Exif.IFD0.Artist", "Exif.Image.Artist", "Tiff.IFD0.Artist", "Tiff.Image.Artist", "Tiff.IFD0.XPAuthor", "Png.Text.Author", "Svg.Author", "Tga.Extension.AuthorName", "Eps.DSC.For" };
    }

    internal static string[] CopyrightKeys()
    {
        return new string[] { "Xmp.dc.rights", "Iptc.Application2.CopyrightNotice", "Xmp.tiff.Copyright", "Exif.IFD0.Copyright", "Exif.Image.Copyright", "Tiff.IFD0.Copyright", "Tiff.Image.Copyright", "Png.Text.Copyright" };
    }

    internal static string[] TitleKeys()
    {
        return new string[] { "Xmp.dc.title", "Iptc.Application2.ObjectName", "Xmp.photoshop.Headline", "Iptc.Application2.Headline", "Png.Text.Title", "Tiff.IFD0.XPTitle", "Tiff.IFD0.XPSubject", "Exif.IFD0.DocumentName", "Exif.Image.DocumentName", "Tiff.IFD0.DocumentName", "Tiff.Image.DocumentName", "Svg.Title", "Eps.DSC.Title" };
    }

    internal static string[] DescriptionKeys()
    {
        return new string[] { "Xmp.dc.description", "Iptc.Application2.Caption", "Iptc.Application2.LocalCaption", "Xmp.tiff.ImageDescription", "Png.Text.Description", "Exif.IFD0.ImageDescription", "Exif.Image.ImageDescription", "Tiff.IFD0.ImageDescription", "Tiff.Image.ImageDescription", "Exif.ExifIFD.UserComment", "Tiff.IFD0.XPComment", "Png.Text.Comment", "Svg.Description", "Tga.Extension.AuthorComments", "Jpeg.COM.Comment", "Gif.Comment.Text", "Netpbm.Header.Comments", "Radiance.Comments" };
    }

    internal static string[] KeywordKeys()
    {
        return new string[] { "Xmp.dc.subject", "Iptc.Application2.Keywords", "Xmp.MicrosoftPhoto.LastKeywordXMP", "Tiff.IFD0.XPKeywords", "Png.Text.Keywords" };
    }

    internal static string[] SimpleRatingKeys()
    {
        return new string[] { "Xmp.xmp.Rating", "Tiff.IFD0.Rating", "Tiff.Image.Rating", "Exif.IFD0.Rating", "Exif.Image.Rating" };
    }

    internal static string[] PercentRatingKeys()
    {
        return new string[] { "Tiff.IFD0.RatingPercent", "Tiff.Image.RatingPercent", "Exif.IFD0.RatingPercent", "Exif.Image.RatingPercent", "Xmp.MicrosoftPhoto.Rating" };
    }

    internal static string[] SoftwareKeys()
    {
        return new string[] { "Exif.IFD0.Software", "Exif.Image.Software", "Tiff.IFD0.Software", "Tiff.Image.Software", "Xmp.xmp.CreatorTool", "Xmp.tiff.Software", "Png.Text.Software", "Radiance.Software", "Eps.DSC.Creator" };
    }

    internal static string[] IptcSoftwareNameKeys()
    {
        return new string[] { "Iptc.Application2.OriginatingProgram" };
    }

    internal static string[] IptcSoftwareVersionKeys()
    {
        return new string[] { "Iptc.Application2.ProgramVersion" };
    }

    internal static string[] TgaSoftwareNameKeys()
    {
        return new string[] { "Tga.Extension.SoftwareId" };
    }

    internal static string[] TgaSoftwareVersionKeys()
    {
        return new string[] { "Tga.Extension.SoftwareVersionNumber" };
    }

    internal static string[] OrientationKeys()
    {
        return new string[] { "JpegXl.Codestream.Orientation", "Bmff.Frame.Orientation", "Bmff.Item.Orientation", "Radiance.Orientation", "Exif.IFD0.Orientation", "Exif.Image.Orientation", "Tiff.IFD0.Orientation", "Tiff.Image.Orientation", "Xmp.tiff.Orientation", "Exif/IFD0/Orientation", "Xmp/TIFF/Orientation" };
    }

    internal static string[] LatitudeKeys()
    {
        return new string[] { "Exif.GPS.Latitude", "Exif.GPS.GPSLatitude", "Gps.GPS.Latitude", "Gps.GPS.GPSLatitude", "Gps/GPS/Latitude" };
    }

    internal static string[] LongitudeKeys()
    {
        return new string[] { "Exif.GPS.Longitude", "Exif.GPS.GPSLongitude", "Gps.GPS.Longitude", "Gps.GPS.GPSLongitude", "Gps/GPS/Longitude" };
    }

    internal static string[] AltitudeKeys()
    {
        return new string[] { "Exif.GPS.Altitude", "Exif.GPS.GPSAltitude", "Gps.GPS.Altitude", "Gps.GPS.GPSAltitude", "Gps/GPS/Altitude" };
    }

    internal static string[] LatitudeReferenceKeys()
    {
        return new string[] { "Exif.GPS.LatitudeRef", "Exif.GPS.GPSLatitudeRef", "Gps.GPS.LatitudeRef", "Gps.GPS.GPSLatitudeRef", "Gps/GPS/LatitudeRef" };
    }

    internal static string[] LongitudeReferenceKeys()
    {
        return new string[] { "Exif.GPS.LongitudeRef", "Exif.GPS.GPSLongitudeRef", "Gps.GPS.LongitudeRef", "Gps.GPS.GPSLongitudeRef", "Gps/GPS/LongitudeRef" };
    }

    internal static string[] AltitudeReferenceKeys()
    {
        return new string[] { "Exif.GPS.AltitudeRef", "Exif.GPS.GPSAltitudeRef", "Gps.GPS.AltitudeRef", "Gps.GPS.GPSAltitudeRef", "Gps/GPS/AltitudeRef" };
    }

    internal static string[] XResolutionKeys()
    {
        return new string[] { "Exif.IFD0.XResolution", "Exif.Image.XResolution", "Tiff.IFD0.XResolution", "Tiff.Image.XResolution", "Jpeg2000.resc.XResolution", "Jpeg2000.resd.XResolution", "Psd.Resolution.Horizontal", "Png.pHYs.PixelsPerUnitX", "Jfif.APP0.XDensity", "Bmp.DIBHeader.XPixelsPerMeter", "Pcx.HorizontalDpi" };
    }

    internal static string[] YResolutionKeys()
    {
        return new string[] { "Exif.IFD0.YResolution", "Exif.Image.YResolution", "Tiff.IFD0.YResolution", "Tiff.Image.YResolution", "Jpeg2000.resc.YResolution", "Jpeg2000.resd.YResolution", "Psd.Resolution.Vertical", "Png.pHYs.PixelsPerUnitY", "Jfif.APP0.YDensity", "Bmp.DIBHeader.YPixelsPerMeter", "Pcx.VerticalDpi" };
    }

    internal static string[] ResolutionUnitKeys()
    {
        return new string[] { "Exif.IFD0.ResolutionUnit", "Exif.Image.ResolutionUnit", "Tiff.IFD0.ResolutionUnit", "Tiff.Image.ResolutionUnit", "Jpeg2000.resc.ResolutionUnit", "Jpeg2000.resd.ResolutionUnit", "Psd.Resolution.HorizontalUnit", "Png.pHYs.UnitSpecifier", "Jfif.APP0.DensityUnits", "Bmp.DIBHeader.ResolutionUnit", "Pcx.ResolutionUnit" };
    }

    internal static string[] PixelAspectRatioKeys()
    {
        return new string[] { "Png.pHYs.PixelAspectRatio", "Gif.LogicalScreen.PixelAspectRatio", "Tga.Extension.PixelAspectRatio", "Radiance.EffectivePixelAspect" };
    }

    internal static string[] SourceColorTypeKeys()
    {
        return new string[] { "Png.IHDR.ColorType", "Psd.Header.ColorMode", "Tga.Header.ImageType", "Dds.PixelFormat.Name", "Netpbm.Header.TupleType", "JpegXl.Codestream.ColorSpace", "Jpeg2000.colr.EnumeratedColorSpace", "Bmff.Item.ColorType" };
    }

    internal static string[] InterlacedKeys()
    {
        return new string[] { "Gif.ImageDescriptor.Interlaced" };
    }

    internal static string[] ProgressiveKeys()
    {
        return new string[] { "Jpeg.SOF2.Progressive", "Jpeg.SOF6.Progressive", "Jpeg.SOF10.Progressive", "Jpeg.SOF14.Progressive" };
    }

    internal static string[] AnimatedKeys()
    {
        return new string[] { "Gif.Animation.IsAnimated", "WebP.VP8X.IsAnimated", "JpegXl.Codestream.HaveAnimation" };
    }

    internal static string[] AnimationFrameCountKeys()
    {
        return new string[] { "Png.acTL.FrameCount" };
    }

    internal static string[] AnimationLoopCountKeys()
    {
        return new string[] { "Gif.Animation.LoopCount", "Png.acTL.PlayCount", "WebP.ANIM.LoopCount", "JpegXl.Codestream.LoopCount" };
    }
}
