namespace barcode.Tests;

public class DataMatrixMinimalEncoderTests
{
    [Test]
    public void ForcedModesProduceCanonicalSingleSegmentReferenceVectors()
    {
        DataMatrixEncodingPlan ascii = DataMatrixMinimalEncoder.Plan(
            global::System.Text.Encoding.ASCII.GetBytes("123456"),
            DataMatrixEncodingMode.Ascii);
        DataMatrixEncodingPlan c40 = DataMatrixMinimalEncoder.Plan(
            global::System.Text.Encoding.ASCII.GetBytes("AIMAIMAIM"),
            DataMatrixEncodingMode.C40);
        DataMatrixEncodingPlan text = DataMatrixMinimalEncoder.Plan(
            global::System.Text.Encoding.ASCII.GetBytes("aimaimaim"),
            DataMatrixEncodingMode.Text);
        DataMatrixEncodingPlan x12 = DataMatrixMinimalEncoder.Plan(
            global::System.Text.Encoding.ASCII.GetBytes("\r*>"),
            DataMatrixEncodingMode.X12);
        DataMatrixEncodingPlan edifact = DataMatrixMinimalEncoder.Plan(
            global::System.Text.Encoding.ASCII.GetBytes("ABCD"),
            DataMatrixEncodingMode.Edifact);
        DataMatrixEncodingPlan base256 = DataMatrixMinimalEncoder.Plan(
            new byte[] { 171, 228, 246, 252, 233, 187 },
            DataMatrixEncodingMode.Base256);

        Assert.Multiple(() =>
        {
            Assert.That(ascii.Codewords, Is.EqualTo(new int[] { 142, 164, 186 }));
            Assert.That(c40.Codewords, Is.EqualTo(new int[] { 230, 91, 11, 91, 11, 91, 11, 254 }));
            Assert.That(text.Codewords, Is.EqualTo(new int[] { 239, 91, 11, 91, 11, 91, 11, 254 }));
            Assert.That(x12.Codewords, Is.EqualTo(new int[] { 238, 0, 43, 254 }));
            Assert.That(edifact.Codewords, Is.EqualTo(new int[] { 240, 4, 32, 196, 124 }));
            Assert.That(base256.Codewords, Is.EqualTo(new int[] { 231, 50, 108, 59, 226, 126, 1, 104 }));
        });

        AssertSingleForcedSegment(ascii, DataMatrixEncodingMode.Ascii, 6);
        AssertSingleForcedSegment(c40, DataMatrixEncodingMode.C40, 9);
        AssertSingleForcedSegment(text, DataMatrixEncodingMode.Text, 9);
        AssertSingleForcedSegment(x12, DataMatrixEncodingMode.X12, 3);
        AssertSingleForcedSegment(edifact, DataMatrixEncodingMode.Edifact, 4);
        AssertSingleForcedSegment(base256, DataMatrixEncodingMode.Base256, 6);
    }

