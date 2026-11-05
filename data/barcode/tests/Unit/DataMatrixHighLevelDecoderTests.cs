namespace barcode.Tests;

public class DataMatrixHighLevelDecoderTests
{
    [Test]
    public void PaddedAsciiRecoversDigitPairsUpperShiftAndDefaultLatin1Text()
    {
        byte[] data = new byte[] { 65, 49, 50, 200 };
        int[] encoded = DataMatrixHighLevelEncoder.EncodeAscii(data);
        int[] padded = DataMatrixHighLevelEncoder.PadCodewords(encoded, 8);

        DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(padded);

        Assert.Multiple(() =>
        {
            Assert.That(result.RawBytes, Is.EqualTo(data));
            Assert.That(result.Text, Is.EqualTo(global::System.Text.Encoding.Latin1.GetString(data)));
            Assert.That(result.CharacterEncoding, Is.EqualTo("ISO-8859-1"));
            Assert.That(result.EciSegments, Is.Empty);
            Assert.That(result.ConsumedCodewordCount, Is.EqualTo(encoded.Length + 1));
            Assert.That(result.IsGs1, Is.False);
        });
    }

    [Test]
    public void EveryEncodationModeRoundTripsThroughTheHighLevelStream()
    {
        byte[][] samples = new byte[][]
        {
            global::System.Text.Encoding.ASCII.GetBytes("123456"),
            global::System.Text.Encoding.ASCII.GetBytes("AIMAIMAIM"),
            global::System.Text.Encoding.ASCII.GetBytes("aimaimaim"),
            global::System.Text.Encoding.ASCII.GetBytes("\r*>ABC"),
            global::System.Text.Encoding.ASCII.GetBytes("ABCD"),
            new byte[] { 0, 127, 128, 255 }
        };
        DataMatrixEncodingMode[] modes = new DataMatrixEncodingMode[]
        {
            DataMatrixEncodingMode.Ascii,
            DataMatrixEncodingMode.C40,
            DataMatrixEncodingMode.Text,
            DataMatrixEncodingMode.X12,
            DataMatrixEncodingMode.Edifact,
            DataMatrixEncodingMode.Base256
        };

        int index = 0;
        while (index < modes.Length)
        {
            DataMatrixEncodingPlan plan = DataMatrixMinimalEncoder.Plan(samples[index], modes[index]);
            DataMatrixHighLevelDecodeResult decoded = DataMatrixHighLevelDecoder.Decode(plan.Codewords);
            Assert.That(decoded.RawBytes, Is.EqualTo(samples[index]), modes[index].ToString());
            index++;
        }
    }

    [Test]
    public void MixedAutomaticPlansUnlatchAndResumeAsciiOrAnotherMode()
    {
        byte[] data = global::System.Text.Encoding.Latin1.GetBytes(
            "AIMAIMAIMaimaimaim\r*>ABC^^^^^^1234");
        DataMatrixEncodingPlan plan = DataMatrixMinimalEncoder.Plan(
            data,
            DataMatrixEncodingMode.Automatic);
        DataMatrixSymbolInfo symbol = DataMatrixSymbolInfoTable.FindSymbolForDataCodewords(
            plan.Codewords.Length,
            DataMatrixShape.Automatic)!;
        int[] padded = DataMatrixHighLevelEncoder.PadCodewords(
            plan.Codewords,
            symbol.DataCodewordCapacity);

        DataMatrixHighLevelDecodeResult decoded = DataMatrixHighLevelDecoder.Decode(padded);

        Assert.Multiple(() =>
        {
            Assert.That(plan.SegmentModes.Length, Is.GreaterThan(1));
            Assert.That(decoded.RawBytes, Is.EqualTo(data));
            Assert.That(decoded.Text, Is.EqualTo(global::System.Text.Encoding.Latin1.GetString(data)));
            Assert.That(decoded.ConsumedCodewordCount, Is.EqualTo(plan.Codewords.Length + 1));
        });
    }

