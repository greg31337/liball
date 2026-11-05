namespace barcode.Tests;

public class DataMatrixModeCodecTests
{
    [TestCase("ABC", new int[] { 230, 89, 233, 254 })]
    [TestCase("AIM", new int[] { 230, 91, 11, 254 })]
    [TestCase("AIMAIMAIM", new int[] { 230, 91, 11, 91, 11, 91, 11, 254 })]
    [TestCase("ABCD", new int[] { 230, 89, 233, 254, 69 })]
    [TestCase("AB", new int[] { 66, 67 })]
    public void C40BasicAndAsciiTailVectorsAreExact(string text, int[] expected)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes(text);

        int[] codewords = DataMatrixModeCodec.EncodeC40(data);

        Assert.Multiple(() =>
        {
            Assert.That(codewords, Is.EqualTo(expected));
            Assert.That(DataMatrixModeCodec.DecodeC40(codewords), Is.EqualTo(data));
        });
    }

    [TestCase("abc", new int[] { 239, 89, 233, 254 })]
    [TestCase("aim", new int[] { 239, 91, 11, 254 })]
    [TestCase("aimaimaim", new int[] { 239, 91, 11, 91, 11, 91, 11, 254 })]
    [TestCase("abcd", new int[] { 239, 89, 233, 254, 101 })]
    [TestCase("ab", new int[] { 98, 99 })]
    public void TextBasicAndAsciiTailVectorsAreExact(string text, int[] expected)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes(text);

        int[] codewords = DataMatrixModeCodec.EncodeText(data);

        Assert.Multiple(() =>
        {
            Assert.That(codewords, Is.EqualTo(expected));
            Assert.That(DataMatrixModeCodec.DecodeText(codewords), Is.EqualTo(data));
        });
    }

    [Test]
    public void C40AndTextRoundTripEveryByteAndEveryPossibleTailBoundary()
    {
        byte[] complete = new byte[256];
        int index = 0;
        while (index < complete.Length)
        {
            complete[index] = (byte)index;
            index++;
        }

        int length = 1;
        while (length <= complete.Length)
        {
            byte[] prefix = CopyPrefix(complete, length);
            int[] c40 = DataMatrixModeCodec.EncodeC40(prefix);
            int[] text = DataMatrixModeCodec.EncodeText(prefix);
            Assert.Multiple(() =>
            {
                Assert.That(DataMatrixModeCodec.DecodeC40(c40), Is.EqualTo(prefix), $"C40 length {length}");
                Assert.That(DataMatrixModeCodec.DecodeText(text), Is.EqualTo(prefix), $"Text length {length}");
            });
            length++;
        }
    }

    [Test]
    public void C40AndTextShiftSetsCoverControlsPunctuationCaseAndUpperShift()
    {
        byte[] data = new byte[]
        {
            0, 13, 31, 32, 33, 47, 48, 57, 58, 64, 65, 90, 91, 95, 96, 97,
            122, 123, 127, 128, 159, 160, 191, 192, 223, 224, 255
        };

        int[] c40 = DataMatrixModeCodec.EncodeC40(data);
        int[] text = DataMatrixModeCodec.EncodeText(data);

        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixModeCodec.DecodeC40(c40), Is.EqualTo(data));
            Assert.That(DataMatrixModeCodec.DecodeText(text), Is.EqualTo(data));
            Assert.That(c40, Does.Contain(DataMatrixHighLevelEncoder.LatchToC40Codeword));
            Assert.That(text, Does.Contain(DataMatrixHighLevelEncoder.LatchToTextCodeword));
        });
    }

    [TestCase("\r*>", new int[] { 238, 0, 43, 254 })]
    [TestCase("ABC", new int[] { 238, 89, 233, 254 })]
    [TestCase("ABC1", new int[] { 238, 89, 233, 254, 50 })]
    [TestCase("AB", new int[] { 66, 67 })]
    public void X12ReferenceVectorsAndAsciiTailsAreExact(string text, int[] expected)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes(text);

        int[] codewords = DataMatrixModeCodec.EncodeX12(data);

        Assert.Multiple(() =>
        {
            Assert.That(codewords, Is.EqualTo(expected));
            Assert.That(DataMatrixModeCodec.DecodeX12(codewords), Is.EqualTo(data));
        });
    }

    [Test]
    public void X12RoundTripsItsCompleteAlphabetAcrossRemainderLengths()
    {
        byte[] alphabet = global::System.Text.Encoding.ASCII.GetBytes("\r*> 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        int length = 1;
        while (length <= alphabet.Length)
        {
            byte[] prefix = CopyPrefix(alphabet, length);
            int[] codewords = DataMatrixModeCodec.EncodeX12(prefix);
            Assert.That(DataMatrixModeCodec.DecodeX12(codewords), Is.EqualTo(prefix), $"Length {length}");
            length++;
        }
    }

    [Test]
    public void TripletModesAcceptImplicitUnlatchAtEndOfData()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixModeCodec.DecodeC40(new int[] { 230, 89, 233 }), Is.EqualTo(new byte[] { 65, 66, 67 }));
            Assert.That(DataMatrixModeCodec.DecodeText(new int[] { 239, 89, 233 }), Is.EqualTo(new byte[] { 97, 98, 99 }));
            Assert.That(DataMatrixModeCodec.DecodeX12(new int[] { 238, 0, 43 }), Is.EqualTo(new byte[] { 13, 42, 62 }));
        });
    }

    [TestCase("A", new int[] { 240, 5, 240 })]
    [TestCase("AB", new int[] { 240, 4, 39, 192 })]
    [TestCase("ABC", new int[] { 240, 4, 32, 223 })]
    [TestCase("ABCD", new int[] { 240, 4, 32, 196, 124 })]
    public void EdifactSixBitPackingAndUnlatchVectorsAreExact(string text, int[] expected)
    {
        byte[] data = global::System.Text.Encoding.ASCII.GetBytes(text);

        int[] codewords = DataMatrixModeCodec.EncodeEdifact(data);

        Assert.Multiple(() =>
        {
            Assert.That(codewords, Is.EqualTo(expected));
            Assert.That(DataMatrixModeCodec.DecodeEdifact(codewords), Is.EqualTo(data));
        });
    }

    [Test]
    public void EdifactRoundTripsEveryPermittedByteAcrossEveryRemainder()
    {
        byte[] alphabet = new byte[63];
        int index = 0;
        while (index < alphabet.Length)
        {
            alphabet[index] = (byte)(32 + index);
            index++;
        }

        int length = 1;
        while (length <= alphabet.Length)
        {
            byte[] prefix = CopyPrefix(alphabet, length);
            int[] codewords = DataMatrixModeCodec.EncodeEdifact(prefix);
            Assert.That(DataMatrixModeCodec.DecodeEdifact(codewords), Is.EqualTo(prefix), $"Length {length}");
            length++;
        }
    }

    [Test]
    public void EdifactAcceptsImplicitUnlatchAfterACompleteFourValueGroup()
    {
        Assert.That(
            DataMatrixModeCodec.DecodeEdifact(new int[] { 240, 4, 32, 196 }),
            Is.EqualTo(new byte[] { 65, 66, 67, 68 }));
    }

    [Test]
    public void Base256ReferenceVectorMatchesAbsolutePositionRandomization()
    {
        byte[] data = new byte[] { 171, 228, 246, 252, 233, 187 };

        int[] codewords = DataMatrixModeCodec.EncodeBase256(data, 1);

        Assert.Multiple(() =>
        {
            Assert.That(codewords, Is.EqualTo(new int[] { 231, 50, 108, 59, 226, 126, 1, 104 }));
            Assert.That(DataMatrixModeCodec.DecodeBase256(codewords, 1), Is.EqualTo(data));
        });
    }

    [TestCase(1)]
    [TestCase(249)]
    [TestCase(250)]
    [TestCase(251)]
    [TestCase(1555)]
    public void Base256RoundTripsEveryLengthFieldBoundary(int length)
    {
        byte[] data = CreateDeterministicBytes(length);

        int[] atStart = DataMatrixModeCodec.EncodeBase256(data, 1);
        int[] afterPrefix = DataMatrixModeCodec.EncodeBase256(data, 23);

        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixModeCodec.DecodeBase256(atStart, 1), Is.EqualTo(data));
            Assert.That(DataMatrixModeCodec.DecodeBase256(afterPrefix, 23), Is.EqualTo(data));
            Assert.That(atStart[0], Is.EqualTo(231));
            Assert.That(afterPrefix[0], Is.EqualTo(231));
            Assert.That(afterPrefix, Is.Not.EqualTo(atStart));
            Assert.That(
                atStart.Length,
                Is.EqualTo(length + (length <= 249 ? 2 : 3)));
        });
    }

    [Test]
    public void Base256DecoderAcceptsZeroLengthFieldMeaningRemainderOfSegment()
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

        Assert.That(DataMatrixModeCodec.DecodeBase256(codewords, 1), Is.EqualTo(data));
    }

    [Test]
    public void ModeConstantsDescribeTripletEdifactAndBase256Limits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DataMatrixModeCodec.C40Shift1Value, Is.EqualTo(0));
            Assert.That(DataMatrixModeCodec.C40Shift2Value, Is.EqualTo(1));
            Assert.That(DataMatrixModeCodec.C40Shift3Value, Is.EqualTo(2));
            Assert.That(DataMatrixModeCodec.C40UpperShiftValue, Is.EqualTo(30));
            Assert.That(DataMatrixModeCodec.EdifactUnlatchValue, Is.EqualTo(31));
            Assert.That(DataMatrixModeCodec.MaximumBase256DataCount, Is.EqualTo(1555));
        });
    }

    [Test]
    public void EncodersRejectMissingEmptyOrModeInvalidData()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixModeCodec.EncodeC40(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeC40(new byte[0]), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeText(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeText(new byte[0]), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeX12(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeX12(new byte[0]), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeX12(new byte[] { (byte)'a' }), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeX12(new byte[] { 128 }), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeEdifact(null!), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeEdifact(new byte[0]), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeEdifact(new byte[] { 31 }), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeEdifact(new byte[] { 95 }), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeBase256(null!, 1), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeBase256(new byte[0], 1), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => DataMatrixModeCodec.EncodeBase256(new byte[] { 1 }, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => DataMatrixModeCodec.EncodeBase256(new byte[1556], 1), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void TripletDecodersRejectMalformedFramingPairsShiftsAndTails()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixModeCodec.DecodeC40(null!), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeC40(new int[0]), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeC40(new int[] { 230, 89 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeC40(new int[] { 230, 10, 164, 254 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeC40(new int[] { 230, 19, 58, 254 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeText(new int[] { 239, 89, 233, 254, 235 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeText(new int[] { 239, -1, 0, 254 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeX12(new int[] { 238, 0 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeX12(new int[] { 238, 0, 43, 254, 240 }), Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    [Test]
    public void EdifactDecoderRejectsMalformedLatchUnlatchRangeAndTrailingBits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixModeCodec.DecodeEdifact(null!), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeEdifact(new int[0]), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeEdifact(new int[] { 239, 124 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeEdifact(new int[] { 240, 4, 32 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeEdifact(new int[] { 240, 125 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeEdifact(new int[] { 240, 124, 0 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeEdifact(new int[] { 240, 256 }), Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    [Test]
    public void Base256DecoderRejectsMalformedLatchLengthRangeAndPosition()
    {
        int[] oneByte = DataMatrixModeCodec.EncodeBase256(new byte[] { 42 }, 1);
        int[] tooLong = DataMatrixModeCodec.EncodeBase256(new byte[250], 1);
        int[] wrongLength = CopyIntegers(oneByte);
        wrongLength[1] = DataMatrixHighLevelEncoder.Randomize255State(2, 2);
        int[] truncatedLong = CopyPrefixIntegers(tooLong, 2);

        Assert.Multiple(() =>
        {
            Assert.That(() => DataMatrixModeCodec.DecodeBase256(null!, 1), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeBase256(new int[0], 1), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeBase256(new int[] { 230, 1 }, 1), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeBase256(new int[] { 231 }, 1), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeBase256(wrongLength, 1), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeBase256(truncatedLong, 1), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeBase256(new int[] { 231, 256 }, 1), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => DataMatrixModeCodec.DecodeBase256(oneByte, 0), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static byte[] CopyPrefix(byte[] source, int length)
    {
        byte[] copy = new byte[length];
        global::System.Array.Copy(source, copy, length);
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

    static int[] CopyIntegers(int[] source)
    {
        return CopyPrefixIntegers(source, source.Length);
    }

    static int[] CopyPrefixIntegers(int[] source, int length)
    {
        int[] copy = new int[length];
        global::System.Array.Copy(source, copy, length);
        return copy;
    }
}
