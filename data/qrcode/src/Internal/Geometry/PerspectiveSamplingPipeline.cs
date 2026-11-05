namespace qrcode;

internal class PerspectiveSamplingPipeline
{
    internal static QrDetection Sample(QrImage image, QrDetection detection,
        QrMatrix? expectedPatterns, QrDetectionOptions options)
    {
        PerspectiveTransform transform = CreateInitialTransform(detection, expectedPatterns, options);
        int width = detection.EstimatedWidth;
        int height = detection.EstimatedHeight;
        QrPolarity polarity = ResolvePolarity(detection.Polarity, options.Polarity);
        QrMatrix[] binaryImages = new QrBinarizer().BinarizeImage(image, options);
        int polarityIndex = options.Polarity == QrPolarity.Automatic && polarity == QrPolarity.LightOnDark ? 1 : 0;
        QrMatrix binary = binaryImages[polarityIndex];
        QrRaster raster = new QrRaster
        {
            Width = binary.Width, Height = binary.Height, Stride = binary.Width,
            Pixels = new byte[binary.Values.Length]
        };
        for (int index = 0; index < binary.Values.Length; index++)
        {
            raster.Pixels[index] = binary.Values[index] == QrModuleValue.Dark ? (byte)0 : (byte)255;
        }

        QrMatrix sampled;
        if (expectedPatterns == null)
        {
            sampled = GridSampler.Sample(raster, transform, width, height, options);
        }
        else
        {
            GridRefinementResult refinement = GridRefiner.Refine(raster, transform, expectedPatterns, options);
            transform = refinement.Transform;
            sampled = refinement.Matrix;
        }

        return CreateDetection(raster, detection, transform, sampled, polarity, options);
    }

    internal static PerspectiveTransform CreateInitialTransform(QrDetection detection, QrMatrix? expectedPatterns, QrDetectionOptions options)
    {
        if (detection == null || options == null)
        {
            throw new QrInputException("Detection and sampling options are required.",
                detection == null ? "detection" : "options");
        }

        int width = detection.EstimatedWidth;
        int height = detection.EstimatedHeight;
        CheckedDimensions.ValidateDimensionBounds(width, height,
            global::System.Math.Min(options.MaximumImageWidth, QrLimits.Model2MaximumDimension),
            global::System.Math.Min(options.MaximumImageHeight, QrLimits.Model2MaximumDimension),
            options.MaximumImagePixels);
        QrQuadrilateral? corners = detection.Corners;
        if (corners == null)
        {
            throw new QrInputException("A complete symbol boundary is required.", "Corners");
        }

        if (detection.HasSymbolKindEstimate
            && ((int)detection.EstimatedSymbolKind < (int)QrSymbolKind.Model2
                || (int)detection.EstimatedSymbolKind > (int)QrSymbolKind.LegacyModel1))
        {
            throw new QrInputException("The estimated symbol kind is not defined.", "EstimatedSymbolKind");
        }

        if (detection.Diagnostics == null)
        {
            throw new QrInputException("The diagnostics array is required.", "Diagnostics");
        }

        if (expectedPatterns != null && (expectedPatterns.Width != width || expectedPatterns.Height != height
            || expectedPatterns.QuietZoneLeft != 0 || expectedPatterns.QuietZoneTop != 0
            || expectedPatterns.QuietZoneRight != 0 || expectedPatterns.QuietZoneBottom != 0))
        {
            throw new QrInputException("Pattern evidence must match the symbol dimensions without a quiet zone.", "expectedPatterns");
        }

        QrQuadrilateral grid = new QrQuadrilateral
        {
            TopLeft = new QrPoint(), TopRight = new QrPoint { X = width },
            BottomRight = new QrPoint { X = width, Y = height }, BottomLeft = new QrPoint { Y = height }
        };
        return PerspectiveTransform.Create(grid, corners);
    }

    internal static QrDetection CreateDetection(QrRaster raster, QrDetection detection,
        PerspectiveTransform transform, QrMatrix sampled, QrPolarity polarity, QrDetectionOptions options)
    {
        int width = detection.EstimatedWidth;
        int height = detection.EstimatedHeight;
        QrDetection result = new QrDetection
        {
            Corners = new QrQuadrilateral
            {
                TopLeft = transform.MapPoint(new QrPoint()),
                TopRight = transform.MapPoint(new QrPoint { X = width }),
                BottomRight = transform.MapPoint(new QrPoint { X = width, Y = height }),
                BottomLeft = transform.MapPoint(new QrPoint { Y = height })
            },
            SampledMatrix = sampled,
            HasSymbolKindEstimate = detection.HasSymbolKindEstimate,
            EstimatedSymbolKind = detection.EstimatedSymbolKind,
            EstimatedVersion = detection.EstimatedVersion,
            EstimatedWidth = width, EstimatedHeight = height,
            Polarity = polarity,
            HasMirrorEstimate = detection.HasMirrorEstimate, IsMirrored = detection.IsMirrored,
            Confidence = detection.Confidence, ModuleSize = detection.ModuleSize,
            CandidateIndex = detection.CandidateIndex,
            Diagnostics = (string[])detection.Diagnostics.Clone()
        };
        if (result.HasSymbolKindEstimate)
        {
            result.QuietZone = QuietZoneMeasurer.Measure(raster, result, options);
        }

        return result;
    }

    internal static QrPolarity ResolvePolarity(QrPolarity detected, QrPolarity configured)
    {
        if ((int)detected < (int)QrPolarity.Automatic || (int)detected > (int)QrPolarity.LightOnDark
            || (int)configured < (int)QrPolarity.Automatic || (int)configured > (int)QrPolarity.LightOnDark)
        {
            throw new QrInputException("The polarity is not defined.", "Polarity");
        }

        if (detected != QrPolarity.Automatic && configured != QrPolarity.Automatic && detected != configured)
        {
            throw new QrInputException("Detection and configured polarities conflict.", "Polarity");
        }

        QrPolarity resolved = detected == QrPolarity.Automatic ? configured : detected;
        if (resolved == QrPolarity.Automatic)
        {
            throw new QrInputException("Sampling requires an explicit detection or configured polarity.", "Polarity");
        }

        return resolved;
    }
}
