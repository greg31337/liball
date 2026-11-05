namespace barcode.Tests;

public class CodabarScanlineDecoderTests
{
    [Test]
    public void ForwardScanlineRecoversDataTerminalPairPatternsAndMetadata()
    {
        BitRow row = CreateScanline("B12D", null, 12, 14, 2);

        CodabarScanlineDecodedData decoded = CodabarScanlineDecoder.DecodeRow(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo("12"));
            Assert.That(decoded.DataText, Is.EqualTo("12"));
            Assert.That(decoded.CarrierDataText, Is.EqualTo("12"));
            Assert.That(decoded.EncodedText, Is.EqualTo("B12D"));
            Assert.That(decoded.StartCharacter, Is.EqualTo('B'));
            Assert.That(decoded.StopCharacter, Is.EqualTo('D'));
            Assert.That(decoded.Values, Is.EqualTo(new int[] { 17, 1, 2, 19 }));
            Assert.That(decoded.PatternBits, Is.EqualTo(new int[] { 0x29, 0x06, 0x09, 0x0E }));
            Assert.That(decoded.HasChecksum, Is.False);
            Assert.That(decoded.CheckCharacter, Is.EqualTo('\0'));
            Assert.That(decoded.SymbologyIdentifier, Is.EqualTo("]F0"));
            Assert.That(decoded.IsReversed, Is.False);
            Assert.That(decoded.StartPosition, Is.EqualTo(24));
            Assert.That(decoded.EndPosition, Is.EqualTo(106));
            Assert.That(decoded.NarrowWidth, Is.EqualTo(2.0));
            Assert.That(decoded.WideToNarrowRatio, Is.EqualTo(2.0));
            Assert.That(decoded.IntercharacterGapModules, Is.EqualTo(1.0));
            Assert.That(decoded.AverageVariance, Is.EqualTo(0.0));
            Assert.That(decoded.Result.Format, Is.EqualTo(BarcodeFormat.Codabar));
            Assert.That(decoded.Result.Dimension, Is.EqualTo(BarcodeDimension.Linear));
            Assert.That(decoded.Result.Payload!.Text, Is.EqualTo("12"));
            Assert.That(decoded.Result.Payload.RawBytes, Is.EqualTo(new byte[] { 49, 50 }));
            Assert.That(decoded.Result.Metadata!.Codewords, Is.EqualTo(new int[] { 17, 1, 2, 19 }));
            Assert.That(decoded.Result.Metadata.HasChecksum, Is.False);
            Assert.That(decoded.Result.Metadata.SymbologyIdentifier, Is.EqualTo("]F0"));
            Assert.That(decoded.Result.Metadata.Confidence, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void ReversedScanlineRecoversPayloadAndOriginalCoordinates()
    {
        BitRow row = CreateScanline("B12D", null, 12, 14, 2);
        row.ReverseBits();

        CodabarScanlineDecodedData decoded = CodabarScanlineDecoder.DecodeRow(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo("12"));
            Assert.That(decoded.StartCharacter, Is.EqualTo('B'));
            Assert.That(decoded.StopCharacter, Is.EqualTo('D'));
            Assert.That(decoded.IsReversed, Is.True);
            Assert.That(decoded.StartPosition, Is.EqualTo(28));
            Assert.That(decoded.EndPosition, Is.EqualTo(110));
            Assert.That(decoded.Result.Rotation, Is.EqualTo(BarcodeRotation.Clockwise180));
            Assert.That(decoded.Result.OrientationDegrees, Is.EqualTo(180.0));
        });
    }

    [Test]
    public void AdaptiveBarSpaceThresholdsAcceptThreeToOneRatioAndRunNoise()
    {
        CodabarOptions ratioOptions = new CodabarOptions();
        ratioOptions.WideToNarrowRatio = 3.0;
        BitRow ratioRow = CreateScanline("B12D", ratioOptions, 10, 10, 2);
        BitRow noisyRow = CreateNoisyScanline("B12D");

        CodabarScanlineDecodedData ratio = CodabarScanlineDecoder.DecodeRow(ratioRow, null);
        CodabarScanlineDecodedData noisy = CodabarScanlineDecoder.DecodeRow(noisyRow, null);

        Assert.Multiple(() =>
        {
            Assert.That(ratio.Text, Is.EqualTo("12"));
            Assert.That(ratio.NarrowWidth, Is.EqualTo(2.0));
            Assert.That(ratio.WideToNarrowRatio, Is.EqualTo(3.0));
            Assert.That(noisy.Text, Is.EqualTo("12"));
            Assert.That(noisy.NarrowWidth, Is.InRange(3.5, 4.5));
            Assert.That(noisy.WideToNarrowRatio, Is.InRange(1.8, 2.2));
            Assert.That(noisy.AverageVariance, Is.GreaterThan(0.0));
            Assert.That(noisy.AverageVariance, Is.LessThan(CodabarScanlineDecoder.MaximumAverageVariance));
        });
    }

