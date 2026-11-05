using System.Globalization;

namespace image;

internal class TiffTagNameTable
{
    internal static string KeyNameFor(MetadataFamily family, string group, int tagId)
    {
        string name = CommonNameFor(family, group, tagId);
        if (name.Length > 0)
        {
            return name;
        }

        return "Tag" + tagId.ToString(CultureInfo.InvariantCulture);
    }

    internal static string DisplayNameFor(MetadataFamily family, string group, int tagId)
    {
        string name = CommonNameFor(family, group, tagId);
        if (name.Length > 0)
        {
            return name;
        }

        return "Tag " + tagId.ToString(CultureInfo.InvariantCulture);
    }

    internal static string CommonNameFor(MetadataFamily family, string group, int tagId)
    {
        if (family == MetadataFamily.Gps)
        {
            return GpsNameFor(tagId);
        }

        if (family == MetadataFamily.Exif)
        {
            return ExifNameFor(tagId);
        }

        return TiffNameFor(tagId);
    }

    internal static string TiffNameFor(int tagId)
    {
        switch (tagId)
        {
            case 254:
                return "NewSubfileType";
            case 255:
                return "SubfileType";
            case 256:
                return "ImageWidth";
            case 257:
                return "ImageLength";
            case 258:
                return "BitsPerSample";
            case 259:
                return "Compression";
            case 262:
                return "PhotometricInterpretation";
            case 270:
                return "ImageDescription";
            case 271:
                return "Make";
            case 272:
                return "Model";
            case 273:
                return "StripOffsets";
            case 274:
                return "Orientation";
            case 277:
                return "SamplesPerPixel";
            case 278:
                return "RowsPerStrip";
            case 279:
                return "StripByteCounts";
            case 282:
                return "XResolution";
            case 283:
                return "YResolution";
            case 284:
                return "PlanarConfiguration";
            case 296:
                return "ResolutionUnit";
            case 305:
                return "Software";
            case 306:
                return "DateTime";
            case 315:
                return "Artist";
            case 318:
                return "WhitePoint";
            case 319:
                return "PrimaryChromaticities";
            case 320:
                return "ColorMap";
            case 322:
                return "TileWidth";
            case 323:
                return "TileLength";
            case 324:
                return "TileOffsets";
            case 325:
                return "TileByteCounts";
            case 330:
                return "SubIFDs";
            case 338:
                return "ExtraSamples";
            case 339:
                return "SampleFormat";
            case 33550:
                return "ModelPixelScaleTag";
            case 33922:
                return "ModelTiepointTag";
            case 34264:
                return "ModelTransformationTag";
            case 34665:
                return "ExifIFDPointer";
            case 34675:
                return "InterColorProfile";
            case 34735:
                return "GeoKeyDirectoryTag";
            case 34736:
                return "GeoDoubleParamsTag";
            case 34737:
                return "GeoAsciiParamsTag";
            case 34853:
                return "GPSInfoIFDPointer";
            case 33432:
                return "Copyright";
            case 18246:
                return "Rating";
            case 18249:
                return "RatingPercent";
            case 40091:
                return "XPTitle";
            case 40092:
                return "XPComment";
            case 40093:
                return "XPAuthor";
            case 40094:
                return "XPKeywords";
            case 40095:
                return "XPSubject";
            case 513:
                return "JPEGInterchangeFormat";
            case 514:
                return "JPEGInterchangeFormatLength";
            case 529:
                return "YCbCrCoefficients";
            case 530:
                return "YCbCrSubSampling";
            case 531:
                return "YCbCrPositioning";
            case 532:
                return "ReferenceBlackWhite";
            case 50706:
                return "DNGVersion";
            case 50707:
                return "DNGBackwardVersion";
            case 50708:
                return "UniqueCameraModel";
            case 50709:
                return "LocalizedCameraModel";
            case 50710:
                return "CFAPlaneColor";
            case 50711:
                return "CFALayout";
            case 50712:
                return "LinearizationTable";
            case 50713:
                return "BlackLevelRepeatDim";
            case 50714:
                return "BlackLevel";
            case 50715:
                return "BlackLevelDeltaH";
            case 50716:
                return "BlackLevelDeltaV";
            case 50717:
                return "WhiteLevel";
            case 50718:
                return "DefaultScale";
            case 50719:
                return "DefaultCropOrigin";
            case 50720:
                return "DefaultCropSize";
            case 50721:
                return "ColorMatrix1";
            case 50722:
                return "ColorMatrix2";
            case 50723:
                return "CameraCalibration1";
            case 50724:
                return "CameraCalibration2";
            case 50725:
                return "ReductionMatrix1";
            case 50726:
                return "ReductionMatrix2";
            case 50727:
                return "AnalogBalance";
            case 50728:
                return "AsShotNeutral";
            case 50729:
                return "AsShotWhiteXY";
            case 50730:
                return "BaselineExposure";
            case 50731:
                return "BaselineNoise";
            case 50732:
                return "BaselineSharpness";
            case 50733:
                return "BayerGreenSplit";
            case 50734:
                return "LinearResponseLimit";
            case 50735:
                return "CameraSerialNumber";
            case 50736:
                return "LensInfo";
            case 50737:
                return "ChromaBlurRadius";
            case 50738:
                return "AntiAliasStrength";
            case 50739:
                return "ShadowScale";
            case 50740:
                return "DNGPrivateData";
            case 50741:
                return "MakerNoteSafety";
            case 50778:
                return "CalibrationIlluminant1";
            case 50779:
                return "CalibrationIlluminant2";
            case 50780:
                return "BestQualityScale";
            case 50781:
                return "RawDataUniqueID";
            case 50827:
                return "OriginalRawFileName";
            case 50828:
                return "OriginalRawFileData";
            case 50829:
                return "ActiveArea";
            case 50830:
                return "MaskedAreas";
            case 50831:
                return "AsShotICCProfile";
            case 50832:
                return "AsShotPreProfileMatrix";
            case 50833:
                return "CurrentICCProfile";
            case 50834:
                return "CurrentPreProfileMatrix";
            case 50879:
                return "ColorimetricReference";
            case 50931:
                return "CameraCalibrationSignature";
            case 50932:
                return "ProfileCalibrationSignature";
            case 50933:
                return "ExtraCameraProfiles";
            case 50934:
                return "AsShotProfileName";
            case 50935:
                return "NoiseReductionApplied";
            case 50936:
                return "ProfileName";
            case 50937:
                return "ProfileHueSatMapDims";
            case 50938:
                return "ProfileHueSatMapData1";
            case 50939:
                return "ProfileHueSatMapData2";
            case 50940:
                return "ProfileToneCurve";
            case 50941:
                return "ProfileEmbedPolicy";
            case 50942:
                return "ProfileCopyright";
            case 50964:
                return "ForwardMatrix1";
            case 50965:
                return "ForwardMatrix2";
            case 50966:
                return "PreviewApplicationName";
            case 50967:
                return "PreviewApplicationVersion";
            case 50968:
                return "PreviewSettingsName";
            case 50969:
                return "PreviewSettingsDigest";
            case 50970:
                return "PreviewColorSpace";
            case 50971:
                return "PreviewDateTime";
            case 50972:
                return "RawImageDigest";
            case 50973:
                return "OriginalRawFileDigest";
            case 50974:
                return "SubTileBlockSize";
            case 50975:
                return "RowInterleaveFactor";
            case 51008:
                return "OpcodeList1";
            case 51009:
                return "OpcodeList2";
            case 51022:
                return "OpcodeList3";
            default:
                return "";
        }
    }

