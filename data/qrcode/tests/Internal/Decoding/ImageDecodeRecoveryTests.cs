using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ImageDecodeRecoveryTests
{
    [TestCase(QrSymbolKind.Model2, false)]
    [TestCase(QrSymbolKind.Model2, true)]
    [TestCase(QrSymbolKind.Micro, false)]
    [TestCase(QrSymbolKind.Micro, true)]
    [TestCase(QrSymbolKind.rMQR, false)]
    [TestCase(QrSymbolKind.rMQR, true)]
    public void DecodesFreshSamplesAndPreservesCallerState(QrSymbolKind kind, bool inverted)
    {
        QrSymbol symbol = CreateSymbol(kind);
        QrImage image = Render(symbol, inverted);
        QrDetection detection = Detection(symbol, inverted);
        detection.SampledMatrix = new QrMatrix { Values = [QrModuleValue.Unknown] };
        detection.Diagnostics = ["original"];
        byte[] before = (byte[])image.Pixels.Clone();
        QrDetectionOptions sampling = BinaryOptions();
        QrDecodeOptions decoding = new QrDecodeOptions { Polarity = detection.Polarity };
        ImageRecoveryResult recovered = ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, decoding);
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.SampleCount, Is.EqualTo(1));
        Assert.That(recovered.ThresholdCandidateIndex, Is.Zero);
        QrDecodeResult result = ImageDecodeRecovery.Decode(image, detection, null, sampling, decoding);
        Assert.That(result.Text, Is.EqualTo("12345"));
        Assert.That(result.Detection!.SampledMatrix!.Values, Is.EqualTo(symbol.Matrix!.Values));
        Assert.That(result.Detection.Polarity, Is.EqualTo(detection.Polarity));
        Assert.That(result.Detection.CandidateIndex, Is.EqualTo(7));
        Assert.That(result.Detection.QuietZone!.IsConformant, Is.True);
        Assert.That(result.Detection.Corners, Is.Not.SameAs(detection.Corners));
        Assert.That(result.Detection.Diagnostics, Is.Not.SameAs(detection.Diagnostics));
        Assert.That(result.Symbol!.Matrix!.Values, Is.EqualTo(symbol.Matrix.Values));
        result.Detection.Corners!.TopLeft!.X = 999;
        result.Detection.Diagnostics[0] = "changed";
        Assert.That(detection.Corners!.TopLeft!.X, Is.EqualTo(24));
        Assert.That(detection.Diagnostics[0], Is.EqualTo("original"));
        Assert.That(detection.SampledMatrix.Values, Has.Length.EqualTo(1));
        Assert.That(image.Pixels, Is.EqualTo(before));
        Assert.That(decoding.Polarity, Is.EqualTo(detection.Polarity));
        Assert.That(decoding.MaximumRecoveryAttempts, Is.EqualTo(64));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RetriesSubmoduleSamplingOffsets(bool multiPoint)
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrImage image = Render(symbol, false);
        QrDetection detection = Detection(symbol, false);
        foreach (QrPoint point in Points(detection.Corners!)) point.X += 3.6;
        QrDetectionOptions sampling = BinaryOptions();
        sampling.RefineGrid = false;
        sampling.UseMultiPointSampling = multiPoint;
        QrDecodeOptions disabled = new QrDecodeOptions { EnableRecovery = false };
        Assert.That(ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, disabled).Success, Is.False);
        ImageRecoveryResult result = ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.SamplingOffsetX, Is.EqualTo(-0.25));
        Assert.That(result.UsedRefinement, Is.False);
        Assert.That(result.SampleCount, Is.GreaterThan(1));
        Assert.That(ImageDecodeRecovery.Decode(image, detection, null, sampling, new QrDecodeOptions()).Text, Is.EqualTo("12345"));
    }

    [Test]
    public void RetriesLocalThresholdsForUnevenIllumination()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrImage image = Render(symbol, false);
        for (int row = 0; row < image.Height; row++)
            for (int col = 0; col < image.Width; col++)
            {
                int index = row * image.Width + col;
                image.Pixels[index] = (byte)(20 + col * 160 / image.Width + (image.Pixels[index] == 0 ? 0 : 60));
            }
        QrDetectionOptions sampling = new QrDetectionOptions { BinarizationMode = QrBinarizationMode.GlobalHistogram, RefineGrid = false };
        QrDetection detection = Detection(symbol, false);
        Assert.That(ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, new QrDecodeOptions { EnableRecovery = false }).Success, Is.False);
        ImageRecoveryResult result = ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.ThresholdCandidateIndex, Is.EqualTo(1));
        Assert.That(result.SampleCount, Is.EqualTo(2));
        Assert.That(ImageDecodeRecovery.Decode(image, detection, null, sampling, new QrDecodeOptions()).Text, Is.EqualTo("12345"));
    }

    [Test]
    public void RefinesHomographyWhenQuarterModuleOffsetsAreInsufficient()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrImage image = Render(symbol, false);
        QrDetection detection = Detection(symbol, false);
        foreach (QrPoint point in Points(detection.Corners!)) { point.X += 5; }
        QrDetectionOptions sampling = BinaryOptions();
        QrDecodeOptions decoding = new QrDecodeOptions { MaximumRecoveryAttempts = 128 };
        ImageRecoveryResult result = ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, decoding);
        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedRefinement, Is.True);
        Assert.That(result.AlignmentCandidateIndex, Is.EqualTo(-1));
        Assert.That(ImageDecodeRecovery.Decode(image, detection, null, sampling, decoding).Text, Is.EqualTo("12345"));
        sampling.RefineGrid = false;
        Assert.That(ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, decoding).Success, Is.False);
    }

    [Test]
    public void RetriesIndependentAlignmentAnchorsForAnIncorrectFourthCorner()
    {
        QrSymbol symbol = new QrEncoder().EncodeText("12345", new QrEncodeOptions
            { ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = 7, MaximumVersion = 7, QuietZoneModules = 0 });
        QrImage image = Render(symbol, false);
        QrDetection detection = Detection(symbol, false);
        int size = symbol.Matrix!.Width;
        QrQuadrilateral logical = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = 3.5, Y = 3.5 }, TopRight = new QrPoint { X = size - 3.5, Y = 3.5 },
            BottomRight = new QrPoint { X = size - 3.5, Y = size - 3.5 }, BottomLeft = new QrPoint { X = 3.5, Y = size - 3.5 }
        };
        PerspectiveTransform actual = new PerspectiveTransform { M00 = 6, M11 = 6, M02 = 24, M12 = 24, M22 = 1 };
        QrQuadrilateral observed = new QrQuadrilateral
        {
            TopLeft = actual.MapPoint(logical.TopLeft!), TopRight = actual.MapPoint(logical.TopRight!),
            BottomRight = actual.MapPoint(logical.BottomRight!), BottomLeft = actual.MapPoint(logical.BottomLeft!)
        };
        observed.BottomRight!.X += 14;
        observed.BottomRight.Y -= 10;
        PerspectiveTransform wrong = PerspectiveTransform.Create(logical, observed);
        detection.Corners = ProjectCorners(wrong, size, size);
        QrRaster raster = new QrRaster { Width = image.Width, Height = image.Height, Stride = image.Stride, Pixels = image.Pixels };
        PerspectiveTransform[] alternatives = ImageRecoveryGeometry.FindAlignmentAlternatives(raster, wrong, detection, BinaryOptions());
        Assert.That(alternatives.Length, Is.GreaterThan(1));
        QrDecodeOptions decoding = new QrDecodeOptions { MaximumRecoveryAttempts = 256 };
        ImageRecoveryResult result = ImageDecodeRecovery.RecoverCodewords(image, detection, null, BinaryOptions(), decoding);
        Assert.That(result.Success, Is.True);
        Assert.That(result.AlignmentCandidateIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(ImageDecodeRecovery.Decode(image, detection, null, BinaryOptions(), decoding).Text, Is.EqualTo("12345"));
    }

    [Test]
    public void SharedAttemptLimitStopsImageAndMatrixRetriesWithoutPartialResults()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrImage image = Render(symbol, false);
        Array.Fill(image.Pixels, (byte)255);
        QrDetection detection = Detection(symbol, false);
        QrDecodeOptions options = new QrDecodeOptions { MaximumRecoveryAttempts = 3 };
        ImageRecoveryResult first = ImageDecodeRecovery.RecoverCodewords(image, detection, null, BinaryOptions(), options);
        ImageRecoveryResult second = ImageDecodeRecovery.RecoverCodewords(image, detection, null, BinaryOptions(), options);
        Assert.That(first.Success, Is.False);
        Assert.That(first.AttemptCount, Is.EqualTo(3));
        Assert.That(first.SampleCount, Is.EqualTo(3));
        Assert.That(first.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(first.Detection, Is.Null);
        Assert.That(first.Recovery, Is.Null);
        Assert.That(second.AttemptCount, Is.EqualTo(first.AttemptCount));
        QrDecodeException error = Assert.Throws<QrDecodeException>(() => ImageDecodeRecovery.Decode(image, detection, null, BinaryOptions(), options))!;
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(3));
        Assert.That(error.CandidateIndex, Is.EqualTo(7));
    }

    [Test]
    public void ValidatesInputsAndImageAllocationBeforeRetrying()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrImage image = Render(symbol, false);
        QrDetection detection = Detection(symbol, false);
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.Decode(null!, detection, null, BinaryOptions(), new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.Decode(image, null!, null, BinaryOptions(), new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.Decode(image, detection, null, null!, new QrDecodeOptions()));
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.Decode(image, detection, null, BinaryOptions(), null!));
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.Decode(image, detection, null, BinaryOptions(), new QrDecodeOptions { MaximumRecoveryAttempts = 0 }));
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.Decode(image, detection, null, BinaryOptions(), new QrDecodeOptions { Polarity = QrPolarity.LightOnDark }));
        Assert.Throws<QrResourceLimitException>(() => ImageDecodeRecovery.Decode(image, detection, null, BinaryOptions(), new QrDecodeOptions { MaximumAllocationBytes = 1024 }));
        image.Pixels[0] = 127;
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.Decode(image, detection, null, BinaryOptions(), new QrDecodeOptions()));
    }

    [Test]
    public void RetriesAnAdjustedGlobalThresholdWhenLocalThresholdingFallsBack()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrRaster raster = new QrRenderer().RenderSymbol(symbol,
            new QrRenderOptions { Scale = 1, QuietZoneModules = 4, DarkValue = 100, LightValue = 140 });
        QrMatrix matrix = symbol.Matrix!;
        for (int row = 0; row < matrix.Height; row++)
            for (int col = 0; col < matrix.Width; col++)
            {
                int index = row * matrix.Width + col;
                if (matrix.Values[index] == QrModuleValue.Dark && (matrix.Roles![index] == QrModuleRole.Data || matrix.Roles[index] == QrModuleRole.ErrorCorrection))
                    raster.Pixels[(row + 4) * raster.Stride + col + 4] = 130;
            }
        QrImage image = new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels };
        QrDetection detection = Detection(symbol, false);
        detection.Corners = ProjectCorners(new PerspectiveTransform { M00 = 1, M11 = 1, M02 = 4, M12 = 4, M22 = 1 }, matrix.Width, matrix.Height);
        detection.ModuleSize = 1;
        QrDetectionOptions sampling = new QrDetectionOptions { BinarizationMode = QrBinarizationMode.GlobalHistogram, RefineGrid = false };
        ImageRecoveryResult recovered = ImageDecodeRecovery.RecoverCodewords(image, detection, null, sampling, new QrDecodeOptions());
        Assert.That(recovered.Success, Is.True);
        Assert.That(recovered.ThresholdCandidateIndex, Is.EqualTo(3));
        Assert.That(recovered.SampleCount, Is.EqualTo(4));
        Assert.That(ImageDecodeRecovery.Decode(image, detection, null, sampling, new QrDecodeOptions()).Text, Is.EqualTo("12345"));
    }

    [Test]
    public void SuppliedBinaryValidationPrecedesContrastNormalization()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Micro);
        QrImage image = Render(symbol, false);
        for (int index = 0; index < image.Pixels.Length; index++)
            if (image.Pixels[index] == 0) image.Pixels[index] = 127;
        QrDetectionOptions sampling = BinaryOptions();
        sampling.NormalizeContrast = true;
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.Decode(image, Detection(symbol, false), null, sampling, new QrDecodeOptions()));
    }

    [Test]
    public void UsesExplicitPatternEvidenceWithoutMutatingIt()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrDetection detection = Detection(symbol, false);
        foreach (QrPoint point in Points(detection.Corners!)) point.X += 5;
        QrMatrix evidence = symbol.Matrix!;
        QrModuleValue[] before = (QrModuleValue[])evidence.Values.Clone();
        ImageRecoveryResult result = ImageDecodeRecovery.RecoverCodewords(Render(symbol, false), detection, evidence,
            BinaryOptions(), new QrDecodeOptions());
        Assert.That(result.Success, Is.True);
        Assert.That(result.UsedRefinement, Is.True);
        Assert.That(evidence.Values, Is.EqualTo(before));
        evidence.Roles = null;
        Assert.Throws<QrInputException>(() => ImageDecodeRecovery.RecoverCodewords(Render(symbol, false), detection, evidence,
            BinaryOptions(), new QrDecodeOptions()));
    }

    [Test]
    public void PatternGenerationUsesGeometryEvidenceWithoutGuessingFormatOrPayload()
    {
        foreach (QrSymbolKind kind in new[] { QrSymbolKind.Model2, QrSymbolKind.Micro, QrSymbolKind.rMQR })
        {
            QrSymbol symbol = CreateSymbol(kind);
            QrMatrix evidence = ImageRecoveryGeometry.CreatePatterns(Detection(symbol, false))!;
            for (int index = 0; index < evidence.Values.Length; index++)
            {
                QrModuleRole role = evidence.Roles![index];
                if (role == QrModuleRole.Format || role == QrModuleRole.Data || role == QrModuleRole.ErrorCorrection)
                    Assert.That(evidence.Values[index], Is.EqualTo(QrModuleValue.Unknown));
                if (role == QrModuleRole.Finder || role == QrModuleRole.Timing || role == QrModuleRole.Alignment)
                    Assert.That(evidence.Values[index], Is.EqualTo(symbol.Matrix!.Values[index]));
            }
        }
    }

    [Test]
    public void HonorsFamilyRestrictionsAndDecodesRawRgbWithoutChangingStridePadding()
    {
        QrSymbol symbol = CreateSymbol(QrSymbolKind.Model2);
        QrImage gray = Render(symbol, false);
        QrImage rgb = new QrImage { Width = gray.Width, Height = gray.Height, Stride = gray.Width * 3 + 7,
            PixelFormat = QrPixelFormat.Rgb24, Pixels = new byte[(gray.Width * 3 + 7) * gray.Height] };
        Array.Fill(rgb.Pixels, (byte)17);
        for (int row = 0; row < rgb.Height; row++)
            for (int col = 0; col < rgb.Width; col++)
                for (int channel = 0; channel < 3; channel++) rgb.Pixels[row * rgb.Stride + col * 3 + channel] = gray.Pixels[row * gray.Stride + col];
        byte[] before = (byte[])rgb.Pixels.Clone();
        QrDetectionOptions sampling = new QrDetectionOptions();
        QrDecodeResult result = ImageDecodeRecovery.Decode(rgb, Detection(symbol, false), null, sampling, new QrDecodeOptions());
        Assert.That(result.Text, Is.EqualTo("12345"));
        Assert.That(rgb.Pixels, Is.EqualTo(before));
        sampling.AllowModel2 = false;
        sampling.AllowLegacyModel1 = false;
        Assert.That(ImageDecodeRecovery.RecoverCodewords(rgb, Detection(symbol, false), null, sampling, new QrDecodeOptions { EnableRecovery = false }).Success, Is.False);
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