    [Test]
    public void DecoderRequiresQuietZonesAndConfiguredIntercharacterGap()
    {
        CodabarOptions gapOptions = new CodabarOptions();
        gapOptions.IntercharacterGapModules = 3;
        BitRow wideGap = CreateScanline("B12D", gapOptions, 10, 10, 1);

        Assert.Multiple(() =>
        {
            Assert.That(
                CodabarScanlineDecoder.TryDecodeRow(CreateScanline("B12D", null, 9, 10, 1), null),
                Is.Null);
            Assert.That(
                CodabarScanlineDecoder.TryDecodeRow(CreateScanline("B12D", null, 10, 9, 1), null),
                Is.Null);
            Assert.That(CodabarScanlineDecoder.TryDecodeRow(wideGap, null), Is.Null);
            Assert.That(
                CodabarScanlineDecoder.DecodeRow(wideGap, gapOptions).IntercharacterGapModules,
                Is.EqualTo(3.0));
        });
    }

    [Test]
    public void DecoderAcceptsArbitraryCanonicalTerminalPairsAndMinimumDataLength()
    {
        BitRow oneData = CreateScanline("A1D", null, 10, 10, 1);
        BitRow noData = CreatePatternScanline(new char[] { 'A', 'D' });
        BitRow wrongStart = CreatePatternScanline(new char[] { '1', '2', 'D' });
        BitRow missingStop = CreatePatternScanline(new char[] { 'A', '1', '2' });

        Assert.Multiple(() =>
        {
            Assert.That(CodabarScanlineDecoder.DecodeRow(oneData, null).Text, Is.EqualTo("1"));
            Assert.That(CodabarScanlineDecoder.TryDecodeRow(noData, null), Is.Null);
            Assert.That(CodabarScanlineDecoder.TryDecodeRow(wrongStart, null), Is.Null);
            Assert.That(CodabarScanlineDecoder.TryDecodeRow(missingStop, null), Is.Null);
        });
    }

    [Test]
    public void DecoderRecoversEveryDataPatternAndCanonicalTerminalPair()
    {
        string completeAlphabet = "A" + CodabarPatterns.DataCharacters + "D";
        CodabarScanlineDecodedData alphabet = CodabarScanlineDecoder.DecodeRow(
            CreateScanline(completeAlphabet, null, 10, 10, 1),
            null);

        Assert.That(alphabet.DataText, Is.EqualTo(CodabarPatterns.DataCharacters));
        int startIndex = 0;
        while (startIndex < CodabarPatterns.StartStopCharacters.Length)
        {
            int stopIndex = 0;
            while (stopIndex < CodabarPatterns.StartStopCharacters.Length)
            {
                char start = CodabarPatterns.StartStopCharacters[startIndex];
                char stop = CodabarPatterns.StartStopCharacters[stopIndex];
                string framed = start + "1" + stop;
                CodabarScanlineDecodedData decoded = CodabarScanlineDecoder.DecodeRow(
                    CreateScanline(framed, null, 10, 10, 1),
                    null);

                Assert.Multiple(() =>
                {
                    Assert.That(decoded.StartCharacter, Is.EqualTo(start));
                    Assert.That(decoded.StopCharacter, Is.EqualTo(stop));
                    Assert.That(decoded.Text, Is.EqualTo("1"));
                });
                stopIndex++;
            }

            startIndex++;
        }
    }

