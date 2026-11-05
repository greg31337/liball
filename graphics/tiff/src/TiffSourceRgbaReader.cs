using System;

namespace tiff;

internal class TiffSourceRgbaReader
{
    internal static TiffRgba8ReadResult ReadRgba8(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRgbaReadOptions options)
    {
        return ReadRgba8WithCodec(info, offsets, byteCounts, source, byteOrder, limits, options, null);
    }

    internal static TiffRgba8ReadResult ReadRgba8WithCodec(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRgbaReadOptions options, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRgbaReadOptions actualOptions = NormalizeOptions(options, diagnostics);
        TiffImageInfo sourceInfo = PrepareInfo(info, actualOptions, diagnostics);
        if (ShouldSkipUnsupportedCompression(sourceInfo, actualOptions, diagnostics, compressionCodec))
        {
            return CreateSkippedRgba8(info, limits, actualOptions, diagnostics);
        }

        double[] samples = ReadSamples(sourceInfo, offsets, byteCounts, source, byteOrder, limits, actualOptions, diagnostics, compressionCodec);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        TiffImageInfo conversionInfo = TiffRgba8Reader.CreateFullContiguousInfo(sourceInfo);
        if (ShouldSkipUnsupportedPhotometric(conversionInfo, actualOptions, diagnostics))
        {
            return CreateSkippedRgba8(info, limits, actualOptions, diagnostics);
        }

        PreparePhotometric(conversionInfo, actualOptions, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        double[] scaledSamples = ApplyMinMaxScaling(conversionInfo, samples, actualOptions, diagnostics);
        byte[] rgba = TiffRgba8Reader.ConvertSamplesToRgba8(conversionInfo, scaledSamples, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        if (actualOptions.AlphaAssociation == TiffExtraSample.AssociatedAlpha)
        {
            rgba = TiffAlphaHandler.AssociateRgba8(rgba);
        }

        TiffOrientation orientation = TiffOrientation.TopLeft;
        if (actualOptions.ApplyOrientation)
        {
            orientation = info.Orientation;
        }

        TiffOrientationTransformResult oriented = TiffOrientationTransformer.TransformInterleavedBytes(info.Width, info.Length, 4, orientation, rgba, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        return TiffRgba8ReadResult.Create(oriented.Width, oriented.Length, oriented.RowByteCount, oriented.TotalByteCount, oriented.Bytes, diagnostics);
    }

    internal static TiffRgba16ReadResult ReadRgba16(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRgbaReadOptions options)
    {
        return ReadRgba16WithCodec(info, offsets, byteCounts, source, byteOrder, limits, options, null);
    }

    internal static TiffRgba16ReadResult ReadRgba16WithCodec(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRgbaReadOptions options, ITiffCompressionCodec? compressionCodec)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRgbaReadOptions actualOptions = NormalizeOptions(options, diagnostics);
        TiffImageInfo sourceInfo = PrepareInfo(info, actualOptions, diagnostics);
        if (ShouldSkipUnsupportedCompression(sourceInfo, actualOptions, diagnostics, compressionCodec))
        {
            return CreateSkippedRgba16(info, limits, actualOptions, diagnostics);
        }

        double[] samples = ReadSamples(sourceInfo, offsets, byteCounts, source, byteOrder, limits, actualOptions, diagnostics, compressionCodec);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(info.Width, info.Length, 0, 0, new ushort[0], diagnostics);
        }

        TiffImageInfo conversionInfo = TiffRgba8Reader.CreateFullContiguousInfo(sourceInfo);
        if (ShouldSkipUnsupportedPhotometric(conversionInfo, actualOptions, diagnostics))
        {
            return CreateSkippedRgba16(info, limits, actualOptions, diagnostics);
        }

        PreparePhotometric(conversionInfo, actualOptions, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(info.Width, info.Length, 0, 0, new ushort[0], diagnostics);
        }

        double[] scaledSamples = ApplyMinMaxScaling(conversionInfo, samples, actualOptions, diagnostics);
        ushort[] rgba = TiffRgba16Reader.ConvertSamplesToRgba16(conversionInfo, scaledSamples, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(info.Width, info.Length, 0, 0, new ushort[0], diagnostics);
        }

        if (actualOptions.AlphaAssociation == TiffExtraSample.AssociatedAlpha)
        {
            rgba = TiffAlphaHandler.AssociateRgba16(rgba);
        }

        TiffOrientation orientation = TiffOrientation.TopLeft;
        if (actualOptions.ApplyOrientation)
        {
            orientation = info.Orientation;
        }

        return TiffRgba16Reader.ApplyOrientation(info.Width, info.Length, orientation, rgba, limits, diagnostics);
    }

    internal static TiffRgba8ReadResult ConvertSamplesToRgba8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffRgbaReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRgbaReadOptions actualOptions = NormalizeOptions(options, diagnostics);
        TiffImageInfo conversionInfo = PrepareInfo(info, actualOptions, diagnostics);
        double[] sourceSamples = CopySamples(samples);
        if (!ValidateConversionDimensions(conversionInfo, sourceSamples.Length, diagnostics))
        {
            return TiffRgba8ReadResult.Create(conversionInfo.Width, conversionInfo.Length, 0, 0, new byte[0], diagnostics);
        }

        if (ShouldSkipUnsupportedPhotometric(conversionInfo, actualOptions, diagnostics))
        {
            return CreateSkippedRgba8(conversionInfo, limits, actualOptions, diagnostics);
        }

        PreparePhotometric(conversionInfo, actualOptions, diagnostics);
        double[] scaledSamples = ApplyMinMaxScaling(conversionInfo, sourceSamples, actualOptions, diagnostics);
        byte[] rgba = TiffRgba8Reader.ConvertSamplesToRgba8(conversionInfo, scaledSamples, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(conversionInfo.Width, conversionInfo.Length, 0, 0, new byte[0], diagnostics);
        }

        if (actualOptions.AlphaAssociation == TiffExtraSample.AssociatedAlpha)
        {
            rgba = TiffAlphaHandler.AssociateRgba8(rgba);
        }

        TiffOrientation orientation = TiffOrientation.TopLeft;
        if (actualOptions.ApplyOrientation)
        {
            orientation = info.Orientation;
        }

        TiffOrientationTransformResult oriented = TiffOrientationTransformer.TransformInterleavedBytes(info.Width, info.Length, 4, orientation, rgba, limits, diagnostics);
        return TiffRgba8ReadResult.Create(oriented.Width, oriented.Length, oriented.RowByteCount, oriented.TotalByteCount, oriented.Bytes, diagnostics);
    }