    internal static string ExifNameFor(int tagId)
    {
        switch (tagId)
        {
            case 33434:
                return "ExposureTime";
            case 33437:
                return "FNumber";
            case 34850:
                return "ExposureProgram";
            case 34855:
                return "ISOSpeedRatings";
            case 36864:
                return "ExifVersion";
            case 36867:
                return "DateTimeOriginal";
            case 36868:
                return "DateTimeDigitized";
            case 37121:
                return "ComponentsConfiguration";
            case 37122:
                return "CompressedBitsPerPixel";
            case 37377:
                return "ShutterSpeedValue";
            case 37378:
                return "ApertureValue";
            case 37379:
                return "BrightnessValue";
            case 37380:
                return "ExposureBiasValue";
            case 37381:
                return "MaxApertureValue";
            case 37382:
                return "SubjectDistance";
            case 37383:
                return "MeteringMode";
            case 37384:
                return "LightSource";
            case 37385:
                return "Flash";
            case 37386:
                return "FocalLength";
            case 37500:
                return "MakerNote";
            case 37510:
                return "UserComment";
            case 40960:
                return "FlashpixVersion";
            case 40961:
                return "ColorSpace";
            case 40962:
                return "PixelXDimension";
            case 40963:
                return "PixelYDimension";
            case 40965:
                return "InteroperabilityIFDPointer";
            case 41495:
                return "SensingMethod";
            case 41728:
                return "FileSource";
            case 41729:
                return "SceneType";
            case 41985:
                return "CustomRendered";
            case 41986:
                return "ExposureMode";
            case 41987:
                return "WhiteBalance";
            case 41988:
                return "DigitalZoomRatio";
            case 41989:
                return "FocalLengthIn35mmFilm";
            case 41990:
                return "SceneCaptureType";
            case 41991:
                return "GainControl";
            case 41992:
                return "Contrast";
            case 41993:
                return "Saturation";
            case 41994:
                return "Sharpness";
            case 42034:
                return "LensSpecification";
            case 42035:
                return "LensMake";
            case 42036:
                return "LensModel";
            case 42037:
                return "LensSerialNumber";
            default:
                return "";
        }
    }

    internal static string GpsNameFor(int tagId)
    {
        switch (tagId)
        {
            case 0:
                return "VersionID";
            case 1:
                return "LatitudeRef";
            case 2:
                return "Latitude";
            case 3:
                return "LongitudeRef";
            case 4:
                return "Longitude";
            case 5:
                return "AltitudeRef";
            case 6:
                return "Altitude";
            case 7:
                return "TimeStamp";
            case 8:
                return "Satellites";
            case 9:
                return "Status";
            case 10:
                return "MeasureMode";
            case 11:
                return "DOP";
            case 12:
                return "SpeedRef";
            case 13:
                return "Speed";
            case 14:
                return "TrackRef";
            case 15:
                return "Track";
            case 16:
                return "ImgDirectionRef";
            case 17:
                return "ImgDirection";
            case 18:
                return "MapDatum";
            case 19:
                return "DestLatitudeRef";
            case 20:
                return "DestLatitude";
            case 21:
                return "DestLongitudeRef";
            case 22:
                return "DestLongitude";
            case 23:
                return "DestBearingRef";
            case 24:
                return "DestBearing";
            case 25:
                return "DestDistanceRef";
            case 26:
                return "DestDistance";
            case 29:
                return "DateStamp";
            case 30:
                return "Differential";
            default:
                return "";
        }
    }
}
