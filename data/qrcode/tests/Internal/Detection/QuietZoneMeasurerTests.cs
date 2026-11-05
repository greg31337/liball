using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QuietZoneMeasurerTests
{
    [TestCase(QrSymbolKind.Model2, 21, 21, 4)]
    [TestCase(QrSymbolKind.LegacyModel1, 21, 21, 4)]
    [TestCase(QrSymbolKind.Micro, 17, 17, 2)]
    [TestCase(QrSymbolKind.rMQR, 43, 7, 2)]
    public void MeasuresTheCompleteRequiredMarginForEveryProfile(QrSymbolKind kind, int width, int height, int required)
    {
        int scale = 3;
        QrRaster raster = Raster(width, height, required, scale);
        QrDetection detection = Detection(kind, width, height, required, scale);
        QrQuietZoneMeasurement result = QuietZoneMeasurer.Measure(raster, detection,
            new QrDetectionOptions { AllowCroppedQuietZone = false });
        detection.QuietZone = result;

        Assert.That(detection.QuietZone, Is.SameAs(result));
        Assert.That(result.RequiredModules, Is.EqualTo(required));
        Assert.That(result.IsFullyObserved, Is.True);
        Assert.That(result.IsConformant, Is.True);
        Assert.That(result.AcceptForDetection, Is.True);
        Assert.That(result.IsCropped, Is.False);
        Assert.That(result.HasUnmappableBoundary, Is.False);
        Assert.That(result.ContaminatedPixelCount, Is.Zero);
        Assert.That(result.ObservedPixelCount, Is.EqualTo(
            ((width + 2 * required) * (height + 2 * required) - width * height) * scale * scale));
        Assert.That(result.LeftPixelCount, Is.EqualTo(required * (height + 2 * required) * scale * scale));
        Assert.That(result.RightPixelCount, Is.EqualTo(result.LeftPixelCount));
        Assert.That(result.TopPixelCount, Is.EqualTo(required * (width + 2 * required) * scale * scale));
        Assert.That(result.BottomPixelCount, Is.EqualTo(result.TopPixelCount));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(3)]
    public void CroppedOrAbsentMarginsRemainNonconformantUnderTolerantDetection(int available)
    {
        QrRaster raster = Raster(21, 21, available, 3);
        QrDetection detection = Detection(QrSymbolKind.Model2, 21, 21, available, 3);
        QrQuietZoneMeasurement tolerant = QuietZoneMeasurer.Measure(raster, detection, new QrDetectionOptions());
        QrQuietZoneMeasurement strict = QuietZoneMeasurer.Measure(raster, detection,
            new QrDetectionOptions { AllowCroppedQuietZone = false });

        Assert.That(tolerant.IsCropped, Is.True);
        Assert.That(tolerant.IsFullyObserved, Is.False);
        Assert.That(tolerant.IsConformant, Is.False);
        Assert.That(tolerant.AcceptForDetection, Is.True);
        Assert.That(strict.AcceptForDetection, Is.False);
        Assert.That(strict.IsConformant, Is.False);
        Assert.That(strict.ObservedPixelCount, Is.EqualTo(tolerant.ObservedPixelCount));
        Assert.That(strict.ContaminatedPixelCount, Is.Zero);
        Assert.That(tolerant.ObservedPixelCount, Is.EqualTo(((21 + 2 * available) * (21 + 2 * available) - 21 * 21) * 9));
    }

    [TestCase(0, 40)]
    [TestCase(40, 0)]
    [TestCase(86, 40)]
    [TestCase(40, 86)]
    [TestCase(0, 0)]
    [TestCase(86, 0)]
    [TestCase(0, 86)]
    [TestCase(86, 86)]
    public void FindsSinglePixelContaminationAnywhereInTheMarginIncludingCorners(int x, int y)
    {
        QrRaster raster = Raster(21, 21, 4, 3);
        raster.Pixels[y * raster.Stride + x] = 0;
        QrDetection detection = Detection(QrSymbolKind.Model2, 21, 21, 4, 3);
        QrQuietZoneMeasurement result = QuietZoneMeasurer.Measure(raster, detection,
            new QrDetectionOptions { AllowCroppedQuietZone = false });
        Assert.That(result.IsFullyObserved, Is.True);
        Assert.That(result.IsCropped, Is.False);
        Assert.That(result.IsConformant, Is.False);
        Assert.That(result.ContaminatedPixelCount, Is.EqualTo(1));
        Assert.That(result.AcceptForDetection, Is.False);
    }

    [Test]
    public void IgnoresTheDarkSymbolInteriorAndPixelsBeyondTheRequiredMargin()
    {
        QrRaster raster = Raster(21, 21, 5, 3);
        for (int x = 0; x < raster.Width; x++) raster.Pixels[x] = 0;
        QrQuietZoneMeasurement result = QuietZoneMeasurer.Measure(raster,
            Detection(QrSymbolKind.Model2, 21, 21, 5, 3), new QrDetectionOptions());
        Assert.That(result.IsConformant, Is.True);
        Assert.That(result.ContaminatedPixelCount, Is.Zero);
        Assert.That(result.ObservedPixelCount, Is.EqualTo((29 * 29 - 21 * 21) * 9));
    }

    [Test]
    public void DetectsFractionalPixelCroppingEvenWhenAllPixelCentersAreAvailable()
    {
        QrRaster raster = Raster(21, 21, 4, 3);
        QrDetection detection = Detection(QrSymbolKind.Model2, 21, 21, 4, 3);
        foreach (QrPoint point in Points(detection.Corners!)) point.X -= 0.125;
        QrQuietZoneMeasurement result = QuietZoneMeasurer.Measure(raster, detection, new QrDetectionOptions());
        Assert.That(result.IsCropped, Is.True);
        Assert.That(result.IsFullyObserved, Is.False);
        Assert.That(result.IsConformant, Is.False);
    }

    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(3, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(2, true)]
    [TestCase(3, true)]
    public void HandlesRotatedAndMirroredRectangularGeometry(int turns, bool mirror)
    {
        QrRaster raster = Raster(43, 7, 2, 2);
        QrDetection detection = Detection(QrSymbolKind.rMQR, 43, 7, 2, 2);
        QrPoint[] corners = Points(detection.Corners!);
        if (mirror)
        {
            foreach (QrPoint corner in corners) corner.X = raster.Width - corner.X;
            for (int row = 0; row < raster.Height; row++)
                Array.Reverse(raster.Pixels, row * raster.Stride, raster.Width);
        }
        for (int turn = 0; turn < turns; turn++)
        {
            foreach (QrPoint corner in corners) (corner.X, corner.Y) = (raster.Height - corner.Y, corner.X);
            byte[] rotated = new byte[raster.Pixels.Length];
            for (int row = 0; row < raster.Height; row++)
                for (int column = 0; column < raster.Width; column++)
                    rotated[column * raster.Height + raster.Height - 1 - row] = raster.Pixels[row * raster.Stride + column];
            raster = new QrRaster { Pixels = rotated, Width = raster.Height, Height = raster.Width, Stride = raster.Height };
        }
        QrQuietZoneMeasurement result = QuietZoneMeasurer.Measure(raster, detection, new QrDetectionOptions());
        Assert.That(result.IsConformant, Is.True);
        Assert.That(result.ContaminatedPixelCount, Is.Zero);
        Assert.That(result.ObservedPixelCount, Is.EqualTo((47 * 11 - 43 * 7) * 4));
    }

    [Test]
    public void MeasuresPerspectiveMarginsInModuleSpace()
    {
        PerspectiveTransform actual = new PerspectiveTransform
        {
            M00 = 4.03, M01 = 0.515, M02 = 30,
            M10 = 0.28, M11 = 4.015, M12 = 30,
            M20 = 0.001, M21 = 0.0005, M22 = 1
        };
        QrDetection detection = Detection(QrSymbolKind.Model2, 21, 21, 0, 1);
        foreach (QrPoint corner in Points(detection.Corners!))
        {
            QrPoint mapped = actual.MapPoint(corner);
            corner.X = mapped.X;
            corner.Y = mapped.Y;
        }
        QrRaster raster = new QrRaster { Width = 160, Height = 160, Stride = 160, Pixels = new byte[160 * 160] };
        Array.Fill(raster.Pixels, (byte)255);
        PerspectiveTransform inverse = actual.Invert();
        for (int row = 0; row < raster.Height; row++)
            for (int column = 0; column < raster.Width; column++)
            {
                QrPoint module = inverse.MapPoint(new QrPoint { X = column + 0.5, Y = row + 0.5 });
                if (module.X >= 0 && module.Y >= 0 && module.X < 21 && module.Y < 21)
                    raster.Pixels[row * raster.Stride + column] = 0;
            }
        QrQuietZoneMeasurement result = QuietZoneMeasurer.Measure(raster, detection, new QrDetectionOptions());
        Assert.That(result.IsFullyObserved, Is.True);
        Assert.That(result.IsConformant, Is.True);
        Assert.That(result.ObservedPixelCount, Is.GreaterThan(0));
        Assert.That(result.ContaminatedPixelCount, Is.Zero);
    }

    [Test]
    public void ReportsAnUnmappableMarginWithoutClaimingConformanceOrRejectingTolerantDetection()
    {
        PerspectiveTransform actual = new PerspectiveTransform
        {
            M00 = 4, M02 = 10, M10 = 3, M11 = 1, M12 = 10, M20 = 0.3, M22 = 1
        };
        QrDetection detection = Detection(QrSymbolKind.Model2, 21, 21, 0, 1);
        foreach (QrPoint corner in Points(detection.Corners!))
        {
            QrPoint mapped = actual.MapPoint(corner);
            corner.X = mapped.X;
            corner.Y = mapped.Y;
        }
        QrQuietZoneMeasurement result = QuietZoneMeasurer.Measure(Raster(21, 21, 4, 1), detection, new QrDetectionOptions());
        Assert.That(result.HasUnmappableBoundary, Is.True);
        Assert.That(result.IsConformant, Is.False);
        Assert.That(result.IsFullyObserved, Is.False);
        Assert.That(result.ObservedPixelCount, Is.Zero);
        Assert.That(result.AcceptForDetection, Is.True);
    }

    [Test]
    public void NoObservedMarginPixelsCannotEstablishConformance()
    {
        QrDetection detection = Detection(QrSymbolKind.Model2, 1, 1, 0, 1);
        foreach (QrPoint point in Points(detection.Corners!))
        {
            point.X = 0.49 + point.X * 0.02;
            point.Y = 0.49 + point.Y * 0.02;
        }
        QrRaster raster = new QrRaster { Width = 1, Height = 1, Stride = 1, Pixels = [255] };
        QrQuietZoneMeasurement result = QuietZoneMeasurer.Measure(raster, detection, new QrDetectionOptions());
        Assert.That(result.IsCropped, Is.False);
        Assert.That(result.IsConformant, Is.False);
        Assert.That(result.IsFullyObserved, Is.False);
        Assert.That(result.ObservedPixelCount, Is.Zero);
    }

    [Test]
    public void HonorsStrideAndNormalizedPolarityWithoutMutatingInputs()
    {
        QrRaster packed = Raster(17, 17, 2, 1);
        QrRaster padded = new QrRaster
        {
            Width = packed.Width, Height = packed.Height, Stride = packed.Width + 3,
            Pixels = Enumerable.Repeat((byte)127, (packed.Height - 1) * (packed.Width + 3) + packed.Width).ToArray()
        };
        for (int row = 0; row < packed.Height; row++)
            Array.Copy(packed.Pixels, row * packed.Width, padded.Pixels, row * padded.Stride, packed.Width);
        byte[] original = (byte[])padded.Pixels.Clone();
        QrDetection detection = Detection(QrSymbolKind.Micro, 17, 17, 2, 1);
        detection.Polarity = QrPolarity.LightOnDark;
        detection.SampledMatrix = new QrMatrix { Values = [QrModuleValue.Unknown] };
        QrMatrix sampled = detection.SampledMatrix;
        QrQuadrilateral corners = detection.Corners!;
        QrQuietZoneMeasurement first = QuietZoneMeasurer.Measure(padded, detection, new QrDetectionOptions());
        QrQuietZoneMeasurement second = QuietZoneMeasurer.Measure(padded, detection, new QrDetectionOptions());
        Assert.That(first.IsConformant, Is.True);
        Assert.That(second.ObservedPixelCount, Is.EqualTo(first.ObservedPixelCount));
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(padded.Pixels, Is.EqualTo(original));
        Assert.That(detection.Polarity, Is.EqualTo(QrPolarity.LightOnDark));
        Assert.That(detection.SampledMatrix, Is.SameAs(sampled));
        Assert.That(sampled.Values, Is.EqualTo(new[] { QrModuleValue.Unknown }));
        Assert.That(detection.Corners, Is.SameAs(corners));
        Assert.That(corners.TopLeft!.X, Is.EqualTo(2));
    }

    [Test]
    public void RejectsInvalidInputAndEnforcesResourceLimits()
    {
        QrRaster raster = Raster(21, 21, 4, 1);
        QrDetection valid = Detection(QrSymbolKind.Model2, 21, 21, 4, 1);
        Assert.Throws<QrInputException>(() => QuietZoneMeasurer.Measure(null!, valid, new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => QuietZoneMeasurer.Measure(raster, null!, new QrDetectionOptions()));
        Assert.Throws<QrInputException>(() => QuietZoneMeasurer.Measure(raster, valid, null!));
        Action<QrDetection>[] corruptions =
        [
            d => d.HasSymbolKindEstimate = false, d => d.EstimatedSymbolKind = (QrSymbolKind)99,
            d => d.EstimatedWidth = 0, d => d.EstimatedHeight = -1,
            d => d.Corners = null, d => d.Corners!.TopRight = null,
            d => d.Corners!.TopLeft!.X = double.NaN, d => d.Corners!.BottomRight = d.Corners.TopLeft
        ];
        foreach (Action<QrDetection> corrupt in corruptions)
        {
            QrDetection invalid = Detection(QrSymbolKind.Model2, 21, 21, 4, 1);
            corrupt(invalid);
            Assert.Throws<QrInputException>(() => QuietZoneMeasurer.Measure(raster, invalid, new QrDetectionOptions()));
        }
        Assert.Throws<QrResourceLimitException>(() => QuietZoneMeasurer.Measure(raster, valid,
            new QrDetectionOptions { MaximumImagePixels = 10 }));
        valid.EstimatedWidth = int.MaxValue;
        Assert.Throws<QrResourceLimitException>(() => QuietZoneMeasurer.Measure(raster, valid, new QrDetectionOptions()));
        valid.EstimatedWidth = 21;
        raster.Pixels[0] = 128;
        Assert.Throws<QrInputException>(() => QuietZoneMeasurer.Measure(raster, valid, new QrDetectionOptions()));
    }

    static QrRaster Raster(int width, int height, int margin, int scale)
    {
        int pixelWidth = (width + 2 * margin) * scale;
        int pixelHeight = (height + 2 * margin) * scale;
        byte[] pixels = Enumerable.Repeat((byte)255, pixelWidth * pixelHeight).ToArray();
        for (int row = margin * scale; row < (margin + height) * scale; row++)
            for (int column = margin * scale; column < (margin + width) * scale; column++)
                pixels[row * pixelWidth + column] = 0;
        return new QrRaster { Width = pixelWidth, Height = pixelHeight, Stride = pixelWidth, Pixels = pixels };
    }

    static QrDetection Detection(QrSymbolKind kind, int width, int height, int margin, int scale)
    {
        return new QrDetection
        {
            HasSymbolKindEstimate = true, EstimatedSymbolKind = kind, EstimatedWidth = width, EstimatedHeight = height,
            Corners = new QrQuadrilateral
            {
                TopLeft = new QrPoint { X = margin * scale, Y = margin * scale },
                TopRight = new QrPoint { X = (margin + width) * scale, Y = margin * scale },
                BottomRight = new QrPoint { X = (margin + width) * scale, Y = (margin + height) * scale },
                BottomLeft = new QrPoint { X = margin * scale, Y = (margin + height) * scale }
            }
        };
    }

    static QrPoint[] Points(QrQuadrilateral corners)
    {
        return [corners.TopLeft!, corners.TopRight!, corners.BottomRight!, corners.BottomLeft!];
    }
}
