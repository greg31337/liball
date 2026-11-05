namespace qrcode;

internal class QuietZoneMeasurer
{
    // The raster is already polarity-normalized: logical dark is 0 and the
    // quiet-zone background is 255, regardless of detection.Polarity metadata.
    internal static QrQuietZoneMeasurement Measure(
        QrRaster raster,
        QrDetection detection,
        QrDetectionOptions options)
    {
        BinaryMorphology.ValidateRaster(raster, options);
        if (detection == null || !detection.HasSymbolKindEstimate)
        {
            throw new QrInputException("A detection with an estimated symbol profile is required.", "detection");
        }

        int required = GetRequiredModules(detection.EstimatedSymbolKind);
        int width = detection.EstimatedWidth;
        int height = detection.EstimatedHeight;
        CheckedDimensions.ValidateDimensionBounds(width, height,
            QrLimits.Model2MaximumDimension, QrLimits.Model2MaximumDimension,
            QrLimits.Model2MaximumDimension * QrLimits.Model2MaximumDimension);
        QrQuadrilateral? corners = detection.Corners;
        if (corners == null)
        {
            throw new QrInputException("The detected symbol boundary is required.", "Corners");
        }

        QrQuadrilateral grid = new QrQuadrilateral
        {
            TopLeft = new QrPoint(), TopRight = new QrPoint { X = width },
            BottomRight = new QrPoint { X = width, Y = height }, BottomLeft = new QrPoint { Y = height }
        };
        PerspectiveTransform transform = PerspectiveTransform.Create(grid, corners);
        PerspectiveTransform inverse = transform.Invert();
        QrQuietZoneMeasurement result = new QrQuietZoneMeasurement { RequiredModules = required };
        QrPoint[]? boundary = ProjectBoundary(transform, width, height, required);
        if (boundary == null)
        {
            result.HasUnmappableBoundary = true;
            result.AcceptForDetection = options.AllowCroppedQuietZone;
            return result;
        }

        double minimumX = boundary[0].X;
        double minimumY = boundary[0].Y;
        double maximumX = minimumX;
        double maximumY = minimumY;
        foreach (QrPoint point in boundary)
        {
            minimumX = global::System.Math.Min(minimumX, point.X);
            minimumY = global::System.Math.Min(minimumY, point.Y);
            maximumX = global::System.Math.Max(maximumX, point.X);
            maximumY = global::System.Math.Max(maximumY, point.Y);
        }

        // Allow only numerical roundoff at an exact image boundary. A partially
        // cropped pixel still makes the physical margin incomplete.
        result.IsCropped = minimumX < -1.0E-7 || minimumY < -1.0E-7
            || maximumX > raster.Width + 1.0E-7 || maximumY > raster.Height + 1.0E-7;
        int firstColumn = (int)global::System.Math.Clamp(global::System.Math.Floor(minimumX), 0.0, raster.Width);
        int lastColumn = (int)global::System.Math.Clamp(global::System.Math.Ceiling(maximumX), 0.0, raster.Width);
        int firstRow = (int)global::System.Math.Clamp(global::System.Math.Floor(minimumY), 0.0, raster.Height);
        int lastRow = (int)global::System.Math.Clamp(global::System.Math.Ceiling(maximumY), 0.0, raster.Height);
        QrPoint module = new QrPoint();
        for (int row = firstRow; row < lastRow; row++)
        {
            for (int column = firstColumn; column < lastColumn; column++)
            {
                if (!inverse.TryMapPoint(column + 0.5, row + 0.5, module)
                    || module.X < -required || module.Y < -required
                    || module.X >= width + required || module.Y >= height + required)
                {
                    continue;
                }

                bool left = module.X < 0.0;
                bool top = module.Y < 0.0;
                bool right = module.X >= width;
                bool bottom = module.Y >= height;
                if (!left && !top && !right && !bottom)
                {
                    continue;
                }

                result.ObservedPixelCount++;
                if (raster.Pixels[row * raster.Stride + column] == 0)
                {
                    result.ContaminatedPixelCount++;
                }

                if (left)
                {
                    result.LeftPixelCount++;
                }

                if (top)
                {
                    result.TopPixelCount++;
                }

                if (right)
                {
                    result.RightPixelCount++;
                }

                if (bottom)
                {
                    result.BottomPixelCount++;
                }
            }
        }

        result.IsFullyObserved = !result.IsCropped && result.LeftPixelCount > 0
            && result.TopPixelCount > 0 && result.RightPixelCount > 0 && result.BottomPixelCount > 0;
        result.IsConformant = result.IsFullyObserved && result.ContaminatedPixelCount == 0;
        // Tolerant detection may retain a symbol with an absent, cropped, or
        // contaminated margin; it never upgrades the conformance measurement.
        result.AcceptForDetection = result.IsConformant || options.AllowCroppedQuietZone;
        return result;
    }

    static int GetRequiredModules(QrSymbolKind kind)
    {
        if (kind == QrSymbolKind.Model2)
        {
            return QrLimits.Model2QuietZoneModules;
        }

        if (kind == QrSymbolKind.LegacyModel1)
        {
            return QrLimits.LegacyModel1QuietZoneModules;
        }

        if (kind == QrSymbolKind.Micro)
        {
            return QrLimits.MicroQuietZoneModules;
        }

        if (kind == QrSymbolKind.rMQR)
        {
            return QrLimits.RectangularMicroQuietZoneModules;
        }

        throw new QrInputException("The symbol profile is not defined.", "EstimatedSymbolKind");
    }

    static QrPoint[]? ProjectBoundary(PerspectiveTransform transform, int width, int height, int required)
    {
        QrPoint[] logical = new QrPoint[]
        {
            new QrPoint { X = -required, Y = -required },
            new QrPoint { X = width + required, Y = -required },
            new QrPoint { X = width + required, Y = height + required },
            new QrPoint { X = -required, Y = height + required }
        };
        QrPoint[] mapped = new QrPoint[4];
        bool positive = false;
        for (int index = 0; index < logical.Length; index++)
        {
            QrPoint point = logical[index];
            double denominator = transform.M20 * point.X + transform.M21 * point.Y + transform.M22;
            if (index > 0 && (denominator > 0.0) != positive)
            {
                return null;
            }

            positive = denominator > 0.0;
            mapped[index] = new QrPoint();
            if (!transform.TryMapPoint(point.X, point.Y, mapped[index]))
            {
                return null;
            }
        }

        return mapped;
    }
}
