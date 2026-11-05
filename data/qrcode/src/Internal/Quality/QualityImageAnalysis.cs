namespace qrcode;

internal class QualityImageAnalysis
{
    internal static QrQualityReport Analyze(QrImage image, QrDetection detection, QrDetectionOptions sampling, QrQualityOptions options)
    {
        QualityAnalysisPipeline.ValidateOptions(options);
        DetectorPipeline.ValidateOptions(sampling);
        if (detection == null) { throw new QrInputException("A symbol boundary is required.", "detection"); }
        int area = CheckedDimensions.CalculateArea(detection.EstimatedWidth, detection.EstimatedHeight, options.MaximumModuleSamples);
        int reads = options.AnalyzeContrast || options.AnalyzeModulation
            ? CheckedDimensions.CalculateAllocationBytes(area, options.SamplesPerModule * options.SamplesPerModule, options.MaximumModuleSamples) : 0;
        CheckedDimensions.CalculateSum(area, reads, options.MaximumModuleSamples, "MaximumModuleSamples");
        QrDetection fresh = PerspectiveSamplingPipeline.Sample(image, detection, null, sampling);
        QrMatrix? observed = fresh.SampledMatrix;
        if (observed == null) { throw new QrInputException("Sampling produced no matrix.", "detection"); }
        QualityAnalysisState state = QualityAnalysisPipeline.AnalyzeMatrix(observed, options);
        QrQualityReport report = state.Report;
        report.Limitations = new string[]
        {
            "Inputs are uncalibrated; ISO/IEC 15415 grades and verifier certification are unavailable.",
            "Logical validation describes the sampled matrix without image margins; the QuietZone metric describes the surrounding image.",
            "Binary damage, quiet-zone, and print-growth measurements reflect the selected preprocessing; contrast and modulation use original luminance."
        };
        fresh.HasSymbolKindEstimate = true;
        fresh.EstimatedSymbolKind = report.SymbolKind;
        PerspectiveTransform transform = PerspectiveSamplingPipeline.CreateInitialTransform(fresh, null, sampling);
        if (reads > 0)
        {
            // Never normalize contrast or binarize the luminance used for these
            // measurements, even when decoding uses those preprocessing options.
            QrImage gray = LuminanceConverter.ConvertToGray8(image, sampling);
            QrRaster samples = SampleLuminance(gray, transform, observed.Width, observed.Height, options);
            GrayscaleQualityMeasurement grayscale = GrayscaleQualityMeasurer.Measure(samples, options);
            report.Metrics[0] = grayscale.SymbolContrast;
            report.Metrics[1] = grayscale.Modulation;
        }
        if (options.AnalyzeGridUniformity)
        {
            QrPoint[] centers = new QrPoint[area];
            for (int row = 0; row < observed.Height; row++)
            {
                for (int column = 0; column < observed.Width; column++)
                {
                    centers[row * observed.Width + column] = transform.MapPoint(new QrPoint { X = column + 0.5, Y = row + 0.5 });
                }
            }
            GridGeometryMeasurement geometry = GridGeometryMeasurer.Measure(centers, observed.Width, observed.Height, options);
            report.Metrics[4] = geometry.AxialNonUniformity;
            report.Metrics[6] = geometry.GeometricDistortion;
            report.Metrics[4].Limitation = "Estimated from the supplied boundary, not independently observed module centers.";
            report.Metrics[6].Limitation = report.Metrics[4].Limitation;
            report.Metrics[5].Limitation = "Independent module centers are unavailable; interpolating the boundary cannot establish grid non-uniformity.";
        }
        QrRaster binary = CreateBinaryRaster(image, fresh, sampling, state.Recovery.IsInverted);
        QrQuietZoneMeasurement quiet = QuietZoneMeasurer.Measure(binary, fresh, sampling);
        report.Metrics[9].IsCalculated = quiet.IsFullyObserved;
        report.Metrics[9].Value = quiet.IsConformant ? 1 : 0;
        report.Metrics[9].Limitation = quiet.IsFullyObserved ? "Measured from the polarity-normalized binary image."
            : "The required quiet zone is cropped or not fully observed.";
        int printReads = 0;
        if (state.Reference != null)
        {
            QrMatrix reference = MatrixTransformer.Rotate(state.Reference, (4 - state.Recovery.QuarterTurnsClockwise) % 4);
            if (state.Recovery.IsMirrored) { reference = MatrixTransformer.Mirror(reference); }
            // rMQR's profile dimensions are defined with the long side horizontal.
            if (report.SymbolKind != QrSymbolKind.rMQR || reference.Width > reference.Height)
            {
                PrintGeometryMeasurement print = PrintGeometryMeasurer.Measure(binary, fresh, reference, sampling, options);
                report.Metrics[7] = print.HorizontalPrintGrowth;
                report.Metrics[8] = print.VerticalPrintGrowth;
                printReads = print.SampleCount;
            }
            else
            {
                report.Metrics[7].Limitation = "Print growth requires rMQR geometry with its long module axis first in the supplied boundary.";
                report.Metrics[8].Limitation = report.Metrics[7].Limitation;
            }
        }
        report.SampleCount = CheckedDimensions.CalculateSum(area, reads, options.MaximumModuleSamples, "MaximumModuleSamples");
        report.SampleCount = CheckedDimensions.CalculateSum(report.SampleCount, printReads, options.MaximumModuleSamples, "MaximumModuleSamples");
        return QualityAnalysisPipeline.Complete(report, options);
    }

    static QrRaster SampleLuminance(QrImage image, PerspectiveTransform transform, int width, int height, QrQualityOptions options)
    {
        QrRaster result = new QrRaster { Width = width, Height = height, Stride = width, Pixels = new byte[width * height] };
        int count = options.SamplesPerModule;
        double aperture = options.ApertureDiameterModules;
        QrPoint point = new QrPoint();
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                int sum = 0;
                for (int y = 0; y < count; y++)
                {
                    for (int x = 0; x < count; x++)
                    {
                        double moduleX = column + 0.5 + ((x + 0.5) / count - 0.5) * aperture;
                        double moduleY = row + 0.5 + ((y + 0.5) / count - 0.5) * aperture;
                        if (!transform.TryMapPoint(moduleX, moduleY, point) || point.X < 0 || point.Y < 0
                            || point.X >= image.Width || point.Y >= image.Height)
                        {
                            throw new QrInputException("Every requested luminance sample must lie within the image.", "detection");
                        }
                        sum += image.Pixels[(int)point.Y * image.Stride + (int)point.X];
                    }
                }
                result.Pixels[row * width + column] = (byte)((sum + count * count / 2) / (count * count));
            }
        }
        return result;
    }

    static QrRaster CreateBinaryRaster(QrImage image, QrDetection detection, QrDetectionOptions sampling, bool inverted)
    {
        QrMatrix[] binaries = new QrBinarizer().BinarizeImage(image, sampling);
        int index = sampling.Polarity == QrPolarity.Automatic && detection.Polarity == QrPolarity.LightOnDark ? 1 : 0;
        QrMatrix matrix = binaries[index];
        QrRaster result = new QrRaster { Width = matrix.Width, Height = matrix.Height, Stride = matrix.Width, Pixels = new byte[matrix.Values.Length] };
        for (int cell = 0; cell < result.Pixels.Length; cell++)
        {
            bool dark = matrix.Values[cell] == QrModuleValue.Dark;
            result.Pixels[cell] = dark != inverted ? (byte)0 : (byte)255;
        }
        return result;
    }
}
