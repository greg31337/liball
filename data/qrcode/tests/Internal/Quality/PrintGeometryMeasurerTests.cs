using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PrintGeometryMeasurerTests
{
    [TestCase(QrSymbolKind.Model2, 21, 21, 4)]
    [TestCase(QrSymbolKind.Micro, 11, 11, 2)]
    [TestCase(QrSymbolKind.rMQR, 43, 7, 2)]
    [TestCase(QrSymbolKind.LegacyModel1, 21, 21, 4)]
    public void UsesProfileNominalDimensionsAndQuietZoneRequirements(QrSymbolKind kind, int width, int height, int quiet)
    {
        QrMatrix reference = Reference(width, height);
        QrRaster raster = Render(reference, quiet, 5, 0, 0);
        PrintGeometryMeasurement result = Measure(raster, Detection(kind, width, height, quiet, 5), reference);
        Assert.That(result.HorizontalPrintGrowth.Value, Is.Zero.Within(0.001));
        Assert.That(result.VerticalPrintGrowth.Value, Is.Zero.Within(0.001));
        Assert.That(result.HorizontalRunCount, Is.EqualTo(4));
        Assert.That(result.VerticalRunCount, Is.EqualTo(4));
        Assert.That(result.RejectedHorizontalRunCount, Is.Zero);
        Assert.That(result.RejectedVerticalRunCount, Is.Zero);
        Assert.That(result.QuietZone.RequiredModules, Is.EqualTo(quiet));
        Assert.That(result.QuietZone.IsConformant, Is.True);
        Assert.That(result.QuietZone.ObservedPixelCount, Is.EqualTo(((width + 2 * quiet) * (height + 2 * quiet) - width * height) * 25));
        AssertMetrics(result, true);
    }

    [TestCase(1, 0, 0.2, 0.0)]
    [TestCase(0, 1, 0.0, 0.2)]
    [TestCase(1, 2, 0.2, 0.4)]
    [TestCase(-1, 0, -0.2, 0.0)]
    [TestCase(0, -1, 0.0, -0.2)]
    [TestCase(-1, -2, -0.2, -0.4)]
    public void MeasuresSignedGrowthWithoutDividingMergedRunsByTheirLength(int growthX, int growthY, double expectedX, double expectedY)
    {
        QrMatrix reference = Reference(21, 21);
        PrintGeometryMeasurement result = Measure(Render(reference, 4, 10, growthX, growthY),
            Detection(QrSymbolKind.Model2, 21, 21, 4, 10), reference);
        Assert.That(result.HorizontalPrintGrowth.Value, Is.EqualTo(expectedX).Within(0.001));
        Assert.That(result.VerticalPrintGrowth.Value, Is.EqualTo(expectedY).Within(0.001));
        Assert.That(result.HorizontalRunCount, Is.EqualTo(4));
        Assert.That(result.VerticalRunCount, Is.EqualTo(4));
        AssertMetrics(result, true);
    }

    [Test]
    public void MeasurementsFollowModuleAxesAfterImageRotation()
    {
        QrMatrix reference = Reference(43, 7);
        QrRaster source = Render(reference, 2, 10, 1, -1);
        QrRaster rotated = new QrRaster { Width = source.Height, Height = source.Width, Stride = source.Height, Pixels = new byte[source.Pixels.Length] };
        for (int y = 0; y < source.Height; y++)
            for (int x = 0; x < source.Width; x++) rotated.Pixels[x * rotated.Stride + source.Height - 1 - y] = source.Pixels[y * source.Stride + x];
        QrDetection detection = Detection(QrSymbolKind.rMQR, 43, 7, 2, 10);
        foreach (QrPoint point in Corners(detection))
        {
            double x = point.X;
            point.X = source.Height - point.Y;
            point.Y = x;
        }
        PrintGeometryMeasurement result = Measure(rotated, detection, reference);
        Assert.That(result.HorizontalPrintGrowth.Value, Is.EqualTo(0.2).Within(0.001));
        Assert.That(result.VerticalPrintGrowth.Value, Is.EqualTo(-0.2).Within(0.001));
        Assert.That(result.QuietZone.IsConformant, Is.True);
    }

    [TestCase(0)]
    [TestCase(3)]
    public void CroppedQuietZoneRemainsNonconformantEvenWhenGrowthIsMeasurable(int quiet)
    {
        QrMatrix reference = Reference(21, 21);
        PrintGeometryMeasurement result = Measure(Render(reference, quiet, 5, 0, 0), Detection(QrSymbolKind.Model2, 21, 21, quiet, 5), reference);
        Assert.That(result.HorizontalPrintGrowth.IsCalculated, Is.True);
        Assert.That(result.QuietZone.RequiredModules, Is.EqualTo(4));
        Assert.That(result.QuietZone.IsCropped, Is.True);
        Assert.That(result.QuietZone.IsConformant, Is.False);
        Assert.That(result.QuietZone.AcceptForDetection, Is.True);
    }

    [Test]
    public void QuietZoneContaminationIsMeasuredWhenPrintGrowthIsDisabled()
    {
        QrMatrix reference = Reference(21, 21);
        QrRaster raster = Render(reference, 4, 5, 0, 0);
        raster.Pixels[0] = 0;
        QrQualityOptions options = new QrQualityOptions { AnalyzePrintGrowth = false };
        PrintGeometryMeasurement result = PrintGeometryMeasurer.Measure(raster, Detection(QrSymbolKind.Model2, 21, 21, 4, 5),
            reference, new QrDetectionOptions(), options);
        AssertMetrics(result, false);
        Assert.That(result.SampleCount, Is.Zero);
        Assert.That(result.QuietZone.ContaminatedPixelCount, Is.EqualTo(1));
        Assert.That(result.QuietZone.IsFullyObserved, Is.True);
        Assert.That(result.QuietZone.IsConformant, Is.False);
        Assert.That(options.AnalyzePrintGrowth, Is.False);
    }

    [Test]
    public void UnreadableOrMissingRunsDoNotMasqueradeAsMeasuredZeroGrowth()
    {
        QrMatrix reference = Reference(21, 21);
        QrRaster raster = Render(reference, 4, 5, 0, 0);
        Array.Fill(raster.Pixels, (byte)255);
        PrintGeometryMeasurement result = Measure(raster, Detection(QrSymbolKind.Model2, 21, 21, 4, 5), reference);
        AssertMetrics(result, false);
        Assert.That(result.HorizontalRunCount, Is.Zero);
        Assert.That(result.RejectedHorizontalRunCount, Is.EqualTo(4));
        Assert.That(result.RejectedVerticalRunCount, Is.EqualTo(4));
        Assert.That(result.HorizontalPrintGrowth.Limitation, Does.Contain("No fully observed"));
        Array.Fill(reference.Values, QrModuleValue.Light);
        PrintGeometryMeasurement noRuns = Measure(raster, Detection(QrSymbolKind.Model2, 21, 21, 4, 5), reference);
        AssertMetrics(noRuns, false);
        Assert.That(noRuns.RejectedHorizontalRunCount, Is.Zero);
    }

    [Test]
    public void SkipsOffImageRunsWithoutChangingInputBuffersOrGeometry()
    {
        QrMatrix reference = Reference(21, 21);
        QrRaster raster = Render(reference, 4, 5, 0, 0);
        QrDetection detection = Detection(QrSymbolKind.Model2, 21, 21, 4, 5);
        foreach (QrPoint corner in Corners(detection)) corner.X += raster.Width;
        byte[] before = (byte[])raster.Pixels.Clone();
        QrModuleValue[] values = (QrModuleValue[])reference.Values.Clone();
        double x = detection.Corners!.TopLeft!.X;
        PrintGeometryMeasurement result = Measure(raster, detection, reference);
        AssertMetrics(result, false);
        Assert.That(result.RejectedHorizontalRunCount, Is.EqualTo(4));
        Assert.That(result.QuietZone.IsCropped, Is.True);
        Assert.That(raster.Pixels, Is.EqualTo(before));
        Assert.That(reference.Values, Is.EqualTo(values));
        Assert.That(detection.Corners.TopLeft.X, Is.EqualTo(x));
        Assert.That(detection.QuietZone, Is.Null);
    }

    [Test]
    public void UsesNormalizedBinaryEvidenceRegardlessOfPolarityMetadataAndPadding()
    {
        QrMatrix reference = Reference(21, 21);
        QrRaster source = Render(reference, 4, 5, 1, 0);
        int stride = source.Width + 3;
        QrRaster padded = new QrRaster { Width = source.Width, Height = source.Height, Stride = stride,
            Pixels = Enumerable.Repeat((byte)17, (source.Height - 1) * stride + source.Width).ToArray() };
        for (int row = 0; row < source.Height; row++) Array.Copy(source.Pixels, row * source.Stride, padded.Pixels, row * stride, source.Width);
        QrDetection detection = Detection(QrSymbolKind.Model2, 21, 21, 4, 5);
        detection.Polarity = QrPolarity.LightOnDark;
        PrintGeometryMeasurement result = Measure(padded, detection, reference);
        Assert.That(result.HorizontalPrintGrowth.Value, Is.EqualTo(0.4).Within(0.001));
        Assert.That(result.QuietZone.IsConformant, Is.True);
    }

    [Test]
    public void ValidatesProfileGeometryReferenceSamplesAndSamplingBudget()
    {
        QrMatrix reference = Reference(21, 21);
        QrRaster raster = Render(reference, 4, 5, 0, 0);
        QrDetection detection = Detection(QrSymbolKind.Model2, 21, 21, 4, 5);
        Assert.Throws<QrInputException>(() => Measure(null!, detection, reference));
        Assert.Throws<QrInputException>(() => Measure(raster, null!, reference));
        Assert.Throws<QrInputException>(() => Measure(raster, detection, null!));
        Assert.Throws<QrInputException>(() => PrintGeometryMeasurer.Measure(raster, detection, reference, new QrDetectionOptions(), null!));
        Assert.Throws<QrInputException>(() => PrintGeometryMeasurer.Measure(raster, detection, reference, new QrDetectionOptions(),
            new QrQualityOptions { AllowModel2 = false }));
        Assert.Throws<QrInputException>(() => PrintGeometryMeasurer.Measure(raster, detection, reference, new QrDetectionOptions(),
            new QrQualityOptions { AutomaticSymbolKind = false, SymbolKind = QrSymbolKind.Micro }));
        QrMatrix smallReference = Reference(11, 11);
        QrRaster smallRaster = Render(smallReference, 2, 5, 0, 0);
        QrDetection smallDetection = Detection(QrSymbolKind.Micro, 11, 11, 2, 5);
        PrintGeometryMeasurement measured = Measure(smallRaster, smallDetection, smallReference);
        Assert.Throws<QrResourceLimitException>(() => PrintGeometryMeasurer.Measure(smallRaster, smallDetection, smallReference, new QrDetectionOptions(),
            new QrQualityOptions { MaximumModuleSamples = measured.SampleCount - 1 }));
        Assert.That(PrintGeometryMeasurer.Measure(smallRaster, smallDetection, smallReference, new QrDetectionOptions(),
            new QrQualityOptions { MaximumModuleSamples = measured.SampleCount }).SampleCount, Is.EqualTo(measured.SampleCount));
        detection.EstimatedSymbolKind = QrSymbolKind.Micro;
        Assert.Throws<QrInputException>(() => Measure(raster, detection, reference));
        detection.EstimatedSymbolKind = QrSymbolKind.Model2;
        reference.Values[0] = QrModuleValue.Unknown;
        Assert.Throws<QrInputException>(() => Measure(raster, detection, reference));
        reference.Values[0] = QrModuleValue.Light;
        raster.Pixels[0] = 128;
        Assert.Throws<QrInputException>(() => Measure(raster, detection, reference));
    }

    [TestCase(QrSymbolKind.Model2)]
    [TestCase(QrSymbolKind.Micro)]
    [TestCase(QrSymbolKind.rMQR)]
    [TestCase(QrSymbolKind.LegacyModel1)]
    public void MeasuresNominalRenderedQrSymbols(QrSymbolKind kind)
    {
        QrSymbol symbol;
        if (kind == QrSymbolKind.LegacyModel1)
        {
            QrMatrix matrix = CodewordErasureRecoveryTests.Build(CodewordErasureRecoveryTests.Words(kind, 1, 0), kind, 1, 0);
            symbol = new QrSymbol { SymbolKind = kind, Width = matrix.Width, Height = matrix.Height, Matrix = matrix };
        }
        else symbol = ImageScanPipelineTests.Encode("12345", kind);
        int quiet = kind is QrSymbolKind.Model2 or QrSymbolKind.LegacyModel1 ? 4 : 2;
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = 6, QuietZoneModules = quiet });
        PrintGeometryMeasurement result = Measure(raster, Detection(kind, symbol.Width, symbol.Height, quiet, 6), symbol.Matrix!);
        AssertMetrics(result, true);
        Assert.That(result.HorizontalPrintGrowth.Value, Is.Zero.Within(0.001));
        Assert.That(result.VerticalPrintGrowth.Value, Is.Zero.Within(0.001));
        Assert.That(result.QuietZone.IsConformant, Is.True);
        Assert.That(result.HorizontalRunCount, Is.GreaterThan(0));
        Assert.That(result.VerticalRunCount, Is.GreaterThan(0));
    }

    static PrintGeometryMeasurement Measure(QrRaster raster, QrDetection detection, QrMatrix reference) =>
        PrintGeometryMeasurer.Measure(raster, detection, reference, new QrDetectionOptions(), new QrQualityOptions { AllowLegacyModel1 = true });

    static QrMatrix Reference(int width, int height)
    {
        QrMatrix matrix = new QrMatrix { Width = width, Height = height, Values = Enumerable.Repeat(QrModuleValue.Light, width * height).ToArray() };
        for (int row = 2; row < 5; row++)
            for (int column = 2; column < 5; column++) matrix.Values[row * width + column] = QrModuleValue.Dark;
        matrix.Values[2 * width + 8] = QrModuleValue.Dark;
        return matrix;
    }

    static QrRaster Render(QrMatrix matrix, int quiet, int scale, int growthX, int growthY)
    {
        int width = (matrix.Width + quiet * 2) * scale;
        int height = (matrix.Height + quiet * 2) * scale;
        QrRaster raster = new QrRaster { Width = width, Height = height, Stride = width, Pixels = Enumerable.Repeat((byte)255, width * height).ToArray() };
        Fill(raster, (quiet + 2) * scale - growthX, (quiet + 2) * scale - growthY, (quiet + 5) * scale + growthX, (quiet + 5) * scale + growthY);
        Fill(raster, (quiet + 8) * scale - growthX, (quiet + 2) * scale - growthY, (quiet + 9) * scale + growthX, (quiet + 3) * scale + growthY);
        return raster;
    }

    static void Fill(QrRaster raster, int left, int top, int right, int bottom)
    {
        for (int row = top; row < bottom; row++)
            for (int column = left; column < right; column++) raster.Pixels[row * raster.Stride + column] = 0;
    }

    static QrDetection Detection(QrSymbolKind kind, int width, int height, int quiet, int scale) => new QrDetection
    {
        HasSymbolKindEstimate = true, EstimatedSymbolKind = kind, EstimatedWidth = width, EstimatedHeight = height, ModuleSize = scale,
        Corners = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = quiet * scale, Y = quiet * scale },
            TopRight = new QrPoint { X = (quiet + width) * scale, Y = quiet * scale },
            BottomRight = new QrPoint { X = (quiet + width) * scale, Y = (quiet + height) * scale },
            BottomLeft = new QrPoint { X = quiet * scale, Y = (quiet + height) * scale }
        }
    };

    static QrPoint[] Corners(QrDetection detection) => [detection.Corners!.TopLeft!, detection.Corners.TopRight!, detection.Corners.BottomRight!, detection.Corners.BottomLeft!];

    static void AssertMetrics(PrintGeometryMeasurement result, bool calculated)
    {
        foreach (QrQualityMetric metric in new[] { result.HorizontalPrintGrowth, result.VerticalPrintGrowth })
        {
            Assert.That(metric.IsCalculated, Is.EqualTo(calculated));
            Assert.That(metric.Grade, Is.EqualTo(QrQualityGrade.NotGraded));
            Assert.That(metric.UsesCalibratedInputs, Is.False);
            Assert.That(metric.Limitation, Is.Not.Null.And.Not.Empty);
            Assert.That(double.IsFinite(metric.Value), Is.True);
        }
    }
}
