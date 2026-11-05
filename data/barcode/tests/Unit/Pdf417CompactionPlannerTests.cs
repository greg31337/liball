namespace barcode.Tests;

public class Pdf417CompactionPlannerTests
{
    [Test]
    public void EciAssignmentBoundariesUseAllThreeStandardDesignators()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                Pdf417CompactionPlanner.EncodeEciDesignator(0),
                Is.EqualTo(new int[] { 927, 0 }));
            Assert.That(
                Pdf417CompactionPlanner.EncodeEciDesignator(899),
                Is.EqualTo(new int[] { 927, 899 }));
            Assert.That(
                Pdf417CompactionPlanner.EncodeEciDesignator(900),
                Is.EqualTo(new int[] { 926, 0, 0 }));
            Assert.That(
                Pdf417CompactionPlanner.EncodeEciDesignator(13579),
                Is.EqualTo(new int[] { 926, 14, 79 }));
            Assert.That(
                Pdf417CompactionPlanner.EncodeEciDesignator(810899),
                Is.EqualTo(new int[] { 926, 899, 899 }));
            Assert.That(
                Pdf417CompactionPlanner.EncodeEciDesignator(810900),
                Is.EqualTo(new int[] { 925, 0 }));
            Assert.That(
                Pdf417CompactionPlanner.EncodeEciDesignator(811799),
                Is.EqualTo(new int[] { 925, 899 }));
        });
    }

    [Test]
    public void ForcedModesDelegateToTheirExactCompactionFormats()
    {
        Pdf417CompactionPlan text = Pdf417CompactionPlanner.Plan(
            Bytes("PDF417"),
            Pdf417CompactionMode.Text,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan bytes = Pdf417CompactionPlanner.Plan(
            new byte[] { 0, 255 },
            Pdf417CompactionMode.Byte,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan numeric = Pdf417CompactionPlanner.Plan(
            Bytes("000213298174000"),
            Pdf417CompactionMode.Numeric,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);

        Assert.Multiple(() =>
        {
            Assert.That(text.Codewords, Is.EqualTo(new int[] { 453, 178, 121, 239 }));
            Assert.That(text.SegmentModes, Is.EqualTo(new[] { Pdf417CompactionMode.Text }));
            Assert.That(bytes.Codewords, Is.EqualTo(new int[] { 901, 0, 255 }));
            Assert.That(bytes.SegmentModes, Is.EqualTo(new[] { Pdf417CompactionMode.Byte }));
            Assert.That(numeric.Codewords, Is.EqualTo(new int[]
            {
                902, 1, 624, 434, 632, 282, 200
            }));
            Assert.That(numeric.SegmentModes, Is.EqualTo(new[] { Pdf417CompactionMode.Numeric }));
        });
    }

    [Test]
    public void EciPrefixPrecedesCompactionAndIsExcludedFromSegmentCounts()
    {
        Pdf417CompactionPlan plan = Pdf417CompactionPlanner.Plan(
            Bytes("PDF417"),
            Pdf417CompactionMode.Text,
            26);

        Assert.Multiple(() =>
        {
            Assert.That(plan.EciAssignmentNumber, Is.EqualTo(26));
            Assert.That(plan.Codewords, Is.EqualTo(new int[]
            {
                927, 26, 453, 178, 121, 239
            }));
            Assert.That(plan.SegmentCodewordStarts, Is.EqualTo(new int[] { 2 }));
            Assert.That(plan.SegmentCodewordCounts, Is.EqualTo(new int[] { 4 }));
        });
    }

    [Test]
    public void AutomaticPlanningUsesTextNumericAndByteForNaturalPayloads()
    {
        Pdf417CompactionPlan text = Pdf417CompactionPlanner.Plan(
            Bytes("HELLO WORLD"),
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan numeric = Pdf417CompactionPlanner.Plan(
            Bytes("123456789012345678901234567890"),
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        byte[] binaryData = new byte[12];
        int index = 0;
        while (index < binaryData.Length)
        {
            binaryData[index] = (byte)(128 + index);
            index++;
        }
        Pdf417CompactionPlan binary = Pdf417CompactionPlanner.Plan(
            binaryData,
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);

        Assert.Multiple(() =>
        {
            Assert.That(text.SegmentModes, Is.EqualTo(new[] { Pdf417CompactionMode.Text }));
            Assert.That(
                text.Codewords,
                Is.EqualTo(Pdf417TextCompaction.EncodeText(
                    "HELLO WORLD",
                    Pdf417TextSubmode.Alpha).Codewords));
            Assert.That(numeric.SegmentModes, Is.EqualTo(new[] { Pdf417CompactionMode.Numeric }));
            Assert.That(numeric.Codewords[0], Is.EqualTo(902));
            Assert.That(binary.SegmentModes, Is.EqualTo(new[] { Pdf417CompactionMode.Byte }));
            Assert.That(binary.Codewords[0], Is.EqualTo(924));
        });
    }

    [Test]
    public void AutomaticPlanningSplitsLongDigitRunBetweenTextRuns()
    {
        const string source = "ABCD12345678901234567890EFGH";

        Pdf417CompactionPlan plan = Pdf417CompactionPlanner.Plan(
            Bytes(source),
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);

        Assert.Multiple(() =>
        {
            Assert.That(plan.SegmentModes, Is.EqualTo(new[]
            {
                Pdf417CompactionMode.Text,
                Pdf417CompactionMode.Numeric,
                Pdf417CompactionMode.Text
            }));
            Assert.That(plan.SegmentByteStarts, Is.EqualTo(new int[] { 0, 4, 24 }));
            Assert.That(plan.SegmentByteCounts, Is.EqualTo(new int[] { 4, 20, 4 }));
            Assert.That(plan.Codewords[plan.SegmentCodewordStarts[1]], Is.EqualTo(902));
            Assert.That(plan.Codewords[plan.SegmentCodewordStarts[2]], Is.EqualTo(900));
            Assert.That(plan.SegmentCodewordCounts.Sum(), Is.EqualTo(plan.Codewords.Length));
        });
    }

    [Test]
    public void OneBinaryByteBetweenTextRunsUsesTemporaryShift()
    {
        byte[] source = new byte[] { (byte)'A', (byte)'B', 255, (byte)'C', (byte)'D' };

        Pdf417CompactionPlan plan = Pdf417CompactionPlanner.Plan(
            source,
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Codewords, Is.EqualTo(new int[] { 1, 913, 255, 63 }));
            Assert.That(plan.SegmentModes, Is.EqualTo(new[]
            {
                Pdf417CompactionMode.Text,
                Pdf417CompactionMode.Byte,
                Pdf417CompactionMode.Text
            }));
            Assert.That(plan.SegmentByteStarts, Is.EqualTo(new int[] { 0, 2, 3 }));
            Assert.That(plan.SegmentByteCounts, Is.EqualTo(new int[] { 2, 1, 2 }));
            Assert.That(plan.SegmentCodewordCounts, Is.EqualTo(new int[] { 1, 2, 1 }));
        });
    }

    [Test]
    public void MixedAutomaticSegmentsDecodeBackToTheOriginalBytes()
    {
        byte[] prefix = Bytes("ab;:");
        byte[] suffix = Bytes("12345678901234567890XYZ");
        byte[] source = new byte[prefix.Length + suffix.Length + 1];
        global::System.Array.Copy(prefix, source, prefix.Length);
        source[prefix.Length] = 255;
        global::System.Array.Copy(
            suffix,
            0,
            source,
            prefix.Length + 1,
            suffix.Length);

        Pdf417CompactionPlan plan = Pdf417CompactionPlanner.Plan(
            source,
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);

        Assert.Multiple(() =>
        {
            Assert.That(plan.SegmentModes, Does.Contain(Pdf417CompactionMode.Text));
            Assert.That(plan.SegmentModes, Does.Contain(Pdf417CompactionMode.Byte));
            Assert.That(plan.SegmentModes, Does.Contain(Pdf417CompactionMode.Numeric));
            Assert.That(DecodePlan(plan), Is.EqualTo(source));
        });
    }

    [Test]
    public void AutomaticPlanNeverExceedsAnyForcedPlanForCompatibleAsciiDigits()
    {
        byte[] source = Bytes("123456789012A123456789012");

        Pdf417CompactionPlan automatic = Pdf417CompactionPlanner.Plan(
            source,
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan text = Pdf417CompactionPlanner.Plan(
            source,
            Pdf417CompactionMode.Text,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan bytes = Pdf417CompactionPlanner.Plan(
            source,
            Pdf417CompactionMode.Byte,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);

        Assert.Multiple(() =>
        {
            Assert.That(automatic.Codewords.Length, Is.LessThanOrEqualTo(text.Codewords.Length));
            Assert.That(automatic.Codewords.Length, Is.LessThanOrEqualTo(bytes.Codewords.Length));
            Assert.That(automatic.SegmentByteCounts.Sum(), Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void ExactCostsBeatFixedNumericThresholdsAndResolveTiesDeterministically()
    {
        byte[] twelveDigits = Bytes("123456789012");
        byte[] fiveDigits = Bytes("12345");

        Pdf417CompactionPlan optimized = Pdf417CompactionPlanner.Plan(
            twelveDigits,
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan forcedText = Pdf417CompactionPlanner.Plan(
            twelveDigits,
            Pdf417CompactionMode.Text,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan tied = Pdf417CompactionPlanner.Plan(
            fiveDigits,
            Pdf417CompactionMode.Automatic,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);

        Assert.Multiple(() =>
        {
            Assert.That(optimized.SegmentModes, Is.EqualTo(new[]
            {
                Pdf417CompactionMode.Numeric
            }));
            Assert.That(optimized.Codewords.Length, Is.EqualTo(6));
            Assert.That(forcedText.Codewords.Length, Is.EqualTo(7));
            Assert.That(tied.Codewords.Length, Is.EqualTo(3));
            Assert.That(tied.SegmentModes, Is.EqualTo(new[]
            {
                Pdf417CompactionMode.Text
            }));
        });
    }

    [Test]
    public void RepeatedPlanningUsesStableTieBreakingAndOwnsAllResultBuffers()
    {
        byte[] source = Bytes("AB12cd34EF");
        Pdf417CompactionPlan first = Pdf417CompactionPlanner.Plan(
            source,
            Pdf417CompactionMode.Automatic,
            3);
        Pdf417CompactionPlan second = Pdf417CompactionPlanner.Plan(
            source,
            Pdf417CompactionMode.Automatic,
            3);
        int expectedByte = first.Data[0];
        int expectedCodeword = first.Codewords[0];
        source[0] = 0;
        first.Codewords[0] = 0;
        first.SegmentByteCounts[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(first.Data[0], Is.EqualTo(expectedByte));
            Assert.That(second.Data[0], Is.EqualTo(expectedByte));
            Assert.That(second.Codewords[0], Is.EqualTo(expectedCodeword));
            Assert.That(second.Codewords, Is.Not.SameAs(first.Codewords));
            Assert.That(second.SegmentByteCounts[0], Is.GreaterThan(0));
        });
    }

    [Test]
    public void EveryForcedModeReachesAndCrossesTheExactSymbolCapacity()
    {
        Pdf417CompactionPlan textMaximum = Pdf417CompactionPlanner.Plan(
            CreateRepeatedByte(1850, (byte)'A'),
            Pdf417CompactionMode.Text,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan byteMaximum = Pdf417CompactionPlanner.Plan(
            CreateRepeatedByte(1108, 255),
            Pdf417CompactionMode.Byte,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan numericMaximum = Pdf417CompactionPlanner.Plan(
            CreateRepeatedByte(2710, (byte)'9'),
            Pdf417CompactionMode.Numeric,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan textExcess = Pdf417CompactionPlanner.Plan(
            CreateRepeatedByte(1851, (byte)'A'),
            Pdf417CompactionMode.Text,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan byteExcess = Pdf417CompactionPlanner.Plan(
            CreateRepeatedByte(1109, 255),
            Pdf417CompactionMode.Byte,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417CompactionPlan numericExcess = Pdf417CompactionPlanner.Plan(
            CreateRepeatedByte(2711, (byte)'9'),
            Pdf417CompactionMode.Numeric,
            Pdf417CompactionPlanner.NoEciAssignmentNumber);
        Pdf417Options levelZero = new Pdf417Options();
        levelZero.ErrorCorrectionLevel = 0;

        Assert.Multiple(() =>
        {
            Assert.That(textMaximum.Codewords, Has.Length.EqualTo(925));
            Assert.That(byteMaximum.Codewords, Has.Length.EqualTo(925));
            Assert.That(numericMaximum.Codewords, Has.Length.EqualTo(925));
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(textMaximum.Codewords, levelZero, null),
                Throws.Nothing);
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(byteMaximum.Codewords, levelZero, null),
                Throws.Nothing);
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(numericMaximum.Codewords, levelZero, null),
                Throws.Nothing);
            Assert.That(textExcess.Codewords, Has.Length.EqualTo(926));
            Assert.That(byteExcess.Codewords, Has.Length.EqualTo(926));
            Assert.That(numericExcess.Codewords, Has.Length.EqualTo(926));
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(textExcess.Codewords, levelZero, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(byteExcess.Codewords, levelZero, null),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(numericExcess.Codewords, levelZero, null),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void EciPrefixConsumesTheExactRemainingTextCapacity()
    {
        Pdf417CompactionPlan maximum = Pdf417CompactionPlanner.Plan(
            CreateRepeatedByte(1846, (byte)'A'),
            Pdf417CompactionMode.Text,
            26);
        Pdf417CompactionPlan excess = Pdf417CompactionPlanner.Plan(
            CreateRepeatedByte(1847, (byte)'A'),
            Pdf417CompactionMode.Text,
            26);
        Pdf417Options levelZero = new Pdf417Options();
        levelZero.ErrorCorrectionLevel = 0;

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Codewords.Take(2), Is.EqualTo(new int[] { 927, 26 }));
            Assert.That(maximum.Codewords, Has.Length.EqualTo(925));
            Assert.That(maximum.SegmentCodewordStarts, Is.EqualTo(new int[] { 2 }));
            Assert.That(maximum.SegmentCodewordCounts, Is.EqualTo(new int[] { 923 }));
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(maximum.Codewords, levelZero, null),
                Throws.Nothing);
            Assert.That(excess.Codewords, Has.Length.EqualTo(926));
            Assert.That(
                () => Pdf417SymbolPlanner.CreateSymbolPlan(excess.Codewords, levelZero, null),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void ForcedModesAndEciRejectInputsOutsideTheirDomains()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => Pdf417CompactionPlanner.Plan(null!, Pdf417CompactionMode.Automatic, -1),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.Plan(new byte[0], Pdf417CompactionMode.Automatic, -1),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.Plan(new byte[3001], Pdf417CompactionMode.Automatic, -1),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.Plan(Bytes("caf\u00e9"), Pdf417CompactionMode.Text, -1),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.Plan(Bytes("12A3"), Pdf417CompactionMode.Numeric, -1),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.Plan(Bytes("A"), (Pdf417CompactionMode)99, -1),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.Plan(Bytes("A"), Pdf417CompactionMode.Automatic, -2),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.Plan(Bytes("A"), Pdf417CompactionMode.Automatic, 811800),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.EncodeEciDesignator(-1),
                Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(
                () => Pdf417CompactionPlanner.EncodeEciDesignator(811800),
                Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    static byte[] Bytes(string text)
    {
        return global::System.Text.Encoding.Latin1.GetBytes(text);
    }

    static byte[] CreateRepeatedByte(int count, byte value)
    {
        byte[] bytes = new byte[count];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = value;
            index++;
        }

        return bytes;
    }

    static byte[] DecodePlan(Pdf417CompactionPlan plan)
    {
        global::System.Collections.Generic.List<byte> bytes = new global::System.Collections.Generic.List<byte>();
        Pdf417CompactionMode currentMode = Pdf417CompactionMode.Text;
        Pdf417TextSubmode textSubmode = Pdf417TextSubmode.Alpha;
        int segmentIndex = 0;
        while (segmentIndex < plan.SegmentModes.Length)
        {
            int start = plan.SegmentCodewordStarts[segmentIndex];
            int count = plan.SegmentCodewordCounts[segmentIndex];
            int[] codewords = plan.Codewords.Skip(start).Take(count).ToArray();
            if (plan.SegmentModes[segmentIndex] == Pdf417CompactionMode.Text)
            {
                if (codewords[0] == Pdf417TextCompaction.TextLatchCodeword)
                {
                    codewords = codewords.Skip(1).ToArray();
                    textSubmode = Pdf417TextSubmode.Alpha;
                }

                Pdf417TextDecodingResult decoded = Pdf417TextCompaction.DecodeCodewords(
                    codewords,
                    textSubmode);
                bytes.AddRange(global::System.Text.Encoding.Latin1.GetBytes(decoded.Text));
                textSubmode = decoded.FinalSubmode;
                currentMode = Pdf417CompactionMode.Text;
            }
            else if (plan.SegmentModes[segmentIndex] == Pdf417CompactionMode.Byte)
            {
                Pdf417ByteDecodingResult decoded = Pdf417ByteCompaction.DecodeCodewords(codewords);
                bytes.AddRange(decoded.Bytes);
                if (!decoded.ReturnsToTextMode)
                {
                    currentMode = Pdf417CompactionMode.Byte;
                    textSubmode = Pdf417TextSubmode.Alpha;
                }
            }
            else
            {
                Pdf417NumericDecodingResult decoded = Pdf417NumericCompaction.DecodeCodewords(codewords);
                bytes.AddRange(global::System.Text.Encoding.ASCII.GetBytes(decoded.Digits));
                currentMode = Pdf417CompactionMode.Numeric;
                textSubmode = Pdf417TextSubmode.Alpha;
            }

            segmentIndex++;
        }

        Assert.That(currentMode, Is.Not.EqualTo(Pdf417CompactionMode.Automatic));
        return bytes.ToArray();
    }
}
