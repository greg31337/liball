namespace barcode.Tests;

public class ItfScanlineDecoderTests
{
    const string Itf14DataDigits = "0950110153001";
    const string Itf14CompleteDigits = "09501101530010";

    [Test]
    public void ForwardScanlineRecoversInterleavedPairsGeometryAndMetadata()
    {
        BitRow row = CreateItfScanline("1234", null, 12, 14, 2);

        ItfScanlineDecodedData decoded = ItfScanlineDecoder.DecodeRow(
            row,
            BarcodeFormat.Itf,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Format, Is.EqualTo(BarcodeFormat.Itf));
            Assert.That(decoded.Text, Is.EqualTo("1234"));
            Assert.That(decoded.DataText, Is.EqualTo("1234"));
            Assert.That(decoded.EncodedText, Is.EqualTo("1234"));
            Assert.That(decoded.Digits, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
            Assert.That(decoded.BarDigitPatternBits, Is.EqualTo(new int[]
            {
                ItfPatterns.GetDigitPatternBits(1),
                ItfPatterns.GetDigitPatternBits(3)
            }));
            Assert.That(decoded.SpaceDigitPatternBits, Is.EqualTo(new int[]
            {
                ItfPatterns.GetDigitPatternBits(2),
                ItfPatterns.GetDigitPatternBits(4)
            }));
            Assert.That(decoded.PairCount, Is.EqualTo(2));
            Assert.That(decoded.StartPosition, Is.EqualTo(24));
            Assert.That(decoded.EndPosition, Is.EqualTo(114));
            Assert.That(decoded.NarrowWidth, Is.EqualTo(2.0));
            Assert.That(decoded.WideToNarrowRatio, Is.EqualTo(3.0));
            Assert.That(decoded.AverageVariance, Is.EqualTo(0.0));
            Assert.That(decoded.IsReversed, Is.False);
            Assert.That(decoded.HasChecksum, Is.False);
            Assert.That(decoded.SymbologyIdentifier, Is.EqualTo("]I0"));
            Assert.That(decoded.Result.Format, Is.EqualTo(BarcodeFormat.Itf));
            Assert.That(decoded.Result.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(decoded.Result.Payload!.Text, Is.EqualTo("1234"));
            Assert.That(decoded.Result.Payload.RawBytes, Is.EqualTo(new byte[] { 49, 50, 51, 52 }));
            Assert.That(decoded.Result.Metadata!.Codewords, Is.EqualTo(new int[] { 1, 2, 3, 4 }));
            Assert.That(decoded.Result.Metadata.HasChecksum, Is.False);
            Assert.That(decoded.Result.Metadata.SymbologyIdentifier, Is.EqualTo("]I0"));
            Assert.That(decoded.Result.Metadata.Confidence, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void ReversedScanlineRecoversPayloadAndOriginalCoordinates()
    {
        BitRow row = CreateItfScanline("1234", null, 12, 14, 2);
        row.ReverseBits();

        ItfScanlineDecodedData decoded = ItfScanlineDecoder.DecodeRow(
            row,
            BarcodeFormat.Itf,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo("1234"));
            Assert.That(decoded.IsReversed, Is.True);
            Assert.That(decoded.StartPosition, Is.EqualTo(28));
            Assert.That(decoded.EndPosition, Is.EqualTo(118));
            Assert.That(decoded.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise180));
            Assert.That(decoded.Result.OrientationDegrees, Is.EqualTo(180.0));
        });
    }

    [Test]
    public void AdaptivePairClassifierAcceptsTwoToOneRatioAndBoundedRunNoise()
    {
        ItfOptions ratioOptions = new ItfOptions();
        ratioOptions.WideToNarrowRatio = 2.0;
        BitRow ratioRow = CreateItfScanline("1234", ratioOptions, 10, 10, 3);
        BitRow noisyRow = CreateNoisyItfScanline("1234");

        ItfScanlineDecodedData ratio = ItfScanlineDecoder.DecodeRow(
            ratioRow,
            BarcodeFormat.Itf,
            null);
        ItfScanlineDecodedData noisy = ItfScanlineDecoder.DecodeRow(
            noisyRow,
            BarcodeFormat.Itf,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(ratio.Text, Is.EqualTo("1234"));
            Assert.That(ratio.NarrowWidth, Is.EqualTo(3.0));
            Assert.That(ratio.WideToNarrowRatio, Is.EqualTo(2.0));
            Assert.That(noisy.Text, Is.EqualTo("1234"));
            Assert.That(noisy.NarrowWidth, Is.InRange(3.5, 4.5));
            Assert.That(noisy.WideToNarrowRatio, Is.InRange(1.8, 2.2));
            Assert.That(noisy.AverageVariance, Is.GreaterThan(0.0));
            Assert.That(noisy.AverageVariance, Is.LessThan(ItfScanlineDecoder.MaximumAverageVariance));
        });
    }