    internal static TiffRgba16ReadResult ConvertSamplesToRgba16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffRgbaReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffRgbaReadOptions actualOptions = NormalizeOptions(options, diagnostics);
        TiffImageInfo conversionInfo = PrepareInfo(info, actualOptions, diagnostics);
        double[] sourceSamples = CopySamples(samples);
        if (!ValidateConversionDimensions(conversionInfo, sourceSamples.Length, diagnostics))
        {
            return TiffRgba16ReadResult.Create(conversionInfo.Width, conversionInfo.Length, 0, 0, new ushort[0], diagnostics);
        }

        if (ShouldSkipUnsupportedPhotometric(conversionInfo, actualOptions, diagnostics))
        {
            return CreateSkippedRgba16(conversionInfo, limits, actualOptions, diagnostics);
        }

        PreparePhotometric(conversionInfo, actualOptions, diagnostics);
        double[] scaledSamples = ApplyMinMaxScaling(conversionInfo, sourceSamples, actualOptions, diagnostics);
        ushort[] rgba = TiffRgba16Reader.ConvertSamplesToRgba16(conversionInfo, scaledSamples, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(conversionInfo.Width, conversionInfo.Length, 0, 0, new ushort[0], diagnostics);
        }

        if (actualOptions.AlphaAssociation == TiffExtraSample.AssociatedAlpha)
        {
            rgba = TiffAlphaHandler.AssociateRgba16(rgba);
        }

