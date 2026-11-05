namespace qrcode;

internal class GridGeometryMeasurer
{
    // Full row-major grid of independently detected centers, in image coordinates.
    // Least-squares affine fitting separates global axis spacing and skew from
    // local displacements. GNU here is the largest residual divided by the mean
    // fitted module pitch; geometric distortion is axis skew away from 90 degrees.
    // These are uncalibrated indicators, not ISO verifier grades. In particular,
    // a homography-generated grid is not independent evidence of print geometry.
    internal static GridGeometryMeasurement Measure(QrPoint[] centers, int columns, int rows, QrQualityOptions options)
    {
        int area = ValidateInput(centers, columns, rows, options);
        GridGeometryMeasurement result = new GridGeometryMeasurement { SampleCount = area };
        SetLimitations(result, "Grid uniformity measurement is disabled.");
        if (!options.AnalyzeGridUniformity) { return result; }

        // Translate before fitting to preserve subpixel displacements far from
        // the image origin, then scale to avoid overflow in squared distances.
        QrPoint origin = centers[0];
        double scale = 0;
        foreach (QrPoint point in centers)
        {
            double dx = point.X - origin.X;
            double dy = point.Y - origin.Y;
            if (!double.IsFinite(dx) || !double.IsFinite(dy))
            {
                throw new QrInputException("The coordinate extent exceeds the supported numeric range.", "centers");
            }
            scale = global::System.Math.Max(scale, global::System.Math.Max(global::System.Math.Abs(dx), global::System.Math.Abs(dy)));
        }
        SetLimitations(result, "The supplied centers do not define two independent grid axes.");
        if (scale == 0) { return result; }
        double middleColumn = (columns - 1) / 2.0;
        double middleRow = (rows - 1) / 2.0;
        double meanX = 0;
        double meanY = 0;
        double horizontalX = 0;
        double horizontalY = 0;
        double verticalX = 0;
        double verticalY = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                QrPoint point = centers[row * columns + column];
                double x = (point.X - origin.X) / scale;
                double y = (point.Y - origin.Y) / scale;
                double columnOffset = column - middleColumn;
                double rowOffset = row - middleRow;
                meanX += x;
                meanY += y;
                horizontalX += columnOffset * x;
                horizontalY += columnOffset * y;
                verticalX += rowOffset * x;
                verticalY += rowOffset * y;
            }
        }
        meanX /= area;
        meanY /= area;
        double columnVariance = (double)area * ((double)columns * columns - 1) / 12;
        double rowVariance = (double)area * ((double)rows * rows - 1) / 12;
        horizontalX /= columnVariance;
        horizontalY /= columnVariance;
        verticalX /= rowVariance;
        verticalY /= rowVariance;
        double horizontalLength = global::System.Math.Sqrt(horizontalX * horizontalX + horizontalY * horizontalY);
        double verticalLength = global::System.Math.Sqrt(verticalX * verticalX + verticalY * verticalY);
        if (horizontalLength == 0 || verticalLength == 0) { return result; }
        double ux = horizontalX / horizontalLength;
        double uy = horizontalY / horizontalLength;
        double vx = verticalX / verticalLength;
        double vy = verticalY / verticalLength;
        if (global::System.Math.Abs(ux * vy - uy * vx) <= 1.0E-12) { return result; }
        double meanPitch = (horizontalLength + verticalLength) / 2;
        result.HorizontalPitch = horizontalLength * scale;
        result.VerticalPitch = verticalLength * scale;
        if (!double.IsFinite(result.HorizontalPitch) || !double.IsFinite(result.VerticalPitch)
            || result.HorizontalPitch == 0 || result.VerticalPitch == 0)
        {
            throw new QrInputException("The fitted module pitches exceed the supported numeric range.", "centers");
        }
        result.AxialNonUniformity.Value = global::System.Math.Abs(horizontalLength - verticalLength) / meanPitch;
        double cosine = global::System.Math.Clamp(ux * vx + uy * vy, -1.0, 1.0);
        result.GeometricDistortion.Value = global::System.Math.Asin(global::System.Math.Abs(cosine)) * 180 / global::System.Math.PI;
        result.ModuleDisplacements = new double[area];
        double maximumDisplacement = 0;
        double meanSquare = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                QrPoint point = centers[index];
                double expectedX = meanX + horizontalX * (column - middleColumn) + verticalX * (row - middleRow);
                double expectedY = meanY + horizontalY * (column - middleColumn) + verticalY * (row - middleRow);
                double dx = ((point.X - origin.X) / scale - expectedX) / meanPitch;
                double dy = ((point.Y - origin.Y) / scale - expectedY) / meanPitch;
                double squared = dx * dx + dy * dy;
                if (!double.IsFinite(squared))
                {
                    throw new QrInputException("Grid residuals exceed the supported numeric range.", "centers");
                }
                double displacement = global::System.Math.Sqrt(squared);
                result.ModuleDisplacements[index] = displacement;
                maximumDisplacement = global::System.Math.Max(maximumDisplacement, displacement);
                // Online mean avoids overflowing a sum of squared residuals.
                meanSquare += (squared - meanSquare) / (index + 1);
            }
        }
        result.GridNonUniformity.Value = maximumDisplacement;
        result.RootMeanSquareDisplacement = global::System.Math.Sqrt(meanSquare);
        result.AxialNonUniformity.IsCalculated = true;
        result.GridNonUniformity.IsCalculated = true;
        result.GeometricDistortion.IsCalculated = true;
        SetLimitations(result, "Calculated from uncalibrated detected centers using an affine reference grid; profile-specific verifier grading is not applied.");
        return result;
    }

    static int ValidateInput(QrPoint[] centers, int columns, int rows, QrQualityOptions options)
    {
        if (centers == null || options == null) { throw new QrInputException("Module centers and quality options are required.", "centers"); }
        if (columns < 2 || rows < 2) { throw new QrInputException("At least two rows and columns of centers are required.", "dimensions"); }
        CheckedDimensions.ValidateDimensionBounds(columns, rows, QrLimits.DefaultMaximumImageDimension,
            QrLimits.DefaultMaximumImageDimension, options.MaximumModuleSamples);
        int area = CheckedDimensions.CalculateArea(columns, rows, options.MaximumModuleSamples);
        if (centers.Length != area) { throw new QrInputException("Centers must match the grid dimensions.", "centers"); }
        int allocation = options.AnalyzeGridUniformity
            ? CheckedDimensions.CalculateAllocationBytes(area, 8, QrLimits.DefaultMaximumAllocationBytes) : 0;
        CheckedDimensions.CalculateSum(allocation, 512, QrLimits.DefaultMaximumAllocationBytes, "MaximumAllocationBytes");
        foreach (QrPoint point in centers)
        {
            if (point == null || !double.IsFinite(point.X) || !double.IsFinite(point.Y))
            {
                throw new QrInputException("Every module center must have finite coordinates.", "centers");
            }
        }
        return area;
    }

    static void SetLimitations(GridGeometryMeasurement result, string limitation)
    {
        result.AxialNonUniformity.Limitation = limitation;
        result.GridNonUniformity.Limitation = limitation;
        result.GeometricDistortion.Limitation = limitation;
    }
}
