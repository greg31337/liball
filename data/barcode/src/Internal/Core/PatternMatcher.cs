namespace barcode;

internal static class PatternMatcher
{
    internal static double CalculateNormalizedVariance(int[] observed, int[] expected, double maximumIndividualVariance)
    {
        if (observed == null || expected == null)
        {
            throw new BarcodeValidationException("Observed and expected patterns must not be null.");
        }

        if (observed.Length == 0 || observed.Length != expected.Length)
        {
            throw new BarcodeValidationException("Observed and expected patterns must have the same nonzero length.");
        }

        if (maximumIndividualVariance < 0.0 || double.IsNaN(maximumIndividualVariance))
        {
            throw new BarcodeValidationException("Maximum individual variance must be a non-negative number.");
        }

        int observedTotal = SumPositiveRuns(observed);
        int expectedTotal = SumPositiveRuns(expected);
        double unitWidth = observedTotal / (double)expectedTotal;
        double individualLimit = maximumIndividualVariance * unitWidth;
        double totalVariance = 0.0;
        int index = 0;
        while (index < observed.Length)
        {
            double scaledExpected = expected[index] * unitWidth;
            double variance = global::System.Math.Abs(observed[index] - scaledExpected);
            if (variance > individualLimit)
            {
                return double.PositiveInfinity;
            }

            totalVariance = totalVariance + variance;
            index = index + 1;
        }

        return totalVariance / observedTotal;
    }

    internal static bool IsPatternMatch(int[] observed, int[] expected, double maximumAverageVariance, double maximumIndividualVariance)
    {
        if (maximumAverageVariance < 0.0 || double.IsNaN(maximumAverageVariance))
        {
            throw new BarcodeValidationException("Maximum average variance must be a non-negative number.");
        }

        double variance = CalculateNormalizedVariance(observed, expected, maximumIndividualVariance);
        return variance <= maximumAverageVariance;
    }

    internal static int[]? FindForward(BitRow row, int[] expected, bool startsDark, double maximumAverageVariance, double maximumIndividualVariance)
    {
        ValidateSearch(row, expected, maximumAverageVariance, maximumIndividualVariance);
        int start = 0;
        while (start < row.Length)
        {
            bool color = row.ReadBitUnchecked(start);
            bool atRunStart = start == 0 || row.ReadBitUnchecked(start - 1) != color;
            if (color == startsDark && atRunStart)
            {
                int[] observed = RunLength.ExtractForward(row, start, expected.Length);
                if (observed.Length == expected.Length && IsPatternMatch(observed, expected, maximumAverageVariance, maximumIndividualVariance))
                {
                    int width = SumPositiveRuns(observed);
                    return new int[] { start, CheckedMath.Add(start, width) };
                }
            }

            start = start + 1;
        }

        return null;
    }

    internal static int[]? FindReverse(BitRow row, int[] expected, bool startsDark, double maximumAverageVariance, double maximumIndividualVariance)
    {
        ValidateSearch(row, expected, maximumAverageVariance, maximumIndividualVariance);
        int start = row.Length - 1;
        while (start >= 0)
        {
            bool color = row.ReadBitUnchecked(start);
            bool atRunStart = start == row.Length - 1 || row.ReadBitUnchecked(start + 1) != color;
            if (color == startsDark && atRunStart)
            {
                int[] observed = RunLength.ExtractReverse(row, start, expected.Length);
                if (observed.Length == expected.Length && IsPatternMatch(observed, expected, maximumAverageVariance, maximumIndividualVariance))
                {
                    int width = SumPositiveRuns(observed);
                    int lower = CheckedMath.Add(CheckedMath.Subtract(start, width), 1);
                    return new int[] { lower, CheckedMath.Add(start, 1) };
                }
            }

            start = start - 1;
        }

        return null;
    }

    internal static int SumPositiveRuns(int[] runs)
    {
        if (runs == null)
        {
            throw new BarcodeValidationException("Run pattern must not be null.");
        }

        if (runs.Length == 0)
        {
            throw new BarcodeValidationException("Run pattern must not be empty.");
        }

        int total = 0;
        int index = 0;
        while (index < runs.Length)
        {
            if (runs[index] <= 0)
            {
                throw new BarcodeValidationException("Run pattern values must be greater than zero.");
            }

            total = CheckedMath.Add(total, runs[index]);
            index = index + 1;
        }

        return total;
    }

    internal static void ValidateSearch(BitRow row, int[] expected, double maximumAverageVariance, double maximumIndividualVariance)
    {
        if (row == null)
        {
            throw new BarcodeValidationException("Pattern source must not be null.");
        }

        SumPositiveRuns(expected);
        if (maximumAverageVariance < 0.0 || double.IsNaN(maximumAverageVariance))
        {
            throw new BarcodeValidationException("Maximum average variance must be a non-negative number.");
        }

        if (maximumIndividualVariance < 0.0 || double.IsNaN(maximumIndividualVariance))
        {
            throw new BarcodeValidationException("Maximum individual variance must be a non-negative number.");
        }
    }
}
