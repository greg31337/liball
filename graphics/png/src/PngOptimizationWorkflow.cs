using System.Collections.Generic;

namespace png;

internal class PngOptimizationWorkflow
{
    internal static PngOptimizerReport OptimizeBytes(byte[] data, PngOptimizerOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        PngDecoderOptions decoderOptions = new PngDecoderOptions();
        decoderOptions.Limits = options.Limits;
        decoderOptions.DecodeAnimation = true;
        decoderOptions.PreferredPixelFormat = PngPixelFormat.Unknown;
        PngImage image = PngDecoder.Decode(data, 0, decoderOptions);
        ApplyExplicitPaletteReduction(image, options);

        PngOptimizerOptions compressionOptions = PngOptimizerOptionMapper.CopyOptions(options);
        compressionOptions.BestKnownByteCount = SelectInitialBestByteCount(data.Length, options.BestKnownByteCount, options.UseEarlyBailout);
        PngCompressionTrialResult compression = PngCompressionLevelTrialRunner.Run(image, compressionOptions);

        PngOptimizerOptions filterOptions = PngOptimizerOptionMapper.CopyOptions(options);
        filterOptions.BestKnownByteCount = SelectFilterBestByteCount(data.Length, compression, compressionOptions.BestKnownByteCount, options.UseEarlyBailout);
        if (compression.BestTrial.EncodedBytes.Length > 0)
        {
            filterOptions.MinimumCompressionLevel = compression.BestTrial.CompressionLevel;
        }

        PngFilterStrategyTrialResult filters = PngFilterStrategyTrialRunner.Run(image, filterOptions);
        PngOptimizerReport report = PngOptimizerReportBuilder.Create(data, compression, filters);
        report.CompressionResult = compression;
        report.FilterStrategyResult = filters;
        SelectOriginalWhenSmaller(report, data);
        PopulateDetails(report, data, options);
        return report;
    }

    internal static void ApplyExplicitPaletteReduction(PngImage image, PngOptimizerOptions options)
    {
        if (!options.AllowPaletteReduction || PngOptimizerOptionMapper.HasAnimation(image)
            || !PngPaletteIndexPixelConverter.IsIndexedFormat(image.Pixels.Format) || image.Metadata.Palette == null)
        {
            return;
        }

        image.Metadata.Palette = PngPaletteTrimmer.Trim(image.Pixels, image.Metadata.Palette);
        if (image.Metadata.IndexedTransparency != null && image.Metadata.IndexedTransparency.Alpha != null
            && image.Metadata.IndexedTransparency.Alpha.Length > image.Metadata.Palette.Entries.Length)
        {
            byte[] alpha = new byte[image.Metadata.Palette.Entries.Length];
            int index = 0;
            while (index < alpha.Length)
            {
                alpha[index] = image.Metadata.IndexedTransparency.Alpha[index];
                index += 1;
            }

            image.Metadata.IndexedTransparency.Alpha = alpha;
        }
    }

    internal static long SelectInitialBestByteCount(long originalByteCount, long configuredBest, bool useEarlyBailout)
    {
        if (!useEarlyBailout)
        {
            return configuredBest;
        }

        if (configuredBest > 0 && configuredBest < originalByteCount)
        {
            return configuredBest;
        }

        return originalByteCount;
    }

    internal static long SelectFilterBestByteCount(long originalByteCount, PngCompressionTrialResult compression, long currentBest, bool useEarlyBailout)
    {
        if (!useEarlyBailout)
        {
            return currentBest;
        }

        long best = currentBest;
        if (best <= 0)
        {
            best = originalByteCount;
        }

        if (compression.BestTrial.EncodedBytes.Length > 0 && compression.BestTrial.ByteCount < best)
        {
            best = compression.BestTrial.ByteCount;
        }

        return best;
    }

    internal static void SelectOriginalWhenSmaller(PngOptimizerReport report, byte[] originalBytes)
    {
        if (report.FoundBestResult && report.BestByteCount < originalBytes.Length)
        {
            return;
        }

        report.FoundBestResult = true;
        report.BestIsOriginal = true;
        report.BestFromCompressionTrial = false;
        report.BestFromFilterStrategyTrial = false;
        report.BestByteCount = originalBytes.Length;
        report.BestEncodedBytes = PngOptimizerReportBuilder.CopyBytes(originalBytes);
        report.ByteSavings = 0;
    }

