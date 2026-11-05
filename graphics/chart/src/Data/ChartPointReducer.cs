namespace chart;

/// <summary>
/// Provides operations for point reducer.
/// </summary>
internal static class ChartPointReducerOperations
{
    /// <summary>
    /// Applies the simplify line operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <param name="tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    public static ChartPoint[] SimplifyLine(ChartPoint[] points, double tolerance)
    {
        return SimplifyDouglasPeucker(points, tolerance);
    }

    /// <summary>
    /// Applies the simplify radial distance operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <param name="tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    public static ChartPoint[] SimplifyRadialDistance(ChartPoint[] points, double tolerance)
    {
        ValidatePoints(points);
        ValidateTolerance(tolerance);

        if (points.Length <= 2)
        {
            return CopyPoints(points);
        }

        double toleranceSquared = tolerance * tolerance;
        ChartPoint[] output = new ChartPoint[points.Length];
        int count = 0;
        ChartPoint previous = points[0];
        output[count] = previous.Copy();
        count = count + 1;

        int index = 1;
        while (index < points.Length - 1)
        {
            if (DistanceSquared(previous, points[index]) >= toleranceSquared)
            {
                previous = points[index];
                output[count] = previous.Copy();
                count = count + 1;
            }

            index = index + 1;
        }

        output[count] = points[points.Length - 1].Copy();
        count = count + 1;
        return CopyPointBuffer(output, count);
    }

    /// <summary>
    /// Applies the simplify douglas peucker operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <param name="tolerance">The tolerance.</param>
    /// <returns>The resulting point array.</returns>
    public static ChartPoint[] SimplifyDouglasPeucker(ChartPoint[] points, double tolerance)
    {
        ValidatePoints(points);
        ValidateTolerance(tolerance);

        if (points.Length <= 2)
        {
            return CopyPoints(points);
        }

        bool[] keep = new bool[points.Length];
        keep[0] = true;
        keep[points.Length - 1] = true;
        SimplifyDouglasPeuckerRange(points, 0, points.Length - 1, tolerance * tolerance, keep);
        return CopyKeptPoints(points, keep);
    }

    internal static void SimplifyDouglasPeuckerRange(ChartPoint[] points, int first, int last, double toleranceSquared, bool[] keep)
    {
        if (last <= first + 1)
        {
            return;
        }

        double maximumDistance = -1.0;
        int maximumIndex = -1;
        int index = first + 1;
        while (index < last)
        {
            double distance = PerpendicularDistanceSquared(points[index], points[first], points[last]);
            if (distance > maximumDistance)
            {
                maximumDistance = distance;
                maximumIndex = index;
            }

            index = index + 1;
        }

        if (maximumDistance > toleranceSquared)
        {
            keep[maximumIndex] = true;
            SimplifyDouglasPeuckerRange(points, first, maximumIndex, toleranceSquared, keep);
            SimplifyDouglasPeuckerRange(points, maximumIndex, last, toleranceSquared, keep);
        }
    }

    /// <summary>
    /// Applies the decimate scatter operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <param name="maxPointCount">The requested number of max point entries.</param>
    /// <returns>The resulting point array.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static ChartPoint[] DecimateScatter(ChartPoint[] points, int maxPointCount)
    {
        ValidatePoints(points);
        if (maxPointCount < 2)
        {
            throw new System.ArgumentOutOfRangeException("maxPointCount");
        }

        if (points.Length <= maxPointCount)
        {
            return CopyPoints(points);
        }

        ChartPoint[] output = new ChartPoint[maxPointCount];
        int index = 0;
        while (index < maxPointCount)
        {
            double position = index * (points.Length - 1.0) / (maxPointCount - 1.0);
            int sourceIndex = (int)System.Math.Round(position);
            output[index] = points[sourceIndex].Copy();
            index = index + 1;
        }

        return output;
    }

    internal static double PerpendicularDistanceSquared(ChartPoint point, ChartPoint start, ChartPoint end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0.0)
        {
            return DistanceSquared(point, start);
        }

        double amount = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared;
        if (amount < 0.0)
        {
            amount = 0.0;
        }
        else if (amount > 1.0)
        {
            amount = 1.0;
        }

        double projectedX = start.X + amount * dx;
        double projectedY = start.Y + amount * dy;
        double pointDx = point.X - projectedX;
        double pointDy = point.Y - projectedY;
        return pointDx * pointDx + pointDy * pointDy;
    }

    internal static double DistanceSquared(ChartPoint first, ChartPoint second)
    {
        double dx = second.X - first.X;
        double dy = second.Y - first.Y;
        return dx * dx + dy * dy;
    }

    internal static ChartPoint[] CopyKeptPoints(ChartPoint[] points, bool[] keep)
    {
        int count = 0;
        int index = 0;
        while (index < keep.Length)
        {
            if (keep[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        ChartPoint[] output = new ChartPoint[count];
        int outputIndex = 0;
        index = 0;
        while (index < points.Length)
        {
            if (keep[index])
            {
                output[outputIndex] = points[index].Copy();
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return output;
    }

    internal static ChartPoint[] CopyPoints(ChartPoint[] points)
    {
        ChartPoint[] copy = new ChartPoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            copy[index] = points[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static ChartPoint[] CopyPointBuffer(ChartPoint[] points, int count)
    {
        ChartPoint[] copy = new ChartPoint[count];
        int index = 0;
        while (index < count)
        {
            copy[index] = points[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidatePoints(ChartPoint[] points)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        int index = 0;
        while (index < points.Length)
        {
            ChartPoint point = points[index];
            if (point == null)
            {
                throw new System.ArgumentNullException("points");
            }

            if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
            {
                throw new System.ArgumentException("Point coordinates must be finite.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateTolerance(double tolerance)
    {
        if (tolerance < 0.0 || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
        {
            throw new System.ArgumentOutOfRangeException("tolerance");
        }
    }
}