    [Test]
    public void C40Fnc1ValueIsPreservedAsGroupSeparatorData()
    {
        int[] codewords = new int[] { 230, 10, 135, 254, 66 };

        DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(codewords);

        Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 29, 65, 65 }));
    }

    [Test]
    public void Utf8EciInterpretsBase256BytesAndRecordsTheirRange()
    {
        byte[] data = global::System.Text.Encoding.UTF8.GetBytes("Grüße 世界");
        DataMatrixOptions options = new DataMatrixOptions();
        options.EncodingMode = DataMatrixEncodingMode.Base256;
        options.EciAssignmentNumber = 26;
        DataMatrixMessagePlan plan = DataMatrixControlCodewordEncoder.CreateMessagePlan(
            data,
            options,
            false);

        DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(plan.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(result.RawBytes, Is.EqualTo(data));
            Assert.That(result.Text, Is.EqualTo("Grüße 世界"));
            Assert.That(result.CharacterEncoding, Is.EqualTo("UTF-8"));
            Assert.That(result.EciSegments, Has.Length.EqualTo(1));
            Assert.That(result.EciSegments[0].AssignmentNumber, Is.EqualTo(26));
            Assert.That(result.EciSegments[0].ByteOffset, Is.Zero);
            Assert.That(result.EciSegments[0].ByteCount, Is.EqualTo(data.Length));
        });
    }

    [Test]
    public void MultipleEciRangesInterpretIndependentlyInPayloadOrder()
    {
        int[] latin1Eci = DataMatrixControlCodewordEncoder.EncodeEciAssignment(3);
        int[] latin1Data = DataMatrixHighLevelEncoder.EncodeAscii(new byte[] { 233 });
        int[] utf8Eci = DataMatrixControlCodewordEncoder.EncodeEciAssignment(26);
        byte[] utf8Data = global::System.Text.Encoding.UTF8.GetBytes("é");
        int base256Position = latin1Eci.Length + latin1Data.Length + utf8Eci.Length + 1;
        int[] base256 = DataMatrixModeCodec.EncodeBase256(utf8Data, base256Position);
        int[] codewords = Join(latin1Eci, latin1Data, utf8Eci, base256);

        DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(codewords);

        Assert.Multiple(() =>
        {
            Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 233, 195, 169 }));
            Assert.That(result.Text, Is.EqualTo("éé"));
            Assert.That(result.CharacterEncoding, Is.Null);
            Assert.That(result.EciSegments, Has.Length.EqualTo(2));
            Assert.That(result.EciSegments[0].AssignmentNumber, Is.EqualTo(3));
            Assert.That(result.EciSegments[0].ByteOffset, Is.Zero);
            Assert.That(result.EciSegments[0].ByteCount, Is.EqualTo(1));
            Assert.That(result.EciSegments[1].AssignmentNumber, Is.EqualTo(26));
            Assert.That(result.EciSegments[1].ByteOffset, Is.EqualTo(1));
            Assert.That(result.EciSegments[1].ByteCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void BinaryAndUnknownEciAssignmentsPreserveRawBytesWithoutInventingText()
    {
        int[] binary = Join(
            DataMatrixControlCodewordEncoder.EncodeEciAssignment(899),
            DataMatrixHighLevelEncoder.EncodeAscii(new byte[] { 0, 255 }));
        int[] unknown = Join(
            DataMatrixControlCodewordEncoder.EncodeEciAssignment(500),
            DataMatrixHighLevelEncoder.EncodeAscii(new byte[] { 65, 66 }));

        DataMatrixHighLevelDecodeResult binaryResult = DataMatrixHighLevelDecoder.Decode(binary);
        DataMatrixHighLevelDecodeResult unknownResult = DataMatrixHighLevelDecoder.Decode(unknown);

        Assert.Multiple(() =>
        {
            Assert.That(binaryResult.RawBytes, Is.EqualTo(new byte[] { 0, 255 }));
            Assert.That(binaryResult.Text, Is.Null);
            Assert.That(binaryResult.CharacterEncoding, Is.EqualTo("Binary"));
            Assert.That(binaryResult.EciSegments[0].ByteCount, Is.EqualTo(2));
            Assert.That(unknownResult.RawBytes, Is.EqualTo(new byte[] { 65, 66 }));
            Assert.That(unknownResult.Text, Is.Null);
            Assert.That(unknownResult.CharacterEncoding, Is.Null);
            Assert.That(unknownResult.EciSegments[0].AssignmentNumber, Is.EqualTo(500));
        });
    }

    [Test]
    public void EveryEciDesignatorLengthDecodesAtItsExactBoundary()
    {
        int[] assignments = new int[] { 0, 126, 127, 16382, 16383, 999999 };
        int index = 0;
        while (index < assignments.Length)
        {
            int assignment = assignments[index];
            int[] codewords = Join(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(assignment),
                new int[] { 66 });
            DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(codewords);
            Assert.Multiple(() =>
            {
                Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 65 }), assignment.ToString());
                Assert.That(result.EciSegments, Has.Length.EqualTo(1), assignment.ToString());
                Assert.That(result.EciSegments[0].AssignmentNumber, Is.EqualTo(assignment));
                Assert.That(result.EciSegments[0].ByteCount, Is.EqualTo(1));
            });
            index++;
        }
    }

    [Test]
    public void EverySupportedCharacterSetEciCanInterpretAsciiBytes()
    {
        int[] assignments = new int[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11,
            15, 17, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 170
        };
        int index = 0;
        while (index < assignments.Length)
        {
            int assignment = assignments[index];
            byte[] bytes = assignment == 25
                ? new byte[] { 0, 65 }
                : new byte[] { 65 };
            int[] codewords = Join(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(assignment),
                DataMatrixHighLevelEncoder.EncodeAscii(bytes));
            DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(codewords);
            Assert.Multiple(() =>
            {
                Assert.That(result.RawBytes, Is.EqualTo(bytes), assignment.ToString());
                Assert.That(result.Text, Is.EqualTo("A"), assignment.ToString());
                Assert.That(result.CharacterEncoding, Is.Not.Null, assignment.ToString());
            });
            index++;
        }
    }

    [Test]
    public void ValidButUnavailableCharacterSetEcisStillPreserveRawBytes()
    {
        int[] assignments = new int[] { 12, 13, 16, 18 };
        int index = 0;
        while (index < assignments.Length)
        {
            int assignment = assignments[index];
            int[] codewords = Join(
                DataMatrixControlCodewordEncoder.EncodeEciAssignment(assignment),
                new int[] { 66 });
            DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(codewords);
            Assert.Multiple(() =>
            {
                Assert.That(result.RawBytes, Is.EqualTo(new byte[] { 65 }), assignment.ToString());
                Assert.That(result.Text, Is.Null, assignment.ToString());
                Assert.That(result.CharacterEncoding, Is.Null, assignment.ToString());
            });
            index++;
        }
    }

    [Test]
    public void StructuredAppendGs1ReaderProgrammingAndMacrosAreRecovered()
    {
        DataMatrixOptions appendOptions = new DataMatrixOptions();
        appendOptions.StructuredAppend = CreateAppend(1, 3, "254254");
        appendOptions.EciAssignmentNumber = 26;
        DataMatrixMessagePlan appendPlan = DataMatrixControlCodewordEncoder.CreateMessagePlan(
            global::System.Text.Encoding.UTF8.GetBytes("123456"),
            appendOptions,
            true);
        DataMatrixOptions readerOptions = new DataMatrixOptions();
        readerOptions.ReaderProgramming = true;
        DataMatrixMessagePlan readerPlan = DataMatrixControlCodewordEncoder.CreateMessagePlan(
            new byte[] { 65 },
            readerOptions,
            false);
        DataMatrixOptions macroOptions = new DataMatrixOptions();
        macroOptions.MacroType = 5;
        DataMatrixMessagePlan macroPlan = DataMatrixControlCodewordEncoder.CreateMessagePlan(
            global::System.Text.Encoding.ASCII.GetBytes("ABC"),
            macroOptions,
            false);

        DataMatrixHighLevelDecodeResult append = DataMatrixHighLevelDecoder.Decode(appendPlan.Codewords);
        DataMatrixHighLevelDecodeResult reader = DataMatrixHighLevelDecoder.Decode(readerPlan.Codewords);
        DataMatrixHighLevelDecodeResult macro = DataMatrixHighLevelDecoder.Decode(macroPlan.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(append.IsGs1, Is.True);
            Assert.That(append.RawBytes, Is.EqualTo(global::System.Text.Encoding.ASCII.GetBytes("123456")));
            Assert.That(append.StructuredAppend, Is.Not.Null);
            Assert.That(append.StructuredAppend!.Index, Is.EqualTo(1));
            Assert.That(append.StructuredAppend.Count, Is.EqualTo(3));
            Assert.That(append.StructuredAppend.FileIdentifier, Is.EqualTo("254254"));
            Assert.That(append.StructuredAppend.IsLastSegment, Is.False);
            Assert.That(reader.ReaderProgramming, Is.True);
            Assert.That(reader.RawBytes, Is.EqualTo(new byte[] { 65 }));
            Assert.That(macro.MacroType, Is.EqualTo(5));
            Assert.That(macro.RawBytes, Is.EqualTo(global::System.Text.Encoding.ASCII.GetBytes("ABC")));
            Assert.That(macro.Text, Is.EqualTo("[)>\u001E05\u001DABC\u001E\u0004"));
        });
    }

    [Test]
    public void Base256ZeroLengthConsumesAndDerandomizesTheRemainder()
    {
        byte[] data = new byte[] { 0, 127, 128, 255 };
        int[] codewords = new int[data.Length + 2];
        codewords[0] = DataMatrixHighLevelEncoder.LatchToBase256Codeword;
        codewords[1] = DataMatrixHighLevelEncoder.Randomize255State(0, 2);
        int index = 0;
        while (index < data.Length)
        {
            codewords[index + 2] = DataMatrixHighLevelEncoder.Randomize255State(data[index], index + 3);
            index++;
        }

        DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(codewords);

        Assert.That(result.RawBytes, Is.EqualTo(data));
    }

    [Test]
    public void DecodedResultsOwnTheirRawAndEciArrays()
    {
        int[] codewords = Join(
            DataMatrixControlCodewordEncoder.EncodeEciAssignment(26),
            new int[] { 66 });
        DataMatrixHighLevelDecodeResult result = DataMatrixHighLevelDecoder.Decode(codewords);
        codewords[codewords.Length - 1] = 91;
        result.RawBytes[0] = 90;
        result.EciSegments[0].AssignmentNumber = 3;

        DataMatrixHighLevelDecodeResult repeated = DataMatrixHighLevelDecoder.Decode(
            Join(DataMatrixControlCodewordEncoder.EncodeEciAssignment(26), new int[] { 66 }));

        Assert.Multiple(() =>
        {
            Assert.That(repeated.RawBytes, Is.EqualTo(new byte[] { 65 }));
            Assert.That(repeated.EciSegments[0].AssignmentNumber, Is.EqualTo(26));
            Assert.That(repeated.Text, Is.EqualTo("A"));
        });
    }

    [Test]
    public void MalformedStreamsAreRejected()
    {
        int[] tooMany = new int[DataMatrixSymbolInfoTable.MaximumDataCodewordCapacity + 1];

        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(null!), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[0]), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(tooMany), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { -1 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 256 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 0 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 242 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 235 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 230, 89 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 240, 4, 32 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 231 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 241 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 241, 0 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 241, 128 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 233, 15 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 66, 233, 15, 1, 2 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 66, 236 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixHighLevelDecoder.Decode(new int[] { 66, 129, 1 }), Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    static StructuredAppendInfo CreateAppend(int index, int count, string fileIdentifier)
    {
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.DataMatrix;
        append.Index = index;
        append.Count = count;
        append.FileIdentifier = fileIdentifier;
        return append;
    }

    static int[] Join(int[] first, int[] second)
    {
        return Join(first, second, new int[0], new int[0]);
    }

    static int[] Join(int[] first, int[] second, int[] third, int[] fourth)
    {
        int[] result = new int[first.Length + second.Length + third.Length + fourth.Length];
        int target = 0;
        int source = 0;
        while (source < first.Length)
        {
            result[target++] = first[source++];
        }

        source = 0;
        while (source < second.Length)
        {
            result[target++] = second[source++];
        }

        source = 0;
        while (source < third.Length)
        {
            result[target++] = third[source++];
        }

        source = 0;
        while (source < fourth.Length)
        {
            result[target++] = fourth[source++];
        }

        return result;
    }
}
