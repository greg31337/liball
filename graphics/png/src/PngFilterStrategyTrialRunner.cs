namespace png;

/// <summary>
/// Encodes an image with selected PNG row-filter strategies and reports the smallest result.
/// </summary>
internal static class PngFilterStrategyTrialRunner
{
    /// <summary>
    /// Encodes the image with the configured row-filter strategies and identifies the smallest output.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <returns>The completed filter-strategy trial result.</returns>
    public static PngFilterStrategyTrialResult Run(PngImage image)
    {
        return Run(image, new PngOptimizerOptions());
    }

    /// <summary>
    /// Encodes the image with the configured row-filter strategies and identifies the smallest output.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The completed filter-strategy trial result.</returns>
    public static PngFilterStrategyTrialResult Run(PngImage image, PngOptimizerOptions options)
    {
        return Run(image, options, new PngCodec());
    }

    /// <summary>
    /// Encodes the image with selected row-filter strategies using a supplied encoder and identifies the smallest output.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <param name="encoder">The encoder used for each trial.</param>
    /// <returns>The completed filter-strategy trial result.</returns>
    public static PngFilterStrategyTrialResult Run(PngImage image, PngOptimizerOptions options, IPngEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        ArgumentNullException.ThrowIfNull(encoder);
        PngSecurityLimitEnforcer.EnforceImage(image, options.Limits);

        ValidateOptions(options);

        PngFilterStrategyTrialResult result = new PngFilterStrategyTrialResult();
        result.Trials = RunTrials(image, options, result, encoder);
        result.TriedFilterStrategies = result.Trials.Length > 1;
        return result;
    }

    internal static void ValidateOptions(PngOptimizerOptions options)
    {
        PngCompressionLevelMapper.Map(options.MinimumCompressionLevel);
        PngFilterEncoder.ValidateRowFilterStrategy(options.RowFilterStrategy);

        if (options.UseFixedFilter || !options.TryFilters || options.RowFilterStrategy == PngRowFilterStrategy.Fixed)
        {
            ValidateFilterType(options.FixedFilterType);
        }
    }

    internal static void ValidateFilterType(PngFilterType filterType)
    {
        if (filterType != PngFilterType.None
            && filterType != PngFilterType.Sub
            && filterType != PngFilterType.Up
            && filterType != PngFilterType.Average
            && filterType != PngFilterType.Paeth)
        {
            throw new ArgumentOutOfRangeException("FixedFilterType");
        }
    }

    internal static PngFilterStrategyTrial[] RunTrials(PngImage image, PngOptimizerOptions options, PngFilterStrategyTrialResult result, IPngEncoder encoder)
    {
        if (options.UseFixedFilter || !options.TryFilters || options.RowFilterStrategy == PngRowFilterStrategy.Fixed)
        {
            PngFilterStrategyTrial[] fixedTrials = new PngFilterStrategyTrial[1];
            fixedTrials[0] = RunFixedTrial(image, options, options.FixedFilterType, encoder, GetCurrentBestByteCount(options, result));
            ApplyBestTrial(fixedTrials[0], options, result);
            MarkBestTrial(result);
            return fixedTrials;
        }

        if (options.RowFilterStrategy == PngRowFilterStrategy.Fast)
        {
            PngFilterStrategyTrial[] fastTrials = new PngFilterStrategyTrial[1];
            fastTrials[0] = RunFastTrial(image, options, encoder, GetCurrentBestByteCount(options, result));
            ApplyBestTrial(fastTrials[0], options, result);
            MarkBestTrial(result);
            return fastTrials;
        }

        if (options.RowFilterStrategy == PngRowFilterStrategy.Adaptive)
        {
            PngFilterStrategyTrial[] adaptiveTrials = new PngFilterStrategyTrial[1];
            adaptiveTrials[0] = RunAdaptiveTrial(image, options, encoder, GetCurrentBestByteCount(options, result));
            ApplyBestTrial(adaptiveTrials[0], options, result);
            MarkBestTrial(result);
            return adaptiveTrials;
        }

        PngFilterStrategyTrial[] trials = new PngFilterStrategyTrial[7];
        trials[0] = RunAdaptiveTrial(image, options, encoder, GetCurrentBestByteCount(options, result));
        ApplyBestTrial(trials[0], options, result);
        trials[1] = RunFastTrial(image, options, encoder, GetCurrentBestByteCount(options, result));
        ApplyBestTrial(trials[1], options, result);
        trials[2] = RunFixedTrial(image, options, PngFilterType.None, encoder, GetCurrentBestByteCount(options, result));
        ApplyBestTrial(trials[2], options, result);
        trials[3] = RunFixedTrial(image, options, PngFilterType.Sub, encoder, GetCurrentBestByteCount(options, result));
        ApplyBestTrial(trials[3], options, result);
        trials[4] = RunFixedTrial(image, options, PngFilterType.Up, encoder, GetCurrentBestByteCount(options, result));
        ApplyBestTrial(trials[4], options, result);
        trials[5] = RunFixedTrial(image, options, PngFilterType.Average, encoder, GetCurrentBestByteCount(options, result));
        ApplyBestTrial(trials[5], options, result);
        trials[6] = RunFixedTrial(image, options, PngFilterType.Paeth, encoder, GetCurrentBestByteCount(options, result));
        ApplyBestTrial(trials[6], options, result);
        MarkBestTrial(result);
        return trials;
    }

