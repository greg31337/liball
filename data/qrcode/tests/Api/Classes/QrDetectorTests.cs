using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrDetectorTests
{
    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void DetectsEncodedProfilesAndBothReflectancesThroughBothEntryPoints(QrSymbolKind kind, bool invert)
    {
        QrSymbol symbol = Encode(kind, 1);
        QrRaster raster = Render(symbol, 4, 5, invert);
        IQrDetector detector = new QrDetector();
        QrDetectionSet imageResult = detector.DetectImage(Image(raster), new QrDetectionOptions());
        QrDetectionSet binaryResult = detector.DetectBinaryMatrix(Binary(raster), new QrDetectionOptions());
        foreach (QrDetectionSet result in new[] { imageResult, binaryResult })
        {
            Assert.That(result.Detections, Has.Length.EqualTo(1), string.Join(";", result.Detections.Select(d => d.EstimatedSymbolKind)));
            QrDetection detection = result.Detections[0];
            Assert.That(detection.HasSymbolKindEstimate, Is.True);
            Assert.That(detection.EstimatedSymbolKind, Is.EqualTo(kind));
            Assert.That(detection.EstimatedWidth, Is.EqualTo(symbol.Width));
            Assert.That(detection.EstimatedHeight, Is.EqualTo(symbol.Height));
            Assert.That(detection.Polarity, Is.EqualTo(invert ? QrPolarity.LightOnDark : QrPolarity.DarkOnLight));
            Assert.That(detection.Corners, Is.Not.Null);
            Assert.That(detection.Corners!.TopLeft!.X, Is.EqualTo(20).Within(2));
            Assert.That(detection.Corners.TopLeft.Y, Is.EqualTo(20).Within(2));
            Assert.That(detection.ModuleSize, Is.EqualTo(4).Within(0.5));
            Assert.That(detection.Confidence, Is.InRange(0.0, 1.0));
            Assert.That(detection.SampledMatrix, Is.Null);
            Assert.That(detection.QuietZone, Is.Not.Null);
            Assert.That(detection.QuietZone!.IsConformant, Is.True);
            Assert.That(result.BinaryRaster, Is.Not.Null);
            Assert.That(result.Polarity, Is.EqualTo(detection.Polarity));
            Assert.That(result.CandidateCount, Is.GreaterThanOrEqualTo(1));
        }
        Assert.That(imageResult.Thresholds, Is.Not.Empty);
        Assert.That(binaryResult.Thresholds, Is.Empty);
        Assert.That(binaryResult.BinarizationMode, Is.EqualTo(QrBinarizationMode.SuppliedBinary));
    }

    [TestCase(QrPixelFormat.Gray8)]
    [TestCase(QrPixelFormat.Rgb24)]
    [TestCase(QrPixelFormat.Rgba32)]
    [TestCase(QrPixelFormat.Bgra32)]
    public void SupportsRawFormatsAndPaddedStridesWithoutChangingCallerMemory(QrPixelFormat format)
    {
        QrRaster raster = Render(Encode(QrSymbolKind.Model2, 1), 3, 5, false);
        int channels = format == QrPixelFormat.Gray8 ? 1 : format == QrPixelFormat.Rgb24 ? 3 : 4;
        int stride = raster.Width * channels + 7;
        byte[] pixels = Enumerable.Repeat((byte)17, (raster.Height - 1) * stride + raster.Width * channels).ToArray();
        for (int row = 0; row < raster.Height; row++)
            for (int column = 0; column < raster.Width; column++)
                for (int channel = 0; channel < channels; channel++)
                    pixels[row * stride + column * channels + channel] = channel == 3 ? (byte)255 : raster.Pixels[row * raster.Stride + column];
        byte[] original = (byte[])pixels.Clone();
        QrImage image = new QrImage { Pixels = pixels, Width = raster.Width, Height = raster.Height, Stride = stride, PixelFormat = format };
        QrDetectionSet result = new QrDetector().DetectImage(image, new QrDetectionOptions());
        Assert.That(result.Detections, Has.Length.EqualTo(1));
        Assert.That(pixels, Is.EqualTo(original));
        Assert.That(result.BinaryRaster!.Pixels, Is.Not.SameAs(pixels));
    }

    [TestCase(QrBinarizationMode.GlobalHistogram)]
    [TestCase(QrBinarizationMode.HybridLocal)]
    [TestCase(QrBinarizationMode.SuppliedBinary)]
    public void HonorsPreprocessingStrategies(QrBinarizationMode mode)
    {
        QrImage image = Image(Render(Encode(QrSymbolKind.Model2, 1), 4, 5, false));
        QrDetectionSet result = new QrDetector().DetectImage(image, new QrDetectionOptions { BinarizationMode = mode });
        Assert.That(result.Detections, Has.Length.EqualTo(1));
        Assert.That(result.BinarizationMode, Is.EqualTo(mode));
        if (mode == QrBinarizationMode.SuppliedBinary) Assert.That(result.Thresholds, Is.Empty);
        else Assert.That(result.Thresholds, Is.Not.Empty);
    }

    [TestCase(1)]
    [TestCase(4)]
    public void RetainsSourceImageCoordinatesAcrossPyramidLevels(int levels)
    {
        QrSymbol symbol = Encode(QrSymbolKind.Model2, 7);
        QrRaster raster = Render(symbol, 5, 5, false);
        QrDetectionSet result = new QrDetector().DetectBinaryMatrix(Binary(raster),
            new QrDetectionOptions { MaximumPyramidLevels = levels });
        Assert.That(result.Detections, Has.Length.EqualTo(1), string.Join(";", result.Detections.Select(d =>
            $"{d.EstimatedSymbolKind} {d.EstimatedWidth}x{d.EstimatedHeight} {d.Polarity} {d.Confidence} at {d.Corners!.TopLeft!.X},{d.Corners.TopLeft.Y}")));
        Assert.That(result.Detections[0].EstimatedVersion, Is.EqualTo(7));
        Assert.That(result.Detections[0].Corners!.TopLeft!.X, Is.EqualTo(25).Within(2));
        Assert.That(result.Detections[0].Corners!.BottomRight!.Y, Is.EqualTo(250).Within(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void QuietZoneToleranceDoesNotUpgradeConformance(bool tolerate)
    {
        QrRaster raster = Render(Encode(QrSymbolKind.Model2, 1), 4, 0, false);
        QrDetectionSet result = new QrDetector().DetectBinaryMatrix(Binary(raster),
            new QrDetectionOptions { AllowCroppedQuietZone = tolerate });
        Assert.That(result.Detections.Length, Is.EqualTo(tolerate ? 1 : 0));
        if (tolerate)
        {
            Assert.That(result.Detections[0].QuietZone!.IsConformant, Is.False);
            Assert.That(result.Detections[0].QuietZone!.IsCropped, Is.True);
        }
    }

    [Test]
    public void ReturnsEmptyForNoSymbolAndHonorsFamilyAndPolarityRestrictions()
    {
        IQrDetector detector = new QrDetector();
        QrMatrix white = new QrMatrix { Width = 50, Height = 50, Values = Enumerable.Repeat(QrModuleValue.Light, 2500).ToArray() };
        Assert.That(detector.DetectBinaryMatrix(white, new QrDetectionOptions()).Detections, Is.Empty);
        QrImage blank = new QrImage { Width = 20, Height = 20, Stride = 20, Pixels = Enumerable.Repeat((byte)255, 400).ToArray() };
        Assert.That(detector.DetectImage(blank, new QrDetectionOptions()).Detections, Is.Empty);
        QrMatrix symbol = Binary(Render(Encode(QrSymbolKind.Model2, 1), 4, 5, false));
        Assert.That(detector.DetectBinaryMatrix(symbol, new QrDetectionOptions
        {
            AllowModel2 = false, AllowLegacyModel1 = false, AllowMicro = false, AllowRectangularMicro = false
        }).Detections, Is.Empty);
        Assert.That(detector.DetectBinaryMatrix(symbol, new QrDetectionOptions { Polarity = QrPolarity.LightOnDark }).Detections, Is.Empty);
        QrDetectionSet legacy = detector.DetectBinaryMatrix(symbol, new QrDetectionOptions
        {
            AllowModel2 = false, AllowMicro = false, AllowRectangularMicro = false
        });
        Assert.That(legacy.Detections, Has.Length.EqualTo(1));
        Assert.That(legacy.Detections[0].EstimatedSymbolKind, Is.EqualTo(QrSymbolKind.LegacyModel1));
    }

    [Test]
    public void DetectsMultipleProfilesAndPolaritiesWithStableOrderingAndIndependentResults()
    {
        QrRaster first = Render(Encode(QrSymbolKind.Model2, 1), 3, 5, false);
        QrRaster second = Render(Encode(QrSymbolKind.Micro, 1), 3, 5, true);
        QrRaster third = Render(Encode(QrSymbolKind.rMQR, 1), 3, 5, false);
        QrRaster scene = new QrRaster { Width = 360, Height = 300, Stride = 360, Pixels = Enumerable.Repeat((byte)255, 360 * 300).ToArray() };
        Paste(scene, first, 10, 10);
        Paste(scene, second, 200, 40);
        Paste(scene, third, 10, 180);
        QrMatrix binary = Binary(scene);
        QrModuleValue[] original = (QrModuleValue[])binary.Values.Clone();
        IQrDetector detector = new QrDetector();
        QrDetectionSet result = detector.DetectBinaryMatrix(binary, new QrDetectionOptions());
        QrDetectionSet again = detector.DetectBinaryMatrix(binary, new QrDetectionOptions());
        Assert.That(result.Detections.Select(d => d.EstimatedSymbolKind),
            Is.EqualTo(new[] { QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR }),
            string.Join(";", result.Detections.Select(d => $"{d.EstimatedSymbolKind} {d.Polarity} {d.Confidence} at {d.Corners!.TopLeft!.X},{d.Corners.TopLeft.Y}")));
        Assert.That(result.Polarity, Is.EqualTo(QrPolarity.Automatic));
        Assert.That(result.BinaryRaster, Is.Null);
        Assert.That(result.Detections.Select(d => d.CandidateIndex), Is.Unique);
        Assert.That(again.Detections.Select(d => d.CandidateIndex), Is.EqualTo(result.Detections.Select(d => d.CandidateIndex)));
        Assert.That(again.Detections[0], Is.Not.SameAs(result.Detections[0]));
        Assert.That(binary.Values, Is.EqualTo(original));
        QrDetectionSet single = detector.DetectBinaryMatrix(binary, new QrDetectionOptions { DetectMultiple = false });
        Assert.That(single.Detections, Has.Length.EqualTo(1));
        Assert.That(single.Detections[0].Confidence, Is.EqualTo(result.Detections.Max(d => d.Confidence)));
        Assert.That(single.BinaryRaster, Is.Not.Null);
    }

    [TestCase(QrSymbolKind.Model2, 1, false)]
    [TestCase(QrSymbolKind.Model2, 2, true)]
    [TestCase(QrSymbolKind.Micro, 0, true)]
    [TestCase(QrSymbolKind.Micro, 1, false)]
    [TestCase(QrSymbolKind.Micro, 3, true)]
    [TestCase(QrSymbolKind.rMQR, 0, true)]
    [TestCase(QrSymbolKind.rMQR, 1, false)]
    [TestCase(QrSymbolKind.rMQR, 3, true)]
    public void DetectsRotatedAndReflectedSymbolsWithoutSamplingPayloads(QrSymbolKind kind, int turns, bool mirror)
    {
        QrSymbol symbol = Encode(kind, 1);
        QrRaster raster = Render(symbol, 4, 5, false);
        if (mirror)
            for (int row = 0; row < raster.Height; row++) Array.Reverse(raster.Pixels, row * raster.Stride, raster.Width);
        for (int turn = 0; turn < turns; turn++)
        {
            byte[] rotated = new byte[raster.Pixels.Length];
            for (int row = 0; row < raster.Height; row++)
                for (int column = 0; column < raster.Width; column++)
                    rotated[column * raster.Height + raster.Height - row - 1] = raster.Pixels[row * raster.Stride + column];
            raster = new QrRaster { Width = raster.Height, Height = raster.Width, Stride = raster.Height, Pixels = rotated };
        }
        QrDetectionSet result = new QrDetector().DetectBinaryMatrix(Binary(raster), new QrDetectionOptions());
        Assert.That(result.Detections, Has.Length.EqualTo(1));
        QrDetection detection = result.Detections[0];
        Assert.That(detection.EstimatedSymbolKind, Is.EqualTo(kind));
        Assert.That(detection.EstimatedWidth, Is.EqualTo(symbol.Width));
        Assert.That(detection.EstimatedHeight, Is.EqualTo(symbol.Height));
        Assert.That(detection.SampledMatrix, Is.Null);
        if (kind == QrSymbolKind.Model2) Assert.That(detection.HasMirrorEstimate, Is.False);
        else
        {
            Assert.That(detection.HasMirrorEstimate, Is.True);
            Assert.That(detection.IsMirrored, Is.EqualTo(mirror));
        }
    }

    [Test]
    public void HonorsExplicitPolarityMinimumModuleSizeAndCandidateLimits()
    {
        QrMatrix binary = Binary(Render(Encode(QrSymbolKind.Model2, 1), 4, 5, true));
        IQrDetector detector = new QrDetector();
        QrDetectionSet result = detector.DetectBinaryMatrix(binary, new QrDetectionOptions { Polarity = QrPolarity.LightOnDark });
        Assert.That(result.Detections, Has.Length.EqualTo(1));
        Assert.That(result.Polarity, Is.EqualTo(QrPolarity.LightOnDark));
        Assert.That(detector.DetectBinaryMatrix(binary, new QrDetectionOptions { MinimumModuleSize = 20 }).Detections, Is.Empty);
        Assert.That(detector.DetectBinaryMatrix(binary, new QrDetectionOptions { MaximumCandidates = 1 }).Detections.Length,
            Is.LessThanOrEqualTo(1));
    }

    [Test]
    public void EnforcesRawImageLayoutAndConfiguredBounds()
    {
        IQrDetector detector = new QrDetector();
        QrImage image = Image(Render(Encode(QrSymbolKind.Model2, 1), 3, 5, false));
        Assert.Throws<QrResourceLimitException>(() => detector.DetectImage(image, new QrDetectionOptions { MaximumImageWidth = 10 }));
        Assert.Throws<QrResourceLimitException>(() => detector.DetectImage(image, new QrDetectionOptions { MaximumImageHeight = 10 }));
        Assert.Throws<QrResourceLimitException>(() => detector.DetectImage(image, new QrDetectionOptions { MaximumImagePixels = 10 }));
        image.Stride = 1;
        Assert.Throws<QrInputException>(() => detector.DetectImage(image, new QrDetectionOptions()));
        image.Stride = image.Width;
        image.Pixels = [];
        Assert.Throws<QrInputException>(() => detector.DetectImage(image, new QrDetectionOptions()));
        image.PixelFormat = QrPixelFormat.Rgb24;
        Assert.Throws<QrInputException>(() => detector.DetectImage(image,
            new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary }));
    }

    [Test]
    public void ValidatesInputsAndOptionsEvenWhenFamiliesAreDisabled()
    {
        IQrDetector detector = new QrDetector();
        QrMatrix valid = new QrMatrix { Width = 1, Height = 1, Values = [QrModuleValue.Light] };
        QrImage image = new QrImage { Width = 1, Height = 1, Stride = 1, Pixels = [255] };
        Assert.Throws<QrInputException>(() => detector.DetectImage(null!, new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => detector.DetectBinaryMatrix(null!, new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => detector.DetectBinaryMatrix(valid, null!));
        Assert.Throws<QrInputException>(() => detector.DetectImage(image, null!));
        foreach (Action<QrDetectionOptions> corrupt in new Action<QrDetectionOptions>[]
        {
            o => o.MaximumCandidates = 0, o => o.MaximumPyramidLevels = 0, o => o.MinimumModuleSize = 0,
            o => o.MaximumImageWidth = 0, o => o.MaximumImageHeight = 0, o => o.MaximumImagePixels = 0,
            o => o.Polarity = (QrPolarity)99, o => o.BinarizationMode = (QrBinarizationMode)99
        })
        {
            QrDetectionOptions options = new QrDetectionOptions
            {
                AllowModel2 = false, AllowMicro = false, AllowRectangularMicro = false, AllowLegacyModel1 = false
            };
            corrupt(options);
            Assert.Throws<QrInputException>(() => detector.DetectBinaryMatrix(valid, options));
            Assert.Throws<QrInputException>(() => detector.DetectImage(image, options));
        }
        foreach (QrMatrix invalid in new QrMatrix[]
        {
            new(), new() { Width = 1, Height = 1, Values = null! },
            new() { Width = 2, Height = 1, Values = [QrModuleValue.Light] },
            new() { Width = 1, Height = 1, Values = [QrModuleValue.Unknown] },
            new() { Width = 1, Height = 1, Values = [(QrModuleValue)99] }
        }) Assert.Throws<QrInputException>(() => detector.DetectBinaryMatrix(invalid, new QrDetectionOptions()));
        Assert.Throws<QrResourceLimitException>(() => detector.DetectBinaryMatrix(
            new QrMatrix { Width = int.MaxValue, Height = 2 }, new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => detector.DetectImage(
            new QrImage { Width = 1, Height = 1, Stride = 1, Pixels = [128] },
            new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary }));
    }

    static QrSymbol Encode(QrSymbolKind kind, int version)
    {
        QrEncodeOptions options = new QrEncodeOptions { ForceSymbolKind = true, SymbolKind = kind, QuietZoneModules = 0 };
        if (kind == QrSymbolKind.Model2) { options.MinimumVersion = version; options.MaximumVersion = version; }
        return new QrEncoder().EncodeText("12345", options);
    }

    static void Paste(QrRaster destination, QrRaster source, int x, int y)
    {
        for (int row = 0; row < source.Height; row++)
            Array.Copy(source.Pixels, row * source.Stride, destination.Pixels, (row + y) * destination.Stride + x, source.Width);
    }

    static QrRaster Render(QrSymbol symbol, int scale, int quiet, bool invert)
    {
        return new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = scale, QuietZoneModules = quiet, Invert = invert });
    }

    static QrImage Image(QrRaster raster)
    {
        return new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels, PixelFormat = QrPixelFormat.Gray8 };
    }

    static QrMatrix Binary(QrRaster raster)
    {
        return new QrMatrix
        {
            Width = raster.Width, Height = raster.Height,
            Values = raster.Pixels.Select(p => p == 0 ? QrModuleValue.Dark : QrModuleValue.Light).ToArray()
        };
    }
}
