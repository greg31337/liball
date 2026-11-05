using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class PublicDataClassTests
{
    [Test]
    public void GeometryClassesRetainImageCoordinatesAndCornerIdentity()
    {
        QrPoint topLeft = new QrPoint { X = 1.25, Y = 2.5 };
        QrPoint topRight = new QrPoint { X = 30.75, Y = 3.0 };
        QrPoint bottomRight = new QrPoint { X = 29.5, Y = 32.25 };
        QrPoint bottomLeft = new QrPoint { X = 0.5, Y = 31.0 };
        QrQuadrilateral quadrilateral = new QrQuadrilateral
        {
            TopLeft = topLeft,
            TopRight = topRight,
            BottomRight = bottomRight,
            BottomLeft = bottomLeft
        };

        Assert.Multiple(() =>
        {
            Assert.That(quadrilateral.TopLeft, Is.SameAs(topLeft));
            Assert.That(quadrilateral.TopRight, Is.SameAs(topRight));
            Assert.That(quadrilateral.BottomRight, Is.SameAs(bottomRight));
            Assert.That(quadrilateral.BottomLeft, Is.SameAs(bottomLeft));
            Assert.That(quadrilateral.BottomRight!.X, Is.EqualTo(29.5));
            Assert.That(quadrilateral.BottomRight.Y, Is.EqualTo(32.25));
        });
    }

    [Test]
    public void ImageMatrixAndRasterRetainCallerOwnedBuffersAndLayouts()
    {
        byte[] imagePixels = new byte[] { 1, 2, 3, 4, 5, 6 };
        QrImage image = new QrImage
        {
            Pixels = imagePixels,
            Width = 2,
            Height = 1,
            Stride = 6,
            PixelFormat = QrPixelFormat.Rgb24
        };
        QrModuleValue[] values = new QrModuleValue[] { QrModuleValue.Dark, QrModuleValue.Light };
        QrModuleRole[] roles = new QrModuleRole[] { QrModuleRole.Finder, QrModuleRole.Data };
        byte[] confidence = new byte[] { 255, 192 };
        QrMatrix matrix = new QrMatrix
        {
            Width = 2,
            Height = 1,
            Values = values,
            Roles = roles,
            Confidence = confidence,
            QuietZoneLeft = 4,
            QuietZoneTop = 3,
            QuietZoneRight = 2,
            QuietZoneBottom = 1
        };
        byte[] rasterPixels = new byte[] { 0, 255 };
        QrRaster raster = new QrRaster
        {
            Pixels = rasterPixels,
            Width = 2,
            Height = 1,
            Stride = 2
        };

        Assert.Multiple(() =>
        {
            Assert.That(image.Pixels, Is.SameAs(imagePixels));
            Assert.That(image.Width, Is.EqualTo(2));
            Assert.That(image.Height, Is.EqualTo(1));
            Assert.That(image.Stride, Is.EqualTo(6));
            Assert.That(image.PixelFormat, Is.EqualTo(QrPixelFormat.Rgb24));
            Assert.That(matrix.Values, Is.SameAs(values));
            Assert.That(matrix.Roles, Is.SameAs(roles));
            Assert.That(matrix.Confidence, Is.SameAs(confidence));
            Assert.That(matrix.QuietZoneLeft, Is.EqualTo(4));
            Assert.That(matrix.QuietZoneTop, Is.EqualTo(3));
            Assert.That(matrix.QuietZoneRight, Is.EqualTo(2));
            Assert.That(matrix.QuietZoneBottom, Is.EqualTo(1));
            Assert.That(raster.Pixels, Is.SameAs(rasterPixels));
            Assert.That(raster.Stride, Is.EqualTo(2));
        });
    }

    [Test]
    public void BufferClassesStartWithUsableEmptyArrays()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new QrImage().Pixels, Is.Empty);
            Assert.That(new QrMatrix().Values, Is.Empty);
            Assert.That(new QrRaster().Pixels, Is.Empty);
            Assert.That(new QrSegment().Data, Is.Empty);
            Assert.That(new QrCorrectionResult().Codewords, Is.Empty);
            Assert.That(new QrCorrectionResult().Syndromes, Is.Empty);
            Assert.That(new QrAttemptResult().RawBytes, Is.Empty);
            Assert.That(new QrAttemptResult().Segments, Is.Empty);
            Assert.That(new QrEncodeAttemptResult().SequenceMatrices, Is.Empty);
        });
    }

    [Test]
    public void SegmentRetainsRawTextAndControlMetadata()
    {
        byte[] data = new byte[] { 0x41, 0x00, 0x42 };
        QrStructuredAppendInfo append = new QrStructuredAppendInfo
        {
            Index = 1,
            Total = 3,
            Parity = 0xA5,
            ApplicationIdentifier = "shipment-7",
            IsComplete = false
        };
        QrSegment segment = new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = 3,
            Data = data,
            Text = "A\0B",
            EciAssignment = 26,
            Fnc1ApplicationIndicator = 42,
            StructuredAppend = append,
            IsHanziCompatibilityMode = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(segment.Mode, Is.EqualTo(QrEncodingMode.Byte));
            Assert.That(segment.CharacterCount, Is.EqualTo(3));
            Assert.That(segment.Data, Is.SameAs(data));
            Assert.That(segment.Text, Is.EqualTo("A\0B"));
            Assert.That(segment.EciAssignment, Is.EqualTo(26));
            Assert.That(segment.Fnc1ApplicationIndicator, Is.EqualTo(42));
            Assert.That(segment.StructuredAppend, Is.SameAs(append));
            Assert.That(segment.IsHanziCompatibilityMode, Is.True);
            Assert.That(append.ApplicationIdentifier, Is.EqualTo("shipment-7"));
        });
    }

    [Test]
    public void OptionalSegmentProtocolNumbersUseUnambiguousSentinels()
    {
        QrSegment segment = new QrSegment();

        Assert.Multiple(() =>
        {
            Assert.That(segment.EciAssignment, Is.EqualTo(-1));
            Assert.That(segment.Fnc1ApplicationIndicator, Is.EqualTo(-1));
            Assert.That(segment.StructuredAppend, Is.Null);
        });
    }

    [Test]
    public void CapacityClassesRetainFamilyAndExactBitAccounting()
    {
        QrCapacityRequest request = new QrCapacityRequest
        {
            SymbolKind = QrSymbolKind.rMQR,
            Version = 0,
            Width = 43,
            Height = 7,
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium,
            EncodingMode = QrEncodingMode.Alphanumeric,
            CharacterCount = 12,
            PayloadBitCount = 66,
            ControlBitCount = 4
        };
        QrCapacityResult result = new QrCapacityResult
        {
            Fits = true,
            RequiredBitCount = 79,
            DataBitCapacity = 96,
            UnusedBitCount = 17,
            DataCodewordCount = 12,
            ErrorCorrectionCodewordCount = 7,
            TotalCodewordCount = 19,
            RemainderBitCount = 3
        };

        Assert.Multiple(() =>
        {
            Assert.That(request.SymbolKind, Is.EqualTo(QrSymbolKind.rMQR));
            Assert.That(request.Width, Is.EqualTo(43));
            Assert.That(request.Height, Is.EqualTo(7));
            Assert.That(request.ErrorCorrectionLevel, Is.EqualTo(QrErrorCorrectionLevel.Medium));
            Assert.That(request.EncodingMode, Is.EqualTo(QrEncodingMode.Alphanumeric));
            Assert.That(request.PayloadBitCount, Is.EqualTo(66));
            Assert.That(request.ControlBitCount, Is.EqualTo(4));
            Assert.That(result.Fits, Is.True);
            Assert.That(result.RequiredBitCount, Is.EqualTo(79));
            Assert.That(result.DataBitCapacity, Is.EqualTo(96));
            Assert.That(result.UnusedBitCount, Is.EqualTo(17));
            Assert.That(result.TotalCodewordCount, Is.EqualTo(19));
            Assert.That(result.RemainderBitCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void CorrectionAndAttemptResultsRetainDiagnosticsAndOutputs()
    {
        byte[] repaired = new byte[] { 10, 20, 30 };
        int[] syndromes = new int[] { 0, 0, 0 };
        QrCorrectionResult correction = new QrCorrectionResult
        {
            Success = true,
            FailureReason = QrFailureReason.None,
            Codewords = repaired,
            CorrectedErrorCount = 2,
            CorrectedErasureCount = 1,
            Syndromes = syndromes,
            Verified = true
        };
        QrSegment segment = new QrSegment { Mode = QrEncodingMode.Numeric, Text = "123" };
        QrAttemptResult decodeAttempt = new QrAttemptResult
        {
            Success = false,
            FailureReason = QrFailureReason.ChecksumFailure,
            Text = "partial",
            RawBytes = new byte[] { 1, 2 },
            Segments = new QrSegment[] { segment },
            CandidateCount = 3,
            AttemptCount = 5,
            CorrectedErrorCount = 1,
            CorrectedErasureCount = 2
        };
        QrMatrix matrix = new QrMatrix { Width = 21, Height = 21 };
        QrEncodeAttemptResult encodeAttempt = new QrEncodeAttemptResult
        {
            Success = true,
            FailureReason = QrFailureReason.None,
            Matrix = matrix,
            SequenceMatrices = new QrMatrix[] { matrix },
            RequiredBitCount = 120,
            AvailableBitCount = 152
        };

        Assert.Multiple(() =>
        {
            Assert.That(correction.Codewords, Is.SameAs(repaired));
            Assert.That(correction.Syndromes, Is.SameAs(syndromes));
            Assert.That(correction.CorrectedErrorCount, Is.EqualTo(2));
            Assert.That(correction.CorrectedErasureCount, Is.EqualTo(1));
            Assert.That(correction.Verified, Is.True);
            Assert.That(decodeAttempt.FailureReason, Is.EqualTo(QrFailureReason.ChecksumFailure));
            Assert.That(decodeAttempt.Segments, Has.Exactly(1).SameAs(segment));
            Assert.That(decodeAttempt.CandidateCount, Is.EqualTo(3));
            Assert.That(decodeAttempt.AttemptCount, Is.EqualTo(5));
            Assert.That(encodeAttempt.Matrix, Is.SameAs(matrix));
            Assert.That(encodeAttempt.SequenceMatrices, Has.Exactly(1).SameAs(matrix));
            Assert.That(encodeAttempt.RequiredBitCount, Is.EqualTo(120));
            Assert.That(encodeAttempt.AvailableBitCount, Is.EqualTo(152));
        });
    }
}
