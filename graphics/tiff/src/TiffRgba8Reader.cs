using System;

namespace tiff;

internal class TiffRgba8Reader
{
    internal static TiffRgba8ReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return Read(directory, fileBytes, byteOrder, new TiffLimits());
    }

    internal static TiffRgba8ReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read an RGBA8 raster.");
            return TiffRgba8ReadResult.Create(0, 0, 0, 0, new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRgba8ReadResult.Create(infoResult.Info.Width, infoResult.Info.Length, 0, 0, new byte[0], diagnostics);
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
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image data offsets are required to read an RGBA8 raster.", directory.ImageIndex, offsetsTagId);
            return TiffRgba8ReadResult.Create(infoResult.Info.Width, infoResult.Info.Length, 0, 0, new byte[0], diagnostics);
        }

        TiffIfdEntry? byteCountsEntry = directory.FindEntry(byteCountsTagId);
        if (byteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image data byte counts are required to read an RGBA8 raster.", directory.ImageIndex, byteCountsTagId);
            return TiffRgba8ReadResult.Create(infoResult.Info.Width, infoResult.Info.Length, 0, 0, new byte[0], diagnostics);
        }

        ulong[] offsets = TiffImageInfoExtractor.ReadUnsignedArray(offsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] byteCounts = TiffImageInfoExtractor.ReadUnsignedArray(byteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(infoResult.Info.Width, infoResult.Info.Length, 0, 0, new byte[0], diagnostics);
        }

        return Read(infoResult.Info, offsets, byteCounts, fileBytes, actualLimits, diagnostics, byteOrder);
    }

    internal static TiffRgba8ReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes)
    {
        return Read(info, offsets, byteCounts, fileBytes, new TiffLimits());
    }

    internal static TiffRgba8ReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return Read(info, offsets, byteCounts, fileBytes, limits, diagnostics, TiffByteOrder.LittleEndian);
    }

    internal static TiffRgba8ReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to read an RGBA8 raster.");
            return TiffRgba8ReadResult.Create(0, 0, 0, 0, new byte[0], diagnostics);
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        byte[] sourceBytes = ReadSourceBytes(info, offsets, byteCounts, fileBytes, actualLimits, diagnostics, byteOrder);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        TiffImageInfo conversionInfo = CreateFullContiguousInfo(info);
        TiffCodecContext context = TiffCodecContext.Create(conversionInfo, false, 0, (ulong)sourceBytes.Length, actualLimits);
        context.ByteOrder = byteOrder;
        double[] samples = TiffSampleFormatConverter.ConvertToDoubleSamples(context, sourceBytes, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        byte[] rgba = ConvertSamplesToRgba8(conversionInfo, samples, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        TiffOrientationTransformResult oriented = TiffOrientationTransformer.TransformInterleavedBytes(info.Width, info.Length, 4, info.Orientation, rgba, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRgba8ReadResult.Create(info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        return TiffRgba8ReadResult.Create(oriented.Width, oriented.Length, oriented.RowByteCount, oriented.TotalByteCount, oriented.Bytes, diagnostics);
    }

    internal static byte[] ReadSourceBytes(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            return ReadSeparateSourceBytes(info, offsets, byteCounts, fileBytes, limits, diagnostics, byteOrder);
        }

        if (HasSubByteSamples(info, limits, diagnostics))
        {
            if (info.IsTiled)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Full-image sub-byte raster reading currently requires stripped image data.", info.ImageIndex, TiffTagId.BitsPerSample);
                return new byte[0];
            }

            return ReadFullSubByteSourceBytes(info, offsets, byteCounts, fileBytes, limits, diagnostics, byteOrder);
        }

        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        TiffRegionReadResult region = ReadSourceRegion(info, offsets, byteCounts, fileBytes, 0, limits, diagnostics, byteOrder);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        return TiffIfdEntry.CopyBytes(region.Bytes);
    }

    internal static bool HasSubByteSamples(TiffImageInfo info, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffImageInfo fullInfo = CreateFullContiguousInfo(info);
        TiffCodecContext context = TiffCodecContext.Create(fullInfo, false, 0, 0, limits);
        return TiffSubByteSampleUnpacker.HasSubByteSamples(context, diagnostics);
    }

    internal static byte[] ReadFullSubByteSourceBytes(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        ulong stripCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ulong packedByteCount = CalculateFullSubBytePackedByteCount(info, stripCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if (packedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Full-image sub-byte source byte count exceeds the supported array range.");
            return new byte[0];
        }

        byte[] packedBytes = new byte[(int)packedByteCount];
        int destinationOffset = 0;
        ulong stripIndex = 0;
        while (stripIndex < stripCount)
        {
            TiffDecodedStripReadResult stripResult = TiffDecodedStripReader.ReadStrip(info, offsets, byteCounts, fileBytes, stripIndex, limits, diagnostics, byteOrder);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            CopySubByteStripBytes(stripResult.Bytes, packedBytes, destinationOffset, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            destinationOffset = destinationOffset + stripResult.Bytes.Length;
            stripIndex = stripIndex + 1;
        }

        TiffImageInfo fullInfo = CreateFullContiguousInfo(info);
        TiffCodecContext context = TiffCodecContext.Create(fullInfo, false, 0, packedByteCount, limits);
        context.ByteOrder = byteOrder;
        return TiffSubByteSampleUnpacker.Unpack(context, packedBytes, diagnostics);
    }

    internal static ulong CalculateFullSubBytePackedByteCount(TiffImageInfo info, ulong stripCount, TiffDiagnostics diagnostics)
    {
        ulong byteCount = 0;
        ulong stripIndex = 0;
        while (stripIndex < stripCount)
        {
            ulong stripByteCount = TiffStrileLayout.CalculateStripByteCount(info, stripIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            if (byteCount > ulong.MaxValue - stripByteCount)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Full-image sub-byte source byte count overflows the supported range.");
                return 0;
            }

            byteCount = byteCount + stripByteCount;
            stripIndex = stripIndex + 1;
        }

        return byteCount;
    }

    internal static void CopySubByteStripBytes(byte[] source, byte[] destination, int destinationOffset, TiffDiagnostics diagnostics)
    {
        if (source == null || destination == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Sub-byte source and destination bytes are required.");
            return;
        }

        if (destinationOffset < 0 || destinationOffset > destination.Length || source.Length > destination.Length - destinationOffset)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sub-byte strip bytes exceed the full-image buffer.");
            return;
        }

        int index = 0;
        while (index < source.Length)
        {
            destination[destinationOffset + index] = source[index];
            index = index + 1;
        }
    }

    internal static byte[] ReadSeparateSourceBytes(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        int samplesPerPixel = info.SamplesPerPixel;
        if (samplesPerPixel <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be positive for RGBA8 raster reading.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return new byte[0];
        }

        int[] sampleByteCounts = CalculateSampleByteCounts(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        int bytesPerPixel = CalculateBytesPerPixel(sampleByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ulong pixelCount = CalculatePixelCount(info.Width, info.Length, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ulong totalByteCount = CalculateTotalByteCount(pixelCount, bytesPerPixel, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] destination = new byte[(int)totalByteCount];
        int sampleIndex = 0;
        int channelOffset = 0;
        while (sampleIndex < samplesPerPixel)
        {
            TiffRegionReadResult region = ReadSourceRegion(info, offsets, byteCounts, fileBytes, sampleIndex, limits, diagnostics, byteOrder);
            if (diagnostics.HasErrors())
            {
                return new byte[0];
            }

            CopyPlaneToInterleaved(region.Bytes, destination, pixelCount, sampleByteCounts[sampleIndex], bytesPerPixel, channelOffset);
            channelOffset = channelOffset + sampleByteCounts[sampleIndex];
            sampleIndex = sampleIndex + 1;
        }

        return destination;
    }

    internal static TiffRegionReadResult ReadSourceRegion(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, int sampleIndex, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        if (info.IsTiled)
        {
            return TiffTileRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 0, 0, info.Width, info.Length, sampleIndex, limits, diagnostics);
        }

        return TiffStripRegionReader.ReadRegion(info, offsets, byteCounts, fileBytes, 0, 0, info.Width, info.Length, sampleIndex, limits, diagnostics);
    }

    internal static byte[] ConvertSamplesToRgba8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateSampleCount(info, samples.Length, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] rgb = ConvertSamplesToRgb8(info, samples, limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        byte[] rgba = CreateRgbaFromRgbAndAlpha(info, samples, rgb, pixelCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if (HasAssociatedAlpha(info))
        {
            TiffImageInfo alphaInfo = info.Copy();
            alphaInfo.SamplesPerPixel = 4;
            rgba = TiffAlphaHandler.NormalizeRgba8(alphaInfo, rgba, diagnostics);
        }

        return rgba;
    }

    internal static byte[] ConvertSamplesToRgb8(TiffImageInfo info, ReadOnlySpan<double> samples, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info.Photometric == TiffPhotometric.WhiteIsZero || info.Photometric == TiffPhotometric.BlackIsZero)
        {
            double[] colorSamples = ExtractChannelSamples(info, samples, 0, diagnostics);
            TiffImageInfo colorInfo = CreateSingleChannelColorInfo(info);
            return TiffGrayscalePhotometricConverter.ConvertToRgb8(colorInfo, colorSamples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.Palette)
        {
            double[] colorSamples = ExtractChannelSamples(info, samples, 0, diagnostics);
            TiffImageInfo colorInfo = CreateSingleChannelColorInfo(info);
            return TiffPaletteColorMapExpander.ExpandToRgb8(colorInfo, colorSamples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.Rgb)
        {
            return TiffRgbPhotometricConverter.ConvertToRgb8(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.Cmyk)
        {
            return TiffCmykPhotometricConverter.ConvertToRgb8(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.YCbCr)
        {
            return TiffYCbCrPhotometricConverter.ConvertToRgb8(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.ColorFilterArray)
        {
            return TiffCfaPhotometricConverter.ConvertToRgb8(info, samples, limits, diagnostics);
        }

        if (TiffLabPhotometricConverter.IsLabPhotometric(info.Photometric))
        {
            return TiffLabPhotometricConverter.ConvertToRgb8(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.LogL)
        {
            return TiffSgiLogPhotometricConverter.ConvertLogLToRgb8(info, samples, limits, diagnostics);
        }

        if (info.Photometric == TiffPhotometric.LogLuv)
        {
            return TiffSgiLogPhotometricConverter.ConvertLogLuvToRgb8(info, samples, limits, diagnostics);
        }

        diagnostics.AddError(TiffErrorCode.UnsupportedPhotometric, "RGBA8 raster reading does not support this photometric interpretation.");
        return new byte[0];
    }

    internal static byte[] CreateRgbaFromRgbAndAlpha(TiffImageInfo info, ReadOnlySpan<double> samples, byte[] rgb, int pixelCount, TiffDiagnostics diagnostics)
    {
        byte[] rgba = new byte[pixelCount * 4];
        bool hasAlpha = HasAlpha(info);
        int alphaChannelIndex = CalculateBaseColorSampleCount(info);
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            int rgbOffset = pixelIndex * 3;
            int rgbaOffset = pixelIndex * 4;
            rgba[rgbaOffset] = rgb[rgbOffset];
            rgba[rgbaOffset + 1] = rgb[rgbOffset + 1];
            rgba[rgbaOffset + 2] = rgb[rgbOffset + 2];
            rgba[rgbaOffset + 3] = 255;
            if (hasAlpha)
            {
                double alphaIntensity = TiffRgbPhotometricConverter.ConvertChannelToIntensity(info, alphaChannelIndex, samples[pixelIndex * info.SamplesPerPixel + alphaChannelIndex], diagnostics);
                if (diagnostics.HasErrors())
                {
                    return new byte[0];
                }

                rgba[rgbaOffset + 3] = TiffGrayscalePhotometricConverter.ScaleIntensityToByte(alphaIntensity);
            }

            pixelIndex = pixelIndex + 1;
        }

        return rgba;
    }

    internal static bool HasAlpha(TiffImageInfo info)
    {
        int alphaChannelIndex = CalculateBaseColorSampleCount(info);
        if (info.ExtraSamples == null || info.ExtraSamples.Length == 0)
        {
            return false;
        }

        if (info.SamplesPerPixel <= alphaChannelIndex)
        {
            return false;
        }

        TiffExtraSample alpha = info.ExtraSamples[0];
        if (alpha == TiffExtraSample.Unspecified || alpha == TiffExtraSample.AssociatedAlpha || alpha == TiffExtraSample.UnassociatedAlpha)
        {
            return true;
        }

        return false;
    }

    internal static bool HasAssociatedAlpha(TiffImageInfo info)
    {
        if (!HasAlpha(info))
        {
            return false;
        }

        if (info.ExtraSamples[0] == TiffExtraSample.AssociatedAlpha)
        {
            return true;
        }

        return false;
    }

    internal static int CalculateBaseColorSampleCount(TiffImageInfo info)
    {
        if (info.Photometric == TiffPhotometric.WhiteIsZero || info.Photometric == TiffPhotometric.BlackIsZero || info.Photometric == TiffPhotometric.Palette || info.Photometric == TiffPhotometric.LogL || info.Photometric == TiffPhotometric.ColorFilterArray)
        {
            return 1;
        }

        if (info.Photometric == TiffPhotometric.Cmyk)
        {
            return 4;
        }

        if (info.Photometric == TiffPhotometric.LogLuv && info.SamplesPerPixel == 1)
        {
            return 1;
        }

        return 3;
    }

    internal static double[] ExtractChannelSamples(TiffImageInfo info, ReadOnlySpan<double> samples, int channelIndex, TiffDiagnostics diagnostics)
    {
        int pixelCount = ValidateSampleCount(info, samples.Length, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new double[0];
        }

        if (channelIndex < 0 || channelIndex >= info.SamplesPerPixel)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Requested channel is outside SamplesPerPixel.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return new double[0];
        }

        double[] channelSamples = new double[pixelCount];
        int pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            channelSamples[pixelIndex] = samples[pixelIndex * info.SamplesPerPixel + channelIndex];
            pixelIndex = pixelIndex + 1;
        }

        return channelSamples;
    }

    internal static TiffImageInfo CreateSingleChannelColorInfo(TiffImageInfo info)
    {
        TiffImageInfo colorInfo = info.Copy();
        colorInfo.SamplesPerPixel = 1;
        colorInfo.BitsPerSample = new int[] { TiffSubByteSampleUnpacker.GetBitsPerSample(info, 0, new TiffDiagnostics()) };
        colorInfo.SampleFormats = new TiffSampleFormat[] { TiffPredictor.GetSampleFormat(info, 0) };
        colorInfo.ExtraSamples = new TiffExtraSample[0];
        return colorInfo;
    }

    internal static int ValidateSampleCount(TiffImageInfo info, int sampleCount, TiffDiagnostics diagnostics)
    {
        if (info.SamplesPerPixel <= 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "SamplesPerPixel must be positive for RGBA8 conversion.", info.ImageIndex, TiffTagId.SamplesPerPixel);
            return 0;
        }

        if (sampleCount % info.SamplesPerPixel != 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "RGBA8 source sample count does not match SamplesPerPixel.");
            return 0;
        }

        return sampleCount / info.SamplesPerPixel;
    }

    internal static TiffImageInfo CreateFullContiguousInfo(TiffImageInfo info)
    {
        TiffImageInfo conversionInfo = info.Copy();
        conversionInfo.PlanarConfiguration = TiffPlanarConfiguration.Contiguous;
        conversionInfo.IsTiled = false;
        conversionInfo.RowsPerStrip = info.Length;
        conversionInfo.TileWidth = 0;
        conversionInfo.TileLength = 0;
        return conversionInfo;
    }

    internal static int[] CalculateSampleByteCounts(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        int[] byteCounts = new int[info.SamplesPerPixel];
        int sampleIndex = 0;
        while (sampleIndex < info.SamplesPerPixel)
        {
            int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, sampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new int[0];
            }

            byteCounts[sampleIndex] = TiffSampleFormatConverter.CalculateSampleByteCount(bitsPerSample, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new int[0];
            }

            sampleIndex = sampleIndex + 1;
        }

        return byteCounts;
    }

    internal static int CalculateBytesPerPixel(int[] sampleByteCounts, TiffDiagnostics diagnostics)
    {
        int byteCount = 0;
        int index = 0;
        while (index < sampleByteCounts.Length)
        {
            if (byteCount > int.MaxValue - sampleByteCounts[index])
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "RGBA8 source pixel byte count overflows the supported range.");
                return 0;
            }

            byteCount = byteCount + sampleByteCounts[index];
            index = index + 1;
        }

        return byteCount;
    }

    internal static ulong CalculatePixelCount(ulong width, ulong length, TiffDiagnostics diagnostics)
    {
        if (width != 0 && length > ulong.MaxValue / width)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "RGBA8 pixel count overflows the supported range.");
            return 0;
        }

        return width * length;
    }

    internal static ulong CalculateTotalByteCount(ulong pixelCount, int bytesPerPixel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if ((ulong)bytesPerPixel != 0 && pixelCount > ulong.MaxValue / (ulong)bytesPerPixel)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "RGBA8 source byte count overflows the supported range.");
            return 0;
        }

        ulong byteCount = pixelCount * (ulong)bytesPerPixel;
        if (byteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGBA8 source byte count exceeds the supported array range.");
            return 0;
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!actualLimits.AllowsAllocation((long)byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGBA8 source byte count exceeds the configured allocation limit.");
            return 0;
        }

        if (!actualLimits.AllowsDecodedBytes((long)byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "RGBA8 source byte count exceeds the configured decoded-size limit.");
            return 0;
        }

        return byteCount;
    }

    internal static void CopyPlaneToInterleaved(byte[] planeBytes, byte[] destination, ulong pixelCount, int sampleByteCount, int bytesPerPixel, int channelOffset)
    {
        ulong pixelIndex = 0;
        while (pixelIndex < pixelCount)
        {
            ulong planeOffset = pixelIndex * (ulong)sampleByteCount;
            ulong destinationOffset = pixelIndex * (ulong)bytesPerPixel + (ulong)channelOffset;
            int byteIndex = 0;
            while (byteIndex < sampleByteCount)
            {
                destination[(int)destinationOffset + byteIndex] = planeBytes[(int)planeOffset + byteIndex];
                byteIndex = byteIndex + 1;
            }

            pixelIndex = pixelIndex + 1;
        }
    }
}
