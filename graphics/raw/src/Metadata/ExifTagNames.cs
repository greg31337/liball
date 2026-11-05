namespace raw;

/// <summary>Resolves Exif 3.1, GPS, interoperability, and common TIFF field names by directory context.</summary>
internal static class RawExifTagNames
{
    /// <summary>Returns the standardized field name or a stable hexadecimal fallback.</summary>
    /// <param name="kind">The containing directory kind.</param>
    /// <param name="tag">The unsigned tag identifier.</param>
    /// <returns>The field name.</returns>
    public static string GetName(RawIfdKind kind, ushort tag)
    {
        string name = FindName(kind, tag);
        if (name.Length != 0)
        {
            return name;
        }
        return "Tag0x" + tag.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Returns whether the tag has a standardized name in the supplied directory kind.</summary>
    /// <param name="kind">The containing directory kind.</param>
    /// <param name="tag">The unsigned tag identifier.</param>
    /// <returns><c>true</c> when a known name exists; otherwise <c>false</c>.</returns>
    public static bool IsKnown(RawIfdKind kind, ushort tag)
    {
        return FindName(kind, tag).Length != 0;
    }

    internal static string FindName(RawIfdKind kind, ushort tag)
    {
        if (kind == RawIfdKind.Gps)
        {
            return FindGpsName(tag);
        }
        if (kind == RawIfdKind.Interoperability)
        {
            return tag == 1 ? "InteroperabilityIndex" : string.Empty;
        }
        if (kind == RawIfdKind.Exif)
        {
            return FindExifName(tag);
        }
        return FindTiffName(tag);
    }

    internal static string FindGpsName(ushort tag)
    {
        switch (tag)
        {
            case 0: return "GPSVersionID";
            case 1: return "GPSLatitudeRef";
            case 2: return "GPSLatitude";
            case 3: return "GPSLongitudeRef";
            case 4: return "GPSLongitude";
            case 5: return "GPSAltitudeRef";
            case 6: return "GPSAltitude";
            case 7: return "GPSTimeStamp";
            case 8: return "GPSSatellites";
            case 9: return "GPSStatus";
            case 10: return "GPSMeasureMode";
            case 11: return "GPSDOP";
            case 12: return "GPSSpeedRef";
            case 13: return "GPSSpeed";
            case 14: return "GPSTrackRef";
            case 15: return "GPSTrack";
            case 16: return "GPSImgDirectionRef";
            case 17: return "GPSImgDirection";
            case 18: return "GPSMapDatum";
            case 19: return "GPSDestLatitudeRef";
            case 20: return "GPSDestLatitude";
            case 21: return "GPSDestLongitudeRef";
            case 22: return "GPSDestLongitude";
            case 23: return "GPSDestBearingRef";
            case 24: return "GPSDestBearing";
            case 25: return "GPSDestDistanceRef";
            case 26: return "GPSDestDistance";
            case 27: return "GPSProcessingMethod";
            case 28: return "GPSAreaInformation";
            case 29: return "GPSDateStamp";
            case 30: return "GPSDifferential";
            case 31: return "GPSHPositioningError";
            default: return string.Empty;
        }
    }

    internal static string FindExifName(ushort tag)
    {
        switch (tag)
        {
            case 33434: return "ExposureTime";
            case 33437: return "FNumber";
            case 34850: return "ExposureProgram";
            case 34852: return "SpectralSensitivity";
            case 34855: return "PhotographicSensitivity";
            case 34856: return "OECF";
            case 34864: return "SensitivityType";
            case 34865: return "StandardOutputSensitivity";
            case 34866: return "RecommendedExposureIndex";
            case 34867: return "ISOSpeed";
            case 34868: return "ISOSpeedLatitudeyyy";
            case 34869: return "ISOSpeedLatitudezzz";
            case 36864: return "ExifVersion";
            case 36867: return "DateTimeOriginal";
            case 36868: return "DateTimeDigitized";
            case 36880: return "OffsetTime";
            case 36881: return "OffsetTimeOriginal";
            case 36882: return "OffsetTimeDigitized";
            case 37121: return "ComponentsConfiguration";
            case 37122: return "CompressedBitsPerPixel";
            case 37377: return "ShutterSpeedValue";
            case 37378: return "ApertureValue";
            case 37379: return "BrightnessValue";
            case 37380: return "ExposureBiasValue";
            case 37381: return "MaxApertureValue";
            case 37382: return "SubjectDistance";
            case 37383: return "MeteringMode";
            case 37384: return "LightSource";
            case 37385: return "Flash";
            case 37386: return "FocalLength";
            case 37396: return "SubjectArea";
            case 37500: return "MakerNote";
            case 37510: return "UserComment";
            case 37511: return "LearningOptOutIn";
            case 37520: return "SubSecTime";
            case 37521: return "SubSecTimeOriginal";
            case 37522: return "SubSecTimeDigitized";
            case 37888: return "Temperature";
            case 37889: return "Humidity";
            case 37890: return "Pressure";
            case 37891: return "WaterDepth";
            case 37892: return "Acceleration";
            case 37893: return "CameraElevationAngle";
            case 40960: return "FlashpixVersion";
            case 40961: return "ColorSpace";
            case 40962: return "PixelXDimension";
            case 40963: return "PixelYDimension";
            case 40964: return "RelatedSoundFile";
            case 40965: return "InteroperabilityIFDPointer";
            case 41483: return "FlashEnergy";
            case 41484: return "SpatialFrequencyResponse";
            case 41486: return "FocalPlaneXResolution";
            case 41487: return "FocalPlaneYResolution";
            case 41488: return "FocalPlaneResolutionUnit";
            case 41492: return "SubjectLocation";
            case 41493: return "ExposureIndex";
            case 41495: return "SensingMethod";
            case 41728: return "FileSource";
            case 41729: return "SceneType";
            case 41730: return "CFAPattern";
            case 41985: return "CustomRendered";
            case 41986: return "ExposureMode";
            case 41987: return "WhiteBalance";
            case 41988: return "DigitalZoomRatio";
            case 41989: return "FocalLengthIn35mmFilm";
            case 41990: return "SceneCaptureType";
            case 41991: return "GainControl";
            case 41992: return "Contrast";
            case 41993: return "Saturation";
            case 41994: return "Sharpness";
            case 41995: return "DeviceSettingDescription";
            case 41996: return "SubjectDistanceRange";
            case 41997: return "DevelopmentType";
            case 41998: return "DevelopmentTypeDescription";
            case 41999: return "DistortionCorrection";
            case 42000: return "ChromaticAberrationCorrection";
            case 42001: return "ShadingCorrection";
            case 42002: return "NoiseReduction";
            case 42016: return "ImageUniqueID";
            case 42032: return "CameraOwnerName";
            case 42033: return "BodySerialNumber";
            case 42034: return "LensSpecification";
            case 42035: return "LensMake";
            case 42036: return "LensModel";
            case 42037: return "LensSerialNumber";
            case 42038: return "ImageTitle";
            case 42039: return "Photographer";
            case 42040: return "ImageEditor";
            case 42041: return "CameraFirmware";
            case 42042: return "RAWDevelopingSoftware";
            case 42043: return "ImageEditingSoftware";
            case 42044: return "MetadataEditingSoftware";
            case 42080: return "CompositeImage";
            case 42081: return "SourceImageNumberOfCompositeImage";
            case 42082: return "SourceExposureTimesOfCompositeImage";
            case 42240: return "Gamma";
            default: return string.Empty;
        }
    }

    internal static string FindTiffName(ushort tag)
    {
        switch (tag)
        {
            case 254: return "NewSubfileType";
            case 255: return "SubfileType";
            case 256: return "ImageWidth";
            case 257: return "ImageLength";
            case 258: return "BitsPerSample";
            case 259: return "Compression";
            case 262: return "PhotometricInterpretation";
            case 270: return "ImageDescription";
            case 271: return "Make";
            case 272: return "Model";
            case 273: return "StripOffsets";
            case 274: return "Orientation";
            case 277: return "SamplesPerPixel";
            case 278: return "RowsPerStrip";
            case 279: return "StripByteCounts";
            case 282: return "XResolution";
            case 283: return "YResolution";
            case 284: return "PlanarConfiguration";
            case 296: return "ResolutionUnit";
            case 301: return "TransferFunction";
            case 305: return "Software";
            case 306: return "DateTime";
            case 315: return "Artist";
            case 317: return "Predictor";
            case 318: return "WhitePoint";
            case 319: return "PrimaryChromaticities";
            case 322: return "TileWidth";
            case 323: return "TileLength";
            case 324: return "TileOffsets";
            case 325: return "TileByteCounts";
            case 330: return "SubIFDs";
            case 529: return "YCbCrCoefficients";
            case 530: return "YCbCrSubSampling";
            case 531: return "YCbCrPositioning";
            case 532: return "ReferenceBlackWhite";
            case 700: return "XMP";
            case 33421: return "CFARepeatPatternDim";
            case 33422: return "CFAPattern";
            case 33432: return "Copyright";
            case 33723: return "RichTIFFIPTC";
            case 34377: return "PhotoshopImageResources";
            case 34665: return "ExifIFDPointer";
            case 34675: return "ICCProfile";
            case 34853: return "GPSInfoIFDPointer";
            case 40091: return "XPTitle";
            case 40092: return "XPComment";
            case 40093: return "XPAuthor";
            case 40094: return "XPKeywords";
            case 40095: return "XPSubject";
            default: return string.Empty;
        }
    }
}