    [Test]
    public void ChecksumModesValidateAndReportKeptOrStrippedPayloads()
    {
        ItfOptions calculate = new ItfOptions();
        calculate.ChecksumMode = BarcodeChecksumMode.Calculate;
        BitRow row = CreateItfScanline("12345", calculate, 10, 10, 1);
        ItfOptions keep = new ItfOptions();
        keep.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        ItfOptions strip = new ItfOptions();
        strip.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        ItfOptions require = new ItfOptions();
        require.ChecksumMode = BarcodeChecksumMode.Require;

        ItfScanlineDecodedData kept = ItfScanlineDecoder.DecodeRow(row, BarcodeFormat.Itf, keep);
        ItfScanlineDecodedData stripped = ItfScanlineDecoder.DecodeRow(row, BarcodeFormat.Itf, strip);
        ItfScanlineDecodedData required = ItfScanlineDecoder.DecodeRow(row, BarcodeFormat.Itf, require);

        Assert.Multiple(() =>
        {
            Assert.That(kept.EncodedText, Is.EqualTo("123457"));
            Assert.That(kept.DataText, Is.EqualTo("12345"));
            Assert.That(kept.Text, Is.EqualTo("123457"));
            Assert.That(kept.ChecksumValue, Is.EqualTo(7));
            Assert.That(kept.HasChecksum, Is.True);
            Assert.That(kept.SymbologyIdentifier, Is.EqualTo("]I1"));
            Assert.That(kept.Result.Metadata!.ChecksumValid, Is.True);
            Assert.That(kept.Result.Metadata.ChecksumValue, Is.EqualTo(7));
            Assert.That(stripped.Text, Is.EqualTo("12345"));
            Assert.That(stripped.EncodedText, Is.EqualTo("123457"));
            Assert.That(stripped.SymbologyIdentifier, Is.EqualTo("]I3"));
            Assert.That(required.Text, Is.EqualTo("123457"));
            Assert.That(required.SymbologyIdentifier, Is.EqualTo("]I1"));
        });
    }

