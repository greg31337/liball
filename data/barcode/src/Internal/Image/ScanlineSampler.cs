namespace barcode;

internal class LinearScanlineSample
{
    internal BitRow Row = new BitRow(1);
    internal bool IsVertical;
    internal int Coordinate;
}

internal static class ScanlineSampler
{
    internal const int MaximumFanLineCount = 63;

    internal static LinearScanlineSample Sample(
        ModuleMatrix matrix,
        bool vertical,
        int coordinate)
    {
        if (matrix == null)
        {
            throw new BarcodeValidationException("Scanline source must not be null.");
        }

        int limit = vertical ? matrix.Width : matrix.Height;
        CheckedMath.ValidateIndex(coordinate, limit);
        bool[] bits = vertical ? matrix.CopyColumn(coordinate) : matrix.CopyRow(coordinate);
        BitRow row = new BitRow(bits.Length);
        row.LoadBits(bits);

        LinearScanlineSample sample = new LinearScanlineSample();
        sample.Row = row;
        sample.IsVertical = vertical;
        sample.Coordinate = coordinate;
        return sample;
    }

    internal static LinearScanlineSample[] CreateFan(
        ModuleMatrix matrix,
        bool vertical,
        int centerCoordinate,
        int lineSpacing,
        int maximumLineCount)
    {
        if (matrix == null)
        {
            throw new BarcodeValidationException("Scanline source must not be null.");
        }

        int limit = vertical ? matrix.Width : matrix.Height;
        CheckedMath.ValidateIndex(centerCoordinate, limit);
        if (lineSpacing <= 0)
        {
            throw new BarcodeValidationException("Scanline fan spacing must be positive.");
        }

        if (maximumLineCount <= 0 || maximumLineCount > MaximumFanLineCount)
        {
            throw new BarcodeValidationException("Scanline fan line count is outside the supported range.");
        }

        global::System.Collections.Generic.List<LinearScanlineSample> samples =
            new global::System.Collections.Generic.List<LinearScanlineSample>();
        samples.Add(Sample(matrix, vertical, centerCoordinate));

        int distance = lineSpacing;
        while (samples.Count < maximumLineCount &&
            (centerCoordinate - distance >= 0 || centerCoordinate + distance < limit))
        {
            int lower = centerCoordinate - distance;
            if (lower >= 0)
            {
                samples.Add(Sample(matrix, vertical, lower));
            }

            int upper = centerCoordinate + distance;
            if (samples.Count < maximumLineCount && upper < limit)
            {
                samples.Add(Sample(matrix, vertical, upper));
            }

            distance = CheckedMath.Add(distance, lineSpacing);
        }

        return samples.ToArray();
    }
}
