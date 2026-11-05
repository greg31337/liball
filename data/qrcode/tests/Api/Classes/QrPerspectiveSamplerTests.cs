using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrPerspectiveSamplerTests
{
    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void SamplesEachFamilyAndPolarityWithoutChangingCallerData(QrSymbolKind kind, bool inverted)
    {
        QrSymbol symbol = CreateSymbol(kind);
        QrImage image = Render(symbol, inverted);
        QrDetection detection = Detection(symbol, inverted);
        detection.Diagnostics = ["caller geometry"];
        detection.QuietZone = new QrQuietZoneMeasurement { RequiredModules = 99 };
        detection.SampledMatrix = new QrMatrix { Values = [QrModuleValue.Unknown] };
        byte[] originalPixels = (byte[])image.Pixels.Clone();
        QrDetectionOptions options = BinaryOptions();
        IQrPerspectiveSampler sampler = new QrPerspectiveSampler();

        QrDetection result = sampler.SampleDetection(image, detection, options);

        Assert.That(result.SampledMatrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(result.SampledMatrix.Confidence, Is.All.EqualTo(255));
        Assert.That(result.SampledMatrix.Width, Is.EqualTo(symbol.Matrix.Width));
        Assert.That(result.SampledMatrix.Height, Is.EqualTo(symbol.Matrix.Height));
        Assert.That(result.Polarity, Is.EqualTo(detection.Polarity));
        Assert.That(result.EstimatedSymbolKind, Is.EqualTo(kind));
        Assert.That(result.EstimatedVersion, Is.EqualTo(detection.EstimatedVersion));
        Assert.That(result.CandidateIndex, Is.EqualTo(7));
        Assert.That(result.Confidence, Is.EqualTo(0.8));
        Assert.That(result.ModuleSize, Is.EqualTo(6));
        Assert.That(result.QuietZone!.IsConformant, Is.True);
        Assert.That(result.QuietZone, Is.Not.SameAs(detection.QuietZone));
        Assert.That(result.Corners, Is.Not.SameAs(detection.Corners));
        Assert.That(result.Corners!.TopLeft, Is.Not.SameAs(detection.Corners!.TopLeft));
        result.Corners.TopLeft!.X = 999;
        result.Diagnostics[0] = "changed";
        result.SampledMatrix.Values[0] = QrModuleValue.Unknown;
        Assert.That(detection.Corners.TopLeft!.X, Is.EqualTo(24));
        Assert.That(detection.Diagnostics, Is.EqualTo(new[] { "caller geometry" }));
        Assert.That(detection.QuietZone.RequiredModules, Is.EqualTo(99));
        Assert.That(detection.SampledMatrix.Values, Has.Length.EqualTo(1));
        Assert.That(symbol.Matrix.Values[0], Is.EqualTo(QrModuleValue.Dark));
        Assert.That(image.Pixels, Is.EqualTo(originalPixels));
        Assert.That(options.Polarity, Is.EqualTo(QrPolarity.Automatic));
    }

    [TestCase(QrBinarizationMode.Automatic)]
    [TestCase(QrBinarizationMode.GlobalHistogram)]
    [TestCase(QrBinarizationMode.HybridLocal)]
    public void SamplesRawRgbWithPaddedStride(QrBinarizationMode mode)
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrImage gray = Render(symbol, false);
        QrImage rgb = new QrImage
        {
            Width = gray.Width, Height = gray.Height, Stride = gray.Width * 3 + 5,
            PixelFormat = QrPixelFormat.Rgb24, Pixels = new byte[(gray.Width * 3 + 5) * gray.Height]
        };
        Array.Fill(rgb.Pixels, (byte)123);
        for (int y = 0; y < gray.Height; y++)
            for (int x = 0; x < gray.Width; x++)
                for (int channel = 0; channel < 3; channel++)
                    rgb.Pixels[y * rgb.Stride + x * 3 + channel] = gray.Pixels[y * gray.Stride + x] == 0 ? (byte)25 : (byte)220;
        byte[] before = (byte[])rgb.Pixels.Clone();
        QrDetection result = new QrPerspectiveSampler().SampleDetection(rgb, Detection(symbol, false),
            new QrDetectionOptions { BinarizationMode = mode });
        Assert.That(result.SampledMatrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(rgb.Pixels, Is.EqualTo(before));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PreservesCornerOrientationForRotatedAndReflectedPerspectiveImages(bool mirrored)
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.rMQR);
        QrMatrix matrix = symbol.Matrix!;
        PerspectiveTransform actual = new PerspectiveTransform
        {
            M00 = 0.3, M01 = mirrored ? 5.0 : -5.0, M02 = mirrored ? 15 : 110,
            M10 = 5.0, M11 = 0.2, M12 = 15,
            M20 = 0.0005, M21 = 0.001, M22 = 1
        };
        QrImage image = Warp(matrix, actual, 240, 450);
        QrDetection detection = Detection(symbol, false);
        detection.Corners = ProjectCorners(actual, matrix.Width, matrix.Height);
        detection.HasMirrorEstimate = true;
        detection.IsMirrored = mirrored;
        QrDetection result = new QrPerspectiveSampler().SampleDetection(image, detection, BinaryOptions());
        Assert.That(result.SampledMatrix!.Values, Is.EqualTo(matrix.Values));
        Assert.That(result.HasMirrorEstimate, Is.True);
        Assert.That(result.IsMirrored, Is.EqualTo(mirrored));
    }

    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void RefinesShiftedDetectionsAndReturnsReusableGeometry(QrSymbolKind kind, bool multiPoint)
    {
        QrSymbol symbol = CreateSymbol(kind);
        QrImage image = Render(symbol, false);
        QrDetection detection = Detection(symbol, false);
        foreach (QrPoint point in Points(detection.Corners!)) { point.X += 3.6; point.Y -= 3.3; }
        QrDetectionOptions options = BinaryOptions();
        options.UseMultiPointSampling = multiPoint;
        IQrPerspectiveSampler sampler = new QrPerspectiveSampler();
        QrMatrix evidence = symbol.Matrix!;
        QrModuleValue[] originalValues = (QrModuleValue[])evidence.Values.Clone();
        QrModuleRole[] originalRoles = (QrModuleRole[])evidence.Roles!.Clone();
        QrDetection before = sampler.SampleDetection(image, detection, options);
        QrDetection result = sampler.ResampleDetection(image, detection, evidence, options);
        QrDetection repeated = sampler.SampleDetection(image, result, options);

        Assert.That(before.SampledMatrix!.Values, Is.Not.EqualTo(evidence.Values));
        Assert.That(result.SampledMatrix!.Values, Is.EqualTo(evidence.Values));
        Assert.That(repeated.SampledMatrix!.Values, Is.EqualTo(result.SampledMatrix.Values));
        Assert.That(result.Corners!.TopLeft!.X, Is.Not.EqualTo(detection.Corners!.TopLeft!.X));
        Assert.That(result.QuietZone, Is.Not.Null);
        Assert.That(detection.Corners.TopLeft.X, Is.EqualTo(27.6));
        Assert.That(detection.SampledMatrix, Is.Null);
        Assert.That(evidence.Values, Is.EqualTo(originalValues));
        Assert.That(evidence.Roles, Is.EqualTo(originalRoles));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DisabledRefinementOrMissingEvidenceRetainsInitialSamples(bool disable)
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrDetection detection = Detection(symbol, false);
        foreach (QrPoint point in Points(detection.Corners!)) point.X += 3.6;
        QrDetectionOptions options = BinaryOptions();
        options.RefineGrid = !disable;
        QrMatrix evidence = symbol.Matrix!;
        if (!disable) Array.Fill(evidence.Roles!, QrModuleRole.Unknown);
        QrImage image = Render(symbol, false);
        IQrPerspectiveSampler sampler = new QrPerspectiveSampler();
        QrDetection initial = sampler.SampleDetection(image, detection, options);
        QrDetection refined = sampler.ResampleDetection(image, detection, evidence, options);
        Assert.That(refined.SampledMatrix!.Values, Is.EqualTo(initial.SampledMatrix!.Values));
        Assert.That(refined.SampledMatrix.Confidence, Is.EqualTo(initial.SampledMatrix.Confidence));
        Assert.That(refined.Corners!.TopLeft!.X, Is.EqualTo(initial.Corners!.TopLeft!.X).Within(1e-9));
    }

    [Test]
    public void CroppedSamplesAreUnknownAndDoNotRequireAProfileEstimate()
    {
        QrImage image = new QrImage { Width = 2, Height = 1, Stride = 2, Pixels = [0, 255] };
        QrDetection detection = new QrDetection
        {
            EstimatedWidth = 3, EstimatedHeight = 1,
            Corners = ProjectCorners(new PerspectiveTransform { M00 = 1, M11 = 1, M02 = -1, M22 = 1 }, 3, 1)
        };
        QrDetectionOptions options = BinaryOptions();
        options.Polarity = QrPolarity.DarkOnLight;
        QrDetection result = new QrPerspectiveSampler().SampleDetection(image, detection, options);
        Assert.That(result.SampledMatrix!.Values, Is.EqualTo(new[] { QrModuleValue.Unknown, QrModuleValue.Dark, QrModuleValue.Light }));
        Assert.That(result.SampledMatrix.Confidence, Is.EqualTo(new byte[] { 0, 255, 255 }));
        Assert.That(result.QuietZone, Is.Null);
        Assert.That(result.Polarity, Is.EqualTo(QrPolarity.DarkOnLight));
        Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.Automatic));
    }

    [TestCase(false, 255)]
    [TestCase(true, 153)]
    public void MultiPointSamplingReportsDisagreement(bool multiPoint, int confidence)
    {
        QrImage image = new QrImage { Width = 2, Height = 1, Stride = 2, Pixels = [0, 255] };
        QrDetection detection = new QrDetection
        {
            EstimatedWidth = 1, EstimatedHeight = 1, Polarity = QrPolarity.DarkOnLight,
            Corners = ProjectCorners(new PerspectiveTransform { M00 = 1, M11 = 1, M02 = 0.4, M22 = 1 }, 1, 1)
        };
        QrDetectionOptions options = BinaryOptions();
        options.UseMultiPointSampling = multiPoint;
        QrDetection result = new QrPerspectiveSampler().SampleDetection(image, detection, options);
        Assert.That(result.SampledMatrix!.Values, Is.EqualTo(new[] { QrModuleValue.Dark }));
        Assert.That(result.SampledMatrix.Confidence, Is.EqualTo(new[] { (byte)confidence }));
    }

    [TestCase(QrPolarity.DarkOnLight, false)]
    [TestCase(QrPolarity.DarkOnLight, true)]
    [TestCase(QrPolarity.LightOnDark, false)]
    [TestCase(QrPolarity.LightOnDark, true)]
    public void UsesExplicitConfiguredPolarity(QrPolarity polarity, bool unknownDetection)
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        bool inverted = polarity == QrPolarity.LightOnDark;
        QrDetection detection = Detection(symbol, inverted);
        if (unknownDetection) detection.Polarity = QrPolarity.Automatic;
        QrDetectionOptions options = BinaryOptions();
        options.Polarity = polarity;
        QrDetection result = new QrPerspectiveSampler().SampleDetection(Render(symbol, inverted), detection, options);
        Assert.That(result.SampledMatrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(result.Polarity, Is.EqualTo(polarity));
    }

    [Test]
    public void CanSampleGeometryReturnedByTheDetector()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrImage image = Render(symbol, false);
        QrDetectionOptions options = BinaryOptions();
        options.AllowMicro = false;
        options.AllowRectangularMicro = false;
        QrDetectionSet detected = new QrDetector().DetectImage(image, options);
        Assert.That(detected.Detections, Has.Length.EqualTo(1));
        QrDetection result = new QrPerspectiveSampler().SampleDetection(image, detected.Detections[0], options);
        Assert.That(result.SampledMatrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(detected.Detections[0].SampledMatrix, Is.Null);
    }

    [Test]
    public void RejectsMissingInputsInvalidGeometryDimensionsAndPolarity()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrImage image = Render(symbol, false);
        IQrPerspectiveSampler sampler = new QrPerspectiveSampler();
        Assert.Throws<QrInputException>(() => sampler.SampleDetection(null!, Detection(symbol, false), BinaryOptions()));
        Assert.Throws<QrInputException>(() => sampler.SampleDetection(image, null!, BinaryOptions()));
        Assert.Throws<QrInputException>(() => sampler.SampleDetection(image, Detection(symbol, false), null!));
        Action<QrDetection>[] corruptions =
        [
            d => d.Corners = null, d => d.Corners!.TopLeft = null,
            d => d.Corners!.TopLeft!.X = double.NaN,
            d => d.Corners!.TopRight = d.Corners.TopLeft,
            d => d.EstimatedWidth = 0, d => d.EstimatedHeight = -1,
            d => d.Polarity = QrPolarity.Automatic, d => d.Polarity = (QrPolarity)99,
            d => d.EstimatedSymbolKind = (QrSymbolKind)99, d => d.Diagnostics = null!
        ];
        foreach (Action<QrDetection> corrupt in corruptions)
        {
            QrDetection detection = Detection(symbol, false);
            corrupt(detection);
            Assert.Throws<QrInputException>(() => sampler.SampleDetection(image, detection, BinaryOptions()));
        }
        QrDetectionOptions conflict = BinaryOptions();
        conflict.Polarity = QrPolarity.LightOnDark;
        Assert.Throws<QrInputException>(() => sampler.SampleDetection(image, Detection(symbol, false), conflict));
        conflict.Polarity = (QrPolarity)99;
        Assert.Throws<QrInputException>(() => sampler.SampleDetection(image, Detection(symbol, false), conflict));
        conflict.Polarity = QrPolarity.Automatic;
        conflict.BinarizationMode = (QrBinarizationMode)99;
        Assert.Throws<QrInputException>(() => sampler.SampleDetection(image, Detection(symbol, false), conflict));
    }

    [Test]
    public void RejectsInvalidEvidenceAndResourceLimits()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrImage image = Render(symbol, false);
        QrDetection detection = Detection(symbol, false);
        IQrPerspectiveSampler sampler = new QrPerspectiveSampler();
        Assert.Throws<QrInputException>(() => sampler.ResampleDetection(image, detection, null!, BinaryOptions()));
        Action<QrMatrix>[] corruptions =
        [
            m => m.Width++, m => m.QuietZoneLeft = 1, m => m.Values = [], m => m.Roles = null,
            m => m.Values[0] = (QrModuleValue)99, m => m.Roles![0] = (QrModuleRole)99
        ];
        foreach (Action<QrMatrix> corrupt in corruptions)
        {
            QrMatrix evidence = CreateSymbol(QrSymbolKind.Micro).Matrix!;
            corrupt(evidence);
            Assert.Throws<QrInputException>(() => sampler.ResampleDetection(image, detection, evidence, BinaryOptions()));
        }
        QrDetectionOptions limited = BinaryOptions();
        limited.MaximumImagePixels = 10;
        Assert.Throws<QrResourceLimitException>(() => sampler.SampleDetection(image, detection, limited));
        limited.MaximumImagePixels = 1000;
        Assert.Throws<QrResourceLimitException>(() => sampler.SampleDetection(image, detection, limited));
        detection.EstimatedWidth = int.MaxValue;
        Assert.Throws<QrResourceLimitException>(() => sampler.SampleDetection(image, detection, BinaryOptions()));
        image.Pixels[0] = 127;
        Assert.Throws<QrInputException>(() => sampler.SampleDetection(image, Detection(symbol, false), BinaryOptions()));
    }

    static QrDetectionOptions BinaryOptions() => new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary };

    static QrSymbol CreateSymbol(QrSymbolKind kind) => new QrEncoder().EncodeText("12345",
        new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = kind, QuietZoneModules = 0 });

    static QrImage Render(QrSymbol symbol, bool inverted)
    {
        QrRaster raster = new QrRenderer().RenderSymbol(symbol,
            new QrRenderOptions { Scale = 6, IncludeQuietZone = true, QuietZoneModules = 4, Invert = inverted });
        return new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels };
    }

    static QrDetection Detection(QrSymbol symbol, bool inverted) => new QrDetection
    {
        EstimatedWidth = symbol.Matrix!.Width, EstimatedHeight = symbol.Matrix.Height,
        HasSymbolKindEstimate = true, EstimatedSymbolKind = symbol.SymbolKind, EstimatedVersion = symbol.Version,
        Polarity = inverted ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight,
        ModuleSize = 6, Confidence = 0.8, CandidateIndex = 7,
        Corners = ProjectCorners(new PerspectiveTransform { M00 = 6, M11 = 6, M02 = 24, M12 = 24, M22 = 1 },
            symbol.Matrix.Width, symbol.Matrix.Height)
    };

    static QrPoint[] Points(QrQuadrilateral corners) => [corners.TopLeft!, corners.TopRight!, corners.BottomRight!, corners.BottomLeft!];

    static QrQuadrilateral ProjectCorners(PerspectiveTransform transform, int width, int height) => new QrQuadrilateral
    {
        TopLeft = transform.MapPoint(new QrPoint()), TopRight = transform.MapPoint(new QrPoint { X = width }),
        BottomRight = transform.MapPoint(new QrPoint { X = width, Y = height }), BottomLeft = transform.MapPoint(new QrPoint { Y = height })
    };

    static QrImage Warp(QrMatrix matrix, PerspectiveTransform transform, int width, int height)
    {
        QrImage image = new QrImage { Width = width, Height = height, Stride = width, Pixels = new byte[width * height] };
        Array.Fill(image.Pixels, (byte)255);
        PerspectiveTransform inverse = transform.Invert();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                QrPoint module = inverse.MapPoint(new QrPoint { X = x + 0.5, Y = y + 0.5 });
                if (module.X >= 0 && module.Y >= 0 && module.X < matrix.Width && module.Y < matrix.Height)
                    image.Pixels[y * width + x] = matrix.Values[(int)module.Y * matrix.Width + (int)module.X] == QrModuleValue.Dark ? (byte)0 : (byte)255;
            }
        return image;
    }
}