    [Test]
    public void WrongOrMissingRequiredChecksumIsRejected()
    {
        ItfOptions required = new ItfOptions();
        required.ChecksumMode = BarcodeChecksumMode.Require;
        BitRow wrong = CreateItfScanline("123458", null, 10, 10, 1);
        BitRow missing = CreateItfScanline("12", null, 10, 10, 1);

        Assert.Multiple(() =>
        {
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(wrong, BarcodeFormat.Itf, required),
                Is.Null);
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(missing, BarcodeFormat.Itf, required),
                Is.Null);
        });
    }

    [Test]
    public void AllowedLengthsRejectACompleteButUnselectedCarrier()
    {
        BitRow row = CreateItfScanline("1234", null, 10, 10, 1);
        ItfOptions selected = new ItfOptions();
        selected.AllowedLengths = new int[] { 4, 8 };
        ItfOptions rejected = new ItfOptions();
        rejected.AllowedLengths = new int[] { 6, 8 };

        Assert.Multiple(() =>
        {
            Assert.That(
                ItfScanlineDecoder.DecodeRow(row, BarcodeFormat.Itf, selected).Text,
                Is.EqualTo("1234"));
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(row, BarcodeFormat.Itf, rejected),
                Is.Null);
        });
    }

    [Test]
    public void Itf14EnforcesExactLengthMandatoryCheckAndGs1Metadata()
    {
        BitRow row = CreateItf14Scanline(Itf14CompleteDigits, false);
        ItfScanlineDecodedData decoded = ItfScanlineDecoder.DecodeRow(
            row,
            BarcodeFormat.Itf14,
            null);
        string shortComplete = Gs1Modulo10.AppendCheckDigit("12345678901");
        BitRow shortRow = CreateItfScanline(shortComplete, null, 10, 10, 2);
        BitRow wrongCheck = CreateItfScanline("09501101530011", null, 10, 10, 2);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo(Itf14CompleteDigits));
            Assert.That(decoded.DataText, Is.EqualTo(Itf14DataDigits));
            Assert.That(decoded.Format, Is.EqualTo(BarcodeFormat.Itf14));
            Assert.That(decoded.PairCount, Is.EqualTo(7));
            Assert.That(decoded.HasChecksum, Is.True);
            Assert.That(decoded.ChecksumValue, Is.EqualTo(0));
            Assert.That(decoded.SymbologyIdentifier, Is.EqualTo("]I1"));
            Assert.That(decoded.Result.Payload!.IsGs1, Is.True);
            Assert.That(decoded.Result.Metadata!.IsGs1, Is.True);
            Assert.That(decoded.Result.Metadata.ChecksumValid, Is.True);
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(shortRow, BarcodeFormat.Itf14, null),
                Is.Null);
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(wrongCheck, BarcodeFormat.Itf14, null),
                Is.Null);
        });
    }

    [Test]
    public void BearerBoxBoundariesOutsideQuietZonesDoNotHideItf14Carrier()
    {
        BitRow row = CreateItf14Scanline(Itf14CompleteDigits, true);

        ItfScanlineDecodedData decoded = ItfScanlineDecoder.DecodeRow(
            row,
            BarcodeFormat.Itf14,
            null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo(Itf14CompleteDigits));
            Assert.That(decoded.LeadingBearerBoundaryObserved, Is.True);
            Assert.That(decoded.TrailingBearerBoundaryObserved, Is.True);
            Assert.That(decoded.StartPosition, Is.EqualTo(28));
            Assert.That(decoded.EndPosition, Is.EqualTo(269));
            Assert.That(decoded.NarrowWidth, Is.EqualTo(2.0));
            Assert.That(decoded.WideToNarrowRatio, Is.EqualTo(2.5));
        });
    }

    [Test]
    public void DecoderRequiresStartStopAndAdjacentQuietZones()
    {
        BitRow shortLeftQuiet = CreateItfScanline("1234", null, 9, 10, 1);
        BitRow shortRightQuiet = CreateItfScanline("1234", null, 10, 9, 1);
        int[] badStartRuns = CreateItfRuns("1234", null, 10, 10, 1);
        badStartRuns[1] = 3;
        int[] badStopRuns = CreateItfRuns("1234", null, 10, 10, 1);
        badStopRuns[badStopRuns.Length - 4] = 1;
        BitRow solidBearer = RunLength.ExpandRuns(new int[] { 50 }, true);

        Assert.Multiple(() =>
        {
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(shortLeftQuiet, BarcodeFormat.Itf, null),
                Is.Null);
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(shortRightQuiet, BarcodeFormat.Itf, null),
                Is.Null);
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(
                    RunLength.ExpandRuns(badStartRuns, false),
                    BarcodeFormat.Itf,
                    null),
                Is.Null);
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(
                    RunLength.ExpandRuns(badStopRuns, false),
                    BarcodeFormat.Itf,
                    null),
                Is.Null);
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(solidBearer, BarcodeFormat.Itf14, null),
                Is.Null);
        });
    }

    [Test]
    public void DecoderRejectsInvalidInputFormatAndProfileOptions()
    {
        BitRow empty = new BitRow(40);
        ItfOptions badChecksum = new ItfOptions();
        badChecksum.ChecksumMode = (BarcodeChecksumMode)99;
        ItfOptions badRatio = new ItfOptions();
        badRatio.WideToNarrowRatio = 3.1;
        ItfOptions badItf14Ratio = new ItfOptions();
        badItf14Ratio.WideToNarrowRatio = 2.2;
        ItfOptions badLengths = new ItfOptions();
        badLengths.AllowedLengths = new int[] { 3 };
        ItfOptions weakenedItf14Lengths = new ItfOptions();
        weakenedItf14Lengths.AllowedLengths = new int[] { 12, 14 };
        ItfOptions stripItf14Check = new ItfOptions();
        stripItf14Check.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        ItfOptions negativeBearer = new ItfOptions();
        negativeBearer.BearerBarThicknessModules = -1;

        Assert.Multiple(() =>
        {
            Assert.That(
                ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Itf, null),
                Is.Null);
            Assert.That(
                () => ItfScanlineDecoder.DecodeRow(empty, BarcodeFormat.Itf, null),
                Throws.TypeOf<BarcodeDecodingException>()
                    .With.Message.EqualTo("The scanline does not contain a valid ITF symbol."));
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(null!, BarcodeFormat.Itf, null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Code39, null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Itf, badChecksum),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Itf, badRatio),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Itf14, badItf14Ratio),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Itf, badLengths),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Itf14, weakenedItf14Lengths),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Itf14, stripItf14Check),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => ItfScanlineDecoder.TryDecodeRow(empty, BarcodeFormat.Itf14, negativeBearer),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BitRow CreateItfScanline(
        string text,
        ItfOptions? layoutOptions,
        int leftQuietModules,
        int rightQuietModules,
        int scale)
    {
        return RunLength.ExpandRuns(
            CreateItfRuns(text, layoutOptions, leftQuietModules, rightQuietModules, scale),
            false);
    }

    static int[] CreateItfRuns(
        string text,
        ItfOptions? layoutOptions,
        int leftQuietModules,
        int rightQuietModules,
        int scale)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        ItfLayoutResult layout = ItfLayout.CreateLayout(text, payload, layoutOptions, null);
        int[] runs = new int[layout.ElementWidths.Length + 2];
        runs[0] = leftQuietModules * scale;
        int index = 0;
        while (index < layout.ElementWidths.Length)
        {
            runs[index + 1] = (int)(layout.ElementWidths[index] * scale);
            index = index + 1;
        }

        runs[runs.Length - 1] = rightQuietModules * scale;
        return runs;
    }

    static BitRow CreateItf14Scanline(string text, bool includeBearerBoundaries)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        Itf14LayoutResult layout = Itf14Profile.CreateLayout(text, payload, null, null);
        int extraRuns = includeBearerBoundaries ? 4 : 2;
        int[] runs = new int[layout.ElementWidths.Length + extraRuns];
        int target = 0;
        if (includeBearerBoundaries)
        {
            runs[target] = 8;
            target = target + 1;
        }

        runs[target] = 20;
        target = target + 1;
        int index = 0;
        while (index < layout.ElementWidths.Length)
        {
            runs[target] = (int)(layout.ElementWidths[index] * 2.0);
            target = target + 1;
            index = index + 1;
        }

        runs[target] = 20;
        target = target + 1;
        if (includeBearerBoundaries)
        {
            runs[target] = 8;
        }

        return RunLength.ExpandRuns(runs, includeBearerBoundaries);
    }

    static BitRow CreateNoisyItfScanline(string text)
    {
        ItfOptions options = new ItfOptions();
        options.WideToNarrowRatio = 2.0;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        ItfLayoutResult layout = ItfLayout.CreateLayout(text, payload, options, null);
        int[] runs = new int[layout.ElementWidths.Length + 2];
        runs[0] = 50;
        int narrowIndex = 0;
        int wideIndex = 0;
        int index = 0;
        while (index < layout.ElementWidths.Length)
        {
            if (layout.ElementWidths[index] == 2.0)
            {
                runs[index + 1] = 7 + (wideIndex % 3);
                wideIndex = wideIndex + 1;
            }
            else
            {
                runs[index + 1] = 3 + (narrowIndex % 3);
                narrowIndex = narrowIndex + 1;
            }

            index = index + 1;
        }

        runs[runs.Length - 1] = 50;
        return RunLength.ExpandRuns(runs, false);
    }
}
