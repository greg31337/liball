namespace barcode.Tests;

public class Code128ScanlineDecoderTests
{
    [Test]
    public void ForwardScanlineRecoversBytesCodewordsAndGeometry()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(new byte[] { 65, 66, 67 }, null);
        BitRow row = CreateScanline(encoded, 10, 10, 1);

        Code128DecodedData decoded = Code128ScanlineDecoder.DecodeRow(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(decoded.Tokens, Is.EqualTo(new int[] { 65, 66, 67 }));
            Assert.That(decoded.Codewords, Is.EqualTo(new int[] { 104, 33, 34, 35, 1, 106 }));
            Assert.That(decoded.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(decoded.ChecksumValue, Is.EqualTo(1));
            Assert.That(decoded.IsReversed, Is.False);
            Assert.That(decoded.StartPosition, Is.EqualTo(10));
            Assert.That(decoded.EndPosition, Is.EqualTo(78));
            Assert.That(decoded.ModuleSize, Is.EqualTo(1.0));
            Assert.That(decoded.AverageVariance, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void ReversedScanlineRecoversTheSameLogicalPayload()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(new byte[] { 49, 50, 51, 52, 53, 54 }, null);
        BitRow row = CreateScanline(encoded, 10, 10, 1);
        row.ReverseBits();

        Code128DecodedData decoded = Code128ScanlineDecoder.DecodeRow(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 49, 50, 51, 52, 53, 54 }));
            Assert.That(decoded.Tokens, Is.EqualTo(new int[] { 49, 50, 51, 52, 53, 54 }));
            Assert.That(decoded.Codewords, Is.EqualTo(new int[] { 105, 12, 34, 56, 44, 106 }));
            Assert.That(decoded.StartCodeSet, Is.EqualTo(Code128CodeSet.C));
            Assert.That(decoded.IsReversed, Is.True);
            Assert.That(decoded.StartPosition, Is.EqualTo(10));
            Assert.That(decoded.EndPosition, Is.EqualTo(78));
        });
    }

    [Test]
    public void DecoderAcceptsIntegralScalingAndBoundedRunNoise()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(new byte[] { 65, 66, 67 }, null);
        BitRow scaled = CreateScanline(encoded, 10, 10, 3);
        BitRow noisy = CreateNoisyScaledScanline(encoded);

        Code128DecodedData scaledResult = Code128ScanlineDecoder.DecodeRow(scaled);
        Code128DecodedData noisyResult = Code128ScanlineDecoder.DecodeRow(noisy);

        Assert.Multiple(() =>
        {
            Assert.That(scaledResult.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(scaledResult.ModuleSize, Is.EqualTo(3.0));
            Assert.That(scaledResult.StartPosition, Is.EqualTo(30));
            Assert.That(scaledResult.EndPosition, Is.EqualTo(234));
            Assert.That(noisyResult.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(noisyResult.AverageVariance, Is.GreaterThan(0.0));
            Assert.That(noisyResult.AverageVariance, Is.LessThan(Code128ScanlineDecoder.MaximumAverageVariance));
        });
    }

    [Test]
    public void DecoderPreservesFunctionTokensAndLatin1UpperShift()
    {
        Code128Options functionOptions = new Code128Options();
        functionOptions.AllowFnc1 = true;
        functionOptions.AllowFnc2 = true;
        functionOptions.AllowFnc3 = true;
        Code128EncodedData functionEncoded = Code128Encoder.EncodeTokens(
            new int[] { Code128Planner.Function1Token, '1', '2' },
            functionOptions);
        Code128EncodedData auxiliaryFunctionsEncoded = Code128Encoder.EncodeTokens(
            new int[] { Code128Planner.Function2Token, 'A', Code128Planner.Function3Token },
            functionOptions);
        Code128Options latin1Options = new Code128Options();
        latin1Options.CodeSet = Code128CodeSet.B;
        latin1Options.AllowFnc4 = true;
        latin1Options.UseLatin1UpperShift = true;
        Code128EncodedData latin1Encoded = Code128Encoder.EncodeBytes(
            new byte[] { 193, 194, 195 },
            latin1Options);
        Code128EncodedData singleLatin1Encoded = Code128Encoder.EncodeBytes(
            new byte[] { 193 },
            latin1Options);

        Code128DecodedData functionDecoded = Code128ScanlineDecoder.DecodeRow(
            CreateScanline(functionEncoded, 10, 10, 1));
        Code128DecodedData auxiliaryFunctionsDecoded = Code128ScanlineDecoder.DecodeRow(
            CreateScanline(auxiliaryFunctionsEncoded, 10, 10, 1));
        Code128DecodedData latin1Decoded = Code128ScanlineDecoder.DecodeRow(
            CreateScanline(latin1Encoded, 10, 10, 1));
        Code128DecodedData singleLatin1Decoded = Code128ScanlineDecoder.DecodeRow(
            CreateScanline(singleLatin1Encoded, 10, 10, 1));

        Assert.Multiple(() =>
        {
            Assert.That(functionDecoded.Bytes, Is.EqualTo(new byte[] { 49, 50 }));
            Assert.That(functionDecoded.Tokens, Is.EqualTo(new int[] { Code128Planner.Function1Token, 49, 50 }));
            Assert.That(auxiliaryFunctionsDecoded.Bytes, Is.EqualTo(new byte[] { 65 }));
            Assert.That(auxiliaryFunctionsDecoded.Tokens, Is.EqualTo(new int[]
            {
                Code128Planner.Function2Token,
                65,
                Code128Planner.Function3Token
            }));
            Assert.That(latin1Decoded.Bytes, Is.EqualTo(new byte[] { 193, 194, 195 }));
            Assert.That(latin1Decoded.Tokens, Is.EqualTo(new int[]
            {
                Code128Planner.Function4Token,
                Code128Planner.Function4Token,
                193,
                194,
                195
            }));
            Assert.That(singleLatin1Decoded.Bytes, Is.EqualTo(new byte[] { 193 }));
        });
    }

    [Test]
    public void DecoderAppliesShiftAndCodeSetSwitches()
    {
        byte[] shiftedBytes = new byte[] { 97, 1, 98 };
        byte[] switchedBytes = new byte[] { 65, 49, 50, 51, 52, 53, 54, 66 };
        Code128EncodedData shifted = Code128Encoder.EncodeBytes(shiftedBytes, null);
        Code128EncodedData switched = Code128Encoder.EncodeBytes(switchedBytes, null);

        Code128DecodedData shiftedResult = Code128ScanlineDecoder.DecodeRow(CreateScanline(shifted, 10, 10, 1));
        Code128DecodedData switchedResult = Code128ScanlineDecoder.DecodeRow(CreateScanline(switched, 10, 10, 1));

        Assert.Multiple(() =>
        {
            Assert.That(shiftedResult.Bytes, Is.EqualTo(shiftedBytes));
            Assert.That(shiftedResult.Codewords, Does.Contain(Code128Patterns.ShiftCodeword));
            Assert.That(switchedResult.Bytes, Is.EqualTo(switchedBytes));
            Assert.That(switchedResult.Codewords, Does.Contain(Code128Patterns.CodeCCodeword));
        });
    }

    [Test]
    public void DecoderRequiresBothStandardQuietZones()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(new byte[] { 65, 66, 67 }, null);

        Assert.Multiple(() =>
        {
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(CreateScanline(encoded, 9, 10, 1)), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(CreateScanline(encoded, 10, 9, 1)), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(CreateScanline(encoded, 0, 10, 1)), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(CreateScanline(encoded, 10, 0, 1)), Is.Null);
        });
    }

    [Test]
    public void DecoderRejectsWrongChecksumMissingStopAndUnfinishedControls()
    {
        BitRow wrongChecksum = CreateCodewordScanline(new int[] { 104, 33, 34, 35, 2, 106 }, 10, 10);
        BitRow missingStop = CreateCodewordScanline(new int[] { 104, 33, 34, 35, 1 }, 10, 10);
        Code128Plan shiftPlan = CreatePlan(Code128CodeSet.B, new int[] { Code128Patterns.ShiftCodeword });
        Code128Plan function4Plan = CreatePlan(Code128CodeSet.B, new int[] { Code128Patterns.Function4InCodeSetBCodeword });
        BitRow unfinishedShift = CreateScanline(Code128Encoder.AssemblePlan(shiftPlan), 10, 10, 1);
        BitRow unfinishedFunction4 = CreateScanline(Code128Encoder.AssemblePlan(function4Plan), 10, 10, 1);
        Code128Plan separatedFunction4Plan = CreatePlan(
            Code128CodeSet.B,
            new int[]
            {
                Code128Patterns.Function4InCodeSetBCodeword,
                Code128Patterns.Function1Codeword
            });
        Code128Plan shiftBeforeControlPlan = CreatePlan(
            Code128CodeSet.B,
            new int[]
            {
                Code128Patterns.ShiftCodeword,
                Code128Patterns.Function1Codeword
            });
        Code128Plan function4BeforeCodeCDataPlan = CreatePlan(
            Code128CodeSet.B,
            new int[]
            {
                Code128Patterns.Function4InCodeSetBCodeword,
                Code128Patterns.CodeCCodeword,
                12
            });
        Code128Plan repeatedSeparatedFunction4Plan = CreatePlan(
            Code128CodeSet.B,
            new int[]
            {
                Code128Patterns.Function4InCodeSetBCodeword,
                Code128Patterns.Function1Codeword,
                Code128Patterns.Function4InCodeSetBCodeword,
                33
            });
        BitRow separatedFunction4 = CreateScanline(
            Code128Encoder.AssemblePlan(separatedFunction4Plan), 10, 10, 1);
        BitRow shiftBeforeControl = CreateScanline(
            Code128Encoder.AssemblePlan(shiftBeforeControlPlan), 10, 10, 1);
        BitRow function4BeforeCodeCData = CreateScanline(
            Code128Encoder.AssemblePlan(function4BeforeCodeCDataPlan), 10, 10, 1);
        BitRow repeatedSeparatedFunction4 = CreateScanline(
            Code128Encoder.AssemblePlan(repeatedSeparatedFunction4Plan), 10, 10, 1);

        Assert.Multiple(() =>
        {
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(wrongChecksum), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(missingStop), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(unfinishedShift), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(unfinishedFunction4), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(separatedFunction4), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(shiftBeforeControl), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(function4BeforeCodeCData), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(repeatedSeparatedFunction4), Is.Null);
        });
    }

    [Test]
    public void DecoderRejectsEarlyStopAndSymbolsBeyondTheCodewordLimit()
    {
        BitRow earlyStop = CreateCodewordScanline(
            new int[] { Code128Patterns.StartBCodeword, 33, Code128Patterns.StopCodeword },
            10,
            10);
        int[] excessiveCodewords = new int[
            Code128ScanlineDecoder.MaximumCompleteCodewordCount - 1];
        excessiveCodewords[0] = Code128Patterns.StartBCodeword;
        BitRow excessive = CreateCodewordScanline(excessiveCodewords, 10, 10);
        BitRow startWithoutFollowingRun = CreateCodewordScanline(
            new int[] { Code128Patterns.StartBCodeword },
            10,
            1);
        BitRow startAtEndOfRow = CreateCodewordScanline(
            new int[] { Code128Patterns.StartBCodeword },
            10,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(earlyStop), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(excessive), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(startWithoutFollowingRun), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(startAtEndOfRow), Is.Null);
        });
    }

    [Test]
    public void DecoderAppliesExplicitSwitchFromCodeSetAToB()
    {
        Code128Plan plan = CreatePlan(
            Code128CodeSet.A,
            new int[] { Code128Patterns.CodeBCodeword, 65 });

        Code128DecodedData decoded = Code128ScanlineDecoder.DecodeRow(
            CreateScanline(Code128Encoder.AssemblePlan(plan), 10, 10, 1));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.StartCodeSet, Is.EqualTo(Code128CodeSet.A));
            Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 97 }));
        });
    }

    [Test]
    public void DecoderRejectsDamagedAndEmptyScanlines()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(new byte[] { 65, 66, 67 }, null);
        BitRow damaged = CreateScanline(encoded, 10, 10, 1);
        int damagePosition = 10 + 17;
        damaged.SetBit(damagePosition, !damaged.GetBit(damagePosition));
        BitRow empty = new BitRow(40);

        Assert.Multiple(() =>
        {
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(damaged), Is.Null);
            Assert.That(Code128ScanlineDecoder.TryDecodeRow(empty), Is.Null);
            Assert.That(
                () => Code128ScanlineDecoder.DecodeRow(empty),
                Throws.TypeOf<BarcodeDecodingException>()
                    .With.Message.EqualTo("The scanline does not contain a valid Code 128 symbol."));
            Assert.That(() => Code128ScanlineDecoder.TryDecodeRow(null!), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void DecoderSkipsAFalseDarkRunBeforeAValidStart()
    {
        Code128EncodedData encoded = Code128Encoder.EncodeBytes(new byte[] { 65, 66, 67 }, null);
        BitRow symbol = CreateScanline(encoded, 10, 10, 1);
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

        Code128DecodedData decoded = Code128ScanlineDecoder.DecodeRow(row);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Bytes, Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(decoded.StartPosition, Is.EqualTo(30));
            Assert.That(decoded.EndPosition, Is.EqualTo(98));
        });
    }

    static BitRow CreateScanline(Code128EncodedData encoded, int leftQuietModules, int rightQuietModules, int scale)
    {
        int length = (leftQuietModules + encoded.Modules.Length + rightQuietModules) * scale;
        BitRow row = new BitRow(length);
        int moduleIndex = 0;
        while (moduleIndex < encoded.Modules.Length)
        {
            if (encoded.Modules[moduleIndex] != 0)
            {
                int pixel = 0;
                while (pixel < scale)
                {
                    row.SetBit(((leftQuietModules + moduleIndex) * scale) + pixel, true);
                    pixel = pixel + 1;
                }
            }

            moduleIndex = moduleIndex + 1;
        }

        return row;
    }

    static BitRow CreateNoisyScaledScanline(Code128EncodedData encoded)
    {
        int[] runs = new int[encoded.ElementWidths.Length + 2];
        runs[0] = 20;
        int index = 0;
        while (index < encoded.ElementWidths.Length)
        {
            runs[index + 1] = encoded.ElementWidths[index] * 2;
            index = index + 1;
        }

        runs[runs.Length - 1] = 20;
        runs[7] = runs[7] + 1;
        runs[8] = runs[8] - 1;
        return RunLength.ExpandRuns(runs, false);
    }

    static BitRow CreateCodewordScanline(int[] codewords, int leftQuietModules, int rightQuietModules)
    {
        int elementCount = 0;
        int index = 0;
        while (index < codewords.Length)
        {
            elementCount = elementCount + Code128Patterns.GetElementCount(codewords[index]);
            index = index + 1;
        }

        int trailingRunCount = rightQuietModules == 0 ? 0 : 1;
        int[] runs = new int[elementCount + 1 + trailingRunCount];
        runs[0] = leftQuietModules;
        int runIndex = 1;
        index = 0;
        while (index < codewords.Length)
        {
            int[] widths = Code128Patterns.GetPatternWidths(codewords[index]);
            int widthIndex = 0;
            while (widthIndex < widths.Length)
            {
                runs[runIndex] = widths[widthIndex];
                runIndex = runIndex + 1;
                widthIndex = widthIndex + 1;
            }

            index = index + 1;
        }

        if (rightQuietModules != 0)
        {
            runs[runs.Length - 1] = rightQuietModules;
        }

        return RunLength.ExpandRuns(runs, false);
    }

    static Code128Plan CreatePlan(Code128CodeSet codeSet, int[] codewords)
    {
        Code128Plan plan = new Code128Plan();
        plan.StartCodeSet = codeSet;
        plan.Codewords = codewords;
        return plan;
    }
}