    [Test]
    public void TerminalFiltersSupportCanonicalCharactersAndEnabledAliases()
    {
        BitRow canonicalRow = CreateScanline("B12D", null, 10, 10, 1);
        CodabarOptions matching = new CodabarOptions();
        matching.StartCharacter = "B";
        matching.StopCharacter = "D";
        CodabarOptions mismatch = new CodabarOptions();
        mismatch.StartCharacter = "A";
        mismatch.StopCharacter = "D";
        BitRow aliasRow = CreateScanline("A12D", null, 10, 10, 1);
        CodabarOptions aliases = new CodabarOptions();
        aliases.StartCharacter = "T";
        aliases.StopCharacter = "E";
        aliases.AcceptAliases = true;

        Assert.Multiple(() =>
        {
            Assert.That(CodabarScanlineDecoder.DecodeRow(canonicalRow, matching).Text, Is.EqualTo("12"));
            Assert.That(CodabarScanlineDecoder.TryDecodeRow(canonicalRow, mismatch), Is.Null);
            Assert.That(CodabarScanlineDecoder.DecodeRow(aliasRow, aliases).EncodedText, Is.EqualTo("A12D"));
        });
    }

    [Test]
    public void ChecksumModesValidateAndReportKeptOrStrippedPayloads()
    {
        CodabarOptions calculate = new CodabarOptions();
        calculate.ChecksumMode = BarcodeChecksumMode.Calculate;
        BitRow row = CreateScanline("A1234B", calculate, 10, 10, 1);
        CodabarOptions keep = new CodabarOptions();
        keep.ChecksumMode = BarcodeChecksumMode.VerifyKeep;
        CodabarOptions strip = new CodabarOptions();
        strip.ChecksumMode = BarcodeChecksumMode.VerifyStrip;
        strip.TransmitCheckCharacter = true;
        CodabarOptions required = new CodabarOptions();
        required.ChecksumMode = BarcodeChecksumMode.Require;
        required.TransmitCheckCharacter = true;
        required.TransmitStartStop = true;
        CodabarOptions calculateDecode = new CodabarOptions();
        calculateDecode.ChecksumMode = BarcodeChecksumMode.Calculate;

        CodabarScanlineDecodedData kept = CodabarScanlineDecoder.DecodeRow(row, keep);
        CodabarScanlineDecodedData stripped = CodabarScanlineDecoder.DecodeRow(row, strip);
        CodabarScanlineDecodedData requiredResult = CodabarScanlineDecoder.DecodeRow(row, required);
        CodabarScanlineDecodedData calculated = CodabarScanlineDecoder.DecodeRow(row, calculateDecode);

        Assert.Multiple(() =>
        {
            Assert.That(kept.EncodedText, Is.EqualTo("A12345B"));
            Assert.That(kept.CarrierDataText, Is.EqualTo("12345"));
            Assert.That(kept.DataText, Is.EqualTo("1234"));
            Assert.That(kept.Text, Is.EqualTo("12345"));
            Assert.That(kept.CheckCharacter, Is.EqualTo('5'));
            Assert.That(kept.ChecksumValue, Is.EqualTo(5));
            Assert.That(kept.HasChecksum, Is.True);
            Assert.That(kept.Result.Metadata!.HasChecksum, Is.True);
            Assert.That(kept.Result.Metadata.ChecksumValid, Is.True);
            Assert.That(kept.Result.Metadata.ChecksumValue, Is.EqualTo(5));
            Assert.That(stripped.Text, Is.EqualTo("1234"));
            Assert.That(stripped.EncodedText, Is.EqualTo("A12345B"));
            Assert.That(requiredResult.Text, Is.EqualTo("A12345B"));
            Assert.That(calculated.Text, Is.EqualTo("1234"));
            Assert.That(calculated.HasChecksum, Is.True);
        });
    }

    [Test]
    public void WrongOrMissingRequiredChecksumIsRejected()
    {
        CodabarOptions required = new CodabarOptions();
        required.ChecksumMode = BarcodeChecksumMode.Require;
        BitRow wrong = CreateScanline("A12346B", null, 10, 10, 1);
        BitRow missing = CreateScanline("A1B", null, 10, 10, 1);

        Assert.Multiple(() =>
        {
            Assert.That(CodabarScanlineDecoder.TryDecodeRow(wrong, required), Is.Null);
            Assert.That(CodabarScanlineDecoder.TryDecodeRow(missing, required), Is.Null);
        });
    }

