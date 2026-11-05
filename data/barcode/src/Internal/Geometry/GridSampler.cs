namespace barcode;

internal static class GridSampler
{
    internal static ModuleMatrix SampleGrid(
        ModuleMatrix source,
        int outputWidth,
        int outputHeight,
        PerspectiveTransform destinationToSource)
    {
        if (source == null)
        {
            throw new BarcodeValidationException("Source matrix must not be null.");
        }

        CheckedMath.ValidateDimension(outputWidth);
        CheckedMath.ValidateDimension(outputHeight);
        CheckedMath.CalculateAllocationLength(outputWidth, outputHeight, 1, DecodingContext.DefaultMaximumModules);
        if (destinationToSource == null)
        {
            throw new BarcodeValidationException("Grid transform must not be null.");
        }

        ModuleMatrix sampled = new ModuleMatrix(outputWidth, outputHeight);
        int y = 0;
        while (y < outputHeight)
        {
            BarcodePoint[] points = new BarcodePoint[outputWidth];
            int x = 0;
            while (x < outputWidth)
            {
                BarcodePoint destination = GeometryMath.CreatePoint(x + 0.5, y + 0.5);
                points[x] = destinationToSource.TransformPoint(destination);
                x = x + 1;
            }

            NudgePointsToBounds(points, source.Width, source.Height);
            x = 0;
            while (x < outputWidth)
            {
                int sourceX = (int)global::System.Math.Floor(points[x].X);
                int sourceY = (int)global::System.Math.Floor(points[x].Y);
                sampled.SetModule(x, y, source.GetModule(sourceX, sourceY));
                x = x + 1;
            }

            y = y + 1;
        }

        return sampled;
    }

    internal static ModuleMatrix SampleQuadrilateralGrid(
        ModuleMatrix source,
        int outputWidth,
        int outputHeight,
        BarcodeQuadrilateral sourceQuadrilateral)
    {
        CheckedMath.ValidateDimension(outputWidth);
        CheckedMath.ValidateDimension(outputHeight);
        BarcodeQuadrilateral destination = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(0.0, 0.0),
            GeometryMath.CreatePoint(outputWidth, 0.0),
            GeometryMath.CreatePoint(outputWidth, outputHeight),
            GeometryMath.CreatePoint(0.0, outputHeight));
        PerspectiveTransform transform = PerspectiveTransform.CreateQuadrilateralMapping(destination, sourceQuadrilateral);
        return SampleGrid(source, outputWidth, outputHeight, transform);
    }

    internal static void NudgePointsToBounds(BarcodePoint[] points, int width, int height)
    {
        if (points == null)
        {
            throw new BarcodeValidationException("Sample points must not be null.");
        }

        CheckedMath.ValidateDimension(width);
        CheckedMath.ValidateDimension(height);
        int index = 0;
        while (index < points.Length)
        {
            BarcodePoint point = points[index];
            GeometryMath.ValidatePoint(point);
            if (point.X < -1.0 || point.X > width || point.Y < -1.0 || point.Y > height)
            {
                throw new BarcodeDecodingException("Transformed sample point lies outside the source matrix.");
            }

            if (point.X < 0.0)
            {
                point.X = 0.0;
            }
            else if (point.X > width - 1)
            {
                point.X = width - 1;
            }

            if (point.Y < 0.0)
            {
                point.Y = 0.0;
            }
            else if (point.Y > height - 1)
            {
                point.Y = height - 1;
            }

            index = index + 1;
        }
    }
}
