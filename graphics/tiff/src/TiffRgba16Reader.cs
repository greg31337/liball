using System;

namespace tiff;

internal class TiffRgba16Reader
{
    internal static TiffRgba16ReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return Read(directory, fileBytes, byteOrder, new TiffLimits());
    }

    internal static TiffRgba16ReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read an RGBA16 raster.");
            return TiffRgba16ReadResult.Create(0, 0, 0, 0, new ushort[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRgba16ReadResult.Create(infoResult.Info.Width, infoResult.Info.Length, 0, 0, new ushort[0], diagnostics);
        }

        int offsetsTagId = TiffTagId.StripOffsets;
        int byteCountsTagId = TiffTagId.StripByteCounts;
        if (infoResult.Info.IsTiled)
        {
            offsetsTagId = TiffTagId.TileOffsets;
            byteCountsTagId = TiffTagId.TileByteCounts;
        }

        TiffIfdEntry? offsetsEntry = directory.FindEntry(offsetsTagId);
        if (offsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image data offsets are required to read an RGBA16 raster.", directory.ImageIndex, offsetsTagId);
            return TiffRgba16ReadResult.Create(infoResult.Info.Width, infoResult.Info.Length, 0, 0, new ushort[0], diagnostics);
        }

        TiffIfdEntry? byteCountsEntry = directory.FindEntry(byteCountsTagId);
        if (byteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image data byte counts are required to read an RGBA16 raster.", directory.ImageIndex, byteCountsTagId);
            return TiffRgba16ReadResult.Create(infoResult.Info.Width, infoResult.Info.Length, 0, 0, new ushort[0], diagnostics);
        }

        ulong[] offsets = TiffImageInfoExtractor.ReadUnsignedArray(offsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] byteCounts = TiffImageInfoExtractor.ReadUnsignedArray(byteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(infoResult.Info.Width, infoResult.Info.Length, 0, 0, new ushort[0], diagnostics);
        }

        return Read(infoResult.Info, offsets, byteCounts, fileBytes, actualLimits, diagnostics, byteOrder);
    }

    internal static TiffRgba16ReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes)
    {
        return Read(info, offsets, byteCounts, fileBytes, new TiffLimits());
    }

    internal static TiffRgba16ReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return Read(info, offsets, byteCounts, fileBytes, limits, diagnostics, TiffByteOrder.LittleEndian);
    }

    internal static TiffRgba16ReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to read an RGBA16 raster.");
            return TiffRgba16ReadResult.Create(0, 0, 0, 0, new ushort[0], diagnostics);
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        byte[] sourceBytes = TiffRgba8Reader.ReadSourceBytes(info, offsets, byteCounts, fileBytes, actualLimits, diagnostics, byteOrder);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(info.Width, info.Length, 0, 0, new ushort[0], diagnostics);
        }

        TiffImageInfo conversionInfo = TiffRgba8Reader.CreateFullContiguousInfo(info);
        TiffCodecContext context = TiffCodecContext.Create(conversionInfo, false, 0, (ulong)sourceBytes.Length, actualLimits);
        context.ByteOrder = byteOrder;
        double[] samples = TiffSampleFormatConverter.ConvertToDoubleSamples(context, sourceBytes, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(info.Width, info.Length, 0, 0, new ushort[0], diagnostics);
        }

        ushort[] rgba = ConvertSamplesToRgba16(conversionInfo, samples, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(info.Width, info.Length, 0, 0, new ushort[0], diagnostics);
        }

        TiffRgba16ReadResult oriented = ApplyOrientation(info.Width, info.Length, info.Orientation, rgba, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba16ReadResult.Create(info.Width, info.Length, 0, 0, new ushort[0], diagnostics);
        }

        return oriented;
    }

    internal static ushort[] ConvertSamplesToRgba16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = TiffRgba8Reader.ValidateSampleCount(info, samples.Length, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] rgb = ConvertSamplesToRgb16(info, samples, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        ushort[] rgba = CreateRgbaFromRgbAndAlpha(info, samples, rgb, pixelCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new ushort[0];
        }

        if (TiffRgba8Reader.HasAssociatedAlpha(info))
        {
            TiffImageInfo alphaInfo = info.Copy();
            alphaInfo.SamplesPerPixel = 4;
            rgba = TiffAlphaHandler.NormalizeRgba16(alphaInfo, rgba, diagnostics);
        }

        return rgba;
    }

    internal static ushort[] ConvertSamplesToRgb16(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info.Photometric == TiffPhotometric.WhiteIsZero || info.Photometric == TiffPhotometric.BlackIsZero)
        {
            double[] colorSamples = TiffRgba8Reader.ExtractChannelSamples(info, samples, 0, diagnostics);
            TiffImageInfo colorInfo = TiffRgba8Reader.CreateSingleChannelColorInfo(info);
            return TiffGrayscalePhotometricConverter.ConvertToRgb16(colorInfo, colorSamples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.Palette)
        {
            double[] colorSamples = TiffRgba8Reader.ExtractChannelSamples(info, samples, 0, diagnostics);
            TiffImageInfo colorInfo = TiffRgba8Reader.CreateSingleChannelColorInfo(info);
            return TiffPaletteColorMapExpander.ExpandToRgb16(colorInfo, colorSamples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.Rgb)
        {
            return TiffRgbPhotometricConverter.ConvertToRgb16(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.Cmyk)
        {
            return TiffCmykPhotometricConverter.ConvertToRgb16(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.YCbCr)
        {
            return TiffYCbCrPhotometricConverter.ConvertToRgb16(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.ColorFilterArray)
        {
            return TiffCfaPhotometricConverter.ConvertToRgb16(info, samples, limits, diagnostics);
        }

        if (TiffLabPhotometricConverter.IsLabPhotometric(info.Photometric))
        {
            return TiffLabPhotometricConverter.ConvertToRgb16(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.LogL)
        {
            return TiffSgiLogPhotometricConverter.ConvertLogLToRgb16(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.LogLuv)
        {
            return TiffSgiLogPhotometricConverter.ConvertLogLuvToRgb16(info, samples, limits, diagnostics);
        }

        diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "RGBA16 raster reading does not support this photometric interpretation.");
        return new ushort[0];
    }

    internal static ushort[] CreateRgbaFromRgbAndAlpha(TiffImageInfo info, ReadOnlySpan<double> samples, ushort[] rgb, int pixelCount, TiffDiagnostics diagnostics)
    {
        ushort[] rgba = new ushort[pixelCount * 4];
        bool hasAlpha = TiffRgba8Reader.HasAlpha(info);
        int alphaChannelIndex = TiffRgba8Reader.CalculateBaseColorSampleCount(info);
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            int rgbOffset = pixelIndex * 3;
            int rgbaOffset = pixelIndex * 4;
            rgba[rgbaOffset] = rgb[rgbOffset];
            rgba[rgbaOffset + 1] = rgb[rgbOffset + 1];
            rgba[rgbaOffset + 2] = rgb[rgbOffset + 2];
            rgba[rgbaOffset + 3] = 65535;
            if (hasAlpha)
            {
                double alphaIntensity = TiffRgbPhotometricConverter.ConvertChannelToIntensity(info, alphaChannelIndex, samples[pixelIndex * info.SamplesPerPixel + alphaChannelIndex], diagnostics);
                if (diagnostics.HasErrors())
                {
                    return new ushort[0];
                }

                rgba[rgbaOffset + 3] = TiffGrayscalePhotometricConverter.ScaleIntensityToUInt16(alphaIntensity);
            }

            pixelIndex = pixelIndex + 1;
        }

        return rgba;
    }

    internal static TiffRgba16ReadResult ApplyOrientation(ulong width, ulong length, TiffOrientation orientation, ushort[] samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (!ValidateOrientationRequest(width, length, orientation, samples.Length, limits, diagnostics))
        {
            return TiffRgba16ReadResult.Create(TiffOrientationTransformer.CalculateOrientedWidth(width, length, orientation), TiffOrientationTransformer.CalculateOrientedLength(width, length, orientation), 0, 0, new ushort[0], diagnostics);
        }

        ulong outputWidth = TiffOrientationTransformer.CalculateOrientedWidth(width, length, orientation);
        ulong outputLength = TiffOrientationTransformer.CalculateOrientedLength(width, length, orientation);
        ulong outputRowByteCount = outputWidth * 8UL;
        ulong outputByteCount = outputRowByteCount * outputLength;
        ushort[] destination = new ushort[samples.Length];

        ulong destinationY = 0;
        while (destinationY < outputLength)
        {
            ulong destinationX = 0;
            while (destinationX < outputWidth)
            {
                ulong sourceX = TiffOrientationTransformer.CalculateSourceX(width, length, destinationX, destinationY, orientation);
                ulong sourceY = TiffOrientationTransformer.CalculateSourceY(width, length, destinationX, destinationY, orientation);
                CopyRgba16Pixel(samples, destination, width, outputWidth, sourceX, sourceY, destinationX, destinationY);
                destinationX = destinationX + 1;
            }

            destinationY = destinationY + 1;
        }

        return TiffRgba16ReadResult.Create(outputWidth, outputLength, outputRowByteCount, outputByteCount, destination, diagnostics);
    }

    internal static bool ValidateOrientationRequest(ulong width, ulong length, TiffOrientation orientation, int sourceSampleCount, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (!TiffOrientationTransformer.IsSupportedOrientation(orientation))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "RGBA16 orientation transform requires a valid TIFF orientation value.", TiffDiagnostic.NoImageIndex, TiffTagId.Orientation);
            return false;
        }

        ulong pixelCount = TiffRgba8Reader.CalculatePixelCount(width, length, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (pixelCount > ulong.MaxValue / 4UL)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "RGBA16 sample count overflows the supported range.");
            return false;
        }

        ulong expectedSampleCount = pixelCount * 4UL;
        if ((ulong)sourceSampleCount != expectedSampleCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGBA16 sample count does not match dimensions.");
            return false;
        }

        if (expectedSampleCount > ulong.MaxValue / 2UL)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "RGBA16 byte count overflows the supported range.");
            return false;
        }

        ulong byteCount = expectedSampleCount * 2UL;
        if (byteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGBA16 raster exceeds the supported array range.");
            return false;
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGBA16 raster exceeds the configured allocation limit.");
            return false;
        }

        return true;
    }

    internal static void CopyRgba16Pixel(ushort[] source, ushort[] destination, ulong sourceWidth, ulong destinationWidth, ulong sourceX, ulong sourceY, ulong destinationX, ulong destinationY)
    {
        ulong sourceOffset = (sourceY * sourceWidth + sourceX) * 4UL;
        ulong destinationOffset = (destinationY * destinationWidth + destinationX) * 4UL;
        int channelIndex = 0;
        while (channelIndex < 4)
        {
            destination[(int)destinationOffset + channelIndex] = source[(int)sourceOffset + channelIndex];
            channelIndex = channelIndex + 1;
        }
    }
}
