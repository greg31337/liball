using System.Collections.Generic;

namespace tiff;

/// <summary>Maps TIFF tag identifiers to known or application-provided type and count definitions.</summary>
public class TiffTagRegistry : ITiffTagRegistry
{
    internal Dictionary<int, TiffTagDefinition> Definitions;

    /// <summary>Creates a new instance of the TIFF Tag Registry class.</summary>
    public TiffTagRegistry()
    {
        Definitions = new Dictionary<int, TiffTagDefinition>();
    }

    /// <summary>Creates a new instance initialized with the library's documented defaults.</summary>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagRegistry CreateDefault()
    {
        TiffTagRegistry registry = new TiffTagRegistry();
        registry.RegisterBaselineTags();
        registry.RegisterExtensionTags();
        registry.RegisterFaxTags();
        registry.RegisterJpegTags();
        registry.RegisterMetadataTags();
        registry.RegisterGeoTiffTags();
        registry.RegisterDngTags();
        return registry;
    }

    /// <summary>Returns the number of available value entries.</summary>
    /// <returns>The resulting int.</returns>
    public int Count()
    {
        return Definitions.Count;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF tag registry.</returns>
    public TiffTagRegistry Copy()
    {
        TiffTagRegistry copy = new TiffTagRegistry();
        foreach (TiffTagDefinition definition in Definitions.Values)
        {
            copy.Register(definition);
        }

        return copy;
    }

    /// <summary>Determines whether the collection contains value.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool Contains(int tagId)
    {
        return Definitions.ContainsKey(tagId);
    }

    /// <summary>Finds the value, or returns the type's documented missing-value result.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <returns>A detached copy of the requested definition, or <c>null</c> when the tag is not registered.</returns>
    public TiffTagDefinition? Find(int tagId)
    {
        if (!Definitions.ContainsKey(tagId))
        {
            return null;
        }

        return Definitions[tagId].Copy();
    }

    /// <summary>Registers value for type and count validation during TIFF processing.</summary>
    /// <param name="definition">The definition.</param>
    public void Register(TiffTagDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        Definitions[definition.TagId] = definition.Copy();
    }

    /// <summary>Registers known for type and count validation during TIFF processing.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="expectedCount">The expected count.</param>
    public void RegisterKnown(int tagId, string name, TiffDataType dataType, long expectedCount)
    {
        TiffDataType[] dataTypes = new TiffDataType[1];
        dataTypes[0] = dataType;
        Register(TiffTagDefinition.Create(tagId, name, dataType, dataTypes, expectedCount));
    }

    /// <summary>Registers known types for type and count validation during TIFF processing.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="name">The name.</param>
    /// <param name="preferredDataType">The preferred data type.</param>
    /// <param name="allowedDataTypes">The allowed data types.</param>
    /// <param name="expectedCount">The expected count.</param>
    public void RegisterKnownTypes(int tagId, string name, TiffDataType preferredDataType, TiffDataType[] allowedDataTypes, long expectedCount)
    {
        Register(TiffTagDefinition.Create(tagId, name, preferredDataType, allowedDataTypes, expectedCount));
    }

    internal void RegisterBaselineTags()
    {
        TiffDataType[] shortOrLong = new TiffDataType[] { TiffDataType.Short, TiffDataType.Long };
        TiffDataType[] shortOrLongOffsets = new TiffDataType[] { TiffDataType.Short, TiffDataType.Long, TiffDataType.Long8 };
        TiffDataType[] longOffsets = new TiffDataType[] { TiffDataType.Long, TiffDataType.Long8 };
        RegisterKnown(TiffTagId.NewSubfileType, "NewSubfileType", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.SubfileType, "SubfileType", TiffDataType.Short, 1);
        RegisterKnownTypes(TiffTagId.ImageWidth, "ImageWidth", TiffDataType.Long, shortOrLong, 1);
        RegisterKnownTypes(TiffTagId.ImageLength, "ImageLength", TiffDataType.Long, shortOrLong, 1);
        RegisterKnown(TiffTagId.BitsPerSample, "BitsPerSample", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.Compression, "Compression", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.PhotometricInterpretation, "PhotometricInterpretation", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.Thresholding, "Thresholding", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.CellWidth, "CellWidth", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.CellLength, "CellLength", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.FillOrder, "FillOrder", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.DocumentName, "DocumentName", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ImageDescription, "ImageDescription", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.Make, "Make", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.Model, "Model", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnownTypes(TiffTagId.StripOffsets, "StripOffsets", TiffDataType.Long, shortOrLongOffsets, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.Orientation, "Orientation", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.SamplesPerPixel, "SamplesPerPixel", TiffDataType.Short, 1);
        RegisterKnownTypes(TiffTagId.RowsPerStrip, "RowsPerStrip", TiffDataType.Long, shortOrLong, 1);
        RegisterKnownTypes(TiffTagId.StripByteCounts, "StripByteCounts", TiffDataType.Long, shortOrLongOffsets, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.MinSampleValue, "MinSampleValue", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.MaxSampleValue, "MaxSampleValue", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.XResolution, "XResolution", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.YResolution, "YResolution", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.PlanarConfiguration, "PlanarConfiguration", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.PageName, "PageName", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.XPosition, "XPosition", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.YPosition, "YPosition", TiffDataType.Rational, 1);
        RegisterKnownTypes(TiffTagId.FreeOffsets, "FreeOffsets", TiffDataType.Long, longOffsets, TiffTagDefinition.AnyCount);
        RegisterKnownTypes(TiffTagId.FreeByteCounts, "FreeByteCounts", TiffDataType.Long, longOffsets, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.GrayResponseUnit, "GrayResponseUnit", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.GrayResponseCurve, "GrayResponseCurve", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.T4Options, "T4Options", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.T6Options, "T6Options", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.ResolutionUnit, "ResolutionUnit", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.PageNumber, "PageNumber", TiffDataType.Short, 2);
        RegisterKnown(TiffTagId.ColorResponseUnit, "ColorResponseUnit", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.TransferFunction, "TransferFunction", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.Software, "Software", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.DateTime, "DateTime", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.Artist, "Artist", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.HostComputer, "HostComputer", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.Predictor, "Predictor", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.WhitePoint, "WhitePoint", TiffDataType.Rational, 2);
        RegisterKnown(TiffTagId.PrimaryChromaticities, "PrimaryChromaticities", TiffDataType.Rational, 6);
        RegisterKnown(TiffTagId.ColorMap, "ColorMap", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ImageJMetadataByteCounts, "MetaDataByteCounts", TiffDataType.Long, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ImageJMetadata, "MetaData", TiffDataType.Byte, TiffTagDefinition.AnyCount);
    }

    internal void RegisterExtensionTags()
    {
        TiffDataType[] shortOrLong = new TiffDataType[] { TiffDataType.Short, TiffDataType.Long };
        TiffDataType[] tileStorageTypes = new TiffDataType[] { TiffDataType.Long, TiffDataType.Long8 };
        TiffDataType[] pointerTypes = new TiffDataType[] { TiffDataType.Long, TiffDataType.Ifd, TiffDataType.Ifd8, TiffDataType.Long8 };
        TiffDataType[] byteOrShort = new TiffDataType[] { TiffDataType.Byte, TiffDataType.Short };
        TiffDataType[] sampleTypes = new TiffDataType[] { TiffDataType.Byte, TiffDataType.Short, TiffDataType.Long, TiffDataType.Rational, TiffDataType.SByte, TiffDataType.SShort, TiffDataType.SLong, TiffDataType.SRational, TiffDataType.Float, TiffDataType.Double, TiffDataType.Long8, TiffDataType.SLong8 };
        RegisterKnown(TiffTagId.HalftoneHints, "HalftoneHints", TiffDataType.Short, 2);
        RegisterKnownTypes(TiffTagId.TileWidth, "TileWidth", TiffDataType.Long, shortOrLong, 1);
        RegisterKnownTypes(TiffTagId.TileLength, "TileLength", TiffDataType.Long, shortOrLong, 1);
        RegisterKnownTypes(TiffTagId.TileOffsets, "TileOffsets", TiffDataType.Long, tileStorageTypes, TiffTagDefinition.AnyCount);
        RegisterKnownTypes(TiffTagId.TileByteCounts, "TileByteCounts", TiffDataType.Long, tileStorageTypes, TiffTagDefinition.AnyCount);
        RegisterKnownTypes(TiffTagId.SubIFDs, "SubIFDs", TiffDataType.Ifd, pointerTypes, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.InkSet, "InkSet", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.InkNames, "InkNames", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.NumberOfInks, "NumberOfInks", TiffDataType.Short, 1);
        RegisterKnownTypes(TiffTagId.DotRange, "DotRange", TiffDataType.Byte, byteOrShort, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.TargetPrinter, "TargetPrinter", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ExtraSamples, "ExtraSamples", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.SampleFormat, "SampleFormat", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnownTypes(TiffTagId.SMinSampleValue, "SMinSampleValue", TiffDataType.Double, sampleTypes, TiffTagDefinition.AnyCount);
        RegisterKnownTypes(TiffTagId.SMaxSampleValue, "SMaxSampleValue", TiffDataType.Double, sampleTypes, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.TransferRange, "TransferRange", TiffDataType.Short, 6);
        RegisterKnown(TiffTagId.JPEGTables, "JPEGTables", TiffDataType.Undefined, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.YCbCrCoefficients, "YCbCrCoefficients", TiffDataType.Rational, 3);
        RegisterKnown(TiffTagId.YCbCrSubSampling, "YCbCrSubSampling", TiffDataType.Short, 2);
        RegisterKnown(TiffTagId.YCbCrPositioning, "YCbCrPositioning", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.ReferenceBlackWhite, "ReferenceBlackWhite", TiffDataType.Rational, 6);
        RegisterKnown(TiffTagId.Copyright, "Copyright", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ImageDepth, "ImageDepth", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.TileDepth, "TileDepth", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.CFARepeatPatternDim, "CFARepeatPatternDim", TiffDataType.Short, 2);
        RegisterKnown(TiffTagId.CFAPattern, "CFAPattern", TiffDataType.Byte, TiffTagDefinition.AnyCount);
    }

    internal void RegisterFaxTags()
    {
        RegisterKnown(TiffTagId.BadFaxLines, "BadFaxLines", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.CleanFaxData, "CleanFaxData", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.ConsecutiveBadFaxLines, "ConsecutiveBadFaxLines", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.FaxReceiveParameters, "FaxRecvParams", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.FaxSubAddress, "FaxSubAddress", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.FaxReceiveTime, "FaxRecvTime", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.FaxDcs, "FaxDcs", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
    }

    internal void RegisterJpegTags()
    {
        RegisterKnown(TiffTagId.JPEGProc, "JPEGProc", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.JPEGInterchangeFormat, "JPEGInterchangeFormat", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.JPEGInterchangeFormatLength, "JPEGInterchangeFormatLength", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.JPEGRestartInterval, "JPEGRestartInterval", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.JPEGLosslessPredictors, "JPEGLosslessPredictors", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.JPEGPointTransforms, "JPEGPointTransforms", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.JPEGQTables, "JPEGQTables", TiffDataType.Long, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.JPEGDCTables, "JPEGDCTables", TiffDataType.Long, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.JPEGACTables, "JPEGACTables", TiffDataType.Long, TiffTagDefinition.AnyCount);
    }

    internal void RegisterMetadataTags()
    {
        TiffDataType[] longOrIfd = new TiffDataType[] { TiffDataType.Long, TiffDataType.Ifd, TiffDataType.Ifd8, TiffDataType.Long8 };
        RegisterKnown(TiffTagId.Xmp, "XMP", TiffDataType.Byte, TiffTagDefinition.AnyCount);
        TiffDataType[] iptcTypes = new TiffDataType[] { TiffDataType.Long, TiffDataType.Undefined };
        RegisterKnownTypes(TiffTagId.Iptc, "RichTIFFIPTC", TiffDataType.Long, iptcTypes, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.Photoshop, "Photoshop", TiffDataType.Byte, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.CzLsmInfo, "CZ_LSMInfo", TiffDataType.Undefined, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.IccProfile, "ICCProfile", TiffDataType.Undefined, TiffTagDefinition.AnyCount);
        RegisterKnownTypes(TiffTagId.ExifIfd, "ExifIFD", TiffDataType.Long, longOrIfd, 1);
        RegisterKnownTypes(TiffTagId.GpsIfd, "GPSIFD", TiffDataType.Long, longOrIfd, 1);
        RegisterKnownTypes(TiffTagId.InteroperabilityIfd, "InteroperabilityIFD", TiffDataType.Long, longOrIfd, 1);
    }

    internal void RegisterGeoTiffTags()
    {
        RegisterKnown(TiffTagId.ModelPixelScale, "ModelPixelScaleTag", TiffDataType.Double, 3);
        RegisterKnown(TiffTagId.ModelTiepoint, "ModelTiepointTag", TiffDataType.Double, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ModelTransformation, "ModelTransformationTag", TiffDataType.Double, 16);
        RegisterKnown(TiffTagId.GeoKeyDirectory, "GeoKeyDirectoryTag", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.GeoDoubleParams, "GeoDoubleParamsTag", TiffDataType.Double, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.GeoAsciiParams, "GeoAsciiParamsTag", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.GdalMetadata, "GDAL_METADATA", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.GdalNoData, "GDAL_NODATA", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
    }

    internal void RegisterDngTags()
    {
        TiffDataType[] shortLongRational = new TiffDataType[] { TiffDataType.Short, TiffDataType.Long, TiffDataType.Rational };
        TiffDataType[] shortOrLong = new TiffDataType[] { TiffDataType.Short, TiffDataType.Long };
        TiffDataType[] cropTypes = new TiffDataType[] { TiffDataType.Short, TiffDataType.Long, TiffDataType.Rational };
        RegisterKnown(TiffTagId.DngVersion, "DNGVersion", TiffDataType.Byte, 4);
        RegisterKnown(TiffTagId.DngBackwardVersion, "DNGBackwardVersion", TiffDataType.Byte, 4);
        RegisterKnown(TiffTagId.UniqueCameraModel, "UniqueCameraModel", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.LocalizedCameraModel, "LocalizedCameraModel", TiffDataType.Byte, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.CFAPlaneColor, "CFAPlaneColor", TiffDataType.Byte, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.CFALayout, "CFALayout", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.LinearizationTable, "LinearizationTable", TiffDataType.Short, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.BlackLevelRepeatDim, "BlackLevelRepeatDim", TiffDataType.Short, 2);
        RegisterKnownTypes(TiffTagId.BlackLevel, "BlackLevel", TiffDataType.Rational, shortLongRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.BlackLevelDeltaH, "BlackLevelDeltaH", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.BlackLevelDeltaV, "BlackLevelDeltaV", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnownTypes(TiffTagId.WhiteLevel, "WhiteLevel", TiffDataType.Long, shortOrLong, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.DefaultScale, "DefaultScale", TiffDataType.Rational, 2);
        RegisterKnownTypes(TiffTagId.DefaultCropOrigin, "DefaultCropOrigin", TiffDataType.Rational, cropTypes, 2);
        RegisterKnownTypes(TiffTagId.DefaultCropSize, "DefaultCropSize", TiffDataType.Rational, cropTypes, 2);
        RegisterKnown(TiffTagId.ColorMatrix1, "ColorMatrix1", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ColorMatrix2, "ColorMatrix2", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.CameraCalibration1, "CameraCalibration1", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.CameraCalibration2, "CameraCalibration2", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ReductionMatrix1, "ReductionMatrix1", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ReductionMatrix2, "ReductionMatrix2", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.AnalogBalance, "AnalogBalance", TiffDataType.Rational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.AsShotNeutral, "AsShotNeutral", TiffDataType.Rational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.AsShotWhiteXY, "AsShotWhiteXY", TiffDataType.Rational, 2);
        RegisterKnown(TiffTagId.BaselineExposure, "BaselineExposure", TiffDataType.SRational, 1);
        RegisterKnown(TiffTagId.BaselineNoise, "BaselineNoise", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.BaselineSharpness, "BaselineSharpness", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.BayerGreenSplit, "BayerGreenSplit", TiffDataType.Long, 1);
        RegisterKnown(TiffTagId.LinearResponseLimit, "LinearResponseLimit", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.CameraSerialNumber, "CameraSerialNumber", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.LensInfo, "LensInfo", TiffDataType.Rational, 4);
        RegisterKnown(TiffTagId.ChromaBlurRadius, "ChromaBlurRadius", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.AntiAliasStrength, "AntiAliasStrength", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.ShadowScale, "ShadowScale", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.DngPrivateData, "DNGPrivateData", TiffDataType.Byte, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.MakerNoteSafety, "MakerNoteSafety", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.CalibrationIlluminant1, "CalibrationIlluminant1", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.CalibrationIlluminant2, "CalibrationIlluminant2", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.BestQualityScale, "BestQualityScale", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.RawDataUniqueId, "RawDataUniqueID", TiffDataType.Byte, 16);
        RegisterKnown(TiffTagId.OriginalRawFileName, "OriginalRawFileName", TiffDataType.Byte, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.OriginalRawFileData, "OriginalRawFileData", TiffDataType.Undefined, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.ActiveArea, "ActiveArea", TiffDataType.Long, 4);
        RegisterKnown(TiffTagId.MaskedAreas, "MaskedAreas", TiffDataType.Long, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.AsShotIccProfile, "AsShotICCProfile", TiffDataType.Undefined, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.AsShotPreProfileMatrix, "AsShotPreProfileMatrix", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.CurrentIccProfile, "CurrentICCProfile", TiffDataType.Undefined, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.CurrentPreProfileMatrix, "CurrentPreProfileMatrix", TiffDataType.SRational, TiffTagDefinition.AnyCount);
        RegisterKnown(TiffTagId.DepthFormat, "DepthFormat", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.DepthNear, "DepthNear", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.DepthFar, "DepthFar", TiffDataType.Rational, 1);
        RegisterKnown(TiffTagId.DepthUnits, "DepthUnits", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.DepthMeasureType, "DepthMeasureType", TiffDataType.Short, 1);
        RegisterKnown(TiffTagId.EnhanceParams, "EnhanceParams", TiffDataType.Ascii, TiffTagDefinition.AnyCount);
    }
}
