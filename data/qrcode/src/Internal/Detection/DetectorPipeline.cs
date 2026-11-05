namespace qrcode;

internal class DetectorPipeline
{
    internal static QrDetectionSet DetectImage(QrImage image, QrDetectionOptions options)
    {
        ValidateOptions(options);
        if (image == null)
        {
            throw new QrInputException("The image is required.", "image");
        }

        if (options.BinarizationMode == QrBinarizationMode.SuppliedBinary && image.PixelFormat != QrPixelFormat.Gray8)
        {
            throw new QrInputException("Supplied binary images must use Gray8 pixels.", "PixelFormat");
        }

        QrImage luminance;
        if (options.BinarizationMode == QrBinarizationMode.SuppliedBinary)
        {
            luminance = LuminanceConverter.ConvertToGray8(image, options);
            QrRaster supplied = new QrRaster
            {
                Pixels = luminance.Pixels, Width = luminance.Width, Height = luminance.Height, Stride = luminance.Stride
            };
            BinaryMorphology.ValidateRaster(supplied, options);
            return DetectPasses(supplied, Invert(supplied), new int[0], QrBinarizationMode.SuppliedBinary, options);
        }

        luminance = new QrBinarizer().CreateLuminanceImage(image, options);
        if (options.BinarizationMode == QrBinarizationMode.GlobalHistogram)
        {
            GlobalHistogramResult global = GlobalHistogramBinarizer.Binarize(luminance, options);
            return DetectPasses(global.DarkOnLight, global.LightOnDark, new int[] { global.Threshold },
                QrBinarizationMode.GlobalHistogram, options);
        }

        HybridBinarizerResult hybrid = HybridBinarizer.Binarize(luminance, options);
        return DetectPasses(hybrid.DarkOnLight, hybrid.LightOnDark, hybrid.Thresholds,
            hybrid.UsedGlobalHistogram ? QrBinarizationMode.GlobalHistogram : QrBinarizationMode.HybridLocal, options);
    }

    internal static QrDetectionSet DetectBinaryMatrix(QrMatrix matrix, QrDetectionOptions options)
    {
        QrRaster raster = CreateBinaryRaster(matrix, options);
        return DetectPasses(raster, Invert(raster), new int[0], QrBinarizationMode.SuppliedBinary, options);
    }

    internal static QrRaster CreateBinaryRaster(QrMatrix matrix, QrDetectionOptions options)
    {
        ValidateOptions(options);
        if (matrix == null)
        {
            throw new QrInputException("The binary matrix is required.", "matrix");
        }

        CheckedDimensions.ValidateDimensionBounds(matrix.Width, matrix.Height,
            options.MaximumImageWidth, options.MaximumImageHeight, options.MaximumImagePixels);
        int count = CheckedDimensions.CalculateArea(matrix.Width, matrix.Height, options.MaximumImagePixels);
        if (matrix.Values == null || matrix.Values.Length != count)
        {
            throw new QrInputException("Binary values must match the image dimensions.", "Values");
        }

        CheckedDimensions.CalculateAllocationBytes(count, 1, QrLimits.DefaultMaximumAllocationBytes);
        QrRaster raster = new QrRaster
        {
            Pixels = new byte[count], Width = matrix.Width, Height = matrix.Height, Stride = matrix.Width
        };
        for (int index = 0; index < count; index++)
        {
            QrModuleValue value = matrix.Values[index];
            if (value != QrModuleValue.Dark && value != QrModuleValue.Light)
            {
                throw new QrInputException("Binary image pixels must be light or dark, not unknown.", "Values");
            }

            raster.Pixels[index] = value == QrModuleValue.Dark ? (byte)0 : (byte)255;
        }

        return raster;
    }

