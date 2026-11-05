using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class ImagingExceptionContractTests
{
    [TestCase("CreateLuminanceImage")]
    [TestCase("BinarizeImage")]
    [TestCase("DetectImage")]
    [TestCase("SampleDetection")]
    [TestCase("ResampleDetection")]
    [TestCase("DecodeDetection")]
    [TestCase("ScanImage")]
    [TestCase("AnalyzeImageQuality")]
    public void ImageOperationsRejectMissingInputsBadLayoutsAndResourceLimits(string member)
    {
        QrSymbol symbol = Symbol();
        QrImage image = Render(symbol);
        byte[] originalPixels = (byte[])image.Pixels.Clone();
        QrDetection detection = Detection();
        object Call(QrImage source, QrDetectionOptions options) => member switch
        {
            "CreateLuminanceImage" => ((IQrBinarizer)new QrBinarizer()).CreateLuminanceImage(source, options),
            "BinarizeImage" => ((IQrBinarizer)new QrBinarizer()).BinarizeImage(source, options),
            "DetectImage" => ((IQrDetector)new QrDetector()).DetectImage(source, options),
            "SampleDetection" => ((IQrPerspectiveSampler)new QrPerspectiveSampler()).SampleDetection(source, detection, options),
            "ResampleDetection" => ((IQrPerspectiveSampler)new QrPerspectiveSampler()).ResampleDetection(source, detection, symbol.Matrix!, options),
            "DecodeDetection" => ((IQrDecoder)new QrDecoder()).DecodeDetection(source, detection, options, new QrDecodeOptions()),
            "ScanImage" => ((IQrScanner)new QrScanner()).ScanImage(source, options, new QrDecodeOptions(), true),
            "AnalyzeImageQuality" => ((IQrQualityAnalyzer)new QrQualityAnalyzer()).AnalyzeImageQuality(source, detection, options, new QrQualityOptions()),
            _ => throw new AssertionException(member)
        };
        QrDetectionOptions valid = Sampling();
        AssertInput(() => Call(null!, valid));
        AssertInput(() => Call(image, null!));
        int stride = image.Stride;
        image.Stride = 1;
        AssertInput(() => Call(image, valid));
        image.Stride = stride;
        byte[] pixels = image.Pixels;
        image.Pixels = [];
        AssertInput(() => Call(image, valid));
        image.Pixels = pixels;
        foreach (string limit in new[] { "width", "height", "area" })
        {
            QrDetectionOptions limited = Sampling();
            if (limit == "width") limited.MaximumImageWidth = image.Width - 1;
            if (limit == "height") limited.MaximumImageHeight = image.Height - 1;
            if (limit == "area") limited.MaximumImagePixels = image.Pixels.Length - 1;
            QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(() => Call(image, limited))!;
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
            Assert.That(error.RequestedValue, Is.EqualTo(error.MaximumValue + 1));
            Assert.That(error.LimitName, Is.EqualTo(limit switch
            {
                "width" => "MaximumWidth", "height" => "MaximumHeight", _ => "MaximumArea"
            }));
        }
        // Valid input must still produce meaningful results after rejected calls.
        object result = Call(image, valid);
        switch (result)
        {
            case QrImage gray: Assert.That(gray.Pixels, Is.EqualTo(pixels)); break;
            case QrMatrix[] binary: Assert.That(binary[0].Values, Does.Contain(QrModuleValue.Dark)); break;
            case QrDetectionSet detections: Assert.That(detections.Detections, Has.Length.EqualTo(1)); break;
            case QrDetection sampled: Assert.That(sampled.SampledMatrix!.Values, Is.EqualTo(symbol.Matrix!.Values)); break;
            case QrDecodeResult decoded: Assert.That(decoded.Text, Is.EqualTo("HELLO")); break;
            case QrDecodeSet scanned: Assert.That(scanned.Results.Single().Text, Is.EqualTo("HELLO")); break;
            case QrQualityReport quality:
                Assert.That(quality.SampleCount, Is.GreaterThanOrEqualTo(441));
                Assert.That(quality.Metrics.Single(m => m.Name == "SymbolContrast").Value, Is.EqualTo(1));
                break;
            default: Assert.Fail("Unexpected public result type."); break;
        }
        Assert.That(image.Pixels, Is.EqualTo(originalPixels));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BinaryDetectionAndScanningEnforceDimensions(bool scan)
    {
        QrImage image = Render(Symbol());
        QrMatrix matrix = new QrBinarizer().BinarizeImage(image, Sampling())[0];
        TestDelegate Call(QrMatrix source, QrDetectionOptions options) => () =>
        {
            if (scan) new QrScanner().ScanBinaryMatrix(source, options, new QrDecodeOptions(), false);
            else new QrDetector().DetectBinaryMatrix(source, options);
        };
        AssertInput(Call(null!, Sampling()));
        AssertInput(Call(matrix, null!));
        QrDetectionOptions limited = Sampling();
        limited.MaximumImagePixels = matrix.Values.Length - 1;
        QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(Call(matrix, limited))!;
        Assert.That(error.LimitName, Is.EqualTo("MaximumArea"));
        Assert.That(error.RequestedValue, Is.EqualTo(matrix.Values.Length));
        Assert.That(error.MaximumValue, Is.EqualTo(matrix.Values.Length - 1));
        Assert.DoesNotThrow(Call(matrix, Sampling()));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BothRenderersRejectOversizedRastersBeforeAllocation(bool roles)
    {
        IQrRenderer renderer = new QrRenderer();
        QrSymbol symbol = Symbol();
        QrRaster Call(int scale) => roles
            ? renderer.RenderRoleMap(symbol, new QrRenderOptions { Scale = scale })
            : renderer.RenderSymbol(symbol, new QrRenderOptions { Scale = scale });
        AssertInput(() => Call(0));
        QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(() => Call(1000))!;
        Assert.That(error.LimitName, Is.EqualTo("MaximumWidth"));
        Assert.That(error.RequestedValue, Is.EqualTo(29000));
        Assert.That(error.MaximumValue, Is.EqualTo(QrLimits.DefaultMaximumImageDimension));
        Assert.That(Call(1).Pixels, Has.Length.EqualTo(29 * 29));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DecoderEntryPointsValidateDecodeOptionsAndAllocationBudget(bool imageInput)
    {
        IQrDecoder decoder = new QrDecoder();
        QrSymbol symbol = Symbol();
        QrImage image = Render(symbol);
        QrDecodeResult Call(QrDecodeOptions options) => imageInput
            ? decoder.DecodeDetection(image, Detection(), Sampling(), options)
            : decoder.DecodeMatrix(symbol.Matrix!, options);
        if (imageInput)
            AssertInput(() => decoder.DecodeDetection(image, null!, Sampling(), new QrDecodeOptions()));
        else
            AssertInput(() => decoder.DecodeMatrix(null!, new QrDecodeOptions()));
        AssertInput(() => Call(null!));
        AssertInput(() => Call(new QrDecodeOptions { MaximumSegments = 0 }));
        QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(() =>
            Call(new QrDecodeOptions { MaximumAllocationBytes = 1024 }))!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
        Assert.That(error.RequestedValue, Is.GreaterThan(error.MaximumValue));
        Assert.That(error.MaximumValue, Is.EqualTo(1024));
        Assert.That(Call(new QrDecodeOptions()).Text, Is.EqualTo("HELLO"));
    }

    [TestCase("DecodeMatrix")]
    [TestCase("DecodeDetection")]
    [TestCase("AnalyzeMatrixQuality")]
    [TestCase("AnalyzeImageQuality")]
    public void UncorrectableSymbolsRaiseDecodeFailures(string member)
    {
        QrSymbol symbol = Symbol();
        // Corrupt twenty distinct codewords while preserving all function modules.
        for (int index = 0; index < 20; index++) symbol.Codewords[index] ^= (byte)(index + 0x81);
        new QrMatrixProcessor().BuildMatrix(symbol, 0);
        QrImage image = Render(symbol);
        QrDecodeOptions options = new QrDecodeOptions { EnableRecovery = false, TryMirrored = false, TryInverted = false };
        QrDecodeException error = Assert.Throws<QrDecodeException>(() =>
        {
            switch (member)
            {
                case "DecodeMatrix": new QrDecoder().DecodeMatrix(symbol.Matrix!, options); break;
                case "DecodeDetection": new QrDecoder().DecodeDetection(image, Detection(), Sampling(), options); break;
                case "AnalyzeMatrixQuality": new QrQualityAnalyzer().AnalyzeMatrixQuality(symbol.Matrix!, new QrQualityOptions()); break;
                case "AnalyzeImageQuality": new QrQualityAnalyzer().AnalyzeImageQuality(image, Detection(), Sampling(), new QrQualityOptions()); break;
                default: throw new AssertionException(member);
            }
        })!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
        Assert.That(error.RecoveryAttemptCount, Is.GreaterThan(0));
        Assert.That(error.CandidateIndex, Is.EqualTo(member == "DecodeDetection" ? 42 : -1));
    }

    static void AssertInput(TestDelegate call)
    {
        QrInputException error = Assert.Throws<QrInputException>(call)!;
        Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidInput));
        Assert.That(error.InputName, Is.Not.Null.And.Not.Empty);
    }

    static QrSymbol Symbol() => new QrEncoder().EncodeText("HELLO", new QrEncodeOptions
    {
        ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = 1, MaximumVersion = 1,
        ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
        BoostErrorCorrectionLevel = false, QuietZoneModules = 0, MaskPattern = QrMaskPattern.Zero
    });

    static QrImage Render(QrSymbol symbol)
    {
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = 4, QuietZoneModules = 4 });
        return new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels };
    }

    static QrDetectionOptions Sampling() => new QrDetectionOptions
    {
        BinarizationMode = QrBinarizationMode.GlobalHistogram, Polarity = QrPolarity.DarkOnLight,
        MaximumPyramidLevels = 1, AllowMicro = false, AllowRectangularMicro = false, AllowLegacyModel1 = false
    };

    static QrDetection Detection() => new QrDetection
    {
        EstimatedWidth = 21, EstimatedHeight = 21, Polarity = QrPolarity.DarkOnLight, CandidateIndex = 42,
        HasSymbolKindEstimate = true, EstimatedSymbolKind = QrSymbolKind.Model2, EstimatedVersion = 1, ModuleSize = 4,
        Corners = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = 16, Y = 16 }, TopRight = new QrPoint { X = 100, Y = 16 },
            BottomRight = new QrPoint { X = 100, Y = 100 }, BottomLeft = new QrPoint { X = 16, Y = 100 }
        }
    };
}
