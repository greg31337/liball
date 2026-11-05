namespace barcode.Tests;

public class Code39ScanlineDecoderTests
{
    [Test]
    public void ForwardScanlineRecoversStandardTextPatternsAndGeometry()
    {
        BitRow row = CreateScanline("ABC", null, 10, 10, 1);

        Code39ScanlineDecodedData decoded = Code39ScanlineDecoder.DecodeRow(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo("ABC"));
            Assert.That(decoded.DataText, Is.EqualTo("ABC"));
            Assert.That(decoded.EncodedText, Is.EqualTo("ABC"));
            Assert.That(decoded.Values, Is.EqualTo(new int[] { 10, 11, 12 }));
            Assert.That(decoded.PatternBits, Has.Length.EqualTo(5));
            Assert.That(decoded.PatternBits[0], Is.EqualTo(Code39Patterns.StartStopPatternBits));
            Assert.That(decoded.PatternBits[^1], Is.EqualTo(Code39Patterns.StartStopPatternBits));
            Assert.That(decoded.IsReversed, Is.False);
            Assert.That(decoded.StartPosition, Is.EqualTo(10));
            Assert.That(decoded.EndPosition, Is.EqualTo(74));
            Assert.That(decoded.NarrowWidth, Is.EqualTo(1.0));
            Assert.That(decoded.WideToNarrowRatio, Is.EqualTo(2.0));
            Assert.That(decoded.IntercharacterGapModules, Is.EqualTo(1.0));
            Assert.That(decoded.AverageVariance, Is.EqualTo(0.0));
            Assert.That(decoded.SymbologyIdentifier, Is.EqualTo("]A0"));
            Assert.That(decoded.Result.Format, Is.EqualTo(BarcodeFormat.Code39));
            Assert.That(decoded.Result.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(decoded.Result.Payload!.Text, Is.EqualTo("ABC"));
            Assert.That(decoded.Result.Payload.RawBytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(decoded.Result.Metadata!.SymbologyIdentifier, Is.EqualTo("]A0"));
            Assert.That(decoded.Result.Metadata.HasChecksum, Is.False);
            Assert.That(decoded.Result.Metadata.Confidence, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void ReversedScanlineRecoversPayloadAndOriginalCoordinates()
    {
        BitRow row = CreateScanline("ABC", null, 12, 14, 2);
        row.ReverseBits();

        Code39ScanlineDecodedData decoded = Code39ScanlineDecoder.DecodeRow(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo("ABC"));
            Assert.That(decoded.IsReversed, Is.True);
            Assert.That(decoded.StartPosition, Is.EqualTo(28));
            Assert.That(decoded.EndPosition, Is.EqualTo(156));
            Assert.That(decoded.NarrowWidth, Is.EqualTo(2.0));
            Assert.That(decoded.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise180));
            Assert.That(decoded.Result.OrientationDegrees, Is.EqualTo(180.0));
        });
    }

    [Test]
    public void AdaptiveClassifierAcceptsThreeToOneRatioAndBoundedRunNoise()
    {
        Code39Options ratioOptions = new Code39Options();
        ratioOptions.WideToNarrowRatio = 3.0;
        BitRow ratioRow = CreateScanline("ABC", ratioOptions, 10, 10, 2);
        BitRow noisyRow = CreateNoisyScanline("ABC");

        Code39ScanlineDecodedData ratio = Code39ScanlineDecoder.DecodeRow(ratioRow, null);
        Code39ScanlineDecodedData noisy = Code39ScanlineDecoder.DecodeRow(noisyRow, null);

        Assert.Multiple(() =>
        {
            Assert.That(ratio.Text, Is.EqualTo("ABC"));
            Assert.That(ratio.NarrowWidth, Is.EqualTo(2.0));
            Assert.That(ratio.WideToNarrowRatio, Is.EqualTo(3.0));
            Assert.That(noisy.Text, Is.EqualTo("ABC"));
            Assert.That(noisy.NarrowWidth, Is.EqualTo(4.0));
            Assert.That(noisy.WideToNarrowRatio, Is.EqualTo(2.0));
            Assert.That(noisy.AverageVariance, Is.GreaterThan(0.0));
            Assert.That(noisy.AverageVariance, Is.LessThan(Code39ScanlineDecoder.MaximumAverageVariance));
        });
    }

    [Test]
    public void DecoderRequiresQuietZonesAndConfiguredIntercharacterGap()
    {
        Code39Options gapOptions = new Code39Options();
        gapOptions.IntercharacterGapModules = 3;
        BitRow wideGap = CreateScanline("ABC", gapOptions, 10, 10, 1);

        Assert.Multiple(() =>
        {
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(CreateScanline("ABC", null, 9, 10, 1), null), Is.Null);
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(CreateScanline("ABC", null, 10, 9, 1), null), Is.Null);
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(wideGap, null), Is.Null);
            Assert.That(Code39ScanlineDecoder.DecodeRow(wideGap, gapOptions).IntercharacterGapModules, Is.EqualTo(3.0));
        });
    }

    [Test]
    public void DecoderRequiresAsteriskFramingAndAssignedDataPatterns()
    {
        BitRow wrongStart = CreatePatternScanline(new char[] { 'A', 'B', '*' });
        BitRow missingStop = CreatePatternScanline(new char[] { '*', 'A', 'B' });
        BitRow damaged = CreateScanline("ABC", null, 10, 10, 2);
        damaged.SetBit(44, !damaged.GetBit(44));

        Assert.Multiple(() =>
        {
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(wrongStart, null), Is.Null);
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(missingStop, null), Is.Null);
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(damaged, null), Is.Null);
        });
    }

    [Test]
    public void ChecksumModesValidateAndReportKeptOrStrippedModifiers()
    {
        Code39Options encoding = new Code39Options();
        encoding.ChecksumMode = BarcodeChecksumMode.Calculate;
        BitRow valid = CreateScanline("CODE39", encoding, 10, 10, 1);
        Code39Options kept = new Code39Options();
        kept.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        Code39Options stripped = new Code39Options();
        stripped.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        Code39Options requiredAndKept = new Code39Options();
        requiredAndKept.ChecksumMode = BarcodeChecksumMode.Require;
        requiredAndKept.TransmitCheckCharacter = true;

        Code39ScanlineDecodedData keepResult = Code39ScanlineDecoder.DecodeRow(valid, kept);
        Code39ScanlineDecodedData stripResult = Code39ScanlineDecoder.DecodeRow(valid, stripped);
        Code39ScanlineDecodedData requiredResult = Code39ScanlineDecoder.DecodeRow(valid, requiredAndKept);

        Assert.Multiple(() =>
        {
            Assert.That(keepResult.Text, Is.EqualTo("CODE39W"));
            Assert.That(keepResult.DataText, Is.EqualTo("CODE39"));
            Assert.That(keepResult.CheckCharacter, Is.EqualTo('W'));
            Assert.That(keepResult.SymbologyIdentifier, Is.EqualTo("]A1"));
            Assert.That(keepResult.Result.Metadata!.HasChecksum, Is.True);
            Assert.That(keepResult.Result.Metadata.ChecksumValid, Is.True);
            Assert.That(keepResult.Result.Metadata.ChecksumValue, Is.EqualTo(32));
            Assert.That(stripResult.Text, Is.EqualTo("CODE39"));
            Assert.That(stripResult.EncodedText, Is.EqualTo("CODE39W"));
            Assert.That(stripResult.DataEncodedText, Is.EqualTo("CODE39"));
            Assert.That(stripResult.SymbologyIdentifier, Is.EqualTo("]A3"));
            Assert.That(requiredResult.Text, Is.EqualTo("CODE39W"));
            Assert.That(requiredResult.SymbologyIdentifier, Is.EqualTo("]A1"));
        });
    }

    [Test]
    public void WrongOrMissingRequiredChecksumIsRejected()
    {
        Code39Options required = new Code39Options();
        required.ChecksumMode = BarcodeChecksumMode.Require;
        BitRow wrong = CreateScanline("CODE39X", null, 10, 10, 1);
        BitRow missing = CreateScanline("A", null, 10, 10, 1);

        Assert.Multiple(() =>
        {
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(wrong, required), Is.Null);
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(missing, required), Is.Null);
        });
    }

    [Test]
    public void FullAsciiProcessingReportsModifiersFourFiveAndSeven()
    {
        Code39Options fullAsciiEncoding = new Code39Options();
        fullAsciiEncoding.CharacterMode = Code39CharacterMode.FullAscii;
        BitRow plain = CreateScanline("a", fullAsciiEncoding, 10, 10, 1);
        Code39Options checkedEncoding = new Code39Options();
        checkedEncoding.CharacterMode = Code39CharacterMode.FullAscii;
        checkedEncoding.ChecksumMode = BarcodeChecksumMode.Calculate;
        BitRow checkedRow = CreateScanline("a", checkedEncoding, 10, 10, 1);
        Code39Options noCheck = new Code39Options();
        noCheck.CharacterMode = Code39CharacterMode.FullAscii;
        Code39Options keep = new Code39Options();
        keep.CharacterMode = Code39CharacterMode.FullAscii;
        keep.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        Code39Options strip = new Code39Options();
        strip.CharacterMode = Code39CharacterMode.FullAscii;
        strip.ChecksumMode = BarcodeChecksumMode.VerifyStrip;

        Code39ScanlineDecodedData plainResult = Code39ScanlineDecoder.DecodeRow(plain, noCheck);
        Code39ScanlineDecodedData keepResult = Code39ScanlineDecoder.DecodeRow(checkedRow, keep);
        Code39ScanlineDecodedData stripResult = Code39ScanlineDecoder.DecodeRow(checkedRow, strip);

        Assert.Multiple(() =>
        {
            Assert.That(plainResult.EncodedText, Is.EqualTo("+A"));
            Assert.That(plainResult.Text, Is.EqualTo("a"));
            Assert.That(plainResult.SymbologyIdentifier, Is.EqualTo("]A4"));
            Assert.That(keepResult.EncodedText, Is.EqualTo("+A8"));
            Assert.That(keepResult.Text, Is.EqualTo("a8"));
            Assert.That(keepResult.SymbologyIdentifier, Is.EqualTo("]A5"));
            Assert.That(stripResult.Text, Is.EqualTo("a"));
            Assert.That(stripResult.SymbologyIdentifier, Is.EqualTo("]A7"));
        });
    }

    [Test]
    public void MalformedFullAsciiCarrierIsRejectedAfterPhysicalDecode()
    {
        BitRow row = CreateScanline("+", null, 10, 10, 1);
        Code39Options fullAscii = new Code39Options();
        fullAscii.CharacterMode = Code39CharacterMode.FullAscii;

        Assert.That(Code39ScanlineDecoder.TryDecodeRow(row, fullAscii), Is.Null);
    }

    [Test]
    public void AppendMetadataPreservesSegmentBoundaries()
    {
        Code39Options append = new Code39Options();
        append.EnableAppend = true;
        BitRow continuationRow = CreateScanline(" PART", null, 10, 10, 1);
        BitRow releaseRow = CreateScanline("FINAL", null, 10, 10, 1);

        Code39ScanlineDecodedData continuation = Code39ScanlineDecoder.DecodeRow(continuationRow, append);
        Code39ScanlineDecodedData release = Code39ScanlineDecoder.DecodeRow(releaseRow, append);

        Assert.Multiple(() =>
        {
            Assert.That(continuation.Text, Is.EqualTo(" PART"));
            Assert.That(continuation.AppendMetadata!.IsContinuation, Is.True);
            Assert.That(continuation.AppendMetadata.ReleasesBufferedData, Is.False);
            Assert.That(continuation.AppendMetadata.SegmentText, Is.EqualTo("PART"));
            Assert.That(release.AppendMetadata!.IsContinuation, Is.False);
            Assert.That(release.AppendMetadata.ReleasesBufferedData, Is.True);
            Assert.That(release.AppendMetadata.SegmentText, Is.EqualTo("FINAL"));
        });
    }

    [Test]
    public void DecoderSkipsFalseStartBeforeValidSymbol()
    {
        BitRow symbol = CreateScanline("ABC", null, 10, 10, 1);
        BitRow row = new BitRow(symbol.Length + 20);
        row.SetBit(1, true);
        int index = 0;
        while (index < symbol.Length)
        {
            if (symbol.GetBit(index))
            {
                row.SetBit(index + 20, true);
            }

            index = index + 1;
        }

        Code39ScanlineDecodedData decoded = Code39ScanlineDecoder.DecodeRow(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo("ABC"));
            Assert.That(decoded.StartPosition, Is.EqualTo(30));
            Assert.That(decoded.EndPosition, Is.EqualTo(94));
        });
    }

    [Test]
    public void DecoderRejectsInvalidInputAndOptions()
    {
        BitRow empty = new BitRow(40);
        Code39Options badMode = new Code39Options();
        badMode.CharacterMode = (Code39CharacterMode)99;
        Code39Options badChecksum = new Code39Options();
        badChecksum.ChecksumMode = (BarcodeChecksumMode)99;
        Code39Options badGap = new Code39Options();
        badGap.IntercharacterGapModules = -1;
        Code39Options badRatio = new Code39Options();
        badRatio.WideToNarrowRatio = 3.1;

        Assert.Multiple(() =>
        {
            Assert.That(Code39ScanlineDecoder.TryDecodeRow(empty, null), Is.Null);
            Assert.That(
                () => Code39ScanlineDecoder.DecodeRow(empty, null),
                Throws.TypeOf<BarcodeDecodingException>()
                    .With.Message.EqualTo("The scanline does not contain a valid Code 39 symbol."));
            Assert.That(() => Code39ScanlineDecoder.TryDecodeRow(null!, null), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39ScanlineDecoder.TryDecodeRow(empty, badMode), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39ScanlineDecoder.TryDecodeRow(empty, badChecksum), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39ScanlineDecoder.TryDecodeRow(empty, badGap), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code39ScanlineDecoder.TryDecodeRow(empty, badRatio), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BitRow CreateScanline(
        string text,
        Code39Options? layoutOptions,
        int leftQuietModules,
        int rightQuietModules,
        int scale)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        Code39LayoutResult layout = Code39Layout.CreateLayout(text, payload, layoutOptions, null);
        int[] runs = new int[layout.ElementWidths.Length + 2];
        runs[0] = leftQuietModules * scale;
        int index = 0;
        while (index < layout.ElementWidths.Length)
        {
            runs[index + 1] = (int)(layout.ElementWidths[index] * scale);
            index = index + 1;
        }

        runs[runs.Length - 1] = rightQuietModules * scale;
        return RunLength.ExpandRuns(runs, false);
    }

    static BitRow CreateNoisyScanline(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        Code39LayoutResult layout = Code39Layout.CreateLayout(text, payload, null, null);
        int[] runs = new int[layout.ElementWidths.Length + 2];
        runs[0] = 40;
        int characterElement = 0;
        int narrowIndex = 0;
        int wideIndex = 0;
        int index = 0;
        while (index < layout.ElementWidths.Length)
        {
            bool isGap = characterElement == Code39Patterns.ElementCount;
            if (isGap)
            {
                runs[index + 1] = 4;
                characterElement = 0;
                narrowIndex = 0;
                wideIndex = 0;
            }
            else if (layout.ElementWidths[index] == 2.0)
            {
                runs[index + 1] = 7 + (wideIndex % 3);
                wideIndex = wideIndex + 1;
                characterElement = characterElement + 1;
            }
            else
            {
                runs[index + 1] = 3 + (narrowIndex % 3);
                narrowIndex = narrowIndex + 1;
                characterElement = characterElement + 1;
            }

            index = index + 1;
        }

        runs[runs.Length - 1] = 40;
        return RunLength.ExpandRuns(runs, false);
    }

    static BitRow CreatePatternScanline(char[] characters)
    {
        int elementCount = (characters.Length * Code39Patterns.ElementCount) + characters.Length - 1;
        int[] runs = new int[elementCount + 2];
        runs[0] = 10;
        int target = 1;
        int characterIndex = 0;
        while (characterIndex < characters.Length)
        {
            int[] widths = Code39Patterns.GetCanonicalWidths(characters[characterIndex]);
            int widthIndex = 0;
            while (widthIndex < widths.Length)
            {
                runs[target] = widths[widthIndex];
                target = target + 1;
                widthIndex = widthIndex + 1;
            }

            if (characterIndex < characters.Length - 1)
            {
                runs[target] = 1;
                target = target + 1;
            }

            characterIndex = characterIndex + 1;
        }

        runs[runs.Length - 1] = 10;
        return RunLength.ExpandRuns(runs, false);
    }
}
