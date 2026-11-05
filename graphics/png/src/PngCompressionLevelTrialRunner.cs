namespace png;

/// <summary>
/// Encodes an image at selected compression levels and reports the smallest result.
/// </summary>
internal static class PngCompressionLevelTrialRunner
{
    /// <summary>
    /// Encodes the image at the configured compression levels and identifies the smallest output.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <returns>The completed compression trial result.</returns>
    public static PngCompressionTrialResult Run(PngImage image)
    {
        return Run(image, new PngOptimizerOptions());
    }

    /// <summary>
    /// Encodes the image at the configured compression levels and identifies the smallest output.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <returns>The completed compression trial result.</returns>
    public static PngCompressionTrialResult Run(PngImage image, PngOptimizerOptions options)
    {
        return Run(image, options, new PngCodec());
    }

    /// <summary>
    /// Encodes the image at the configured compression levels with a supplied encoder and identifies the smallest output.
    /// </summary>
    /// <param name="image">The image to process.</param>
    /// <param name="options">The options and safety limits for this operation.</param>
    /// <param name="encoder">The encoder used for each trial.</param>
    /// <returns>The completed compression trial result.</returns>
    public static PngCompressionTrialResult Run(PngImage image, PngOptimizerOptions options, IPngEncoder encoder)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        ArgumentNullException.ThrowIfNull(encoder);
        PngSecurityLimitEnforcer.EnforceImage(image, options.Limits);

        ValidateCompressionRange(options.MinimumCompressionLevel, options.MaximumCompressionLevel);
        PngFilterEncoder.ValidateRowFilterStrategy(options.RowFilterStrategy);
        if (options.UseFixedFilter || !options.TryFilters || options.RowFilterStrategy == PngRowFilterStrategy.Fixed)
        {
            PngFilterEncoder.ValidateFixedFilterType(options.FixedFilterType);
        }

        int firstLevel = options.MinimumCompressionLevel;
        int lastLevel = options.MaximumCompressionLevel;
        if (!options.TryCompressionLevels)
        {
            lastLevel = firstLevel;
        }

        PngCompressionTrialResult result = new PngCompressionTrialResult();
        result.Trials = RunTrials(image, options, firstLevel, lastLevel, result, encoder);
        result.TriedCompressionLevels = options.TryCompressionLevels && result.Trials.Length > 1;
        return result;
    }

    internal static void ValidateCompressionRange(int minimumCompressionLevel, int maximumCompressionLevel)
    {
        if (minimumCompressionLevel < PngCompressionLevelMapper.MinimumCompressionLevel || minimumCompressionLevel > PngCompressionLevelMapper.MaximumCompressionLevel)
        {
            throw new ArgumentOutOfRangeException("MinimumCompressionLevel");
        }

        if (maximumCompressionLevel < PngCompressionLevelMapper.MinimumCompressionLevel || maximumCompressionLevel > PngCompressionLevelMapper.MaximumCompressionLevel)
        {
            throw new ArgumentOutOfRangeException("MaximumCompressionLevel");
        }

        if (minimumCompressionLevel > maximumCompressionLevel)
        {
            throw new ArgumentOutOfRangeException("MinimumCompressionLevel");
        }
    }

    internal static PngCompressionTrial[] RunTrials(PngImage image, PngOptimizerOptions options, int firstLevel, int lastLevel, PngCompressionTrialResult result, IPngEncoder encoder)
    {
        PngCompressionTrial[] trials = new PngCompressionTrial[lastLevel - firstLevel + 1];
        long bestByteCount = options.BestKnownByteCount;
        bool hasBestTrial = false;

        int level = firstLevel;
        while (level <= lastLevel)
        {
            PngCompressionTrial trial = RunTrial(image, options, level, encoder, bestByteCount);
            ApplyEarlyBailout(trial, options, bestByteCount, result);
            trials[level - firstLevel] = trial;
            if (!trial.WasBailedOut && (!hasBestTrial || trial.ByteCount < bestByteCount))
            {
                bestByteCount = trial.ByteCount;
                result.BestTrial = trial;
                hasBestTrial = true;
            }

            level += 1;
        }

        if (hasBestTrial)
        {
            result.BestTrial.IsBest = true;
        }

        return trials;
    }

    internal static void ApplyEarlyBailout(PngCompressionTrial trial, PngOptimizerOptions options, long bestByteCount, PngCompressionTrialResult result)
    {
        if (trial.WasBailedOut)
        {
            result.BailedOutTrialCount += 1;
            return;
        }

        if (!trial.WasBailedOut && options.UseEarlyBailout && bestByteCount > 0 && trial.ByteCount > bestByteCount)
        {
            trial.WasBailedOut = true;
            trial.EncodedBytes = new byte[0];
            result.BailedOutTrialCount += 1;
        }
    }

    internal static PngCompressionTrial RunTrial(PngImage image, PngOptimizerOptions options, int compressionLevel, IPngEncoder encoder, long bestByteCount)
    {
        PngEncoderOptions encoderOptions = PngOptimizerOptionMapper.CreateEncoderOptions(image, options, compressionLevel);
        ConfigureEarlyBailout(encoderOptions, options, bestByteCount);
        byte[] encodedBytes = encoder.EncodeToBytes(image, encoderOptions);

        PngCompressionTrial trial = new PngCompressionTrial();
        trial.CompressionLevel = compressionLevel;
        trial.EncodedBytes = encodedBytes;
        trial.ByteCount = encodedBytes.Length;
        if (encoderOptions.WasEarlyBailedOut)
        {
            trial.WasBailedOut = true;
            trial.ByteCount = encoderOptions.ProducedByteCount;
        }

        return trial;
    }

    internal static void ConfigureEarlyBailout(PngEncoderOptions encoderOptions, PngOptimizerOptions options, long bestByteCount)
    {
        if (!options.UseEarlyBailout || bestByteCount <= 0)
        {
            return;
        }

        encoderOptions.EarlyBailoutByteCount = bestByteCount;
    }

    internal static PngEncoderOptions CreateEncoderOptions(PngOptimizerOptions options, int compressionLevel)
    {
        PngImage image = new PngImage();
        return PngOptimizerOptionMapper.CreateEncoderOptions(image, options, compressionLevel);
    }

}
