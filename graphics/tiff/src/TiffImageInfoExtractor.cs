using System;

namespace tiff;

internal class TiffImageInfoExtractor
{
    internal static TiffImageInfoResult Extract(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return Extract(directory, fileBytes, byteOrder, new TiffReadOptions());
    }

    internal static TiffImageInfoResult Extract(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = TiffDirectoryReader.NormalizeOptions(options);
        TiffLimits limits = TiffDirectoryReader.GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffImageInfo info = new TiffImageInfo();

        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to extract image information.");
            return TiffImageInfoResult.Create(info, diagnostics);
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Image information extraction requires a known byte order.");
            return TiffImageInfoResult.Create(info, diagnostics);
        }

        info.ImageIndex = directory.ImageIndex;
        info.Width = ReadRequiredUnsignedScalar(directory, fileBytes, byteOrder, limits, TiffTagId.ImageWidth, diagnostics);
        info.Length = ReadRequiredUnsignedScalar(directory, fileBytes, byteOrder, limits, TiffTagId.ImageLength, diagnostics);
        ValidateImageDimensions(info, limits, diagnostics);
        info.SamplesPerPixel = ReadOptionalIntScalar(directory, fileBytes, byteOrder, limits, TiffTagId.SamplesPerPixel, 1, diagnostics);
        info.BitsPerSample = NormalizeIntArray(ReadOptionalIntArray(directory, fileBytes, byteOrder, limits, TiffTagId.BitsPerSample, CreateFilledIntArray(1, 1), diagnostics), info.SamplesPerPixel, TiffTagId.BitsPerSample, diagnostics);
        info.Compression = (TiffCompression)ReadOptionalIntScalar(directory, fileBytes, byteOrder, limits, TiffTagId.Compression, (int)TiffCompression.None, diagnostics);
        info.FillOrder = ReadOptionalIntScalar(directory, fileBytes, byteOrder, limits, TiffTagId.FillOrder, 1, diagnostics);
        info.Photometric = (TiffPhotometric)ConvertUnsignedValueToInt(ReadRequiredUnsignedScalar(directory, fileBytes, byteOrder, limits, TiffTagId.PhotometricInterpretation, diagnostics), directory.ImageIndex, TiffTagId.PhotometricInterpretation, diagnostics);
        info.PlanarConfiguration = (TiffPlanarConfiguration)ReadOptionalIntScalar(directory, fileBytes, byteOrder, limits, TiffTagId.PlanarConfiguration, (int)TiffPlanarConfiguration.Contiguous, diagnostics);
        info.SampleFormats = NormalizeSampleFormats(ReadOptionalSampleFormatArray(directory, fileBytes, byteOrder, limits, TiffTagId.SampleFormat, CreateFilledSampleFormatArray(TiffSampleFormat.UnsignedInteger, info.SamplesPerPixel), diagnostics), info.SamplesPerPixel, diagnostics);
        info.ExtraSamples = ReadOptionalExtraSampleArray(directory, fileBytes, byteOrder, limits, TiffTagId.ExtraSamples, new TiffExtraSample[0], diagnostics);
        info.ColorMap = ReadOptionalUShortArray(directory, fileBytes, byteOrder, limits, TiffTagId.ColorMap, new ushort[0], diagnostics);
        info.Orientation = (TiffOrientation)ReadOptionalIntScalar(directory, fileBytes, byteOrder, limits, TiffTagId.Orientation, (int)TiffOrientation.TopLeft, diagnostics);
        info.IsTiled = HasAnyTileTag(directory);
        info.RowsPerStrip = ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, limits, TiffTagId.RowsPerStrip, uint.MaxValue, diagnostics);
        info.TileWidth = ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, limits, TiffTagId.TileWidth, 0, diagnostics);
        info.TileLength = ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, limits, TiffTagId.TileLength, 0, diagnostics);
        info.Predictor = ReadOptionalIntScalar(directory, fileBytes, byteOrder, limits, TiffTagId.Predictor, 1, diagnostics);
        info.T4Options = ReadOptionalIntScalar(directory, fileBytes, byteOrder, limits, TiffTagId.T4Options, 0, diagnostics);
        info.T6Options = ReadOptionalIntScalar(directory, fileBytes, byteOrder, limits, TiffTagId.T6Options, 0, diagnostics);
        info.YCbCrCoefficients = NormalizeDoubleArray(ReadOptionalRationalDoubleArray(directory, fileBytes, byteOrder, limits, TiffTagId.YCbCrCoefficients, CreateYCbCrCoefficientsDefault(), diagnostics), 3, TiffTagId.YCbCrCoefficients, diagnostics);
        int[] ycbcrSubSampling = ReadOptionalIntArray(directory, fileBytes, byteOrder, limits, TiffTagId.YCbCrSubSampling, CreateYCbCrSubSamplingDefault(), diagnostics);
        if (ycbcrSubSampling.Length >= 2)
        {
            info.YCbCrSubSamplingHorizontal = ycbcrSubSampling[0];
            info.YCbCrSubSamplingVertical = ycbcrSubSampling[1];
        }
        else
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "YCbCrSubSampling must contain two values.", directory.ImageIndex, TiffTagId.YCbCrSubSampling);
        }

        info.ReferenceBlackWhite = NormalizeDoubleArray(ReadOptionalRationalDoubleArray(directory, fileBytes, byteOrder, limits, TiffTagId.ReferenceBlackWhite, CreateReferenceBlackWhiteDefault(), diagnostics), 6, TiffTagId.ReferenceBlackWhite, diagnostics);
        TiffTagValue[] extendedValues = ReadExtendedValues(directory, fileBytes, byteOrder, limits, diagnostics);
        ApplyExtendedValues(info, extendedValues, directory.ImageIndex, diagnostics);
        return TiffImageInfoResult.Create(info, diagnostics);
    }

    internal static TiffTagValue[] ReadExtendedValues(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] values = new TiffTagValue[0];
        int[] tagIds = GetExtendedInfoTagIds();
        int index = 0;
        while (index < tagIds.Length)
        {
            TiffIfdEntry? entry = directory.FindEntry(tagIds[index]);
            if (entry != null)
            {
                TiffTagValue value = TiffExifMetadataReader.ReadValue(entry, fileBytes, byteOrder, limits, diagnostics);
                if (value.DataType != TiffDataType.Unknown)
                {
                    values = TiffExifMetadataReader.AppendValue(values, value);
                }
            }

            index = index + 1;
        }

        return values;
    }

    internal static int[] GetExtendedInfoTagIds()
    {
        return new int[]
        {
            TiffTagId.NewSubfileType, TiffTagId.SubfileType, TiffTagId.ImageDepth, TiffTagId.TileDepth,
            TiffTagId.Thresholding, TiffTagId.CellWidth, TiffTagId.CellLength,
            TiffTagId.DocumentName, TiffTagId.ImageDescription, TiffTagId.Make, TiffTagId.Model,
            TiffTagId.MinSampleValue, TiffTagId.MaxSampleValue,
            TiffTagId.XResolution, TiffTagId.YResolution, TiffTagId.ResolutionUnit,
            TiffTagId.PageName, TiffTagId.XPosition, TiffTagId.YPosition,
            TiffTagId.GrayResponseUnit, TiffTagId.GrayResponseCurve, TiffTagId.PageNumber, TiffTagId.ColorResponseUnit,
            TiffTagId.TransferFunction, TiffTagId.Software, TiffTagId.DateTime, TiffTagId.Artist,
            TiffTagId.HostComputer, TiffTagId.WhitePoint, TiffTagId.PrimaryChromaticities,
            TiffTagId.HalftoneHints, TiffTagId.InkSet, TiffTagId.InkNames, TiffTagId.NumberOfInks,
            TiffTagId.DotRange, TiffTagId.TargetPrinter, TiffTagId.SMinSampleValue,
            TiffTagId.SMaxSampleValue, TiffTagId.TransferRange, TiffTagId.JPEGTables,
            TiffTagId.YCbCrPositioning, TiffTagId.Copyright,
            TiffTagId.CFARepeatPatternDim, TiffTagId.CFAPattern
        };
    }

    internal static void ApplyExtendedValues(TiffImageInfo info, TiffTagValue[] values, int imageIndex, TiffDiagnostics diagnostics)
    {
        info.ImageDepth = ReadExtendedUnsignedScalar(values, TiffTagId.ImageDepth, 1, imageIndex, diagnostics);
        info.TileDepth = ReadExtendedUnsignedScalar(values, TiffTagId.TileDepth, 1, imageIndex, diagnostics);
        info.NewSubfileType = ReadExtendedUnsignedScalar(values, TiffTagId.NewSubfileType, 0, imageIndex, diagnostics);
        info.SubfileType = ReadExtendedIntScalar(values, TiffTagId.SubfileType, 0, imageIndex, diagnostics);
        info.IsReducedResolution = (info.NewSubfileType & 1) != 0 || info.SubfileType == 2;
        info.IsPage = (info.NewSubfileType & 2) != 0 || info.SubfileType == 3;
        info.IsTransparencyMask = (info.NewSubfileType & 4) != 0;
        info.IsDepthMap = (info.NewSubfileType & 8) != 0;
        info.IsEnhancedImage = (info.NewSubfileType & 16) != 0;

        info.XResolution = ReadExtendedRationalScalar(values, TiffTagId.XResolution, 0d, imageIndex, diagnostics);
        info.YResolution = ReadExtendedRationalScalar(values, TiffTagId.YResolution, 0d, imageIndex, diagnostics);
        info.ResolutionUnit = (TiffResolutionUnit)ReadExtendedIntScalar(values, TiffTagId.ResolutionUnit, (int)TiffResolutionUnit.Inch, imageIndex, diagnostics);
        info.XPosition = ReadExtendedRationalScalar(values, TiffTagId.XPosition, 0d, imageIndex, diagnostics);
        info.YPosition = ReadExtendedRationalScalar(values, TiffTagId.YPosition, 0d, imageIndex, diagnostics);

        info.MinSampleValues = ReadExtendedSampleValues(values, TiffTagId.MinSampleValue, CreateMinimumSampleDefaults(info.SamplesPerPixel), info.SamplesPerPixel, imageIndex, diagnostics);
        info.MaxSampleValues = ReadExtendedSampleValues(values, TiffTagId.MaxSampleValue, CreateMaximumSampleDefaults(info.BitsPerSample, info.SamplesPerPixel), info.SamplesPerPixel, imageIndex, diagnostics);
        info.SMinSampleValues = ReadExtendedSampleValues(values, TiffTagId.SMinSampleValue, new double[0], info.SamplesPerPixel, imageIndex, diagnostics);
        info.SMaxSampleValues = ReadExtendedSampleValues(values, TiffTagId.SMaxSampleValue, new double[0], info.SamplesPerPixel, imageIndex, diagnostics);
        info.WhitePoint = ReadExtendedFixedNumericValues(values, TiffTagId.WhitePoint, 2, imageIndex, diagnostics);
        info.PrimaryChromaticities = ReadExtendedFixedNumericValues(values, TiffTagId.PrimaryChromaticities, 6, imageIndex, diagnostics);

        info.InkSet = ReadExtendedIntScalar(values, TiffTagId.InkSet, 1, imageIndex, diagnostics);
        info.InkNames = ReadExtendedTexts(values, TiffTagId.InkNames, imageIndex, diagnostics);
        info.NumberOfInks = ReadExtendedIntScalar(values, TiffTagId.NumberOfInks, 4, imageIndex, diagnostics);
        info.DotRange = ReadExtendedUShorts(values, TiffTagId.DotRange, imageIndex, diagnostics);
        info.TargetPrinter = ReadExtendedText(values, TiffTagId.TargetPrinter, imageIndex, diagnostics);
        info.TransferFunction = ReadExtendedUShorts(values, TiffTagId.TransferFunction, imageIndex, diagnostics);
        info.TransferRange = ReadExtendedFixedUShorts(values, TiffTagId.TransferRange, 6, imageIndex, diagnostics);
        info.JpegTables = ReadExtendedRawBytes(values, TiffTagId.JPEGTables, imageIndex, diagnostics);
        info.CFARepeatPatternDimensions = ReadExtendedFixedInts(values, TiffTagId.CFARepeatPatternDim, 2, imageIndex, diagnostics);
        info.CFAPattern = ReadExtendedBytes(values, TiffTagId.CFAPattern, imageIndex, diagnostics);

        info.Thresholding = ReadExtendedIntScalar(values, TiffTagId.Thresholding, 1, imageIndex, diagnostics);
        info.CellWidth = ReadExtendedIntScalar(values, TiffTagId.CellWidth, 0, imageIndex, diagnostics);
        info.CellLength = ReadExtendedIntScalar(values, TiffTagId.CellLength, 0, imageIndex, diagnostics);
        info.GrayResponseUnit = ReadExtendedIntScalar(values, TiffTagId.GrayResponseUnit, 2, imageIndex, diagnostics);
        info.GrayResponseCurve = ReadExtendedUShorts(values, TiffTagId.GrayResponseCurve, imageIndex, diagnostics);
        info.ColorResponseUnit = ReadExtendedIntScalar(values, TiffTagId.ColorResponseUnit, 0, imageIndex, diagnostics);
        info.HalftoneHints = ReadExtendedFixedInts(values, TiffTagId.HalftoneHints, 2, imageIndex, diagnostics);
        info.PageNumber = ReadExtendedFixedInts(values, TiffTagId.PageNumber, 2, imageIndex, diagnostics);
        info.YCbCrPositioning = ReadExtendedIntScalar(values, TiffTagId.YCbCrPositioning, 1, imageIndex, diagnostics);
        info.DocumentName = ReadExtendedText(values, TiffTagId.DocumentName, imageIndex, diagnostics);
        info.ImageDescription = ReadExtendedText(values, TiffTagId.ImageDescription, imageIndex, diagnostics);
        info.Make = ReadExtendedText(values, TiffTagId.Make, imageIndex, diagnostics);
        info.Model = ReadExtendedText(values, TiffTagId.Model, imageIndex, diagnostics);
        info.PageName = ReadExtendedText(values, TiffTagId.PageName, imageIndex, diagnostics);
        info.Software = ReadExtendedText(values, TiffTagId.Software, imageIndex, diagnostics);
        info.DateTime = ReadExtendedText(values, TiffTagId.DateTime, imageIndex, diagnostics);
        info.Artist = ReadExtendedText(values, TiffTagId.Artist, imageIndex, diagnostics);
        info.HostComputer = ReadExtendedText(values, TiffTagId.HostComputer, imageIndex, diagnostics);
        info.Copyright = ReadExtendedText(values, TiffTagId.Copyright, imageIndex, diagnostics);
    }

    internal static TiffTagValue? FindExtendedValue(TiffTagValue[] values, int tagId)
    {
        return TiffTagDefaultLookup.FindValue(values, tagId);
    }

    internal static ulong ReadExtendedUnsignedScalar(TiffTagValue[] values, int tagId, ulong defaultValue, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return defaultValue;
        }

        if (!value.IsUnsigned())
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information scalar must use an unsigned integer data type.", imageIndex, tagId);
            return defaultValue;
        }

        if (value.UnsignedValues.Length != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information scalar must contain one value.", imageIndex, tagId);
            return defaultValue;
        }

        return value.UnsignedValues[0];
    }

    internal static int ReadExtendedIntScalar(TiffTagValue[] values, int tagId, int defaultValue, int imageIndex, TiffDiagnostics diagnostics)
    {
        ulong result = ReadExtendedUnsignedScalar(values, tagId, (ulong)defaultValue, imageIndex, diagnostics);
        return ConvertUnsignedValueToInt(result, imageIndex, tagId, diagnostics);
    }

    internal static double ReadExtendedRationalScalar(TiffTagValue[] values, int tagId, double defaultValue, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return defaultValue;
        }

        if (!value.IsRational())
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information value must use a rational data type.", imageIndex, tagId);
            return defaultValue;
        }

        if (value.RationalValues.Length != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information rational scalar must contain one value.", imageIndex, tagId);
            return defaultValue;
        }

        return value.RationalValues[0].ToDouble();
    }

    internal static double[] ReadExtendedSampleValues(TiffTagValue[] values, int tagId, double[] defaultValues, int samplesPerPixel, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return TiffImageInfo.CopyDoubleArray(defaultValues);
        }

        double[] converted = ConvertExtendedNumericValues(value, imageIndex, diagnostics);
        if (samplesPerPixel <= 0 || converted.Length == samplesPerPixel)
        {
            return converted;
        }

        if (converted.Length == 1)
        {
            double[] repeated = new double[samplesPerPixel];
            int repeatIndex = 0;
            while (repeatIndex < repeated.Length)
            {
                repeated[repeatIndex] = converted[0];
                repeatIndex = repeatIndex + 1;
            }

            return repeated;
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Sample value count must be one or match SamplesPerPixel.", imageIndex, tagId);
        return converted;
    }

    internal static double[] ReadExtendedFixedNumericValues(TiffTagValue[] values, int tagId, int expectedCount, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return new double[0];
        }

        double[] converted = ConvertExtendedNumericValues(value, imageIndex, diagnostics);
        if (converted.Length != expectedCount)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information array does not contain the expected number of values.", imageIndex, tagId);
        }

        return converted;
    }

    internal static double[] ConvertExtendedNumericValues(TiffTagValue value, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (value.IsUnsigned())
        {
            double[] unsigned = new double[value.UnsignedValues.Length];
            int unsignedIndex = 0;
            while (unsignedIndex < unsigned.Length)
            {
                unsigned[unsignedIndex] = value.UnsignedValues[unsignedIndex];
                unsignedIndex = unsignedIndex + 1;
            }

            return unsigned;
        }

        if (value.IsSigned())
        {
            double[] signed = new double[value.SignedValues.Length];
            int signedIndex = 0;
            while (signedIndex < signed.Length)
            {
                signed[signedIndex] = value.SignedValues[signedIndex];
                signedIndex = signedIndex + 1;
            }

            return signed;
        }

        if (value.IsRational())
        {
            double[] rational = new double[value.RationalValues.Length];
            int rationalIndex = 0;
            while (rationalIndex < rational.Length)
            {
                rational[rationalIndex] = value.RationalValues[rationalIndex].ToDouble();
                rationalIndex = rationalIndex + 1;
            }

            return rational;
        }

        if (value.IsFloating())
        {
            return TiffImageInfo.CopyDoubleArray(value.FloatingValues);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information value must use a numeric data type.", imageIndex, value.TagId);
        return new double[0];
    }

    internal static ushort[] ReadExtendedUShorts(TiffTagValue[] values, int tagId, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return new ushort[0];
        }

        if (!value.IsUnsigned())
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information array must use an unsigned integer data type.", imageIndex, tagId);
            return new ushort[0];
        }

        ushort[] result = new ushort[value.UnsignedValues.Length];
        int index = 0;
        while (index < result.Length)
        {
            if (value.UnsignedValues[index] > ushort.MaxValue)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "Image information value does not fit in an unsigned short.", imageIndex, tagId);
            }
            else
            {
                result[index] = (ushort)value.UnsignedValues[index];
            }

            index = index + 1;
        }

        return result;
    }

    internal static ushort[] ReadExtendedFixedUShorts(TiffTagValue[] values, int tagId, int expectedCount, int imageIndex, TiffDiagnostics diagnostics)
    {
        ushort[] result = ReadExtendedUShorts(values, tagId, imageIndex, diagnostics);
        if (FindExtendedValue(values, tagId) != null && result.Length != expectedCount)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information array does not contain the expected number of values.", imageIndex, tagId);
        }

        return result;
    }

    internal static int[] ReadExtendedFixedInts(TiffTagValue[] values, int tagId, int expectedCount, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return new int[0];
        }

        if (!value.IsUnsigned())
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information array must use an unsigned integer data type.", imageIndex, tagId);
            return new int[0];
        }

        int[] result = new int[value.UnsignedValues.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = ConvertUnsignedValueToInt(value.UnsignedValues[index], imageIndex, tagId, diagnostics);
            index = index + 1;
        }

        if (result.Length != expectedCount)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information array does not contain the expected number of values.", imageIndex, tagId);
        }

        return result;
    }

    internal static byte[] ReadExtendedBytes(TiffTagValue[] values, int tagId, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return new byte[0];
        }

        if (!value.IsUnsigned())
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information byte array must use an unsigned integer data type.", imageIndex, tagId);
            return new byte[0];
        }

        byte[] result = new byte[value.UnsignedValues.Length];
        int index = 0;
        while (index < result.Length)
        {
            if (value.UnsignedValues[index] > byte.MaxValue)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "Image information value does not fit in a byte.", imageIndex, tagId);
            }
            else
            {
                result[index] = (byte)value.UnsignedValues[index];
            }

            index = index + 1;
        }

        return result;
    }

    internal static byte[] ReadExtendedRawBytes(TiffTagValue[] values, int tagId, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return new byte[0];
        }

        if (!value.IsRaw)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information raw value must use an uninterpreted data type.", imageIndex, tagId);
            return new byte[0];
        }

        return TiffImageInfo.CopyByteArray(value.RawBytes);
    }

    internal static string ReadExtendedText(TiffTagValue[] values, int tagId, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return "";
        }

        if (!value.IsAscii())
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information text must use the ASCII data type.", imageIndex, tagId);
            return "";
        }

        return value.Text;
    }

    internal static string[] ReadExtendedTexts(TiffTagValue[] values, int tagId, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffTagValue? value = FindExtendedValue(values, tagId);
        if (value == null)
        {
            return new string[0];
        }

        if (!value.IsAscii())
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information text must use the ASCII data type.", imageIndex, tagId);
            return new string[0];
        }

        return TiffImageInfo.CopyStringArray(value.TextValues);
    }

    internal static double[] CreateMinimumSampleDefaults(int samplesPerPixel)
    {
        int count = samplesPerPixel;
        if (count <= 0)
        {
            count = 1;
        }

        return new double[count];
    }

    internal static double[] CreateMaximumSampleDefaults(int[] bitsPerSample, int samplesPerPixel)
    {
        int count = samplesPerPixel;
        if (count <= 0)
        {
            count = 1;
        }

        double[] result = new double[count];
        int index = 0;
        while (index < result.Length)
        {
            int bits = 1;
            if (bitsPerSample != null && bitsPerSample.Length > index)
            {
                bits = bitsPerSample[index];
            }

            result[index] = Math.Pow(2d, bits) - 1d;
            index = index + 1;
        }

        return result;
    }

    internal static void ValidateImageDimensions(TiffImageInfo info, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null || info.Width == 0 || info.Length == 0)
        {
            return;
        }

        TiffLimits actualLimits = limits;
        if (actualLimits == null)
        {
            actualLimits = new TiffLimits();
        }

        if (info.Width > int.MaxValue || info.Length > int.MaxValue)
        {
            return;
        }

        if (!actualLimits.AllowsImageDimensions((int)info.Width, (int)info.Length))
        {
            diagnostics.AddError(TiffErrorCode.ImageDimensionLimitExceeded, "Image dimensions exceed the configured limits.");
        }
    }

    internal static ulong ReadRequiredUnsignedScalar(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image information is missing a required tag.", directory.ImageIndex, tagId);
            return 0;
        }

        return ReadUnsignedScalar(entry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
    }

    internal static ulong ReadOptionalUnsignedScalar(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, ulong defaultValue, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return defaultValue;
        }

        return ReadUnsignedScalar(entry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
    }

    internal static int ReadOptionalIntScalar(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, int defaultValue, TiffDiagnostics diagnostics)
    {
        ulong value = ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, limits, tagId, (ulong)defaultValue, diagnostics);
        return ConvertUnsignedValueToInt(value, directory.ImageIndex, tagId, diagnostics);
    }

    internal static int ConvertUnsignedValueToInt(ulong value, int imageIndex, int tagId, TiffDiagnostics diagnostics)
    {
        if (value > int.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "Image information value does not fit in a signed integer.", imageIndex, tagId);
            return 0;
        }

        return (int)value;
    }

    internal static ulong ReadUnsignedScalar(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (entry.Count != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Scalar image information tag must contain one value.", imageIndex, entry.TagId);
            return 0;
        }

        ulong[] values = ReadUnsignedArray(entry, fileBytes, byteOrder, limits, imageIndex, diagnostics);
        if (values.Length == 0)
        {
            return 0;
        }

        return values[0];
    }

    internal static int[] ReadOptionalIntArray(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, int[] defaultValues, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return TiffImageInfo.CopyIntArray(defaultValues);
        }

        ulong[] values = ReadUnsignedArray(entry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
        int[] converted = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] > int.MaxValue)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "Image information value does not fit in a signed integer.", directory.ImageIndex, tagId);
                converted[index] = 0;
            }
            else
            {
                converted[index] = (int)values[index];
            }

            index = index + 1;
        }

        return converted;
    }

    internal static TiffSampleFormat[] ReadOptionalSampleFormatArray(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, TiffSampleFormat[] defaultValues, TiffDiagnostics diagnostics)
    {
        int[] values = ReadOptionalIntArray(directory, fileBytes, byteOrder, limits, tagId, SampleFormatsToInts(defaultValues), diagnostics);
        TiffSampleFormat[] formats = new TiffSampleFormat[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            formats[index] = (TiffSampleFormat)values[index];
            index = index + 1;
        }

        return formats;
    }

    internal static TiffExtraSample[] ReadOptionalExtraSampleArray(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, TiffExtraSample[] defaultValues, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return TiffImageInfo.CopyExtraSamples(defaultValues);
        }

        ulong[] values = ReadUnsignedArray(entry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
        TiffExtraSample[] extras = new TiffExtraSample[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            extras[index] = (TiffExtraSample)ConvertUnsignedValueToInt(values[index], directory.ImageIndex, tagId, diagnostics);
            index = index + 1;
        }

        return extras;
    }

    internal static ushort[] ReadOptionalUShortArray(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, ushort[] defaultValues, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return TiffImageInfo.CopyUShortArray(defaultValues);
        }

        ulong[] values = ReadUnsignedArray(entry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
        ushort[] converted = new ushort[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] > ushort.MaxValue)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "Image information value does not fit in an unsigned short.", directory.ImageIndex, tagId);
                converted[index] = 0;
            }
            else
            {
                converted[index] = (ushort)values[index];
            }

            index = index + 1;
        }

        return converted;
    }

    internal static double[] ReadOptionalRationalDoubleArray(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, double[] defaultValues, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return TiffImageInfo.CopyDoubleArray(defaultValues);
        }

        if (entry.DataType != TiffDataType.Rational && entry.DataType != TiffDataType.SRational)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information rational tag must use RATIONAL or SRATIONAL data type.", directory.ImageIndex, tagId);
            return new double[0];
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return new double[0];
        }

        TiffRationalCodecResult rationalResult = TiffRationalCodec.Decode(loadResult.Bytes, entry.DataType, byteOrder);
        diagnostics.Merge(rationalResult.Diagnostics);
        double[] values = new double[rationalResult.Values.Length];
        int index = 0;
        while (index < rationalResult.Values.Length)
        {
            values[index] = rationalResult.Values[index].ToDouble();
            index = index + 1;
        }

        return values;
    }

    internal static ulong[] ReadUnsignedArray(TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (!IsUnsignedIntegerDataType(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information tag must use an unsigned integer data type.", imageIndex, entry.TagId);
            return new ulong[0];
        }

        if (entry.Count < 0 || entry.Count > int.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information tag count is outside the supported range.", imageIndex, entry.TagId);
            return new ulong[0];
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return new ulong[0];
        }

        int elementSize = TiffDataTypeInfo.GetElementSize(entry.DataType);
        ulong[] values = new ulong[(int)entry.Count];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * elementSize;
            values[index] = ReadUnsignedValue(loadResult.Bytes, offset, entry.DataType, byteOrder);
            index = index + 1;
        }

        return values;
    }

    internal static ulong ReadUnsignedValue(byte[] bytes, int offset, TiffDataType dataType, TiffByteOrder byteOrder)
    {
        if (dataType == TiffDataType.Byte)
        {
            return bytes[offset];
        }

        if (dataType == TiffDataType.Short)
        {
            return TiffBinaryReader.ReadUInt16(bytes.AsSpan(offset, 2), byteOrder);
        }

        if (dataType == TiffDataType.Long || dataType == TiffDataType.Ifd)
        {
            return TiffBinaryReader.ReadUInt32(bytes.AsSpan(offset, 4), byteOrder);
        }

        return TiffBinaryReader.ReadUInt64(bytes.AsSpan(offset, 8), byteOrder);
    }

    internal static bool IsUnsignedIntegerDataType(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Byte:
            case TiffDataType.Short:
            case TiffDataType.Long:
            case TiffDataType.Ifd:
            case TiffDataType.Long8:
            case TiffDataType.Ifd8:
                return true;
            default:
                return false;
        }
    }

    internal static int[] NormalizeIntArray(int[] values, int expectedCount, int tagId, TiffDiagnostics diagnostics)
    {
        if (expectedCount <= 0)
        {
            return TiffImageInfo.CopyIntArray(values);
        }

        if (values.Length == expectedCount)
        {
            return TiffImageInfo.CopyIntArray(values);
        }

        if (values.Length == 1)
        {
            return CreateFilledIntArray(values[0], expectedCount);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information array count does not match SamplesPerPixel.", TiffDiagnostic.NoImageIndex, tagId);
        return TiffImageInfo.CopyIntArray(values);
    }

    internal static TiffSampleFormat[] NormalizeSampleFormats(TiffSampleFormat[] values, int expectedCount, TiffDiagnostics diagnostics)
    {
        if (expectedCount <= 0)
        {
            return TiffImageInfo.CopySampleFormats(values);
        }

        if (values.Length == expectedCount)
        {
            return TiffImageInfo.CopySampleFormats(values);
        }

        if (values.Length == 1)
        {
            return CreateFilledSampleFormatArray(values[0], expectedCount);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "SampleFormat count does not match SamplesPerPixel.", TiffDiagnostic.NoImageIndex, TiffTagId.SampleFormat);
        return TiffImageInfo.CopySampleFormats(values);
    }

    internal static double[] NormalizeDoubleArray(double[] values, int expectedCount, int tagId, TiffDiagnostics diagnostics)
    {
        if (values.Length == expectedCount)
        {
            return TiffImageInfo.CopyDoubleArray(values);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information rational array count does not match the expected count.", TiffDiagnostic.NoImageIndex, tagId);
        return TiffImageInfo.CopyDoubleArray(values);
    }

    internal static bool HasAnyTileTag(TiffDirectory directory)
    {
        if (directory == null)
        {
            return false;
        }

        if (directory.FindEntry(TiffTagId.TileWidth) != null)
        {
            return true;
        }

        if (directory.FindEntry(TiffTagId.TileLength) != null)
        {
            return true;
        }

        if (directory.FindEntry(TiffTagId.TileOffsets) != null)
        {
            return true;
        }

        return directory.FindEntry(TiffTagId.TileByteCounts) != null;
    }

    internal static int[] CreateFilledIntArray(int value, int count)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = value;
            index = index + 1;
        }

        return values;
    }

    internal static TiffSampleFormat[] CreateFilledSampleFormatArray(TiffSampleFormat value, int count)
    {
        TiffSampleFormat[] values = new TiffSampleFormat[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = value;
            index = index + 1;
        }

        return values;
    }

    internal static int[] SampleFormatsToInts(TiffSampleFormat[] values)
    {
        int[] converted = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            converted[index] = (int)values[index];
            index = index + 1;
        }

        return converted;
    }

    internal static int[] CreateYCbCrSubSamplingDefault()
    {
        int[] values = new int[2];
        values[0] = 2;
        values[1] = 2;
        return values;
    }

    internal static double[] CreateYCbCrCoefficientsDefault()
    {
        double[] values = new double[3];
        values[0] = 0.299d;
        values[1] = 0.587d;
        values[2] = 0.114d;
        return values;
    }

    internal static double[] CreateReferenceBlackWhiteDefault()
    {
        double[] values = new double[6];
        values[0] = 0d;
        values[1] = 255d;
        values[2] = 128d;
        values[3] = 255d;
        values[4] = 128d;
        values[5] = 255d;
        return values;
    }
}
