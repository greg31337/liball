using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class DecodeAttemptParityTests
{
    [TestCase(false, QrFailureReason.NotFound, 1)]
    [TestCase(true, QrFailureReason.ResourceLimit, 2)]
    public void MissingImageRecoveryPreservesFailureDiagnosticsAndAllowsLaterSuccess(
        bool enableRecovery, QrFailureReason reason, int attempts)
    {
        IQrDecoder decoder = new QrDecoder();
        QrSymbol symbol = new QrEncoder().EncodeText("HELLO", EncodingOptions());
        QrImage blank = Render(symbol);
        Array.Fill(blank.Pixels, (byte)255);
        QrDetection detection = Detection();
        detection.SampledMatrix = symbol.Matrix;
        QrDetectionOptions sampling = new()
        {
            BinarizationMode = QrBinarizationMode.SuppliedBinary, RefineGrid = false
        };
        QrDecodeOptions options = new()
        {
            EnableRecovery = enableRecovery, MaximumRecoveryAttempts = 2,
            TryMirrored = false, TryInverted = false
        };

        QrAttemptResult failed = decoder.TryDecodeDetection(blank, detection, sampling, options);
        Assert.That(failed.Success, Is.False);
        Assert.That(failed.FailureReason, Is.EqualTo(reason));
        Assert.That(failed.CandidateCount, Is.EqualTo(1));
        Assert.That(failed.AttemptCount, Is.EqualTo(attempts));
        Assert.That(failed.Result, Is.Null);
        Assert.That(failed.Text, Is.Null);
        Assert.That(failed.RawBytes, Is.Empty);
        Assert.That(failed.Segments, Is.Empty);
        QrDecodeException error = Assert.Throws<QrDecodeException>(() =>
            decoder.DecodeDetection(blank, detection, sampling, options))!;
        Assert.That(error.FailureReason, Is.EqualTo(reason));
        Assert.That(error.CandidateIndex, Is.EqualTo(17));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(attempts));
        Assert.That(blank.Pixels, Is.All.EqualTo(255));
        Assert.That(detection.SampledMatrix, Is.SameAs(symbol.Matrix));

        QrAttemptResult success = decoder.TryDecodeDetection(Render(symbol), detection, sampling, options);
        Assert.That(success.Success, Is.True);
        Assert.That(success.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(success.Text, Is.EqualTo("HELLO"));
        Assert.That(success.RawBytes, Is.EqualTo("HELLO"u8.ToArray()));
        Assert.That(success.AttemptCount, Is.EqualTo(1));
        Assert.That(success.Result!.Detection!.CandidateIndex, Is.EqualTo(17));
    }

    [TestCase(false, QrFailureReason.InvalidMode)]
    [TestCase(true, QrFailureReason.InvalidMode)]
    [TestCase(false, QrFailureReason.InvalidData)]
    [TestCase(true, QrFailureReason.InvalidData)]
    [TestCase(false, QrFailureReason.UnsupportedTextEncoding)]
    [TestCase(true, QrFailureReason.UnsupportedTextEncoding)]
    [TestCase(false, QrFailureReason.ResourceLimit)]
    [TestCase(true, QrFailureReason.ResourceLimit)]
    [TestCase(false, QrFailureReason.ChecksumFailure)]
    [TestCase(true, QrFailureReason.ChecksumFailure)]
    public void FailedAttemptsMatchThrowingApisWithoutExposingEarlierPayload(bool imageInput, QrFailureReason reason)
    {
        IQrDecoder decoder = new QrDecoder();
        QrSymbol valid = new QrEncoder().EncodeText("HELLO", EncodingOptions());
        QrSymbol broken = BrokenSymbol(reason);
        QrDecodeOptions options = new QrDecodeOptions
        {
            PreserveUnsupportedEciBytes = false, EnableRecovery = false, TryMirrored = false, TryInverted = false,
            MaximumSegments = reason == QrFailureReason.ResourceLimit ? 1 : 100
        };
        QrDetectionOptions sampling = new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary };
        QrDetection detection = Detection();
        QrImage image = Render(broken);
        QrModuleValue[] original = (QrModuleValue[])broken.Matrix!.Values.Clone();
        byte[] pixels = (byte[])image.Pixels.Clone();
        // A cached good sample must not turn a failed fresh image into a success.
        detection.SampledMatrix = valid.Matrix;
        QrAttemptResult prior = imageInput
            ? decoder.TryDecodeDetection(Render(valid), detection, sampling, options)
            : decoder.TryDecodeMatrix(valid.Matrix!, options);
        Assert.That(prior.Success, Is.True);
        Assert.That(prior.Text, Is.EqualTo("HELLO"));
        QrAttemptResult attempt = imageInput
            ? decoder.TryDecodeDetection(image, detection, sampling, options)
            : decoder.TryDecodeMatrix(broken.Matrix, options);
        QrDecodeException error = Assert.Throws<QrDecodeException>(() =>
        {
            if (imageInput) decoder.DecodeDetection(image, detection, sampling, options);
            else decoder.DecodeMatrix(broken.Matrix, options);
        })!;
        Assert.That(error.FailureReason, Is.EqualTo(reason).And.EqualTo(attempt.FailureReason));
        Assert.That(error.CandidateIndex, Is.EqualTo(imageInput ? 17 : -1));
        Assert.That(error.RecoveryAttemptCount, Is.EqualTo(attempt.AttemptCount));
        Assert.That(attempt.AttemptCount, Is.InRange(1, options.MaximumRecoveryAttempts));
        Assert.That(attempt.CandidateCount, Is.EqualTo(1));
        Assert.That(attempt.Success, Is.False);
        Assert.That(attempt.Result, Is.Null);
        Assert.That(attempt.Text, Is.Null);
        Assert.That(attempt.RawBytes, Is.Empty);
        Assert.That(attempt.Segments, Is.Empty);
        Assert.That(attempt.CorrectedErrorCount, Is.Zero);
        Assert.That(attempt.CorrectedErasureCount, Is.Zero);
        Assert.That(broken.Matrix.Values, Is.EqualTo(original));
        Assert.That(image.Pixels, Is.EqualTo(pixels));
        Assert.That(prior.Text, Is.EqualTo("HELLO"));
        Assert.That(prior.RawBytes, Is.EqualTo("HELLO"u8.ToArray()));
        QrAttemptResult next = imageInput
            ? decoder.TryDecodeDetection(Render(valid), detection, sampling, options)
            : decoder.TryDecodeMatrix(valid.Matrix!, options);
        Assert.That(next.Success, Is.True);
        Assert.That(next.RawBytes, Is.EqualTo(prior.RawBytes));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnsupportedEciSuccessPreservesBytesWithoutInventingText(bool imageInput)
    {
        IQrDecoder decoder = new QrDecoder();
        QrSymbol symbol = BrokenSymbol(QrFailureReason.UnsupportedTextEncoding);
        QrDecodeOptions options = new QrDecodeOptions { PreserveUnsupportedEciBytes = true };
        QrImage image = Render(symbol);
        QrDetection detection = Detection();
        QrDetectionOptions sampling = new QrDetectionOptions { BinarizationMode = QrBinarizationMode.SuppliedBinary };
        QrAttemptResult attempt = imageInput ? decoder.TryDecodeDetection(image, detection, sampling, options)
            : decoder.TryDecodeMatrix(symbol.Matrix!, options);
        QrDecodeResult result = imageInput ? decoder.DecodeDetection(image, detection, sampling, options)
            : decoder.DecodeMatrix(symbol.Matrix!, options);
        Assert.That(attempt.Success, Is.True);
        Assert.That(attempt.FailureReason, Is.EqualTo(QrFailureReason.None));
        Assert.That(attempt.Text, Is.Null);
        Assert.That(result.Text, Is.Null);
        Assert.That(attempt.RawBytes, Is.EqualTo(new byte[] { 65, 0, 255 }).And.EqualTo(result.RawBytes));
        Assert.That(attempt.Result!.EciAssignments, Is.EqualTo(new[] { 899 }).And.EqualTo(result.EciAssignments));
        Assert.That(attempt.Segments.Select(s => s.Mode), Is.EqualTo(result.Segments.Select(s => s.Mode)));
        Assert.That(attempt.Result.Warnings, Is.EqualTo(result.Warnings));
        result.RawBytes[0] = 0;
        Assert.That(attempt.RawBytes[0], Is.EqualTo(65));
    }

    static QrSymbol BrokenSymbol(QrFailureReason reason)
    {
        QrEncodeOptions options = EncodingOptions();
        if (reason == QrFailureReason.UnsupportedTextEncoding)
        {
            options.EciAssignment = 899;
            return new QrEncoder().EncodeBytes([65, 0, 255], options);
        }
        QrSymbol symbol = new QrEncoder().EncodeText("HELLO", options);
        if (reason == QrFailureReason.ChecksumFailure)
        {
            for (int i = 0; i < 20; i++) symbol.Codewords[i] ^= (byte)(i + 0x81);
        }
        else
        {
            // Start with a valid byte segment "A", then fail at the following segment.
            BitBuffer bits = new BitBuffer(152);
            bits.AppendBits(4, 4);
            bits.AppendBits(1, 8);
            bits.AppendBits(65, 8);
            bits.AppendBits(reason == QrFailureReason.InvalidMode ? 6U : 4U, 4);
            if (reason != QrFailureReason.InvalidMode)
            {
                bits.AppendBits(reason == QrFailureReason.InvalidData ? 255U : 1U, 8);
                bits.AppendBits(66, 8);
            }
            while (bits.GetBitLength() < 152) bits.AppendBits(0, 1);
            byte[] data = bits.GetBytes();
            symbol.Codewords = data.Concat(new QrReedSolomonCodec().CreateErrorCorrection(data, 7)).ToArray();
        }
        new QrMatrixProcessor().BuildMatrix(symbol, 0);
        return symbol;
    }

    static QrEncodeOptions EncodingOptions() => new QrEncodeOptions
    {
        ForceSymbolKind = true, SymbolKind = QrSymbolKind.Model2, MaximumVersion = 1,
        ErrorCorrectionLevel = QrErrorCorrectionLevel.Low, ForceErrorCorrectionLevel = true,
        BoostErrorCorrectionLevel = false, QuietZoneModules = 0, MaskPattern = QrMaskPattern.Zero
    };

    static QrImage Render(QrSymbol symbol)
    {
        QrRaster raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions { Scale = 4, QuietZoneModules = 4 });
        return new QrImage { Width = raster.Width, Height = raster.Height, Stride = raster.Stride, Pixels = raster.Pixels };
    }

    static QrDetection Detection() => new QrDetection
    {
        CandidateIndex = 17, EstimatedWidth = 21, EstimatedHeight = 21, Polarity = QrPolarity.DarkOnLight,
        Corners = new QrQuadrilateral
        {
            TopLeft = new QrPoint { X = 16, Y = 16 }, TopRight = new QrPoint { X = 100, Y = 16 },
            BottomRight = new QrPoint { X = 100, Y = 100 }, BottomLeft = new QrPoint { X = 16, Y = 100 }
        }
    };
}