    internal static QrDetectionSet DetectPasses(QrRaster darkOnLight, QrRaster lightOnDark,
        int[] thresholds, QrBinarizationMode mode, QrDetectionOptions options)
    {
        QrDetectionSet result = new QrDetectionSet { BinarizationMode = mode, Thresholds = thresholds };
        global::System.Collections.Generic.List<QrDetection> candidates = new global::System.Collections.Generic.List<QrDetection>();
        QrRaster? cleanedDark = null;
        QrRaster? cleanedLight = null;
        if (options.Polarity != QrPolarity.LightOnDark)
        {
            cleanedDark = BinaryMorphology.Clean(darkOnLight, options);
            DetectPass(cleanedDark, QrPolarity.DarkOnLight, options, candidates, result);
        }

        if (options.Polarity != QrPolarity.DarkOnLight)
        {
            cleanedLight = BinaryMorphology.Clean(lightOnDark, options);
            DetectPass(cleanedLight, QrPolarity.LightOnDark, options, candidates, result);
        }

        result.Detections = DetectionDeduplicator.SelectDetections(candidates.ToArray(), options);
        QrPolarity polarity = options.Polarity;
        if (result.Detections.Length > 0)
        {
            polarity = result.Detections[0].Polarity;
            foreach (QrDetection detection in result.Detections)
            {
                if (detection.Polarity != polarity)
                {
                    polarity = QrPolarity.Automatic;
                    break;
                }
            }
        }

        result.Polarity = polarity;
        result.BinaryRaster = polarity == QrPolarity.DarkOnLight ? cleanedDark
            : polarity == QrPolarity.LightOnDark ? cleanedLight : null;
        result.Diagnostics = new string[]
        {
            "Detection returns geometry and profile estimates; payload sampling and decoding are separate operations.",
            "A binary raster is retained only when one polarity represents the selected results."
        };
        return result;
    }

    static void DetectPass(QrRaster raster, QrPolarity polarity, QrDetectionOptions options,
        global::System.Collections.Generic.List<QrDetection> detections, QrDetectionSet result)
    {
        if (!options.AllowModel2 && !options.AllowLegacyModel1 && !options.AllowMicro && !options.AllowRectangularMicro)
        {
            return;
        }

        ImagePyramidLevel[] levels = ImagePyramid.Build(raster, options);
        global::System.Collections.Generic.List<FinderCandidate> allFinders = new global::System.Collections.Generic.List<FinderCandidate>();
        foreach (ImagePyramidLevel level in levels)
        {
            FinderCandidate[] scanned = RunRatioScanner.Scan(level.Raster, level.ScaleToSource, options);
            FinderCandidate[] validated = FinderRegionValidator.Validate(level.Raster, scanned, options);
            foreach (FinderCandidate finder in validated)
            {
                finder.Polarity = polarity;
                allFinders.Add(finder);
            }
        }

        FinderCandidate[] finders = FinderClusterer.Merge(allFinders.ToArray(), options);
        global::System.Collections.Generic.Dictionary<FinderCandidate, double> squareFinders =
            new global::System.Collections.Generic.Dictionary<FinderCandidate, double>();
        if (options.AllowModel2 || options.AllowLegacyModel1)
        {
            FinderGroup[] groups = FinderGrouper.Group(finders, options);
            foreach (FinderGroup group in groups)
            {
                QrDetection? detection = CreateSquare(raster, group, options);
                if (detection != null)
                {
                    RecordSquareFinder(squareFinders, group.TopLeft, detection.Confidence);
                    RecordSquareFinder(squareFinders, group.TopRight, detection.Confidence);
                    RecordSquareFinder(squareFinders, group.BottomLeft, detection.Confidence);
                    AddDetection(raster, detection, options, detections, result);
                }
            }
        }

        // Select a single result only after all profiles and polarity passes
        // compete, so a rejected profile hypothesis cannot hide another symbol.
        QrDetectionOptions profileOptions = new QrDetectionOptions
        {
            AllowMicro = options.AllowMicro, AllowRectangularMicro = options.AllowRectangularMicro,
            MaximumCandidates = options.MaximumCandidates, AllowCroppedQuietZone = options.AllowCroppedQuietZone,
            MaximumImageWidth = options.MaximumImageWidth, MaximumImageHeight = options.MaximumImageHeight,
            MaximumImagePixels = options.MaximumImagePixels, DetectMultiple = true
        };
        RectangularMicroFinderDetection[] rectangulars = RectangularMicroFinderDetector.Detect(raster, finders, profileOptions);
        global::System.Collections.Generic.Dictionary<FinderCandidate, double> rectangularFinders =
            new global::System.Collections.Generic.Dictionary<FinderCandidate, double>();
        foreach (RectangularMicroFinderDetection rectangular in rectangulars)
        {
            rectangularFinders[rectangular.Finder] = rectangular.Confidence;
        }

        foreach (MicroFinderDetection micro in MicroFinderDetector.Detect(raster, finders, profileOptions))
        {
            if (squareFinders.ContainsKey(micro.Finder) && squareFinders[micro.Finder] >= micro.Confidence
                || rectangularFinders.ContainsKey(micro.Finder) && rectangularFinders[micro.Finder] >= micro.Confidence)
            {
                continue;
            }

            AddDetection(raster, new QrDetection
            {
                Corners = micro.SymbolCorners, HasSymbolKindEstimate = true, EstimatedSymbolKind = QrSymbolKind.Micro,
                EstimatedVersion = micro.Version, EstimatedWidth = micro.Dimension, EstimatedHeight = micro.Dimension,
                Polarity = polarity, ModuleSize = micro.Finder.ModuleSize, Confidence = micro.Confidence,
                HasMirrorEstimate = micro.HasMirrorEstimate, IsMirrored = micro.IsMirrored,
                Diagnostics = new string[] { "Micro QR timing, format, and extent evidence accepted." }
            }, options, detections, result);
        }

        foreach (RectangularMicroFinderDetection rectangular in rectangulars)
        {
            if (squareFinders.ContainsKey(rectangular.Finder) && squareFinders[rectangular.Finder] >= rectangular.Confidence)
            {
                continue;
            }

            AddDetection(raster, new QrDetection
            {
                Corners = rectangular.SymbolCorners, HasSymbolKindEstimate = true, EstimatedSymbolKind = QrSymbolKind.rMQR,
                EstimatedVersion = rectangular.VersionIndex, EstimatedWidth = rectangular.Width, EstimatedHeight = rectangular.Height,
                Polarity = polarity, ModuleSize = rectangular.Finder.ModuleSize, Confidence = rectangular.Confidence,
                HasMirrorEstimate = rectangular.HasMirrorEstimate,
                IsMirrored = rectangular.HasMirrorEstimate && rectangular.IsMirrored,
                Diagnostics = new string[] { "Rectangular Micro QR finder, subfinder, and format evidence accepted." }
            }, options, detections, result);
        }
    }

