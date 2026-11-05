using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrMalformedLayoutTests
{
    static readonly string[] ImageMembers = { "CreateLuminanceImage", "BinarizeImage", "DetectImage", "SampleDetection",
        "ResampleDetection", "DecodeDetection", "TryDecodeDetection", "ScanImage", "AnalyzeImageQuality" };
    static readonly string[] MatrixMembers = { "ExtractCodewords", "ClassifyModules", "ValidateMatrix", "TransformMatrix",
        "RenderSymbol", "RenderRoleMap", "DecodeMatrix", "TryDecodeMatrix", "AnalyzeMatrixQuality", "DetectBinaryMatrix", "ScanBinaryMatrix" };

    static IEnumerable<TestCaseData> ImageLayouts()
    {
        foreach (string member in ImageMembers)
            foreach (QrPixelFormat format in Enum.GetValues<QrPixelFormat>())
                foreach (string damage in new[] { "NegativeStride", "ZeroStride", "ShortRow", "TruncatedBuffer", "OverflowStride", "OverflowDimensions" })
                    yield return new TestCaseData(member, format, damage);
    }

    [TestCaseSource(nameof(ImageLayouts))]
    public void EveryImageEntryPointRejectsMalformedLayoutsBeforeReadingOrAllocating(string member, QrPixelFormat format, string damage)
    {
        int channels = format == QrPixelFormat.Gray8 ? 1 : format == QrPixelFormat.Rgb24 ? 3 : 4;
        QrImage image = new QrImage { Width = 2, Height = 2, Stride = 2 * channels, PixelFormat = format,
            Pixels = Enumerable.Repeat((byte)117, 4 * channels).ToArray() };
        switch (damage)
        {
            case "NegativeStride": image.Stride = int.MinValue; break;
            case "ZeroStride": image.Stride = 0; break;
            case "ShortRow": image.Stride--; break;
            case "TruncatedBuffer": image.Pixels = image.Pixels[..^1]; break;
            case "OverflowStride": image.Stride = int.MaxValue; break;
            case "OverflowDimensions": image.Width = image.Height = int.MaxValue; break;
        }
        byte[] before = (byte[])image.Pixels.Clone();
        bool resource = damage.StartsWith("Overflow", StringComparison.Ordinal);
        AssertRejected(() => CallImage(member, image), resource);
        AssertRejected(() => CallImage(member, image), resource);
        Assert.That(image.Pixels, Is.EqualTo(before));
    }

    static IEnumerable<TestCaseData> MatrixLayouts()
    {
        foreach (string member in MatrixMembers)
            foreach (string damage in new[] { "ZeroWidth", "NegativeHeight", "ShortValues", "LongValues", "NullValues", "OverflowDimensions" })
                yield return new TestCaseData(member, damage);
    }

    [TestCaseSource(nameof(MatrixLayouts))]
    public void EveryMatrixEntryPointRejectsMalformedStorage(string member, string damage)
    {
        QrSymbol symbol = Symbol();
        QrMatrix matrix = symbol.Matrix!;
        switch (damage)
        {
            case "ZeroWidth": matrix.Width = 0; break;
            case "NegativeHeight": matrix.Height = int.MinValue; break;
            case "ShortValues": matrix.Values = matrix.Values[..^1]; break;
            case "LongValues": matrix.Values = matrix.Values.Concat(new[] { QrModuleValue.Light }).ToArray(); break;
            case "NullValues": matrix.Values = null!; break;
            case "OverflowDimensions": matrix.Width = matrix.Height = int.MaxValue; break;
        }
        QrModuleValue[]? before = matrix.Values == null ? null : (QrModuleValue[])matrix.Values.Clone();
        AssertRejected(() => CallMatrix(member, symbol), damage == "OverflowDimensions");
        Assert.That(matrix.Values, Is.EqualTo(before));
    }

    [TestCase("ExtractCodewords")]
    [TestCase("ClassifyModules")]
    [TestCase("TransformMatrix")]
    [TestCase("RenderSymbol")]
    [TestCase("RenderRoleMap")]
    [TestCase("DecodeMatrix")]
    [TestCase("TryDecodeMatrix")]
    [TestCase("AnalyzeMatrixQuality")]
    public void OverflowingQuietZoneExtentsDoNotEscapeAsArithmeticExceptions(string member)
    {
        QrSymbol symbol = Symbol();
        symbol.Matrix!.QuietZoneLeft = symbol.Matrix.QuietZoneRight = int.MaxValue;
        AssertRejected(() => CallMatrix(member, symbol), false);
    }

    [TestCase("BuildMatrix")]
    [TestCase("TransformMatrix")]
    [TestCase("RenderSymbol")]
    [TestCase("RenderRoleMap")]
    [TestCase("EncodeText")]
    [TestCase("EncodeBytes")]
    [TestCase("EncodeSegments")]
    [TestCase("TryEncodeText")]
    [TestCase("TryEncodeSegments")]
    [TestCase("EncodeStructuredAppend")]
    [TestCase("EncodeManualStructuredAppend")]
    public void OversizedOutputRequestsAreBoundedBeforeAllocation(string member)
    {
        QrSymbol symbol = Symbol();
        QrEncodeOptions options = Encoding();
        options.QuietZoneModules = int.MaxValue;
        IQrEncoder encoder = new QrEncoder();
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        QrSegment bytes = new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = [0] };
        AssertRejected(() =>
        {
            switch (member)
            {
                case "BuildMatrix": processor.BuildMatrix(symbol, int.MaxValue); break;
                case "TransformMatrix": processor.TransformMatrix(symbol.Matrix!, QrSymbolKind.Model2, 0, false, false, 0, 0, 0, 0, false, 0, int.MaxValue); break;
                case "RenderSymbol": new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = int.MaxValue }); break;
                case "RenderRoleMap": new QrRenderer().RenderRoleMap(symbol, new QrRenderOptions { Scale = int.MaxValue }); break;
                case "EncodeText": encoder.EncodeText("A", options); break;
                case "EncodeBytes": encoder.EncodeBytes([0], options); break;
                case "EncodeSegments": encoder.EncodeSegments([bytes], options); break;
                case "TryEncodeText": encoder.TryEncodeText("A", options); break;
                case "TryEncodeSegments": encoder.TryEncodeSegments([bytes], options); break;
                case "EncodeStructuredAppend": encoder.EncodeStructuredAppend(new byte[30], options); break;
                case "EncodeManualStructuredAppend": encoder.EncodeManualStructuredAppend(ManualParts(), options); break;
            }
        }, true);
    }

    [TestCase("SampleDetection")]
    [TestCase("ResampleDetection")]
    [TestCase("DecodeDetection")]
    [TestCase("TryDecodeDetection")]
    [TestCase("AnalyzeImageQuality")]
    public void NonfiniteAndDegenerateGeometryIsRejected(string member)
    {
        QrImage image = QrMalformedPayloadTests.Render(Symbol().Matrix!);
        foreach (double coordinate in new[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity })
        {
            QrDetection detection = QrMalformedPayloadTests.Detection(Symbol().Matrix!);
            detection.Corners!.TopLeft!.X = coordinate;
            AssertRejected(() => CallImage(member, image, detection), false);
        }
        QrDetection collapsed = QrMalformedPayloadTests.Detection(Symbol().Matrix!);
        collapsed.Corners!.BottomRight = collapsed.Corners.TopLeft;
        AssertRejected(() => CallImage(member, image, collapsed), false);
    }

    static object CallImage(string member, QrImage image, QrDetection? supplied = null)
    {
        QrSymbol symbol = Symbol();
        QrDetection detection = supplied ?? QrMalformedPayloadTests.Detection(symbol.Matrix!);
        QrDetectionOptions sampling = QrMalformedPayloadTests.Sampling();
        QrDecodeOptions decoding = QrMalformedPayloadTests.Decoding();
        return member switch
        {
            "CreateLuminanceImage" => ((IQrBinarizer)new QrBinarizer()).CreateLuminanceImage(image, sampling),
            "BinarizeImage" => ((IQrBinarizer)new QrBinarizer()).BinarizeImage(image, sampling),
            "DetectImage" => ((IQrDetector)new QrDetector()).DetectImage(image, sampling),
            "SampleDetection" => ((IQrPerspectiveSampler)new QrPerspectiveSampler()).SampleDetection(image, detection, sampling),
            "ResampleDetection" => ((IQrPerspectiveSampler)new QrPerspectiveSampler()).ResampleDetection(image, detection, symbol.Matrix!, sampling),
            "DecodeDetection" => ((IQrDecoder)new QrDecoder()).DecodeDetection(image, detection, sampling, decoding),
            "TryDecodeDetection" => ((IQrDecoder)new QrDecoder()).TryDecodeDetection(image, detection, sampling, decoding),
            "ScanImage" => ((IQrScanner)new QrScanner()).ScanImage(image, sampling, decoding, true),
            "AnalyzeImageQuality" => ((IQrQualityAnalyzer)new QrQualityAnalyzer()).AnalyzeImageQuality(image, detection, sampling, new QrQualityOptions()),
            _ => throw new AssertionException(member)
        };
    }
    static object CallMatrix(string member, QrSymbol symbol)
    {
        QrMatrix matrix = symbol.Matrix!;
        IQrMatrixProcessor processor = new QrMatrixProcessor();
        return member switch
        {
            "ExtractCodewords" => processor.ExtractCodewords(matrix, symbol.SymbolKind, symbol.Version, symbol.ErrorCorrectionLevel, symbol.MaskPattern),
            "ClassifyModules" => processor.ClassifyModules(matrix, symbol.SymbolKind, symbol.Version, symbol.ErrorCorrectionLevel),
            "ValidateMatrix" => processor.ValidateMatrix(symbol),
            "TransformMatrix" => processor.TransformMatrix(matrix, symbol.SymbolKind, 0, false, false, 0, 0, 0, 0, false, 0, 1),
            "RenderSymbol" => ((IQrRenderer)new QrRenderer()).RenderSymbol(symbol, new QrRenderOptions()),
            "RenderRoleMap" => ((IQrRenderer)new QrRenderer()).RenderRoleMap(symbol, new QrRenderOptions()),
            "DecodeMatrix" => ((IQrDecoder)new QrDecoder()).DecodeMatrix(matrix, QrMalformedPayloadTests.Decoding()),
            "TryDecodeMatrix" => ((IQrDecoder)new QrDecoder()).TryDecodeMatrix(matrix, QrMalformedPayloadTests.Decoding()),
            "AnalyzeMatrixQuality" => ((IQrQualityAnalyzer)new QrQualityAnalyzer()).AnalyzeMatrixQuality(matrix, new QrQualityOptions()),
            "DetectBinaryMatrix" => ((IQrDetector)new QrDetector()).DetectBinaryMatrix(matrix, QrMalformedPayloadTests.Sampling()),
            "ScanBinaryMatrix" => ((IQrScanner)new QrScanner()).ScanBinaryMatrix(matrix, QrMalformedPayloadTests.Sampling(), QrMalformedPayloadTests.Decoding(), true),
            _ => throw new AssertionException(member)
        };
    }
    internal static void AssertRejected(TestDelegate call, bool resource)
    {
        if (resource)
        {
            QrResourceLimitException error = Assert.Throws<QrResourceLimitException>(call)!;
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.ResourceLimit));
            Assert.That(error.LimitName, Is.Not.Empty);
        }
        else
        {
            QrInputException error = Assert.Throws<QrInputException>(call)!;
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.InvalidInput));
            Assert.That(error.InputName, Is.Not.Null.And.Not.Empty);
        }
    }
    internal static QrEncodeOptions Encoding() => new QrEncodeOptions
    {
        ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MinimumVersion = 1, MaximumVersion = 1,
        ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = QrErrorCorrectionLevel.Low,
        BoostErrorCorrectionLevel = false, QuietZoneModules = 0, MaskPattern = QrMaskPattern.Zero
    };
    internal static QrSymbol Symbol() => new QrEncoder().EncodeText("A", Encoding());
    internal static QrSegment[][] ManualParts() => Enumerable.Range(0, 2).Select(index => new[]
    {
        new QrSegment { Mode = QrEncodingMode.StructuredAppend, StructuredAppend = new QrStructuredAppendInfo { Index = index, Total = 2, Parity = 0 } },
        new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = [0] }
    }).ToArray();
}
