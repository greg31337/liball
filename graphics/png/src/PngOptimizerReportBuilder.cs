namespace png;

/// <summary>
/// Combines compression and filter trial results into one optimization report.
/// </summary>
internal static class PngOptimizerReportBuilder
{
    /// <summary>
    /// Compares completed optimization trials and describes the smallest encoded output.
    /// </summary>
    /// <param name="originalByteCount">The original encoded byte count.</param>
    /// <param name="compressionResult">The completed compression-level trials.</param>
    /// <param name="filterStrategyResult">The completed row-filter strategy trials.</param>
    /// <returns>The combined optimization report.</returns>
    public static PngOptimizerReport Create(long originalByteCount, PngCompressionTrialResult compressionResult, PngFilterStrategyTrialResult filterStrategyResult)
    {
        if (originalByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("originalByteCount");
        }

        ArgumentNullException.ThrowIfNull(compressionResult);
        ArgumentNullException.ThrowIfNull(compressionResult.Trials);
        ArgumentNullException.ThrowIfNull(compressionResult.BestTrial);
        ArgumentNullException.ThrowIfNull(filterStrategyResult);
        ArgumentNullException.ThrowIfNull(filterStrategyResult.Trials);
        ArgumentNullException.ThrowIfNull(filterStrategyResult.BestTrial);

        PngOptimizerReport report = new PngOptimizerReport();
        report.OriginalByteCount = originalByteCount;
        report.CompressionTrialCount = compressionResult.Trials.Length;
        report.FilterStrategyTrialCount = filterStrategyResult.Trials.Length;
        report.BailedOutTrialCount = compressionResult.BailedOutTrialCount + filterStrategyResult.BailedOutTrialCount;

        ApplyCompressionBest(report, compressionResult.BestTrial);
        ApplyFilterStrategyBest(report, filterStrategyResult.BestTrial);
        if (report.FoundBestResult)
        {
            report.ByteSavings = originalByteCount - report.BestByteCount;
        }

        return report;
    }

    /// <summary>
    /// Compares completed optimization trials and describes the smallest encoded output.
    /// </summary>
    /// <param name="originalBytes">The original encoded PNG bytes.</param>
    /// <param name="compressionResult">The completed compression-level trials.</param>
    /// <param name="filterStrategyResult">The completed row-filter strategy trials.</param>
    /// <returns>The combined optimization report.</returns>
    public static PngOptimizerReport Create(byte[] originalBytes, PngCompressionTrialResult compressionResult, PngFilterStrategyTrialResult filterStrategyResult)
    {
        ArgumentNullException.ThrowIfNull(originalBytes);

        return Create(originalBytes.Length, compressionResult, filterStrategyResult);
    }

    internal static void ApplyCompressionBest(PngOptimizerReport report, PngCompressionTrial trial)
    {
        ValidateCompressionTrial(trial);
        if (trial.EncodedBytes.Length == 0)
        {
            return;
        }

        report.FoundBestResult = true;
        report.BestFromCompressionTrial = true;
        report.BestFromFilterStrategyTrial = false;
        report.BestCompressionLevel = trial.CompressionLevel;
        report.BestFilterType = PngFilterType.None;
        report.BestUsesFixedFilter = false;
        report.BestByteCount = trial.ByteCount;
        report.BestEncodedBytes = CopyBytes(trial.EncodedBytes);
    }

    internal static void ApplyFilterStrategyBest(PngOptimizerReport report, PngFilterStrategyTrial trial)
    {
        ValidateFilterStrategyTrial(trial);
        if (trial.EncodedBytes.Length == 0)
        {
            return;
        }

        if (report.FoundBestResult && trial.ByteCount >= report.BestByteCount)
        {
            return;
        }

        report.FoundBestResult = true;
        report.BestFromCompressionTrial = false;
        report.BestFromFilterStrategyTrial = true;
        report.BestCompressionLevel = trial.CompressionLevel;
        report.BestFilterType = trial.FilterType;
        report.BestUsesFixedFilter = trial.UseFixedFilter;
        report.BestRowFilterStrategy = trial.RowFilterStrategy;
        report.BestByteCount = trial.ByteCount;
        report.BestEncodedBytes = CopyBytes(trial.EncodedBytes);
    }

    internal static void ValidateCompressionTrial(PngCompressionTrial trial)
    {
        ArgumentNullException.ThrowIfNull(trial);
        ArgumentNullException.ThrowIfNull(trial.EncodedBytes);
    }

    internal static void ValidateFilterStrategyTrial(PngFilterStrategyTrial trial)
    {
        ArgumentNullException.ThrowIfNull(trial);
        ArgumentNullException.ThrowIfNull(trial.EncodedBytes);
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            copy[index] = source[index];
            index += 1;
        }

        return copy;
    }
}