    static void RecordSquareFinder(global::System.Collections.Generic.Dictionary<FinderCandidate, double> finders,
        FinderCandidate finder, double confidence)
    {
        if (!finders.ContainsKey(finder) || confidence > finders[finder])
        {
            finders[finder] = confidence;
        }
    }

    static QrDetection? CreateSquare(QrRaster raster, FinderGroup group, QrDetectionOptions options)
    {
        TimingPatternRefinementResult timing = TimingPatternRefiner.Refine(raster, group, options);
        if (!timing.Success)
        {
            return null;
        }

        group.EstimatedVersion = timing.RefinedVersion;
        group.EstimatedDimension = timing.RefinedDimension;
        group.LegacyModel1Possible = group.LegacyModel1Possible
            && timing.RefinedVersion <= QrLimits.LegacyModel1MaximumVersion;
        AlignmentPatternSearchResult alignment = AlignmentPatternDetector.Detect(raster, group, options);
        int dimension = group.EstimatedDimension;
        QrQuadrilateral centers = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = group.TopLeft.CenterX, Y = group.TopLeft.CenterY },
            TopRight = new QrPoint { X = group.TopRight.CenterX, Y = group.TopRight.CenterY },
            BottomRight = alignment.RefinedBottomRight,
            BottomLeft = new QrPoint { X = group.BottomLeft.CenterX, Y = group.BottomLeft.CenterY }
        };
        // Refinement is bounded, but a false alignment hit must not cause an
        // invalid homography to escape as a programmer-input exception.
        if (!HasConvexGeometry(centers))
        {
            centers.BottomRight = group.EstimatedBottomRight;
        }

        QrQuadrilateral logicalCenters = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = 3.5, Y = 3.5 },
            TopRight = new QrPoint { X = dimension - 3.5, Y = 3.5 },
            BottomRight = new QrPoint { X = dimension - 3.5, Y = dimension - 3.5 },
            BottomLeft = new QrPoint { X = 3.5, Y = dimension - 3.5 }
        };
        PerspectiveTransform transform = PerspectiveTransform.Create(logicalCenters, centers);
        QrQuadrilateral corners = new QrQuadrilateral
        {
            TopLeft = new QrPoint(), TopRight = new QrPoint(), BottomRight = new QrPoint(), BottomLeft = new QrPoint()
        };
        if (!transform.TryMapPoint(0, 0, corners.TopLeft)
            || !transform.TryMapPoint(dimension, 0, corners.TopRight)
            || !transform.TryMapPoint(dimension, dimension, corners.BottomRight)
            || !transform.TryMapPoint(0, dimension, corners.BottomLeft) || !HasConvexGeometry(corners))
        {
            return null;
        }

        return new QrDetection
        {
            Corners = corners, HasSymbolKindEstimate = true, EstimatedSymbolKind = group.EstimatedSymbolKind,
            EstimatedVersion = group.EstimatedVersion, EstimatedWidth = dimension, EstimatedHeight = dimension,
            Polarity = group.Polarity, ModuleSize = (timing.HorizontalModuleSize + timing.VerticalModuleSize) / 2.0,
            Confidence = (group.Confidence + timing.Confidence) / 2.0,
            Diagnostics = new string[]
            {
                group.Model2Possible && group.LegacyModel1Possible
                    ? "Three-finder geometry is compatible with Model 2 and legacy Model 1; Model 2 is the provisional estimate."
                    : "Three-finder geometry and timing evidence accepted.",
                alignment.UsedFallback ? "Unavailable alignment evidence retained the geometry fallback." : "Alignment geometry evaluated."
            }
        };
    }

    static void AddDetection(QrRaster raster, QrDetection detection, QrDetectionOptions options,
        global::System.Collections.Generic.List<QrDetection> detections, QrDetectionSet result)
    {
        detection.CandidateIndex = result.CandidateCount;
        result.CandidateCount++;
        QrQuietZoneMeasurement quiet = QuietZoneMeasurer.Measure(raster, detection, options);
        detection.QuietZone = quiet;
        if (!quiet.AcceptForDetection)
        {
            return;
        }

        // Bound intermediate symbol candidates across the two polarity passes.
        if ((long)detections.Count < (long)options.MaximumCandidates * 2)
        {
            detections.Add(detection);
            return;
        }

        int worst = 0;
        for (int index = 1; index < detections.Count; index++)
        {
            if (detections[index].Confidence < detections[worst].Confidence
                || detections[index].Confidence == detections[worst].Confidence
                    && detections[index].CandidateIndex > detections[worst].CandidateIndex)
            {
                worst = index;
            }
        }

        if (detection.Confidence > detections[worst].Confidence)
        {
            detections[worst] = detection;
        }
    }

    internal static bool HasConvexGeometry(QrQuadrilateral corners)
    {
        QrPoint? a = corners.TopLeft;
        QrPoint? b = corners.TopRight;
        QrPoint? c = corners.BottomRight;
        QrPoint? d = corners.BottomLeft;
        if (a == null || b == null || c == null || d == null)
        {
            return false;
        }

        QrPoint[] points = new QrPoint[] { a, b, c, d };
        double scale = 0.0;
        foreach (QrPoint point in points)
        {
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
            {
                return false;
            }

            scale = global::System.Math.Max(scale, global::System.Math.Abs(point.X - a.X));
            scale = global::System.Math.Max(scale, global::System.Math.Abs(point.Y - a.Y));
        }

        if (scale == 0.0 || !double.IsFinite(scale))
        {
            return false;
        }

        bool positive = false;
        for (int index = 0; index < 4; index++)
        {
            QrPoint first = points[index];
            QrPoint second = points[(index + 1) % 4];
            QrPoint third = points[(index + 2) % 4];
            double cross = (second.X - first.X) / scale * ((third.Y - second.Y) / scale)
                - (second.Y - first.Y) / scale * ((third.X - second.X) / scale);
            if (global::System.Math.Abs(cross) <= 1.0E-10 || index > 0 && (cross > 0.0) != positive)
            {
                return false;
            }

            positive = cross > 0.0;
        }

        return true;
    }

    internal static QrRaster Invert(QrRaster raster)
    {
        QrRaster result = new QrRaster
        {
            Pixels = new byte[raster.Pixels.Length], Width = raster.Width, Height = raster.Height, Stride = raster.Stride
        };
        for (int index = 0; index < result.Pixels.Length; index++)
        {
            result.Pixels[index] = (byte)(255 - raster.Pixels[index]);
        }

        return result;
    }

    internal static void ValidateOptions(QrDetectionOptions options)
    {
        if (options == null)
        {
            throw new QrInputException("The detection options are required.", "options");
        }

        if (options.MaximumCandidates <= 0 || options.MaximumPyramidLevels <= 0 || options.MinimumModuleSize <= 0
            || options.MaximumImageWidth <= 0 || options.MaximumImageHeight <= 0 || options.MaximumImagePixels <= 0)
        {
            throw new QrInputException("Detection resource limits and the minimum module size must be positive.", "options");
        }

        if ((int)options.Polarity < (int)QrPolarity.Automatic || (int)options.Polarity > (int)QrPolarity.LightOnDark
            || (int)options.BinarizationMode < (int)QrBinarizationMode.Automatic
            || (int)options.BinarizationMode > (int)QrBinarizationMode.SuppliedBinary)
        {
            throw new QrInputException("The polarity or binarization mode is not defined.", "options");
        }
    }
}