    [Test]
    public void AutomaticPlanningChoosesNaturalCompactModes()
    {
        DataMatrixEncodingPlan digits = PlanAsciiText("123456");
        DataMatrixEncodingPlan upper = PlanAsciiText("AIMAIMAIM");
        DataMatrixEncodingPlan lower = PlanAsciiText("aimaimaim");
        DataMatrixEncodingPlan x12 = PlanAsciiText("\r*>\r*>\r*>");
        DataMatrixEncodingPlan edifact = PlanAsciiText("^^^^^^^^^^^^");
        DataMatrixEncodingPlan binary = DataMatrixMinimalEncoder.Plan(
            new byte[] { 128, 129, 130 },
            DataMatrixEncodingMode.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(digits.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.Ascii }));
            Assert.That(digits.Codewords, Is.EqualTo(new int[] { 142, 164, 186 }));
            Assert.That(upper.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.C40 }));
            Assert.That(upper.Codewords, Has.Length.EqualTo(8));
            Assert.That(lower.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.Text }));
            Assert.That(lower.Codewords, Has.Length.EqualTo(8));
            Assert.That(x12.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.X12 }));
            Assert.That(x12.Codewords, Has.Length.EqualTo(8));
            Assert.That(edifact.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.Edifact }));
            Assert.That(edifact.Codewords, Has.Length.EqualTo(11));
            Assert.That(binary.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.Base256 }));
            Assert.That(binary.Codewords, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void AutomaticPlanningChangesModesOnlyAfterEveryAsciiCostBoundary()
    {
        byte[][] tiedWithAscii = new byte[][]
        {
            global::System.Text.Encoding.ASCII.GetBytes("AAAAAAAA"),
            global::System.Text.Encoding.ASCII.GetBytes("aaaaaaaa"),
            global::System.Text.Encoding.ASCII.GetBytes("\r*>\r*>\r*"),
            global::System.Text.Encoding.ASCII.GetBytes("^^^^^^^^^^"),
            new byte[] { 128, 129 }
        };
        byte[][] compactModeWins = new byte[][]
        {
            global::System.Text.Encoding.ASCII.GetBytes("AAAAAAAAA"),
            global::System.Text.Encoding.ASCII.GetBytes("aaaaaaaaa"),
            global::System.Text.Encoding.ASCII.GetBytes("\r*>\r*>\r*>"),
            global::System.Text.Encoding.ASCII.GetBytes("^^^^^^^^^^^"),
            new byte[] { 128, 129, 130 }
        };
        DataMatrixEncodingMode[] expectedModes = new DataMatrixEncodingMode[]
        {
            DataMatrixEncodingMode.C40,
            DataMatrixEncodingMode.Text,
            DataMatrixEncodingMode.X12,
            DataMatrixEncodingMode.Edifact,
            DataMatrixEncodingMode.Base256
        };
        int[] expectedCompactCosts = new int[] { 8, 8, 8, 10, 5 };

        int index = 0;
        while (index < expectedModes.Length)
        {
            DataMatrixEncodingPlan tied = DataMatrixMinimalEncoder.Plan(
                tiedWithAscii[index],
                DataMatrixEncodingMode.Automatic);
            DataMatrixEncodingPlan compact = DataMatrixMinimalEncoder.Plan(
                compactModeWins[index],
                DataMatrixEncodingMode.Automatic);

            Assert.Multiple(() =>
            {
                Assert.That(
                    tied.SegmentModes,
                    Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.Ascii }),
                    $"ASCII tie boundary for {expectedModes[index]}");
                Assert.That(
                    compact.SegmentModes,
                    Is.EqualTo(new DataMatrixEncodingMode[] { expectedModes[index] }),
                    $"First compact win for {expectedModes[index]}");
                Assert.That(
                    compact.Codewords,
                    Has.Length.EqualTo(expectedCompactCosts[index]),
                    $"Compact cost for {expectedModes[index]}");
                Assert.That(DecodePlan(compact), Is.EqualTo(compactModeWins[index]));
            });
            index++;
        }
    }

    [Test]
    public void AutomaticPlanningCanJoinDifferentCompactModes()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("AIMAIMAIMaimaimaim");

        DataMatrixEncodingPlan plan = DataMatrixMinimalEncoder.Plan(
            data,
            DataMatrixEncodingMode.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(plan.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[]
            {
                DataMatrixEncodingMode.C40,
                DataMatrixEncodingMode.Text
            }));
            Assert.That(plan.SegmentByteStarts, Is.EqualTo(new int[] { 0, 9 }));
            Assert.That(plan.SegmentByteCounts, Is.EqualTo(new int[] { 9, 9 }));
            Assert.That(plan.SegmentCodewordCounts, Is.EqualTo(new int[] { 8, 8 }));
            Assert.That(plan.Codewords, Has.Length.EqualTo(16));
            Assert.That(DecodePlan(plan), Is.EqualTo(data));
        });
    }

    [Test]
    public void EveryAutomaticPlanSegmentRoundTripsAndHasContiguousMetadata()
    {
        byte[][] samples = new byte[][]
        {
            global::System.Text.Encoding.ASCII.GetBytes("ABC123abc456"),
            global::System.Text.Encoding.ASCII.GetBytes("AIMAIMAIMaimaimaim"),
            global::System.Text.Encoding.ASCII.GetBytes("\r*>ABC123^^^^"),
            new byte[] { 65, 128, 129, 130, 49, 50, 51, 52, 97, 98, 99 },
            CreateDeterministicBytes(40)
        };

        int sampleIndex = 0;
        while (sampleIndex < samples.Length)
        {
            DataMatrixEncodingPlan plan = DataMatrixMinimalEncoder.Plan(
                samples[sampleIndex],
                DataMatrixEncodingMode.Automatic);
            Assert.That(DecodePlan(plan), Is.EqualTo(samples[sampleIndex]), $"Sample {sampleIndex}");
            AssertPlanMetadata(plan);
            sampleIndex++;
        }
    }

    [Test]
    public void DynamicPlannerMatchesIndependentExhaustiveSearchOnShortInputs()
    {
        byte[][] samples = new byte[][]
        {
            global::System.Text.Encoding.ASCII.GetBytes("ABC123"),
            global::System.Text.Encoding.ASCII.GetBytes("abc123"),
            global::System.Text.Encoding.ASCII.GetBytes("\r*>ABC"),
            global::System.Text.Encoding.ASCII.GetBytes("^^^^^^"),
            new byte[] { 128, 129, 130, 65 },
            new byte[] { 65, 66, 67, 97, 98, 99 },
            new byte[] { 49, 50, 65, 128, 51, 52 }
        };

        int index = 0;
        while (index < samples.Length)
        {
            byte[] data = samples[index];
            int[] memo = CreateMemo(data.Length + 1);
            int expectedCost = FindMinimumCostReference(data, 0, memo);
            DataMatrixEncodingPlan actual = DataMatrixMinimalEncoder.Plan(
                data,
                DataMatrixEncodingMode.Automatic);
            Assert.That(actual.Codewords.Length, Is.EqualTo(expectedCost), $"Sample {index}");
            index++;
        }
    }

    [Test]
    public void EqualCostPlansUseStableAsciiFirstTieBreaking()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("^^^^^^^^");

        DataMatrixEncodingPlan first = DataMatrixMinimalEncoder.Plan(
            data,
            DataMatrixEncodingMode.Automatic);
        DataMatrixEncodingPlan second = DataMatrixMinimalEncoder.Plan(
            data,
            DataMatrixEncodingMode.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(first.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.Ascii }));
            Assert.That(first.Codewords, Is.EqualTo(new int[] { 95, 95, 95, 95, 95, 95, 95, 95 }));
            Assert.That(second.Codewords, Is.EqualTo(first.Codewords));
            Assert.That(second.SegmentModes, Is.EqualTo(first.SegmentModes));
            Assert.That(second.SegmentByteStarts, Is.EqualTo(first.SegmentByteStarts));
            Assert.That(second.SegmentByteCounts, Is.EqualTo(first.SegmentByteCounts));
        });
    }

    [Test]
    public void ForcedModesRoundTripTheirCompleteSupportedByteRanges()
    {
        byte[] everyByte = new byte[256];
        int index = 0;
        while (index < everyByte.Length)
        {
            everyByte[index] = (byte)index;
            index++;
        }

        DataMatrixEncodingPlan ascii = DataMatrixMinimalEncoder.Plan(everyByte, DataMatrixEncodingMode.Ascii);
        DataMatrixEncodingPlan c40 = DataMatrixMinimalEncoder.Plan(everyByte, DataMatrixEncodingMode.C40);
        DataMatrixEncodingPlan text = DataMatrixMinimalEncoder.Plan(everyByte, DataMatrixEncodingMode.Text);
        DataMatrixEncodingPlan base256 = DataMatrixMinimalEncoder.Plan(everyByte, DataMatrixEncodingMode.Base256);
        byte[] x12Alphabet = global::System.Text.Encoding.ASCII.GetBytes("\r*> 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        byte[] edifactAlphabet = CreateEdifactAlphabet();
        DataMatrixEncodingPlan x12 = DataMatrixMinimalEncoder.Plan(x12Alphabet, DataMatrixEncodingMode.X12);
        DataMatrixEncodingPlan edifact = DataMatrixMinimalEncoder.Plan(edifactAlphabet, DataMatrixEncodingMode.Edifact);

        Assert.Multiple(() =>
        {
            Assert.That(DecodePlan(ascii), Is.EqualTo(everyByte));
            Assert.That(DecodePlan(c40), Is.EqualTo(everyByte));
            Assert.That(DecodePlan(text), Is.EqualTo(everyByte));
            Assert.That(DecodePlan(base256), Is.EqualTo(everyByte));
            Assert.That(DecodePlan(x12), Is.EqualTo(x12Alphabet));
            Assert.That(DecodePlan(edifact), Is.EqualTo(edifactAlphabet));
        });
    }

    [Test]
    public void ForcedModeValidationRejectsUnsupportedAlphabetsLengthsAndModes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => DataMatrixMinimalEncoder.Plan(new byte[] { (byte)'a' }, DataMatrixEncodingMode.X12),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixMinimalEncoder.Plan(new byte[] { 128 }, DataMatrixEncodingMode.X12),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixMinimalEncoder.Plan(new byte[] { 31 }, DataMatrixEncodingMode.Edifact),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixMinimalEncoder.Plan(new byte[] { 95 }, DataMatrixEncodingMode.Edifact),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixMinimalEncoder.Plan(new byte[1556], DataMatrixEncodingMode.Base256),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixMinimalEncoder.Plan(new byte[] { 65 }, (DataMatrixEncodingMode)99),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void PlannerEnforcesStandardMaximumCapacityAndNumericInputBoundary()
    {
        byte[] maximumDigits = CreateRepeatedByte(3116, (byte)'1');
        DataMatrixEncodingPlan maximum = DataMatrixMinimalEncoder.Plan(
            maximumDigits,
            DataMatrixEncodingMode.Automatic);

        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixMinimalEncoder.MaximumInputByteCount, Is.EqualTo(3116));
            Assert.That(DataMatrixMinimalEncoder.MaximumPlannedCodewordCount, Is.EqualTo(1558));
            Assert.That(maximum.Codewords, Has.Length.EqualTo(1558));
            Assert.That(maximum.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { DataMatrixEncodingMode.Ascii }));
            Assert.That(
                () => DataMatrixMinimalEncoder.Plan(CreateRepeatedByte(3117, (byte)'1'), DataMatrixEncodingMode.Automatic),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => DataMatrixMinimalEncoder.Plan(CreateRepeatedByte(1556, 128), DataMatrixEncodingMode.Automatic),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void PlannerRejectsMissingEmptyDataAndDoesNotAliasInput()
    {
        byte[] input = global::System.Text.Encoding.ASCII.GetBytes("AIMAIMAIM");
        DataMatrixEncodingPlan plan = DataMatrixMinimalEncoder.Plan(input, DataMatrixEncodingMode.Automatic);
        input[0] = (byte)'Z';

        Assert.Multiple(() =>
        {
            Assert.That(plan.Data[0], Is.EqualTo((byte)'A'));
            Assert.That(() => DataMatrixMinimalEncoder.Plan(null!, DataMatrixEncodingMode.Automatic), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixMinimalEncoder.Plan(new byte[0], DataMatrixEncodingMode.Automatic), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static DataMatrixEncodingPlan PlanAsciiText(string text)
    {
        return DataMatrixMinimalEncoder.Plan(
            global::System.Text.Encoding.ASCII.GetBytes(text),
            DataMatrixEncodingMode.Automatic);
    }

    static void AssertSingleForcedSegment(
        DataMatrixEncodingPlan plan,
        DataMatrixEncodingMode mode,
        int byteCount)
    {
        Assert.Multiple(() =>
        {
            Assert.That(plan.RequestedMode, Is.EqualTo(mode));
            Assert.That(plan.SegmentModes, Is.EqualTo(new DataMatrixEncodingMode[] { mode }));
            Assert.That(plan.SegmentByteStarts, Is.EqualTo(new int[] { 0 }));
            Assert.That(plan.SegmentByteCounts, Is.EqualTo(new int[] { byteCount }));
            Assert.That(plan.SegmentCodewordStarts, Is.EqualTo(new int[] { 0 }));
            Assert.That(plan.SegmentCodewordCounts, Is.EqualTo(new int[] { plan.Codewords.Length }));
        });
    }

    static void AssertPlanMetadata(DataMatrixEncodingPlan plan)
    {
        int expectedByteStart = 0;
        int expectedCodewordStart = 0;
        int index = 0;
        while (index < plan.SegmentModes.Length)
        {
            Assert.Multiple(() =>
            {
                Assert.That(plan.SegmentByteStarts[index], Is.EqualTo(expectedByteStart));
                Assert.That(plan.SegmentCodewordStarts[index], Is.EqualTo(expectedCodewordStart));
                Assert.That(plan.SegmentByteCounts[index], Is.GreaterThan(0));
                Assert.That(plan.SegmentCodewordCounts[index], Is.GreaterThan(0));
            });
            expectedByteStart += plan.SegmentByteCounts[index];
            expectedCodewordStart += plan.SegmentCodewordCounts[index];
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(expectedByteStart, Is.EqualTo(plan.Data.Length));
            Assert.That(expectedCodewordStart, Is.EqualTo(plan.Codewords.Length));
            Assert.That(plan.SegmentByteStarts, Has.Length.EqualTo(plan.SegmentModes.Length));
            Assert.That(plan.SegmentByteCounts, Has.Length.EqualTo(plan.SegmentModes.Length));
            Assert.That(plan.SegmentCodewordStarts, Has.Length.EqualTo(plan.SegmentModes.Length));
            Assert.That(plan.SegmentCodewordCounts, Has.Length.EqualTo(plan.SegmentModes.Length));
        });
    }

    static byte[] DecodePlan(DataMatrixEncodingPlan plan)
    {
        global::System.Collections.Generic.List<byte> output = new global::System.Collections.Generic.List<byte>();
        int segmentIndex = 0;
        while (segmentIndex < plan.SegmentModes.Length)
        {
            int[] segment = CopyCodewordSegment(plan, segmentIndex);
            byte[] decoded;
            DataMatrixEncodingMode mode = plan.SegmentModes[segmentIndex];
            if (mode == DataMatrixEncodingMode.Ascii)
            {
                decoded = DecodeAsciiReference(segment);
            }
            else if (mode == DataMatrixEncodingMode.C40)
            {
                decoded = DataMatrixModeCodec.DecodeC40(segment);
            }
            else if (mode == DataMatrixEncodingMode.Text)
            {
                decoded = DataMatrixModeCodec.DecodeText(segment);
            }
            else if (mode == DataMatrixEncodingMode.X12)
            {
                decoded = DataMatrixModeCodec.DecodeX12(segment);
            }
            else if (mode == DataMatrixEncodingMode.Edifact)
            {
                decoded = DataMatrixModeCodec.DecodeEdifact(segment);
            }
            else
            {
                decoded = DataMatrixModeCodec.DecodeBase256(
                    segment,
                    plan.SegmentCodewordStarts[segmentIndex] + 1);
            }

            output.AddRange(decoded);
            segmentIndex++;
        }

        return output.ToArray();
    }

    static int[] CopyCodewordSegment(DataMatrixEncodingPlan plan, int segmentIndex)
    {
        int count = plan.SegmentCodewordCounts[segmentIndex];
        int[] segment = new int[count];
        global::System.Array.Copy(
            plan.Codewords,
            plan.SegmentCodewordStarts[segmentIndex],
            segment,
            0,
            count);
        return segment;
    }

    static byte[] DecodeAsciiReference(int[] codewords)
    {
        global::System.Collections.Generic.List<byte> output = new global::System.Collections.Generic.List<byte>();
        int index = 0;
        while (index < codewords.Length)
        {
            int codeword = codewords[index];
            if (codeword >= 1 && codeword <= 128)
            {
                output.Add((byte)(codeword - 1));
                index++;
            }
            else if (codeword >= 130 && codeword <= 229)
            {
                int pair = codeword - 130;
                output.Add((byte)(48 + (pair / 10)));
                output.Add((byte)(48 + (pair % 10)));
                index++;
            }
            else if (codeword == 235)
            {
                output.Add((byte)(codewords[index + 1] + 127));
                index += 2;
            }
            else
            {
                throw new global::System.InvalidOperationException("Unexpected ASCII plan codeword.");
            }
        }

        return output.ToArray();
    }

    static int FindMinimumCostReference(byte[] data, int position, int[] memo)
    {
        if (position == data.Length)
        {
            return 0;
        }

        if (memo[position] >= 0)
        {
            return memo[position];
        }

        int best = int.MaxValue;
        int end = position + 1;
        while (end <= data.Length)
        {
            byte[] segmentData = CopyByteRange(data, position, end - position);
            int modeValue = (int)DataMatrixEncodingMode.Ascii;
            while (modeValue <= (int)DataMatrixEncodingMode.Base256)
            {
                DataMatrixEncodingMode mode = (DataMatrixEncodingMode)modeValue;
                int segmentCost = TryGetReferenceSegmentCost(
                    segmentData,
                    mode);
                if (segmentCost >= 0)
                {
                    int remainder = FindMinimumCostReference(data, end, memo);
                    if (remainder != int.MaxValue && segmentCost + remainder < best)
                    {
                        best = segmentCost + remainder;
                    }
                }

                modeValue++;
            }

            end++;
        }

        memo[position] = best;
        return best;
    }

    static int TryGetReferenceSegmentCost(
        byte[] data,
        DataMatrixEncodingMode mode)
    {
        try
        {
            int[] codewords;
            if (mode == DataMatrixEncodingMode.Ascii)
            {
                codewords = DataMatrixHighLevelEncoder.EncodeAscii(data);
            }
            else if (mode == DataMatrixEncodingMode.C40)
            {
                codewords = DataMatrixModeCodec.EncodeC40(data);
            }
            else if (mode == DataMatrixEncodingMode.Text)
            {
                codewords = DataMatrixModeCodec.EncodeText(data);
            }
            else if (mode == DataMatrixEncodingMode.X12)
            {
                codewords = DataMatrixModeCodec.EncodeX12(data);
            }
            else if (mode == DataMatrixEncodingMode.Edifact)
            {
                codewords = DataMatrixModeCodec.EncodeEdifact(data);
            }
            else
            {
                codewords = DataMatrixModeCodec.EncodeBase256(data, 1);
            }

            return codewords.Length;
        }
        catch (BarcodeEncodingException)
        {
            return -1;
        }
    }

    static int[] CreateMemo(int length)
    {
        int[] memo = new int[length];
        int index = 0;
        while (index < memo.Length)
        {
            memo[index] = -1;
            index++;
        }

        return memo;
    }

    static byte[] CopyByteRange(byte[] source, int start, int count)
    {
        byte[] copy = new byte[count];
        global::System.Array.Copy(source, start, copy, 0, count);
        return copy;
    }

    static byte[] CreateDeterministicBytes(int length)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)((index * 73) + 19);
            index++;
        }

        return data;
    }

    static byte[] CreateEdifactAlphabet()
    {
        byte[] data = new byte[63];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)(32 + index);
            index++;
        }

        return data;
    }

    static byte[] CreateRepeatedByte(int count, byte value)
    {
        byte[] data = new byte[count];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = value;
            index++;
        }

        return data;
    }
}