    internal static void ApplyBestTrial(PngFilterStrategyTrial trial, PngOptimizerOptions options, PngFilterStrategyTrialResult result)
    {
        if (trial.WasBailedOut)
        {
            result.BailedOutTrialCount += 1;
            return;
        }

        long bestByteCount = GetCurrentBestByteCount(options, result);
        if (options.UseEarlyBailout && bestByteCount > 0 && trial.ByteCount > bestByteCount)
        {
            trial.WasBailedOut = true;
            trial.EncodedBytes = new byte[0];
            result.BailedOutTrialCount += 1;
            return;
        }

        if (result.BestTrial.EncodedBytes.Length == 0 || trial.ByteCount < result.BestTrial.ByteCount)
        {
            result.BestTrial = trial;
        }
    }

    internal static long GetCurrentBestByteCount(PngOptimizerOptions options, PngFilterStrategyTrialResult result)
    {
        if (result.BestTrial.EncodedBytes.Length > 0)
        {
            return result.BestTrial.ByteCount;
        }

        return options.BestKnownByteCount;
    }

    internal static void MarkBestTrial(PngFilterStrategyTrialResult result)
    {
        if (result.BestTrial.EncodedBytes.Length > 0)
        {
            result.BestTrial.IsBest = true;
        }
    }

    internal static PngFilterStrategyTrial RunAdaptiveTrial(PngImage image, PngOptimizerOptions options, IPngEncoder encoder, long bestByteCount)
    {
        PngEncoderOptions encoderOptions = PngOptimizerOptionMapper.CreateEncoderOptions(image, options, options.MinimumCompressionLevel);
        encoderOptions.RowFilterStrategy = PngRowFilterStrategy.Adaptive;
        encoderOptions.UseFixedFilter = false;
        return RunTrial(image, encoderOptions, PngFilterType.None, PngRowFilterStrategy.Adaptive, false, encoder, options, bestByteCount);
    }

    internal static PngFilterStrategyTrial RunFastTrial(PngImage image, PngOptimizerOptions options, IPngEncoder encoder, long bestByteCount)
    {
        PngEncoderOptions encoderOptions = PngOptimizerOptionMapper.CreateEncoderOptions(image, options, options.MinimumCompressionLevel);
        encoderOptions.RowFilterStrategy = PngRowFilterStrategy.Fast;
        encoderOptions.UseFixedFilter = false;
        return RunTrial(image, encoderOptions, PngFilterType.None, PngRowFilterStrategy.Fast, false, encoder, options, bestByteCount);
    }

    internal static PngFilterStrategyTrial RunFixedTrial(PngImage image, PngOptimizerOptions options, PngFilterType filterType, IPngEncoder encoder, long bestByteCount)
    {
        PngEncoderOptions encoderOptions = PngOptimizerOptionMapper.CreateEncoderOptions(image, options, options.MinimumCompressionLevel);
        encoderOptions.RowFilterStrategy = PngRowFilterStrategy.Fixed;
        encoderOptions.UseFixedFilter = true;
        encoderOptions.FixedFilterType = filterType;
        return RunTrial(image, encoderOptions, filterType, PngRowFilterStrategy.Fixed, true, encoder, options, bestByteCount);
    }

    internal static PngFilterStrategyTrial RunTrial(PngImage image, PngEncoderOptions encoderOptions, PngFilterType filterType, PngRowFilterStrategy strategy, bool useFixedFilter, IPngEncoder encoder, PngOptimizerOptions options, long bestByteCount)
    {
        PngCompressionLevelTrialRunner.ConfigureEarlyBailout(encoderOptions, options, bestByteCount);
        byte[] encodedBytes = encoder.EncodeToBytes(image, encoderOptions);

        PngFilterStrategyTrial trial = new PngFilterStrategyTrial();
        trial.CompressionLevel = encoderOptions.CompressionLevel;
        trial.FilterType = filterType;
        trial.RowFilterStrategy = strategy;
        trial.UseFixedFilter = useFixedFilter;
        trial.EncodedBytes = encodedBytes;
        trial.ByteCount = encodedBytes.Length;
        if (encoderOptions.WasEarlyBailedOut)
        {
            trial.WasBailedOut = true;
            trial.ByteCount = encoderOptions.ProducedByteCount;
        }

        return trial;
    }

    internal static PngEncoderOptions CreateEncoderOptions(PngOptimizerOptions options)
    {
        PngImage image = new PngImage();
        return PngOptimizerOptionMapper.CreateEncoderOptions(image, options, options.MinimumCompressionLevel);
    }

}