        TiffOrientation orientation = TiffOrientation.TopLeft;
        if (actualOptions.ApplyOrientation)
        {
            orientation = info.Orientation;
        }

        return TiffRgba16Reader.ApplyOrientation(info.Width, info.Length, orientation, rgba, limits, diagnostics);
    }

    internal static TiffRgbaReadOptions NormalizeOptions(TiffRgbaReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffRgbaReadOptions actualOptions = new TiffRgbaReadOptions();
        if (options != null)
        {
            actualOptions = options.Copy();
        }

        if (actualOptions.AlphaAssociation != TiffExtraSample.AssociatedAlpha && actualOptions.AlphaAssociation != TiffExtraSample.UnassociatedAlpha)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "RGBA output alpha association must be AssociatedAlpha or UnassociatedAlpha.");
        }

        if (actualOptions.UnsupportedFeaturePolicy != TiffUnsupportedFeaturePolicy.Error && actualOptions.UnsupportedFeaturePolicy != TiffUnsupportedFeaturePolicy.PreserveRaw && actualOptions.UnsupportedFeaturePolicy != TiffUnsupportedFeaturePolicy.Skip && actualOptions.UnsupportedFeaturePolicy != TiffUnsupportedFeaturePolicy.Ignore)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "RGBA unsupported-feature policy is invalid.");
        }

        if (actualOptions.CfaConversionMode != TiffCfaConversionMode.Bilinear && actualOptions.CfaConversionMode != TiffCfaConversionMode.Grayscale && actualOptions.CfaConversionMode != TiffCfaConversionMode.Reject)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "RGBA CFA conversion mode is invalid.");
        }

        return actualOptions;
    }

    internal static TiffImageInfo PrepareInfo(TiffImageInfo info, TiffRgbaReadOptions options, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to read an RGBA raster.");
            return new TiffImageInfo();
        }

        TiffImageInfo conversionInfo = info.Copy();
        if (!options.ApplyFillOrder)
        {
            conversionInfo.FillOrder = 1;
        }

        if (!options.ApplyPredictor)
        {
            conversionInfo.Predictor = 1;
        }

        if (!options.UseUnspecifiedExtraSampleAsAlpha && conversionInfo.ExtraSamples != null && conversionInfo.ExtraSamples.Length > 0 && conversionInfo.ExtraSamples[0] == TiffExtraSample.Unspecified)
        {
            conversionInfo.ExtraSamples = new TiffExtraSample[0];
        }

        return conversionInfo;
    }

    internal static double[] ReadSamples(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffRgbaReadOptions options, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        if (!TiffCodec.CanDecodeCompressionWithCodec(info.Compression, compressionCodec))
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedCompression, "RGBA raster reading does not support this compression.");
            return new double[0];
        }

        TiffRasterReadInfo readInfo = TiffSourceSampleRasterReader.CreateRasterInfo(info);
        diagnostics.Merge(readInfo.Diagnostics);
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (diagnostics.HasErrors())
        {
            return new double[0];
        }

        if (readInfo.TotalByteCount > int.MaxValue || !actualLimits.AllowsAllocation((long)readInfo.TotalByteCount) || !actualLimits.AllowsDecodedBytes((long)readInfo.TotalByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGBA source raster exceeds the configured allocation or decoded-size limit.");
            return new double[0];
        }

        byte[] sourceBytes = new byte[(int)readInfo.TotalByteCount];
        TiffByteOrder decodeByteOrder = byteOrder;
        if (!options.ApplyByteOrderConversion)
        {
            decodeByteOrder = TiffByteOrder.BigEndian;
            if (BitConverter.IsLittleEndian)
            {
                decodeByteOrder = TiffByteOrder.LittleEndian;
            }
        }

        TiffDiagnostics readDiagnostics = TiffSourceSampleRasterReader.ReadRasterWithCodec(info, offsets, byteCounts, source, decodeByteOrder, actualLimits, sourceBytes, compressionCodec);
        diagnostics.Merge(readDiagnostics);
        if (diagnostics.HasErrors())
        {
            return new double[0];
        }

        TiffImageInfo conversionInfo = TiffRgba8Reader.CreateFullContiguousInfo(info);
        TiffCodecContext context = TiffCodecContext.Create(conversionInfo, false, 0, (ulong)sourceBytes.Length, actualLimits);
        if (readInfo.ByteOrder == TiffByteOrder.Unknown)
        {
            context.ByteOrder = byteOrder;
        }
        else
        {
            context.ByteOrder = readInfo.ByteOrder;
        }

        if (readInfo.IsPacked)
        {
            sourceBytes = TiffSubByteSampleUnpacker.Unpack(context, sourceBytes, diagnostics);
        }

        if (diagnostics.HasErrors())
        {
            return new double[0];
        }

        return TiffSampleFormatConverter.ConvertToDoubleSamples(context, sourceBytes, diagnostics);
    }

    internal static bool ShouldSkipUnsupportedCompression(TiffImageInfo info, TiffRgbaReadOptions options, TiffDiagnostics diagnostics, ITiffCompressionCodec? compressionCodec)
    {
        if (TiffCodec.CanDecodeCompressionWithCodec(info.Compression, compressionCodec))
        {
            return false;
        }

        if (options.UnsupportedFeaturePolicy == TiffUnsupportedFeaturePolicy.Skip)
        {
            diagnostics.AddWarning(TiffErrorCode.UnsupportedCompression, "Unsupported compression was skipped; transparent pixels were returned.");
            return true;
        }

        return false;
    }

    internal static bool IsSupportedPhotometric(TiffPhotometric photometric)
    {
        if (photometric == TiffPhotometric.WhiteIsZero || photometric == TiffPhotometric.BlackIsZero || photometric == TiffPhotometric.Palette || photometric == TiffPhotometric.Rgb || photometric == TiffPhotometric.Cmyk || photometric == TiffPhotometric.YCbCr || photometric == TiffPhotometric.LogL || photometric == TiffPhotometric.LogLuv || photometric == TiffPhotometric.ColorFilterArray)
        {
            return true;
        }

        return TiffLabPhotometricConverter.IsLabPhotometric(photometric);
    }

    internal static bool ShouldSkipUnsupportedPhotometric(TiffImageInfo info, TiffRgbaReadOptions options, TiffDiagnostics diagnostics)
    {
        if (!options.ApplyPhotometricConversion || (IsSupportedPhotometric(info.Photometric) && (info.Photometric != TiffPhotometric.ColorFilterArray || options.CfaConversionMode != TiffCfaConversionMode.Reject)))
        {
            return false;
        }

        if (options.UnsupportedFeaturePolicy == TiffUnsupportedFeaturePolicy.Skip)
        {
            diagnostics.AddWarning(TiffErrorCode.UnsupportedPhotometric, "Unsupported photometric conversion was skipped; transparent pixels were returned.");
            return true;
        }

        if (options.UnsupportedFeaturePolicy == TiffUnsupportedFeaturePolicy.Ignore)
        {
            diagnostics.AddWarning(TiffErrorCode.UnsupportedPhotometric, "Unsupported photometric interpretation was ignored and source channels were treated as direct intensities.");
            return false;
        }

        if (options.UnsupportedFeaturePolicy == TiffUnsupportedFeaturePolicy.PreserveRaw)
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "RGBA output cannot preserve an unsupported photometric interpretation as raw samples.");
            return false;
        }

        diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "RGBA raster reading does not support this photometric interpretation.");
        return false;
    }

    internal static void PreparePhotometric(TiffImageInfo info, TiffRgbaReadOptions options, TiffDiagnostics diagnostics)
    {
        if (options.ApplyPhotometricConversion && info.Photometric == TiffPhotometric.ColorFilterArray)
        {
            if (options.CfaConversionMode == TiffCfaConversionMode.Bilinear)
            {
                return;
            }

            if (options.CfaConversionMode == TiffCfaConversionMode.Grayscale)
            {
                info.Photometric = TiffPhotometric.BlackIsZero;
                return;
            }
        }

        if (options.ApplyPhotometricConversion && IsSupportedPhotometric(info.Photometric) && (info.Photometric != TiffPhotometric.ColorFilterArray || options.CfaConversionMode != TiffCfaConversionMode.Reject))
        {
            return;
        }

        if (options.ApplyPhotometricConversion && options.UnsupportedFeaturePolicy != TiffUnsupportedFeaturePolicy.Ignore)
        {
            return;
        }

        if (info.SamplesPerPixel >= 3)
        {
            info.Photometric = TiffPhotometric.Rgb;
            return;
        }

        if (info.SamplesPerPixel >= 1)
        {
            info.Photometric = TiffPhotometric.BlackIsZero;
            return;
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Direct RGBA channel conversion requires at least one sample per pixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
    }

    internal static bool ValidateConversionDimensions(TiffImageInfo info, int sampleCount, TiffDiagnostics diagnostics)
    {
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (info.Width == 0 || info.Length == 0 || info.SamplesPerPixel <= 0 || info.Width > (ulong)int.MaxValue / info.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "RGBA conversion requires positive supported image dimensions and SamplesPerPixel.");
            return false;
        }

        ulong expectedSampleCount = info.Width * info.Length * (ulong)info.SamplesPerPixel;
        if (expectedSampleCount > int.MaxValue || sampleCount != (int)expectedSampleCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGBA source sample count does not match the image dimensions and SamplesPerPixel.");
            return false;
        }

        return true;
    }

    internal static double[] CopySamples(ReadOnlySpan<double> samples)
    {
        double[] copy = new double[samples.Length];
        int index = 0;
        while (index < samples.Length)
        {
            copy[index] = samples[index];
            index = index + 1;
        }

        return copy;
    }

    internal static double[] ApplyMinMaxScaling(TiffImageInfo info, double[] samples, TiffRgbaReadOptions options, TiffDiagnostics diagnostics)
    {
        if (!options.ApplyMinMaxScaling || info.Photometric == TiffPhotometric.Palette)
        {
            return samples;
        }

        double[] destination = new double[samples.Length];
        int index = 0;
        while (index < samples.Length)
        {
            int channelIndex = index % info.SamplesPerPixel;
            double minimum = GetMinimum(info, channelIndex, diagnostics);
            double maximum = GetMaximum(info, channelIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new double[0];
            }

            double normalized = (samples[index] - minimum) / (maximum - minimum);
            if (normalized < 0d)
            {
                normalized = 0d;
            }

            if (normalized > 1d)
            {
                normalized = 1d;
            }

            destination[index] = MapNormalizedToSampleRange(info, channelIndex, normalized, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new double[0];
            }

            index = index + 1;
        }

        return destination;
    }

    internal static double GetMinimum(TiffImageInfo info, int channelIndex, TiffDiagnostics diagnostics)
    {
        double value = GetRangeValue(info.MinSampleValues, channelIndex, diagnostics);
        if (info.SMinSampleValues != null && info.SMinSampleValues.Length == info.SamplesPerPixel)
        {
            value = info.SMinSampleValues[channelIndex];
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Minimum sample values must be finite.", info.ImageIndex, TiffTagId.SMinSampleValue);
        }

        return value;
    }

    internal static double GetMaximum(TiffImageInfo info, int channelIndex, TiffDiagnostics diagnostics)
    {
        double minimum = GetMinimum(info, channelIndex, diagnostics);
        double value = GetRangeValue(info.MaxSampleValues, channelIndex, diagnostics);
        if (info.SMaxSampleValues != null && info.SMaxSampleValues.Length == info.SamplesPerPixel)
        {
            value = info.SMaxSampleValues[channelIndex];
        }

        if (double.IsNaN(value) || double.IsInfinity(value) || value <= minimum)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Maximum sample values must be finite and greater than minimum sample values.", info.ImageIndex, TiffTagId.SMaxSampleValue);
        }

        return value;
    }

    internal static double GetRangeValue(double[] values, int channelIndex, TiffDiagnostics diagnostics)
    {
        if (values == null || values.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "RGBA min/max scaling requires one declared range per source channel.");
            return 0d;
        }

        if (values.Length == 1)
        {
            return values[0];
        }

        if (channelIndex < values.Length)
        {
            return values[channelIndex];
        }

        diagnostics.AddError(TiffErrorCode.InvalidTagCount, "RGBA min/max scaling requires one declared range per source channel.");
        return 0d;
    }

    internal static double MapNormalizedToSampleRange(TiffImageInfo info, int channelIndex, double normalized, TiffDiagnostics diagnostics)
    {
        TiffSampleFormat format = TiffPredictor.GetSampleFormat(info, channelIndex);
        int bits = TiffSubByteSampleUnpacker.GetBitsPerSample(info, channelIndex, diagnostics);
        if (format == TiffSampleFormat.IeeeFloatingPoint)
        {
            return normalized;
        }

        if (format == TiffSampleFormat.SignedInteger)
        {
            double minimum = 0d - TiffGrayscalePhotometricConverter.CalculatePowerOfTwo(bits - 1);
            double maximum = TiffGrayscalePhotometricConverter.CalculatePowerOfTwo(bits - 1) - 1d;
            return Math.Floor(minimum + normalized * (maximum - minimum) + 0.5d);
        }

        if (format == TiffSampleFormat.UnsignedInteger || format == TiffSampleFormat.Undefined)
        {
            return Math.Floor(normalized * TiffGrayscalePhotometricConverter.CalculateUnsignedMaximum(bits) + 0.5d);
        }

        diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SampleFormat is not supported for RGBA min/max scaling.", info.ImageIndex, TiffTagId.SampleFormat);
        return 0d;
    }

    internal static TiffRgba8ReadResult CreateSkippedRgba8(TiffImageInfo info, TiffLimits limits, TiffRgbaReadOptions options, TiffDiagnostics diagnostics)
    {
        ulong pixelCount = TiffRgba8Reader.CalculatePixelCount(info.Width, info.Length, diagnostics);
        ulong byteCount = TiffRgba8Reader.CalculateTotalByteCount(pixelCount, 4, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        byte[] rgba = new byte[(int)byteCount];
        TiffOrientation orientation = TiffOrientation.TopLeft;
        if (options.ApplyOrientation)
        {
            orientation = info.Orientation;
        }

        TiffOrientationTransformResult oriented = TiffOrientationTransformer.TransformInterleavedBytes(info.Width, info.Length, 4, orientation, rgba, limits, diagnostics);
        return TiffRgba8ReadResult.Create(oriented.Width, oriented.Length, oriented.RowByteCount, oriented.TotalByteCount, oriented.Bytes, diagnostics);
    }

    internal static TiffRgba16ReadResult CreateSkippedRgba16(TiffImageInfo info, TiffLimits limits, TiffRgbaReadOptions options, TiffDiagnostics diagnostics)
    {
        ulong pixelCount = TiffRgba8Reader.CalculatePixelCount(info.Width, info.Length, diagnostics);
        ulong byteCount = TiffRgba8Reader.CalculateTotalByteCount(pixelCount, 8, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(info.Width, info.Length, 0, 0, new ushort[0], diagnostics);
        }

        ushort[] rgba = new ushort[(int)(byteCount / 2UL)];
        TiffOrientation orientation = TiffOrientation.TopLeft;
        if (options.ApplyOrientation)
        {
            orientation = info.Orientation;
        }

        return TiffRgba16Reader.ApplyOrientation(info.Width, info.Length, orientation, rgba, limits, diagnostics);
    }
}
