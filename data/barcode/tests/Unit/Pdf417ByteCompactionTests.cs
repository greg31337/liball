namespace barcode.Tests;

public class Pdf417ByteCompactionTests
{
    [Test]
    public void SingleByteInTextModeUsesTemporary913ShiftAndReturnsToText()
    {
        byte[] source = new byte[] { 200 };

        Pdf417ByteEncodingResult encoded = Pdf417ByteCompaction.EncodeBytes(source, true);
        Pdf417ByteDecodingResult decoded = Pdf417ByteCompaction.DecodeCodewords(
            encoded.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ModeCodeword, Is.EqualTo(913));
            Assert.That(encoded.DataCodewords, Is.EqualTo(new int[] { 200 }));
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 913, 200 }));
            Assert.That(encoded.ReturnsToTextMode, Is.True);
            Assert.That(decoded.Bytes, Is.EqualTo(source));
            Assert.That(decoded.ModeCodeword, Is.EqualTo(913));
            Assert.That(decoded.ConsumedCodewordCount, Is.EqualTo(2));
            Assert.That(decoded.ReturnsToTextMode, Is.True);
        });
    }

    [Test]
    public void CompleteSixByteGroupsUse924AndMatchBase900ReferenceVectors()
    {
        byte[] source = global::System.Text.Encoding.ASCII.GetBytes("ABCDEFabcdef");
        int[] expectedData = new int[]
        {
            109, 326, 368, 127, 330,
            163, 179, 507, 603, 522
        };

        Pdf417ByteEncodingResult encoded = Pdf417ByteCompaction.EncodeBytes(source, false);
        Pdf417ByteDecodingResult decoded = Pdf417ByteCompaction.DecodeDataCodewords(
            expectedData,
            Pdf417ByteCompaction.LatchSixByteCodeword);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ModeCodeword, Is.EqualTo(924));
            Assert.That(encoded.DataCodewords, Is.EqualTo(expectedData));
            Assert.That(encoded.Codewords, Is.EqualTo(new int[]
            {
                924, 109, 326, 368, 127, 330,
                163, 179, 507, 603, 522
            }));
            Assert.That(encoded.ReturnsToTextMode, Is.False);
            Assert.That(decoded.Bytes, Is.EqualTo(source));
            Assert.That(decoded.DataCodewords, Is.EqualTo(expectedData));
            Assert.That(decoded.ConsumedCodewordCount, Is.EqualTo(11));
        });
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void Mode901PacksSixByteGroupsAndPreservesEveryRemainderLength(int remainderLength)
    {
        byte[] source = new byte[6 + remainderLength];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = (byte)index;
            index++;
        }

        Pdf417ByteEncodingResult encoded = Pdf417ByteCompaction.EncodeBytes(source, false);
        Pdf417ByteDecodingResult decoded = Pdf417ByteCompaction.DecodeCodewords(
            encoded.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ModeCodeword, Is.EqualTo(901));
            Assert.That(encoded.DataCodewords.Take(5), Is.EqualTo(new int[] { 0, 5, 844, 88, 165 }));
            Assert.That(encoded.DataCodewords.Skip(5), Is.EqualTo(source.Skip(6).Select(value => (int)value)));
            Assert.That(encoded.DataCodewords, Has.Length.EqualTo(5 + remainderLength));
            Assert.That(decoded.Bytes, Is.EqualTo(source));
            Assert.That(decoded.ReturnsToTextMode, Is.False);
        });
    }

    [Test]
    public void FiveDirectRemainderCodewordsAreNotMistakenForOnePackedGroup()
    {
        byte[] source = new byte[] { 1, 2, 3, 254, 255 };

        Pdf417ByteEncodingResult encoded = Pdf417ByteCompaction.EncodeWithMode(
            source,
            Pdf417ByteCompaction.LatchWithRemainderCodeword);
        Pdf417ByteDecodingResult decoded = Pdf417ByteCompaction.DecodeCodewords(
            encoded.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Codewords, Is.EqualTo(new int[] { 901, 1, 2, 3, 254, 255 }));
            Assert.That(decoded.Bytes, Is.EqualTo(source));
            Assert.That(decoded.ConsumedCodewordCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void AutomaticModeSelectionDistinguishesTextShiftCompleteGroupsAndRemainders()
    {
        Pdf417ByteEncodingResult shifted = Pdf417ByteCompaction.EncodeBytes(
            new byte[] { 1 },
            true);
        Pdf417ByteEncodingResult latchedSingle = Pdf417ByteCompaction.EncodeBytes(
            new byte[] { 1 },
            false);
        Pdf417ByteEncodingResult complete = Pdf417ByteCompaction.EncodeBytes(
            new byte[6],
            true);
        Pdf417ByteEncodingResult remainder = Pdf417ByteCompaction.EncodeBytes(
            new byte[7],
            true);

        Assert.Multiple(() =>
        {
            Assert.That(shifted.ModeCodeword, Is.EqualTo(913));
            Assert.That(latchedSingle.ModeCodeword, Is.EqualTo(901));
            Assert.That(complete.ModeCodeword, Is.EqualTo(924));
            Assert.That(remainder.ModeCodeword, Is.EqualTo(901));
        });
    }

    [Test]
    public void AllByteValuesAndRepeatedPackedGroupsRoundTripExactly()
    {
        byte[] source = new byte[256];
        int index = 0;
        while (index < source.Length)
        {
            source[index] = (byte)index;
            index++;
        }

        Pdf417ByteEncodingResult encoded = Pdf417ByteCompaction.EncodeBytes(source, false);
        Pdf417ByteDecodingResult decoded = Pdf417ByteCompaction.DecodeCodewords(
            encoded.Codewords);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.ModeCodeword, Is.EqualTo(901));
            Assert.That(decoded.Bytes, Is.EqualTo(source));
            Assert.That(decoded.DataCodewords, Is.EqualTo(encoded.DataCodewords));
        });
    }

    [Test]
    public void ResultsOwnCallerAndResultBuffers()
    {
        byte[] source = global::System.Text.Encoding.ASCII.GetBytes("ABCDEFG");
        Pdf417ByteEncodingResult encoded = Pdf417ByteCompaction.EncodeBytes(source, false);
        Pdf417ByteDecodingResult decoded = Pdf417ByteCompaction.DecodeDataCodewords(
            encoded.DataCodewords,
            encoded.ModeCodeword);
        byte expectedByte = encoded.Bytes[0];
        int expectedCodeword = encoded.Codewords[1];
        source[0] = 0;
        encoded.DataCodewords[0] = 0;
        decoded.DataCodewords[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Bytes[0], Is.EqualTo(expectedByte));
            Assert.That(encoded.Codewords[1], Is.EqualTo(expectedCodeword));
            Assert.That(decoded.Bytes[0], Is.EqualTo(expectedByte));
        });
    }

    [Test]
    public void InvalidModesLengthsCodewordsAndPackedValuesAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Pdf417ByteCompaction.EncodeBytes(null!, false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417ByteCompaction.EncodeBytes(new byte[0], false), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417ByteCompaction.EncodeWithMode(new byte[2], 913), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417ByteCompaction.EncodeWithMode(new byte[7], 924), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417ByteCompaction.EncodeWithMode(new byte[6], 901), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417ByteCompaction.EncodeWithMode(new byte[1], 900), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Pdf417ByteCompaction.DecodeCodewords(null!), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417ByteCompaction.DecodeCodewords(new int[0]), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417ByteCompaction.DecodeCodewords(new int[] { 901 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417ByteCompaction.DecodeCodewords(new int[] { 900, 1 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417ByteCompaction.DecodeCodewords(new int[] { 913, 1, 2 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417ByteCompaction.DecodeCodewords(new int[] { 913, 256 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417ByteCompaction.DecodeCodewords(new int[] { 924, 1 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(() => Pdf417ByteCompaction.DecodeCodewords(new int[] { 901, 256 }), Throws.TypeOf<BarcodeDecodingException>());
            Assert.That(
                () => Pdf417ByteCompaction.DecodeCodewords(new int[] { 924, 899, 899, 899, 899, 899 }),
                Throws.TypeOf<BarcodeDecodingException>());
        });
    }
}
