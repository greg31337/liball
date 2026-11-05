namespace barcode.Tests;

public class DataMatrixHighLevelEncoderTests
{
    [Test]
    public void AsciiEncodationMatchesValuePlusOneForEverySevenBitByte()
    {
        int value = 0;
        while (value <= 127)
        {
            int[] encoded = DataMatrixHighLevelEncoder.EncodeAscii(new byte[] { (byte)value });
            Assert.That(encoded, Is.EqualTo(new int[] { value + 1 }), $"Byte {value}");
            value++;
        }
    }

    [Test]
    public void EveryExtendedByteUsesUpperShiftAndTheReducedValue()
    {
        int value = 128;
        while (value <= 255)
        {
            int[] encoded = DataMatrixHighLevelEncoder.EncodeAscii(new byte[] { (byte)value });
            Assert.That(
                encoded,
                Is.EqualTo(new int[] { DataMatrixHighLevelEncoder.UpperShiftCodeword, value - 127 }),
                $"Byte {value}");
            value++;
        }
    }

    [Test]
    public void EveryAsciiDigitPairMapsToCodewords130Through229()
    {
        int value = 0;
        while (value <= 99)
        {
            int first = 48 + (value / 10);
            int second = 48 + (value % 10);
            int expected = 130 + value;
            Assert.Multiple(() =>
            {
                Assert.That(DataMatrixHighLevelEncoder.EncodeDigitPair(first, second), Is.EqualTo(expected));
                Assert.That(
                    DataMatrixHighLevelEncoder.EncodeAscii(new byte[] { (byte)first, (byte)second }),
                    Is.EqualTo(new int[] { expected }));
            });
            value++;
        }
    }