    [Test]
    public void DecoderSkipsFalseStartBeforeValidSymbol()
    {
        BitRow symbol = CreateScanline("B12D", null, 10, 10, 1);
        BitRow row = new BitRow(symbol.Length + 20);
        row.SetBit(1, true);
        int index = 0;
        while (index < symbol.Length)
        {
            if (symbol.GetBit(index))
            {
                row.SetBit(index + 20, true);
            }

            index++;
        }

        CodabarScanlineDecodedData decoded = CodabarScanlineDecoder.DecodeRow(row, null);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Text, Is.EqualTo("12"));
            Assert.That(decoded.StartPosition, Is.EqualTo(30));
            Assert.That(decoded.EndPosition, Is.EqualTo(71));
        });
    }

    [Test]
    public void DecoderRejectsInvalidInputAndOptions()
    {
        BitRow empty = new BitRow(40);
        CodabarOptions badChecksum = new CodabarOptions();
        badChecksum.ChecksumMode = (BarcodeChecksumMode)99;
        CodabarOptions badGap = new CodabarOptions();
        badGap.IntercharacterGapModules = -1;
        CodabarOptions badRatio = new CodabarOptions();
        badRatio.WideToNarrowRatio = 3.1;
        CodabarOptions badStartLength = new CodabarOptions();
        badStartLength.StartCharacter = "AB";
        CodabarOptions disabledAlias = new CodabarOptions();
        disabledAlias.StopCharacter = "E";

        Assert.Multiple(() =>
        {
            Assert.That(CodabarScanlineDecoder.TryDecodeRow(empty, null), Is.Null);
            Assert.That(
                () => CodabarScanlineDecoder.DecodeRow(empty, null),
                Throws.TypeOf<BarcodeDecodingException>()
                    .With.Message.EqualTo("The scanline does not contain a valid Codabar symbol."));
            Assert.That(
                () => CodabarScanlineDecoder.TryDecodeRow(null!, null),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CodabarScanlineDecoder.TryDecodeRow(empty, badChecksum),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CodabarScanlineDecoder.TryDecodeRow(empty, badGap),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CodabarScanlineDecoder.TryDecodeRow(empty, badRatio),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CodabarScanlineDecoder.TryDecodeRow(empty, badStartLength),
                Throws.TypeOf<BarcodeValidationException>());
            Assert.That(
                () => CodabarScanlineDecoder.TryDecodeRow(empty, disabledAlias),
                Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static BitRow CreateScanline(
        string text,
        CodabarOptions? layoutOptions,
        int leftQuietModules,
        int rightQuietModules,
        int scale)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        CodabarLayoutResult layout = CodabarLayout.CreateLayout(text, payload, layoutOptions, null);
        int[] runs = new int[layout.ElementWidths.Length + 2];
        runs[0] = leftQuietModules * scale;
        int index = 0;
        while (index < layout.ElementWidths.Length)
        {
            runs[index + 1] = (int)(layout.ElementWidths[index] * scale);
            index++;
        }

        runs[^1] = rightQuietModules * scale;
        return RunLength.ExpandRuns(runs, false);
    }

    static BitRow CreateNoisyScanline(string text)
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        CodabarLayoutResult layout = CodabarLayout.CreateLayout(text, payload, null, null);
        int[] runs = new int[layout.ElementWidths.Length + 2];
        runs[0] = 50;
        int characterElement = 0;
        int narrowIndex = 0;
        int wideIndex = 0;
        int index = 0;
        while (index < layout.ElementWidths.Length)
        {
            bool isGap = characterElement == CodabarPatterns.ElementCount;
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
                wideIndex++;
                characterElement++;
            }
            else
            {
                runs[index + 1] = 3 + (narrowIndex % 3);
                narrowIndex++;
                characterElement++;
            }

            index++;
        }

        runs[^1] = 50;
        return RunLength.ExpandRuns(runs, false);
    }

    static BitRow CreatePatternScanline(char[] characters)
    {
        int elementCount = (characters.Length * CodabarPatterns.ElementCount) + characters.Length - 1;
        int[] runs = new int[elementCount + 2];
        runs[0] = 10;
        int target = 1;
        int characterIndex = 0;
        while (characterIndex < characters.Length)
        {
            int[] widths = CodabarPatterns.GetCanonicalWidths(characters[characterIndex]);
            int widthIndex = 0;
            while (widthIndex < widths.Length)
            {
                runs[target] = widths[widthIndex];
                target++;
                widthIndex++;
            }

            if (characterIndex < characters.Length - 1)
            {
                runs[target] = 1;
                target++;
            }

            characterIndex++;
        }

        runs[^1] = 10;
        return RunLength.ExpandRuns(runs, false);
    }
}
