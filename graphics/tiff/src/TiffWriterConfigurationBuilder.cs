namespace tiff;

internal class TiffWriterConfigurationBuilder
{
    internal static bool Validate(TiffWriterImageConfiguration configuration, TiffWriteOptions options, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (configuration == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A writer image configuration is required.");
            return false;
        }

        ValidateDimensions(configuration, options, imageIndex, diagnostics);
        ValidateByteOrder(configuration, diagnostics);
        ValidateCompressionAndPredictor(configuration, imageIndex, diagnostics);
        ValidatePixelFormat(configuration.PixelFormat, imageIndex, diagnostics);
        ValidateLayout(configuration, imageIndex, diagnostics);
        if (!diagnostics.HasErrors())
        {
            int segmentCount = CalculateSegmentCount(configuration, diagnostics);
            long segmentArrayBytes = (long)segmentCount * 8L;
            if (segmentCount <= 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidTagValue, "The configured image must contain at least one segment.");
            }
            else if (!options.Limits.AllowsAllocation(segmentArrayBytes))
            {
                diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "The configured segment array exceeds the writer allocation limit.");
            }
        }

        return !diagnostics.HasErrors();
    }

    internal static TiffImageDefinition BuildDefinition(TiffWriterImageConfiguration configuration)
    {
        TiffImageDefinition definition = new TiffImageDefinition();
        TiffWriterPixelFormat pixelFormat = configuration.PixelFormat;
        TiffWriterLayoutConfiguration layout = configuration.Layout;
        int rowsPerStrip = GetRowsPerStrip(configuration);
        definition.Tags = AddTag(definition.Tags, CreateLongScalar(TiffTagId.ImageWidth, configuration.Width));
        definition.Tags = AddTag(definition.Tags, CreateLongScalar(TiffTagId.ImageLength, configuration.Length));
        definition.Tags = AddTag(definition.Tags, CreateShortArray(TiffTagId.BitsPerSample, NormalizeIntValues(pixelFormat.BitsPerSample, pixelFormat.SamplesPerPixel)));
        definition.Tags = AddTag(definition.Tags, CreateShortScalar(TiffTagId.Compression, (int)configuration.Compression));
        definition.Tags = AddTag(definition.Tags, CreateShortScalar(TiffTagId.PhotometricInterpretation, (int)pixelFormat.Photometric));
        definition.Tags = AddTag(definition.Tags, CreateShortScalar(TiffTagId.SamplesPerPixel, pixelFormat.SamplesPerPixel));
        definition.Tags = AddTag(definition.Tags, CreateShortScalar(TiffTagId.PlanarConfiguration, (int)layout.PlanarConfiguration));
        definition.Tags = AddTag(definition.Tags, CreateShortArray(TiffTagId.SampleFormat, NormalizeSampleFormatValues(pixelFormat.SampleFormats, pixelFormat.SamplesPerPixel)));
        definition.Tags = AddTag(definition.Tags, CreateShortScalar(TiffTagId.Predictor, configuration.Predictor));
        definition.Tags = AddTag(definition.Tags, CreateLongScalar(TiffTagId.ImageDepth, configuration.Depth));

        if (pixelFormat.ExtraSamples != null && pixelFormat.ExtraSamples.Length > 0)
        {
            definition.Tags = AddTag(definition.Tags, CreateShortArray(TiffTagId.ExtraSamples, ConvertExtraSamples(pixelFormat.ExtraSamples)));
        }

        int segmentCount = CalculateSegmentCount(configuration, new TiffDiagnostics());
        if (layout.Kind == TiffWriterLayoutKind.Tiles)
        {
            definition.Tags = AddTag(definition.Tags, CreateLongScalar(TiffTagId.TileWidth, layout.TileWidth));
            definition.Tags = AddTag(definition.Tags, CreateLongScalar(TiffTagId.TileLength, layout.TileLength));
            definition.Tags = AddTag(definition.Tags, CreateLongScalar(TiffTagId.TileDepth, layout.TileDepth));
            definition.RawTiles = CreateEmptySegments(segmentCount);
        }
        else
        {
            definition.Tags = AddTag(definition.Tags, CreateLongScalar(TiffTagId.RowsPerStrip, rowsPerStrip));
            definition.RawStrips = CreateEmptySegments(segmentCount);
        }

        return definition;
    }

    internal static TiffImageInfo BuildImageInfo(TiffWriterImageConfiguration configuration, int imageIndex)
    {
        TiffImageInfo info = new TiffImageInfo();
        info.ImageIndex = imageIndex;
        info.Width = (ulong)configuration.Width;
        info.Length = (ulong)configuration.Length;
        info.ImageDepth = (ulong)configuration.Depth;
        info.SamplesPerPixel = configuration.PixelFormat.SamplesPerPixel;
        info.BitsPerSample = NormalizeBitsPerSample(configuration.PixelFormat.BitsPerSample, configuration.PixelFormat.SamplesPerPixel);
        info.SampleFormats = NormalizeSampleFormats(configuration.PixelFormat.SampleFormats, configuration.PixelFormat.SamplesPerPixel);
        info.Photometric = configuration.PixelFormat.Photometric;
        info.ExtraSamples = TiffImageInfo.CopyExtraSamples(configuration.PixelFormat.ExtraSamples);
        info.Compression = configuration.Compression;
        info.Predictor = configuration.Predictor;
        info.PlanarConfiguration = configuration.Layout.PlanarConfiguration;
        info.IsTiled = configuration.Layout.Kind == TiffWriterLayoutKind.Tiles;
        info.RowsPerStrip = (ulong)GetRowsPerStrip(configuration);
        if (info.IsTiled)
        {
            info.TileWidth = (ulong)configuration.Layout.TileWidth;
            info.TileLength = (ulong)configuration.Layout.TileLength;
            info.TileDepth = (ulong)configuration.Layout.TileDepth;
        }

        return info;
    }

    internal static int CalculateSegmentCount(TiffWriterImageConfiguration configuration, TiffDiagnostics diagnostics)
    {
        long segmentsPerPlane;
        if (configuration.Layout.Kind == TiffWriterLayoutKind.Tiles)
        {
            long across = DivideRoundUp(configuration.Width, configuration.Layout.TileWidth);
            long down = DivideRoundUp(configuration.Length, configuration.Layout.TileLength);
            long deep = DivideRoundUp(configuration.Depth, configuration.Layout.TileDepth);
            if (!CanMultiply(across, down) || !CanMultiply(across * down, deep))
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Configured tile count overflows the supported range.");
                return 0;
            }

            segmentsPerPlane = across * down * deep;
        }
        else
        {
            segmentsPerPlane = DivideRoundUp(configuration.Length, GetRowsPerStrip(configuration));
        }

        long planeCount = 1;
        if (configuration.Layout.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            planeCount = configuration.PixelFormat.SamplesPerPixel;
        }

        if (!CanMultiply(segmentsPerPlane, planeCount) || segmentsPerPlane * planeCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Configured segment count exceeds the supported array length.");
            return 0;
        }

        return (int)(segmentsPerPlane * planeCount);
    }

    internal static void ValidateDimensions(TiffWriterImageConfiguration configuration, TiffWriteOptions options, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (configuration.Width <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Configured image width must be positive.", imageIndex, TiffTagId.ImageWidth);
        }

        if (configuration.Length <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Configured image length must be positive.", imageIndex, TiffTagId.ImageLength);
        }

        if (configuration.Depth <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Configured image depth must be positive.", imageIndex, TiffTagId.ImageDepth);
        }

        if (configuration.Width > 0 && configuration.Length > 0 && !options.Limits.AllowsImageDimensions(configuration.Width, configuration.Length))
        {
            diagnostics.AddError(TiffErrorCode.ImageDimensionLimitExceeded, "Configured image dimensions exceed the writer limits.");
        }
    }

    internal static void ValidateByteOrder(TiffWriterImageConfiguration configuration, TiffDiagnostics diagnostics)
    {
        if (configuration.ByteOrder != TiffByteOrder.Unknown && configuration.ByteOrder != TiffByteOrder.LittleEndian && configuration.ByteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Configured byte order is not a recognized TIFF byte order.");
        }
    }

    internal static void ValidateCompressionAndPredictor(TiffWriterImageConfiguration configuration, int imageIndex, TiffDiagnostics diagnostics)
    {
        int compression = (int)configuration.Compression;
        if (compression <= 0 || compression > 65535)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.UnsupportedCompression, "Configured compression must contain a valid TIFF compression identifier.", imageIndex, TiffTagId.Compression);
        }

        if (configuration.Predictor < 1 || configuration.Predictor > 3)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Configured predictor must be 1, 2, or 3.", imageIndex, TiffTagId.Predictor);
        }
    }

    internal static void ValidatePixelFormat(TiffWriterPixelFormat pixelFormat, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (pixelFormat == null)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "A source pixel format is required.");
            return;
        }

        if (pixelFormat.SamplesPerPixel <= 0 || pixelFormat.SamplesPerPixel > 65535)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be between 1 and 65535.", imageIndex, TiffTagId.SamplesPerPixel);
        }

        ValidateBitsPerSample(pixelFormat, imageIndex, diagnostics);
        ValidateSampleFormats(pixelFormat, imageIndex, diagnostics);
        int colorSampleCount = GetColorSampleCount(pixelFormat.Photometric);
        if (colorSampleCount == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.UnsupportedPhotometric, "Configured photometric interpretation is not recognized.", imageIndex, TiffTagId.PhotometricInterpretation);
        }

        int extraSampleCount = 0;
        if (pixelFormat.ExtraSamples != null)
        {
            extraSampleCount = pixelFormat.ExtraSamples.Length;
            int index = 0;
            while (index < pixelFormat.ExtraSamples.Length)
            {
                if (pixelFormat.ExtraSamples[index] != TiffExtraSample.Unspecified && pixelFormat.ExtraSamples[index] != TiffExtraSample.AssociatedAlpha && pixelFormat.ExtraSamples[index] != TiffExtraSample.UnassociatedAlpha)
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ExtraSamples contains an unrecognized alpha or extra-sample value.", imageIndex, TiffTagId.ExtraSamples);
                }

                index = index + 1;
            }
        }

        if (colorSampleCount > 0 && pixelFormat.SamplesPerPixel > 0 && pixelFormat.SamplesPerPixel != colorSampleCount + extraSampleCount)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "ExtraSamples must describe every sample beyond the photometric color samples.", imageIndex, TiffTagId.ExtraSamples);
        }
    }

    internal static void ValidateBitsPerSample(TiffWriterPixelFormat pixelFormat, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (pixelFormat.BitsPerSample == null || (pixelFormat.BitsPerSample.Length != 1 && pixelFormat.BitsPerSample.Length != pixelFormat.SamplesPerPixel))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BitsPerSample must contain one value or one value per sample.", imageIndex, TiffTagId.BitsPerSample);
            return;
        }

        int index = 0;
        while (index < pixelFormat.BitsPerSample.Length)
        {
            if (pixelFormat.BitsPerSample[index] <= 0 || pixelFormat.BitsPerSample[index] > 65535)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "BitsPerSample values must be between 1 and 65535.", imageIndex, TiffTagId.BitsPerSample);
            }

            index = index + 1;
        }
    }

    internal static void ValidateSampleFormats(TiffWriterPixelFormat pixelFormat, int imageIndex, TiffDiagnostics diagnostics)
    {
        if (pixelFormat.SampleFormats == null || (pixelFormat.SampleFormats.Length != 1 && pixelFormat.SampleFormats.Length != pixelFormat.SamplesPerPixel))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "SampleFormats must contain one value or one value per sample.", imageIndex, TiffTagId.SampleFormat);
            return;
        }

        int index = 0;
        while (index < pixelFormat.SampleFormats.Length)
        {
            int value = (int)pixelFormat.SampleFormats[index];
            if (value < (int)TiffSampleFormat.UnsignedInteger || value > (int)TiffSampleFormat.ComplexIeeeFloatingPoint)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SampleFormats contains an unrecognized TIFF sample format.", imageIndex, TiffTagId.SampleFormat);
            }

            index = index + 1;
        }
    }

    internal static void ValidateLayout(TiffWriterImageConfiguration configuration, int imageIndex, TiffDiagnostics diagnostics)
    {
        TiffWriterLayoutConfiguration layout = configuration.Layout;
        if (layout == null)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "A writer strip or tile layout is required.");
            return;
        }

        if (layout.PlanarConfiguration != TiffPlanarConfiguration.Contiguous && layout.PlanarConfiguration != TiffPlanarConfiguration.Separate)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Configured planar organization must be contiguous or separate.", imageIndex, TiffTagId.PlanarConfiguration);
        }

        if (layout.Kind == TiffWriterLayoutKind.Strips)
        {
            if (layout.RowsPerStrip < 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "RowsPerStrip cannot be negative.", imageIndex, TiffTagId.RowsPerStrip);
            }
        }
        else if (layout.Kind == TiffWriterLayoutKind.Tiles)
        {
            if (layout.TileWidth <= 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Configured tile width must be positive.", imageIndex, TiffTagId.TileWidth);
            }

            if (layout.TileLength <= 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Configured tile length must be positive.", imageIndex, TiffTagId.TileLength);
            }

            if (layout.TileDepth <= 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Configured tile depth must be positive.", imageIndex, TiffTagId.TileDepth);
            }
        }
        else
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Configured writer layout kind must select strips or tiles.");
        }
    }

    internal static int GetRowsPerStrip(TiffWriterImageConfiguration configuration)
    {
        if (configuration.Layout.RowsPerStrip == 0)
        {
            return configuration.Length;
        }

        return configuration.Layout.RowsPerStrip;
    }

    internal static int GetColorSampleCount(TiffPhotometric photometric)
    {
        switch (photometric)
        {
            case TiffPhotometric.Rgb:
            case TiffPhotometric.YCbCr:
            case TiffPhotometric.CieLab:
            case TiffPhotometric.IccLab:
            case TiffPhotometric.ItuLab:
            case TiffPhotometric.LogLuv:
                return 3;
            case TiffPhotometric.Cmyk:
                return 4;
            case TiffPhotometric.WhiteIsZero:
            case TiffPhotometric.BlackIsZero:
            case TiffPhotometric.Palette:
            case TiffPhotometric.TransparencyMask:
            case TiffPhotometric.ColorFilterArray:
            case TiffPhotometric.LogL:
            case TiffPhotometric.LinearRaw:
                return 1;
            default:
                return 0;
        }
    }

    internal static long DivideRoundUp(int value, int divisor)
    {
        return ((long)value + (long)divisor - 1L) / (long)divisor;
    }

    internal static bool CanMultiply(long left, long right)
    {
        if (left < 0 || right < 0)
        {
            return false;
        }

        if (left == 0 || right == 0)
        {
            return true;
        }

        return left <= long.MaxValue / right;
    }

    internal static byte[][] CreateEmptySegments(int count)
    {
        byte[][] segments = new byte[count][];
        int index = 0;
        while (index < segments.Length)
        {
            segments[index] = new byte[0];
            index = index + 1;
        }

        return segments;
    }

    internal static ulong[] NormalizeIntValues(int[] values, int count)
    {
        ulong[] normalized = new ulong[count];
        int index = 0;
        while (index < normalized.Length)
        {
            int sourceIndex = index;
            if (values.Length == 1)
            {
                sourceIndex = 0;
            }

            normalized[index] = (ulong)values[sourceIndex];
            index = index + 1;
        }

        return normalized;
    }

    internal static ulong[] NormalizeSampleFormatValues(TiffSampleFormat[] values, int count)
    {
        ulong[] normalized = new ulong[count];
        int index = 0;
        while (index < normalized.Length)
        {
            int sourceIndex = index;
            if (values.Length == 1)
            {
                sourceIndex = 0;
            }

            normalized[index] = (ulong)values[sourceIndex];
            index = index + 1;
        }

        return normalized;
    }

    internal static int[] NormalizeBitsPerSample(int[] values, int count)
    {
        int[] normalized = new int[count];
        int index = 0;
        while (index < normalized.Length)
        {
            int sourceIndex = index;
            if (values.Length == 1)
            {
                sourceIndex = 0;
            }

            normalized[index] = values[sourceIndex];
            index = index + 1;
        }

        return normalized;
    }

    internal static TiffSampleFormat[] NormalizeSampleFormats(TiffSampleFormat[] values, int count)
    {
        TiffSampleFormat[] normalized = new TiffSampleFormat[count];
        int index = 0;
        while (index < normalized.Length)
        {
            int sourceIndex = index;
            if (values.Length == 1)
            {
                sourceIndex = 0;
            }

            normalized[index] = values[sourceIndex];
            index = index + 1;
        }

        return normalized;
    }

    internal static ulong[] ConvertExtraSamples(TiffExtraSample[] values)
    {
        ulong[] converted = new ulong[values.Length];
        int index = 0;
        while (index < converted.Length)
        {
            converted[index] = (ulong)values[index];
            index = index + 1;
        }

        return converted;
    }

    internal static TiffTagValue[] AddTag(TiffTagValue[] values, TiffTagValue value)
    {
        return TiffImageDefinition.SetTagValue(values, value);
    }

    internal static TiffTagValue CreateLongScalar(int tagId, int value)
    {
        return TiffTagValue.CreateUnsignedScalar(tagId, TiffDataType.Long, (ulong)value);
    }

    internal static TiffTagValue CreateShortScalar(int tagId, int value)
    {
        return TiffTagValue.CreateUnsignedScalar(tagId, TiffDataType.Short, (ulong)value);
    }

    internal static TiffTagValue CreateShortArray(int tagId, ulong[] values)
    {
        return TiffTagValue.CreateUnsignedArray(tagId, TiffDataType.Short, values);
    }
}
