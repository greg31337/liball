namespace png;

internal class PngFilterTrialScorer
{
    internal static PngFilterTrialResult ScoreAll(byte[] rowData, int bytesPerPixel)
    {
        ArgumentNullException.ThrowIfNull(rowData);

        return ScoreAllWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, new byte[0], false);
    }

    internal static PngFilterTrialResult ScoreAll(byte[] rowData, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);

        return ScoreAllWithPrevious(rowData, 0, rowData.Length, bytesPerPixel, previousRow, true);
    }

    internal static PngFilterTrialResult ScoreAll(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel)
    {
        return ScoreAllWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, new byte[0], false);
    }

    internal static PngFilterTrialResult ScoreAll(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow)
    {
        ArgumentNullException.ThrowIfNull(previousRow);

        return ScoreAllWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, true);
    }

    internal static PngFilterTrialResult Score(byte[] rowData, int bytesPerPixel, PngOptimizerOptions options)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(options);

        return ScoreWithOptions(rowData, 0, rowData.Length, bytesPerPixel, new byte[0], false, options);
    }

    internal static PngFilterTrialResult Score(byte[] rowData, int bytesPerPixel, byte[] previousRow, PngOptimizerOptions options)
    {
        ArgumentNullException.ThrowIfNull(rowData);
        ArgumentNullException.ThrowIfNull(previousRow);
        ArgumentNullException.ThrowIfNull(options);

        return ScoreWithOptions(rowData, 0, rowData.Length, bytesPerPixel, previousRow, true, options);
    }

    internal static PngFilterTrialResult Score(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, PngOptimizerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return ScoreWithOptions(rowData, offset, rowDataBytes, bytesPerPixel, new byte[0], false, options);
    }

    internal static PngFilterTrialResult Score(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, PngOptimizerOptions options)
    {
        ArgumentNullException.ThrowIfNull(previousRow);
        ArgumentNullException.ThrowIfNull(options);

        return ScoreWithOptions(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, true, options);
    }

    internal static PngFilterTrialResult ScoreWithOptions(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow, PngOptimizerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Limits);
        PngSecurityLimitEnforcer.EnforceRowBytes(rowDataBytes, options.Limits);
        PngFilterEncoder.ValidateRowFilterStrategy(options.RowFilterStrategy);

        if (options.UseFixedFilter || options.RowFilterStrategy == PngRowFilterStrategy.Fixed)
        {
            PngFilterTrialResult result = ScoreFixed(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, options.FixedFilterType);
            result.UsedFixedFilter = true;
            return result;
        }

        if (!options.TryFilters)
        {
            return ScoreFixed(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, options.FixedFilterType);
        }

        if (options.RowFilterStrategy == PngRowFilterStrategy.Fast)
        {
            PngFilterType filterType = PngFilterType.Sub;
            if (hasPreviousRow)
            {
                filterType = PngFilterType.Up;
            }

            return ScoreFixed(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, filterType);
        }

        return ScoreAllWithPrevious(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow);
    }

    internal static PngFilterTrialResult ScoreAllWithPrevious(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow)
    {
        PngFilterTrialResult result = new PngFilterTrialResult();
        result.Trials = new PngFilterTrial[5];
        result.Trials[0] = CreateTrial(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, PngFilterType.None);
        result.Trials[1] = CreateTrial(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, PngFilterType.Sub);
        result.Trials[2] = CreateTrial(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, PngFilterType.Up);
        result.Trials[3] = CreateTrial(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, PngFilterType.Average);
        result.Trials[4] = CreateTrial(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, PngFilterType.Paeth);
        result.BestTrial = ChooseBest(result.Trials);
        result.TriedAllFilters = true;
        return result;
    }

    internal static PngFilterTrialResult ScoreFixed(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow, PngFilterType filterType)
    {
        PngFilterTrialResult result = new PngFilterTrialResult();
        result.Trials = new PngFilterTrial[1];
        result.Trials[0] = CreateTrial(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, hasPreviousRow, filterType);
        result.BestTrial = result.Trials[0];
        return result;
    }

    internal static PngFilterTrial CreateTrial(byte[] rowData, int offset, int rowDataBytes, int bytesPerPixel, byte[] previousRow, bool hasPreviousRow, PngFilterType filterType)
    {
        byte[] filteredRow;
        if (hasPreviousRow)
        {
            filteredRow = PngFilterEncoder.EncodeFixed(rowData, offset, rowDataBytes, bytesPerPixel, previousRow, filterType);
        }
        else
        {
            filteredRow = PngFilterEncoder.EncodeFixed(rowData, offset, rowDataBytes, bytesPerPixel, filterType);
        }

        PngFilterTrial trial = new PngFilterTrial();
        trial.FilterType = filterType;
        trial.FilteredRow = filteredRow;
        trial.Score = PngFilterEncoder.CalculateRowScore(filteredRow);
        return trial;
    }

    internal static PngFilterTrial ChooseBest(PngFilterTrial[] trials)
    {
        PngFilterTrial bestTrial = trials[0];
        int index = 1;
        while (index < trials.Length)
        {
            if (trials[index].Score < bestTrial.Score)
            {
                bestTrial = trials[index];
            }

            index += 1;
        }

        return bestTrial;
    }
}