    internal static void PopulateDetails(PngOptimizerReport report, byte[] originalBytes, PngOptimizerOptions options)
    {
        PngDecoderOptions identifyOptions = new PngDecoderOptions();
        identifyOptions.Limits = options.Limits;
        PngIdentifyResult original = PngIdentifier.Identify(originalBytes, 0, identifyOptions);
        PngIdentifyResult optimized = PngIdentifier.Identify(report.BestEncodedBytes, 0, identifyOptions);

        report.OriginalColorType = original.Info.ColorType;
        report.OriginalBitDepth = original.Info.BitDepth;
        report.OriginalPaletteEntryCount = GetPaletteEntryCount(original.Metadata);
        report.OriginalMetadataBytes = original.Metadata.MetadataBytes;
        report.OptimizedColorType = optimized.Info.ColorType;
        report.OptimizedBitDepth = optimized.Info.BitDepth;
        report.OptimizedPaletteEntryCount = GetPaletteEntryCount(optimized.Metadata);
        report.OptimizedMetadataBytes = optimized.Metadata.MetadataBytes;
        if (report.BestFromFilterStrategyTrial)
        {
            report.BestRowFilterStrategy = report.FilterStrategyResult.BestTrial.RowFilterStrategy;
        }

        report.Decisions = CreateDecisions(report, options, originalBytes.Length);
    }

    internal static int GetPaletteEntryCount(PngMetadata metadata)
    {
        if (metadata == null || metadata.Palette == null || metadata.Palette.Entries == null)
        {
            return 0;
        }

        return metadata.Palette.Entries.Length;
    }

    internal static PngOptimizationDecision[] CreateDecisions(PngOptimizerReport report, PngOptimizerOptions options, long originalByteCount)
    {
        List<PngOptimizationDecision> decisions = new List<PngOptimizationDecision>();
        decisions.Add(CreateDecision("PNG_OPTIMIZE_COLOR_TYPE_REDUCTION", "Considered lossless color-type reduction.", options.AllowColorTypeReduction, report.OptimizedColorType != report.OriginalColorType));
        decisions.Add(CreateDecision("PNG_OPTIMIZE_BIT_DEPTH_REDUCTION", "Considered lossless sample-depth reduction.", options.AllowBitDepthReduction, (int)report.OptimizedBitDepth < (int)report.OriginalBitDepth));
        decisions.Add(CreateDecision("PNG_OPTIMIZE_PALETTE_REDUCTION", "Considered removal of unused trailing palette entries.", options.AllowPaletteReduction, report.OptimizedPaletteEntryCount > 0 && report.OptimizedPaletteEntryCount < report.OriginalPaletteEntryCount));
        decisions.Add(CreateDecision("PNG_OPTIMIZE_METADATA_SELECTION", "Applied the configured ancillary metadata policy.", options.MetadataPolicy != PngMetadataPolicy.Preserve, report.OptimizedMetadataBytes < report.OriginalMetadataBytes));
        decisions.Add(CreateDecision("PNG_OPTIMIZE_FILTER_TRIALS", "Compared the configured row-filter strategies.", options.TryFilters, report.FilterStrategyResult.Trials.Length > 1));
        decisions.Add(CreateDecision("PNG_OPTIMIZE_COMPRESSION_TRIALS", "Compared the configured Deflate compression levels.", options.TryCompressionLevels, report.CompressionResult.Trials.Length > 1));
        decisions.Add(CreateDecision("PNG_OPTIMIZE_EARLY_BAILOUT", "Stopped trials once their output could not beat the current best size.", options.UseEarlyBailout, report.BailedOutTrialCount > 0));
        decisions.Add(CreateDecision("PNG_OPTIMIZE_RESULT_SELECTION", "Selected the smallest lossless encoded result.", true, report.BestByteCount < originalByteCount));
        return decisions.ToArray();
    }

    internal static PngOptimizationDecision CreateDecision(string code, string description, bool enabled, bool applied)
    {
        PngOptimizationDecision decision = new PngOptimizationDecision();
        decision.Code = code;
        decision.Description = description;
        decision.Enabled = enabled;
        decision.Applied = applied;
        return decision;
    }
}
