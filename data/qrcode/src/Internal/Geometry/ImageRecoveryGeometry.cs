namespace qrcode;

internal class ImageRecoveryGeometry
{
    internal static PerspectiveTransform Shift(PerspectiveTransform initial, double x, double y)
    {
        return new PerspectiveTransform
        {
            M00 = initial.M00, M01 = initial.M01, M02 = initial.M02 + initial.M00 * x + initial.M01 * y,
            M10 = initial.M10, M11 = initial.M11, M12 = initial.M12 + initial.M10 * x + initial.M11 * y,
            M20 = initial.M20, M21 = initial.M21, M22 = initial.M22 + initial.M20 * x + initial.M21 * y
        };
    }

    internal static QrMatrix? CreatePatterns(QrDetection detection)
    {
        if (!detection.HasSymbolKindEstimate || detection.EstimatedSymbolKind == QrSymbolKind.LegacyModel1) { return null; }
        QrSymbolKind kind = detection.EstimatedSymbolKind;
        int width = detection.EstimatedWidth;
        int height = detection.EstimatedHeight;
        int version = -1;
        int level = 0;
        if (kind == QrSymbolKind.Model2 && width == height && width >= 21 && width <= 177 && (width - 17) % 4 == 0)
        {
            version = (width - 17) / 4;
        }
        else if (kind == QrSymbolKind.Micro && width == height && width >= 11 && width <= 17 && width % 2 == 1)
        {
            version = (width - 9) / 2;
            if (version == 1) { level = -1; }
        }
        else if (kind == QrSymbolKind.rMQR)
        {
            for (int index = 0; index < 32; index++)
            {
                if (RectangularMicroVersionTable.GetWidth(index) == width && RectangularMicroVersionTable.GetHeight(index) == height) { version = index; }
            }
        }

        if (version < 0) { return null; }
        BitMatrixBuffer bits = kind == QrSymbolKind.Model2 ? FunctionPatternBuilder.CreateModel2FunctionPatterns(version, level, 0)
            : kind == QrSymbolKind.Micro ? FunctionPatternBuilder.CreateMicroFunctionPatterns(version, level, 0)
            : FunctionPatternBuilder.CreateRectangularMicroFunctionPatterns(version, level);
        QrMatrix result = new QrMatrix { Width = width, Height = height, Values = new QrModuleValue[width * height],
            Roles = ModuleClassifier.CreateEncodingRoleMap(kind, version, level) };
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                QrModuleRole role = result.Roles[row * width + column];
                // Format/ECC/mask and payload guesses must not influence geometry.
                if (role == QrModuleRole.Finder || role == QrModuleRole.Timing || role == QrModuleRole.Alignment)
                {
                    result.Values[row * width + column] = bits.GetModule(row, column) ? QrModuleValue.Dark : QrModuleValue.Light;
                }
            }
        }

        return detection.HasMirrorEstimate && detection.IsMirrored ? MatrixTransformer.Mirror(result) : result;
    }

    internal static PerspectiveTransform[] FindAlignmentAlternatives(QrRaster raster, PerspectiveTransform initial,
        QrDetection detection, QrDetectionOptions options)
    {
        int size = detection.EstimatedWidth;
        if (!detection.HasSymbolKindEstimate || detection.EstimatedSymbolKind != QrSymbolKind.Model2
            || size != detection.EstimatedHeight || size < 25 || size > 177 || (size - 17) % 4 != 0)
        {
            return new PerspectiveTransform[0];
        }

        QrPoint tl = initial.MapPoint(new QrPoint { X = 3.5, Y = 3.5 });
        QrPoint tr = initial.MapPoint(new QrPoint { X = size - 3.5, Y = 3.5 });
        QrPoint bl = initial.MapPoint(new QrPoint { X = 3.5, Y = size - 3.5 });
        double module = global::System.Math.Sqrt((tr.X - tl.X) * (tr.X - tl.X) + (tr.Y - tl.Y) * (tr.Y - tl.Y)) / (size - 7);
        FinderGroup group = new FinderGroup(new FinderCandidate { CenterX = tl.X, CenterY = tl.Y, ModuleSize = module },
            new FinderCandidate { CenterX = tr.X, CenterY = tr.Y, ModuleSize = module },
            new FinderCandidate { CenterX = bl.X, CenterY = bl.Y, ModuleSize = module })
        {
            Model2Possible = true, EstimatedVersion = (size - 17) / 4, EstimatedDimension = size,
            ModuleSize = module, EstimatedBottomRight = initial.MapPoint(new QrPoint { X = size - 3.5, Y = size - 3.5 })
        };
        AlignmentPatternSearchResult found = AlignmentPatternDetector.Detect(raster, group, options);
        global::System.Collections.Generic.List<PerspectiveTransform> alternatives = new global::System.Collections.Generic.List<PerspectiveTransform>();
        // Prefer the furthest alignment anchor, then examine the other observed
        // anchors independently so one false match cannot determine every retry.
        for (int index = found.Patterns.Length - 1; index >= 0 && alternatives.Count < options.MaximumCandidates; index--)
        {
            AlignmentPatternDetection pattern = found.Patterns[index];
            if (!pattern.Found) { continue; }
            QrQuadrilateral source = new QrQuadrilateral
            {
                TopLeft = new QrPoint { X = 3.5, Y = 3.5 }, TopRight = new QrPoint { X = size - 3.5, Y = 3.5 },
                BottomRight = new QrPoint { X = pattern.LogicalColumn + 0.5, Y = pattern.LogicalRow + 0.5 },
                BottomLeft = new QrPoint { X = 3.5, Y = size - 3.5 }
            };
            QrQuadrilateral destination = new QrQuadrilateral { TopLeft = tl, TopRight = tr, BottomRight = pattern.Center, BottomLeft = bl };
            if (!DetectorPipeline.HasConvexGeometry(source) || !DetectorPipeline.HasConvexGeometry(destination)) { continue; }
            PerspectiveTransform transform = PerspectiveTransform.Create(source, destination);
            QrQuadrilateral boundary = new QrQuadrilateral { TopLeft = new QrPoint(), TopRight = new QrPoint(), BottomRight = new QrPoint(), BottomLeft = new QrPoint() };
            if (transform.TryMapPoint(0, 0, boundary.TopLeft) && transform.TryMapPoint(size, 0, boundary.TopRight)
                && transform.TryMapPoint(size, size, boundary.BottomRight) && transform.TryMapPoint(0, size, boundary.BottomLeft)
                && DetectorPipeline.HasConvexGeometry(boundary)) { alternatives.Add(transform); }
        }

        return alternatives.ToArray();
    }
}
