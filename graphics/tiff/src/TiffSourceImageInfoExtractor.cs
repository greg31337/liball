namespace tiff;

internal class TiffSourceImageInfoExtractor
{
    internal static TiffImageInfoResult Extract(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffReadOptions options)
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
        info.Width = ReadRequiredUnsignedScalar(source, directory, byteOrder, limits, TiffTagId.ImageWidth, diagnostics);
        info.Length = ReadRequiredUnsignedScalar(source, directory, byteOrder, limits, TiffTagId.ImageLength, diagnostics);
        TiffImageInfoExtractor.ValidateImageDimensions(info, limits, diagnostics);
        info.SamplesPerPixel = ReadOptionalIntScalar(source, directory, byteOrder, limits, TiffTagId.SamplesPerPixel, 1, diagnostics);
        info.BitsPerSample = TiffImageInfoExtractor.NormalizeIntArray(ReadOptionalIntArray(source, directory, byteOrder, limits, TiffTagId.BitsPerSample, TiffImageInfoExtractor.CreateFilledIntArray(1, 1), diagnostics), info.SamplesPerPixel, TiffTagId.BitsPerSample, diagnostics);
        info.Compression = (TiffCompression)ReadOptionalIntScalar(source, directory, byteOrder, limits, TiffTagId.Compression, (int)TiffCompression.None, diagnostics);
        info.FillOrder = ReadOptionalIntScalar(source, directory, byteOrder, limits, TiffTagId.FillOrder, 1, diagnostics);
        info.Photometric = (TiffPhotometric)TiffImageInfoExtractor.ConvertUnsignedValueToInt(ReadRequiredUnsignedScalar(source, directory, byteOrder, limits, TiffTagId.PhotometricInterpretation, diagnostics), directory.ImageIndex, TiffTagId.PhotometricInterpretation, diagnostics);
        info.PlanarConfiguration = (TiffPlanarConfiguration)ReadOptionalIntScalar(source, directory, byteOrder, limits, TiffTagId.PlanarConfiguration, (int)TiffPlanarConfiguration.Contiguous, diagnostics);
        info.SampleFormats = TiffImageInfoExtractor.NormalizeSampleFormats(ReadOptionalSampleFormatArray(source, directory, byteOrder, limits, TiffTagId.SampleFormat, TiffImageInfoExtractor.CreateFilledSampleFormatArray(TiffSampleFormat.UnsignedInteger, info.SamplesPerPixel), diagnostics), info.SamplesPerPixel, diagnostics);
        info.ExtraSamples = ReadOptionalExtraSampleArray(source, directory, byteOrder, limits, TiffTagId.ExtraSamples, new TiffExtraSample[0], diagnostics);
        info.ColorMap = ReadOptionalUShortArray(source, directory, byteOrder, limits, TiffTagId.ColorMap, new ushort[0], diagnostics);
        info.Orientation = (TiffOrientation)ReadOptionalIntScalar(source, directory, byteOrder, limits, TiffTagId.Orientation, (int)TiffOrientation.TopLeft, diagnostics);
        info.IsTiled = TiffImageInfoExtractor.HasAnyTileTag(directory);
        info.RowsPerStrip = ReadOptionalUnsignedScalar(source, directory, byteOrder, limits, TiffTagId.RowsPerStrip, uint.MaxValue, diagnostics);
        info.TileWidth = ReadOptionalUnsignedScalar(source, directory, byteOrder, limits, TiffTagId.TileWidth, 0, diagnostics);
        info.TileLength = ReadOptionalUnsignedScalar(source, directory, byteOrder, limits, TiffTagId.TileLength, 0, diagnostics);
        info.Predictor = ReadOptionalIntScalar(source, directory, byteOrder, limits, TiffTagId.Predictor, 1, diagnostics);
        info.T4Options = ReadOptionalIntScalar(source, directory, byteOrder, limits, TiffTagId.T4Options, 0, diagnostics);
        info.T6Options = ReadOptionalIntScalar(source, directory, byteOrder, limits, TiffTagId.T6Options, 0, diagnostics);
        info.YCbCrCoefficients = TiffImageInfoExtractor.NormalizeDoubleArray(ReadOptionalRationalDoubleArray(source, directory, byteOrder, limits, TiffTagId.YCbCrCoefficients, TiffImageInfoExtractor.CreateYCbCrCoefficientsDefault(), diagnostics), 3, TiffTagId.YCbCrCoefficients, diagnostics);
        int[] subsampling = ReadOptionalIntArray(source, directory, byteOrder, limits, TiffTagId.YCbCrSubSampling, TiffImageInfoExtractor.CreateYCbCrSubSamplingDefault(), diagnostics);
        if (subsampling.Length >= 2)
        {
            info.YCbCrSubSamplingHorizontal = subsampling[0];
            info.YCbCrSubSamplingVertical = subsampling[1];
        }
        else
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "YCbCrSubSampling must contain two values.", directory.ImageIndex, TiffTagId.YCbCrSubSampling);
        }

        info.ReferenceBlackWhite = TiffImageInfoExtractor.NormalizeDoubleArray(ReadOptionalRationalDoubleArray(source, directory, byteOrder, limits, TiffTagId.ReferenceBlackWhite, TiffImageInfoExtractor.CreateReferenceBlackWhiteDefault(), diagnostics), 6, TiffTagId.ReferenceBlackWhite, diagnostics);
        TiffTagValue[] extendedValues = ReadExtendedValues(source, directory, byteOrder, limits, diagnostics);
        TiffImageInfoExtractor.ApplyExtendedValues(info, extendedValues, directory.ImageIndex, diagnostics);
        return TiffImageInfoResult.Create(info, diagnostics);
    }

    internal static TiffTagValue[] ReadExtendedValues(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] values = new TiffTagValue[0];
        int[] tagIds = TiffImageInfoExtractor.GetExtendedInfoTagIds();
        int index = 0;
        while (index < tagIds.Length)
        {
            TiffIfdEntry? entry = directory.FindEntry(tagIds[index]);
            if (entry != null)
            {
                TiffTagValue value = TiffExifMetadataReader.ReadSourceValue(entry, source, byteOrder, limits, diagnostics);
                if (value.DataType != TiffDataType.Unknown)
                {
                    values = TiffExifMetadataReader.AppendValue(values, value);
                }
            }

            index = index + 1;
        }

        return values;
    }

    internal static ulong ReadRequiredUnsignedScalar(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, int tagId, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image information is missing a required tag.", directory.ImageIndex, tagId);
            return 0;
        }

        return ReadUnsignedScalar(source, entry, byteOrder, limits, directory.ImageIndex, diagnostics);
    }

    internal static ulong ReadOptionalUnsignedScalar(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, int tagId, ulong defaultValue, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return defaultValue;
        }

        return ReadUnsignedScalar(source, entry, byteOrder, limits, directory.ImageIndex, diagnostics);
    }

    internal static ulong ReadUnsignedScalar(ITiffRandomAccessSource source, TiffIfdEntry entry, TiffByteOrder byteOrder, TiffLimits limits, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (entry.Count != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Scalar image information tag must contain one value.", imageIndex, entry.TagId);
            return 0;
        }

        ulong[] values = ReadUnsignedArray(source, entry, byteOrder, limits, imageIndex, diagnostics);
        if (values.Length == 0)
        {
            return 0;
        }

        return values[0];
    }

    internal static int ReadOptionalIntScalar(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, int tagId, int defaultValue, TiffDiagnostics diagnostics)
    {
        ulong value = ReadOptionalUnsignedScalar(source, directory, byteOrder, limits, tagId, (ulong)defaultValue, diagnostics);
        return TiffImageInfoExtractor.ConvertUnsignedValueToInt(value, directory.ImageIndex, tagId, diagnostics);
    }

    internal static int[] ReadOptionalIntArray(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, int tagId, int[] defaultValues, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return TiffImageInfo.CopyIntArray(defaultValues);
        }

        ulong[] values = ReadUnsignedArray(source, entry, byteOrder, limits, directory.ImageIndex, diagnostics);
        int[] converted = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            converted[index] = TiffImageInfoExtractor.ConvertUnsignedValueToInt(values[index], directory.ImageIndex, tagId, diagnostics);
            index = index + 1;
        }

        return converted;
    }

    internal static TiffSampleFormat[] ReadOptionalSampleFormatArray(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, int tagId, TiffSampleFormat[] defaultValues, TiffDiagnostics diagnostics)
    {
        int[] values = ReadOptionalIntArray(source, directory, byteOrder, limits, tagId, TiffImageInfoExtractor.SampleFormatsToInts(defaultValues), diagnostics);
        TiffSampleFormat[] formats = new TiffSampleFormat[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            formats[index] = (TiffSampleFormat)values[index];
            index = index + 1;
        }

        return formats;
    }

    internal static TiffExtraSample[] ReadOptionalExtraSampleArray(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, int tagId, TiffExtraSample[] defaultValues, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return TiffImageInfo.CopyExtraSamples(defaultValues);
        }

        ulong[] values = ReadUnsignedArray(source, entry, byteOrder, limits, directory.ImageIndex, diagnostics);
        TiffExtraSample[] converted = new TiffExtraSample[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            converted[index] = (TiffExtraSample)TiffImageInfoExtractor.ConvertUnsignedValueToInt(values[index], directory.ImageIndex, tagId, diagnostics);
            index = index + 1;
        }

        return converted;
    }

    internal static ushort[] ReadOptionalUShortArray(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, int tagId, ushort[] defaultValues, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return TiffImageInfo.CopyUShortArray(defaultValues);
        }

        ulong[] values = ReadUnsignedArray(source, entry, byteOrder, limits, directory.ImageIndex, diagnostics);
        ushort[] converted = new ushort[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] > ushort.MaxValue)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "Image information value does not fit in an unsigned short.", directory.ImageIndex, tagId);
            }
            else
            {
                converted[index] = (ushort)values[index];
            }

            index = index + 1;
        }

        return converted;
    }

    internal static double[] ReadOptionalRationalDoubleArray(ITiffRandomAccessSource source, TiffDirectory directory, TiffByteOrder byteOrder, TiffLimits limits, int tagId, double[] defaultValues, TiffDiagnostics diagnostics)
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

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadSourceBytes(entry, source, limits);
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

    internal static ulong[] ReadUnsignedArray(ITiffRandomAccessSource source, TiffIfdEntry entry, TiffByteOrder byteOrder, TiffLimits limits, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (!TiffImageInfoExtractor.IsUnsignedIntegerDataType(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Image information tag must use an unsigned integer data type.", imageIndex, entry.TagId);
            return new ulong[0];
        }

        if (entry.Count < 0 || entry.Count > int.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Image information tag count is outside the supported range.", imageIndex, entry.TagId);
            return new ulong[0];
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadSourceBytes(entry, source, limits);
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
            values[index] = TiffImageInfoExtractor.ReadUnsignedValue(loadResult.Bytes, index * elementSize, entry.DataType, byteOrder);
            index = index + 1;
        }

        return values;
    }
}