    [TestCase("12345", new int[] { 142, 164, 54 })]
    [TestCase("A12B34", new int[] { 66, 142, 67, 164 })]
    [TestCase("1A23B4", new int[] { 50, 66, 153, 67, 53 })]
    [TestCase("001099", new int[] { 130, 140, 229 })]
    public void DigitPairCompactionIsGreedyAndRequiresAdjacentDigits(string text, int[] expected)
    {
        int[] actual = DataMatrixHighLevelEncoder.EncodeAscii(global::System.Text.Encoding.ASCII.GetBytes(text));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void MixedAsciiExampleMatchesIndependentReferenceVector()
    {
        byte[] data = new byte[] { 32, 50, 51, 163 };

        int[] encoded = DataMatrixHighLevelEncoder.EncodeAscii(data);
        int[] padded = DataMatrixHighLevelEncoder.EncodeAsciiToCapacity(data, 5);

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(new int[] { 33, 153, 235, 36 }));
            Assert.That(padded, Is.EqualTo(new int[] { 33, 153, 235, 36, 129 }));
        });
    }

    [Test]
    public void MixedEncodationRoundTripsEveryByteThroughAnIndependentAsciiDecoder()
    {
        byte[] data = new byte[256];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = (byte)index;
            index++;
        }

        int[] codewords = DataMatrixHighLevelEncoder.EncodeAscii(data);
        byte[] decoded = DecodeAsciiReference(codewords);

        Assert.Multiple(() =>
        {
            Assert.That(decoded, Is.EqualTo(data));
            Assert.That(codewords, Has.Length.LessThan(512));
        });
    }

    [Test]
    public void SegmentEncodationHonorsBoundariesAndAllowsAnEmptyPlanningSegment()
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes("A1234Z");

        Assert.Multiple(() =>
        {
            Assert.That(
                DataMatrixHighLevelEncoder.EncodeAsciiSegment(data, 1, 4),
                Is.EqualTo(new int[] { 142, 164 }));
            Assert.That(
                DataMatrixHighLevelEncoder.EncodeAsciiSegment(data, 0, data.Length),
                Is.EqualTo(new int[] { 66, 142, 164, 91 }));
            Assert.That(DataMatrixHighLevelEncoder.EncodeAsciiSegment(data, data.Length, 0), Is.Empty);
        });
    }

    [TestCase("12345A", 0, 5)]
    [TestCase("12345A", 2, 3)]
    [TestCase("A12345", 0, 0)]
    [TestCase("A12345", 1, 5)]
    [TestCase("A12345", 6, 0)]
    public void ConsecutiveDigitCountingStopsAtTheFirstNonDigit(
        string text,
        int startIndex,
        int expectedCount)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes(text);

        Assert.That(
            DataMatrixHighLevelEncoder.CountConsecutiveDigits(data, startIndex),
            Is.EqualTo(expectedCount));
    }

    [TestCase(DataMatrixEncodingMode.C40, 230)]
    [TestCase(DataMatrixEncodingMode.Base256, 231)]
    [TestCase(DataMatrixEncodingMode.X12, 238)]
    [TestCase(DataMatrixEncodingMode.Text, 239)]
    [TestCase(DataMatrixEncodingMode.Edifact, 240)]
    public void EveryNonAsciiModeHasTheStandardLatchCodeword(
        DataMatrixEncodingMode mode,
        int expectedCodeword)
    {
        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixHighLevelEncoder.GetLatchCodeword(mode), Is.EqualTo(expectedCodeword));
            Assert.That(DataMatrixHighLevelEncoder.FindModeForLatchCodeword(expectedCodeword), Is.EqualTo(mode));
            Assert.That(DataMatrixHighLevelEncoder.IsLatchCodeword(expectedCodeword), Is.True);
        });
    }

    [Test]
    public void ControlCodewordConstantsMatchTheAsciiEncodationTable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixHighLevelEncoder.PadCodeword, Is.EqualTo(129));
            Assert.That(DataMatrixHighLevelEncoder.LatchToC40Codeword, Is.EqualTo(230));
            Assert.That(DataMatrixHighLevelEncoder.LatchToBase256Codeword, Is.EqualTo(231));
            Assert.That(DataMatrixHighLevelEncoder.UpperShiftCodeword, Is.EqualTo(235));
            Assert.That(DataMatrixHighLevelEncoder.LatchToX12Codeword, Is.EqualTo(238));
            Assert.That(DataMatrixHighLevelEncoder.LatchToTextCodeword, Is.EqualTo(239));
            Assert.That(DataMatrixHighLevelEncoder.LatchToEdifactCodeword, Is.EqualTo(240));
            Assert.That(DataMatrixHighLevelEncoder.UnlatchCodeword, Is.EqualTo(254));
            Assert.That(DataMatrixHighLevelEncoder.FindModeForLatchCodeword(129), Is.EqualTo(DataMatrixEncodingMode.Automatic));
            Assert.That(DataMatrixHighLevelEncoder.FindModeForLatchCodeword(254), Is.EqualTo(DataMatrixEncodingMode.Automatic));
            Assert.That(DataMatrixHighLevelEncoder.IsLatchCodeword(235), Is.False);
        });
    }

    [Test]
    public void PaddingUsesOneLiteralPadThenAbsolutePosition253StateValues()
    {
        int[] padded = DataMatrixHighLevelEncoder.PadCodewords(new int[] { 66 }, 5);
        int[] fromEmpty = DataMatrixHighLevelEncoder.PadCodewords(new int[0], 3);

        Assert.Multiple(() =>
        {
            Assert.That(padded, Is.EqualTo(new int[] { 66, 129, 70, 220, 115 }));
            Assert.That(fromEmpty, Is.EqualTo(new int[] { 129, 175, 70 }));
        });
    }

    [Test]
    public void PaddingAtExactCapacityCopiesWithoutAddingAPad()
    {
        int[] input = new int[] { 142, 164, 186 };

        int[] padded = DataMatrixHighLevelEncoder.PadCodewords(input, input.Length);
        padded[0] = 1;

        Assert.Multiple(() =>
        {
            Assert.That(padded, Is.EqualTo(new int[] { 1, 164, 186 }));
            Assert.That(input, Is.EqualTo(new int[] { 142, 164, 186 }));
        });
    }

    [Test]
    public void RandomizedPadReferenceSequenceMatchesThe253StateAlgorithm()
    {
        int[] expected = new int[] { 25, 175, 70, 220, 115, 11, 161, 56, 206, 101 };
        int position = 1;
        while (position <= expected.Length)
        {
            Assert.That(
                DataMatrixHighLevelEncoder.Randomize253State(129, position),
                Is.EqualTo(expected[position - 1]),
                $"Position {position}");
            position++;
        }
    }

    [Test]
    public void StateRandomizersAreExactInversesAcrossCyclesAndBoundaryValues()
    {
        int[] values253 = new int[] { 1, 29, 129, 254 };
        int[] values255 = new int[] { 0, 1, 127, 255 };
        int position = 1;
        while (position <= 600)
        {
            int index = 0;
            while (index < values253.Length)
            {
                int randomized = DataMatrixHighLevelEncoder.Randomize253State(values253[index], position);
                Assert.That(randomized, Is.InRange(1, 254));
                Assert.That(
                    DataMatrixHighLevelEncoder.Derandomize253State(randomized, position),
                    Is.EqualTo(values253[index]));
                index++;
            }

            index = 0;
            while (index < values255.Length)
            {
                int randomized = DataMatrixHighLevelEncoder.Randomize255State(values255[index], position);
                Assert.That(randomized, Is.InRange(0, 255));
                Assert.That(
                    DataMatrixHighLevelEncoder.Derandomize255State(randomized, position),
                    Is.EqualTo(values255[index]));
                index++;
            }

            position++;
        }

        int maximumPositionRandomized = DataMatrixHighLevelEncoder.Randomize255State(255, int.MaxValue);
        Assert.That(
            DataMatrixHighLevelEncoder.Derandomize255State(maximumPositionRandomized, int.MaxValue),
            Is.EqualTo(255));
    }

    [Test]
    public void InvalidAsciiInputsRangesDigitsModesAndCapacitiesAreRejected()
    {
        byte[] data = new byte[] { 65, 66 };
        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixHighLevelEncoder.EncodeAscii(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixHighLevelEncoder.EncodeAscii(new byte[0]), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixHighLevelEncoder.EncodeAsciiSegment(null!, 0, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.EncodeAsciiSegment(data, -1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.EncodeAsciiSegment(data, 1, 2), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.CountConsecutiveDigits(null!, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.CountConsecutiveDigits(data, 3), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.EncodeDigitPair('A', '0'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.EncodeDigitPair('0', 128), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.GetLatchCodeword(DataMatrixEncodingMode.Ascii), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.GetLatchCodeword(DataMatrixEncodingMode.Automatic), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.GetLatchCodeword((DataMatrixEncodingMode)99), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.PadCodewords(null!, 3), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.PadCodewords(new int[0], -1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.PadCodewords(new int[0], 1559), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.PadCodewords(new int[] { 1, 2 }, 1), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixHighLevelEncoder.PadCodewords(new int[] { -1 }, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.PadCodewords(new int[] { 256 }, 1), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void InvalidRandomizerValuesAndPositionsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixHighLevelEncoder.Randomize253State(0, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.Randomize253State(255, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.Derandomize253State(1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.Randomize255State(-1, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.Randomize255State(256, 1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixHighLevelEncoder.Derandomize255State(0, -1), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static byte[] DecodeAsciiReference(int[] codewords)
    {
        global::System.Collections.Generic.List<byte> output = new global::System.Collections.Generic.List<byte>();
        int index = 0;
        while (index < codewords.Length)
        {
            int codeword = codewords[index];
            if (codeword <= 128)
            {
                output.Add((byte)(codeword - 1));
                index++;
            }
            else if (codeword <= 229)
            {
                int pair = codeword - 130;
                output.Add((byte)(48 + (pair / 10)));
                output.Add((byte)(48 + (pair % 10)));
                index++;
            }
            else if (codeword == 235)
            {
                output.Add((byte)(codewords[index + 1] + 127));
                index = index + 2;
            }
            else
            {
                throw new global::System.InvalidOperationException("Unexpected ASCII test codeword.");
            }
        }

        return output.ToArray();
    }
}
